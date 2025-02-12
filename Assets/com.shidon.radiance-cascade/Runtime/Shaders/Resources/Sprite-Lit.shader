Shader "Sprites/Default-Lit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #pragma shader_feature _ AMBIENT_ENABLED

            #include "UnityCG.cginc"

            #ifdef UNITY_INSTANCING_ENABLED
            
            UNITY_INSTANCING_BUFFER_START(PerDrawSprite)
                // SpriteRenderer.Color while Non-Batched/Instanced.
                UNITY_DEFINE_INSTANCED_PROP(fixed4, unity_SpriteRendererColorArray)
                // this could be smaller but that's how bit each entry is regardless of type
                UNITY_DEFINE_INSTANCED_PROP(fixed2, unity_SpriteFlipArray)
            UNITY_INSTANCING_BUFFER_END(PerDrawSprite)

            #define _RendererColor  UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteRendererColorArray)
            #define _Flip           UNITY_ACCESS_INSTANCED_PROP(PerDrawSprite, unity_SpriteFlipArray)

            #endif // instancing

            CBUFFER_START(UnityPerDrawSprite)
            #ifndef UNITY_INSTANCING_ENABLED
            fixed4 _RendererColor;
            fixed2 _Flip;
            #endif
            float _EnableExternalAlpha;
            CBUFFER_END

            sampler2D _MainTex;
            sampler2D _AlphaTex;
            sampler2D _RadianceMap;
            fixed4 _Color;
            
            float2 _RadianceMapScale;

            float3 GetRadiance(float4 uv)
            {
                float2 sceneCoord = uv.xy / _RadianceMapScale;
                sceneCoord += (1 - 1 / _RadianceMapScale) * 0.5f;

                float3 radiance = tex2Dproj(_RadianceMap, float4(sceneCoord.xy, uv.z, uv.w)).rgb;
                return radiance;
            }

            #define LIGHT_COORD(index) float4 lightcoord : TEXCOORD##index;
            #define COMPUTE_LIGHT_UV(output, pos) output.lightcoord = ComputeScreenPos(pos);
            #define APPLY_RADIANCE(color, input) color.rgb *= GetRadiance(input.lightcoord);

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                LIGHT_COORD(1)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 SampleSpriteTexture(float2 uv)
            {
                fixed4 color = tex2D(_MainTex, uv);

                #if ETC1_EXTERNAL_ALPHA
                fixed4 alpha = tex2D (_AlphaTex, uv);
                color.a = lerp (color.a, alpha.r, _EnableExternalAlpha);
                #endif

                return color;
            }

            inline float4 UnityFlipSprite(in float3 pos, in fixed2 flip)
            {
                return float4(pos.xy * flip, pos.z, 1.0);
            }

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityFlipSprite(v.vertex, _Flip);
                o.vertex = UnityObjectToClipPos(o.vertex);
                o.texcoord = v.texcoord;

                COMPUTE_LIGHT_UV(o, o.vertex)
                o.color = v.color * _Color * _RendererColor;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 c = SampleSpriteTexture(i.texcoord) * i.color;

                APPLY_RADIANCE(c, i)
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}