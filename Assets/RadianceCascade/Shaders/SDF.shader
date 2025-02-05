Shader "Unlit/SDF"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Scale", Vector) = (1, 1, 1,1)
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float2 _Scale;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }

            float2 f16v2(float f)
            {
                return float2(f, f);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float4 jumpUV = tex2D(_MainTex, i.uv);
                if (jumpUV.x == 0 || jumpUV.y == 0)
                    discard;

                float d = distance(i.uv * _MainTex_TexelSize.zw, jumpUV * _MainTex_TexelSize.zw);
                float gradient = d / length(_MainTex_TexelSize.zw);
                return float4(gradient.xxx, 1.0);
            }
            ENDCG
        }
    }
}