using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UltraCraft.Generated;
using UltraCraft.World;
namespace UltraCraft.Render;
internal sealed unsafe class WaterVolumes {
    sealed class Section{public float[] Boxes=Array.Empty<float>();public GameObject? Object;public bool Dirty=true;}
    readonly Dictionary<(int,int,int),Section> sections=new();
    GameObject? root;
    public int Count=>sections.Values.Count(s=>s.Object!=null&&s.Object.activeSelf);
    public void Clear(){foreach(var s in sections.Values)if(s.Object!=null)UnityEngine.Object.Destroy(s.Object);sections.Clear();}
    public void Evict(int cx,int cz,int radius){
        foreach(var key in sections.Keys.Where(k=>Math.Abs(k.Item1-cx)>radius||Math.Abs(k.Item3-cz)>radius).ToList()){
            if(sections[key].Object!=null)UnityEngine.Object.Destroy(sections[key].Object);sections.Remove(key);}
    }
    public void Receive(byte* payload,uint bytes){
        if(bytes<16)return;var h=(int*)payload;int count=h[3];if(count<0||16L+count*24L>bytes)return;
        var key=(h[0],h[1],h[2]);var values=new float[count*6];var data=(float*)(payload+16);
        for(int i=0;i<values.Length;i++)values[i]=data[i];
        if(!sections.TryGetValue(key,out var section))sections[key]=section=new Section();
        if(section.Boxes.SequenceEqual(values))return;section.Boxes=values;section.Dirty=true;
    }
    public void Update(Vector3 feet){
        if(SceneHelper.CurrentScene!=Design.Launch_Main.HostScene||SceneHelper.PendingScene!=null)return;
        if(root==null){root=new GameObject("UltraCraft water volumes");UnityEngine.Object.DontDestroyOnLoad(root);}
        foreach(var entry in sections){var s=entry.Value;
            var corner=Coords.ToUnity(entry.Key.Item1*16,entry.Key.Item2*16,entry.Key.Item3*16);
            var bounds=new Bounds(corner+new Vector3(8,8,-8)*Coords.Scale,Vector3.one*16*Coords.Scale);
            bool near=(bounds.ClosestPoint(feet)-feet).sqrMagnitude<Mathf.Pow(Design.Fluids_Water.ActiveRadiusBlocks*Coords.Scale,2);
            if(!near){if(s.Object!=null&&s.Object.activeSelf)s.Object.SetActive(false);continue;}
            if(!s.Dirty&&(s.Object!=null||s.Boxes.Length==0)){if(s.Object!=null&&!s.Object.activeSelf)s.Object.SetActive(true);continue;}
            if(s.Object!=null)UnityEngine.Object.Destroy(s.Object);s.Object=null;s.Dirty=false;
            if(s.Boxes.Length==0)continue;
            var go=new GameObject("UltraCraft Java water");go.SetActive(false);go.transform.SetParent(root.transform,false);
            go.transform.position=Coords.ToUnity(entry.Key.Item1*16,entry.Key.Item2*16,entry.Key.Item3*16);go.layer=Design.Fluids_Water.Layer;
            for(int i=0;i<s.Boxes.Length;i+=6){var b=s.Boxes;var box=go.AddComponent<BoxCollider>();box.isTrigger=true;
                box.center=new Vector3((b[i]+b[i+3])*.5f,(b[i+1]+b[i+4])*.5f,-(b[i+2]+b[i+5])*.5f)*Coords.Scale;
                box.size=new Vector3(b[i+3]-b[i],b[i+4]-b[i+1],b[i+5]-b[i+2])*Coords.Scale;}
            var water=go.AddComponent<Water>();water.simplifyWaterProcessing=true;water.notWet=true;go.SetActive(true);s.Object=go;
        }
    }
}
