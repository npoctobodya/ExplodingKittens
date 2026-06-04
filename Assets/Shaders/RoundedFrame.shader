Shader "Custom/RoundedFrame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Radius ("Roundness Radius", Range(0, 0.5)) = 0.1
        _Smoothness ("Anti-aliasing", Range(0, 0.1)) = 0.01
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Radius;
            float _Smoothness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Получаем цвет текстуры с учётом цвета спрайта
                fixed4 col = tex2D(_MainTex, i.uv) * i.color * _Color;
                
                // Преобразуем UV координаты: смещаем центр в (0,0), диапазон [-0.5, 0.5]
                float2 uvCentered = i.uv * 2.0 - 1.0;
                float2 absUV = abs(uvCentered);
                
                // Расчёт расстояния до угла
                float2 distToCorner = max(0.0, absUV - (1.0 - _Radius));
                float cornerDist = length(distToCorner);
                
                // Плавное отсечение углов с анти-алиасингом
                float alpha = 1.0 - smoothstep(0.0, _Smoothness, cornerDist);
                col.a *= alpha;
                
                return col;
            }
            ENDCG
        }
    }
}