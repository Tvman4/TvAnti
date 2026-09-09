# TvAnti 1.1

Defensive Unity VR anti-cheat framework for Gorilla Tag-style fangames.

## Detection families
1. Exact blocked `.so` basename matching
2. Blocked `.so` SHA-256 matching
3. Native-library inventory telemetry
4. Managed assembly inventory telemetry
5. Horizontal speed anomaly
6. Vertical velocity anomaly
7. Acceleration anomaly
8. Teleport-distance anomaly
9. Head-height delta anomaly
10. Rotation-rate anomaly
11. Sampling-gap anomaly
12. Non-finite/invalid transform detection
13. Duplicate/replayed sequence detection
14. Server-authoritative movement divergence
15. Packet-rate/burst anomaly
16. Clock-drift anomaly
17. Impossible velocity anomaly
18. Out-of-bounds state detection
19. Remote/local state divergence
20. Correlated suspicion scoring

The client is treated as untrusted. The package avoids blindly killing a process because a native library exists: legitimate Unity/VR/SDK libraries can be loaded dynamically. Exact library matches and server-side correlation are safer.

Unity variants are packaged by `.github/workflows/build.yml` as `TvAnti22.x.zip` and `TvAnti6.2.zip`.
