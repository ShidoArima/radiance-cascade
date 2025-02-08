Shader "Hidden/GI/MergeGI"
{
    HLSLINCLUDE
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    TEXTURE2D_SAMPLER2D(_SceneTexture, sampler_SceneTexture);
    TEXTURE2D_SAMPLER2D(_RadianceMap, sampler_RadianceMap);

    float4 _AmbientColor;
    float  _Gamma;

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
        float4 radiance = SAMPLE_TEXTURE2D(_RadianceMap, sampler_RadianceMap, i.texcoord);
        float mask = SAMPLE_TEXTURE2D(_SceneTexture, sampler_SceneTexture, i.texcoord).a;

        radiance.rgb = pow(radiance.rgb, 1.0 / _Gamma);

        radiance.rgb += _AmbientColor;
        float4 light = saturate(color * radiance);

        color = lerp(color, light, 1 - pow(mask, 50));

        return color;
    }
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag
            ENDHLSL
        }
    }
}