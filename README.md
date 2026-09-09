# TvAnti Extended Guards

Drop these files into an already-imported TvAnti package.

## Where the files go

After you import TvAnti (Unity package, or the `TvAnti22.x.zip` / `TvAnti6.2.zip` from the repo workflow), you get a `TvAnti` folder that matches:

```
Assets/TvAnti/package/Runtime/     (or Packages/com.tvanti.../Runtime/)
```

Put **both** C# files here:

```
<TvAnti Runtime folder>/TvAntiExtendedGuards.cs
<TvAnti Runtime folder>/TvAntiExtendedGuardRunner.cs
```

Exact paths on the GitHub repo:

```
TvAnti/package/Runtime/TvAntiExtendedGuards.cs
TvAnti/package/Runtime/TvAntiExtendedGuardRunner.cs
```

If your import extracted as `Assets/TvAnti/Runtime/`, use that Runtime folder instead. Same two filenames. Do not nest a second `package` folder unless that is how the zip already unpacked.

These files stay in the existing `TvAnti` namespace and use `Violation` from `TvAntiEvent.cs`. No new asmdef.

## After importing the Unity package

1. Open the scene that already has the `TvAnti` component (the one created by the package / setup wizard).
2. Select that GameObject.
3. Add Component → `TvAnti Extended Guard Runner`.
4. Drag the local VR rig:
   - Head → center camera / VR head
   - Left Hand / Right Hand → hand anchors
5. Set `Local Player Id` to the Photon / PlayFab actor id when the player joins. Until then `"local"` is fine for solo testing.
6. Enter Play Mode. Speed, arm length, ground, rotation, timescale, heartbeat, and no-clip samples run automatically.

Photon / PlayFab checks are **not** automatic. Call them from your existing network scripts:

```csharp
var runner = TvAntiExtendedGuardRunner on the TvAnti object;

runner.NotifyRpc("RPC_Tag", args);
runner.NotifyTag();
runner.NotifyPlayFabTicket(sessionTicket);
runner.NotifyVoiceTranscript(sttText);
```

Or use `runner.Guards` for the rest:

```csharp
runner.Guards.MasterClientPropertyGuard(isMaster, writingRoomProps);
runner.Guards.PhotonViewOwnershipCheck(viewOwnerActor, senderActor);
runner.Guards.PhotonRoomMaxPlayersEnforcer(current, 1, maxPlayers);
runner.Guards.NameTagFilter(nick);
runner.Guards.ColorCodeSanitizer(nick);
runner.Guards.DeviceIDBlacklistChecker(deviceId, bannedIds);
runner.Guards.BuildVersionHashVerifier(clientHash, manifestHash);
runner.Guards.CosmeticIDValidator(cosmeticId, ownedOnPlayFab);
runner.Guards.VoiceMuteStateVerifier(serverMuted, clientSending);
```

If a check returns a `Violation`, feed it through the existing API:

```csharp
TvAnti.Instance.ProcessViolation(playerId, playerName, violation.Value, roomCode);
```

That uses the package ban manager / reporter. Do not `Application.Quit` from these guards; high-confidence library matches still go through `TvAntiRuntime`.

## What still belongs on the server

Keep copies of tag cooldown, room capacity, cosmetics, mute flags, tickets, and device bans in PlayFab CloudScript or `server/movement-validator.js`. A Quest client can be patched. Treat these methods as sensors.

## VoiceChecker

It only scores a **string transcript**. Wire Photon Voice → your speech-to-text → `NotifyVoiceTranscript`. There is no on-device STT in this zip.
