using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UltraCraft.Generated;
using UltraCraft.Link;
namespace UltraCraft.World;
/// <summary>
/// ULTRAKILL weapons mining Java terrain. Each hook below notices a player weapon touching the Java
/// block collision and tells Java which block took how much damage (or where a blast went off);
/// Java adds it up against the block's real hardness, shows cracks and breaks it with drops.
/// </summary>
internal static class WeaponMining {
    internal static UltraLink? Link;
    static Vector3 lastBlast;static float lastBlastTime;
    static int logs;
    /// <summary>The bridge's block colliders (TerrainCollision), not native level geometry.</summary>
    public static bool IsTerrain(Collider? c)=>c!=null&&c.gameObject.name=="UltraCraft collision";
    /// <summary>Damage the block just behind <paramref name="point"/> along <paramref name="into"/> (into the surface).</summary>
    public static void Damage(Vector3 point,Vector3 into,float power,bool saw=false){
      if(Link==null||power<=0)return;
      var inside=point+into.normalized*.2f;
      Coords.ToMc(inside,out double x,out double y,out double z);
      int code=Mathf.Clamp(Mathf.RoundToInt(power*100),1,0xFFF)|(saw?0x1000:0);
      Link.PushInput(Proto.InputType.BreakBlock,(ushort)code,Mathf.FloorToInt((float)x),Mathf.FloorToInt((float)y),Mathf.FloorToInt((float)z));
      if(logs++<10)Plugin.Log.LogInfo($"UltraCraft: weapon {(saw?"saw":"hit")} on block {Mathf.FloorToInt((float)x)} {Mathf.FloorToInt((float)y)} {Mathf.FloorToInt((float)z)} power {power:F2}");
    }
    public static void Blast(Vector3 center,float radiusBlocks){
      if(Link==null||radiusBlocks<.75f)return;
      // One explosion prefab can hold several Explosion parts: one blast per spot.
      if(Time.time-lastBlastTime<.2f&&(center-lastBlast).sqrMagnitude<4*Coords.Scale*Coords.Scale)return;
      lastBlast=center;lastBlastTime=Time.time;
      Coords.ToMc(center,out double x,out double y,out double z);
      Link.PushInput(Proto.InputType.Blast,(ushort)Mathf.RoundToInt(radiusBlocks*100),Mathf.RoundToInt((float)x*100),Mathf.RoundToInt((float)y*100),Mathf.RoundToInt((float)z*100));
      Plugin.Log.LogInfo($"UltraCraft: weapon blast radius {radiusBlocks:F1} blocks");
    }
}

/// <summary>Revolver bullets chip blocks; the railcannon blasts an explosive circle.</summary>
[HarmonyPatch(typeof(RevolverBeam),"HitSomething")]
internal static class BeamMining {
    static void Postfix(RevolverBeam __instance,PhysicsCastResult hit){
      if(!WeaponMining.IsTerrain(hit.collider))return;
      if(__instance.beamType==BeamType.Railgun)WeaponMining.Blast(hit.point,(float)Design.Mining_Guns.RailgunRadiusBlocks);
      else if(__instance.beamType==BeamType.Revolver)WeaponMining.Damage(hit.point,-hit.normal,(float)Design.Mining_Guns.RevolverPower);
    }
}

/// <summary>Shotgun pellets and other player projectiles.</summary>
[HarmonyPatch(typeof(Projectile),"Collided")]
internal static class PelletMining {
    static void Prefix(Projectile __instance,Collider other){
      if(!(__instance.friendly||__instance.playerBullet)||!WeaponMining.IsTerrain(other))return;
      var at=__instance.transform.position;var point=other.ClosestPoint(at);
      var into=point==at?__instance.transform.forward:point-at;
      WeaponMining.Damage(point,into,(float)Design.Mining_Guns.PelletPower);
    }
}

/// <summary>Nails chip blocks; sawblades saw them.</summary>
[HarmonyPatch(typeof(Nail),"OnCollisionEnter")]
internal static class NailMining {
    static void Prefix(Nail __instance,Collision other){
      if(__instance.enemy||!WeaponMining.IsTerrain(other.collider)||other.contactCount==0)return;
      var contact=other.GetContact(0);
      bool saw=__instance.sawblade||__instance.chainsaw;
      WeaponMining.Damage(contact.point,-contact.normal,(float)(saw?Design.Mining_Guns.SawPower:Design.Mining_Guns.NailPower),saw);
    }
}

/// <summary>The player's explosions (rockets, shotgun cores, malicious railcannon) blow craters.</summary>
[HarmonyPatch(typeof(Explosion),"Start")]
internal static class ExplosionMining {
    static void Postfix(Explosion __instance){
      if(__instance.enemy||__instance.harmless)return;
      var at=__instance.transform.position;
      if(!Physics.CheckSphere(at,__instance.maxSize*.5f+1,LayerMaskDefaults.Get(LMD.Environment),QueryTriggerInteraction.Ignore))return;
      WeaponMining.Blast(at,Mathf.Min(__instance.maxSize*(float)Design.Mining_Guns.ExplosionRadiusPerUnit,(float)Design.Mining_Guns.MaxExplosionRadiusBlocks));
    }
}

/// <summary>The thrown chainsaw saws whatever block it touches, wood fastest.</summary>
[HarmonyPatch(typeof(Chainsaw),"FixedUpdate")]
internal static class ChainsawMining {
    static readonly Dictionary<Chainsaw,float> next=new();
    static readonly Collider[] touching=new Collider[8];
    static void Postfix(Chainsaw __instance){
      if(next.TryGetValue(__instance,out float due)&&Time.time<due)return;
      next[__instance]=Time.time+.1f;
      if(next.Count>32)next.Clear(); // destroyed saws; live ones re-register next tick
      var at=__instance.transform.position;
      int n=Physics.OverlapSphereNonAlloc(at,.9f*Coords.Scale,touching,LayerMaskDefaults.Get(LMD.Environment),QueryTriggerInteraction.Ignore);
      for(int i=0,sawn=0;i<n&&sawn<2;i++){
        if(!WeaponMining.IsTerrain(touching[i]))continue;
        var point=touching[i].ClosestPoint(at);
        WeaponMining.Damage(point,point==at?Vector3.down:point-at,(float)Design.Mining_Guns.SawPower,saw:true);sawn++;
      }
    }
}
