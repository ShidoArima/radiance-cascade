Shader "Hidden/GI/RadianCascades-Smooth"
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
            float _DistanceScaleOffset;
            float4 _AmbientColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            #define TAU 6.283185
            #define V2F16(v) (v.y * float(0.0039215689) + v.x)
            #define mod(x, y) (x - y * floor(x / y))
            #define EPS 0.00001

            float4 raymarch(float2 origin, float2 dir, float interval)
            {
                float rayDistance = 0.0;
                const float scale = length(_RenderExtent);

                [loop]
                for (float ii = 0.0; ii < interval; ii++)
                {
                    const float2 ray = (origin + dir * rayDistance) * (1.0 / _RenderExtent);
                    const float distance = V2F16(tex2Dlod(_DistanceField, float4(ray.xy, 0,0)).rg);
                    rayDistance += scale * distance + _DistanceScaleOffset;

                    float2 rf = floor(ray);

                    if (rf.x != 0 || rf.y != 0)
                        break;

                    if (rayDistance >= interval)
                        break;

                    if (distance <= EPS)
                        return float4(tex2Dlod(_SceneTexture, float4(ray, 0, 0)).rgb, 0.0);
                }

                return float4(0.0, 0.0, 0, 1.0);
            }

            float4 merge(float4 radiance, float index, float2 probe)
            {
                //Ignore match found
                if (radiance.a == 0.0 || _CascadeIndex >= _CascadeCount - 1.0)
                    return float4(radiance.rgb, 1.0 - radiance.a);

                float angularN1 = pow(2.0, floor(_CascadeIndex + 1.0));
                float2 extentN1 = floor(_CascadeExtent / angularN1);
                float2 interpN1 = float2(mod(index, angularN1), floor(index / angularN1)) * extentN1;
                interpN1 += clamp(probe * 0.5 + 0.25, 0.5, extentN1 - 0.5);
                return radiance + tex2D(_MainTex, interpN1 * (1.0 / _CascadeExtent));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 coord = floor(i.uv * _CascadeExtent);
                float sqr_angular = pow(2.0, floor(_CascadeIndex));
                float2 extent = floor(_CascadeExtent / sqr_angular);
                float4 probe = float4(mod(coord, extent), floor(coord / extent));
                float interval = _CascadeInterval * (1.0 - pow(4.0, _CascadeIndex)) / (1.0 - 4.0);
                float limit = _CascadeInterval * pow(4.0, _CascadeIndex);

                float2 linear_pos = _CascadeLinear * pow(2.0, _CascadeIndex);

                float2 origin = (probe.xy + 0.5) * linear_pos;
                float angular = sqr_angular * sqr_angular * 4.0;
                float index = (probe.z + probe.w * sqr_angular) * 4.0;

                float4 color = float4(0, 0, 0, 0);

                [loop]
                for (float id = 0.0; id < 4.0; id++)
                {
                    float preavg = index + float(id);
                    float angle = (preavg + 0.5) * (TAU / angular);
                    float2 delta = float2(cos(angle), -sin(angle));

                    float2 ray = origin + delta * interval;
                    float4 radiance = raymarch(ray, delta, limit);
                    color += merge(radiance, preavg, probe.xy) * 0.25;
                }

                if (_CascadeIndex == 0)
                    return float4(color.rgb, 1);

                return color;
            }
            ENDCG
        }
    }
}