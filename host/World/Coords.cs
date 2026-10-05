using UnityEngine;
using UltraCraft.Generated;
namespace UltraCraft.World;
internal static class Coords {
    public static readonly float Scale=(float)Design.World_Fresh.HostScale;
    public static Vector3 ToUnity(double x,double y,double z)=>new Vector3((float)x,(float)y,(float)-z)*Scale;
    public static void ToMc(Vector3 p,out double x,out double y,out double z){x=p.x/Scale;y=p.y/Scale;z=-p.z/Scale;}
    public static float McYawFromUnity(float yaw)=>Mathf.DeltaAngle(0,yaw+180);
    public static Quaternion Rotation(float yaw,float pitch)=>Quaternion.Euler(pitch,yaw+180,0);
}
