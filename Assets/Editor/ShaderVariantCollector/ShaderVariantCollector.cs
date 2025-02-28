// Editor/ShaderVariantCollector/ShaderVariantCollector.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Rendering;

public class ShaderVariantCollector : EditorWindow
{
    [MenuItem("Tools/Shader/Collect Variants")]
    static void Collect()
    {
        // 1. 创建新集合
        var svc = new ShaderVariantCollection();
        string path = "Assets/CollectedVariants.shadervariants";

        // 2. 遍历所有材质
        var mats = Resources.FindObjectsOfTypeAll<Material>();
        HashSet<string> variants = new HashSet<string>();
        
        foreach (var mat in mats)
        {
            if (mat.shader == null) continue;
            
            // 3. 记录材质使用的变体
            var keywords = mat.shaderKeywords;
            var variant = new ShaderVariantCollection.ShaderVariant(
                mat.shader, 
                PassType.ForwardBase, // 根据实际情况调整
                keywords
            );
            
            string key = $"{mat.shader.name}_{string.Join("_", keywords)}";
            if (!variants.Contains(key))
            {
                svc.Add(variant);
                variants.Add(key);
            }
        }

        // 4. 保存并关联到Graphics设置
        AssetDatabase.CreateAsset(svc, path);
        SetShaderVariantCollection(path);
        /*GraphicsSettings.defaultShaderVariantCollection = svc;
        EditorUtility.SetDirty(GraphicsSettings.Instance);*/
        AssetDatabase.SaveAssets();
    }

    private static void SetShaderVariantCollection(string path)
    {
        var method = typeof(ShaderUtil).GetMethod("SaveCurrentShaderVariantCollection", 
            BindingFlags.Static | BindingFlags.NonPublic);
    
        if (method != null)
        {
            method.Invoke(null, new object[] { path });
        }
        else
        {
            Debug.LogError("Failed to find SaveCurrentShaderVariantCollection method");
        }
    }
}