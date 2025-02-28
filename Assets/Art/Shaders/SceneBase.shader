Shader "Custom/BuildingBRDF"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        
        _MetallicMap("Metallic (R) Roughness (G)", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0.0
        _Roughness("Roughness", Range(0,1)) = 0.5
        
        _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Range(0,2)) = 1.0
        
        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0,1)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "LightMode"="UniversalForward" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
            };

            // Texture Samplers
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_MetallicMap); SAMPLER(sampler_MetallicMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_OcclusionMap); SAMPLER(sampler_OcclusionMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Metallic;
                half _Roughness;
                half _BumpScale;
                half _OcclusionStrength;
            CBUFFER_END

            // BRDF核心函数
            half3 DisneyDiffuseCustom(half NdotV, half NdotL, half LdotH, half roughness)
            {
                half fd90 = 0.5 + 2 * LdotH * LdotH * roughness;
                return lerp(1.0, fd90, pow(1.0 - NdotL, 5)) * 
                       lerp(1.0, fd90, pow(1.0 - NdotV, 5));
            }

            half GGX(half NdotH, half roughness)
            {
                half a = roughness * roughness;
                half a2 = a * a;
                half denom = NdotH * NdotH * (a2 - 1.0) + 1.0;
                return a2 / (PI * denom * denom);
            }

            half GeometrySchlickGGX(half NdotV, half roughness)
            {
                half r = (roughness + 1.0);
                half k = (r * r) / 8.0;
                return NdotV / (NdotV * (1.0 - k) + k);
            }

            half GeometrySmith(half NdotV, half NdotL, half roughness)
            {
                return GeometrySchlickGGX(NdotV, roughness) * 
                       GeometrySchlickGGX(NdotL, roughness);
            }

            half3 FresnelSchlick(half cosTheta, half3 F0)
            {
                return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                
                output.normalWS = normalInput.normalWS;
                output.tangentWS = normalInput.tangentWS;
                output.bitangentWS = cross(output.normalWS, output.tangentWS) * input.tangentOS.w;
                
                output.shadowCoord = GetShadowCoord(vertexInput);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 基础数据采样
                half4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                half4 metallicRoughness = SAMPLE_TEXTURE2D(_MetallicMap, sampler_MetallicMap, input.uv);
                half4 normalMap = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                half ao = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;

                // 法线计算
                half3 normalTS = UnpackNormalScale(normalMap, _BumpScale);
                float3x3 TBN = float3x3(input.tangentWS, input.bitangentWS, input.normalWS);
                half3 normalWS = mul(normalTS, TBN);
                normalWS = normalize(normalWS);

                // PBR参数计算
                half metallic = metallicRoughness.r * _Metallic;
                half roughness = clamp(metallicRoughness.g * _Roughness, 0.04, 1.0);
                ao = lerp(1.0, ao, _OcclusionStrength);

                // 基础反射率
                half3 F0 = lerp(0.04, albedo.rgb, metallic);

                // 视角方向
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 reflectDir = reflect(-viewDirWS, normalWS);

                // 环境光照
                half3 ambient = SampleSH(normalWS) * ao;

                // 主光源计算
                Light mainLight = GetMainLight(input.shadowCoord);
                half3 lightDirWS = normalize(mainLight.direction);
                half3 radiance = mainLight.color * mainLight.distanceAttenuation;

                // 中间向量计算
                half3 halfDir = normalize(viewDirWS + lightDirWS);
                half NdotL = saturate(dot(normalWS, lightDirWS));
                half NdotV = saturate(dot(normalWS, viewDirWS));
                half NdotH = saturate(dot(normalWS, halfDir));
                half LdotH = saturate(dot(lightDirWS, halfDir));

                // BRDF计算
                half3 F = FresnelSchlick(LdotH, F0);
                half D = GGX(NdotH, roughness);
                half G = GeometrySmith(NdotV, NdotL, roughness);
                
                // 漫反射
                half3 kd = (1.0 - F) * (1.0 - metallic);
                half3 diffuse = kd * albedo.rgb * DisneyDiffuseCustom(NdotV, NdotL, LdotH, roughness);

                // 镜面反射
                half3 specular = (F * D * G) / max(4.0 * NdotV * NdotL, 0.001);
                
                // 直接光照贡献
                half3 directLight = (diffuse + specular) * radiance * NdotL;

                // 最终颜色合成
                half3 color = ambient * albedo.rgb + directLight;
                return half4(color, albedo.a);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
    }
}