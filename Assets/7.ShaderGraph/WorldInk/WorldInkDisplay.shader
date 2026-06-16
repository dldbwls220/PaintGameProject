Shader "WorldInk/Display"
{
    Properties
    {
        [Header(Ground)]
        _MainTex        ("지형 기본 텍스처",            2D)          = "white" {}
        _BumpMap        ("지형 노말맵",                 2D)          = "bump"  {}
        _Smoothness     ("지형 스무스니스",             Range(0, 1)) = 0.2
        _Metallic       ("지형 메탈릭",                 Range(0, 1)) = 0.0

        [Header(Ink)]
        _WorldInkTex    ("월드 잉크 텍스처 (자동연결)", 2D)          = "black" {}
        _InkNormalMap   ("잉크 노말맵",                 2D)          = "bump"  {}
        _InkNormalStr   ("잉크 노말 강도",              Range(0, 2)) = 1.0
        _InkNormalTiling("잉크 노말 타일링",            Float)       = 4.0
        _InkSmoothness  ("잉크 스무스니스",             Range(0, 1)) = 0.85
        _InkMetallic    ("잉크 메탈릭",                 Range(0, 1)) = 0.0

        [Header(Map)]
        _WorldMapOffset ("맵 시작 좌표 (XZ)",           Vector)      = (-50, -50, 0, 0)
        _WorldMapSize   ("맵 크기 (월드 유닛)",         Float)       = 100
    }

    SubShader
    {
        Tags
        {
            "RenderType"            = "Opaque"
            "RenderPipeline"        = "UniversalPipeline"
            "Queue"                 = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);     SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);     SAMPLER(sampler_BumpMap);
            TEXTURE2D(_WorldInkTex); SAMPLER(sampler_WorldInkTex);
            TEXTURE2D(_InkNormalMap);SAMPLER(sampler_InkNormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _Smoothness;
                float  _Metallic;
                float4 _WorldMapOffset;
                float  _WorldMapSize;
                float  _InkNormalStr;
                float  _InkNormalTiling;
                float  _InkSmoothness;
                float  _InkMetallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                float3 tangentWS   : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS  = posInputs.positionWS;
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.normalWS    = nrmInputs.normalWS;
                OUT.tangentWS   = nrmInputs.tangentWS;
                OUT.bitangentWS = nrmInputs.bitangentWS;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ① 월드 XZ → 잉크 텍스처 UV 변환
                float2 inkUV;
                inkUV.x = (IN.positionWS.x - _WorldMapOffset.x) / _WorldMapSize;
                inkUV.y = (IN.positionWS.z - _WorldMapOffset.y) / _WorldMapSize;

                // ② 잉크 노말맵은 타일링 UV 사용
                float2 inkNormalUV = inkUV * _InkNormalTiling;

                // ③ 지형 샘플링
                half4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // 지형 노말맵 언팩 후 월드 공간으로 변환
                half3 baseNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv));
                half3 baseNormalWS = TransformTangentToWorld(
                    baseNormalTS,
                    half3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS)
                );

                // ④ 잉크 샘플링
                half4 inkColor   = SAMPLE_TEXTURE2D(_WorldInkTex,  sampler_WorldInkTex,  inkUV);
                half3 inkNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_InkNormalMap, sampler_InkNormalMap, inkNormalUV));

                // 노말 강도 적용
                inkNormalTS.xy  *= _InkNormalStr;
                half3 inkNormalWS = TransformTangentToWorld(
                    inkNormalTS,
                    half3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS)
                );

                // ⑤ 잉크 알파 기준으로 블렌딩
                float ink = inkColor.a;

                half3 albedo     = lerp(baseColor.rgb,  inkColor.rgb,    ink);
                half3 normalWS   = normalize(lerp(baseNormalWS, inkNormalWS, ink));
                half  smoothness = lerp(_Smoothness,    _InkSmoothness,  ink);
                half  metallic   = lerp(_Metallic,      _InkMetallic,    ink);

                // ⑥ URP PBR 라이팅 계산
                InputData lightingInput     = (InputData)0;
                lightingInput.positionWS    = IN.positionWS;
                lightingInput.normalWS      = normalWS;
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                lightingInput.shadowCoord   = TransformWorldToShadowCoord(IN.positionWS);
                lightingInput.fogCoord      = ComputeFogFactor(IN.positionHCS.z);
                lightingInput.bakedGI       = SampleSH(normalWS);

                SurfaceData surfaceData     = (SurfaceData)0;
                surfaceData.albedo          = albedo;
                surfaceData.normalTS        = half3(0, 0, 1);
                surfaceData.metallic        = metallic;
                surfaceData.smoothness      = smoothness;
                surfaceData.occlusion       = 1;
                surfaceData.alpha           = 1;

                half4 finalColor = UniversalFragmentPBR(lightingInput, surfaceData);
                finalColor.rgb   = MixFog(finalColor.rgb, lightingInput.fogCoord);

                return finalColor;
            }
            ENDHLSL
        }

        // 그림자 캐스팅 패스
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }
}
