using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UltraCraft.Generated;
namespace UltraCraft.World;
/// <summary>
/// Native ground enemies walk on a navmesh, which Java terrain never had. While enemies exist this
/// keeps one built from the Java block collision around the player, for every native agent type,
/// rebuilt in the background as the player travels. Single-block steps are climbable.
/// </summary>
internal sealed class NavmeshFollower {
    readonly Dictionary<int,(NavMeshData data,NavMeshDataInstance instance)> meshes=new();
    readonly List<NavMeshBuildSource> sources=new();
    readonly List<NavMeshBuildMarkup> markups=new();
    readonly List<AsyncOperation> pending=new();
    Vector3 center;bool built;float lastBuild,lastEnemyCheck;bool enemies;
    float Radius=>Design.Encounters_Waves.NavmeshRadiusBlocks*Coords.Scale;
    public void ClearSceneNavmesh(){NavMesh.RemoveAllNavMeshData();meshes.Clear();built=false;}
    public void Update(Vector3 feet,bool waveAlive){
      if(Time.unscaledTime-lastEnemyCheck>2){lastEnemyCheck=Time.unscaledTime;var tracker=MonoSingleton<EnemyTracker>.Instance;enemies=tracker!=null&&tracker.GetCurrentEnemies().Count>0;}
      if(!(waveAlive||enemies))return;
      pending.RemoveAll(p=>p.isDone);if(pending.Count>0)return;
      var flat=feet-center;flat.y=0;
      if(!built||flat.magnitude>Radius*.35f||Mathf.Abs(feet.y-center.y)>16||Time.unscaledTime-lastBuild>45)Build(feet,false);
    }
    /// <summary>Build (synchronously when enemies are about to be placed on it).</summary>
    public void Build(Vector3 feet,bool now){
      center=feet;built=true;lastBuild=Time.unscaledTime;
      var bounds=new Bounds(feet,new Vector3(Radius*2,Radius,Radius*2));
      sources.Clear();
      NavMeshBuilder.CollectSources(bounds,LayerMaskDefaults.Get(LMD.Environment),NavMeshCollectGeometry.PhysicsColliders,0,markups,sources);
      for(int i=0;i<NavMesh.GetSettingsCount();i++){
        var settings=NavMesh.GetSettingsByIndex(i);
        settings.agentClimb=Mathf.Max(settings.agentClimb,(float)Design.Encounters_Waves.NavmeshClimbBlocks*Coords.Scale);
        settings.overrideVoxelSize=true;settings.voxelSize=Mathf.Clamp(settings.agentRadius/3f,.25f,.5f);
        if(!meshes.TryGetValue(settings.agentTypeID,out var mesh)){var data=new NavMeshData(settings.agentTypeID);mesh=(data,NavMesh.AddNavMeshData(data));meshes[settings.agentTypeID]=mesh;}
        if(now)NavMeshBuilder.UpdateNavMeshData(mesh.data,settings,sources,bounds);
        else pending.Add(NavMeshBuilder.UpdateNavMeshDataAsync(mesh.data,settings,sources,bounds));
      }
    }
    public void Dispose(){foreach(var m in meshes.Values)m.instance.Remove();meshes.Clear();}
}
