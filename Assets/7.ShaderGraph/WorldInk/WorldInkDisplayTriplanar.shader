Shader "WorldInk/DisplayTriplanar"
{
    Properties
    {
        [Header(Ground)]
        _MainTex        ("지형 기본 텍스처",            2D)          = "white" {}
        _BumpMap        ("지형 노말맵",                 2D)          = "bump"  {}
        _Smoothness     ("지형 스무스니스",             Range(0, 1)) = 0.2
        _Metallic       ("지형 메탈릭",                 Range(0, 1)) = 0.0

        [Header(Ink)]
        _WorldInkTexXZ  ("잉크 텍스처 XZ (바닥/천장)", 2D)          = "black" {}
        _WorldInkTexXY  ("잉크 텍스처 XY (앞뒤 벽)",  2D)          = "black" {}
        _WorldInkTexZY  ("잉크 텍스처 ZY (좌우 벽)",  2D)          = "black" {}
        _InkNormalMap   ("잉크 노말맵",                 2D)          = "bump"  {}
        _InkNormalStr   ("잉크 노말 강도",              Range(0, 2)) = 1.0
        _InkNormalTiling("잉크 노말 타일링",            Float)       = 4.0
        _InkSmoothness  ("잉크 스무스니스",             Range(0, 1)) = 0.85
        _InkMetallic    ("잉크 메탈릭",                 Range(0, 1)) = 0.0

        [Header(Map)]
        _WorldMapOffset ("맵 시작 좌표 (XZ)",           Vector)      = (-50, -50, 0, 0)
        _WorldMapSize   ("맵 크기 (월드 유닛)",         Float)       = 100
        _WorldMapMinY   ("맵 최소 높이 (Y)",            Float)       = 0
        _WorldMapHeight ("맵 높이 크기 (월드 유닛)",   Float)       = 30
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

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_MainTex);        SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
            TEXTURE2D(_WorldInkTexXZ);  SAMPLER(sampler_WorldInkTexXZ);
            TEXTURE2D(_WorldInkTexXY);  SAMPLER(sampler_WorldInkTexXY);
            TEXTURE2D(_WorldInkTexZY);  SAMPLER(sampler_WorldInkTexZY);
            TEXTURE2D(_InkNormalMap);   SAMPLER(sampler_InkNormalMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float  _Smoothness;
                float  _Metallic;
                float4 _WorldMapOffset;
                float  _WorldMapSize;
                float  _WorldMapMinY;
                float  _WorldMapHeight;
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
                float3 pos = IN.positionWS;

                // ① 3방향 UV 계산
                // XZ : 바닥/천장용 (X, Z 좌표 사용)
                float2 uvXZ = float2(
                    (pos.x - _WorldMapOffset.x) / _WorldMapSize,
                    (pos.z - _WorldMapOffset.y) / _WorldMapSize
                );

                // XY : 앞뒤 벽용 (X, Y 좌표 사용)
                float2 uvXY = float2(
                    (pos.x - _WorldMapOffset.x) / _WorldMapSize,
                    (pos.y - _WorldMapMinY)      / _WorldMapHeight
                );

                // ZY : 좌우 벽용 (Z, Y 좌표 사용)
                float2 uvZY = float2(
                    (pos.z - _WorldMapOffset.y) / _WorldMapSize,
                    (pos.y - _WorldMapMinY)      / _WorldMapHeight
                );

                // ② 법선의 절댓값 = 각 방향 텍스처의 기여 가중치
                // 바닥(법선 위) → absN = (0,1,0) → XZ 100%, XY 0%, ZY 0%
                // 45도 경사   → absN = (0,0.7,0.7) → XZ 50%, XY 50%
                float3 absN = abs(IN.normalWS);

                // 가중치 합이 1이 되도록 정규화
                float totalWeight = absN.x + absN.y + absN.z + 0.0001f;
                float wXZ = absN.y / totalWeight;   // 바닥/천장 기여도
                float wXY = absN.z / totalWeight;   // 앞뒤 벽 기여도
                float wZY = absN.x / totalWeight;   // 좌우 벽 기여도

                // ③ 각 방향 잉크 텍스처 샘플링
                half4 inkXZ = SAMPLE_TEXTURE2D(_WorldInkTexXZ, sampler_WorldInkTexXZ, uvXZ);
                half4 inkXY = SAMPLE_TEXTURE2D(_WorldInkTexXY, sampler_WorldInkTexXY, uvXY);
                half4 inkZY = SAMPLE_TEXTURE2D(_WorldInkTexZY, sampler_WorldInkTexZY, uvZY);

                // ④ 가중치로 3방향 잉크를 블렌딩
                // 각 텍스처의 알파에도 가중치 적용
                half4 inkColor = inkXZ * wXZ + inkXY * wXY + inkZY * wZY;

                // ⑤ 잉크 노말맵 (타일링 UV)
                float2 inkNormalUV = uvXZ * _InkNormalTiling;
                half3 inkNormalTS  = UnpackNormal(SAMPLE_TEXTURE2D(_InkNormalMap, sampler_InkNormalMap, inkNormalUV));
                inkNormalTS.xy    *= _InkNormalStr;

                // ⑥ 지형 샘플링
                half4 baseColor   = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                half3 baseNormalTS = UnpackNormal(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv));

                half3x3 TBN = half3x3(IN.tangentWS, IN.bitangentWS, IN.normalWS);

                half3 baseNormalWS = TransformTangentToWorld(baseNormalTS, TBN);
                half3 inkNormalWS  = TransformTangentToWorld(inkNormalTS,  TBN);

                // ⑦ 잉크 알파 기준으로 최종 블렌딩
                float ink = inkColor.a;

                half3 albedo     = lerp(baseColor.rgb, inkColor.rgb,    ink);
                half3 normalWS   = normalize(lerp(baseNormalWS, inkNormalWS, ink));
                half  smoothness = lerp(_Smoothness,   _InkSmoothness,  ink);
                half  metallic   = lerp(_Metallic,     _InkMetallic,    ink);

                // ⑧ URP PBR 라이팅
                InputData lightingInput         = (InputData)0;
                lightingInput.positionWS        = pos;
                lightingInput.normalWS          = normalWS;
                lightingInput.viewDirectionWS   = GetWorldSpaceNormalizeViewDir(pos);
                lightingInput.shadowCoord       = TransformWorldToShadowCoord(pos);
                lightingInput.fogCoord          = ComputeFogFactor(IN.positionHCS.z);
                lightingInput.bakedGI           = SampleSH(normalWS);

                SurfaceData surfaceData         = (SurfaceData)0;
                surfaceData.albedo              = albedo;
                surfaceData.normalTS            = half3(0, 0, 1);
                surfaceData.metallic            = metallic;
                surfaceData.smoothness          = smoothness;
                surfaceData.occlusion           = 1;
                surfaceData.alpha               = 1;

                half4 finalColor  = UniversalFragmentPBR(lightingInput, surfaceData);
                finalColor.rgb    = MixFog(finalColor.rgb, lightingInput.fogCoord);

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
}
