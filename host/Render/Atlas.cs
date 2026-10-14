using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.Rendering;

namespace UltraCraft.Render;

/// <summary>How a face is drawn, decided per triangle from its texture's alpha (see <see cref="Atlas.Classify"/>).</summary>
internal enum Surface
{
    /// <summary>Every texel opaque: writes depth and casts shadows.</summary>
    Solid,

    /// <summary>Holes in it: alpha test with depth and cutout shadows.</summary>
    Cutout,

    /// <summary>Minecraft's translucent layer (stained glass, water): blended, no depth write.</summary>
    Translucent,

    /// <summary>Nothing to see (a skin's unused hat layer): not drawn at all.</summary>
    Empty,
}

/// <summary>
/// Java textures, tint/AO and lightmap rendered by the bridge's own compiled world shader.
/// </summary>
internal sealed unsafe class Atlas
{
    private Shader? opaqueShader, cutoutShader, transparentShader;
    private readonly string requestedShader;
    private string? shaderScene;
    private float lastShaderLookup;
    private readonly float brightness;
    private readonly Dictionary<uint, Texture2D> textures = new(); // 0 = the block atlas
    private readonly Dictionary<uint, (int width, int height, byte[] alpha)> alphas = new();
    private readonly Dictionary<ulong, Material> materials = new();
    private readonly Dictionary<(int, int), Texture2D> stamps = new();
    private readonly Dictionary<ulong, Surface> classified = new();
    private readonly AssetBundle? shaderBundle;
    private readonly Shader? worldShader;
    public Texture2D? Lightmap {get;private set;}

    public Atlas(ConfigFile config)
    {
        ConfigEntry<string> wanted = config.Bind("Render", "BlockShader", "",
            "Optional opaque shader override. Empty uses ULTRAKILL's depth-writing world shader.");
        brightness = config.Bind("Render", "BlockBrightness", 1f,
            "Scales Minecraft texture and vertex colour brightness.").Value;
        var path=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(typeof(Plugin).Assembly.Location),Generated.Design.Render_Native.ShaderBundle);
        if(System.IO.File.Exists(path)){
            shaderBundle=AssetBundle.LoadFromFile(path);
            if(shaderBundle!=null)worldShader=shaderBundle.LoadAsset<Shader>("Assets/World.shader");
        }
        if(worldShader==null||!worldShader.isSupported)Plugin.Log.LogWarning("UltraCraft own world shader unavailable; using stable native fallback.");
        requestedShader=wanted.Value;ResolveShaders();
    }

    static Shader? Find(params string[] names){foreach(var name in names){if(string.IsNullOrEmpty(name))continue;var candidate=Shader.Find(name);if(candidate!=null&&candidate.isSupported)return candidate;foreach(var loaded in Resources.FindObjectsOfTypeAll<Shader>())if(loaded.name==name&&loaded.isSupported)return loaded;}return null;}
    public bool Usable => opaqueShader != null && cutoutShader != null && transparentShader != null;
    public void ResolveShaders(){
        var phase=SceneHelper.CurrentScene+"|"+SceneHelper.PendingScene;
        if(shaderScene==phase&&Time.realtimeSinceStartup-lastShaderLookup<2)return;
        shaderScene=phase;lastShaderLookup=Time.realtimeSinceStartup;
        var oldOpaque=opaqueShader;var oldCutout=cutoutShader;var oldTransparent=transparentShader;
        opaqueShader=string.IsNullOrEmpty(requestedShader)?worldShader:Find(requestedShader);
        opaqueShader??=Find("psx/unlit/unlit","Unlit/Texture","Standard");
        cutoutShader=worldShader??Find("psx/unlit/alphatest/nocull","Unlit/Transparent Cutout","Sprites/Default");
        transparentShader=worldShader??Find("psx/unlit/transparent/nocull","Sprites/Default");
        if(oldOpaque==opaqueShader&&oldCutout==cutoutShader&&oldTransparent==transparentShader)return;
        foreach(var pair in materials){var kind=(Surface)(pair.Key&3);bool twoSided=(pair.Key&4)!=0;var chosen=Select(kind,twoSided);if(chosen!=null){pair.Value.shader=chosen;Configure(pair.Value,kind,twoSided);}}
        Plugin.Log.LogInfo($"UltraCraft shaders: opaque={opaqueShader?.name}, cutout={cutoutShader?.name}, transparent={transparentShader?.name}");
    }
    Shader? Select(Surface surface,bool twoSided)=>surface==Surface.Translucent?transparentShader:surface==Surface.Cutout||twoSided?cutoutShader:opaqueShader;

    /// <summary>kRenAtlas / kRenTexture: a whole texture, RGBA8, top row first.</summary>
    public void SetTexture(uint id, int width, int height, byte* pixels)
    {
        if (width <= 0 || height <= 0 || width > 16384 || height > 16384)
        {
            return;
        }
        if (!textures.TryGetValue(id, out Texture2D? texture) || texture.width != width || texture.height != height)
        {
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
            }
            // The block atlas gets Minecraft's four mip levels: without them distant grass and
            // leaves alias into flickering noise whenever the camera moves.
            texture = new Texture2D(width, height, TextureFormat.RGBA32, id == 0 ? AtlasMips(width, height) : 1, linear: id==Generated.Design.Render_Native.LightmapTextureId)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            textures[id] = texture;
            foreach (KeyValuePair<ulong, Material> pair in materials)
            {
                if ((uint)(pair.Key >> 8) == id)
                {
                    if(pair.Value.HasProperty("_BaseMap"))pair.Value.SetTexture("_BaseMap", texture);
                    pair.Value.mainTexture = texture;
                }
            }
            Plugin.Log.LogInfo(id == 0 ? $"blocks: atlas {width}x{height}" : $"blocks: texture {id} {width}x{height}");
        }
        var alpha = new byte[width * height];
        for (int i = 0; i < alpha.Length; i++)
        {
            alpha[i] = pixels[i * 4 + 3];
        }
        alphas[id] = (width, height, alpha);
        classified.Clear();
        // Loaded as sent: Unity's row 0 is then Minecraft's top row, so Minecraft's v is used unchanged.
        if (texture.mipmapCount > 1)
        {
            var copy = new byte[width * height * 4];
            System.Runtime.InteropServices.Marshal.Copy((IntPtr)pixels, copy, 0, copy.Length);
            texture.SetPixelData(copy, 0);
            texture.Apply(updateMipmaps: true);
        }
        else
        {
            texture.LoadRawTextureData((IntPtr)pixels, width * height * 4);
            texture.Apply(updateMipmaps: false);
        }
        if(id==Generated.Design.Render_Native.LightmapTextureId){
            Lightmap=texture;
            foreach(var material in materials.Values)if(material.HasProperty("_MinecraftLightMap"))material.SetTexture("_MinecraftLightMap",texture);
        }
    }

    static int AtlasMips(int width, int height)
    {
        int levels = 1;
        while (levels < 5 && (width >> levels) >= 1 && (height >> levels) >= 1 && (width % (1 << levels)) == 0 && (height % (1 << levels)) == 0)
        {
            levels++;
        }
        return levels;
    }

    /// <summary>kRenAtlasRegion: an animated sprite's current frame.</summary>
    public void SetRegion(int x, int y, int width, int height, byte* pixels)
    {
        if (!textures.TryGetValue(0, out Texture2D? texture) || width <= 0 || height <= 0 || x < 0 || y < 0
            || (long)x + width > texture.width || (long)y + height > texture.height)
        {
            return;
        }
        // A small texture per sprite size, copied into the atlas on the GPU: re-uploading the whole
        // atlas for every animated sprite every frame would move hundreds of megabytes.
        if (!stamps.TryGetValue((width, height), out Texture2D? stamp))
        {
            stamp = new Texture2D(width, height, TextureFormat.RGBA32, mipChain: false, linear: false) { hideFlags = HideFlags.HideAndDontSave };
            stamps[(width, height)] = stamp;
        }
        stamp.LoadRawTextureData((IntPtr)pixels, width * height * 4);
        stamp.Apply(updateMipmaps: false);
        Graphics.CopyTexture(stamp, 0, 0, 0, 0, width, height, texture, 0, 0, x, y);
    }

    /// <summary>
    /// Looks at the texels a triangle covers (a grid of points across it, pulled slightly inwards so
    /// neighbouring sprites don't bleed in): all opaque is <see cref="Surface.Solid"/>, none is
    /// <see cref="Surface.Empty"/>, a mix is <see cref="Surface.Cutout"/>.
    /// </summary>
    public Surface Classify(uint id, float u0, float v0, float u1, float v1, float u2, float v2)
    {
        if (!alphas.TryGetValue(id, out (int width, int height, byte[] alpha) map))
        {
            return Surface.Solid;
        }
        // Entity models and terrain repeat the same UV triangles every frame: classify each once.
        ulong key = Mix(Mix(Mix(Mix(Mix(Mix(id, u0), v0), u1), v1), u2), v2);
        if (classified.TryGetValue(key, out Surface known))
        {
            return known;
        }
        Surface result = ClassifySamples(map, u0, v0, u1, v1, u2, v2);
        if (classified.Count > 400000)
        {
            classified.Clear();
        }
        classified[key] = result;
        return result;
    }

    private static ulong Mix(ulong key, float f) => (key ^ (uint)BitConverter.SingleToInt32Bits(f)) * 1099511628211UL;

    private static Surface ClassifySamples((int width, int height, byte[] alpha) map, float u0, float v0, float u1, float v1, float u2, float v2)
    {
        const int steps = 5;
        bool anyOpaque = false, anyClear = false;
        for (int i = 0; i <= steps; i++)
        {
            for (int j = 0; j <= steps - i; j++)
            {
                // Barycentric weights, shrunk towards the middle by a twentieth.
                float a = (i + 0.05f * steps) / (steps * 1.15f), b = (j + 0.05f * steps) / (steps * 1.15f), c = 1f - a - b;
                int x = Mathf.Clamp((int)((u0 * a + u1 * b + u2 * c) * map.width), 0, map.width - 1);
                int y = Mathf.Clamp((int)((v0 * a + v1 * b + v2 * c) * map.height), 0, map.height - 1);
                if (map.alpha[y * map.width + x] < 26)
                {
                    anyClear = true;
                }
                else
                {
                    anyOpaque = true;
                }
            }
        }
        return !anyOpaque ? Surface.Empty : anyClear ? Surface.Cutout : Surface.Solid;
    }

    /// <summary>The material for a texture and surface kind, culled or two-sided.</summary>
    public Material? Material(uint textureId, Surface surface, bool twoSided)
    {
        var shader=Select(surface,twoSided);
        if (shader == null || surface == Surface.Empty)
        {
            return null;
        }
        ulong key = ((ulong)textureId << 8) | (ulong)surface | (twoSided ? 4ul : 0ul);
        if (materials.TryGetValue(key, out Material? material))
        {
            return material;
        }
        material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, enableInstancing = false };
        textures.TryGetValue(textureId, out Texture2D? texture);
        if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap", texture);
        material.mainTexture = texture;
        Configure(material,surface,twoSided);
        materials[key] = material;
        return material;
    }
    void Configure(Material material,Surface surface,bool twoSided){
        var tint=new Color(brightness*nightTint.r, brightness*nightTint.g, brightness*nightTint.b, 1f);
        if(material.HasProperty("_Color"))material.SetColor("_Color",tint);
        if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",tint);
        if(material.HasProperty("_Cull"))material.SetFloat("_Cull",twoSided?(float)CullMode.Off:(float)CullMode.Back);
        if(material.HasProperty("_Cutoff"))material.SetFloat("_Cutoff",.1f);
        if(material.HasProperty("_VertexWarpScale"))material.SetFloat("_VertexWarpScale",0);
        if(material.HasProperty("_MinecraftLightMap"))material.SetTexture("_MinecraftLightMap",Lightmap!=null?Lightmap:Texture2D.whiteTexture);
        if(material.HasProperty("_AlphaClip"))material.SetFloat("_AlphaClip",surface==Surface.Cutout||twoSided?1:0);
        if(material.HasProperty("_ShadowStrength"))material.SetFloat("_ShadowStrength",(float)Generated.Design.Render_Native.ShadowStrength);
        if(material.HasProperty("_SrcBlend"))material.SetFloat("_SrcBlend",surface==Surface.Translucent?(float)BlendMode.SrcAlpha:(float)BlendMode.One);
        if(material.HasProperty("_DstBlend"))material.SetFloat("_DstBlend",surface==Surface.Translucent?(float)BlendMode.OneMinusSrcAlpha:(float)BlendMode.Zero);
        if(material.HasProperty("_ZWrite"))material.SetFloat("_ZWrite",surface==Surface.Translucent?0:1);
        material.SetShaderPassEnabled("ShadowCaster",surface!=Surface.Translucent);
        if (surface != Surface.Translucent)
        {
            // The shader itself writes depth; a material property cannot override
            // Sprites/Default's literal ZWrite Off. Keep skybox behind these passes.
            material.renderQueue = material.shader.name=="Sprites/Default"?(int)RenderQueue.Transparent-50:surface==Surface.Solid?(int)RenderQueue.Geometry:(int)RenderQueue.AlphaTest;
        }
        else
        {
            material.SetShaderPassEnabled("ShadowCaster", false);
            material.renderQueue = (int)RenderQueue.Transparent;
        }
    }
    private Color nightTint = Color.white;
    /// <summary>Day/night colour over every Java material (white by day, red at night).</summary>
    public void SetTint(Color tint)
    {
        nightTint = tint;
        var colour = new Color(brightness * tint.r, brightness * tint.g, brightness * tint.b, 1f);
        foreach (Material material in materials.Values)
        {
            if (material.HasProperty("_Color")) material.SetColor("_Color", colour);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
        }
    }
    public static Vector2 LightUv(uint light)=>new Vector2(((light&15)+.5f)/16f,(((light>>8)&15)+.5f)/16f);
}
