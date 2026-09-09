# TvAnti extended guards

Copy `TvAntiExtendedGuards.cs` into `package/Runtime/` of https://github.com/Tvman4/TvAnti

Wire it next to the existing movement validator:

```csharp
private TvAntiExtendedGuards guards;

void Awake()
{
    guards = new TvAntiExtendedGuards(config);
}

void Update()
{
    var v = guards.TimeScaleIntegrityCheck();
    if (v.HasValue) Register(v.Value);

    v = guards.IntegrityHeartbeatMonitor();
    if (v.HasValue) Register(v.Value);
}
```

Photon / PlayFab hooks stay call-in, not auto-scanned:

- RPCValidationAndRateLimiter(session, rpcName, args)
- MasterClientPropertyGuard(photonView.IsMasterClient, writingRoomProps)
- PhotonViewOwnershipCheck(view.OwnerActorNr, senderActor)
- PlayFabAuthenticationHook(PlayFabClientAPI session ticket)
- DeviceIDBlacklistChecker(SystemInfo.deviceUniqueIdentifier, bannedSet)
- VoiceChecker(transcriptFromYourSttPipeline)

VoiceChecker does **not** implement speech-to-text. Feed it text from Photon Voice + your STT. The blocklist lives on your server copy of this logic; a client-only list is trivial to patch.

Authoritative copies of TagCooldown, room capacity, cosmetics, mute state, and device bans belong in PlayFab CloudScript / your Node validator (`server/movement-validator.js`), not only on the Quest client.
