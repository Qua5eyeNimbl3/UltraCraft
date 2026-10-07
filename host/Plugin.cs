using System;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UltraCraft.Generated;
using UltraCraft.Input;
using UltraCraft.Link;
using UltraCraft.Render;
using UltraCraft.World;
using Object=UnityEngine.Object;
namespace UltraCraft;
[BepInPlugin("local.ultracraft.bridge","UltraCraft","0.1.0")]
[DefaultExecutionOrder(1000)]
public sealed class Plugin:BaseUnityPlugin {
    internal static Plugin? Instance;
    internal bool JavaScreenOpen=>input!=null&&input.ScreenOpen;
    Harmony? patches;
    internal static ManualLogSource Log=null!;
    UltraLink link=null!;
    RenderRing render=null!;
    Overlay overlay=null!;
    InputRouter input=null!;
    Proto.GuestState guest;
    bool adopted,requestedScene,hadScreen;
    float started,lastLog;
    int framesSinceLog;
    NewMovement? player;
    Vector3 spawn,adoptCandidate,lastSafe,prevFeet;
    float adoptWaitStart=-1,respawnSince=-1;
    float adoptSince;
    ShopZone? terminal;
    GameObject? heldWeapon;
    string devFile="";
    long lastCommand;
    readonly NavmeshFollower navmesh=new();
    readonly Waves waves=new();
    CrossCombat combat=null!;
    Companion companion=null!;
    float javaDayTime=-1;
    bool focusedBefore;
    readonly JavaScreenGate screens=new();
    readonly WorldLighting lighting=new();
    void Awake(){
      gameObject.hideFlags=HideFlags.HideAndDontSave;
      Object.DontDestroyOnLoad(gameObject);
      Application.runInBackground=true;
      Log=Logger;started=Time.realtimeSinceStartup;
      Instance=this;patches=new Harmony("local.ultracraft.bridge");patches.PatchAll(typeof(Plugin).Assembly);Logger.LogInfo("UltraCraft hooks: "+string.Join(", ",patches.GetPatchedMethods().Select(m=>m.DeclaringType?.Name+"."+m.Name)));
      if(!Proto.SelfCheck(Logger)||!Rings.SelfTest(Logger)){enabled=false;return;}
      link=new UltraLink(Logger);if(!link.Create(Proto.MappingName)){enabled=false;return;}
      render=new RenderRing(link,Config);overlay=new Overlay(link);input=new InputRouter(link,Config);combat=new CrossCombat(link);companion=new Companion();WeaponMining.Link=link;waves.Prepare=feet=>navmesh.Build(feet,true);
      devFile=Path.Combine(Paths.PluginPath,"UltraCraft","dev-command.txt");
      // Only commands written while this game runs count: a leftover file must never act on a new session.
      if(File.Exists(devFile))lastCommand=File.GetLastWriteTimeUtc(devFile).Ticks;
      Logger.LogInfo("UltraCraft: Java-only survival bridge started; native ULTRAKILL movement owns the player.");
    }
    void LateUpdate(){
      link.Heartbeat();
      if(link.GuestAlive&&link.ReadGuestState(out var fresh))guest=fresh;
      if(!requestedScene&&Time.realtimeSinceStartup-started>4&&SceneHelper.PendingScene==null&&SceneHelper.CurrentScene!=null&&IsFirstMenuScene(SceneHelper.CurrentScene)){requestedScene=true;Logger.LogInfo("UltraCraft: loading native sandbox from "+SceneHelper.CurrentScene);SceneHelper.LoadScene(Design.Launch_Main.HostScene);}
      // Singletons: FindObjectOfType scans every object, and the Java world adds thousands.
      var nm=MonoSingleton<NewMovement>.Instance;if(nm!=null&&!nm.isActiveAndEnabled&&!nm.gameObject.activeInHierarchy)nm=null;
      if(nm!=player){player=nm;adopted=false;terminal=null;}
      bool world=link.GuestAlive&&(guest.flags&(uint)Proto.GuestFlags.InWorld)!=0;
      if(!adopted)companion.Update(link.GuestAlive,world);
      while(link.PopEvent(out var ev)){
        if(ev.type==Proto.EvPlayerDied&&adopted&&respawnSince<0){
          // Minecraft killed its player (void, lava, a mob...): let its respawn happen, then follow it.
          respawnSince=Time.realtimeSinceStartup;Logger.LogInfo("UltraCraft: Java player died; following Minecraft's respawn");
        }
        if(!adopted||nm==null)continue;
        if(ev.type==Proto.EvHitActor)combat.HitActor(ev);
        else if(ev.type==Proto.EvTime){javaDayTime=ev.a;lighting.SetTime(ev.a);}
        else if(ev.type==Proto.EvNativeSpawn)waves.SpawnNatural((int)ev.formId,Coords.ToUnity(ev.a,ev.b,ev.c),CurrentFeet());
      }
      if(world){overlay.Update();render.Update(nm!=null,CurrentFeet(),guest.yaw,true,input.Building&&!input.ScreenOpen,adopted);}
      if(respawnSince>=0&&Time.realtimeSinceStartup-respawnSince>2){respawnSince=-1;adopted=false;adoptWaitStart=-1;} // place the native player at the respawn
      if(nm!=null&&world&&!adopted&&SceneHelper.CurrentScene==Design.Launch_Main.HostScene&&SceneHelper.PendingScene==null){
        var target=Coords.ToUnity(guest.x,guest.y,guest.z);
        if(adoptWaitStart<0)adoptWaitStart=Time.realtimeSinceStartup;
        float waited=Time.realtimeSinceStartup-adoptWaitStart;
        // The saved Java position can be mid-jump or mid-fall: land on the ground below it, but only once
        // the terrain around it has arrived (a freshly started Minecraft sends it over several seconds;
        // placing the player earlier dropped them through missing ground into the void) and the ground
        // has stopped changing for a moment.
        bool ground=Physics.Raycast(target+Vector3.up*4,Vector3.down,out var hit,800,LayerMaskDefaults.Get(LMD.Environment),QueryTriggerInteraction.Ignore);
        bool near=ground&&target.y-hit.point.y<=48*Coords.Scale;
        bool loaded=render.SectionCount>=200||waited>45;
        if(ground&&(hit.point-adoptCandidate).sqrMagnitude>.01f){adoptCandidate=hit.point;adoptSince=Time.realtimeSinceStartup;}
        if(ground&&loaded&&(near||waited>45)&&Time.realtimeSinceStartup-adoptSince>1.5f){
          if(hit.point.y<target.y-1)target=hit.point;
          spawn=target;
          float center=nm.transform.position.y-nm.playerCollider.bounds.min.y;
          nm.rb.position=target+Vector3.up*(center+.1f);nm.rb.velocity=Vector3.zero;adopted=true;lastSafe=prevFeet=target;
          screens.Adopt(nm);hadScreen=false;lighting.Install();
          Logger.LogInfo($"UltraCraft: native player adopted Java spawn {guest.x:F2} {guest.y:F2} {guest.z:F2}; sections {render.SectionCount}");
          PlaceTerminal();SandboxHider.Hide(nm.gameObject.scene,terminal!=null?terminal.transform:null);navmesh.ClearSceneNavmesh();
          RenderSettings.skybox=null;
          if(nm.cc!=null){nm.cc.enabled=true;nm.cc.activated=true;}
          Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;
        }
      }
      if(adopted&&world&&nm!=null){
        GuardAgainstVoid(nm);
        input.Update(guest);bool screen=input.ScreenOpen;
        screens.Sync(screen);
        if(screen!=hadScreen){
          hadScreen=screen;
          Cursor.lockState=screen?CursorLockMode.None:CursorLockMode.Locked;Cursor.visible=false;
        }
        if(Application.isFocused&&!focusedBefore&&!screen&&!GameStateManager.Instance.CameraLocked){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;if(nm.cc!=null){nm.cc.enabled=true;nm.cc.activated=true;}}
        focusedBefore=Application.isFocused;
        var gc=MonoSingleton<GunControl>.Instance;
        if(gc!=null){bool guns=!input.Building&&!screen;gc.activated=guns;if(gc.currentWeapon!=null){heldWeapon=gc.currentWeapon;heldWeapon.SetActive(guns);}}
        if(terminal!=null&&Keyboard.current!=null&&Keyboard.current.gKey.wasPressedThisFrame&&Vector3.Distance(nm.transform.position,terminal.transform.position)<12){input.Building=false;terminal.TurnOn();Logger.LogInfo("UltraCraft: native gun terminal opened");}
        if(!screen&&Keyboard.current!=null){if(Keyboard.current[Enum.Parse<Key>(Design.Controls_Camp.Key)].wasPressedThisFrame)Travel(nm,spawn);}
        var facing=nm.cc!=null&&nm.cc.cam!=null?nm.cc.cam.transform.forward:nm.transform.forward;
        waves.Update(CurrentFeet(),facing,javaDayTime);
        lighting.Update(nm.cc!=null?nm.cc.cam:null,render.Atlas);
        navmesh.Update(CurrentFeet(),waves.Alive+waves.Natural>0);
        combat.Update(CurrentFeet(),nm);
        DevCommands(nm);
      }
      Publish(nm);
      framesSinceLog++;
      if(Time.realtimeSinceStartup-lastLog>10){float fps=framesSinceLog/(Time.realtimeSinceStartup-lastLog);framesSinceLog=0;lastLog=Time.realtimeSinceStartup;Logger.LogInfo($"UltraCraft status: fps={fps:F0}, mobHitboxes={combat?.MobCount}, waveAlive={waves.Alive}, nightEnemies={waves.Natural}, scene={SceneHelper.CurrentScene}, Java={link.GuestAlive}, flags=0x{guest.flags:X}, sections={render.SectionCount}, collisionSections={render.CollisionSectionCount}, waterSections={render.WaterSectionCount}, grounded={nm?.gc?.onGround}, movementActive={nm?.activated}, movementEnabled={nm?.enabled}, kinematic={nm?.rb?.isKinematic}, inputLocked={GameStateManager.Instance?.PlayerInputLocked}, mouseLocked={Cursor.lockState}, cameraActive={nm?.cc?.activated}, adopted={adopted}, position=({guest.x:F2},{guest.y:F2},{guest.z:F2})");}
    }
    /// <summary>
    /// Falling out of the Java world (below Minecraft's bottom, -64) or being moved far in one frame by
    /// ULTRAKILL itself (its death/checkpoint respawn puts the player back in the hidden sandbox map,
    /// over nothing) returns the native player to the last ground they stood on, instead of the void.
    /// </summary>
    void GuardAgainstVoid(NewMovement nm){
      var feet=CurrentFeet();
      bool fell=feet.y<-70*Coords.Scale;
      bool jumped=(feet-prevFeet).magnitude>150;
      if(fell||jumped){
        float center=nm.transform.position.y-nm.playerCollider.bounds.min.y;
        nm.rb.position=lastSafe+Vector3.up*(center+.15f);nm.rb.velocity=Vector3.zero;
        Logger.LogWarning($"UltraCraft: returned the player to solid ground ({(fell?"fell out of the world":"teleported away by the game")})");
        feet=lastSafe;
      }
      else if(nm.gc!=null&&nm.gc.onGround)lastSafe=feet;
      prevFeet=feet;
    }
    Vector3 CurrentFeet(){if(player==null)return Vector3.zero;return new Vector3(player.transform.position.x,player.playerCollider.bounds.min.y,player.transform.position.z);}
    void Publish(NewMovement? nm){
      var p=new Proto.HostState{flags=(uint)(nm!=null?Proto.HostFlags.InGame:Proto.HostFlags.Loading),worldId=1,collisionEpoch=1,teleportSeq=adopted&&respawnSince<0?1u:0u,viewportW=(uint)Mathf.Max(320,Screen.width*(float)Design.Render_Native.OverlayScale),viewportH=(uint)Mathf.Max(180,Screen.height*(float)Design.Render_Native.OverlayScale),gameHour=12};
      if(nm!=null&&nm.gc!=null&&nm.gc.onGround)p.flags|=(uint)Proto.HostFlags.Grounded;
      if(adopted&&nm!=null){Coords.ToMc(CurrentFeet(),out p.posX,out p.posY,out p.posZ);var camera=nm.cc!=null?nm.cc.cam:null;if(camera!=null){var r=camera.transform.eulerAngles;p.yaw=Coords.McYawFromUnity(r.y);p.pitch=Mathf.DeltaAngle(0,r.x);}}
      link.WriteHostState(p);
    }
    void PlaceTerminal(){
      terminal=Object.FindObjectsOfType<ShopZone>().FirstOrDefault();
      if(terminal==null){Logger.LogWarning("UltraCraft: no native gun terminal in this scene; terminal acceptance test is pending.");return;}
      Logger.LogInfo("UltraCraft terminal hierarchy: "+terminal.name+" parent="+(terminal.transform.parent?.name??"none"));
      Transform machine=terminal.transform.parent!=null?terminal.transform.parent:terminal.transform;
      machine.position+=spawn+new Vector3(-7,2,-7)-terminal.transform.position;
      Logger.LogInfo("UltraCraft: moved actual native gun terminal near Java spawn");
    }
    Vector3 Surface(Vector3 position){if(Physics.Raycast(new Vector3(position.x,640,position.z),Vector3.down,out var hit,900,LayerMaskDefaults.Get(LMD.Environment),QueryTriggerInteraction.Ignore))position.y=hit.point.y;return position;}
    void Travel(NewMovement nm,Vector3 destination){destination=Surface(destination);float center=nm.transform.position.y-nm.playerCollider.bounds.min.y;nm.rb.position=destination+Vector3.up*(center+.15f);lastSafe=prevFeet=destination;nm.rb.velocity=Vector3.zero;}
    void SpawnWave(NewMovement nm){
      var feet=CurrentFeet();navmesh.Build(feet,true);
      var facing=nm.cc!=null&&nm.cc.cam!=null?nm.cc.cam.transform.forward:nm.transform.forward;
      waves.Spawn(feet,facing);input.Building=false;
    }
    void DevCommands(NewMovement nm){
      if(!File.Exists(devFile))return;var stamp=File.GetLastWriteTimeUtc(devFile).Ticks;if(stamp==lastCommand)return;lastCommand=stamp;string cmd=File.ReadAllText(devFile).Trim();
      if(cmd=="dump"){Logger.LogInfo("UltraCraft native guns="+string.Join(",",Object.FindObjectsOfType<GunControl>().SelectMany(g=>g.allWeapons).Select(g=>g.name)));Logger.LogInfo("UltraCraft loaded shaders="+string.Join(",",Resources.FindObjectsOfTypeAll<Shader>().Select(s=>s.name).Take(50)));}
      else if(cmd=="quit")Application.Quit();
      else if(cmd=="audit")Diagnostics.Audit(nm);
      else if(cmd.StartsWith("diagnostics ")){StartCoroutine(Diagnostics.Run(render.Atlas,cmd.Substring(12),nm,render));}
      else if(cmd.StartsWith("capture ")){var path=cmd.Substring(8);Directory.CreateDirectory(Path.GetDirectoryName(path));ScreenCapture.CaptureScreenshot(path);}
      else if(cmd=="inventory"){link.PushInput(Proto.InputType.Key,8,1);link.PushInput(Proto.InputType.Key,8,0);}
      else if(cmd=="combat")input.Building=false;
      else if(cmd=="build")input.Building=true;
      else if(cmd=="wave")SpawnWave(nm);
      else if(cmd.StartsWith("preview-time ")){var t=cmd.Split(' ');lighting.Preview(float.Parse(t[1]),t.Length>2?float.Parse(t[2]):5);}
      else if(cmd=="test-hits")combat.SelfTest(CurrentFeet());
      else if(cmd=="scene")Logger.LogInfo("UltraCraft scene roots: "+SandboxHider.Describe(nm.gameObject.scene));
      else if(cmd.StartsWith("mouse ")){var t=cmd.Split(' ');link.PushInput(Proto.InputType.MouseButton,ushort.Parse(t[1]),int.Parse(t[2]));}
      Logger.LogInfo("UltraCraft dev command: "+cmd);
    }
    /// <summary>
    /// The first scene ULTRAKILL shows after its intro: the main menu, or the tutorial for players who
    /// haven't finished it (IntroViolenceScreen sends them there), so new players get UltraCraft too.
    /// </summary>
    static bool IsFirstMenuScene(string scene)=>scene.Length>0&&scene!="Intro"&&!scene.StartsWith("Bootstrap",StringComparison.OrdinalIgnoreCase)&&scene!=Design.Launch_Main.HostScene;
    void OnGUI(){if(!adopted){var waiting=companion!=null?companion.Status:"";GUI.Box(new Rect(20,20,640,string.IsNullOrEmpty(waiting)?70:110),"UltraCraft\nPreparing your Minecraft Java world…"+(string.IsNullOrEmpty(waiting)?"":"\n"+waiting));return;}
      if(input.Building||input.ScreenOpen)overlay.Draw(input.ScreenOpen,input.Cursor);
      GUI.Box(new Rect(12,12,560,76),input.Building?"BUILD MODE  •  V: guns  •  E: inventory  •  /: commands\nH: return to camp":"COMBAT MODE  •  V: mining/building  •  E: inventory\nH: return to camp");
      int alive=waves.Alive;
      string status;
      if(javaDayTime<0)status="";
      else if(waves.Night)status=alive>0?$"BLOOD NIGHT • wave {waves.Wave} • {alive} enemies left":$"BLOOD NIGHT • wave {waves.Wave} cleared • next wave in {waves.NextWaveIn:F0}s";
      else{float ticks=Mathf.Repeat(Design.Encounters_Waves.NightStartTicks-javaDayTime,24000);int secs=Mathf.CeilToInt(ticks/20f);status=alive>0?$"Day • {alive} enemies left • night in {secs/60}:{secs%60:00}":$"Day • night falls in {secs/60}:{secs%60:00}";}
      GUI.Label(new Rect(24,62,530,24),status);
    }
    void OnDestroy(){Logger.LogInfo("UltraCraft runtime object destroyed");patches?.UnpatchSelf();Instance=null;screens.Release();lighting.Dispose();if(heldWeapon!=null)heldWeapon.SetActive(true);navmesh.Dispose();combat?.Dispose();link?.Close();}
}
[HarmonyPatch(typeof(OptionsManager),nameof(OptionsManager.Pause))]
internal static class JavaScreenPauseGuard {
    static bool Prefix()=>Plugin.Instance==null||!Plugin.Instance.JavaScreenOpen;
}
