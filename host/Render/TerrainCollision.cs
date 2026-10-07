using System;
using System.Collections.Generic;
using UnityEngine;
using UltraCraft.World;
namespace UltraCraft.Render;
internal sealed unsafe class TerrainCollision {
    readonly Dictionary<(int,int,int,int),GameObject> sections=new();
    readonly List<(int,int,int,int)> evicted=new();
    readonly Dictionary<(int,int,int,int),ulong> hashes=new();
    GameObject? root;
    public int Count=>sections.Count;
    public void Clear(){foreach(var p in sections.Values)UnityEngine.Object.Destroy(p);sections.Clear();hashes.Clear();}
    /// <summary>Drops collision for sections more than <paramref name="radius"/> chunks from the given chunk.</summary>
    public void Evict(int cx,int cz,int radius){
      evicted.Clear();foreach(var k in sections.Keys)if(Math.Abs(k.Item1-cx)>radius||Math.Abs(k.Item3-cz)>radius)evicted.Add(k);
      foreach(var k in evicted){UnityEngine.Object.Destroy(sections[k]);sections.Remove(k);hashes.Remove(k);}
    }
    /// <summary>
    /// Java re-sends a section whenever its light changes (torches, sunrise), with the same blocks.
    /// Rebuilding hundreds of static colliders for that stalls PhysX, so unchanged data is skipped.
    /// </summary>
    bool Unchanged(int x,int y,int z,int kind,byte* p,uint bytes){
      ulong h=14695981039346656037UL;for(uint i=0;i<bytes;i++){h^=p[i];h*=1099511628211UL;}
      var k=(x,y,z,kind);if(sections.ContainsKey(k)&&hashes.TryGetValue(k,out var old)&&old==h)return true;
      hashes[k]=h;return false;
    }
    GameObject Begin(int x,int y,int z,int kind){
      if(root==null){root=new GameObject("UltraCraft terrain collision");UnityEngine.Object.DontDestroyOnLoad(root);}
      var k=(x,y,z,kind);if(sections.TryGetValue(k,out var old))UnityEngine.Object.Destroy(old);
      var go=new GameObject("UltraCraft collision");go.transform.SetParent(root.transform,false);
      go.transform.position=Coords.ToUnity(x*16,y*16,z*16);go.layer=EnvironmentLayer();go.tag="Floor";
      // Firmer footing than ULTRAKILL's arenas: its ground easing left about two-thirds of a block of slide,
      // which on blocks felt like ice. Only friction changes; jumps, dashes and slides stay native.
      go.AddComponent<CustomGroundProperties>().friction=(float)Generated.Design.World_Fresh.GroundFriction;
      sections[k]=go;return go;
    }
    /// <summary>
    /// ULTRAKILL's "Environment" layer (8). The first bit of LMD.Environment is layer 6, which the
    /// player stands on but enemies' physics ignore, so they fell through Java terrain.
    /// GroundCheckEnemy also needs a standable tag such as "Floor".
    /// </summary>
    public static int EnvironmentLayer(){int named=LayerMask.NameToLayer("Environment");return named>=0?named:8;}
    static void Box(GameObject go,float x0,float y0,float z0,float x1,float y1,float z1){var c=go.AddComponent<BoxCollider>();c.center=new Vector3((x0+x1)*.5f,(y0+y1)*.5f,-(z0+z1)*.5f)*Coords.Scale;c.size=new Vector3(x1-x0,y1-y0,z1-z0)*Coords.Scale;}
    public void Partials(byte* p,uint bytes){if(bytes<16)return;int* h=(int*)p;int count=h[3];if(count<0||16L+24L*count>bytes)return;if(Unchanged(h[0],h[1],h[2],1,p,bytes))return;var go=Begin(h[0],h[1],h[2],1);float* b=(float*)(p+16);for(int i=0;i<count;i++,b+=6)Box(go,b[0],b[1],b[2],b[3],b[4],b[5]);}
    public void Solids(byte* p,uint bytes){if(bytes<16)return;int* h=(int*)p;if(Unchanged(h[0],h[1],h[2],0,p,bytes))return;var go=Begin(h[0],h[1],h[2],0);if(h[3]==0)return;if(bytes<528)return;ulong* bits=(ulong*)(p+16);var used=new bool[4096];for(int i=0;i<4096;i++)used[i]=(bits[i>>6]&(1UL<<(i&63)))!=0;
      for(int y=0;y<16;y++)for(int z=0;z<16;z++)for(int x=0;x<16;x++){
        if(!used[x+16*z+256*y])continue;int xe=x+1;while(xe<16&&used[xe+16*z+256*y])xe++;
        int ze=z+1;while(ze<16&&Row(used,x,xe,ze,y))ze++;
        int ye=y+1;while(ye<16&&Plane(used,x,xe,z,ze,ye))ye++;
        for(int yy=y;yy<ye;yy++)for(int zz=z;zz<ze;zz++)for(int xx=x;xx<xe;xx++)used[xx+16*zz+256*yy]=false;
        Box(go,x,y,z,xe,ye,ze);
      }
    }
    static bool Row(bool[] a,int x,int xe,int z,int y){for(int i=x;i<xe;i++)if(!a[i+16*z+256*y])return false;return true;}
    static bool Plane(bool[] a,int x,int xe,int z,int ze,int y){for(int i=z;i<ze;i++)if(!Row(a,x,xe,i,y))return false;return true;}
}
