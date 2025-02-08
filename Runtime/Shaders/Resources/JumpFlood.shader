Shader "Hidden/GI/JumpFlood"
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

            float _JumpDistance;

            float2 getUV(float2 uv)
            {
                return (uv * _RenderSize.zw + 0.5f) * _RenderSize.xy;
            }

            #define V2F16(v) ((v.y * float(0.0039215689)) + v.x)

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
                    float2 jump = uv + offsets[index] * step * _RenderSize.xy;
                    float4 seed = tex2Dlod(_MainTex, float4(jump.xy, 0, 0));
                
                    if(seed.x == 0 || seed.y == 0)
                        continue;

                    const float dist = distance(seed.xy * _RenderSize.zw, uv * _RenderSize.zw);
                
                    if (dist <= closest_dist)
                    {
                        closest_dist = dist;
                        closest_data = seed;
                    }
                }
                
                // for(int id = 0; id < 9; id++) {
                //     float2 jump = i.uv + offsets[id] * float2(_JumpDistance * _RenderSize.xy);
                //     float4 seed = tex2D(_MainTex, jump);
                //     float2 seedpos = float2(V2F16(seed.xy), V2F16(seed.zw));
                //     float dist = distance(seedpos * _RenderSize.zw, i.uv * _RenderSize.zw);
                //
                //     if(seed.x == 0 || seed.y == 0)
                //          continue;
                //     
                //     if (dist <= closest_dist) {
                //         closest_dist = dist;
                //         closest_data = seed;
                //     }
                // }

                return closest_data;
            }
            ENDCG
        }
    }
}