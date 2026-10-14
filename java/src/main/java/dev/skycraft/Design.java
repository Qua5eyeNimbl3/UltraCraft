// Generated from checked JSON sheets. Do not edit.
package dev.skycraft;
public final class Design {
  public static final class Combat_Bridge {
    public static final String Id = "bridge";
    public static final double UltrakillToMcDamage = 10.0;
    public static final double McToUltrakillDamage = 0.1;
    public static final double HeadshotMultiplier = 1.5;
    public static final int ActorRangeBlocks = 48;
    public static final int MaxActors = 64;
    public static final int MobProxyLayer = 10;
    public static final String System = "weapons";
  }
  public static final class Controls_BuildMode {
    public static final String Id = "build_mode";
    public static final String Key = "V";
    public static final String Action = "Toggle mining/building mode and gun mode";
    public static final String System = "building";
  }
  public static final class Controls_Inventory {
    public static final String Id = "inventory";
    public static final String Key = "E";
    public static final String Action = "Open Java inventory";
    public static final String System = "inventory";
  }
  public static final class Controls_JavaMenu {
    public static final String Id = "java_menu";
    public static final String Key = "O";
    public static final String Action = "Open Java pause/options screen";
    public static final String System = "inventory";
  }
  public static final class Controls_Terminal {
    public static final String Id = "terminal";
    public static final String Key = "G";
    public static final String Action = "Open native gun terminal when near it";
    public static final String System = "terminal";
  }
  public static final class Controls_Chat {
    public static final String Id = "chat";
    public static final String Key = "T";
    public static final String Action = "Open Java chat; slash opens Java commands";
    public static final String System = "commands";
  }
  public static final class Controls_Camp {
    public static final String Id = "camp";
    public static final String Key = "H";
    public static final String Action = "Return to the session camp on request";
    public static final String System = "movement";
  }
  public static final class Encounters_Waves {
    public static final String Id = "waves";
    public static final String Waves = "Filth:4|Filth:4,Stray:2|Filth:3,Stray:2,Schism:1|Stray:2,Schism:2,Soldier:2,Drone:2|Filth:4,Soldier:2,Drone:3,Streetcleaner:1|Schism:2,Soldier:2,Drone:2,Swordsmachine:1,MaliciousFace:1|Stray:3,Drone:3,Cerberus:1,Mindflayer:1";
    public static final int MinRadiusBlocks = 10;
    public static final int MaxRadiusBlocks = 18;
    public static final int NavmeshRadiusBlocks = 48;
    public static final double NavmeshClimbBlocks = 1.1;
    public static final String System = "encounters";
    public static final int AutoNextDelaySeconds = 6;
    public static final int NightStartTicks = 13000;
    public static final int NightEndTicks = 23000;
  }
  public static final class Fluids_Water {
    public static final String Id = "water";
    public static final int MessageType = 16;
    public static final int Layer = 4;
    public static final String System = "water";
    public static final int ActiveRadiusBlocks = 32;
  }
  public static final class Hooks_JavaWorld {
    public static final String Id = "java_world";
    public static final String Engine = "java";
    public static final String Type = "dev.skycraft.client.MirrorWorld";
    public static final String Member = "openWhenReady";
    public static final String Source = "PeakCraft/fabric/src/client/java/dev/skycraft/client/MirrorWorld.java";
  }
  public static final class Hooks_JavaLink {
    public static final String Id = "java_link";
    public static final String Engine = "java";
    public static final String Type = "dev.skycraft.link.SkyLink";
    public static final String Member = "readSkyState";
    public static final String Source = "PeakCraft/fabric/src/main/java/dev/skycraft/link/SkyLink.java";
  }
  public static final class Hooks_JavaInput {
    public static final String Id = "java_input";
    public static final String Engine = "java";
    public static final String Type = "dev.skycraft.client.InputBridge";
    public static final String Member = "drain";
    public static final String Source = "PeakCraft/fabric/src/client/java/dev/skycraft/client/InputBridge.java";
  }
  public static final class Hooks_JavaRender {
    public static final String Id = "java_render";
    public static final String Engine = "java";
    public static final String Type = "dev.skycraft.client.render.WorldExporter";
    public static final String Member = "frame";
    public static final String Source = "PeakCraft/fabric/src/client/java/dev/skycraft/client/render/WorldExporter.java";
  }
  public static final class Hooks_NativePlayer {
    public static final String Id = "native_player";
    public static final String Engine = "unity";
    public static final String Type = "NewMovement";
    public static final String Member = "rb";
    public static final String Source = "game-read/NewMovement.cs";
  }
  public static final class Hooks_NativeGuns {
    public static final String Id = "native_guns";
    public static final String Engine = "unity";
    public static final String Type = "GunControl";
    public static final String Member = "currentWeapon";
    public static final String Source = "game-read/GunControl.cs";
  }
  public static final class Hooks_NativeTerminal {
    public static final String Id = "native_terminal";
    public static final String Engine = "unity";
    public static final String Type = "ShopZone";
    public static final String Member = "TurnOn";
    public static final String Source = "game-read/ShopZone.cs";
  }
  public static final class Hooks_NativeScene {
    public static final String Id = "native_scene";
    public static final String Engine = "unity";
    public static final String Type = "SceneHelper";
    public static final String Member = "LoadScene";
    public static final String Source = "game-read/SceneHelper.cs";
  }
  public static final class Hooks_NativeEnemies {
    public static final String Id = "native_enemies";
    public static final String Engine = "unity";
    public static final String Type = "SpawnableObjectsDatabase";
    public static final String Member = "enemies";
    public static final String Source = "game-read/SpawnableObjectsDatabase.cs";
  }
  public static final class Hooks_NativeWater {
    public static final String Id = "native_water";
    public static final String Engine = "unity";
    public static final String Type = "Water";
    public static final String Member = "ApplyWaterForces";
    public static final String Source = "game-read/Water.decompiled.cs";
  }
  public static final class Launch_Main {
    public static final String Id = "main";
    public static final String HostScene = "uk_construct";
    public static final String Mapping = "Local\\UltraCraft_v1";
    public static final String Loader = "bepinex-5";
    public static final String LoaderVersion = "5.4.23.5";
    public static final String GuestLoaderVersion = "0.19.5";
    public static final String System = "launch";
    public static final int ProtocolVersion = 12;
  }
  public static final class Mining_Guns {
    public static final String Id = "guns";
    public static final double RevolverPower = 1.0;
    public static final double PelletPower = 0.3;
    public static final double NailPower = 0.2;
    public static final double SawPower = 0.6;
    public static final double SawWoodMultiplier = 6.0;
    public static final double SawOtherMultiplier = 0.4;
    public static final double RailgunRadiusBlocks = 3.0;
    public static final double ExplosionRadiusPerUnit = 0.25;
    public static final double MaxExplosionRadiusBlocks = 3.0;
    public static final double BlastDropChance = 0.35;
    public static final double MaxBreakableHardness = 49.0;
    public static final int DamageDecaySeconds = 8;
    public static final String System = "weapon_mining";
  }
  public static final class Natural_Night {
    public static final String Id = "night";
    public static final String Replacements = "zombie:Filth,husk:Filth,zombie_villager:Filth,drowned:Filth,skeleton:Stray,stray:Stray,bogged:Stray,creeper:Drone,spider:Schism,cave_spider:Schism,enderman:Virtue,witch:Mindflayer,pillager:Soldier,vindicator:Soldier";
    public static final int MinDistanceBlocks = 16;
    public static final int MaxDistanceBlocks = 40;
    public static final int MaxHeightDifferenceBlocks = 16;
    public static final int MaxAlive = 10;
    public static final int DespawnDistanceBlocks = 96;
    public static final int CooldownSeconds = 3;
    public static final String System = "natural_spawns";
  }
  public static final class Render_Native {
    public static final String Id = "native";
    public static final String OpaqueShader = "UltraCraft/World";
    public static final String CutoutShader = "UltraCraft/World";
    public static final String TransparentShader = "UltraCraft/World";
    public static final String SkyColor = "#8fc5e8";
    public static final String System = "world_render";
    public static final String ShaderBundle = "ultracraft-shaders";
    public static final int LightmapTextureId = 65534;
    public static final double ShadowStrength = 0.55;
    public static final int ShadowDistanceBlocks = 48;
    public static final String SunRotation = "50,-30,0";
    public static final double OverlayScale = 0.5;
    public static final int ShadowCascades = 2;
    public static final String DuskSkyColor = "#d8613a";
    public static final String NightSkyColor = "#2a0306";
    public static final String NightTint = "0.42,0.17,0.15";
    public static final String MoonColor = "#8a2418";
    public static final int FogDayStartBlocks = 72;
    public static final int FogDayEndBlocks = 126;
    public static final int FogNightStartBlocks = 12;
    public static final int FogNightEndBlocks = 70;
    public static final String NightFogColor = "#1c0204";
  }
  public static final class Systems_Movement {
    public static final String Id = "movement";
    public static final String Driver = "ULTRAKILL";
    public static final String Hook = "native_player";
    public static final String Policy = "native movement owns feet and grounded state; initialize alive native movement on adoption; Java screens suspend and restore only bridge-owned movement/camera state; preserve native pause and death; integrated server player chunk tracking follows native position every tick";
    public static final String Test = "active/enabled/dynamic body after adoption; Java-screen suspend/resume round trip; manual walk, jump, dash, slide and wall jump on Java block collisions";
  }
  public static final class Systems_Mining {
    public static final String Id = "mining";
    public static final String Driver = "Minecraft Java";
    public static final String Hook = "java_input";
    public static final String Policy = "forward held left mouse in building mode; vanilla survival mining";
    public static final String Test = "break log, pick up item, mine stone with correct tool";
  }
  public static final class Systems_Building {
    public static final String Id = "building";
    public static final String Driver = "Minecraft Java";
    public static final String Hook = "java_input";
    public static final String Policy = "forward right mouse in building mode; vanilla placement";
    public static final String Test = "place block, collide with it, break it, save and reload";
  }
  public static final class Systems_Crafting {
    public static final String Id = "crafting";
    public static final String Driver = "Minecraft Java";
    public static final String Hook = "java_input";
    public static final String Policy = "vanilla inventory and crafting table screens";
    public static final String Test = "log to planks, planks to crafting table, craft a pickaxe";
  }
  public static final class Systems_Inventory {
    public static final String Id = "inventory";
    public static final String Driver = "Minecraft Java";
    public static final String Hook = "java_input";
    public static final String Policy = "native Java inventory and hotbar overlay; Escape closes Java screens without opening the native pause menu";
    public static final String Test = "open inventory, move stacks, select hotbar slots";
  }
  public static final class Systems_WorldRender {
    public static final String Id = "world_render";
    public static final String Driver = "Minecraft Java plus Unity";
    public static final String Hook = "java_render";
    public static final String Policy = "own stable depth-writing shader, alpha cutout and transparent water; preserve Java face shading, AO and live lightmap; directional light casts shadows; stream geometry, textures, mobs and collisions across scene loads; mipmapped block atlas and stable-fit shadow cascades against shimmer; hide native sandbox map geometry and collision after adoption; Minecraft-style dark block outline only in build mode; break cracks drawn from the Java destroy stage; day/night from the Java clock: moving sun/moon, sunset, blood-red night sky, red terrain tint and fog";
    public static final String Test = "GPU opaque/cutout depth, cutout holes, vertex tint, lightmap contrast, cast-shadow comparison and smooth projected motion; manual terrain/camera test";
  }
  public static final class Systems_Weapons {
    public static final String Id = "weapons";
    public static final String Driver = "ULTRAKILL";
    public static final String Hook = "native_guns";
    public static final String Policy = "preserve native guns, variants, damage and combat; native weapon hits on Minecraft-mob hitboxes become Minecraft damage; Minecraft melee/arrow hits on mirrored native enemies become native damage";
    public static final String Test = "shoot a Minecraft cow/zombie with the revolver and see it take damage; hit a Filth with a Minecraft sword in build mode and see it take damage";
  }
  public static final class Systems_Terminal {
    public static final String Id = "terminal";
    public static final String Driver = "ULTRAKILL";
    public static final String Hook = "native_terminal";
    public static final String Policy = "move native gun-selection terminal near Java spawn";
    public static final String Test = "open actual terminal and change equipped weapon";
  }
  public static final class Systems_Encounters {
    public static final String Id = "encounters";
    public static final String Driver = "ULTRAKILL";
    public static final String Hook = "native_enemies";
    public static final String Policy = "no wave key; every night (Java day time 13000-23000) runs native enemy waves around the player, next wave after each clear, none started after dawn; navmesh follows the player; enemies stand on Java collision; stuck watchdog re-seats enemies off the navmesh, stranded far away or fallen through terrain";
    public static final String Test = "at nightfall wave 1 appears without pressing anything; clearing it brings wave 2; daytime brings no new waves";
  }
  public static final class Systems_Saving {
    public static final String Id = "saving";
    public static final String Driver = "Minecraft Java";
    public static final String Hook = "java_world";
    public static final String Policy = "dedicated UltraCraft world; native Java autosave and graceful shutdown; desktop reset tool backs up then removes the save (only on the user's confirmation), next launch creates a new world with a random seed";
    public static final String Test = "reload same block changes and inventory";
  }
  public static final class Systems_Launch {
    public static final String Id = "launch";
    public static final String Driver = "Melty plus companion launcher";
    public static final String Hook = "native_scene";
    public static final String Policy = "BepInEx 5 host; fully automatic bundled Java companion setup; wait for idle native scene loading, keep the bridge running in the background";
    public static final String Test = "Melty installs package, sets up Java, launches both and links";
  }
  public static final class Systems_Water {
    public static final String Id = "water";
    public static final String Driver = "Minecraft Java plus ULTRAKILL";
    public static final String Hook = "native_water";
    public static final String Policy = "export merged real Java water volumes; create native layer-4 Water triggers after sandbox loads";
    public static final String Test = "enter water, sink slowly, swim/jump upward, dash and leave; placed water updates volume";
  }
  public static final class Systems_Commands {
    public static final String Id = "commands";
    public static final String Driver = "Minecraft Java";
    public static final String Hook = "java_input";
    public static final String Policy = "route T and slash to vanilla chat/command screen, then route text and keyboard while open";
    public static final String Test = "open commands using slash, enter /time query daytime, receive result without native pause";
  }
  public static final class Systems_NaturalSpawns {
    public static final String Id = "natural_spawns";
    public static final String Driver = "Minecraft Java plus ULTRAKILL";
    public static final String Hook = "native_enemies";
    public static final String Policy = "vanilla decides when and where hostile mobs may spawn; mapped hostile types become native enemies on Java terrain near the player; passive mobs unchanged";
    public static final String Test = "at night or in a dark cave, Filth/Strays/etc. appear instead of zombies/skeletons; cows, pigs and sheep still spawn";
  }
  public static final class Systems_WeaponMining {
    public static final String Id = "weapon_mining";
    public static final String Driver = "ULTRAKILL plus Minecraft Java";
    public static final String Hook = "native_guns";
    public static final String Policy = "player weapon hits on Java terrain accumulate block damage with cracks and break blocks with drops; railcannon and player explosions break a sphere; thrown chainsaw saws contacted blocks, wood fast";
    public static final String Test = "shoot dirt with the revolver (breaks, drops), railcannon a hillside (crater), throw the chainsaw at a tree (logs drop quickly)";
  }
  public static final class Whiplash_Arm {
    public static final String Id = "arm";
    public static final double HeavyMobVolumeBlocks = 2.0;
    public static final double MobPullBlocksPerSecond = 30.0;
    public static final boolean GrappleBlocks = true;
    public static final double AnchorOffsetBlocks = 0.5;
    public static final double AnchorRadiusBlocks = 0.5;
    public static final int HookLayer = 12;
    public static final String System = "weapons";
  }
  public static final class World_Fresh {
    public static final String Id = "fresh";
    public static final String Name = "UltraCraft";
    public static final String JavaVersion = "26.3";
    public static final int Seed = 742916305;
    public static final String Preset = "minecraft:normal";
    public static final String Mode = "survival";
    public static final String Difficulty = "easy";
    public static final boolean Structures = true;
    public static final double HostScale = 2.0;
    public static final String Hook = "java_world";
    public static final int ViewDistanceChunks = 8;
    public static final int SimulationDistanceChunks = 6;
    public static final int KeepRadiusChunks = 10;
    public static final String SeedMode = "random";
    public static final double GroundFriction = 2.2;
  }
}
