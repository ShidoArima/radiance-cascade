using System;
using RadianceCascade.Scripts;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace RadianceCascade.Postprocessing
{
    [Serializable]
    [PostProcess(typeof(RadianceCascadeEffect), PostProcessEvent.BeforeStack, "GI/RadianceCascade")]
    public sealed class RadianceCascade : PostProcessEffectSettings
    {
        public FloatParameter linearSize = new() {value = 2f};
        public ColorParameter ambient = new() {value = Color.black};
        public FloatParameter gamma = new() {value = 1f};
    }

    public sealed class RadianceCascadeEffect : PostProcessEffectRenderer<RadianceCascade>
    {
        private readonly RadianceCascadeRenderer _renderer = new();

        public override void Release()
        {
            base.Release();

            _renderer.Dispose();
        }

        public override void Render(PostProcessRenderContext context)
        {
            int width = context.width;
            int height = context.height;

            context.command.SetGlobalColor("_AmbientColor", settings.ambient);
            context.command.SetGlobalFloat("_Gamma", settings.gamma);
            _renderer.Render(context.command, width, height, settings.linearSize);

            var sheet = context.propertySheets.Get(Shader.Find("Hidden/GI/MergeGI"));
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        }
    }
}