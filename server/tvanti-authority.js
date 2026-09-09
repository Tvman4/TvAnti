#!/usr/bin/env node
/**
 * TvAnti server authority.
 * Client packets are proposals. Bans and mutes are decided here.
 *
 *   node tvanti-authority.js
 *
 * Unity posts to:
 *   POST /v1/snapshot
 *   POST /v1/voice
 *   POST /v1/event
 *   GET  /v1/status?playerId=
 *   POST /v1/join
 */
const http = require("http");
const fs = require("fs");
const path = require("path");
const cfg = require("./config");

const state = {
  players: Object.create(null)
};

function load() {
  try {
    const raw = fs.readFileSync(cfg.dataFile, "utf8");
    const parsed = JSON.parse(raw);
    if (parsed && parsed.players) state.players = parsed.players;
  } catch (_) {
    fs.mkdirSync(path.dirname(cfg.dataFile), { recursive: true });
  }
}

function save() {
  fs.mkdirSync(path.dirname(cfg.dataFile), { recursive: true });
  fs.writeFileSync(cfg.dataFile, JSON.stringify(state, null, 2));
}

function player(id) {
  if (!state.players[id]) {
    state.players[id] = {
      id,
      name: "",
      banned: false,
      banReason: "",
      bannedAt: 0,
      muteUntil: 0,
      muteReason: "",
      lastPos: null,
      lastRot: null,
      lastTime: 0,
      lastTag: 0,
      lastRpc: {},
      lastHeartbeat: Date.now() / 1000
    };
  }
  return state.players[id];
}

function nowSec() {
  return Date.now() / 1000;
}

function isMuted(p) {
  return p.muteUntil > nowSec();
}

function ban(p, reason) {
  p.banned = true;
  p.banReason = reason;
  p.bannedAt = nowSec();
  save();
}

function mute(p, reason) {
  p.muteUntil = nowSec() + cfg.slurMuteHours * 3600;
  p.muteReason = reason;
  save();
}

function containsSlur(text) {
  if (!text) return false;
  const n = String(text).toLowerCase().replace(/[^a-z0-9\s]/g, " ");
  return cfg.slurWords.some((w) => n.indexOf(w) >= 0);
}

function dist(a, b) {
  const dx = a.x - b.x, dy = a.y - b.y, dz = a.z - b.z;
  return Math.sqrt(dx * dx + dy * dy + dz * dz);
}

function decideCheat(type, detail) {
  return {
    action: "ban",
    type,
    detail,
    banned: true,
    muteUntil: 0
  };
}

function decideMute(type, detail, p) {
  return {
    action: "mute",
    type,
    detail,
    banned: false,
    muteUntil: p.muteUntil,
    muteHours: cfg.slurMuteHours
  };
}

function validateSnapshot(body) {
  const id = String(body.playerId || "");
  if (!id) return { ok: false, error: "playerId required" };
  const p = player(id);
  p.name = body.playerName || p.name;

  if (p.banned) {
    return {
      ok: false,
      action: "kick_banned",
      banned: true,
      banReason: p.banReason,
      muteUntil: p.muteUntil
    };
  }

  const t = nowSec();
  p.lastHeartbeat = t;

  const pos = body.position;
  const rot = body.rotation;
  const hands = body.hands || {};
  const events = Array.isArray(body.events) ? body.events : [];

  if (pos && p.lastPos && p.lastTime) {
    const dt = Math.max(0.02, Math.min(t - p.lastTime, 0.35));
    const d = dist(pos, p.lastPos);
    const speed = d / dt;
    if (speed > cfg.maxHorizontalSpeed * 1.35) {
      ban(p, "Speed " + speed.toFixed(2));
      return Object.assign({ ok: false }, decideCheat("Speed", "hs=" + speed.toFixed(2)));
    }
    if (d > cfg.maxTeleportDistance) {
      ban(p, "Teleport " + d.toFixed(2));
      return Object.assign({ ok: false }, decideCheat("Teleport", "d=" + d.toFixed(2)));
    }
    if (Math.abs(pos.y - p.lastPos.y) / dt > cfg.maxVerticalSpeed && pos.y > p.lastPos.y + 0.4) {
      ban(p, "Flight");
      return Object.assign({ ok: false }, decideCheat("Flight", "vy"));
    }
  }

  if (pos && body.head && hands.left && hands.right) {
    const armL = dist(body.head, hands.left);
    const armR = dist(body.head, hands.right);
    if (armL > cfg.maxArmMeters || armR > cfg.maxArmMeters) {
      ban(p, "ArmLength");
      return Object.assign({ ok: false }, decideCheat("ArmLength", "L=" + armL.toFixed(2) + " R=" + armR.toFixed(2)));
    }
  }

  if (typeof body.timeScale === "number" && Math.abs(body.timeScale - 1) > 0.01) {
    ban(p, "TimeScale");
    return Object.assign({ ok: false }, decideCheat("TimeScale", String(body.timeScale)));
  }

  if (body.clientHash && body.manifestHash && body.clientHash !== body.manifestHash) {
    ban(p, "BuildHash");
    return Object.assign({ ok: false }, decideCheat("BuildHash", "mismatch"));
  }

  if (body.transcript && containsSlur(body.transcript)) {
    mute(p, "voice_slur");
    return Object.assign({ ok: true }, decideMute("VoicePolicy", "slur", p));
  }

  if (body.chat && containsSlur(body.chat)) {
    mute(p, "chat_slur");
    return Object.assign({ ok: true }, decideMute("ChatPolicy", "slur", p));
  }

  if (body.name && containsSlur(body.name)) {
    mute(p, "name_slur");
    return Object.assign({ ok: true }, decideMute("NamePolicy", "slur", p));
  }

  for (const ev of events) {
    const type = String(ev.type || ev.Type || "");
    if (cfg.cheatBanTypes.indexOf(type) >= 0) {
      ban(p, type + " " + (ev.detail || ""));
      return Object.assign({ ok: false }, decideCheat(type, ev.detail || ""));
    }
    if (type === "VoicePolicy" || type === "LocalStateDivergence" && /voice|slur|name/i.test(ev.detail || "")) {
      mute(p, ev.detail || type);
      return Object.assign({ ok: true }, decideMute(type, ev.detail || "", p));
    }
  }

  if (pos) p.lastPos = pos;
  if (rot) p.lastRot = rot;
  p.lastTime = t;
  save();

  return {
    ok: true,
    action: "ok",
    banned: false,
    muteUntil: p.muteUntil,
    muted: isMuted(p)
  };
}

function handleVoice(body) {
  const id = String(body.playerId || "");
  if (!id) return { ok: false, error: "playerId required" };
  const p = player(id);
  if (p.banned) return { ok: false, action: "kick_banned", banned: true, banReason: p.banReason };
  if (containsSlur(body.transcript || body.text || "")) {
    mute(p, "voice_slur");
    return Object.assign({ ok: true }, decideMute("VoicePolicy", "slur", p));
  }
  return { ok: true, action: "ok", muted: isMuted(p), muteUntil: p.muteUntil };
}

function handleJoin(body) {
  const id = String(body.playerId || "");
  if (!id) return { ok: false, error: "playerId required" };
  const p = player(id);
  p.name = body.playerName || p.name;
  if (p.banned) return { accepted: false, banned: true, banReason: p.banReason };
  const roomCount = Number(body.roomPlayers || 0);
  if (roomCount >= cfg.maxRoomPlayers) {
    return { accepted: false, banned: false, reason: "room_full" };
  }
  if (body.sessionTicket && String(body.sessionTicket).length < 16) {
    ban(p, "PlayFabTicket");
    return { accepted: false, banned: true, banReason: "PlayFabTicket" };
  }
  return {
    accepted: true,
    banned: false,
    muted: isMuted(p),
    muteUntil: p.muteUntil
  };
}

function statusOf(id) {
  const p = player(id);
  return {
    playerId: id,
    banned: !!p.banned,
    banReason: p.banReason || "",
    muted: isMuted(p),
    muteUntil: p.muteUntil || 0,
    muteHours: cfg.slurMuteHours
  };
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    let d = "";
    req.on("data", (c) => {
      d += c;
      if (d.length > 1e6) req.destroy();
    });
    req.on("end", () => {
      if (!d) return resolve({});
      try { resolve(JSON.parse(d)); }
      catch (e) { reject(e); }
    });
  });
}

function send(res, code, obj) {
  const body = JSON.stringify(obj);
  res.writeHead(code, {
    "Content-Type": "application/json",
    "Content-Length": Buffer.byteLength(body)
  });
  res.end(body);
}

function authorized(req) {
  const key = req.headers["x-tvanti-key"];
  return key && key === cfg.ingestKey;
}

load();

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url, "http://localhost");
  try {
    if (req.method === "GET" && url.pathname === "/health") {
      return send(res, 200, { ok: true, slurMuteHours: cfg.slurMuteHours });
    }

    if (!authorized(req)) return send(res, 401, { ok: false, error: "unauthorized" });

    if (req.method === "GET" && url.pathname === "/v1/status") {
      return send(res, 200, statusOf(url.searchParams.get("playerId") || ""));
    }

    const body = req.method === "POST" ? await readBody(req) : {};

    if (req.method === "POST" && url.pathname === "/v1/snapshot")
      return send(res, 200, validateSnapshot(body));
    if (req.method === "POST" && url.pathname === "/v1/voice")
      return send(res, 200, handleVoice(body));
    if (req.method === "POST" && url.pathname === "/v1/event")
      return send(res, 200, validateSnapshot({ playerId: body.playerId, playerName: body.playerName, events: [body] }));
    if (req.method === "POST" && url.pathname === "/v1/join")
      return send(res, 200, handleJoin(body));
    if (req.method === "POST" && url.pathname === "/v1/unban") {
      const p = player(String(body.playerId || ""));
      p.banned = false;
      p.banReason = "";
      p.muteUntil = 0;
      save();
      return send(res, 200, { ok: true });
    }

    send(res, 404, { ok: false, error: "not_found" });
  } catch (e) {
    send(res, 400, { ok: false, error: String(e.message || e) });
  }
});

server.listen(cfg.listenPort, () => {
  console.log("TvAnti authority on :" + cfg.listenPort + " slurMuteHours=" + cfg.slurMuteHours);
});
