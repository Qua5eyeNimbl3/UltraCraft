# Test status

Confirmed:
- JSON preflight: 183 specification cells complete; no unresolved references.
- Java 26.3 bridge: compiles.
- ULTRAKILL host: compiles with zero errors or warnings.
- 25 Java tests pass: 21 inherited ray/collision tests and four new fluid-volume tests. They do not verify all gameplay input.
- BepInEx loads the native host plugin. Protocol layout and ring wrap-around self-tests pass in the host.
- Actual Java companion launches with an existing authenticated local launcher account.
- Both live game processes link through the generated mapping; heartbeats and frame sequences advance.
- Fresh Java UltraCraft world created, saved after host exit, and reopened on the next launch.
- 1,192 terrain mesh sections and 2,902 collision section records present after native sandbox load.
- The native player is placed on the real Java spawn and both halves report matching feet positions.
- Native gun terminal found and moved near spawn; native weapon objects loaded.
- These checks were completed without computer control or injected operating-system input.

Still to prove in the running game:
- [x] Both games remain linked after startup.
- [ ] Fresh Java terrain looks correct and collides correctly during hands-on movement.
- [ ] Native walking, jumping, dashing, sliding, and wall jumping work on it.
- [ ] Mining removes a real Java block and collects its drop.
- [ ] Placing a block updates both the picture and collision.
- [ ] Java inventory and crafting-table screens accept input; real recipes produce their outputs.
- [ ] Native gun terminal lets the player choose guns.
- [ ] Optional arena spawns native enemies; shooting damages them.
- [ ] World edits and inventory survive save/reload.
- [ ] Finished package passes inspection, recipe validation, and one-click file checks.
- [ ] Actual Melty install starts the verified build.
- [ ] Real gameplay media captured from this version.

Nothing has been uploaded or published.

## First user test and fixes

The user confirmed mining and placement function, but reported invisible opaque blocks/mob bodies, inconsistent mouse look, slow mining, inaccessible commands, no water movement, and hard-to-find combat markers. Those observations supersede the earlier background-only readiness estimate.

Revision checked on 2026-10-05:
- GPU tests pass for both solid and cutout materials, using real native shaders. A rear transparent object is correctly hidden by the nearer surface. See `test-media/fix-2/runtime-check.json`.
- Settled bridge state reports host flags 9 and guest flags 5: both halves recognize the player as grounded. See `test-media/fix-2/settled-link.json`.
- Native mouse capture is locked and its camera is active in settled runtime logs; focus restoration added.
- Java movement packet re-checks are bypassed only while this solo native bridge is active. Movement rejection spam absent at rest; fast movement still needs a manual retest.
- Native Water components created from actual merged Java fluid volumes. Twenty-five nearby sections are active in this saved location; swimming behavior needs a manual retest.
- T/slash routes added for vanilla chat/commands. Native pause is suppressed while a Java screen is open. Needs a manual retest.
- Beacons positioned above terrain; direction/distance label plus player-triggered J travel and H return. Native trial combat needs a manual retest.
- The original sandbox skybox is removed in favor of a plain blue background. Minecraft's full animated sky/weather rendering is not integrated.

No operating-system input injection or Computer Use was used for these fixes, tests, or game-engine image captures.

## Second user test and corrective build

The user reported strong grass wobble during camera movement, absent shading/shadows, and working mouse look with no movement. This supersedes any earlier estimate of movement readiness.

- A native audit found `NewMovement.activated=false`, while the player was alive, enabled, dynamic, and free of an input lock. Adoption now restores alive native movement once. Java screens store and restore the movement/body/camera state they suspend, preserving native pause/death behavior.
- Replaced native PSX unlit shaders with the own `UltraCraft/World` shader, compiled for the game's exact Unity 2022.3.29f1 runtime. It uses ordinary perspective transforms, depth writing, alpha cutout, vertex colours, the Java lightmap and a shadow-caster pass.
- The inherited exporter had removed Java directional face shading, and the host had ignored packed block/sky light. Face shading/AO are preserved; Java's actual 16x16 lightmap is captured asynchronously and sampled by block and captured-model meshes.
- A bridge-owned directional light casts shadows. Shadow quality changes exist only during this plugin session and restore on shutdown.
- Final real-engine report `test-media/fix-3/runtime-check.json`: opaque/cutout depth, cutout holes, vertex tint, lightmap contrast, smooth projection, cast shadows, movement readiness and atomic Java-screen suspend/resume all pass. The lightmap dark/bright samples are 0.094/1.000; enabling a caster darkens 203 pixels in the controlled comparison.
- Settled state is grounded in both games, with 1,314 mesh sections, 2,857 collision-section records and 26 nearby water sections. Both current builds and all 25 Java tests pass.
- `world-lighting.png` is a separate engine-camera view of the real saved Java world; it visibly shows shaded faces and a tree shadow. It is evidence of rendering, not a hands-on movement test.
- `live-window.png` is a capture of the actual running window after restoring windowed launch arguments. Terrain and native guns are visible. Passive logs show the player moving across the world with active/enabled movement and no input lock; no input was injected by the agent.
- The earlier `fix-3-initial` report records the failed lightmap test before the missing texture-header padding word was fixed. Failed reports are retained and are not final-build evidence.

Hands-on walking, jumping, camera feel and inventory reopening still require the user's retest. No computer control or injected input was used.

## Third user test and corrective build (2026-10-05)

User report: (1) only a few chunks load, no further generation; (2) sandbox map always present with an obnoxious outline; (3) grass still flickers; (4) replace the trial area with a wave spawn button; (5) Minecraft mobs do not spawn, and command/egg mobs disappear; (6) mining neither breaks nor drops blocks and has no crack texture; (7) native enemies fall through Java ground.

Causes found:
- 1, 5 and 6: native-authoritative movement cancels the server's movement packets, and the bridge moved the integrated server player with `setPos` only. `ServerChunkCache.move` (verified with javap: only called from the packet handler and teleports) never ran, so chunk loading, chunk sending and entity tracking stayed centred on the login position. It is now called every tick.
- 5: world rules forced `doMobSpawning=false` every start and the world was peaceful, which deletes hostile mobs instantly. Spawning rules and easy difficulty are applied once per world (marker `ultracraft-world.properties`); later `/difficulty` or `/gamerule` changes are kept.
- 6: the host never drew Java's destroy-stage entries; cracks are now rendered.
- 7: terrain collision used the first bit of `LMD.Environment`, layer 6, which enemies ignore; it now uses layer 8 "Environment" plus the "Floor" tag that `GroundCheckEnemy` requires (decompiled `LayerMaskDefaults`, `GroundCheckEnemy`). Ground enemies also need a navmesh: one is now built from Java collision around the player while enemies exist, for every native agent type, with a one-block step.
- 3: the block atlas had no mipmaps (distant point-sampled grass aliases into flicker) and shadow cascades used close-fit projection. The atlas now has 5 mip levels; cascades use stable fit with a larger normal bias.
- 2: sandbox geometry and colliders (including OutOfBounds triggers) are disabled after adoption, and the scene navmesh is removed. The block outline is now Minecraft-style (black, 40% opaque) and only shown in build mode.
- 4: trial arena, beacons and travel removed; J spawns escalating waves around the player.
- Adoption also hung forever when the saved Java position was mid-air (seen on the first relaunch); it now lands on the ground below once that ground is stable.

Evidence: 187 design cells clean; host builds with zero warnings; Java builds with tests passing. Live passive logs after relaunch: adoption succeeded; sandbox hidden (1,845 renderers, 682 colliders); natural mobs present (Java requested zombie, skeleton, creeper, spider and enderman textures); terrain grew past 3,000 sections while the user moved; mining clicks on several log positions followed by Java advancement unlocks (16 to 30) and inventory crafting. `test-media/fix-4/live-1.png` (engine capture, no input) shows break cracks on a log, no sandbox map, and the wave HUD.
A refined hider (keeps GameController's pooled effects, hides the extra sandbox shops) is built and applies on the next restart. Wave combat, enemy ground collision and navmesh walking, and grass flicker still need the user's confirmation.

## Fourth user test and corrective build (2026-10-05)

User report: good at first (mining and crafting worked), then increasing lag, a shaking/breaking hand, and mined items not picked up; poor performance; wanted cross-game damage both ways; wave enemies did not run at the player and no new wave followed a cleared one; wanted night hostile spawns replaced by ULTRAKILL enemies with passive mobs unchanged.

Findings and changes:
- Lag growth: easy difficulty plus natural spawning filled nearby caves with Minecraft monsters, and every entity within 64 blocks is captured and re-meshed each frame. Mapped hostiles are now replaced by native enemies (NaturalSpawnerMixin after vanilla's own checks; leftover mapped monsters are removed). Host FPS was about 33 for the whole previous session.
- Performance: Java HUD/hand overlay rendered and uploaded at half resolution (was a 2560x1440 RGBA upload per frame) with GUI scale 2; terrain merged into one renderer per chunk column (was one per section, about 3,000 objects); per-triangle transparency classification cached (the entity model is rebuilt every frame); unchanged collision is no longer rebuilt on light-only section updates; FindObjectOfType per frame replaced by the game's singletons; shadows use 2 cascades over 48 blocks.
- Hand shake: Minecraft eases hand sway toward the view once per tick, but the native camera turns every frame; the gap jittered. Sway state now follows the view exactly while linked.
- Cross-game damage: Minecraft mobs get trigger hitboxes on layer 10 tagged Body/Head with an EnemyIdentifierIdentifier pointing at an inactive EnemyIdentifier; a Harmony prefix on EnemyIdentifier.DeliverDamage forwards those hits as Minecraft player-attack damage (10 Minecraft damage per native damage unit, headshots x1.5). Native enemies within 48 blocks are published to the inherited actor table; Java's invisible stand-ins take Minecraft melee/arrow hits and send EvHitActor back (0.1 native per Minecraft damage).
- Waves: J starts waves; each cleared wave is followed by the next after 6 s. Per-enemy diagnostics (agent on navmesh, path, target) are logged every 5 s.

Evidence (live, passive; one in-process self-test, no OS input): host FPS 100-166 during play versus about 33 before; server tick 2.5-4.6 ms; natural zombie spawn replaced by a native Filth, mirrored to Java as a hittable stand-in; night enemies report agent=path/onmesh and target=True; `test-hits` delivered 1 native damage to Minecraft mob 1384 through the DeliverDamage prefix and a 7-damage Minecraft hit killed a Filth (health 0.50 to -0.20); Java status shows the server player tracking the native position and free inventory slots changing as items are picked up.
Pending: a Java build that removes leftover mapped monsters and logs the Java-side health change for native-weapon hits is built but not yet installed (user was playing). Real weapon hits on Minecraft mobs, sword hits on native enemies, wave auto-advance and the hand fix still need the user's confirmation.

## Fifth user test and corrective build (2026-10-05)

User report: remove the wave key (every night should be waves); the revolver still could not hurt a cow; make nights darker with a blood sky; dynamic seeds with a new one each reset.

- Revolver miss: RevolverBeam's main hit test is a non-All SphereCast (global trigger setting) that reads hit.transform, which is the attached rigidbody's object. The mob hitboxes were triggers under a parent rigidbody, so shots passed through; the earlier test called DeliverDamage directly and skipped this. Hitboxes are now solid colliders, each with its own kinematic rigidbody, ignoring collision with the player.
- Waves: wave key removed (controls sheet). Java sends the overworld clock each second (EV_TIME); night (13000-23000) starts wave 1, each clear brings the next, none start after dawn; surface natural replacements pause while waves run.
- Blood night: sun/moon follow the clock; sky fades day blue, sunset orange, near-black blood red; dim red moon light; red tint over Java materials; fog through global shader values (own shader rebuilt with Unity 2022.3.29f1; keyword fog was stripped to 4 variants in the minimal build project, so fog is computed from _UltraCraftFog* globals) plus Unity fog for native objects. Noon is now set only for a new world.
- Seeds: new worlds use a random seed (logged). Desktop `Reset UltraCraft World.exe` refuses while the games run, asks for confirmation, zips the save to the backups folder, then removes it.

Evidence: 220 design cells clean; host zero warnings; Java build and tests pass; shader bundle rebuilt and reloaded by its builder. In-process `test-hits` after the fix: the revolver-identical SphereCast hit the mob's Head collider (layer 10, identifier found) and Java logged a bat at 6.0 -> 0.0 health. Host-only lighting previews (no change to the Java world) in `test-media/fix-5/`: day.png, sunset.png, night.png (first tint), night-2.png (final darker tint). The in-game night will be darker than the preview because Java's own lightmap also dims at real night. Live wave nights, an actual pistol shot on a cow and the reset tool still need the user's test; the reset tool was built but not run (it removes the user's world).

## Sixth user test and corrective build (2026-10-05)

User report: grass, flowers and leaf litter still flicker; enemies sometimes stop moving; wanted weapons to mine (railcannon explosive circle, chainsaw cuts wood fast).

- Plant flicker: Minecraft sends cross/flat plant models as back-to-back quad pairs and its terrain always culls backfaces. The bridge drew the terrain cutout layer two-sided, so each pair z-fought. Terrain cutout is now culled (entity models and item sprites stay two-sided).
- Stuck enemies: a watchdog (every second) re-seats native enemies that are off the navmesh for 4 s, stranded over 20 blocks away without moving for 4 s, or fallen 40 blocks below the player: onto the nearest navmesh point, else a navmesh point near the player for their agent type. This also lets a night's wave be cleared when one enemy is lost.
- Weapon mining (sheet mining.json): Harmony hooks on RevolverBeam.HitSomething (revolver chips 1.0 hardness per shot; railcannon blast radius 3), Projectile.Collided (player pellets 0.3), Nail.OnCollisionEnter (nails 0.2; sawblades saw), Explosion.Start (player explosions, radius 0.25 x maxSize up to 3 blocks), Chainsaw.FixedUpdate (thrown saw, 0.6 per 0.1 s, x6 on axe-mineable blocks, x0.4 otherwise). Only the bridge's own block colliders count. Java WeaponMining accumulates damage against real hardness with crack progress, breaks with Block.dropResources and the matching diamond tool, blasts skip unbreakable and hardness > 49 blocks with a 35% drop chance, and partial damage heals after 8 s.

Evidence: 239 design cells clean; host zero warnings; Java build and tests pass. Live: all seven Harmony hooks reported applied (OptionsManager.Pause, EnemyIdentifier.DeliverDamage, RevolverBeam.HitSomething, Projectile.Collided, Nail.OnCollisionEnter, Explosion.Start, Chainsaw.FixedUpdate); adoption and play at 93 fps with no errors. Block breaking by weapons was not exercised by the agent (it would alter the user's world); it needs the user's test, as do the flicker and watchdog changes.
