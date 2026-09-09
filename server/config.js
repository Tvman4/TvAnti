// TvAnti authority policy. Edit these values; restart the process.
module.exports = {
  listenPort: process.env.TVANTI_PORT || 8787,
  // Shared secret the Unity client must send as header X-TvAnti-Key
  ingestKey: process.env.TVANTI_KEY || "CHANGE_ME_INGEST_KEY",

  // Slur mute length. Change this number — do not hardcode elsewhere.
  slurMuteHours: 5,

  maxHorizontalSpeed: 8.5,
  maxVerticalSpeed: 10,
  maxTeleportDistance: 3.25,
  maxArmMeters: 1.15,
  maxRotationDegPerSec: 1080,
  tagCooldownSec: 0.8,
  rpcMinIntervalSec: 0.05,
  maxRoomPlayers: 10,
  heartbeatMaxGapSec: 12,

  slurWords: [
    "nigger", "nigga", "fag", "faggot", "slut", "retard", "kike", "tranny"
  ],

  // Violation types that are a permanent ban (not a mute).
  cheatBanTypes: [
    "Speed", "Acceleration", "Teleport", "VerticalVelocity", "HeightDelta",
    "RotationRate", "ImpossibleVelocity", "OutOfBounds", "BlockedLibrary",
    "LibraryHashMatch", "UnexpectedNativeModule", "UnexpectedAssembly",
    "RemoteStateDivergence", "DuplicateSequence", "ClockDrift",
    "ArmLength", "Flight", "NoClip", "Ownership", "Instantiate",
    "BuildHash", "DeviceBan", "ModeratorForge", "CosmeticUnlock",
    "TimeScale", "SyncVarTamper", "Spectator", "RoomCapacity"
  ],

  dataFile: __dirname + "/data/authority-state.json"
};
