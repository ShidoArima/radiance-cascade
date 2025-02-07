Shader "Hidden/GI/RadianCascades-Interlaced"
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

            #pragma exclude_renderers d3d11_9x

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
            // Radiance Cascades Inputs:
            // Gameplay lights/occluders scene.
            sampler2D _SceneTexture;
            // Distance Field surface to sample from.
            sampler2D _DistanceField;
            // Surface size of the cascade being rendered.
            float2 _RenderExtent;
            // Surface size of the cascade being rendered.
            float2 _CascadeExtent;
            // Total number of cascades used.
            float _CascadeCount;
            // Current cascade index to render.
            float _CascadeIndex;
            // Probe density/spacing of cascade0.
            float _CascadeLinear;
            // Ray interval length of cascade0.
            float _CascadeInterval;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            #define TAU 6.283185
            #define PI (0.5 * TAU)
            #define V2F16(v) (v.y * float(0.0039215689) + v.x)
            #define SRGB(c) pow(c.rgb, 2.2)
            #define LINEAR(c) pow(c.rgb, 1.0 / 2.2)
            #define mod(x, y) (x - y * floor(x / y))
            #define EPS 0.00001
            
            float4 raymarch(float2 origin, float2 dir, float interval)
            {
                float rayDistance = 0.0;
                const float scale = length(_RenderExtent);
                
                [loop]
                for (float ii = 0.0; ii < interval; ii++)
                {
                    float2 ray = (origin + dir * rayDistance) * (1.0 / _RenderExtent);
                    float distance = V2F16(tex2Dlod(_DistanceField, float4(ray.xy, 0, 0)).rg);
                    rayDistance += scale * distance;

                    float2 rf = floor(ray);

                    if(rf.x != 0 || rf.y != 0)
                        break;
                    
                    if (rayDistance >= interval)
                        break;

                    if (distance <= EPS)
                        return float4(SRGB(tex2D(_SceneTexture, ray).rgb), 0.0);
                }

                return float4(0.0, 0.0, 0, 1.0);
            }

            float4 mergeNearestProbe(float4 radiance, float index, float2 probe)
            {
                if (radiance.a == 0.0 || _CascadeIndex >= _CascadeCount - 1.0)
                    return float4(radiance.rgb, 1.0 - radiance.a);

                float angularN1 = pow(2.0, floor(_CascadeIndex + 1.0));
                float2 extentN1 = floor(_CascadeExtent / angularN1);
                float2 interpN1 = float2(mod(index, angularN1), floor(index / angularN1)) * extentN1;
                interpN1 += clamp(probe + 0.5, 0.5, extentN1 - 0.5);
                return tex2D(_MainTex, interpN1 * (1.0 / _CascadeExtent));
            }

            void getInterlacedProbes(float2 probe, out float2 probes[4])
            {
                float2 probeN1 = floor((probe - 1.0) / 2.0);
	            probes[2] = probeN1 + float2(0.0, 0.0);
	            probes[1] = probeN1 + float2(1.0, 0.0);
	            probes[0] = probeN1 + float2(0.0, 1.0);
	            probes[3] = probeN1 + float2(1.0, 1.0);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 coord = floor(i.uv * _CascadeExtent);
                float sqr_angular = pow(2.0, floor(_CascadeIndex));
                float2 extent = floor(_CascadeExtent / sqr_angular);
                float4 probe = float4(mod(coord, extent), floor(coord / extent));
                float interval = _CascadeInterval * (1.0 - pow(4.0, _CascadeIndex)) / (1.0 - 4.0);
                float limit = _CascadeInterval * pow(4.0, _CascadeIndex);

                float2 linearN = _CascadeLinear * pow(2.0, _CascadeIndex);
                float2 linearN1 = _CascadeLinear * pow(2.0, _CascadeIndex + 1.0);

                float2 origin = (probe.xy + 0.5) * linearN;
                float angular = sqr_angular * sqr_angular * 4.0;
                float index = (probe.z + probe.w * sqr_angular) * 4.0;

	            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
	            // Nearest Interlaced:
	            float2 probesN1[4];
	            getInterlacedProbes(probe.xy, probesN1);
                float offset = probe.x * 2.0 + probe.y;
                float2 probeN1 = probesN1[int(mod(offset, 4.0))];
	            ////////////////////////////////////////////////////////////////////////////////////////////////////////////

                float4 color = float4(0, 0, 0, 0);

                [loop]
                for (float id = 0.0; id < 4.0; id++)
                {
                    float preavg = index + float(id);
                    float theta = (preavg + 0.5) * (TAU / angular);
                    float thetaNm1 = (floor(preavg / 4.0) + 0.5) * (TAU / (angular / 4.0));
                    
                    float2 delta = float2(cos(theta), -sin(theta));
                    float2 deltaNm1 = float2(cos(thetaNm1), -sin(thetaNm1));
                    float2 ray_start = origin + deltaNm1 * interval;

		            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
		            /// Nearest Interlaced:
		            float2 originN1 = (probeN1 + 0.5) * linearN1;
		            float2 ray_end = originN1 + delta * (interval + limit);
                    float len = length(ray_end - ray_start);
		            float4 rad = raymarch(ray_start, normalize(ray_end - ray_start), len);
		            rad = mergeNearestProbe(rad, preavg, probeN1);
		            ////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    color += rad * 0.25;
                }

                if (_CascadeIndex < 1.0)
                    color = float4(color.rgb, 1.0);
                
                return color;
            }
            ENDCG
        }
    }
}