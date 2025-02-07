using System;
using RadianceCascade.Scripts;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace RadianceCascade.Postprocessing
{
    [Serializable]
    [PostProcess(typeof(RadianceCascadeEffect), PostProcessEvent.BeforeStack, "GI/RadianceCascade")]
    public sealed class RadianceCascade : PostProcessEffectSettings
    {
        public FloatParameter linearSize = new() {value = 2f};
        public FloatParameter distanceScaleOffset = new() {value = 1f};
    }

    public sealed class RadianceCascadeEffect : PostProcessEffectRenderer<RadianceCascade>
    {
        private readonly RadianceCascadeRenderer _renderer = new();
        
        private static readonly int Scene = Shader.PropertyToID("_SceneGI");
        private static readonly int DistanceScaleOffset = Shader.PropertyToID("_DistanceScaleOffset");

        public override void Release()
        {
            base.Release();

            _renderer.Dispose();
        }

        public override void Render(PostProcessRenderContext context)
        {
            int width = context.width;
            int height = context.height;
            //context.command.GetTemporaryRT(Scene, width, height, 0);
            //RenderTargetIdentifier scene = new RenderTargetIdentifier(Scene);

            context.command.SetGlobalFloat(DistanceScaleOffset, settings.distanceScaleOffset);
            //context.command.Blit(context.source, scene);
            _renderer.Render(context.command, width, height, settings.linearSize);

            context.command.ReleaseTemporaryRT(Scene);

            var sheet = context.propertySheets.Get(Shader.Find("Hidden/GI/MergeGI"));
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}