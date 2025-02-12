Shader "Hidden/GI/DistanceField"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always
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
            float4 _RenderSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
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
                float4 jumpUV = tex2Dlod(_MainTex, float4(i.uv, 0, 0));
                float d = distance(i.uv * _RenderSize.zw, jumpUV * _RenderSize.zw);
                float gradient = d / length(_RenderSize.zw);
                return float4(gradient.xx, 0, 1.0);
            }
            ENDCG
        }
    }
}