using System;
using System.Collections;
using System.IO;
using UnityEngine;
using Object=UnityEngine.Object;
namespace UltraCraft.Render;
internal static unsafe class Diagnostics {
    public static void Audit(NewMovement player){
        var states=HarmonyLib.AccessTools.Field(typeof(GameStateManager),"activeStates")?.GetValue(GameStateManager.Instance) as System.Collections.IDictionary;
        string stateNames=states==null?"unknown":string.Join(",",System.Linq.Enumerable.Cast<object>(states.Keys));
        Plugin.Log.LogInfo($"UltraCraft movement audit: enabled={player.enabled}, active={player.activated}, kinematic={player.rb.isKinematic}, constraints={player.rb.constraints}, dead={player.dead}, inputLocked={GameStateManager.Instance.PlayerInputLocked}, states={stateNames}, timescale={Time.timeScale}, velocity={player.rb.velocity}");
        foreach(var shader in Resources.FindObjectsOfTypeAll<Shader>())if(shader.name=="ULTRAKILL/Master"||shader.name.Contains("psx/")||shader.name=="Standard"){
            var material=new Material(shader);string props="";
            for(int i=0;i<shader.GetPropertyCount();i++){
                string name=shader.GetPropertyName(i);var type=shader.GetPropertyType(i);
                props+=$" {name}({type})="+(type==UnityEngine.Rendering.ShaderPropertyType.Float||type==UnityEngine.Rendering.ShaderPropertyType.Range?material.GetFloat(name).ToString():type==UnityEngine.Rendering.ShaderPropertyType.Color?material.GetColor(name).ToString():type==UnityEngine.Rendering.ShaderPropertyType.Vector?material.GetVector(name).ToString():"texture");
            }
            Plugin.Log.LogInfo($"UltraCraft shader audit: {shader.name}, supported={shader.isSupported}, passes={shader.passCount}, properties={props}");Object.Destroy(material);
        }
    }
    // Render in an isolated camera. No mouse/keyboard events or native-player movement.
    public static IEnumerator Run(Atlas atlas,string folder,NewMovement player,RenderRing renderer){
        Directory.CreateDirectory(folder);
        var cameraObject=new GameObject("UltraCraft diagnostic camera");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
        camera.transform.position=new Vector3(1000,1000,0);camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.05f,.12f,.8f);camera.renderingPath=RenderingPath.Forward;
        camera.nearClipPlane=.1f;camera.farClipPlane=10;camera.orthographic=true;camera.orthographicSize=1.2f;
        var target=new RenderTexture(128,128,24,RenderTextureFormat.ARGB32);camera.targetTexture=target;
        var texture=new Texture2D(2,2);texture.SetPixels(new[]{Color.white,Color.white,Color.white,Color.white});texture.Apply();
        byte[] pixels=new byte[16];for(int i=0;i<pixels.Length;i++)pixels[i]=255;
        fixed(byte* p=pixels)atlas.SetTexture(0xFFFF,2,2,p);
        var front=Quad(camera.transform.position+Vector3.forward*3,Color.yellow);
        var back=Quad(camera.transform.position+Vector3.forward*5,Color.white);
        var rearMaterial=new Material(Shader.Find("Sprites/Default"));rearMaterial.mainTexture=texture;rearMaterial.color=new Color(.05f,.12f,.8f);rearMaterial.renderQueue=3100;back.GetComponent<Renderer>().sharedMaterial=rearMaterial;
        string cases="";bool all=true;
        foreach(var surface in new[]{Surface.Solid,Surface.Cutout}){
            var material=new Material(atlas.Material(0xFFFF,surface,false));material.SetTexture("_MinecraftLightMap",Texture2D.whiteTexture);material.SetFloat("_ShadowStrength",0);front.GetComponent<Renderer>().sharedMaterial=material;
            camera.Render();var old=RenderTexture.active;RenderTexture.active=target;
            var readback=new Texture2D(128,128,TextureFormat.RGBA32,false);readback.ReadPixels(new Rect(0,0,128,128),0,0);readback.Apply();RenderTexture.active=old;
            var center=readback.GetPixel(64,64);bool pass=center.r>.45f&&center.g>.45f&&material.shader.name==Generated.Design.Render_Native.OpaqueShader;all&=pass;
            File.WriteAllBytes(Path.Combine(folder,"depth-"+surface+".png"),readback.EncodeToPNG());
            if(cases.Length>0)cases+=",";cases+=$"{{\"surface\":\"{surface}\",\"shader\":\"{material?.shader.name}\",\"pass\":{pass.ToString().ToLowerInvariant()},\"center\":[{center.r},{center.g},{center.b}]}}";
            Plugin.Log.LogInfo($"UltraCraft GPU depth test {surface}: {(pass?"PASS":"FAIL")} shader={material?.shader.name}, center={center}");Object.Destroy(readback);Object.Destroy(material);
        }
        var extraMaterial=new Material(atlas.Material(0xFFFF,Surface.Cutout,true));extraMaterial.SetTexture("_MinecraftLightMap",Texture2D.whiteTexture);extraMaterial.SetFloat("_ShadowStrength",0);front.GetComponent<Renderer>().sharedMaterial=extraMaterial;
        var holes=new Texture2D(2,1);holes.SetPixels(new[]{Color.clear,Color.white});holes.filterMode=FilterMode.Point;holes.Apply();extraMaterial.mainTexture=holes;
        var cutImage=Read(camera,target);var left=cutImage.GetPixel(32,64);var right=cutImage.GetPixel(96,64);bool cutoutPass=left.b>.5f&&left.r<.25f&&right.r>.5f;
        File.WriteAllBytes(Path.Combine(folder,"cutout-holes.png"),cutImage.EncodeToPNG());Object.Destroy(cutImage);
        extraMaterial.mainTexture=texture;back.SetActive(false);SetColors(front,Color.white);
        bool lightmapPass=false;float dark=0,bright=0;
        if(atlas.Lightmap!=null){
            File.WriteAllBytes(Path.Combine(folder,"java-lightmap.png"),atlas.Lightmap.EncodeToPNG());extraMaterial.SetTexture("_MinecraftLightMap",atlas.Lightmap);
            SetLight(front,Atlas.LightUv(0));dark=Sample(camera,target).grayscale;
            SetLight(front,Atlas.LightUv(0xF00));bright=Sample(camera,target).grayscale;
            lightmapPass=bright>.5f&&dark<bright*.65f;
        }
        extraMaterial.SetTexture("_MinecraftLightMap",Texture2D.whiteTexture);SetColors(front,new Color(.2f,.8f,.3f));var tint=Sample(camera,target);bool tintPass=tint.g>tint.r*2&&tint.g>tint.b*1.5f;
        SetColors(front,Color.white);
        var gradient=new Texture2D(2,2,TextureFormat.RGBAFloat,false,true);gradient.filterMode=FilterMode.Bilinear;gradient.wrapMode=TextureWrapMode.Clamp;gradient.SetPixels(new[]{new Color(0,1,1),Color.white,new Color(0,1,1),Color.white});gradient.Apply();extraMaterial.mainTexture=gradient;
        var floatTarget=new RenderTexture(128,128,24,RenderTextureFormat.ARGBFloat);camera.targetTexture=floatTarget;
        float previous=0,minStep=float.MaxValue,maxStep=0;
        for(int i=0;i<16;i++){
            camera.transform.position=new Vector3(1000+i*.01f,1000,0);float value=Sample(camera,floatTarget).r;
            if(i>0){float step=value-previous;minStep=Mathf.Min(minStep,step);maxStep=Mathf.Max(maxStep,step);}previous=value;
        }
        bool smoothPass=minStep>.005f&&maxStep<.015f;
        camera.targetTexture=target;Object.Destroy(floatTarget);extraMaterial.mainTexture=texture;
        front.SetActive(false);
        var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="UltraCraft shadow test floor";floor.layer=31;floor.transform.position=new Vector3(1000,999.8f,0);floor.transform.localScale=new Vector3(12,.4f,12);
        var caster=GameObject.CreatePrimitive(PrimitiveType.Cube);caster.name="UltraCraft shadow test caster";caster.layer=31;caster.transform.position=new Vector3(1000,1001,0);caster.transform.localScale=Vector3.one*2;
        var shadowMat=new Material(extraMaterial);shadowMat.SetFloat("_ShadowStrength",.85f);shadowMat.SetFloat("_AlphaClip",0);floor.GetComponent<Renderer>().sharedMaterial=shadowMat;caster.GetComponent<Renderer>().sharedMaterial=shadowMat;
        foreach(var go in new[]{floor,caster}){var mesh=go.GetComponent<MeshFilter>().mesh;var lights=new Vector2[mesh.vertexCount];for(int i=0;i<lights.Length;i++)lights[i]=Atlas.LightUv(0xF00);mesh.uv2=lights;}
        var sunObject=new GameObject("UltraCraft shadow diagnostic light");var sun=sunObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1;sun.cullingMask=1<<31;sun.renderMode=LightRenderMode.ForcePixel;sun.shadows=LightShadows.Hard;sun.shadowBias=.015f;sun.shadowNormalBias=.02f;sunObject.transform.rotation=Quaternion.Euler(55,-45,0);
        camera.transform.position=new Vector3(1000,1008,-12);camera.transform.rotation=Quaternion.LookRotation(new Vector3(0,-8,12));camera.orthographicSize=6;camera.farClipPlane=40;
        caster.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;var noShadow=Read(camera,target);File.WriteAllBytes(Path.Combine(folder,"shadow-disabled.png"),noShadow.EncodeToPNG());
        caster.GetComponent<Renderer>().shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;var withShadow=Read(camera,target);File.WriteAllBytes(Path.Combine(folder,"shadow-enabled.png"),withShadow.EncodeToPNG());
        var a=noShadow.GetPixels();var b=withShadow.GetPixels();int changed=0;for(int i=0;i<a.Length;i++)if(a[i].grayscale-b[i].grayscale>.08f)changed++;
        bool shadowPass=changed>30;Object.Destroy(noShadow);Object.Destroy(withShadow);
        bool roundTrip=Input.JavaScreenGate.RoundTrip(player);
        bool movementReady=player.enabled&&player.activated&&!player.rb.isKinematic&&!player.dead&&!GameStateManager.Instance.PlayerInputLocked&&(player.rb.constraints&RigidbodyConstraints.FreezePosition)==0&&Time.timeScale>0;
        string tests=F($"\"cutoutHolesPass\":{Bool(cutoutPass)},\"vertexTintPass\":{Bool(tintPass)},\"javaLightmapPass\":{Bool(lightmapPass)},\"lightmapDark\":{dark},\"lightmapBright\":{bright},\"smoothMotionPass\":{Bool(smoothPass)},\"motionMinStep\":{minStep},\"motionMaxStep\":{maxStep},\"shadowPass\":{Bool(shadowPass)},\"shadowPixels\":{changed},\"movementReady\":{Bool(movementReady)},\"javaScreenRoundTripPass\":{Bool(roundTrip)}");
        var report=$"{{\"gpuDepthPass\":{Bool(all)},\"cases\":[{cases}],{tests},\"nativeGrounded\":{Bool(player.gc.onGround)},\"javaSectionCount\":{renderer.SectionCount},\"collisionSectionCount\":{renderer.CollisionSectionCount},\"waterSectionCount\":{renderer.WaterSectionCount},\"cameraActive\":{Bool(player.cc.activated)},\"mouseLocked\":\"{Cursor.lockState}\"}}";
        File.WriteAllText(Path.Combine(folder,"runtime-check.json"),report);
        Plugin.Log.LogInfo("UltraCraft GPU and movement checks: "+tests);
        foreach(var go in new[]{front,back}){Object.Destroy(go.GetComponent<MeshFilter>().sharedMesh);Object.Destroy(go);}
        foreach(var go in new[]{floor,caster}){Object.Destroy(go.GetComponent<MeshFilter>().sharedMesh);Object.Destroy(go);}Object.Destroy(sunObject);
        camera.cullingMask=~(1<<31);camera.orthographic=false;camera.fieldOfView=75;camera.farClipPlane=300;
        camera.transform.position=player.transform.position+new Vector3(-12,15,-20);camera.transform.rotation=Quaternion.LookRotation(player.transform.position+new Vector3(0,0,14)-camera.transform.position);
        var worldTarget=new RenderTexture(1280,720,24);camera.targetTexture=worldTarget;var worldImage=Read(camera,worldTarget);File.WriteAllBytes(Path.Combine(folder,"world-lighting.png"),worldImage.EncodeToPNG());Object.Destroy(worldImage);Object.Destroy(worldTarget);
        Object.Destroy(shadowMat);Object.Destroy(extraMaterial);Object.Destroy(holes);Object.Destroy(gradient);Object.Destroy(rearMaterial);Object.Destroy(texture);Object.Destroy(target);Object.Destroy(cameraObject);
        return Capture(folder);
    }
    static string Bool(bool value)=>value?"true":"false";
    static string F(FormattableString value)=>value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    static Texture2D Read(Camera camera,RenderTexture target){camera.Render();var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(target.width,target.height,target.format==RenderTextureFormat.ARGBFloat?TextureFormat.RGBAFloat:TextureFormat.RGBA32,false,true);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();RenderTexture.active=old;return image;}
    static Color Sample(Camera camera,RenderTexture target){var image=Read(camera,target);var center=image.GetPixel(target.width/2,target.height/2);Object.Destroy(image);return center;}
    static void SetColors(GameObject go,Color color){var mesh=go.GetComponent<MeshFilter>().sharedMesh;var colors=new Color[mesh.vertexCount];for(int i=0;i<colors.Length;i++)colors[i]=color;mesh.colors=colors;}
    static void SetLight(GameObject go,Vector2 light){var mesh=go.GetComponent<MeshFilter>().sharedMesh;var uv=new Vector2[mesh.vertexCount];for(int i=0;i<uv.Length;i++)uv[i]=light;mesh.uv2=uv;}
    static IEnumerator Capture(string folder){yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"gameplay-frame.png"));}
    static GameObject Quad(Vector3 position,Color color){
        var go=new GameObject("UltraCraft diagnostic quad");go.layer=31;go.transform.position=position;
        var mesh=new Mesh();mesh.vertices=new[]{new Vector3(-1,-1,0),new Vector3(1,-1,0),new Vector3(1,1,0),new Vector3(-1,1,0)};
        mesh.triangles=new[]{0,2,1,0,3,2};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.uv2=new[]{Atlas.LightUv(0xF00),Atlas.LightUv(0xF00),Atlas.LightUv(0xF00),Atlas.LightUv(0xF00)};mesh.colors=new[]{color,color,color,color};mesh.RecalculateNormals();mesh.RecalculateBounds();go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>();return go;
    }
}
