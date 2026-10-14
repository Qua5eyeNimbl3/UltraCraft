using HarmonyLib;
using UnityEngine;
using UltraCraft.Generated;
using UltraCraft.Render;
using Object=UnityEngine.Object;
namespace UltraCraft.World;
/// <summary>
/// ULTRAKILL's Whiplash (HookArm) in the Java world. Native rules stay: the hook catches enemies on
/// layers 11/12, yanks light ones to the player, pulls the player to heavy ones, and clinks off walls.
/// Two additions:
/// <para>Minecraft mobs: their hitboxes carry a layer-12 hook trigger (CrossCombat). Heavy ones need nothing
/// more; for light ones, HookArm pulls through the enemy's Rigidbody, which a Minecraft mob doesn't have, so
/// the pull is sent to Java as the mob's velocity instead.</para>
/// <para>Blocks: when the throw is about to hit Java terrain, a hook trigger with no enemy on it is placed just
/// in front of the face. HookArm catches it like any layer-12 object without an EnemyIdentifier, and pulls
/// the player there at its usual 60 units a second, landing as it does on a grapple point.</para>
/// </summary>
[HarmonyPatch(typeof(HookArm),"FixedUpdate")]
internal static class Whiplash {
    static readonly AccessTools.FieldRef<HookArm,Vector3> HookPoint=AccessTools.FieldRefAccess<HookArm,Vector3>("hookPoint");
    static readonly AccessTools.FieldRef<HookArm,Vector3> ThrowDirection=AccessTools.FieldRefAccess<HookArm,Vector3>("throwDirection");
    static readonly AccessTools.FieldRef<HookArm,Vector3> CaughtPoint=AccessTools.FieldRefAccess<HookArm,Vector3>("caughtPoint");
    static readonly AccessTools.FieldRef<HookArm,bool> LightTarget=AccessTools.FieldRefAccess<HookArm,bool>("lightTarget");
    static readonly AccessTools.FieldRef<HookArm,EnemyIdentifier> CaughtEid=AccessTools.FieldRefAccess<HookArm,EnemyIdentifier>("caughtEid");
    static readonly AccessTools.FieldRef<HookArm,Transform> CaughtTransform=AccessTools.FieldRefAccess<HookArm,Transform>("caughtTransform");
    static readonly AccessTools.FieldRef<HookArm,Collider> CaughtCollider=AccessTools.FieldRefAccess<HookArm,Collider>("caughtCollider");
    static readonly AccessTools.FieldRef<HookArm,LayerMask> EnviroMask=AccessTools.FieldRefAccess<HookArm,LayerMask>("enviroMask");
    static readonly AccessTools.FieldRef<HookArm,CapsuleCollider> PlayerCollider=AccessTools.FieldRefAccess<HookArm,CapsuleCollider>("playerCollider");
    // HookArm's own limits: 250 units a second thrown, grapple points need more than 5 units of rope.
    const float ThrowSpeed=250f,MinRope=5f,MaxPullSeconds=3f;
    static GameObject? anchor;
    static float pullStarted=-1;
    static int grapples;
    static bool logged; // once per grapple, not once per physics step while it is held

    static bool Prefix(HookArm __instance){
      if(__instance.state==HookState.Throwing&&Design.Whiplash_Arm.GrappleBlocks)PlaceAnchor(__instance);
      if(__instance.state==HookState.Pulling&&LightTarget(__instance)&&CrossCombat.TryMob(CaughtEid(__instance),out int id))return PullMob(__instance,id);
      pullStarted=-1;
      return true;
    }

    /// <summary>The anchor lives only while the hook holds it.</summary>
    static void Postfix(HookArm __instance){
      if(anchor==null||!anchor.activeSelf)return;
      bool held=(__instance.state==HookState.Caught||__instance.state==HookState.Pulling)&&CaughtTransform(__instance)==anchor.transform;
      if(!held){anchor.SetActive(false);logged=false;}
      else if(!logged){logged=true;if(grapples++<20)Plugin.Log.LogInfo("UltraCraft: Whiplash grappled a block");}
    }

    /// <summary>Same ray HookArm casts this step against the environment; on Java terrain, put the anchor in front of the face.</summary>
    static void PlaceAnchor(HookArm arm){
      Vector3 origin=HookPoint(arm),direction=ThrowDirection(arm);
      if(!Physics.Raycast(origin,direction,out var hit,ThrowSpeed*Time.fixedDeltaTime,EnviroMask(arm),QueryTriggerInteraction.Ignore)||!TerrainCollision.Owns(hit.collider))return;
      if(Vector3.Distance(arm.transform.position,hit.point)<=MinRope)return; // too close to swing: it clinks, as in ULTRAKILL
      if(anchor==null){
        anchor=new GameObject("UltraCraft grapple anchor"){layer=Design.Whiplash_Arm.HookLayer};
        Object.DontDestroyOnLoad(anchor);
        var sphere=anchor.AddComponent<SphereCollider>();sphere.isTrigger=true;sphere.radius=(float)Design.Whiplash_Arm.AnchorRadiusBlocks*Coords.Scale;
      }
      anchor.transform.position=hit.point+hit.normal*((float)Design.Whiplash_Arm.AnchorOffsetBlocks*Coords.Scale);
      anchor.SetActive(true);
      Physics.SyncTransforms(); // HookArm's sphere cast runs right after this, in the same step
    }

    /// <summary>HookArm's light-enemy pull (towards the camera at 60 units a second), applied to the Minecraft mob.</summary>
    static bool PullMob(HookArm arm,int id){
      var combat=CrossCombat.Current;Transform caught=CaughtTransform(arm);Collider collider=CaughtCollider(arm);
      if(combat==null||caught==null||collider==null){pullStarted=-1;arm.StopThrow(1f);return false;}
      if(pullStarted<0)pullStarted=Time.time;
      Vector3 point=caught.position+CaughtPoint(arm);HookPoint(arm)=point;
      Vector3 hand=arm.transform.position;
      bool blocked=Physics.Raycast(hand,point-hand,Vector3.Distance(hand,point),EnviroMask(arm),QueryTriggerInteraction.Ignore);
      var body=PlayerCollider(arm);Vector3 near=body!=null?body.ClosestPoint(point):hand;
      bool arrived=Vector3.Distance(near,collider.ClosestPoint(near))<.5f*Coords.Scale; // Java moves it a tick at a time: half a block counts
      if(blocked||arrived||Time.time-pullStarted>MaxPullSeconds){combat.Pull(id,Vector3.zero);pullStarted=-1;arm.StopThrow(1f);return false;}
      var camera=MonoSingleton<CameraController>.Instance;Vector3 target=camera!=null?camera.transform.position:hand;
      combat.Pull(id,(target-point).normalized*((float)Design.Whiplash_Arm.MobPullBlocksPerSecond*Coords.Scale));
      return false;
    }
}
