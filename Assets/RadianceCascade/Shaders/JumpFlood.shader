Shader "Unlit/JumpFlood"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _JumpDistance("Jump Distance", Float) = 0

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

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o, o.vertex);
                return o;
            }


            float _JumpDistance;

            float2 getUV(float2 uv)
            {
                return (uv * _MainTex_TexelSize.zw + 0.5f) * _MainTex_TexelSize.xy;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 offsets[9];
                offsets[0] = float2(-1.0, -1.0);
                offsets[1] = float2(-1.0, 0.0);
                offsets[2] = float2(-1.0, 1.0);
                offsets[3] = float2(0.0, -1.0);
                offsets[4] = float2(0.0, 0.0);
                offsets[5] = float2(0.0, 1.0);
                offsets[6] = float2(1.0, -1.0);
                offsets[7] = float2(1.0, 0.0);
                offsets[8] = float2(1.0, 1.0);

                float closest_dist = 9999999.9;
                float4 closest_data = float4(0, 0, 0, 0);

                float2 uv = i.uv;
                float step = max(1, _JumpDistance);

                for (int index = 0; index < 9; index++)
                {
                    float2 jump = uv + offsets[index] * step * _MainTex_TexelSize.xy;
                    float4 seed = tex2Dlod(_MainTex, float4(jump.xy, 0, 0));

                    if(seed.x == 0 || seed.y == 0)
                        continue;
                    
                    float dist = distance(seed.xy, uv);

                    if (dist <= closest_dist)
                    {
                        closest_dist = dist;
                        closest_data = seed;
                    }
                }

                return closest_data;
            }
            ENDCG
        }
    }
}