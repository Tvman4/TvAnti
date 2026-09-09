using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TvAnti
{
    /// <summary>
    /// Additional defensive validators for Gorilla Tag-style VR fangames.
    /// Client results are evidence only. Authoritative decisions belong on PlayFab / your backend.
    /// Photon types are passed as primitives so this file compiles without PUN installed.
    /// </summary>
    public sealed class TvAntiExtendedGuards
    {
        readonly TvAntiConfig config;
        readonly Dictionary<string, float> lastRpc = new Dictionary<string, float>();
        readonly Dictionary<string, float> lastTag = new Dictionary<string, float>();
        readonly Dictionary<string, float> lastTeleport = new Dictionary<string, float>();
        readonly Dictionary<string, float> lastLobbyJoin = new Dictionary<string, float>();
        readonly Dictionary<string, float> lastReport = new Dictionary<string, float>();
        readonly Dictionary<string, float> lastFriendQuery = new Dictionary<string, float>();
        readonly Dictionary<string, float> lastCustomProps = new Dictionary<string, float>();
        readonly Dictionary<string, Vector3> lastSyncPos = new Dictionary<string, Vector3>();
        readonly Dictionary<string, float> lastActivity = new Dictionary<string, float>();
        readonly Dictionary<string, int> inputBurst = new Dictionary<string, int>();
        readonly Dictionary<string, float> inputBurstWindow = new Dictionary<string, float>();
        readonly HashSet<string> blockedNames;
        readonly HashSet<string> voiceBlocklist;
        readonly HashSet<string> allowedPrefabs;
        readonly HashSet<string> allowedScenes;
        readonly HashSet<string> allowedCosmetics;
        readonly HashSet<int> expectedCollisionPairs;
        float lastHeartbeat;
        Vector3 floatingOrigin;

        static readonly Regex RichColor = new Regex(
            @"</?\s*(color|size|b|i|material|quad)[^>]*>|<#?[0-9A-Fa-f]{3,8}>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        static readonly Regex Invisible = new Regex(
            @"[\u200B-\u200F\u202A-\u202E\u2060-\u206F\uFEFF]",
            RegexOptions.Compiled);

        public TvAntiExtendedGuards(TvAntiConfig config)
        {
            this.config = config;
            blockedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "mod","admin","owner","dev","staff","tvanti","moderator"
            };
            voiceBlocklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "nigger","nigga","fag","faggot","slut","retard","kike","tranny"
            };
            allowedPrefabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Player","GorillaPlayer","NetworkedPlayer","HeldItem"
            };
            allowedScenes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "GorillaTag","Forest","Canyon","City","Cave"
            };
            allowedCosmetics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            expectedCollisionPairs = new HashSet<int>();
            lastHeartbeat = Time.unscaledTime;
        }

        // --- movement / body ---

        public Violation? SpeedLimitCheck(Vector3 prev, Vector3 next, float dt)
        {
            if (dt <= 0.0001f) return null;
            float speed = (next - prev).magnitude / dt;
            float cap = Mathf.Max(1f, config.maxHorizontalSpeed);
            if (speed > cap)
                return new Violation(ViolationType.Speed, 0.85f, $"speed={speed:F2}");
            return null;
        }

        public Violation? ArmLengthValidator(Vector3 head, Vector3 leftHand, Vector3 rightHand, float maxArm = 1.15f)
        {
            float l = Vector3.Distance(head, leftHand);
            float r = Vector3.Distance(head, rightHand);
            if (l > maxArm || r > maxArm)
                return new Violation(ViolationType.HeightDelta, 0.8f, $"arm L={l:F2} R={r:F2}");
            return null;
        }

        public Violation? GravityAndGroundCheck(Vector3 origin, Vector3 velocity, float groundedSlack = 0.35f)
        {
            bool grounded = Physics.Raycast(origin + Vector3.up * 0.05f, Vector3.down, groundedSlack + 0.2f);
            if (!grounded && velocity.y > 0.15f && origin.y > 2.5f)
                return new Violation(ViolationType.VerticalVelocity, 0.75f, "ungrounded upward impulse");
            return null;
        }

        public Violation? PositionSanityFilter(string session, Vector3 pos, float maxStep)
        {
            if (lastSyncPos.TryGetValue(session, out var prev))
            {
                float d = Vector3.Distance(prev, pos);
                lastSyncPos[session] = pos;
                if (d > Mathf.Max(maxStep, config.maxTeleportDistance))
                    return new Violation(ViolationType.Teleport, 0.9f, $"jump={d:F2}");
                return null;
            }
            lastSyncPos[session] = pos;
            return null;
        }

        public Violation? RotationSpeedLimiter(Quaternion prev, Quaternion next, float dt, float maxDegPerSec)
        {
            if (dt <= 0.0001f) return null;
            float deg = Quaternion.Angle(prev, next) / dt;
            if (deg > Mathf.Max(maxDegPerSec, config.maxRotationRate))
                return new Violation(ViolationType.RotationRate, 0.7f, $"ang={deg:F1}");
            return null;
        }

        public Violation? HeadRollPitchLimitChecker(Quaternion head, float maxPitch = 85f, float maxRoll = 50f)
        {
            Vector3 e = head.eulerAngles;
            float pitch = NormalizeAngle(e.x);
            float roll = NormalizeAngle(e.z);
            if (Mathf.Abs(pitch) > maxPitch || Mathf.Abs(roll) > maxRoll)
                return new Violation(ViolationType.RotationRate, 0.65f, $"pitch={pitch:F1} roll={roll:F1}");
            return null;
        }

        public Violation? TeleportPacketValidator(Vector3 advertised, Vector3 interpolated, float tolerance)
        {
            if (Vector3.Distance(advertised, interpolated) > Mathf.Max(0.4f, tolerance))
                return new Violation(ViolationType.ImpossibleVelocity, 0.7f, "sync path mismatch");
            return null;
        }

        public Violation? TeleportCooldownTracker(string session, float cooldown = 4f)
        {
            float now = Time.unscaledTime;
            if (lastTeleport.TryGetValue(session, out var t) && now - t < cooldown)
                return new Violation(ViolationType.Teleport, 0.6f, "teleport cooldown");
            lastTeleport[session] = now;
            return null;
        }

        public Violation? StateReconciliationGuard(Vector3 client, Vector3 server, float soft, float hard)
        {
            float d = Vector3.Distance(client, server);
            if (d <= soft) return null;
            if (d > hard)
                return new Violation(ViolationType.RemoteStateDivergence, 0.7f, $"reconcile={d:F2}");
            return null;
        }

        public Vector3 FloatingOriginStabilizer(Vector3 world, float threshold = 2000f)
        {
            if (world.magnitude > threshold)
            {
                floatingOrigin += world;
                return Vector3.zero;
            }
            return world - floatingOrigin;
        }

        // --- physics ---

        public Violation? InvisiblePlatformDetector(Vector3 feet, int playerLayer)
        {
            var hits = Physics.OverlapSphere(feet + Vector3.down * 0.1f, 0.2f);
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h.gameObject.layer == playerLayer) continue;
                var r = h.GetComponent<Renderer>();
                if (r == null || !r.enabled)
                    return new Violation(ViolationType.OutOfBounds, 0.7f, "invisible collider under feet");
            }
            return null;
        }

        public Violation? NoClipCollisionEnforcer(Vector3 from, Vector3 to, int mask)
        {
            if (Physics.Linecast(from, to, mask))
                return new Violation(ViolationType.OutOfBounds, 0.8f, "moved through solid geometry");
            return null;
        }

        public Violation? MaterialFrictionValidator(Collider surface, float minFriction = 0.15f, float maxBounce = 0.85f)
        {
            if (surface == null || surface.material == null) return null;
            var m = surface.material;
            if (m.dynamicFriction < minFriction || m.bounciness > maxBounce)
                return new Violation(ViolationType.LocalStateDivergence, 0.55f, "physics material out of range");
            return null;
        }

        public Violation? PhysicsInterpenetrationCheck(Collider body, float maxPen = 0.08f)
        {
            if (body == null) return null;
            var overlaps = Physics.OverlapBox(body.bounds.center, body.bounds.extents * 0.9f);
            int count = 0;
            foreach (var o in overlaps)
                if (o != null && o != body) count++;
            if (count >= 3)
                return new Violation(ViolationType.OutOfBounds, 0.5f, "deep interpenetration");
            return maxPen > 0 ? null : null;
        }

        public Violation? WallClimbAngleValidator(Vector3 surfaceNormal, float maxSlopeDeg = 55f)
        {
            float angle = Vector3.Angle(surfaceNormal, Vector3.up);
            if (angle > maxSlopeDeg && angle < 130f)
                return new Violation(ViolationType.Acceleration, 0.6f, $"climbAngle={angle:F1}");
            return null;
        }

        public Violation? CollisionMatrixValidator(int layerA, int layerB, bool expectedIgnore)
        {
            bool ignore = Physics.GetIgnoreLayerCollision(layerA, layerB);
            if (ignore != expectedIgnore)
                return new Violation(ViolationType.LocalStateDivergence, 0.8f, $"layer matrix {layerA}/{layerB}");
            return null;
        }

        public Violation? DynamicBoneLengthLimiter(float boneLength, float maxLength = 1.4f)
        {
            if (boneLength > maxLength)
                return new Violation(ViolationType.HeightDelta, 0.45f, $"bone={boneLength:F2}");
            return null;
        }

        public Violation? TimeScaleIntegrityCheck()
        {
            if (!Mathf.Approximately(Time.timeScale, 1f))
                return new Violation(ViolationType.ClockDrift, 0.9f, $"timeScale={Time.timeScale:F2}");
            return null;
        }

        // --- network / photon-shaped ---

        public Violation? RPCValidationAndRateLimiter(string session, string rpcName, object[] payload, float minInterval = 0.05f)
        {
            if (string.IsNullOrEmpty(rpcName) || rpcName.Length > 64)
                return new Violation(ViolationType.PacketBurst, 0.7f, "rpc name rejected");
            if (payload != null && payload.Length > 16)
                return new Violation(ViolationType.PacketBurst, 0.7f, "rpc payload too large");
            float now = Time.unscaledTime;
            string key = session + ":" + rpcName;
            if (lastRpc.TryGetValue(key, out var t) && now - t < minInterval)
                return new Violation(ViolationType.InputRate, 0.6f, "rpc rate");
            lastRpc[key] = now;
            return null;
        }

        public Violation? MasterClientPropertyGuard(bool isMaster, bool mutatingRoomProps)
        {
            if (!isMaster && mutatingRoomProps)
                return new Violation(ViolationType.RemoteStateDivergence, 1f, "non-master room property write");
            return null;
        }

        public Violation? PhotonViewOwnershipCheck(int viewOwnerActor, int senderActor)
        {
            if (viewOwnerActor != senderActor)
                return new Violation(ViolationType.RemoteStateDivergence, 0.85f, "view ownership mismatch");
            return null;
        }

        public Violation? TagCooldownEnforcer(string session, float cooldown = 0.8f)
        {
            float now = Time.unscaledTime;
            if (lastTag.TryGetValue(session, out var t) && now - t < cooldown)
                return new Violation(ViolationType.InputRate, 0.75f, "tag spam");
            lastTag[session] = now;
            return null;
        }

        public Violation? InputBufferOverflowGuard(string session, int incomingCount, int maxPerSecond = 90)
        {
            float now = Time.unscaledTime;
            if (!inputBurstWindow.TryGetValue(session, out var win) || now - win >= 1f)
            {
                inputBurstWindow[session] = now;
                inputBurst[session] = incomingCount;
                return null;
            }
            inputBurst[session] = inputBurst.TryGetValue(session, out var n) ? n + incomingCount : incomingCount;
            if (inputBurst[session] > maxPerSecond)
                return new Violation(ViolationType.PacketBurst, 0.7f, "input buffer overflow");
            return null;
        }

        public Violation? PhotonRoomMaxPlayersEnforcer(int current, int incoming, int maxPlayers)
        {
            if (current + incoming > maxPlayers)
                return new Violation(ViolationType.RemoteStateDivergence, 0.9f, "room over capacity");
            return null;
        }

        public Violation? StrictRoomCustomPropertiesSanitizer(IDictionary<string, object> props)
        {
            if (props == null) return null;
            foreach (var kv in props)
            {
                if (kv.Key != null && kv.Key.StartsWith("mod_", StringComparison.OrdinalIgnoreCase))
                    return new Violation(ViolationType.RemoteStateDivergence, 0.8f, "illegal room property key");
                if (kv.Value is string s && s.Length > 128)
                    return new Violation(ViolationType.PacketBurst, 0.5f, "room property oversized");
            }
            return null;
        }

        public Violation? CustomPropertiesRateLimiter(string session, float minInterval = 0.2f)
        {
            float now = Time.unscaledTime;
            if (lastCustomProps.TryGetValue(session, out var t) && now - t < minInterval)
                return new Violation(ViolationType.InputRate, 0.45f, "custom props rate");
            lastCustomProps[session] = now;
            return null;
        }

        public Violation? NetworkInstantiationGuard(string prefab)
        {
            if (string.IsNullOrEmpty(prefab) || !allowedPrefabs.Contains(prefab))
                return new Violation(ViolationType.UnexpectedAssembly, 0.85f, "unapproved instantiate " + prefab);
            return null;
        }

        public Violation? MasterClientMigrationHandler(int oldMaster, int newMaster, bool electionAuthorized)
        {
            if (!electionAuthorized || newMaster <= 0 || newMaster == oldMaster)
                return new Violation(ViolationType.RemoteStateDivergence, 0.6f, "master migration rejected");
            return null;
        }

        public Violation? PacketLossAnomalyDetector(float reportedLoss, float measuredLoss, float spike = 0.45f)
        {
            if (reportedLoss < 0.02f && measuredLoss > spike)
                return new Violation(ViolationType.PacketBurst, 0.55f, "loss pattern anomaly");
            return null;
        }

        public Violation? LobbyJoinRateLimiter(string session, float cooldown = 1.5f)
        {
            float now = Time.unscaledTime;
            if (lastLobbyJoin.TryGetValue(session, out var t) && now - t < cooldown)
                return new Violation(ViolationType.InputRate, 0.4f, "lobby join rate");
            lastLobbyJoin[session] = now;
            return null;
        }

        public Violation? FriendListQueryGuard(string session, float cooldown = 2f)
        {
            float now = Time.unscaledTime;
            if (lastFriendQuery.TryGetValue(session, out var t) && now - t < cooldown)
                return new Violation(ViolationType.InputRate, 0.35f, "friend query throttle");
            lastFriendQuery[session] = now;
            return null;
        }

        public Violation? ObjectPoolingIntegrityCheck(int liveNetworked, int poolCap)
        {
            if (liveNetworked > poolCap)
                return new Violation(ViolationType.UnexpectedNativeModule, 0.6f, "pool overflow");
            return null;
        }

        // --- identity / text / audio ---

        public Violation? NameTagFilter(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return new Violation(ViolationType.LocalStateDivergence, 0.4f, "empty name");
            if (Invisible.IsMatch(name))
                return new Violation(ViolationType.LocalStateDivergence, 0.7f, "invisible chars in name");
            string n = Invisible.Replace(name, "").Trim();
            if (blockedNames.Contains(n))
                return new Violation(ViolationType.LocalStateDivergence, 0.8f, "restricted name");
            return null;
        }

        public string ColorCodeSanitizer(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return string.Empty;
            return RichColor.Replace(raw, string.Empty);
        }

        public Violation? VoiceAudioSpamLimiter(float peakDb, int framesThisSecond, float maxDb = -3f, int maxFrames = 50)
        {
            if (peakDb > maxDb || framesThisSecond > maxFrames)
                return new Violation(ViolationType.PacketBurst, 0.5f, "voice spam");
            return null;
        }

        public Violation? VoiceMuteStateVerifier(bool serverMuted, bool clientTransmitting)
        {
            if (serverMuted && clientTransmitting)
                return new Violation(ViolationType.RemoteStateDivergence, 0.8f, "mute bypass");
            return null;
        }

        public Violation? VoiceChecker(string transcript)
        {
            if (string.IsNullOrWhiteSpace(transcript)) return null;
            string norm = NormalizeSpeech(transcript);
            foreach (var w in voiceBlocklist)
            {
                if (norm.IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0)
                    return new Violation(ViolationType.LocalStateDivergence, 0.5f, "voice policy");
            }
            return null;
        }

        public Violation? ReportSpamThrottler(string session, float cooldown = 20f)
        {
            float now = Time.unscaledTime;
            if (lastReport.TryGetValue(session, out var t) && now - t < cooldown)
                return new Violation(ViolationType.InputRate, 0.4f, "report flood");
            lastReport[session] = now;
            return null;
        }

        // --- session / account ---

        public Violation? PlayFabAuthenticationHook(string sessionTicket)
        {
            if (string.IsNullOrEmpty(sessionTicket) || sessionTicket.Length < 16)
                return new Violation(ViolationType.RemoteStateDivergence, 1f, "missing PlayFab ticket");
            return null;
        }

        public Violation? BuildVersionHashVerifier(string clientHash, string manifestHash)
        {
            if (string.IsNullOrEmpty(clientHash) || !string.Equals(clientHash, manifestHash, StringComparison.OrdinalIgnoreCase))
                return new Violation(ViolationType.LibraryHashMatch, 0.95f, "build hash mismatch");
            return null;
        }

        public Violation? DeviceIDBlacklistChecker(string deviceId, ICollection<string> banned)
        {
            if (!string.IsNullOrEmpty(deviceId) && banned != null && banned.Contains(deviceId))
                return new Violation(ViolationType.BlockedLibrary, 1f, "device blacklisted");
            return null;
        }

        public Violation? ModeratorTokenChecker(bool playFabModTag, bool requestedAdmin)
        {
            if (requestedAdmin && !playFabModTag)
                return new Violation(ViolationType.RemoteStateDivergence, 1f, "forged moderator token");
            return null;
        }

        public Violation? SpectatorModeAuthenticator(bool allowed, bool enteringSpectator)
        {
            if (enteringSpectator && !allowed)
                return new Violation(ViolationType.RemoteStateDivergence, 0.7f, "unauthorized spectator");
            return null;
        }

        public Violation? CosmeticIDValidator(string cosmeticId, bool ownedOnPlayFab)
        {
            if (!string.IsNullOrEmpty(cosmeticId) && !ownedOnPlayFab)
                return new Violation(ViolationType.LocalStateDivergence, 0.75f, "unowned cosmetic " + cosmeticId);
            return null;
        }

        public Violation? InactivityKickTimer(string session, bool moved, float maxIdle = 180f)
        {
            float now = Time.unscaledTime;
            if (moved || !lastActivity.ContainsKey(session))
            {
                lastActivity[session] = now;
                return null;
            }
            if (now - lastActivity[session] > maxIdle)
                return new Violation(ViolationType.SampleGap, 0.3f, "idle timeout");
            return null;
        }

        public Violation? IntegrityHeartbeatMonitor(float maxGap = 8f)
        {
            float now = Time.unscaledTime;
            if (now - lastHeartbeat > maxGap)
                return new Violation(ViolationType.ClockDrift, 0.6f, "heartbeat stall");
            lastHeartbeat = now;
            return null;
        }

        public void PulseHeartbeat() => lastHeartbeat = Time.unscaledTime;

        public Violation? SyncVarTamperDetector(string expectedStructHash, string actualStructHash)
        {
            if (!string.Equals(expectedStructHash, actualStructHash, StringComparison.Ordinal))
                return new Violation(ViolationType.LocalStateDivergence, 0.8f, "syncvar structure mismatch");
            return null;
        }

        public Violation? HandRaycastValidator(Vector3 hand, Vector3 clickPoint, float maxReach = 0.45f)
        {
            if (Vector3.Distance(hand, clickPoint) > maxReach)
                return new Violation(ViolationType.HeightDelta, 0.55f, "hand click reach");
            return null;
        }

        public Violation? SceneLoadValidator(string scene)
        {
            if (string.IsNullOrEmpty(scene))
                return new Violation(ViolationType.OutOfBounds, 0.7f, "empty scene load");
            if (allowedScenes.Count > 0 && !allowedScenes.Contains(scene))
                return new Violation(ViolationType.OutOfBounds, 0.8f, "restricted scene " + scene);
            return null;
        }

        public Violation? MenuKeybindDisabler(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.Insert:
                case KeyCode.Delete:
                case KeyCode.F1:
                case KeyCode.F2:
                case KeyCode.F8:
                case KeyCode.RightShift:
                    return new Violation(ViolationType.UnexpectedAssembly, 0.35f, "blocked menu key");
                default:
                    return null;
            }
        }

        public void RegisterAllowedPrefab(string name)
        {
            if (!string.IsNullOrEmpty(name)) allowedPrefabs.Add(name);
        }

        public void RegisterAllowedScene(string name)
        {
            if (!string.IsNullOrEmpty(name)) allowedScenes.Add(name);
        }

        public void RegisterOwnedCosmetic(string id)
        {
            if (!string.IsNullOrEmpty(id)) allowedCosmetics.Add(id);
        }

        static float NormalizeAngle(float a)
        {
            if (a > 180f) a -= 360f;
            return a;
        }

        static string NormalizeSpeech(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s.ToLowerInvariant())
            {
                if (char.IsLetter(ch)) sb.Append(ch);
                else if (char.IsWhiteSpace(ch)) sb.Append(' ');
            }
            return sb.ToString();
        }
    }
}
