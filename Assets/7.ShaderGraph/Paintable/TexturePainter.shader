Shader "TNTC/TexturePainter"{   

    Properties{
        _PainterColor ("Painter Color", Color) = (0, 0, 0, 0)
    }

    SubShader{
        Cull Off ZWrite Off ZTest Off

        Pass{
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

			sampler2D _MainTex;
            float4 _MainTex_ST;
            
            float3 _PainterPosition;
            float _Radius;
            float _Hardness;
            float _Strength;
            float4 _PainterColor;
            float _PrepareUV;

            struct appdata{
                float4 vertex : POSITION;
				float2 uv : TEXCOORD1;
            };

            struct v2f{
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
            };

            float mask(float3 position, float3 center, float radius, float hardness){
                float m = distance(center, position);
                return 1 - smoothstep(radius * hardness, radius, m);    
            }

            v2f vert (appdata v){
                v2f o;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.uv = v.uv;
				float4 uv = float4(0, 0, 0, 1);
                // 타일링으로 UV가 [0,1]을 벗어날 수 있음 (예: 3.7). frac() 없이 그대로 clip 좌표로
                // 쓰면 유효 범위(-1~1)를 벗어나 GPU가 통째로 클리핑해버려서, 화면에는 반복돼 보이지만
                // 실제로는 원본 [0,1] 타일에만 칠해지고 나머지 타일은 전혀 칠해지지 않는다.
                // 화면 표시가 마스크 텍스처를 Repeat로 샘플링하는 것과 동일하게, 여기서도 UV를
                // frac()으로 감싸서 항상 같은 [0,1] 캔버스에 칠하도록 맞춘다.
                float2 wrappedUV = frac(v.uv.xy);
                uv.xy = float2(1, _ProjectionParams.x) * (wrappedUV * float2( 2, 2) - float2(1, 1));
				o.vertex = uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target{   
                if(_PrepareUV > 0 ){
                    return float4(1, 0, 0, 1);
                }         

                float4 col = tex2D(_MainTex, i.uv);
                float f = mask(i.worldPos, _PainterPosition, _Radius, _Hardness);
                float edge = f * _Strength;

                // 색(팀 소유권)은 섞지 않고 통째로 교체 → 텍스처 RGB는 항상 순수 팀색
                // 빈 곳은 가장자리까지 내 색으로, 다른 잉크 위에서는 절반 이상 덮였을 때만 교체
                float owns = (edge > 0) * max(step(0.5, edge), step(col.a, 0.01));
                float3 rgb = lerp(col.rgb, _PainterColor.rgb, owns);

                // 흐림(커버리지)은 알파에만 누적
                float a = lerp(col.a, max(col.a, edge), owns);
                return float4(rgb, a);
            }
            ENDCG
        }
    }
}