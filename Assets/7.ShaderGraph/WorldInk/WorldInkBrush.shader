Shader "WorldInk/Brush"
{
    Properties
    {
        _MainTex      ("현재 잉크 텍스처", 2D)      = "black" {}
        _BrushPos     ("브러시 위치 (UV)", Vector)  = (0.5, 0.5, 0, 0)
        _BrushRadius  ("브러시 반경 (UV)", Float)   = 0.01
        _BrushColor   ("잉크 색상", Color)          = (1, 0, 0, 1)
        _BrushHardness("경계 선명도", Float)        = 0.8
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

            fixed4 frag(v2f_img i) : SV_Target
            {
                // 현재까지 쌓인 잉크 텍스처 읽기
                float4 current = tex2D(_MainTex, i.uv);

                // 이 픽셀의 UV와 브러시 중심까지의 거리
                float dist = distance(i.uv, _BrushPos.xy);

                // 거리 기반으로 원형 브러시 마스크 계산
                // _BrushHardness가 높을수록 경계가 선명해짐
                float alpha = 1 - smoothstep(_BrushRadius * _BrushHardness, _BrushRadius, dist);

                // 기존 잉크 위에 새 잉크 블렌딩
                return lerp(current, _BrushColor, alpha * _BrushColor.a);
            }
            ENDCG
        }
    }
}
