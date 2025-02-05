using UnityEngine;

namespace RadianceCascade.Scripts
{
    [ExecuteInEditMode]
    public class SDF : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private RenderTexture _texture;
        [SerializeField] private Material _jumpMaterial;
        [SerializeField] private Material _sdfMaterial;
        [SerializeField] private Shader _jumpUVShader;

        [SerializeField] private int _iterations;

        private RenderTexture _tempTex;
        private bool _isValid;
        private static readonly int JumpDistance = Shader.PropertyToID("_JumpDistance");

        private void OnEnable()
        {
            if (_camera == null || _texture == null)
                return;

            _texture.Release();
            _texture.width = Screen.width;
            _texture.height = Screen.height;
            _tempTex = RenderTexture.GetTemporary(_texture.descriptor);
            _isValid = true;
        }

        private void OnDisable()
        {
            RenderTexture.ReleaseTemporary(_tempTex);
            _isValid = false;
        }

        private void Update()
        {
            if (!_isValid)
                return;

            _camera.targetTexture = _tempTex;
            _camera.RenderWithShader(_jumpUVShader, string.Empty);
            _camera.targetTexture = null;

            var source = _tempTex;
            var target = _texture;

            var jumpDistance = _texture.height / 2;
            var steps = Mathf.Ceil(Mathf.Log10(_texture.height) / Mathf.Log10(2));

            for (int i = 0; i < _iterations; i++)
            {
                _jumpMaterial.SetFloat(JumpDistance, jumpDistance);
                Graphics.Blit(source, target, _jumpMaterial);
                (source, target) = (target, source);

                jumpDistance /= 2;
            }

            if (target == _tempTex)
            {
                Graphics.Blit(_texture, _tempTex);
            }

            Graphics.Blit(_tempTex, _texture, _sdfMaterial);
        }
    }
}