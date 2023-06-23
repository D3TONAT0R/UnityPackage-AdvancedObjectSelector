Shader "Hidden/SelectorIcon"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Lighting Off
        Cull Off
        ZTest Always
        ZWrite Off
        Fog { Mode Off }
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 clipUV : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            sampler2D _GUIClipTexture;
            uniform float4x4 unity_GUIClipTextureMatrix;

            half _Whiteout;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                float3 eyePos = UnityObjectToViewPos(v.vertex);
				o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= tex2D(_GUIClipTexture, i.clipUV).a;
                col = pow(col, 0.454545);
                col.rgb = lerp(col.rgb, fixed3(1,1,1), _Whiteout);
                return col;
            }
            ENDCG
        }
    }
}
