using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace RadianceCascade.Scripts
{
    public sealed class DistanceFieldRenderer : IDisposable
    {
        private readonly Material _jumpMaterial;
        private readonly Material _dfMaterial;
        private readonly Material _jumpSeedMaterial;

        private static readonly int JumpDistance = Shader.PropertyToID("_JumpDistance");
        private static readonly int RenderSize = Shader.PropertyToID("_RenderSize");
        private static readonly int DistanceField = Shader.PropertyToID("_DistanceField");
        private static readonly int JumpFloodSource = Shader.PropertyToID("_JumpFloodSource");
        private static readonly int JumpFloodTarget = Shader.PropertyToID("_JumpFloodTarget");


        public DistanceFieldRenderer()
        {
            _jumpSeedMaterial = new Material(Shader.Find("Hidden/GI/JumpFloodSeed"));
            _jumpMaterial = new Material(Shader.Find("Hidden/GI/JumpFlood"));
            _dfMaterial = new Material(Shader.Find("Hidden/GI/DistanceField"));
        }

        ~DistanceFieldRenderer()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            Object.DestroyImmediate(_jumpMaterial);
            Object.DestroyImmediate(_dfMaterial);
            Object.DestroyImmediate(_jumpSeedMaterial);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        public void Render(CommandBuffer buffer, int width, int height)
        {
            RenderTextureDescriptor textureDescriptor = new RenderTextureDescriptor(width, height, GraphicsFormat.R16G16_UNorm, 0, 0);

            buffer.GetTemporaryRT(DistanceField, textureDescriptor, FilterMode.Point);
            buffer.GetTemporaryRT(JumpFloodSource, textureDescriptor, FilterMode.Bilinear);
            buffer.GetTemporaryRT(JumpFloodTarget, textureDescriptor, FilterMode.Bilinear);

            RenderTargetIdentifier distanceField = new RenderTargetIdentifier(DistanceField);
            RenderTargetIdentifier source = new RenderTargetIdentifier(JumpFloodSource);
            RenderTargetIdentifier target = new RenderTargetIdentifier(JumpFloodTarget);

            buffer.Blit(Texture2D.blackTexture, source);
            buffer.Blit(Texture2D.blackTexture, target);
            buffer.Blit(Texture2D.blackTexture, source, _jumpSeedMaterial);

            var distance = height * (width / height);
            var jumpDistance = distance / 2;

            int steps = Mathf.CeilToInt(Mathf.Log10(distance) / Mathf.Log10(2));

            for (int i = 0; i < steps - 1; i++)
            {
                buffer.SetGlobalFloat(JumpDistance, jumpDistance);

                //Unity has a bug of passing source blit texture to the custom material in some cases, so we set it on our own
                buffer.SetGlobalTexture("_MainTex", source);
                buffer.SetGlobalVector(RenderSize, new Vector4(1f / width, 1f / height, width, height));

                buffer.Blit(source, target, _jumpMaterial);
                buffer.CopyTexture(target, source);

                jumpDistance /= 2;
            }

            buffer.SetGlobalVector(RenderSize, new Vector4(1f / width, 1f / height, width, height));
            buffer.Blit(target, distanceField, _dfMaterial);
            buffer.SetGlobalTexture(DistanceField, distanceField);

            buffer.ReleaseTemporaryRT(JumpFloodSource);
            buffer.ReleaseTemporaryRT(JumpFloodTarget);
        }
    }
}