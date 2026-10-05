# UltraCraft — development prototype

A fresh Minecraft **Java Edition** world with mining, building, crafting, and ULTRAKILL movement, weapons, and combat. Solo only. Bedrock is not used.

**Status: development build undergoing play tests and fixes.** The latest revision fixes inactive native movement, replaces retro vertex-warp shaders, restores Java face shading and its actual lightmap, and adds cast shadows. Real engine checks pass; movement feel and the remaining gameplay steps still need hands-on testing. The world saved and reopened. See `PLAY-TEST.md` and `TEST-STATUS.md`. Full Minecraft feature parity has not been demonstrated.

## Intended first playable experience

Start in a fresh Java survival world (easy difficulty, with natural Minecraft mobs; use `/difficulty peaceful` to remove hostile ones). Mine resources, craft in Minecraft's own inventory and crafting-table screens, and place blocks. Switch to ULTRAKILL guns at any time. The native gun terminal belongs near spawn. Press J whenever you want a wave of native enemies. Minecraft stores your world and inventory separately from existing saves.

The integration runs the actual Minecraft Java simulation and the actual ULTRAKILL game. The ULTRAKILL player supplies movement and gun combat; the Java companion supplies block, inventory, crafting, and save rules. No game assemblies or extracted game assets belong in a published package.

## Prototype controls

- Your existing ULTRAKILL movement and gun bindings.
- **V:** switch mining/building mode and gun mode.
- **E:** Minecraft inventory.
- **O:** Minecraft options/menu.
- **G:** use the gun terminal when nearby.
- **T** / **/**: Java chat / commands.
## Easy install for players

Download `UltraCraft.exe` from the Releases page and run it, then press **Play**. The first time it:

- finds ULTRAKILL through Steam, installs BepInEx 5.4.23.5 if needed and adds the UltraCraft plugin;
- downloads Prism Launcher, Java 25 and Fabric API from their official releases into `%LOCALAPPDATA%\UltraCraft`
  and sets up a Minecraft 26.3 + Fabric instance with the UltraCraft mod;
- opens Prism once so you can sign in with your own Microsoft account (you need to own Minecraft Java Edition).

After that, **Play** starts Minecraft hidden and ULTRAKILL through Steam. **Reset world** backs up your world and
makes a new one with a random seed next time. The program is built by `tools/launcher/build.ps1` with the plugin,
shader bundle and mod jar embedded.

**Desktop launcher:** `UltraCraft.exe` on the Desktop starts the hidden Java companion and then ULTRAKILL; quitting ULTRAKILL saves and closes Java. Rebuild it with `tools/launcher/build.ps1`.

- **H:** return to camp.

**Blood nights:** day and night follow the Minecraft clock. At nightfall the sky turns blood red, a red fog closes in and ULTRAKILL enemy waves begin around you; each cleared wave brings the next until dawn. There is no wave key.

**Guns mine blocks:** revolver shots chip and break blocks (dirt in one shot, stone in two), shotgun pellets and nails chip them, the railcannon blasts out a sphere, your rockets and explosions blow craters, and the thrown chainsaw saws through logs very fast. Broken blocks drop their items. Tune it in `sheets/mining.json`.

**New world / new seed:** run `Reset UltraCraft World` on the Desktop (with the game closed). It asks first, saves a backup zip of your current world, then removes it; the next launch generates a brand-new world with a random seed.

At night and in dark caves, ULTRAKILL enemies spawn instead of Minecraft hostiles (zombie → Filth, skeleton → Stray, creeper → Drone, spider → Schism, enderman → Virtue, witch → Mindflayer, pillager/vindicator → Soldier; edit `sheets/natural.json` to change). Passive mobs are unchanged. ULTRAKILL guns hurt Minecraft mobs, and Minecraft weapons (in build mode) hurt ULTRAKILL enemies.
- In building mode, left mouse mines, right mouse places or interacts, and number keys select the Java hotbar.

These defaults are recorded in `sheets/controls.json`. V/E/O can be configured through the host loader.

## Requirements and install status

Targets Minecraft Java **26.3**, ULTRAKILL's installed Unity Mono build, and BepInEx **5.4.23.5**. Melty installs the host loader. The proposed companion-launch recipe passed a **design-only** Melty one-click check. The finished package, automatic setup, and real Melty install still need validation; one-click gameplay has not been established.

The local test instance is isolated from your other Minecraft worlds. This source folder is not an installable Melty release.

## Design and builds

JSON sheets are the source of truth. Every row produces a C# descriptor and a Java descriptor. Before each build, `tools/preflight.py --generate` checks all cells, their specification checkboxes, and cross-sheet references. Runtime acceptance tests remain separate from specification preflight.

The own world shader is in `shaders/World.shader`; its compiled Unity 2022.3.29f1 bundle is in `shaders/built/ultracraft-shaders`. It must sit beside `UltraCraft.dll` in the host plugin folder. The local installer copies both. Rebuilding this bundle uses `tools/build-shaders.ps1 -Editor <Unity.exe>`; playing the compiled build does not require the Unity Editor.

## Credits and permissions

The Java bridge and host transport/renderer are adapted from **PeakCraft** by **aeironnsarmiento**, originating in **SkyCraft** by **chasmlol**. Their MIT notices are preserved under `THIRD-PARTY/`. BepInEx, Fabric Loader/API, Prism Launcher, and Eclipse Temurin are supporting tools/dependencies; exact distributed versions and license notices must accompany the final package.

Minecraft is by Mojang/Microsoft; ULTRAKILL is by Arsi “Hakita” Patala and published by New Blood Interactive. This is a fan project, not an official product. New integration code was written with AI assistance. The user's content license and remix permission are **not yet chosen**; no new license or remix grant is implied.
