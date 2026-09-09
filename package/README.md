# TvAnti

Defensive VR anti-cheat framework for Unity fangames.

## Threat catalog

TvAnti can recognize exact assembly/file identities supplied by the game owner, including:

- flow.lol
- flow.lol22
- SPM
- FlyingCatsStupidMenuV4
- AntiCheatBypass2Ma
- AntiCheatBypass2022.2and2023.3
- NullHoldableV2
- RMH
- BebiteCheats
- cool-lib
- TvMenu

The scanner intentionally uses exact identity matches for automatic hard-stop decisions. Generic substring matching is not used for bans because that creates false positives.

## Better detection

Use multiple independent signals:

1. Exact assembly/file identity.
2. Server-authoritative movement validation.
3. Impossible velocity/acceleration/teleport checks.
4. Suspicion score with decay.
5. Server-side session restrictions.
6. Telemetry before enforcement.

A client scanner is only one layer. For PlayFab, keep authoritative decisions on the server; PlayFab CloudScript is designed for server-side functionality that clients cannot directly modify.

Unity's RuntimeInitializeOnLoadMethod can start integrity checks during runtime initialization, while loaded assemblies can be inspected at runtime.
