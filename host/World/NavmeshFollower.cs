using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UltraCraft.Generated;
namespace UltraCraft.World;
/// <summary>
/// Native ground enemies walk on a navmesh, which Java terrain never had. While enemies exist this
/// keeps one built from the Java block collision around the player, for every native agent type,
/// rebuilt in the background as the player travels. Single-block steps are climbable. Agent types
/// build one after another rather than all at once, so the background voxelising never takes every
/// worker thread from the game's own rendering jobs in the same frame.
/// </summary>
internal sealed class NavmeshFollower {
    readonly Dictionary<int,(NavMeshData data,NavMeshDataInstance instance)> meshes=new();
    readonly List<NavMeshBuildSource> sources=new();
    readonly List<NavMeshBuildMarkup> markups=new();
    readonly List<AsyncOperation> pending=new();
    readonly Queue<NavMeshBuildSettings> queue=new();
    Bounds bounds;
    Vector3 center;bool built;float lastBuild,lastEnemyCheck;bool enemies;
    public bool Ready { get { Pump();return built&&pending.Count==0&&queue.Count==0; } }
    bool Busy { get { Pump();return pending.Count>0||queue.Count>0; } }
    // Re-centre once the player is half a radius from the middle (ULTRAKILL players cross a third of
    // it in about a second, which kept a rebuild running for the whole fight).
    bool Stale(Vector3 feet){var flat=feet-center;flat.y=0;return !built||flat.magnitude>Radius*.5f||Mathf.Abs(feet.y-center.y)>16||Time.unscaledTime-lastBuild>45;}
    float Radius=>Design.Encounters_Waves.NavmeshRadiusBlocks*Coords.Scale;
    public void ClearSceneNavmesh(){Dispose();NavMesh.RemoveAllNavMeshData();built=false;}
    public bool Prepare(Vector3 feet){
      if(Busy)return false;
      if(Stale(feet))Build(feet,false);
      return Ready;
    }
    public void Update(Vector3 feet,bool waveAlive){
      if(Time.unscaledTime-lastEnemyCheck>2){lastEnemyCheck=Time.unscaledTime;var tracker=MonoSingleton<EnemyTracker>.Instance;enemies=tracker!=null&&tracker.GetCurrentEnemies().Count>0;}
      if(!(waveAlive||enemies))return;
      if(Busy)return;
      if(Stale(feet))Build(feet,false);
    }
    /// <summary>Build (synchronously when enemies are about to be placed on it).</summary>
    public void Build(Vector3 feet,bool now){
      // Keep sources and native data stable while a worker owns them.
      if(Busy)return;
      center=feet;built=true;lastBuild=Time.unscaledTime;
      bounds=new Bounds(feet,new Vector3(Radius*2,Radius,Radius*2));
      sources.Clear();
      NavMeshBuilder.CollectSources(bounds,LayerMaskDefaults.Get(LMD.Environment),NavMeshCollectGeometry.PhysicsColliders,0,markups,sources);
      for(int i=0;i<NavMesh.GetSettingsCount();i++){
        var settings=NavMesh.GetSettingsByIndex(i);
        settings.agentClimb=Mathf.Max(settings.agentClimb,(float)Design.Encounters_Waves.NavmeshClimbBlocks*Coords.Scale);
        settings.overrideVoxelSize=true;settings.voxelSize=Mathf.Clamp(settings.agentRadius/3f,.25f,.5f);
        queue.Enqueue(settings);
      }
      Pump();
    }
    /// <summary>Starts the next agent type's build once the previous one has finished.</summary>
    void Pump(){
      pending.RemoveAll(p=>p.isDone);
      if(pending.Count>0||queue.Count==0)return;
      var settings=queue.Dequeue();
      if(!meshes.TryGetValue(settings.agentTypeID,out var mesh)){var data=new NavMeshData(settings.agentTypeID);mesh=(data,NavMesh.AddNavMeshData(data));meshes[settings.agentTypeID]=mesh;}
      pending.Add(NavMeshBuilder.UpdateNavMeshDataAsync(mesh.data,settings,sources,bounds));
    }
    public void Dispose(){foreach(var m in meshes.Values){NavMeshBuilder.Cancel(m.data);m.instance.Remove();Object.Destroy(m.data);}pending.Clear();queue.Clear();meshes.Clear();built=false;}
}
