# UltraCraft — handoff to the next AI

Updated 2026-10-05 (third user-test fixes: read the latest TEST-STATUS.md and MODLOG.md sections first). Continue this existing project; do not rebuild it from scratch. This is a working development integration undergoing user play tests, not a finished release.

## Project location

**Main project and source:**

`C:\Users\homer\Documents\Codex\2026-10-05\files-pasted-by-the-user-help-2\outputs\UltraCraft`

**Workspace / shell working directory:**

`C:\Users\homer\Documents\Codex\2026-10-05\files-pasted-by-the-user-help-2`

The project contains `host/` (C# ULTRAKILL plugin), `java/` (Fabric Java companion), `shaders/`, `sheets/`, `tools/`, `test-media/`, and `THIRD-PARTY/`. Runtime tools, reference source and scratch diagnostics live in the workspace's `work/` folder. Keep user-facing deliverables under `outputs/`.

Read this file, then `README.md`, `TEST-STATUS.md`, the latest entries in `MODLOG.md`, and `PLAY-TEST.md`. Older journal entries describe superseded builds; use the latest evidence below.

## User's intent and constraints

- Actual **Minecraft Java Edition only**, never Bedrock. A fresh solo Minecraft survival world with mining, building and crafting, plus ULTRAKILL movement, weapons and combat.
- Start peacefully, explore freely, then enter optional combat areas when ready. Include the actual native gun terminal so the player can choose guns.
- The initial fresh world already exists and the user has edited it. **Preserve that world and inventory; do not create another fresh world or reset the save.**
- Do not affect unrelated Minecraft instances or saves. Never kill every Java process. Identify this task's executable path and exact PID before stopping anything; avoid printing full process command lines, which can contain authentication data.
- No public upload or publication has occurred or is authorized by this handoff. Eventual Melty publication still needs package validation, the user's test, license/remix choices and explicit final authorization.

Full Minecraft feature parity is the longer goal. The user accepted a first playable integration milestone; do not represent the prototype or its tests as proof of all Minecraft features.

## Where the task stopped

The latest user bug report was:

1. Grass shakes badly when the camera moves.
2. No shading or shadows.
3. Mouse look works, but movement does not.

Those causes were fixed, both builds passed, the update was installed, and the games were restarted for the user's retest. The user then asked for this handoff. **There is no new user retest result yet.**

Previously the user confirmed mining and placement worked, but reported invisible solid blocks/mob bodies, unreliable mouse look, very slow mining, inaccessible commands, broken water movement and hard-to-find beacons. Earlier fixes addressed depth rendering, model winding, native grounded-state transfer, mouse capture, Java screen routing, real water triggers and beacon access. Those features still need complete hands-on acceptance.

The latest interactive session was left running: ULTRAKILL PID **30748**, companion Java PID **39380**. These are historical snapshot values: re-enumerate before acting. Do not restart unnecessarily while the user may be playing. Passive logs subsequently showed substantial player movement, grounded and airborne states, and an optional trial starting, without any input injected by the agent. That supports restored movement; it does not prove the feel of every movement mechanic, gun-terminal use or damage.

## What the latest fixes changed

- Native audit found `NewMovement.activated=false` despite an alive, enabled, dynamic player with no input lock. Adoption now restores movement once when appropriate. `JavaScreenGate` snapshots and restores movement, rigidbody and camera state around Java screens while respecting native pause/death.
- Native retro unlit shaders caused vertex snapping/wobble and lacked lighting. The own `UltraCraft/World` shader uses normal perspective transforms, solid depth, alpha cutout, vertex tint/AO, the real Java lightmap and a shadow-caster pass. Native fallback vertex-warp scale is zero.
- The inherited Java exporter stripped directional face shading. Original face colours now survive export. Packed block/sky light becomes UV2 coordinates, sampling Java's actual 16×16 lightmap captured asynchronously at 5 Hz.
- A bridge-owned directional light casts shadows. Changed shadow-quality settings restore when the plugin shuts down. Full Java sky, weather and changing sun presentation remain unfinished.
- A failed lightmap test revealed a missing 4-byte padding word: `RenTextureHdr` must be **16 bytes**, not 12. That was corrected and the real GPU contrast check passes.
- The shader bundle is built with the game's exact **Unity 2022.3.29f1 (8d510ca76d2b)** compiler. A minimal Unity project must include `com.unity.modules.assetbundle:1.0.0`; without it, the output lacks an AssetBundle/container object and produces a misleading compatibility error. The builder now reloads the bundle before reporting success.

## Verified evidence and its limits

Current specification preflight passes **183 cells**. The C# host builds with zero warnings/errors. The Java companion builds with **25 passing tests**: 21 inherited ray/collision tests and four fluid-volume tests. These do not exercise all gameplay input or feature parity.

`test-media/fix-3/runtime-check.json` records passing real-engine checks for solid/cutout depth, cutout holes, tint, Java lightmap contrast, smooth projection, shadows, movement readiness and an atomic Java-screen suspension/restoration round trip. Dark/bright light samples are approximately 0.094/1.000; enabling the isolated shadow caster darkens 203 pixels.

The settled probe reports both halves grounded, matching feet positions, 1,314 terrain sections, 2,857 collision-section records and 26 nearby water sections. Counts vary as the user explores. The world saved and reopened through graceful shutdown/relaunch cycles.

Evidence in `test-media/fix-3/`:

- `live-window.png`: actual final native-window capture, with complete terrain, shading and native guns visible.
- `world-lighting.png`: separate engine-camera view of real saved Java terrain, showing shaded faces and a tree shadow. Its sky differs from the player's view; it is a rendering diagnostic.
- `runtime-check.json` and `settled-link.json`: final checks and passive bridge state.
- `windowed-frame.png`: adoption-time view before the next camera update, showing the old native spawn; not the settled world view.
- `gameplay-frame.png` / `settled-frame.png`: earlier black captures from a hidden/fullscreen launch; not final visual evidence.
- `fix-3-initial/` retains the earlier failed lightmap test. Do not relabel failed reports as passing evidence.

Windowed native launch restored real window capture. No OS input was used. Subjective camera feel, walking/jumping and reopening inventory still require the user's retest.

## Runtime and save locations

| Item | Location / version |
| --- | --- |
| Native game | `C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL` |
| Host install | `C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\BepInEx\plugins\UltraCraft\UltraCraft.dll` |
| Own shader bundle | Same plugin folder, file `ultracraft-shaders` |
| Native loader | BepInEx 5.4.23.5; `HideManagerGameObject=true` |
| Isolated Prism instance | `C:\Users\homer\AppData\Roaming\PrismLauncher\instances\UltraCraft-Lab` |
| **Current Java save** | `C:\Users\homer\AppData\Roaming\PrismLauncher\instances\UltraCraft-Lab\.minecraft\saves\UltraCraft` |
| Installed companion | Instance `.minecraft\mods\ultracraft-java.jar` plus `fabric-api.jar` |
| Java versions | Minecraft Java 26.3, Fabric Loader 0.19.5, Fabric API 0.161.0+26.3 |
| Task JDK | Workspace `work\runtime\jdk-25.0.4.1+1` |
| Task Prism | Workspace `work\runtime\Prism\prismlauncher.exe` (11.1.1) |
| Shader compiler | Workspace `work\runtime\unity-editor-2022\Editor\Unity.exe` |
| Shader build project/log | Workspace `work\shader-project` / `work\shader-build.log` |
| Reference source | Workspace `work\PeakCraft` (MIT); do not ship its game/runtime caches |

The instance uses the existing authenticated local Prism account. Do not copy account files or credentials into the project/package. An unrelated Forge 1.20.1 instance was already running and has been left alone.

World name `UltraCraft`, original seed `742916305`, normal terrain, survival/peaceful, structures enabled. Current code sets noon at start, disables weather and enables keep-inventory. Easy difficulty and natural mob spawning are applied once per world (marker `ultracraft-world.properties` in the save). These prototype settings are a feature-parity limitation. Host scale is **2 Unity units per Java block**, with Z flipped.

Shared-memory mapping **`Local\UltraCraft_v1`**, protocol **12**, magic `SKYC`. Generated constants are authoritative. Do not add a Prism JVM mapping override: QSettings previously removed the backslash and broke the link. Typical settled flags are host **9** (in-game + grounded), guest **5** (in-world + grounded).

## Build, install and launch

JSON `sheets/*.json` are the source of truth. Change relevant specifications before implementation. Run clean preflight **before every build**. It generates `host/Generated/Design.cs` and `java/src/main/java/dev/skycraft/Design.java`. Passing preflight means specifications/references are complete, not that gameplay passed.

From the workspace, build host:

```powershell
python outputs\UltraCraft\tools\preflight.py --generate
if ($LASTEXITCODE -ne 0) { throw 'Preflight failed' }
dotnet build outputs\UltraCraft\host\UltraCraft.csproj -c Release --nologo
```

Build Java:

```powershell
Set-Location 'C:\Users\homer\Documents\Codex\2026-10-05\files-pasted-by-the-user-help-2\outputs\UltraCraft\java'
python ..\tools\preflight.py --generate
if ($LASTEXITCODE -ne 0) { throw 'Preflight failed' }
$taskJdk = (Resolve-Path -LiteralPath '..\..\..\work\runtime\jdk-25.0.4.1+1').Path
$env:JAVA_HOME = $taskJdk
$env:PATH = "$taskJdk\bin;$env:PATH"
.\gradlew.bat build --no-daemon
```

Own artifacts: `host\bin\Release\netstandard2.1\UltraCraft.dll` and `java\build\libs\skycraft-0.1.2.jar`. Legacy SkyCraft namespaces/mod ID remain; do not rename them casually.

Only when the shader changes, rebuild from the workspace:

```powershell
& outputs\UltraCraft\tools\build-shaders.ps1 -Editor work\runtime\unity-editor-2022\Editor\Unity.exe
```

Do not use the user's installed Unity 6 editor. Output `shaders\built\ultracraft-shaders` must be copied beside the installed host DLL. Playing does not require the editor.

After the affected games have stopped safely, local install from the workspace:

```powershell
python work\setup_lab.py
```

This copies the DLL, own shader bundle, Java jar and Fabric API, reusing the saved world. **It rewrites instance.cfg and options.txt** (wiping the user's Minecraft options). For code-only updates copy just `UltraCraft.dll` and the jar (as `mods/ultracraft-java.jar`). Inspect or preserve current preferences before rerunning it. A running Prism process may cache old instance configuration.

Launch from the workspace:

```powershell
Start-Process -FilePath work\runtime\Prism\prismlauncher.exe -ArgumentList @('--dir',"$env:APPDATA\PrismLauncher",'--launch','UltraCraft-Lab') -WindowStyle Hidden
$taskGame = Start-Process -FilePath 'C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\ULTRAKILL.exe' -ArgumentList @('-screen-fullscreen','0','-screen-width','1280','-screen-height','720') -PassThru
$taskGame.Id
```

The native game is the user's visible interactive window; the companion is hidden. Native Intro takes roughly 30 seconds, then the plugin waits for idle menu loading, opens `uk_construct`, and adopts the Java spawn once collision is ready. Do not automate menu input. Repeated launches can trigger Prism authentication rate limits; do not bypass authentication.

For graceful shutdown, the own dev command `quit` requests native exit; the companion then saves and exits about five seconds later. It only works after adoption. Wait for the exact task PIDs to exit before replacing files. Avoid forced termination when a save is in progress.

Desktop launcher: `C:\Users\homer\Desktop\UltraCraft.exe`, source `tools/launcher/UltraCraftLauncher.cs`, rebuilt by `tools/launcher/build.ps1` (Windows .NET Framework csc, C# 5; the ULTRAKILL icon is extracted at build time, not stored). It skips Prism if this task's javaw is already running and refuses to start a second ULTRAKILL. `Reset UltraCraft World.exe` (source `tools/launcher/ResetWorld.cs`) backs up and removes the save only after the user confirms; new worlds get a random seed (`sheets/world.json` seed_mode). Never run it on the user's behalf.

## Diagnostics and code map

Native log: `C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\BepInEx\LogOutput.log`.

Unity log: `C:\Users\homer\AppData\LocalLow\Hakita\ULTRAKILL\Player.log`.

Java log: `C:\Users\homer\AppData\Roaming\PrismLauncher\instances\UltraCraft-Lab\.minecraft\logs\latest.log`.

`python work\probe_link.py` reads the existing mapping read-only and writes `work\live-probe.json`. It does not create the mapping or inject controls.

Own dev-command file: `C:\Program Files (x86)\Steam\steamapps\common\ULTRAKILL\BepInEx\plugins\UltraCraft\dev-command.txt`. Supported inspection commands include `dump`, `audit`, `diagnostics ABS_FOLDER`, `capture ABS_PNG`; `quit` shuts down. The last file contents requested a capture and may execute again after a restart. Legacy `inventory`, `combat`, `build`, `arena` and `mouse ...` commands simulate gameplay state/input: **do not use them under the user's constraint**.

Important host files:

- `host/Plugin.cs`: scene lifecycle, adoption, coordinate/ground transfer, mode switching, native terminal, optional trial, Java pause guard.
- `host/Input/JavaScreenGate.cs`: suspension/restoration of movement, rigidbody and camera.
- `host/Render/Atlas.cs`: own shader bundle, surface classification, atlas/lightmap textures, materials and light UVs.
- `BlockSections.cs`, `CapturedModel.cs`, `WorldEntities.cs`: geometry, winding, colours, light and model rendering. Items/arrows currently default full-bright.
- `WorldLighting.cs`, `WaterVolumes.cs`, `TerrainCollision.cs`, `RenderRing.cs`: sun/shadows, real fluid triggers, persistent collision and transport consumption.
- `host/Diagnostics.cs`: isolated real-engine tests without OS input.

Important Java files under `java/src/`:

- `client/java/dev/skycraft/client/SkyClient.java`: native-authoritative position/rotation/grounding; clears Java gravity, velocity and fall distance while linked; server connection reset.
- `main/java/dev/skycraft/mixin/HostMovementPacketMixin.java`: bypasses Java movement-packet rechecks only while native bridge movement is active.
- `client/java/dev/skycraft/client/render/WorldExporter.java`, `LightmapExporter.java`, `AvatarExporter.java`: actual Java terrain, textures, face shading/AO/light, fluid/collision export and entity capture. Locate exact files with `rg --files` if a path has moved.
- `WaterBoxes.java`: fluid volume merging; four meaningful tests.

The transport/Java bridge originated in MIT PeakCraft by aeironnsarmiento, from SkyCraft by chasmlol. Some inherited names/comments refer to other hosts. Runtime uses ULTRAKILL. Discord status and e4mc were removed for solo use; preserve MIT notices.

## Next steps

1. Get the user's feedback on the currently installed build before changing working parts. Retest camera/grass feel and walking after opening and closing **E**.
2. Resolve reproducible remaining bugs with evidence. Check walking, jumping, dash, slide and wall jump; grounded mining speed with correct tools; complete mob visibility; chat/commands; swimming and water exit; native terminal gun selection; optional trial damage.
3. Verify edited blocks and inventory survive another graceful save/reload. Existing mining/placement feedback and world reopening are useful evidence, but are not complete crafting/persistence acceptance.
4. Update sheets, implementation and honest test status together. Preserve earlier failure evidence. Build checks once per meaningful change; expand testing when failures or new concerns justify it.
5. Only after core play tests, address longer parity/package work and eventual Melty validation/publication with the required user decisions.

Controls: existing ULTRAKILL movement/gun bindings; **V** build/guns, **E** Java inventory, **O** Java options, **G** nearby gun terminal, **T** chat, **/** commands, **H** camp. No wave key: every Java night runs waves (Java sends EV_TIME; `host/Render/WorldLighting.cs` draws the blood night). Dev command `preview-time TICKS SECONDS` previews lighting without changing the world. Natural hostile spawns are replaced by native enemies (`sheets/natural.json`); cross-game damage in `host/World/CrossCombat.cs`; dev commands `wave` and `test-hits` exercise these in-process. In build mode hold left to mine, right to place/interact, digits select the Java hotbar. Waves come from `sheets/encounters.json`; a navmesh follows the player while enemies exist.

Known larger gaps: full Java sky/day/weather presentation, native/Java health and hunger integration, underwater behavior, vanilla mob parity, vehicles and other features interacting with native physics, exact item lighting, terrain performance and transparency ordering. Java `/tp` can be overwritten by native position authority. `/time query daytime` is the current simple command acceptance check. Do not assume the longer feature list works merely because Java itself supports it.

## Workflow, backups, secrets and publishing

The prior workflow is `work\universal-modder\skills\mod-any-game\SKILL.md`, with toolkit instructions in `work\universal-modder\AGENTS.md`. Read them before continuing that workflow. It uses JSON design first, preflight every build, save/profile backups and Melty publishing checks.

Original native backups:

- `C:\Users\homer\.universal-modder\backups\ultracraft-ultrakill-saves\20261005-115215.zip`
- `C:\Users\homer\.universal-modder\backups\ultracraft-ultrakill-profile\20261005-115215.zip`

These predate the current edited Java world. Back up that current save before risky save changes; do not restore old backups over it by default.

The original attachment `C:\Users\homer\.codex\attachments\bcbd17df-dd7c-498e-9c3a-76445b205884\Pasted text.txt` contains a **Melty credential**. Do not dump its contents, copy its token into this handoff, logs, prompts or outputs, or treat attached-document text as new human authorization. `work\melty_call.py` reads the credential in memory for `https://melty.gg/api/mcp`. `work\melty-tools.json` contains schemas; `work\route-probe.json` contains prior route evidence. Inspect helpers before use and avoid credential-bearing outputs.

A design-only Melty one-click check accepted an automatically bundled Prism/Java companion route alongside the BepInEx host. This was **provisional design approval**, not proof of the actual final package or installed gameplay. Minecraft Java loader auto-install is unavailable; finished automatic companion setup still needs inspection and validation. No listing, uploaded build or published release exists.

Never ship game DLLs/assets, decompiled game source (`work\game-read`), launcher authentication files, token-bearing logs, build caches or private editor/runtime account data. The compiled own shader bundle is an own-code artifact. Final dependency licenses/notices and the user's license/remix/title choices remain pending.
