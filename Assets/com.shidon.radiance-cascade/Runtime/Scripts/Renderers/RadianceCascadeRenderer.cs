using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Shidon.RadianceCascade.Renderers
{
    public class RadianceCascadeRenderer : IDisposable
    {
        private readonly DistanceFieldRenderer _distanceFieldRenderer;
        private Material _cascadeMaterial;

        private int _renderWidth;
        private int _renderHeight;

        private int _radianceCascades;
        private float _radianceLinear;
        private float _radianceInterval;
        private int _radianceWidth;
        private int _radianceHeight;

        private RenderTexture _currentRadianceTexture;
        private RenderTexture _previousRadianceTexture;

        private int _lastWidth;
        private int _lastHeight;
        private float _lastLinearSize;
        
        private static readonly int RenderExtent = Shader.PropertyToID("_RenderExtent");
        private static readonly int CascadeExtent = Shader.PropertyToID("_CascadeExtent");
        private static readonly int CascadeCount = Shader.PropertyToID("_CascadeCount");
        private static readonly int CascadeIndex = Shader.PropertyToID("_CascadeIndex");
        private static readonly int CascadeLinear = Shader.PropertyToID("_CascadeLinear");
        private static readonly int CascadeInterval = Shader.PropertyToID("_CascadeInterval");
        private static readonly int RadianceMap = Shader.PropertyToID("_RadianceMap");

        private static readonly int RadianceCascadeN = Shader.PropertyToID("_RadianceCascadeN");
        private static readonly int RadianceCascadeN1 = Shader.PropertyToID("_RadianceCascadeN1");

        public RadianceCascadeRenderer()
        {
            _distanceFieldRenderer = new DistanceFieldRenderer();
            _cascadeMaterial = new Material(Shader.Find("Hidden/GI/RadianCascades-Smooth"));
        }

        ~RadianceCascadeRenderer()
        {
            Dispose(false);
        }

        private void ReleaseUnmanagedResources()
        {
            ReleaseInternal();
            Object.DestroyImmediate(_cascadeMaterial);
        }

        private void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _distanceFieldRenderer?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void InitializeInternal(int width, int height, float linearSize = 2)
        {
            if (_lastHeight == width && _lastWidth == height && Math.Abs(_lastLinearSize - linearSize) < 0.1)
            {
                return;
            }

            ReleaseInternal();

            _renderWidth = width;
            _renderHeight = height;

            _cascadeMaterial = new Material(Shader.Find("Hidden/GI/RadianCascades-Smooth"));

            _radianceCascades = Mathf.CeilToInt(Mathf.Log(Vector2.Distance(Vector2.zero, new Vector2(_renderWidth, _renderHeight)), 4));
            var renderInterval = Vector2.Distance(Vector2.zero, new Vector2(linearSize, linearSize)) * 0.5f;

            _radianceLinear = PowerOfN(linearSize, 2);
            _radianceInterval = MultipleOfN(renderInterval, 2);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // FIXES CASCADE RAY/PROBE TRADE-OFF ERROR RATE FOR NON-POW2 RESOLUTIONS: (very important).
            var errorRate = Mathf.Pow(2, _radianceCascades - 1);
            var errorX = Mathf.CeilToInt(_renderWidth / errorRate);
            var errorY = Mathf.CeilToInt(_renderHeight / errorRate);
            _renderWidth = Mathf.FloorToInt(errorX * errorRate);
            _renderHeight = Mathf.FloorToInt(errorY * errorRate);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            _radianceWidth = Mathf.FloorToInt(_renderWidth / _radianceLinear);
            _radianceHeight = Mathf.FloorToInt(_renderHeight / _radianceLinear);
        }

        private void ReleaseInternal()
        {
            _lastHeight = -1;
            _lastWidth = -1;
            _lastLinearSize = -1;

            RenderTexture.ReleaseTemporary(_currentRadianceTexture);
            RenderTexture.ReleaseTemporary(_previousRadianceTexture);
            Shader.SetGlobalTexture(RadianceMap, Texture2D.blackTexture);
        }

        public void Render(CommandBuffer buffer, int width, int height, float linearSize = 2)
        {
            InitializeInternal(width, height, linearSize);

            _distanceFieldRenderer.Render(buffer, width, height);

            buffer.GetTemporaryRT(RadianceCascadeN, _radianceWidth, _radianceHeight, 0, FilterMode.Bilinear, GraphicsFormat.R16G16B16A16_SFloat);
            buffer.GetTemporaryRT(RadianceCascadeN1, _radianceWidth, _radianceHeight, 0, FilterMode.Bilinear, GraphicsFormat.R16G16B16A16_SFloat);

            RenderTargetIdentifier cascadeN = new RenderTargetIdentifier(RadianceCascadeN);
            RenderTargetIdentifier cascadeN1 = new RenderTargetIdentifier(RadianceCascadeN1);

            buffer.Blit(Texture2D.blackTexture, cascadeN);
            buffer.Blit(Texture2D.blackTexture, cascadeN1);

            _cascadeMaterial.SetVector(RenderExtent, new Vector4(_renderWidth, _renderHeight));
            _cascadeMaterial.SetVector(CascadeExtent, new Vector4(_radianceWidth, _radianceHeight));
            _cascadeMaterial.SetFloat(CascadeCount, _radianceCascades);
            _cascadeMaterial.SetFloat(CascadeLinear, _radianceLinear);
            _cascadeMaterial.SetFloat(CascadeInterval, _radianceInterval);

            for (var n = _radianceCascades - 1; n >= 0; n--)
            {
                buffer.SetGlobalFloat(CascadeIndex, n);
                
                buffer.Blit(cascadeN1, cascadeN, _cascadeMaterial);
                buffer.CopyTexture(cascadeN, cascadeN1);
            }

            buffer.SetGlobalTexture(RadianceMap, cascadeN);
        }

        private float MultipleOfN(float number, float n)
        {
            return n == 0 ? number : Mathf.Ceil(number / n) * n;
        }

        private float PowerOfN(float number, float n)
        {
            return Mathf.Pow(n, Mathf.Ceil(Mathf.Log(number, n)));
        }
    }
}