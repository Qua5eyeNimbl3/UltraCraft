# UltraCraft

A solo Minecraft **Java Edition** survival world with ULTRAKILL movement, weapons and combat. Minecraft's actual simulation supplies mining, placement, inventory, crafting, mobs and saves; the player's installed ULTRAKILL supplies the visible game, movement, guns and native enemies. Minecraft runs hidden behind ULTRAKILL. Bedrock is not used.

Mine resources, craft in Minecraft's inventory and crafting-table screens, and place blocks. Switch to native guns at any time. An actual ULTRAKILL gun terminal sits near camp. The world and inventory are isolated from other instances and save when the host closes.

**Blood nights:** lighting follows Minecraft's clock. At nightfall the sky turns blood red and native enemy waves begin; a cleared wave brings the next until dawn. No manual wave key. Navigation builds asynchronously before a wave is placed.

**Natural enemies:** in dark caves and at night, mapped monsters become native enemies: zombie → Filth, skeleton → Stray, creeper → Drone, spider → Schism, enderman → Virtue, witch → Mindflayer, pillager/vindicator → Soldier. Passive mobs stay. Guns damage Minecraft mobs, and Minecraft weapons damage native enemies.

**Whiplash:** works like ULTRAKILL's and grapples blocks. Small Minecraft mobs (zombies, skeletons, animals) and light ULTRAKILL enemies get yanked to you; big mobs (iron golems, ravagers, horses, the warden) and heavy enemies pull you to them. Throw it at any block more than a couple of blocks away and it latches on and pulls you there; jump while being pulled to let go with a boost. Tuned in `sheets/whiplash.json`.

**Guns mine:** revolver shots, pellets and nails chip blocks; the railcannon blasts a sphere; rockets/explosions make craters; the thrown chainsaw cuts logs. Broken blocks drop resources according to the Java bridge's mining rules.

**New worlds:** close UltraCraft and run Reset UltraCraft World.exe in %LOCALAPPDATA%\UltraCraft. It asks first, backs up the current world, then removes it. Next Play generates a random seed. Never reset a player's existing world to test a release.

## Controls

- Existing ULTRAKILL movement and weapon bindings.
- V: building / gun mode; E: Minecraft inventory and crafting; O: Minecraft options.
- T or /: chat / commands; G: nearby gun terminal; H: return to camp.
- Building mode: hold left mouse to mine, right mouse to place/interact, number keys select the Java hotbar.

V/E/O are configurable through BepInEx. Design defaults live in sheets/.

## Requirements and release

Requires the player's installed **ULTRAKILL Unity Mono** game and a Microsoft account owning **Minecraft: Java Edition**. The isolated companion is pinned to Minecraft **26.3**, Fabric Loader **0.19.5** and Fabric API **0.161.0+26.3**. The version of an unrelated Minecraft installation does not change this companion. Melty installs BepInEx **5.4.23.5**.

The release bundles unmodified portable Prism Launcher **11.1.1**, Eclipse Temurin JRE **25.0.4.1+1**, the Java bridge, host plugin and own shader bundle. Prism retrieves Minecraft using the player's account. No accounts, saves, extracted game assets or game assemblies are distributed.

Play signs in when needed, starts the companion with its explicit portable directory, waits for initialization and leaves it running for ULTRAKILL. Later Plays warm-start that same instance. Closing ULTRAKILL lets Minecraft save and close. No separate desktop installer is required.

melty.json describes both archives and destinations. **0.1.6 is undergoing real Melty install and stability tests**; see RELEASE-0.1.6.md. Full Minecraft feature parity has not been demonstrated.

## Builds

Use JDK 25 and the installed game's assemblies. Build host/UltraCraft.csproj with dotnet, and run java/gradlew.bat build. tests/transport/TransportTests.csproj checks protocol layouts, ring wrapping, malformed messages, exception recovery, frame budgets and native upload counts. tools/package-melty.ps1 -Version 0.1.6 creates the release and verifies required launcher/runtime entries.

Before building, tools/preflight.py --generate checks design sheets and hook references. ULTRACRAFT_REFERENCE_ROOT can point to the development references containing PeakCraft/ and game-read/. Specification checks are separate from runtime acceptance.

The own shader bundle is compiled for Unity 2022.3.29f1 and sits beside UltraCraft.dll. Playing does not need Unity Editor.

## Credits and permissions

UltraCraft by **Qua5eyeNimbl3**, **MIT** licensed; remixes are allowed under that license. New code includes AI assistance. Bridge adapted from **PeakCraft** by **aeironnsarmiento**, originating in **SkyCraft** by **chasmlol**; MIT notices remain in THIRD-PARTY/.

Prism contributors (GPLv3), FabricMC (Fabric Loader/API, Apache-2.0), and Eclipse Adoptium/OpenJDK contributors (Temurin, GPLv2 with Classpath Exception) supply supporting software. Archives retain dependency notices and corresponding source links.

Minecraft is by Mojang/Microsoft; ULTRAKILL by Hakita and New Blood Interactive. This is an unofficial fan integration.
