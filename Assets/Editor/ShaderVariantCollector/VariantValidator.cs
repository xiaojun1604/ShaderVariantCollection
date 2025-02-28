// Editor/ShaderVariantCollector/VariantValidator.cs

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class VariantValidator : EditorWindow
{
    [MenuItem("Tools/Shader/Validate Coverage")]
    static void Validate()
    {
        try
        {
            var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(
                "Assets/CollectedVariants.shadervariants");
            if (svc == null)
            {
                Debug.LogError("未找到 ShaderVariantCollection 文件");
                return;
            }

            int missing = 0;
            int processed = 0;
            var passTypes = Enum.GetValues(typeof(PassType)); // 缓存枚举值
            var guids = AssetDatabase.FindAssets("t:Shader");
            int totalShaders = guids.Length;

            try
            {
                foreach (var guid in guids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                
                    // 更新进度条
                    EditorUtility.DisplayProgressBar("验证中...", 
                        $"{shader?.name ?? "未知Shader"} ({processed+1}/{totalShaders})", 
                        (float)processed / totalShaders);

                    if (shader == null) continue;

                    foreach (PassType pass in passTypes)
                    {
                        if (!svc.Contains(new ShaderVariantCollection.ShaderVariant(shader, pass, new string[0])))
                        {
                            Debug.LogWarning($"Missing: {shader.name} - {pass}");
                            missing++;
                        }
                    }
                    processed++;
                
                    // 每处理50个Shader释放一次内存
                    if (processed % 50 == 0)
                    {
                        EditorUtility.UnloadUnusedAssetsImmediate();
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log($"验证完成\n总Shader数: {totalShaders}\n缺失变体数: {missing}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"验证过程发生异常: {e}");
        }
    }
}