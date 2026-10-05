# UltraCraft development journal

Status: feasibility and first playable integration; no release or listing exists.

## Agreed experience
- Solo; Minecraft **Java Edition only**, never Bedrock.
- Fresh Minecraft world. Peaceful exploration, mining, building, crafting.
- ULTRAKILL movement, guns, combat, and the gun-selection terminal.
- Combat areas entered voluntarily. Save/load world and inventory.
- Full Minecraft parity is a longer goal; first prove this core loop in the real games.
- User approved beginning this plan on 2026-10-05.

## Publishing constraints
- ULTRAKILL is the intended host. Melty auto-installs BepInEx 5 v5.4.23.5.
- Melty does not auto-install any Java mod loader. No manually installed dependency can be required.
- Preserve actual Java world logic; establish permitted automatic setup before expanding.
- Use universal-modder's mod-any-game workflow; use Melty for publishing.
- JSON design sheets precede generated code; every build requires clean preflight.
- Do not ship game assemblies/assets, authentication data, or the Melty credential.
- License/remix/title choices are still pending. Publishing needs user test and explicit authorization.

## Local sources and games
- ULTRAKILL: C:/Program Files (x86)/Steam/steamapps/common/ULTRAKILL; Unity Mono, Assembly-CSharp.dll present.
- Java: C:/Users/homer/AppData/Roaming/.minecraft; installed versions and saves found.
- Provided Microsoft.MinecraftUWP path is Bedrock and is excluded.
- Toolkit clone: work/universal-modder (official repository).
- Reference clone: work/PeakCraft (MIT; by aeironnsarmiento; SkyCraft origin by chasmlol).
- PeakCraft's live Melty recipe bundles a Java companion and launcher, with setup and together launches.
- PeakCraft source uses a Fabric bridge; this conflicts with the initial unsupported-loader constraint unless Melty accepts an entirely bundled automatic companion. Investigate before using it.

## Next
Inspect installed version, toolkit knowledge, bridge source and the precise Melty automatic-setup rules.

## First build and lab, 2026-10-05
- Toolkit installed in its local virtual environment. Java 25.0.4.1, BepInEx 5.4.23.5, portable Prism 11.1.1 fetched from official releases. JDK SHA-256 verified.
- Installed ULTRAKILL: Unity 2022.3.29f1, no anti-cheat detected. Scanner evidence agrees with UnityPlayer version.
- Java 26.3 already installed. Companion bridge uses this exact version.
- Design-only Melty one_click_check accepted a BepInEx host plus automatically prepared companion. Actual package bytes still need checking.
- 139 specification cells verified, descriptors generated. Both halves compile; 21 inherited bridge tests pass.
- Save backup: C:/Users/homer/.universal-modder/backups/ultracraft-ultrakill-saves/20261005-115215.zip.
- Profile backup: C:/Users/homer/.universal-modder/backups/ultracraft-ultrakill-profile/20261005-115215.zip.
- Local isolated Java instance: C:/Users/homer/AppData/Roaming/PrismLauncher/instances/UltraCraft-Lab.
- BepInEx and prototype host installed in the game folder for testing.
- First run: native plugin loads, protocol/ring self-tests pass; Java launches. Runtime link did not persist. Host manager appears to be removed during bootstrap. Prism's INI parsing also removed the backslash in the mapping argument.
- Fix under test: BepInEx HideManagerGameObject=true; explicit hide/persist on host object; guest mapping defaults to generated sheet constant, with no INI mapping argument.
- Removed inherited Discord status calls; solo requires no Discord or relay. Removed e4mc runtime dependency.
- Existing unrelated Minecraft Forge 1.20.1 game was already running and has been left untouched. Only this task's Java process and ULTRAKILL process are controlled.

## Background integration checks, 2026-10-05
- User forbids computer control. No further Computer Use calls or operating-system input injection are used.
- Removed the legacy mapping-property override from Java: Prism had cached an incorrectly escaped argument. The generated mapping is now authoritative on both halves.
- Scene load waits for `SceneHelper.PendingScene == null`, avoiding the native coroutine's early exit while the main menu loads.
- Collision objects now share a persistent root. They previously disappeared on native sandbox scene transition while render meshes survived.
- Bridge runs in the background. Native player adoption waits for the sandbox to finish loading.
- Actual live read-only probes at 12:31:07 and 12:32:00 CDT show both heartbeats under 20ms, advancing frame sequences, in-world Java state, teleport sequence 1, and matching feet positions near (-233.5, 72, -464.5).
- Native log confirms 1,192 mesh sections, 2,902 collision section records, player adoption, native Shop terminal moved near spawn, and loaded native gun objects. This does not establish visual correctness or terminal usability.
- Java automatically saved and exited after the previous host process ended; the next launch opened the same UltraCraft world. Edited-block/inventory persistence is still pending.
- Host process 49904 and this task's Java process 38108 are left running for manual testing. The existing unrelated Forge process remains untouched.
- `PLAY-TEST.md` records the first manual acceptance checks. Publication, licensing/remix decisions, package checks and Melty testing remain pending.

## First user play test and correction, 2026-10-05
- User provided two screenshots and a Prism log. Opaque terrain and mob bodies were missing; grass/water/bee wings appeared. Mining/placement worked, but mining was slow, mouse capture unreliable, commands inaccessible, water physics absent, and beacons hard to find.
- Sprite shader's fixed ZWrite Off plus opaque render queue let native skybox paint over solid geometry. Replaced with real native opaque, alpha-test and translucent shaders; refreshed shader lookup after scene assets finish loading. Flipped captured entity winding consistently with block meshes.
- Protocol version 12 adds native grounded flag without changing shared-memory layout. Java client/server ground state follows native ground checks. Solo server ignores vanilla movement packets while native bridge is authoritative, avoiding repeated moved-wrongly re-checks.
- Added water message 16 with merged Java fluid bounds, native layer-4 Water triggers, deferred creation after native sandbox load, and nearby-only processing radius 32 blocks. Four genuine merge/volume tests pass.
- Mouse locks on adoption and focus regain; camera activated when closing Java screens. T/slash route to commands/chat. Harmony guard prevents native Pause from consuming Escape while Java screens are open.
- Native sandbox skybox removed; plain blue background configured. Full Java sky/weather presentation remains pending.
- Terrain-height beacon placement, trial direction/distance HUD, J trial travel and H camp travel. These are player-triggered controls; no automated keyboard/mouse input was used.
- Both builds pass; 178 design cells clean; 25 Java tests pass. Real GPU depth tests pass with psx/unlit/unlit and psx/unlit/alphatest/nocull. Settled host flags 9 / guest flags 5 prove grounded-state transfer.
- Current test host PID 49380, Java companion PID 45328. Fresh-world edits and inventory from the user's previous play session are preserved. Other Minecraft processes remain untouched.
- `test-media/fix-2` contains real engine-captured diagnostics and frames. The initial frame/report is taken on adoption; settled frame/probe records the later stable state. Manual retests of movement, mining speed, mobs, commands, swimming and combat are still required.

## Second user test and correction, 2026-10-05
- User reported grass shaking with camera movement, no shading/shadows, and no movement despite mouse look working.
- Read-only native audit: movement enabled=True, activated=False, body dynamic, FreezeRotation only, alive, input lock=False, timescale=1. Restore alive movement on adoption; use JavaScreenGate snapshots for bridge-owned screen suspension/restoration.
- Retro native shaders use `_VertexWarpScale=1` and are unlit. Built an own shader with ordinary perspective transforms, Java vertex tint/AO, exact live Java lightmap, alpha-test depth and cast shadows. Native fallback warp scale is zero.
- Java exporter previously stripped per-face shading; host ignored packed sky/block light. Preserve original face colour, add UV2 light coordinates, and export the actual Java 16x16 RGBA lightmap asynchronously at 5 Hz.
- Unity 2022.3.29f1 compiler downloaded from official release metadata into `work/runtime/unity-editor-2022`; installer MD5 `150A7A9A212E7D06CF99BB4B9914A120` matches release integrity. Compiler is a local build dependency, not shipped. Existing Unity 6 editor was not used because the game's runtime is 2022.3.29f1.
- Initial shader bundle was invalid: the minimal project omitted the built-in assetbundle module, so it contained only a Shader object and no AssetBundle/container object. Adding `com.unity.modules.assetbundle:1.0.0` fixes it. Builder now loads its bundle back before reporting success.
- Initial Java lightmap message omitted the 4-byte padding word in RenTextureHdr. Real GPU contrast test caught the missing palette; fixed header is 16 bytes and final contrast passes.
- Final `test-media/fix-3/runtime-check.json`: depth solid/cutout, holes, tint, actual Java lightmap, smooth camera projection, shadows, movement readiness and Java-screen round trip all pass. Dark/bright light samples 0.094/1.000; 203 pixels darken when enabling the isolated caster.
- Separate engine-camera image shows real terrain, face shading and tree shadow. No keyboard/mouse events or native-player movement were injected. Game-window screenshot capture was black while launched hidden/fullscreen. Relaunching with the original windowed arguments restores capture; `test-media/fix-3/live-window.png` shows the actual live window with complete terrain, shading and native guns. The adoption-time window capture shows the old native spawn before the next camera update and is not the settled view.
- Added WorldLighting, own shader source/bundle, reproducible build helper, UV2 lighting for blocks/models and corresponding installer copy. 183 design cells clean; host has zero build warnings/errors; 25 Java tests pass.
- Saved world and user edits preserved throughout graceful restart cycles. Publication and full feature parity remain pending.
- Final interactive test host PID 30748 and isolated Java PID 39380 remain running. Passive native logs show substantial movement across the Java world, both airborne and grounded states, and the optional native trial starting. No controls were injected by the agent. Full movement feel, gun-machine use and damage remain manual acceptance items.

## Third user test and correction, 2026-10-05
- Reported: chunk loading stalls, sandbox map/outline visible, grass flicker, wanted a wave button instead of the trial, mobs absent/disappearing, mining doesn't break/drop/show cracks, enemies fall through ground.
- Root cause for chunks/mobs/mining: the integrated server player was moved with setPos only; ChunkMap tracking never re-centred because movement packets are cancelled. SkyClient now calls ServerChunkCache.move every tick.
- World: easy difficulty and natural spawning applied once per world via a marker file. View distance 8, simulation 6; host evicts terrain beyond 10 chunks.
- Host: collision on layer 8 + Floor tag; runtime navmesh follower; wave spawner (encounters sheet); sandbox hider; mipmapped atlas; stable-fit shadows; crack rendering; Minecraft-style outline in build mode only; robust mid-air adoption.
- Java save backed up before the change: C:/Users/homer/.universal-modder/backups/ultracraft-java-save/20261005-140533.zip. Installed by copying only the DLL and jar (setup_lab.py would overwrite options.txt).
- Relaunched for the user's retest (ULTRAKILL PID 43712). Passive logs and one engine capture confirm adoption, hidden sandbox, mobs, terrain growth, cracks, mining and crafting. Refined hider built but not yet installed (user was playing).

## Fourth user test and correction, 2026-10-05
- Reported: lag growing over time, shaking hand, items not picked up, bad performance; wanted cross-game damage, wave enemies that chase and auto-advancing waves, and night hostiles replaced by ULTRAKILL enemies.
- New sheets natural.json (spawn replacement map and caps) and combat.json (damage scales, actor range); render.json gains overlay_scale and shadow_cascades; encounters.json gains auto_next_delay_seconds. 212 cells clean.
- Java: NaturalSpawnerMixin + NativeSpawns (veto mapped natural spawns, EV_NATIVE_SPAWN to host, sweep leftovers), IN_HIT_ENTITY handler, mob hitbox export (height, hostile, eye height), hand-sway lock, GUI scale 2 for the half-resolution overlay, periodic server status line.
- Host: CrossCombat (mob hitboxes + DeliverDamage prefix, actor table publishing, EvHitActor), Waves auto-advance + natural spawns + diagnostics, chunk-column mesh merging, classification cache, collision change detection, singleton lookups, half-resolution overlay viewport and cursor.
- Installed host DLL and first Java jar; relaunched (ULTRAKILL PID 11028). Live: FPS 100-166, natural Filth replacement, both damage paths verified by the in-process test-hits command. Follow-up jar (leftover sweep, hit log) built, install on next restart.

## Fifth user test and correction, 2026-10-05
- Wave key removed; night-driven waves from the Java clock (EV_TIME 7). Natural replacements pause during night waves.
- Mob hitboxes: solid colliders with per-part kinematic rigidbodies (revolver SphereCast + hit.transform), player collision ignored. Verified with a revolver-identical cast and Java health log.
- Blood night: WorldLighting day/night cycle, sky/fog/tint/moon from render sheet; World.shader fog via globals; shader bundle rebuilt.
- Random seed for new worlds; Desktop `Reset UltraCraft World.exe` (backup zip, then remove, after confirmation). Launcher build script builds both programs.
- Installed DLL, shader bundle and jar; relaunched through the Desktop launcher. Previews in test-media/fix-5.

## Sixth user test and correction, 2026-10-05
- Terrain cutout culled (plant quad pairs z-fought when two-sided).
- Waves watchdog for off-navmesh, stranded and fallen enemies.
- Weapon mining: host World/WeaponMining.cs hooks + IN_BREAK_BLOCK (10) / IN_BLAST (11); Java world/WeaponMining.java. New sheet mining.json.
- Startup log lists applied Harmony hooks. Installed and relaunched through the Desktop launcher.
