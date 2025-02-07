Shader "Hidden/GI/MergeGI"
{
  HLSLINCLUDE
      #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
      TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
      TEXTURE2D_SAMPLER2D(_RadianceMap, sampler_RadianceMap);

      float4 Frag(VaryingsDefault i) : SV_Target
      {
          float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
          float4 radiance = SAMPLE_TEXTURE2D(_RadianceMap, sampler_RadianceMap, i.texcoord);
          color.rgb = saturate(color.rgb + radiance.rgb);

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