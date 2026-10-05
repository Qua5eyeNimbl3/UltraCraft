using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using UltraCraft.Generated;
using Object=UnityEngine.Object;
namespace UltraCraft.World;
/// <summary>
/// Native enemies in the Java world. Every night runs waves (wave 1 at nightfall, the next after
/// each clear, none started after dawn), and natural spawns Java hands over instead of zombies,
/// skeletons and the other mapped monsters (paused while a night's waves are running).
/// </summary>
internal sealed class Waves {
    static readonly HashSet<EnemyType> Flying=new(){EnemyType.Drone,EnemyType.MaliciousFace,EnemyType.Virtue};
    readonly List<(EnemyType type,int count)[]> waves=new();
    readonly List<EnemyType?> replacements=new();
    readonly List<EnemyIdentifier> alive=new();
    readonly List<EnemyIdentifier> natural=new();
    bool running;float clearedAt=-1,lastReport;
    public int Wave {get;private set;}
    public bool Running=>running;
    public bool Night {get;private set;}
    public Action<Vector3>? Prepare;
    public int Alive{get{alive.RemoveAll(e=>e==null||e.dead);return alive.Count;}}
    public int Natural{get{natural.RemoveAll(e=>e==null||e.dead);return natural.Count;}}
    public float NextWaveIn=>running&&clearedAt>=0?Mathf.Max(0,(float)Design.Encounters_Waves.AutoNextDelaySeconds-(Time.time-clearedAt)):-1;
    public Waves(){
      foreach(var wave in Design.Encounters_Waves.Waves.Split('|')){
        var list=new List<(EnemyType,int)>();
        foreach(var part in wave.Split(',')){var kv=part.Split(':');if(Enum.TryParse<EnemyType>(kv[0].Trim(),out var type))list.Add((type,int.Parse(kv[1])));else Plugin.Log.LogWarning("UltraCraft waves: unknown enemy "+kv[0]);}
        waves.Add(list.ToArray());
      }
      foreach(var pair in Design.Natural_Night.Replacements.Split(',')){var kv=pair.Split(':');replacements.Add(Enum.TryParse<EnemyType>(kv[1].Trim(),out var t)?t:null);}
    }
    static List<SpawnableObject>? database;
    static List<SpawnableObject> Database()=>database is {Count:>0}?database:database=Resources.FindObjectsOfTypeAll<SpawnableObjectsDatabase>().SelectMany(d=>d.enemies??Array.Empty<SpawnableObject>()).Where(e=>e!=null&&e.gameObject!=null).ToList();
    static EnemyIdentifier? Instantiate(SpawnableObject prefab,Vector3 pos,Vector3 feet){
      var look=feet-pos;look.y=0;
      var go=Object.Instantiate(prefab.gameObject,pos+Vector3.up*prefab.spawnOffset,look.sqrMagnitude>.01f?Quaternion.LookRotation(look):Quaternion.identity);
      go.SetActive(true);
      return go.GetComponentInChildren<EnemyIdentifier>(true);
    }
    /// <summary>Spawns the next wave of the current night now.</summary>
    public void Spawn(Vector3 feet,Vector3 facing){
      var database=Database();
      if(database.Count==0){Plugin.Log.LogWarning("UltraCraft waves: native enemy registry is not ready");return;}
      running=true;clearedAt=-1;
      Prepare?.Invoke(feet); // walkers need the navmesh under them before they land
      var composition=waves[Mathf.Min(Wave,waves.Count-1)];int extra=Mathf.Max(0,Wave-(waves.Count-1));
      Wave++;int spawned=0;
      foreach(var (type,count) in composition){
        var prefab=database.FirstOrDefault(e=>e.enemyType==type);
        if(prefab==null){Plugin.Log.LogWarning($"UltraCraft waves: no native prefab for {type}");continue;}
        for(int i=0;i<count+extra;i++){var eid=Instantiate(prefab,Spot(feet,facing,Flying.Contains(type)),feet);if(eid!=null)alive.Add(eid);spawned++;}
      }
      Plugin.Log.LogInfo($"UltraCraft: wave {Wave} spawned {spawned} native enemies");
    }
    /// <summary>Java vetoed a natural Minecraft monster: spawn its native replacement where vanilla allowed it.</summary>
    public void SpawnNatural(int index,Vector3 at,Vector3 feet){
      if(running||index<0||index>=replacements.Count||replacements[index]==null||Natural>=Design.Natural_Night.MaxAlive)return;
      var type=replacements[index]!.Value;
      var prefab=Database().FirstOrDefault(e=>e.enemyType==type);if(prefab==null)return;
      if(!Physics.Raycast(at+Vector3.up*3,Vector3.down,out var hit,12,LayerMaskDefaults.Get(LMD.Environment),QueryTriggerInteraction.Ignore))return; // its terrain isn't here
      var pos=hit.point;
      if(Flying.Contains(type))pos+=Vector3.up*6;
      else if(NavMesh.SamplePosition(pos,out var nav,3,NavMesh.AllAreas))pos=nav.position;
      var eid=Instantiate(prefab,pos,feet);if(eid!=null)natural.Add(eid);
      Plugin.Log.LogInfo($"UltraCraft: natural {type} spawned ({Natural} native night enemies)");
    }
    /// <summary><paramref name="dayTime"/> is Java's overworld clock (0..24000), negative while unknown.</summary>
    public void Update(Vector3 feet,Vector3 facing,float dayTime){
      bool night=dayTime>=Design.Encounters_Waves.NightStartTicks&&dayTime<Design.Encounters_Waves.NightEndTicks;
      if(night&&!Night){Night=true;Wave=0;Plugin.Log.LogInfo("UltraCraft: night falls, waves begin");Spawn(feet,facing);}
      else if(!night&&Night){Night=false;running=false;clearedAt=-1;Plugin.Log.LogInfo($"UltraCraft: dawn after wave {Wave}");}
      if(running&&Wave>0&&Alive==0){
        if(clearedAt<0){clearedAt=Time.time;Plugin.Log.LogInfo($"UltraCraft: wave {Wave} cleared");}
        else if(Time.time-clearedAt>=Design.Encounters_Waves.AutoNextDelaySeconds)Spawn(feet,facing);
      }
      float despawn=Design.Natural_Night.DespawnDistanceBlocks*Coords.Scale;
      foreach(var eid in natural)if(eid!=null&&!eid.dead&&((eid.transform.position-feet).sqrMagnitude>despawn*despawn||eid.transform.position.y<-140))Object.Destroy(eid.gameObject);
      if(Time.time-lastReport>5&&(Alive>0||Natural>0)){lastReport=Time.time;Report(feet);}
      if(Time.time-lastWatch>=1){lastWatch=Time.time;Watch(feet,facing);}
    }
    readonly Dictionary<EnemyIdentifier,(Vector3 pos,float stuck)> watched=new();
    float lastWatch;
    /// <summary>
    /// Enemies that can't carry on: knocked off the navmesh, stranded far away without moving, or
    /// fallen through/under the terrain. After a few seconds they are put back on the navmesh (where
    /// they are, else near the player) so they keep coming and a night's wave can still be cleared.
    /// </summary>
    void Watch(Vector3 feet,Vector3 facing){
      foreach(var eid in alive.Concat(natural)){
        if(eid==null||eid.dead)continue;
        var agent=eid.GetComponent<NavMeshAgent>();
        var pos=eid.transform.position;
        watched.TryGetValue(eid,out var w);
        float distance=(pos-feet).magnitude/Coords.Scale;
        bool fell=pos.y<feet.y-40*Coords.Scale;
        bool offMesh=agent!=null&&agent.enabled&&!agent.isOnNavMesh;
        bool still=(pos-w.pos).magnitude<.5f*Coords.Scale;
        bool stranded=agent!=null&&still&&distance>20;
        float stuck=fell||offMesh||stranded?w.stuck+1:0;
        watched[eid]=(pos,stuck);
        if(stuck<(fell?1:4))continue;
        watched[eid]=(pos,0);
        Vector3 target;
        var filter=new NavMeshQueryFilter{agentTypeID=agent!=null?agent.agentTypeID:0,areaMask=NavMesh.AllAreas};
        if(!fell&&distance<=20&&agent!=null&&NavMesh.SamplePosition(pos,out var near,6*Coords.Scale,filter))target=near.position;
        else target=Spot(feet,facing,agent==null,agent!=null?agent.agentTypeID:-1);
        if(agent!=null&&agent.enabled&&agent.isOnNavMesh)agent.Warp(target);
        else{
          eid.transform.position=target;
          var rb=eid.GetComponent<Rigidbody>();if(rb!=null){rb.position=target;if(!rb.isKinematic)rb.velocity=Vector3.zero;}
          if(agent!=null&&agent.enabled)agent.Warp(target);
        }
        Plugin.Log.LogInfo($"UltraCraft: unstuck {eid.enemyType} ({(fell?"fell":offMesh?"off navmesh":"stranded")}, {distance:F0} blocks away)");
      }
      if(watched.Count>64){var gone=watched.Keys.Where(k=>k==null||k.dead).ToList();foreach(var k in gone)watched.Remove(k);}
    }
    /// <summary>Why an enemy is or isn't coming: grounded, agent on the navmesh, path and target.</summary>
    void Report(Vector3 feet){
      var parts=alive.Concat(natural).Where(e=>e!=null&&!e.dead).Take(6).Select(e=>{
        var agent=e.GetComponent<NavMeshAgent>();
        return $"{e.enemyType} d={(e.transform.position-feet).magnitude/Coords.Scale:F0}b y={e.transform.position.y/Coords.Scale:F0} grounded={(e.machine!=null?e.machine.grounded.ToString():"-")} agent={(agent==null?"none":agent.enabled?(agent.isOnNavMesh?(agent.hasPath?"path":"onmesh"):"offmesh"):"off")} target={(e.target!=null)}";
      });
      Plugin.Log.LogInfo("UltraCraft enemies: "+string.Join(" | ",parts));
    }
    /// <summary>A point on Java ground (and on the navmesh for walkers) in a ring around the player, preferring ahead.</summary>
    static Vector3 Spot(Vector3 feet,Vector3 facing,bool flying,int agentType=-1){
      float min=Design.Encounters_Waves.MinRadiusBlocks*Coords.Scale,max=Design.Encounters_Waves.MaxRadiusBlocks*Coords.Scale;
      facing.y=0;float baseAngle=facing.sqrMagnitude>.01f?Mathf.Atan2(facing.x,facing.z):0;
      for(int attempt=0;attempt<24;attempt++){
        float angle=baseAngle+UnityEngine.Random.Range(-1f,1f)*(attempt<12?Mathf.PI*.6f:Mathf.PI);
        var probe=feet+new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle))*UnityEngine.Random.Range(min,max);
        if(!Physics.Raycast(probe+Vector3.up*48,Vector3.down,out var hit,120,LayerMaskDefaults.Get(LMD.Environment),QueryTriggerInteraction.Ignore))continue;
        if(Mathf.Abs(hit.point.y-feet.y)>24)continue;
        if(flying)return hit.point+Vector3.up*8;
        if(agentType>=0?NavMesh.SamplePosition(hit.point,out var nav,3,new NavMeshQueryFilter{agentTypeID=agentType,areaMask=NavMesh.AllAreas}):NavMesh.SamplePosition(hit.point,out nav,3,NavMesh.AllAreas))return nav.position;
      }
      return feet+facing.normalized*min+Vector3.up*(flying?8:1);
    }
}
