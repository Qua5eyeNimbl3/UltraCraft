using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UltraCraft.Generated;
using UltraCraft.Link;
using Object=UnityEngine.Object;
namespace UltraCraft.World;
/// <summary>
/// Damage across the two games.
/// <para>Minecraft mobs (from the world-entities table) get solid hitboxes on enemy layer 10 tagged
/// Body/Head with an EnemyIdentifierIdentifier, which is exactly what every native weapon looks for.
/// Their EnemyIdentifier sits on an inactive object, so it never registers, thinks or targets; the
/// DeliverDamage prefix below turns hits on it into Minecraft damage for that entity.</para>
/// <para>Native enemies near the player go into the actor table; Java mirrors them as invisible
/// hittable stand-ins (SkyCombat) and reports Minecraft melee/arrow hits back as EvHitActor.</para>
/// </summary>
internal sealed class CrossCombat {
    sealed class Mob {public GameObject Root=null!;public BoxCollider Body=null!,Head=null!;public EnemyIdentifier Eid=null!;public int Seen;}
    static readonly Dictionary<EnemyIdentifier,int> proxyIds=new();
    static readonly Dictionary<int,(float damage,Vector3 force)> pendingHits=new();
    readonly UltraLink link;
    readonly Dictionary<int,Mob> mobs=new();
    readonly List<UltraLink.WorldEntity> entities=new();
    readonly List<int> gone=new();
    readonly Dictionary<int,EnemyIdentifier> actors=new();
    readonly List<Proto.ActorRecord> records=new();
    readonly Dictionary<EnemyIdentifier,Collider[]> hitColliders=new();
    GameObject? root;
    int frame;
    public int MobCount=>mobs.Count;
    public CrossCombat(UltraLink link){this.link=link;}

    Collider[] playerColliders=System.Array.Empty<Collider>();
    public void Update(Vector3 feet,NewMovement player){
      frame++;
      if(playerColliders.Length==0||playerColliders[0]==null)playerColliders=player.GetComponentsInChildren<Collider>(true);
      UpdateMobs();
      FlushHits();
      PublishActors(feet);
    }

    // ---- Minecraft mobs as native targets ----
    void UpdateMobs(){
      if(!link.ReadWorldEntities(entities))return;
      if(root==null){root=new GameObject("UltraCraft Minecraft mob hitboxes");Object.DontDestroyOnLoad(root);}
      foreach(var e in entities){
        if(e.kind!=6||e.ext0<=0)continue;
        if(!mobs.TryGetValue((int)e.id,out var mob)){mob=Create((int)e.id);mobs[(int)e.id]=mob;}
        mob.Seen=frame;
        float width=Mathf.Max(e.scale,.3f)*Coords.Scale,height=e.ext0*Coords.Scale,eye=(e.ext2>0?e.ext2:e.ext0*.85f)*Coords.Scale;
        mob.Root.transform.position=Coords.ToUnity(e.x,e.y,e.z);
        mob.Body.center=new Vector3(0,height*.5f,0);mob.Body.size=new Vector3(width,height,width);
        float head=Mathf.Min(width,.6f*Coords.Scale);
        mob.Head.center=new Vector3(0,eye,0);mob.Head.size=Vector3.one*head;
      }
      gone.Clear();foreach(var pair in mobs)if(pair.Value.Seen!=frame)gone.Add(pair.Key);
      foreach(var id in gone){proxyIds.Remove(mobs[id].Eid);Object.Destroy(mobs[id].Root);mobs.Remove(id);}
    }
    Mob Create(int id){
      var go=new GameObject("Minecraft mob "+id);go.transform.SetParent(root!.transform,false);
      var holder=new GameObject("inactive identifier");holder.SetActive(false);holder.transform.SetParent(go.transform,false);
      var eid=holder.AddComponent<EnemyIdentifier>();eid.enemyType=EnemyType.Filth;eid.dontUnlockBestiary=true;
      proxyIds[eid]=id;
      return new Mob{Root=go,Eid=eid,Body=Part(go,"Body",eid),Head=Part(go,"Head",eid)};
    }
    /// <summary>
    /// Like a real enemy limb: a solid collider with its own kinematic body. The revolver's main
    /// sphere-cast ignores triggers and reads hit.transform, which is the attached rigidbody's object,
    /// so a trigger or a rigidbody on the parent made the shot pass through. The player walks through.
    /// </summary>
    BoxCollider Part(GameObject parent,string tag,EnemyIdentifier eid){
      var go=new GameObject(tag);go.transform.SetParent(parent.transform,false);go.layer=Design.Combat_Bridge.MobProxyLayer;go.tag=tag;
      var rb=go.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
      var box=go.AddComponent<BoxCollider>();
      foreach(var c in playerColliders)if(c!=null)Physics.IgnoreCollision(box,c);
      go.AddComponent<EnemyIdentifierIdentifier>().eid=eid;
      return box;
    }
    /// <summary>Harmony prefix target: true when the hit was on a Minecraft mob (and is forwarded).</summary>
    public static bool Intercept(EnemyIdentifier eid,GameObject target,Vector3 force,float multiplier,float critMultiplier){
      if(eid==null||!proxyIds.TryGetValue(eid,out int id))return false;
      float damage=multiplier*(1+critMultiplier)*(float)Design.Combat_Bridge.UltrakillToMcDamage;
      if(target!=null&&target.CompareTag("Head"))damage*=(float)Design.Combat_Bridge.HeadshotMultiplier;
      pendingHits.TryGetValue(id,out var hit);
      pendingHits[id]=(hit.damage+damage,hit.force+force);
      return true;
    }
    void FlushHits(){
      // Shotgun pellets and explosions land many hits in a frame: one combined hit per mob.
      foreach(var pair in pendingHits){
        if(pair.Value.damage<=0)continue;
        var f=pair.Value.force;float yaw=f.sqrMagnitude>1e-4f?Mathf.Atan2(-f.x,-f.z)*Mathf.Rad2Deg:0; // Minecraft yaw of the push (Z flipped)
        link.PushInput(Proto.InputType.HitEntity,0,pair.Key,Mathf.RoundToInt(pair.Value.damage*100),Mathf.RoundToInt(yaw*100));
      }
      if(pendingHits.Count>0)Plugin.Log.LogInfo($"UltraCraft: native weapons hit {pendingHits.Count} Minecraft mob(s)");
      pendingHits.Clear();
    }

    // ---- native enemies as Minecraft targets ----
    void PublishActors(Vector3 feet){
      if((frame&1)!=0)return;
      records.Clear();actors.Clear();
      var tracker=MonoSingleton<EnemyTracker>.Instance;if(tracker==null){link.WriteActors(records);return;}
      float range=Design.Combat_Bridge.ActorRangeBlocks*Coords.Scale;
      foreach(var eid in tracker.GetCurrentEnemies()){
        if(eid==null||eid.dead||!eid.gameObject.activeInHierarchy||proxyIds.ContainsKey(eid))continue;
        if(records.Count>=Design.Combat_Bridge.MaxActors)break;
        if(!hitColliders.TryGetValue(eid,out var cols)){cols=System.Array.FindAll(eid.GetComponentsInChildren<Collider>(),c=>c.gameObject.layer==10||c.gameObject.layer==11);hitColliders[eid]=cols;}
        Bounds b=default;bool any=false;
        foreach(var c in cols){if(c==null||!c.enabled)continue;if(!any){b=c.bounds;any=true;}else b.Encapsulate(c.bounds);}
        if(!any)continue;
        if((b.center-feet).sqrMagnitude>range*range)continue;
        Coords.ToMc(new Vector3(b.center.x,b.min.y,b.center.z),out double x,out double y,out double z);
        int id=eid.GetInstanceID();actors[id]=eid;
        records.Add(new Proto.ActorRecord{formId=id,flags=Proto.ActorHostile,x=(float)x,y=(float)y,z=(float)z,yaw=0,
          width=Mathf.Clamp(Mathf.Max(b.size.x,b.size.z)/Coords.Scale,.4f,4f),height=Mathf.Clamp(b.size.y/Coords.Scale,.4f,6f),healthFrac=1,level=1});
      }
      link.WriteActors(records);
      if(frame%600==0)hitColliders.Clear(); // drop destroyed enemies; live ones are re-cached
    }
    /// <summary>EvHitActor from Java: a Minecraft weapon hit a native enemy's stand-in.</summary>
    public void HitActor(in Proto.GuestEvent e){
      if(!actors.TryGetValue((int)e.formId,out var eid)||eid==null||eid.dead)return;
      GameObject target=eid.gameObject;
      foreach(var c in hitColliders.TryGetValue(eid,out var cols)?cols:System.Array.Empty<Collider>())if(c!=null&&c.CompareTag("Body")){target=c.gameObject;break;}
      var push=new Vector3(e.b,0,-e.c);push=push.sqrMagnitude>1e-4f?push.normalized:Vector3.zero;
      var force=(push+Vector3.up*.25f)*(2000f+6000f*Mathf.Max(0,e.d));
      eid.hitter="minecraft";
      eid.DeliverDamage(target,force,target.transform.position,e.a*(float)Design.Combat_Bridge.McToUltrakillDamage,false,0,null);
      Plugin.Log.LogInfo($"UltraCraft: Minecraft weapon hit {eid.enemyType} for {e.a:F1} ({e.a*Design.Combat_Bridge.McToUltrakillDamage:F2} native)");
    }
    /// <summary>Dev check of both damage paths without weapons: 1 native damage on the nearest Minecraft mob, and a 7-damage Minecraft hit on the nearest native enemy.</summary>
    public void SelfTest(Vector3 feet){
      Mob? nearest=null;foreach(var m in mobs.Values)if(nearest==null||(m.Root.transform.position-feet).sqrMagnitude<(nearest.Root.transform.position-feet).sqrMagnitude)nearest=m;
      if(nearest!=null){
        // Same query as the revolver's main hit test (RevolverBeam: SphereCast on layers 10/11, global trigger setting, hit.transform).
        var aim=nearest.Body.bounds.center;var from=aim+Vector3.up*2+(new Vector3(feet.x,aim.y,feet.z)-aim).normalized*8;
        bool cast=Physics.SphereCast(from,.1f,(aim-from).normalized,out var hit,40,(1<<10)|(1<<11));
        Plugin.Log.LogInfo(cast?$"UltraCraft test: revolver cast hit {hit.transform.name} layer={hit.transform.gameObject.layer} tag={hit.transform.tag} identifier={hit.transform.GetComponentInParent<EnemyIdentifierIdentifier>()!=null}":"UltraCraft test: revolver cast missed the mob hitbox");
        nearest.Eid.DeliverDamage(nearest.Body.gameObject,Vector3.forward*1000,nearest.Body.transform.position,1,false);FlushHits();Plugin.Log.LogInfo($"UltraCraft test: 1 native damage on Minecraft mob {proxyIds[nearest.Eid]}");}
      else Plugin.Log.LogInfo("UltraCraft test: no Minecraft mob hitboxes");
      foreach(var pair in actors){if(pair.Value==null||pair.Value.dead)continue;float before=pair.Value.health;HitActor(new Proto.GuestEvent{type=Proto.EvHitActor,formId=(uint)pair.Key,a=7,b=0,c=1,d=.4f});Plugin.Log.LogInfo($"UltraCraft test: native {pair.Value.enemyType} health {before:F2} -> {pair.Value.health:F2} dead={pair.Value.dead}");break;}
      Plugin.Log.LogInfo($"UltraCraft test: {actors.Count} native enemies published to Java");
    }
    public void Dispose(){if(root!=null)Object.Destroy(root);mobs.Clear();proxyIds.Clear();}
}

[HarmonyPatch(typeof(EnemyIdentifier),nameof(EnemyIdentifier.DeliverDamage))]
internal static class MinecraftMobDamage {
    static bool Prefix(EnemyIdentifier __instance,GameObject target,Vector3 force,float multiplier,float critMultiplier)=>!CrossCombat.Intercept(__instance,target,force,multiplier,critMultiplier);
}
