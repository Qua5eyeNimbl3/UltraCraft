using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class BuildShaders {
    public static void Run(){
        var shader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/World.shader");
        if(shader==null||ShaderUtil.ShaderHasError(shader))throw new Exception("UltraCraft shader did not compile");
        string folder=Environment.GetEnvironmentVariable("ULTRACRAFT_SHADER_OUTPUT");
        if(string.IsNullOrEmpty(folder))throw new Exception("Missing shader output folder");
        Directory.CreateDirectory(folder);
        var manifest=BuildPipeline.BuildAssetBundles(folder,new[]{new AssetBundleBuild{assetBundleName="ultracraft-shaders",assetNames=new[]{"Assets/World.shader"}}},BuildAssetBundleOptions.ChunkBasedCompression|BuildAssetBundleOptions.ForceRebuildAssetBundle,BuildTarget.StandaloneWindows64);
        if(manifest==null)throw new Exception("UltraCraft shader bundle failed");
        var bundle=AssetBundle.LoadFromFile(Path.Combine(folder,"ultracraft-shaders"));
        if(bundle==null||bundle.LoadAsset<Shader>("Assets/World.shader")==null)throw new Exception("UltraCraft shader bundle did not load back in its own editor");
        bundle.Unload(true);
        Debug.Log("UltraCraft own shader bundle built successfully.");
    }
}
