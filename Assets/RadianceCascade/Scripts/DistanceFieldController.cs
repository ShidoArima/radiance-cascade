using UnityEngine;
using UnityEngine.Profiling;

namespace RadianceCascade.Scripts
{
    public class DistanceFieldController : MonoBehaviour
    {
        [SerializeField] private RenderTexture _texture;
        [SerializeField] private Material _jumpMaterial;
        [SerializeField] private Material _sdfMaterial;
        [SerializeField] private Material _jumpUVMaterial;

        [SerializeField] private int _iterations;
        [SerializeField] private StepMode _stepMode;

        public RenderTexture Texture => _texture;

        private Vector4 _renderSize;
        private RenderTexture _tempTex1;
        private RenderTexture _tempTex2;
        private RenderTexture _sceneTexture;

        private bool _isValid;

        private static readonly int JumpDistance = Shader.PropertyToID("_JumpDistance");
        private static readonly int RenderSize = Shader.PropertyToID("_RenderSize");

        public void Initialize(RenderTexture sceneTexture)
        {
            if (_texture == null)
                return;

            _sceneTexture = sceneTexture;
            _texture.Release();

            _texture.width = sceneTexture.width;
            _texture.height = sceneTexture.height;
            
            _tempTex1 = GetTexture(_texture.descriptor);
            _tempTex2 = GetTexture(_texture.descriptor);
            
            _renderSize = new Vector4(1f / _tempTex1.width, 1f / _tempTex1.height, _tempTex1.width, _tempTex1.height);
            _isValid = true;
        }

        public void Release()
        {
            _sceneTexture = null;
            RenderTexture.ReleaseTemporary(_tempTex1);
            RenderTexture.ReleaseTemporary(_tempTex2);
            _isValid = false;
        }

        public void Render()
        {
            if (!_isValid)
                return;

            Profiler.BeginSample("Begin Distance Field");
            Graphics.Blit(Texture2D.blackTexture, _tempTex1);
            Graphics.Blit(_sceneTexture, _tempTex1, _jumpUVMaterial);

            var source = _tempTex1;
            var target = _tempTex2;

            var distance = _tempTex1.height * (_tempTex1.width / _tempTex1.height);
            var jumpDistance = distance / 2;

            var steps = _iterations;
            if (_stepMode == StepMode.Auto)
            {
                steps = Mathf.CeilToInt(Mathf.Log10(distance) / Mathf.Log10(2));
            }

            _jumpMaterial.SetVector(RenderSize, _renderSize);

            for (int i = 0; i < steps - 1; i++)
            {
                _jumpMaterial.SetFloat(JumpDistance, jumpDistance);
                Graphics.Blit(source, target, _jumpMaterial);
                (source, target) = (target, source);

                jumpDistance /= 2;
            }
            //_renderSize = new Vector4(1f / _tempTex1.width, 1f / _tempTex1.height, _tempTex1.width, _tempTex1.height);
            //_sdfMaterial.SetVector(RenderSize, _renderSize);
            Graphics.Blit(target, _texture, _sdfMaterial);
            Profiler.EndSample();
        }

        private RenderTexture GetTexture(RenderTextureDescriptor descriptor)
        {
            int width = Mathf.CeilToInt(descriptor.width);
            int height = Mathf.CeilToInt(descriptor.height);

            var texture = RenderTexture.GetTemporary(width, height, descriptor.depthBufferBits, descriptor.graphicsFormat, descriptor.msaaSamples);
            texture.filterMode = FilterMode.Point;

            return texture;
        }

        private enum StepMode
        {
            Manual,
            Auto
        }
    }
}