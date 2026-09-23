Shader "WorldInk/Brush"
{
    Properties
    {
        _MainTex      ("현재 잉크 텍스처", 2D)      = "black" {}
        _BrushPos     ("브러시 위치 (UV)", Vector)  = (0.5, 0.5, 0, 0)
        _BrushRadius  ("브러시 반경 (UV)", Float)   = 0.01
        _BrushColor   ("잉크 색상", Color)          = (1, 0, 0, 1)
        _BrushHardness("경계 선명도", Float)        = 0.8
        _AspectRatio  ("종횡비 보정 (sizeV/sizeU)", Float) = 1.0
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4    _BrushPos;
            float     _BrushRadius;
            float4    _BrushColor;
            float     _BrushHardness;
            float     _AspectRatio;

            fixed4 frag(v2f_img i) : SV_Target
            {
                float4 current = tex2D(_MainTex, i.uv);

                float2 delta = i.uv - _BrushPos.xy;
                // V 방향에 sizeV/sizeU 배율을 곱해 UV 거리를 월드 거리 기준으로 보정
                // 결과: sizeU != sizeV 여도 바닥에서 원형으로 찍힘
                delta.y *= _AspectRatio;
                float dist = length(delta);

                float alpha = (1 - smoothstep(_BrushRadius * _BrushHardness, _BrushRadius, dist)) * _BrushColor.a;

                // 색(팀 소유권)은 섞지 않고 통째로 교체 → 텍스처 RGB는 항상 순수 팀색
                // 빈 곳은 가장자리까지 내 색으로, 다른 잉크 위에서는 절반 이상 덮였을 때만 교체
                float owns = (alpha > 0) * max(step(0.5, alpha), step(current.a, 0.01));
                float3 rgb = lerp(current.rgb, _BrushColor.rgb, owns);

                // 흐림(커버리지)은 알파에만 누적
                float a = lerp(current.a, max(current.a, alpha), owns);
                return float4(rgb, a);
            }
            ENDCG
        }
    }
}
