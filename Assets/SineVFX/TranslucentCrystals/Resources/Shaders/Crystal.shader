Shader "SineVFX/TranslucentCrystals/Crystal"
{
    Properties
    {
        [Header(Translucency)]
        _Translucency("Strength", Range(0, 50)) = 1
        _TransNormalDistortion("Normal Distortion", Range(0, 1)) = 0.1
        _TransScattering("Scattering Falloff", Range(1, 50)) = 2
        _TransDirect("Direct", Range(0, 1)) = 1
        _TransAmbient("Ambient", Range(0, 1)) = 0.2
        _TranslucencyMask("Translucency Mask", 2D) = "white" {}
        _TransShadow("Shadow", Range(0, 1)) = 0.9
        _TranslucencyMaskPower("Translucency Mask Power", Range(0, 1)) = 1
        _AlbedoMask("Albedo Mask", 2D) = "white" {}
        _ColorTint1("Color Tint 1", Color) = (1,1,1,1)
        _ColorTint2("Color Tint 2", Color) = (1,1,1,1)
        _Normal("Normal", 2D) = "bump" {}
        _Emission("Emission", 2D) = "white" {}
        _EmissionColor("Emission Color", Color) = (1,1,1,1)
        _EmissionPower("Emission Power", Range(0, 10)) = 2
        [Toggle]_RampEnabled("Ramp Enabled", Float) = 0
        [Toggle]_RampInverted("Ramp Inverted", Float) = 0
        _Ramp("Ramp", 2D) = "white" {}
        _RampMask("Ramp Mask", 2D) = "white" {}
        _MetallicSmoothness("MetallicSmoothness", 2D) = "white" {}
        _Metallic("Metallic", Range(0, 1)) = 1
        _Smoothness("Smoothness", Range(0, 1)) = 1
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] __dirty("", Int) = 1
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_AlbedoMask); SAMPLER(sampler_AlbedoMask);
            TEXTURE2D(_Normal); SAMPLER(sampler_Normal);
            TEXTURE2D(_Emission); SAMPLER(sampler_Emission);
            TEXTURE2D(_Ramp); SAMPLER(sampler_Ramp);
            TEXTURE2D(_RampMask); SAMPLER(sampler_RampMask);
            TEXTURE2D(_MetallicSmoothness); SAMPLER(sampler_MetallicSmoothness);
            TEXTURE2D(_TranslucencyMask); SAMPLER(sampler_TranslucencyMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _AlbedoMask_ST;
                float4 _Normal_ST;
                float4 _Emission_ST;
                float4 _RampMask_ST;
                float4 _MetallicSmoothness_ST;
                float4 _TranslucencyMask_ST;
                float4 _ColorTint1;
                float4 _ColorTint2;
                float4 _EmissionColor;
                float _EmissionPower;
                float _RampEnabled;
                float _RampInverted;
                float _Metallic;
                float _Smoothness;
                float _Translucency;
                float _TransNormalDistortion;
                float _TransScattering;
                float _TransDirect;
                float _TransAmbient;
                float _TransShadow;
                float _TranslucencyMaskPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 tangentWS    : TEXCOORD2;
                float3 bitangentWS  : TEXCOORD3;
                float2 uv           : TEXCOORD4;
                float  fogFactor    : TEXCOORD5;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normInputs.normalWS;
                output.tangentWS = normInputs.tangentWS;
                output.bitangentWS = normInputs.bitangentWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uvNormal = input.uv * _Normal_ST.xy + _Normal_ST.zw;
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_Normal, sampler_Normal, uvNormal));
                half3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS, input.bitangentWS, input.normalWS));
                normalWS = normalize(normalWS);

                float2 uvAlbedo = input.uv * _AlbedoMask_ST.xy + _AlbedoMask_ST.zw;
                half albedoMask = SAMPLE_TEXTURE2D(_AlbedoMask, sampler_AlbedoMask, uvAlbedo).r;
                half3 albedo = lerp(_ColorTint1.rgb, _ColorTint2.rgb, albedoMask);

                float2 uvEmission = input.uv * _Emission_ST.xy + _Emission_ST.zw;
                half4 emTex = SAMPLE_TEXTURE2D(_Emission, sampler_Emission, uvEmission);
                half3 emission = half3(0, 0, 0);

                if (_RampEnabled > 0.5)
                {
                    float2 uvRampMask = input.uv * _RampMask_ST.xy + _RampMask_ST.zw;
                    half rampMaskVal = SAMPLE_TEXTURE2D(_RampMask, sampler_RampMask, uvRampMask).r;
                    if (_RampInverted > 0.5) rampMaskVal = 1.0 - rampMaskVal;
                    half4 rampColor = SAMPLE_TEXTURE2D(_Ramp, sampler_Ramp, float2(rampMaskVal, 0.0));
                    emission = emTex.rgb * rampColor.rgb * _EmissionPower;
                }
                else
                {
                    emission = emTex.rgb * _EmissionColor.rgb * _EmissionPower;
                }

                float2 uvMS = input.uv * _MetallicSmoothness_ST.xy + _MetallicSmoothness_ST.zw;
                half4 msTex = SAMPLE_TEXTURE2D(_MetallicSmoothness, sampler_MetallicSmoothness, uvMS);
                half metallic = msTex.r * _Metallic;
                half smoothness = msTex.a * _Smoothness;

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo;
                surfaceData.metallic = metallic;
                surfaceData.smoothness = smoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = emission;
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = 1.0;

                half4 finalColor = UniversalFragmentPBR(inputData, surfaceData);

                float2 uvTrans = input.uv * _TranslucencyMask_ST.xy + _TranslucencyMask_ST.zw;
                half transMask = SAMPLE_TEXTURE2D(_TranslucencyMask, sampler_TranslucencyMask, uvTrans).r * _TranslucencyMaskPower;

                Light mainLight = GetMainLight(inputData.shadowCoord);
                half3 lightDir = mainLight.direction + normalWS * _TransNormalDistortion;
                half transVdotL = pow(saturate(dot(inputData.viewDirectionWS, -lightDir)), max(1.0, _TransScattering));
                half3 translucency = mainLight.color * (transVdotL * _TransDirect + inputData.bakedGI * _TransAmbient) * transMask * _Translucency;
                finalColor.rgb += albedo * translucency;

                half fresnel = pow(1.0 - saturate(dot(normalWS, inputData.viewDirectionWS)), 3.0);
                finalColor.rgb += emission * fresnel * 0.75;

                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);

                return finalColor;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Back
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings ShadowPassVertex(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                #if UNITY_REVERSED_Z
                    output.positionCS.z = min(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    output.positionCS.z = max(output.positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                return output;
            }

            half4 ShadowPassFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Back
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 DepthOnlyFragment(Varyings input) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
