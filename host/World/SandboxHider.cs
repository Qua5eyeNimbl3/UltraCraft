using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace UltraCraft.World;
/// <summary>
/// The native sandbox map is only a host for the player, guns and terminal: once the player stands
/// in the Java world its geometry, outlines and collision (including out-of-bounds triggers) are
/// switched off. Anything holding the player, guns, terminal, enemies, UI, cameras or physics props
/// is left alone, so native systems keep working.
/// </summary>
internal static class SandboxHider {
    static readonly System.Type[] Keepers={typeof(NewMovement),typeof(GunControl),typeof(EnemyIdentifier),typeof(Canvas),typeof(Camera),typeof(Rigidbody),typeof(CameraController)};
    // Runtime systems, not map: GameController pools gore and projectiles, so its renderers must stay.
    static readonly HashSet<string> Systems=new(){"GameController","PlatformerController(Clone)","Player","Canvas","EventSystem(Clone)","CheatBinds","ComponentsDatabase","CheckPointsController","StatsManager","Navigation Manager","Level Info"};
    /// <summary><paramref name="terminal"/> is the one gun terminal kept (moved to camp); the sandbox's other shops go with the map.</summary>
    public static void Hide(Scene scene,Transform? terminal){
      int renderers=0,colliders=0;var hidden=new List<string>();
      foreach(var root in scene.GetRootGameObjects()){
        if(Systems.Contains(root.name)||(terminal!=null&&terminal.root==root.transform))continue;
        int before=renderers+colliders;Visit(root.transform,ref renderers,ref colliders);
        if(renderers+colliders>before)hidden.Add(root.name);
      }
      Plugin.Log.LogInfo($"UltraCraft: hid native sandbox map ({renderers} renderers, {colliders} colliders) under: {string.Join(", ",hidden.Take(40))}");
    }
    public static string Describe(Scene scene)=>string.Join("; ",scene.GetRootGameObjects().Select(r=>$"{r.name}[active={r.activeInHierarchy},layer={r.layer},renderers={r.GetComponentsInChildren<Renderer>(true).Count(x=>x.enabled)},colliders={r.GetComponentsInChildren<Collider>(true).Count(x=>x.enabled)}]"));
    static bool IsKeeper(GameObject go){foreach(var t in Keepers)if(go.GetComponent(t)!=null)return true;return false;}
    static bool HasKeeperBelow(Transform t){foreach(var k in Keepers)if(t.GetComponentInChildren(k,true)!=null)return true;return false;}
    static void Visit(Transform t,ref int renderers,ref int colliders){
      if(IsKeeper(t.gameObject))return;
      if(!HasKeeperBelow(t)){
        foreach(var r in t.GetComponentsInChildren<Renderer>(true))if(r.enabled){r.enabled=false;renderers++;}
        foreach(var c in t.GetComponentsInChildren<Collider>(true))if(c.enabled){c.enabled=false;colliders++;}
        foreach(var terrain in t.GetComponentsInChildren<Terrain>(true))terrain.enabled=false;
        return;
      }
      foreach(var r in t.GetComponents<Renderer>())if(r.enabled){r.enabled=false;renderers++;}
      foreach(var c in t.GetComponents<Collider>())if(c.enabled){c.enabled=false;colliders++;}
      for(int i=0;i<t.childCount;i++)Visit(t.GetChild(i),ref renderers,ref colliders);
    }
}
