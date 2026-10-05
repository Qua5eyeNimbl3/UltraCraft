// Generated from checked JSON sheets. Do not edit.
namespace UltraCraft.Generated;
internal static class Design {
  internal static class Combat_Bridge {
    public const string Id = "bridge";
    public const double UltrakillToMcDamage = 10.0;
    public const double McToUltrakillDamage = 0.1;
    public const double HeadshotMultiplier = 1.5;
    public const int ActorRangeBlocks = 48;
    public const int MaxActors = 64;
    public const int MobProxyLayer = 10;
    public const string System = "weapons";
  }
  internal static class Controls_BuildMode {
    public const string Id = "build_mode";
    public const string Key = "V";
    public const string Action = "Toggle mining/building mode and gun mode";
    public const string System = "building";
  }
  internal static class Controls_Inventory {
    public const string Id = "inventory";
    public const string Key = "E";
    public const string Action = "Open Java inventory";
    public const string System = "inventory";
  }
  internal static class Controls_JavaMenu {
    public const string Id = "java_menu";
    public const string Key = "O";
    public const string Action = "Open Java pause/options screen";
    public const string System = "inventory";
  }
  internal static class Controls_Terminal {
    public const string Id = "terminal";
    public const string Key = "G";
    public const string Action = "Open native gun terminal when near it";
    public const string System = "terminal";
  }
  internal static class Controls_Chat {
    public const string Id = "chat";
    public const string Key = "T";
    public const string Action = "Open Java chat; slash opens Java commands";
    public const string System = "commands";
  }
  internal static class Controls_Camp {
    public const string Id = "camp";
    public const string Key = "H";
    public const string Action = "Return to the session camp on request";
    public const string System = "movement";
  }
  internal static class Encounters_Waves {
    public const string Id = "waves";
    public const string Waves = "Filth:4|Filth:4,Stray:2|Filth:3,Stray:2,Schism:1|Stray:2,Schism:2,Soldier:2,Drone:2|Filth:4,Soldier:2,Drone:3,Streetcleaner:1|Schism:2,Soldier:2,Drone:2,Swordsmachine:1,MaliciousFace:1|Stray:3,Drone:3,Cerberus:1,Mindflayer:1";
    public const int MinRadiusBlocks = 10;
    public const int MaxRadiusBlocks = 18;
    public const int NavmeshRadiusBlocks = 48;
    public const double NavmeshClimbBlocks = 1.1;
    public const string System = "encounters";
    public const int AutoNextDelaySeconds = 6;
    public const int NightStartTicks = 13000;
    public const int NightEndTicks = 23000;
  }
  internal static class Fluids_Water {
    public const string Id = "water";
    public const int MessageType = 16;
    public const int Layer = 4;
    public const string System = "water";
    public const int ActiveRadiusBlocks = 32;
  }
  internal static class Hooks_JavaWorld {
    public const string Id = "java_world";
    public const string Engine = "java";
    public const string Type = "dev.skycraft.client.MirrorWorld";
    public const string Member = "openWhenReady";
    public const string Source = "PeakCraft/fabric/src/client/java/dev/skycraft/client/MirrorWorld.java";
  }
  internal static class Hooks_JavaLink {
    public const string Id = "java_link";
    public const string Engine = "java";
    public const string Type = "dev.skycraft.link.SkyLink";
    public const string Member = "readSkyState";
    public const string Source = "PeakCraft/fabric/src/main/java/dev/skycraft/link/SkyLink.java";
  }
  internal static class Hooks_JavaInput {
    public const string Id = "java_input";
    public const string Engine = "java";
    public const string Type = "dev.skycraft.client.InputBridge";
    public const string Member = "drain";
    public const string Source = "PeakCraft/fabric/src/client/java/dev/skycraft/client/InputBridge.java";
  }
  internal static class Hooks_JavaRender {
    public const string Id = "java_render";
    public const string Engine = "java";
    public const string Type = "dev.skycraft.client.render.WorldExporter";
    public const string Member = "frame";
    public const string Source = "PeakCraft/fabric/src/client/java/dev/skycraft/client/render/WorldExporter.java";
  }
  internal static class Hooks_NativePlayer {
    public const string Id = "native_player";
    public const string Engine = "unity";
    public const string Type = "NewMovement";
    public const string Member = "rb";
    public const string Source = "game-read/NewMovement.cs";
  }
  internal static class Hooks_NativeGuns {
    public const string Id = "native_guns";
    public const string Engine = "unity";
    public const string Type = "GunControl";
    public const string Member = "currentWeapon";
    public const string Source = "game-read/GunControl.cs";
  }
  internal static class Hooks_NativeTerminal {
    public const string Id = "native_terminal";
    public const string Engine = "unity";
    public const string Type = "ShopZone";
    public const string Member = "TurnOn";
    public const string Source = "game-read/ShopZone.cs";
  }
  internal static class Hooks_NativeScene {
    public const string Id = "native_scene";
    public const string Engine = "unity";
    public const string Type = "SceneHelper";
    public const string Member = "LoadScene";
    public const string Source = "game-read/SceneHelper.cs";
  }
  internal static class Hooks_NativeEnemies {
    public const string Id = "native_enemies";
    public const string Engine = "unity";
    public const string Type = "SpawnableObjectsDatabase";
    public const string Member = "enemies";
    public const string Source = "game-read/SpawnableObjectsDatabase.cs";
  }
  internal static class Hooks_NativeWater {
    public const string Id = "native_water";
    public const string Engine = "unity";
    public const string Type = "Water";
    public const string Member = "ApplyWaterForces";
    public const string Source = "game-read/Water.decompiled.cs";
  }
  internal static class Launch_Main {
    public const string Id = "main";
    public const string HostScene = "uk_construct";
    public const string Mapping = "Local\\UltraCraft_v1";
    public const string Loader = "bepinex-5";
    public const string LoaderVersion = "5.4.23.5";
    public const string GuestLoaderVersion = "0.19.5";
    public const string System = "launch";
    public const int ProtocolVersion = 12;
  }
  internal static class Mining_Guns {
    public const string Id = "guns";
    public const double RevolverPower = 1.0;
    public const double PelletPower = 0.3;
    public const double NailPower = 0.2;
    public const double SawPower = 0.6;
    public const double SawWoodMultiplier = 6.0;
    public const double SawOtherMultiplier = 0.4;
    public const double RailgunRadiusBlocks = 3.0;
    public const double ExplosionRadiusPerUnit = 0.25;
    public const double MaxExplosionRadiusBlocks = 3.0;
    public const double BlastDropChance = 0.35;
    public const double MaxBreakableHardness = 49.0;
    public const int DamageDecaySeconds = 8;
    public const string System = "weapon_mining";
  }
  internal static class Natural_Night {
    public const string Id = "night";
    public const string Replacements = "zombie:Filth,husk:Filth,zombie_villager:Filth,drowned:Filth,skeleton:Stray,stray:Stray,bogged:Stray,creeper:Drone,spider:Schism,cave_spider:Schism,enderman:Virtue,witch:Mindflayer,pillager:Soldier,vindicator:Soldier";
    public const int MinDistanceBlocks = 16;
    public const int MaxDistanceBlocks = 40;
    public const int MaxHeightDifferenceBlocks = 16;
    public const int MaxAlive = 10;
    public const int DespawnDistanceBlocks = 96;
    public const int CooldownSeconds = 3;
    public const string System = "natural_spawns";
  }
  internal static class Render_Native {
    public const string Id = "native";
    public const string OpaqueShader = "UltraCraft/World";
    public const string CutoutShader = "UltraCraft/World";
    public const string TransparentShader = "UltraCraft/World";
    public const string SkyColor = "#8fc5e8";
    public const string System = "world_render";
    public const string ShaderBundle = "ultracraft-shaders";
    public const int LightmapTextureId = 65534;
    public const double ShadowStrength = 0.55;
    public const int ShadowDistanceBlocks = 48;
    public const string SunRotation = "50,-30,0";
    public const double OverlayScale = 0.5;
    public const int ShadowCascades = 2;
    public const string DuskSkyColor = "#d8613a";
    public const string NightSkyColor = "#2a0306";
    public const string NightTint = "0.42,0.17,0.15";
    public const string MoonColor = "#8a2418";
    public const int FogDayStartBlocks = 72;
    public const int FogDayEndBlocks = 126;
    public const int FogNightStartBlocks = 12;
    public const int FogNightEndBlocks = 70;
    public const string NightFogColor = "#1c0204";
  }
  internal static class Systems_Movement {
    public const string Id = "movement";
    public const string Driver = "ULTRAKILL";
    public const string Hook = "native_player";
    public const string Policy = "native movement owns feet and grounded state; initialize alive native movement on adoption; Java screens suspend and restore only bridge-owned movement/camera state; preserve native pause and death; integrated server player chunk tracking follows native position every tick";
    public const string Test = "active/enabled/dynamic body after adoption; Java-screen suspend/resume round trip; manual walk, jump, dash, slide and wall jump on Java block collisions";
  }
  internal static class Systems_Mining {
    public const string Id = "mining";
    public const string Driver = "Minecraft Java";
    public const string Hook = "java_input";
    public const string Policy = "forward held left mouse in building mode; vanilla survival mining";
    public const string Test = "break log, pick up item, mine stone with correct tool";
  }
  internal static class Systems_Building {
    public const string Id = "building";
    public const string Driver = "Minecraft Java";
    public const string Hook = "java_input";
    public const string Policy = "forward right mouse in building mode; vanilla placement";
    public const string Test = "place block, collide with it, break it, save and reload";
  }
  internal static class Systems_Crafting {
    public const string Id = "crafting";
    public const string Driver = "Minecraft Java";
    public const string Hook = "java_input";
    public const string Policy = "vanilla inventory and crafting table screens";
    public const string Test = "log to planks, planks to crafting table, craft a pickaxe";
  }
  internal static class Systems_Inventory {
    public const string Id = "inventory";
    public const string Driver = "Minecraft Java";
    public const string Hook = "java_input";
    public const string Policy = "native Java inventory and hotbar overlay; Escape closes Java screens without opening the native pause menu";
    public const string Test = "open inventory, move stacks, select hotbar slots";
  }
  internal static class Systems_WorldRender {
    public const string Id = "world_render";
    public const string Driver = "Minecraft Java plus Unity";
    public const string Hook = "java_render";
    public const string Policy = "own stable depth-writing shader, alpha cutout and transparent water; preserve Java face shading, AO and live lightmap; directional light casts shadows; stream geometry, textures, mobs and collisions across scene loads; mipmapped block atlas and stable-fit shadow cascades against shimmer; hide native sandbox map geometry and collision after adoption; Minecraft-style dark block outline only in build mode; break cracks drawn from the Java destroy stage; day/night from the Java clock: moving sun/moon, sunset, blood-red night sky, red terrain tint and fog";
    public const string Test = "GPU opaque/cutout depth, cutout holes, vertex tint, lightmap contrast, cast-shadow comparison and smooth projected motion; manual terrain/camera test";
  }
  internal static class Systems_Weapons {
    public const string Id = "weapons";
    public const string Driver = "ULTRAKILL";
    public const string Hook = "native_guns";
    public const string Policy = "preserve native guns, variants, damage and combat; native weapon hits on Minecraft-mob hitboxes become Minecraft damage; Minecraft melee/arrow hits on mirrored native enemies become native damage";
    public const string Test = "shoot a Minecraft cow/zombie with the revolver and see it take damage; hit a Filth with a Minecraft sword in build mode and see it take damage";
  }
  internal static class Systems_Terminal {
    public const string Id = "terminal";
    public const string Driver = "ULTRAKILL";
    public const string Hook = "native_terminal";
    public const string Policy = "move native gun-selection terminal near Java spawn";
    public const string Test = "open actual terminal and change equipped weapon";
  }
  internal static class Systems_Encounters {
    public const string Id = "encounters";
    public const string Driver = "ULTRAKILL";
    public const string Hook = "native_enemies";
    public const string Policy = "no wave key; every night (Java day time 13000-23000) runs native enemy waves around the player, next wave after each clear, none started after dawn; navmesh follows the player; enemies stand on Java collision; stuck watchdog re-seats enemies off the navmesh, stranded far away or fallen through terrain";
    public const string Test = "at nightfall wave 1 appears without pressing anything; clearing it brings wave 2; daytime brings no new waves";
  }
  internal static class Systems_Saving {
    public const string Id = "saving";
    public const string Driver = "Minecraft Java";
    public const string Hook = "java_world";
    public const string Policy = "dedicated UltraCraft world; native Java autosave and graceful shutdown; desktop reset tool backs up then removes the save (only on the user's confirmation), next launch creates a new world with a random seed";
    public const string Test = "reload same block changes and inventory";
  }
  internal static class Systems_Launch {
    public const string Id = "launch";
    public const string Driver = "Melty plus companion launcher";
    public const string Hook = "native_scene";
    public const string Policy = "BepInEx 5 host; fully automatic bundled Java companion setup; wait for idle native scene loading, keep the bridge running in the background";
    public const string Test = "Melty installs package, sets up Java, launches both and links";
  }
  internal static class Systems_Water {
    public const string Id = "water";
    public const string Driver = "Minecraft Java plus ULTRAKILL";
    public const string Hook = "native_water";
    public const string Policy = "export merged real Java water volumes; create native layer-4 Water triggers after sandbox loads";
    public const string Test = "enter water, sink slowly, swim/jump upward, dash and leave; placed water updates volume";
  }
  internal static class Systems_Commands {
    public const string Id = "commands";
    public const string Driver = "Minecraft Java";
    public const string Hook = "java_input";
    public const string Policy = "route T and slash to vanilla chat/command screen, then route text and keyboard while open";
    public const string Test = "open commands using slash, enter /time query daytime, receive result without native pause";
  }
  internal static class Systems_NaturalSpawns {
    public const string Id = "natural_spawns";
    public const string Driver = "Minecraft Java plus ULTRAKILL";
    public const string Hook = "native_enemies";
    public const string Policy = "vanilla decides when and where hostile mobs may spawn; mapped hostile types become native enemies on Java terrain near the player; passive mobs unchanged";
    public const string Test = "at night or in a dark cave, Filth/Strays/etc. appear instead of zombies/skeletons; cows, pigs and sheep still spawn";
  }
  internal static class Systems_WeaponMining {
    public const string Id = "weapon_mining";
    public const string Driver = "ULTRAKILL plus Minecraft Java";
    public const string Hook = "native_guns";
    public const string Policy = "player weapon hits on Java terrain accumulate block damage with cracks and break blocks with drops; railcannon and player explosions break a sphere; thrown chainsaw saws contacted blocks, wood fast";
    public const string Test = "shoot dirt with the revolver (breaks, drops), railcannon a hillside (crater), throw the chainsaw at a tree (logs drop quickly)";
  }
  internal static class World_Fresh {
    public const string Id = "fresh";
    public const string Name = "UltraCraft";
    public const string JavaVersion = "26.3";
    public const int Seed = 742916305;
    public const string Preset = "minecraft:normal";
    public const string Mode = "survival";
    public const string Difficulty = "easy";
    public const bool Structures = true;
    public const double HostScale = 2.0;
    public const string Hook = "java_world";
    public const int ViewDistanceChunks = 8;
    public const int SimulationDistanceChunks = 6;
    public const int KeepRadiusChunks = 10;
    public const string SeedMode = "random";
  }
}
