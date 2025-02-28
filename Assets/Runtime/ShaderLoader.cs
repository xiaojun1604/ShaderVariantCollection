// Runtime/ShaderLoader.cs
using UnityEngine;
using System.Collections;

public class ShaderLoader : MonoBehaviour
{
    [SerializeField] private string shaderBundleName = "shaders";

    IEnumerator Start()
    {
        // 1. 加载AB
        var bundleLoad = AssetBundle.LoadFromFileAsync(
            System.IO.Path.Combine(Application.streamingAssetsPath, shaderBundleName)
        );
        yield return bundleLoad;

        // 2. 加载变体集合
        var svcLoad = bundleLoad.assetBundle.LoadAssetAsync<ShaderVariantCollection>("CollectedVariants");
        yield return svcLoad;

        if (svcLoad.asset != null)
        {
            // 3. 预热变体
            var svc = (ShaderVariantCollection)svcLoad.asset;
            svc.WarmUp();
            Debug.Log($"已预热 {svc.shaderCount} 个Shader变体");
        }

        // 4. 加载变体映射表
        var mapReq = UnityEngine.Networking.UnityWebRequest.Get(
            System.IO.Path.Combine(Application.streamingAssetsPath, "shader_variant_map.txt")
        );
        yield return mapReq.SendWebRequest();
        ParseVariantMap(mapReq.downloadHandler.text);
    }

    void ParseVariantMap(string mapText)
    {
        foreach (var line in mapText.Split('\n'))
        {
            // 解析变体使用情况
            if (!string.IsNullOrEmpty(line))
                Shader.EnableKeyword(line.Trim());
        }
    }
}