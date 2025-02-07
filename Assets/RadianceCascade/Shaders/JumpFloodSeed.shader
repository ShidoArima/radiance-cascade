Shader "Hidden/GI/JumpFloodSeed"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100
        
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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                return o;
            }

            #define F16V2(f) float2(floor(f * 255.0) * float(0.0039215689), frac(f * 255.0))

            fixed4 frag(v2f i) : SV_Target
            {
                float4 scene = tex2Dlod(_MainTex, float4(i.uv, 0, 1));
                if (scene.a < 1)
                    discard;

                //return float4(F16V2(i.uv.x * scene.a), F16V2(i.uv.y * scene.a));

                return float4(i.uv, 0, 0);
            }
            ENDCG
        }
    }
}