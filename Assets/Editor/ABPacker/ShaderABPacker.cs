// Editor/ABPacker/ShaderABPacker.cs
using UnityEditor;
using UnityEngine;

public class ShaderABPacker : EditorWindow
{
    [MenuItem("Tools/AB/Build Shader Bundle")]
    static void BuildShaderAB()
    {
        // 1. 配置AB
        var shaderImporter = AssetImporter.GetAtPath("Assets/CollectedVariants.shadervariants");
        shaderImporter.SetAssetBundleNameAndVariant("shaders", "");

        // 2. 打包设置
        BuildPipeline.BuildAssetBundles(
            Application.streamingAssetsPath,
            BuildAssetBundleOptions.ForceRebuildAssetBundle |
            BuildAssetBundleOptions.DisableLoadAssetByFileName,
            BuildTarget.StandaloneWindows
        );

        // 3. 生成变体映射文件
        var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(
            "Assets/CollectedVariants.shadervariants"
        );
        GenerateVariantMap(svc);
    }

    static void GenerateVariantMap(ShaderVariantCollection svc)
    {
        var variants = new System.Collections.Generic.List<string>();
        
        // 通过反射获取变体列表（Unity 2019+）
        var method = typeof(ShaderVariantCollection).GetMethod("GetShaderVariantEntries");
        var args = new object[1] { null };
        var count = (int)method.Invoke(svc, args);
        var entries = (System.Collections.Generic.List<ShaderVariantCollection.ShaderVariant>)args[0];

        foreach (var entry in entries)
        {
            variants.Add($"{entry.shader.name}:{entry.passType}:{string.Join(",", entry.keywords)}");
        }

        System.IO.File.WriteAllText(
            $"{Application.streamingAssetsPath}/shader_variant_map.txt",
            string.Join("\n", variants)
        );
    }

    /*static void GenerateVariantMap(ShaderVariantCollection svc)
    {
        //var map = new TextAsset(string.Join("\n", svc.GetShaderVariants()));
        var map = new TextAsset(string.Join("\n", svc.GetShaderVariants()));
        System.IO.File.WriteAllText(
            $"{Application.streamingAssetsPath}/shader_variant_map.txt",
            map.text
        );
    }*/
}