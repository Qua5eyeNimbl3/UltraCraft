using System.Collections;
using UnityEngine;
namespace UltraCraft.World;
/// <summary>
/// Dev check for "the ground feels like ice": a stand-in with the player's capsule, physics material
/// and walking speed (NewMovement's grounded rule: velocity eases a quarter of the way to the target
/// each physics step) runs straight across the Java terrain for a few seconds. It counts steps where
/// it lost the ground or was kicked upwards, which put ULTRAKILL's movement into air control.
/// The real player and input are never touched.
/// </summary>
internal static class GroundProbe {
    public static IEnumerator Run(NewMovement player,Vector3 feet){
      var col=player.playerCollider as CapsuleCollider;
      if(col==null){Plugin.Log.LogInfo("UltraCraft probe: player collider isn't a capsule");yield break;}
      var go=new GameObject("UltraCraft ground probe"){layer=2};
      var capsule=go.AddComponent<CapsuleCollider>();capsule.radius=col.radius;capsule.height=col.height;capsule.direction=col.direction;capsule.sharedMaterial=col.sharedMaterial;
      var rb=go.AddComponent<Rigidbody>();rb.mass=player.rb.mass;rb.drag=player.rb.drag;rb.interpolation=RigidbodyInterpolation.None;rb.collisionDetectionMode=player.rb.collisionDetectionMode;rb.constraints=RigidbodyConstraints.FreezeRotation;
      foreach(var c in player.GetComponentsInChildren<Collider>(true))Physics.IgnoreCollision(capsule,c);
      var dir=player.cc!=null&&player.cc.cam!=null?player.cc.cam.transform.forward:Vector3.forward;dir.y=0;dir.Normalize();
      go.transform.position=feet+Vector3.up*(col.height*.5f+.2f)-dir*2;
      float speed=player.walkSpeed*Time.fixedDeltaTime*2.75f;
      int steps=0,airborne=0,kicks=0;float maxUp=0;
      yield return new WaitForFixedUpdate();
      for(float t=0;t<6;t+=Time.fixedDeltaTime){
        yield return new WaitForFixedUpdate();
        steps++;
        var bottom=go.transform.position+Vector3.down*(col.height*.5f-col.radius);
        bool grounded=Physics.SphereCast(bottom+Vector3.up*.1f,col.radius*.9f,Vector3.down,out _,.35f,LayerMaskDefaults.Get(LMD.Environment),QueryTriggerInteraction.Ignore);
        if(!grounded)airborne++;
        if(rb.velocity.y>1.5f){kicks++;maxUp=Mathf.Max(maxUp,rb.velocity.y);}
        var target=dir*speed;target.y=grounded?Mathf.Min(rb.velocity.y,0):rb.velocity.y;
        rb.velocity=grounded?Vector3.Lerp(rb.velocity,target,.25f):rb.velocity;
      }
      Plugin.Log.LogInfo($"UltraCraft probe: {steps} steps at {speed:F1} u/s: airborne {airborne} ({100f*airborne/steps:F0}%), upward kicks {kicks}, max up {maxUp:F1} u/s, travelled {(go.transform.position-feet).magnitude/Coords.Scale:F0} blocks");
      Object.Destroy(go);
    }
}
