Shader "Unlit/DistanceField"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100
        
        Blend Off

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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float4 _RenderSize;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float2 f16v2(float f)
            {
                return float2(f, f);
            }

            #define V2F16(v) ((v.y * float(0.0039215689)) + v.x)
            #define F16V2(f) float2(floor(f * 255.0) * float(0.0039215689), frac(f * 255.0))

            fixed4 frag(v2f i) : SV_Target
            {
                // float4 jfuv = tex2D(_MainTex, i.uv);
                // float2 jumpflood = float2(V2F16(jfuv.rg),V2F16(jfuv.ba));
                // float dist = distance(i.uv * _RenderSize.zw, jumpflood * _RenderSize.zw);
                // float4 color = float4(F16V2(dist / length(_RenderSize.zw)), 0.0, 1.0);
                // return color;
                
                float4 jumpUV = tex2D(_MainTex, i.uv);
                // if (jumpUV.x == 0 || jumpUV.y == 0)
                //     discard;
                
                float d = distance(i.uv * _RenderSize.zw, jumpUV * _RenderSize.zw);
                float gradient = d / length(_RenderSize.zw);
                return float4(F16V2(gradient.x), 0, 1.0);
            }
            ENDCG
        }
    }
}