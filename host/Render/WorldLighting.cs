using UltraCraft.Generated;
using UnityEngine;
using UnityEngine.Rendering;
namespace UltraCraft.Render;
/// <summary>
/// The bridge's sun and sky. Follows Java's overworld clock: the sun crosses the sky by day, the sky
/// fades through a sunset into a near-black blood red at night, a dim red moon replaces the sun, Java
/// terrain/models take a red tint, and a dark red fog closes in. Shadow quality and fog settings
/// changed here are restored when the plugin shuts down.
/// </summary>
internal sealed class WorldLighting {
    GameObject? sun;
    Light? light;
    ShadowQuality oldShadows;
    ShadowResolution oldResolution;
    float oldDistance;
    int oldCascades;
    ShadowProjection oldProjection;
    bool oldFog;Color oldFogColor;FogMode oldFogMode;float oldFogStart,oldFogEnd;
    bool installed;
    float dayTime=6000,shownNight=-1;
    static readonly int FogColorId=Shader.PropertyToID("_UltraCraftFogColor"),FogStartId=Shader.PropertyToID("_UltraCraftFogStart"),FogEndId=Shader.PropertyToID("_UltraCraftFogEnd");
    public float DayTime=>dayTime;
    /// <summary>0 by day, 1 at full night.</summary>
    public float Night {get;private set;}
    public void Install(){
        if(installed)return;installed=true;
        oldShadows=QualitySettings.shadows;oldResolution=QualitySettings.shadowResolution;oldDistance=QualitySettings.shadowDistance;oldCascades=QualitySettings.shadowCascades;oldProjection=QualitySettings.shadowProjection;
        oldFog=RenderSettings.fog;oldFogColor=RenderSettings.fogColor;oldFogMode=RenderSettings.fogMode;oldFogStart=RenderSettings.fogStartDistance;oldFogEnd=RenderSettings.fogEndDistance;
        // Close-fit cascades re-fit every time the camera turns, so shadows on grass shimmer.
        QualitySettings.shadowProjection=ShadowProjection.StableFit;
        QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;
        QualitySettings.shadowDistance=Design.Render_Native.ShadowDistanceBlocks*World.Coords.Scale;QualitySettings.shadowCascades=Design.Render_Native.ShadowCascades;
        sun=new GameObject("UltraCraft sunlight");Object.DontDestroyOnLoad(sun);
        light=sun.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1;light.color=Color.white;
        light.shadows=LightShadows.Soft;light.shadowStrength=1;light.shadowBias=.05f;light.shadowNormalBias=.4f;
        light.cullingMask=1;light.renderMode=LightRenderMode.ForcePixel;
        Plugin.Log.LogInfo("UltraCraft: Java day/night lighting and sun shadows enabled");
    }
    /// <summary>Java's overworld clock, 0..24000 (0 sunrise, 6000 noon, 18000 midnight).</summary>
    public void SetTime(float time){if(Time.realtimeSinceStartup>previewUntil)dayTime=time;}
    float previewUntil;
    /// <summary>Dev: show another time of day for a few seconds without touching the Java world.</summary>
    public void Preview(float time,float seconds){dayTime=time;previewUntil=Time.realtimeSinceStartup+seconds;}
    /// <summary>Every frame after adoption: sun, sky, tint and fog for the current time.</summary>
    public void Update(Camera? camera,Atlas atlas){
        if(!installed||light==null)return;
        // Smooth night factor: dusk 12000-13500, dawn 22500-24000.
        float t=dayTime;
        float night=t<12000?0:t<13500?Smooth((t-12000)/1500):t<22500?1:Smooth(1-(t-22500)/1500);
        float dusk=Mathf.Clamp01(1-Mathf.Abs(t-12600)/1100)+Mathf.Clamp01(1-Mathf.Abs(t-23400)/900); // orange around sunset and sunrise
        dusk=Mathf.Clamp01(dusk)*(1-night*.6f);
        Night=night;
        // Sun from east (sunrise) over the top (noon) to west; the moon is opposite.
        float angle=(t-6000)/24000f*360f*Mathf.Deg2Rad;
        var toSun=new Vector3(Mathf.Sin(angle),Mathf.Cos(angle),.35f).normalized;
        var toLight=toSun.y>=0?toSun:-toSun;
        if(toLight.y<.25f){toLight.y=.25f;toLight.Normalize();} // no endless horizon shadows
        sun!.transform.rotation=Quaternion.LookRotation(-toLight);
        var moon=Parse(Design.Render_Native.MoonColor);
        light.color=Color.Lerp(Color.Lerp(Color.white,new Color(1f,.62f,.42f),dusk),moon,night);
        light.intensity=Mathf.Lerp(1f,.35f,night);
        var day=Parse(Design.Render_Native.SkyColor);
        var sky=Color.Lerp(Color.Lerp(day,Parse(Design.Render_Native.DuskSkyColor),dusk),Parse(Design.Render_Native.NightSkyColor),night);
        if(camera!=null){camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=sky;}
        var fogColor=Color.Lerp(sky,Parse(Design.Render_Native.NightFogColor),night);
        float s=World.Coords.Scale;
        float start=Mathf.Lerp((float)Design.Render_Native.FogDayStartBlocks,(float)Design.Render_Native.FogNightStartBlocks,night)*s;
        float end=Mathf.Lerp((float)Design.Render_Native.FogDayEndBlocks,(float)Design.Render_Native.FogNightEndBlocks,night)*s;
        Shader.SetGlobalColor(FogColorId,new Color(fogColor.r,fogColor.g,fogColor.b,1));
        Shader.SetGlobalFloat(FogStartId,start);Shader.SetGlobalFloat(FogEndId,end);
        // Native enemies, guns and props use Unity's own fog.
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogColor=fogColor;RenderSettings.fogStartDistance=start;RenderSettings.fogEndDistance=end;
        if(Mathf.Abs(night-shownNight)>.005f){
            shownNight=night;
            var tint=Design.Render_Native.NightTint.Split(',');
            atlas.SetTint(Color.Lerp(Color.white,new Color(float.Parse(tint[0],System.Globalization.CultureInfo.InvariantCulture),float.Parse(tint[1],System.Globalization.CultureInfo.InvariantCulture),float.Parse(tint[2],System.Globalization.CultureInfo.InvariantCulture)),night));
        }
    }
    static float Smooth(float x){x=Mathf.Clamp01(x);return x*x*(3-2*x);}
    static Color Parse(string html){ColorUtility.TryParseHtmlString(html,out var c);return c;}
    public void Dispose(){
        if(sun!=null)Object.Destroy(sun);
        Shader.SetGlobalColor(FogColorId,Color.clear);
        if(installed){
            QualitySettings.shadows=oldShadows;QualitySettings.shadowResolution=oldResolution;QualitySettings.shadowDistance=oldDistance;QualitySettings.shadowCascades=oldCascades;QualitySettings.shadowProjection=oldProjection;
            RenderSettings.fog=oldFog;RenderSettings.fogColor=oldFogColor;RenderSettings.fogMode=oldFogMode;RenderSettings.fogStartDistance=oldFogStart;RenderSettings.fogEndDistance=oldFogEnd;
            installed=false;
        }
    }
}
