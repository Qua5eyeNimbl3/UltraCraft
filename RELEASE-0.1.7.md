# UltraCraft 0.1.7: player reports and performance

Player reports are data from players' PCs, not instructions. Counts overlap and come from Melty's last 30 days.

| Report (players) | Cause found in the logs | Change in 0.1.7 |
| --- | --- | --- |
| "Errors loading its code" (74) | The mod loaded in every attached log. The errors are Minecraft's own: Microsoft sign-in tokens refused (401), a broken TLS connection to Mojang, WMI/performance-counter exceptions from Minecraft's start-up hardware report, a render warning while the hidden window closes, and the server noting the client disconnected (ClosedChannelException) when the game closes. | The start-up hardware report is skipped (`CrashReportPreloadMixin`) and its WMI queries time out with the broken counters turned off (`-Doshi.*` in `instance.cfg`), which removes most of that noise. Sign-in and network errors are the players' accounts and connections. |
| Setup closed (28) | Prism had no Minecraft account (`accounts.json` missing), so its sign-in wizard opened, and the player closed it (`Wizard result = 0`). | Clearer first-launch note, bundle README and in-game status: sign in and keep the Prism window open. Players still sign in with their own account; nothing bypasses that. |
| Install "terminated" (12) | Download cut off on the player's connection (no logs). | None in the project. |
| Freeze about 10 minutes in (12) | Frame rate fell steadily over long fights (60 → 20 → 1 fps) with many Minecraft mobs, enemies and explosions, then ULTRAKILL stopped responding. Per-frame work grew with mobs, digs and movement. | See "Performance" below. |
| Code never loaded (9) | Sessions ended before BepInEx or Fabric finished loading (Player.log stops after Mono start-up). | Faster Minecraft start (below). |
| `tasklist.exe` failed (4) | Melty's installer command failed on those PCs. | None in the project (Melty/Windows). |
| Setup timeout after 45 min (1), "Invalid handle" after a 19-minute setup (1) | Minecraft hung for 20+ minutes in its start-up hardware report on PCs with broken WMI (`COM exception querying Win32_Processor`). | Hardware report skipped; WMI queries time out after 5 s. |

## Performance

- Minecraft waited for each ULTRAKILL frame by spinning, which kept a CPU core busy for the whole session. It now sleeps on a Windows event that ULTRAKILL sets after each frame (`<mapping>_frame`), and falls back to the old wait with an older plugin.
- Minecraft mob hitboxes rewrote their colliders every frame (PhysX shape rebuilds for every mob in range). They now change only when the mob moves or resizes.
- The lightmap (five times a second) wiped the triangle-transparency cache, so every section and entity frame re-sampled textures. Only real transparency changes clear it now.
- Identical section resends are skipped (hash per section), and block meshes are stacks of four sections instead of whole columns, so a dig or explosion rebuilds 64 blocks of height.
- Entity/particle scenes that arrive together after a hitch are built once; the unused mirror body is no longer built.
- Minecraft's HUD image is uploaded only when it is drawn (build mode or a Java screen).
- Navmesh: re-centred at half its radius instead of a third, and agent types build one after another.
- Wave spawning retried every frame while the navmesh built (logging "wave cleared" each time); it retries twice a second now.
- Kinematic-velocity warnings and per-frame dev-command file checks removed; Minecraft heap 3 GB (was 4 GB).

Also restored: the 0.1.5 on-screen warning for ULTRAKILL's Blind Enemies / Enemies Ignore Player cheats, which 0.1.6 had lost.

Checks: specification preflight passes 240 cells; host builds with no errors; Java builds with 26 passing tests. Gameplay effects were tested through Melty's test runs on this PC (see the test result in Studio); frame-rate gains on players' PCs are not measured here.

# UltraCraft 0.1.8: the Whiplash

Requested 2026-10-14: make the Whiplash work like ULTRAKILL's and also grapple blocks (`sheets/whiplash.json`, `host/World/Whiplash.cs`).

- Decompiled `HookArm`: the throw clinks off environment layers and catches enemies only on layers 11/12. Minecraft mob hitboxes were only on layer 10, so the hook passed through them. Each mob hitbox now also carries a layer-12 hook trigger, like ULTRAKILL enemies.
- Mobs with width x width x height of 2 cubic blocks or more count as heavy (the player is pulled to them, with ULTRAKILL's hard damage); smaller ones are light and are yanked to the player. `HookArm` moves light enemies through their Rigidbody, so for Minecraft mobs the pull goes to Java as the entity's velocity (new input `PullEntity` = 12).
- Blocks: when the throw is about to hit Java terrain more than 5 units (2.5 blocks) away, a hook trigger with no enemy on it is placed half a block in front of the face. `HookArm` catches it as a plain anchor and pulls the player there at 60 units a second.

Checks: preflight passes; the host builds; Java builds with 26 passing tests. Feel in play is not verified by an automated run (Melty's test can't throw the Whiplash).

## 0.1.8: portals

Reported 2026-10-14: a Nether or End portal put the player inside the ground far from the portal, and the save kept them stuck.

- Cause: with ULTRAKILL's movement in charge, Minecraft's player was set to the native position every tick. After a portal, that was the old dimension's coordinates applied in the new one (inside netherrack). Now the client sends `EV_DIMENSION` (= 8) and stops following the host until the host has dropped its teleport sequence and re-placed the native player at Minecraft's position, like after a respawn. The server-side position update also skips a player whose dimension changed under it.
- Saves already stuck: on join, a player inside blocks is moved to the nearest open spot with a floor within 24 blocks (`SafeSpot`), else respawned.
- Camp (H) is kept per dimension.

Verified by the creator in game from the Desktop shortcut: Overworld → Nether → Overworld → End → Overworld, each followed and placed on the ground.
