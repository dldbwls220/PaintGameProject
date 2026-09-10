Shader "WorldInk/DisplayZone"
{
    Properties
    {
        [Header(Ground)]
        _MainTex        ("지형 기본 텍스처",            2D)          = "white" {}
        _BumpMap        ("지형 노말맵",                 2D)          = "bump"  {}
        _Smoothness     ("지형 스무스니스",             Range(0, 1)) = 0.2
        _Metallic       ("지형 메탈릭",                 Range(0, 1)) = 0.0

        [Header(Ink)]
        _WorldInkTex    ("잉크 텍스처 (자동연결)",      2D)          = "black" {}
        _InkNormalMap   ("잉크 노말맵",                 2D)          = "bump"  {}
        _InkNormalStr   ("잉크 노말 강도",              Range(0, 2)) = 0.35
        _InkNormalTiling("잉크 노말 타일링",            Float)       = 5.0
        _InkSmoothness  ("잉크 스무스니스",             Range(0, 1)) = 0.65
        _InkMetallic    ("잉크 메탈릭",                 Range(0, 1)) = 0.0

        [Header(Zone)]
        _ZoneOffset     ("Zone 시작 좌표",              Vector)      = (-50, -50, 0, 0)
        _ZoneSizeU      ("Zone 가로 크기",              Float)       = 100
        _ZoneSizeV      ("Zone 세로 크기",              Float)       = 100

        [Header(Surface Options)]
        [Enum(Opaque, 0, Transparent, 1)]
        _Surface        ("표면 타입",                   Float)       = 0
        [Toggle(_ALPHATEST_ON)]
        _AlphaClip      ("알파 클립 사용",              Float)       = 0
        _Cutoff         ("알파 클립 임계값",            Range(0, 1)) = 0.5
        _Alpha          ("전체 불투명도",               Range(0, 1)) = 1.0

        [HideInInspector] _SrcBlend  ("__src", Float) = 1
        [HideInInspector] _DstBlend  ("__dst", Float) = 0
        [HideInInspector] _ZWrite    ("__zw",  Float) = 1

        [HideInInspector]
        _ZoneAxis       ("Zone 투영 축",                Float)       = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend  [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _SURFACE_TYPE_TRANSPARENT

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
                float4 _ZoneOffset;
                float  _ZoneSizeU;
                float  _ZoneSizeV;
                float  _ZoneAxis;
                float  _InkNormalStr;
                float  _InkNormalTiling;
                float  _InkSmoothness;
                float  _InkMetallic;
                float  _Surface;
                float  _AlphaClip;
                float  _Cutoff;
                float  _Alpha;
                float  _SrcBlend;
                float  _DstBlend;
                float  _ZWrite;
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
                float3 pos = IN.positionWS;
                float2 inkUV;

                if (_ZoneAxis < 0.5)
                {
                    inkUV.x = (pos.x - _ZoneOffset.x) / _ZoneSizeU;
                    inkUV.y = (pos.z - _ZoneOffset.y) / _ZoneSizeV;
                }
                else if (_ZoneAxis < 1.5)
                {
                    inkUV.x = (pos.x - _ZoneOffset.x) / _ZoneSizeU;
                    inkUV.y = (pos.y - _ZoneOffset.y) / _ZoneSizeV;
                }
                else
                {
                    inkUV.x = (pos.z - _ZoneOffset.x) / _ZoneSizeU;
                    inkUV.y = (pos.y - _ZoneOffset.y) / _ZoneSizeV;
                }

                float2 inkNormalUV = inkUV * _InkNormalTiling;

                half4 baseColor    = SAMPLE_TEXTURE2D(_MainTex,  sampler_MainTex,  IN.uv);
                half3 baseNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv));

                half4 inkColor    = SAMPLE_TEXTURE2D(_WorldInkTex,  sampler_WorldInkTex,  inkUV);
                half3 inkNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_InkNormalMap, sampler_InkNormalMap, inkNormalUV));
                inkNormalTS.xy   *= _InkNormalStr;

                half3x3 TBN = half3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);

                half3 baseNormalWS = TransformTangentToWorld(baseNormalTS, TBN);
                half3 inkNormalWS  = TransformTangentToWorld(inkNormalTS,  TBN);

                float ink = inkColor.a;

                half3 albedo     = lerp(baseColor.rgb, inkColor.rgb,    ink);
                half3 normalWS   = normalize(lerp(baseNormalWS, inkNormalWS, ink));
                half  smoothness = lerp(_Smoothness,   _InkSmoothness,  ink);
                half  metallic   = lerp(_Metallic,     _InkMetallic,    ink);
                half  alpha      = _Alpha;

            #if defined(_ALPHATEST_ON)
                clip(alpha - _Cutoff);
            #endif

                InputData lightingInput       = (InputData)0;
                lightingInput.positionWS      = pos;
                lightingInput.normalWS        = normalWS;
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(pos);
                lightingInput.shadowCoord     = TransformWorldToShadowCoord(pos);
                lightingInput.fogCoord        = ComputeFogFactor(IN.positionHCS.z);
                lightingInput.bakedGI         = SampleSH(normalWS);

                SurfaceData surfaceData       = (SurfaceData)0;
                surfaceData.albedo            = albedo;
                surfaceData.normalTS          = half3(0, 0, 1);
                surfaceData.metallic          = metallic;
                surfaceData.smoothness        = smoothness;
                surfaceData.occlusion         = 1;
                surfaceData.alpha             = alpha;

                half4 finalColor = UniversalFragmentPBR(lightingInput, surfaceData);
                finalColor.rgb   = MixFog(finalColor.rgb, lightingInput.fogCoord);

            #if defined(_SURFACE_TYPE_TRANSPARENT)
                finalColor.a = alpha;
            #else
                finalColor.a = 1.0;
            #endif

                return finalColor;
            }
            ENDHLSL
        }

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

    CustomEditor "WorldInkDisplayZoneGUI"
}
