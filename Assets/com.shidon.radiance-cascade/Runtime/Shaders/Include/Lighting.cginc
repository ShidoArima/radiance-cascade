#ifndef LIGHTING_INCLUDED
#define LIGHTING_INCLUDED

#include <HLSLSupport.cginc>
#include "UnityCG.cginc"

#ifdef LIGHTING_GI_ENABLED

sampler2D _RadianceMap;
float2 _RadianceMapScale;
float _Emission;
float _LightInfluence = 1;

UNITY_DECLARE_TEX3D(_RadianceLut);

float3 ApplyLut(float3 color)
{
    const float3 scale = (32.0 - 1.0) / 32.0;
    const float3 offset = 0.5 / 32.0;
    float3 result = UNITY_SAMPLE_TEX3D(_RadianceLut, scale * color + offset);
    return result;
}

float3 GetRadiance(float4 uv)
{
    float2 sceneCoord = uv.xy / _RadianceMapScale;
    sceneCoord += (1 - 1 / _RadianceMapScale) * 0.5f;

    float3 radiance = tex2Dproj(_RadianceMap, float4(sceneCoord.xy, uv.z, uv.w)).rgb;
    return lerp(radiance, ApplyLut(radiance), _LightInfluence);
}

#define LIGHT_COORD(index) float4 lightcoord : TEXCOORD##index;
#define COMPUTE_LIGHT_UV(output, pos) output.lightcoord = ComputeScreenPos(pos);
#define APPLY_RADIANCE(color, input) color.rgb *= GetRadiance(input.lightcoord);

#else

#define LIGHT_COORD(index)
#define COMPUTE_LIGHT_UV(output, pos)
#define APPLY_RADIANCE(color, input)

#endif
#endif
