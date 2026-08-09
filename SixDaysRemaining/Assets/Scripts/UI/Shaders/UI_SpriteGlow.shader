Shader "UI/SpriteGlow"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _GlowColor ("Glow Color", Color) = (1,0.8,0.3,1)

        _GlowStrength ("Glow Strength", Range(0,3))
        = 0
    }


    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
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


            sampler2D _MainTex;

            float4 _GlowColor;
            float _GlowStrength;


            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };


            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };


            v2f vert(appdata v)
            {
                v2f o;

                o.vertex =
                UnityObjectToClipPos(v.vertex);

                o.uv=v.uv;
                o.color=v.color;

                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {

                fixed4 col =
                tex2D(_MainTex,i.uv);


                col.rgb +=
                _GlowColor.rgb *
                col.a *
                _GlowStrength;


                return col*i.color;

            }


            ENDCG
        }
    }
}