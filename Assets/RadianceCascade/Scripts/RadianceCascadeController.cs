using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

namespace RadianceCascade.Scripts
{
    public class RadianceCascadeController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Material _cascadeMaterial;
        [SerializeField] private float _renderLinear;
        [SerializeField] private int _radianceCascades;
        [SerializeField] private int _cascadeIndex;
        [SerializeField] private DistanceFieldController _distanceFieldController;

        private float _renderInterval;
        private int _renderWidth;
        private int _renderHeight;

        private float _radianceLinear;
        private float _radianceInterval;
        private int _radianceWidth;
        private int _radianceHeight;

        private float _errorRate;
        private int _errorX;
        private int _errorY;

        private RenderTexture _sceneTexture;
        private RenderTexture _currentRadianceTexture;
        private RenderTexture _previousRadianceTexture;
        private bool _isValid;

        private static readonly int SceneTexture = Shader.PropertyToID("_SceneTexture");
        private static readonly int DistanceField = Shader.PropertyToID("_DistanceField");
        private static readonly int RenderExtent = Shader.PropertyToID("_RenderExtent");
        private static readonly int CascadeExtent = Shader.PropertyToID("_CascadeExtent");
        private static readonly int CascadeCount = Shader.PropertyToID("_CascadeCount");
        private static readonly int CascadeIndex = Shader.PropertyToID("_CascadeIndex");
        private static readonly int CascadeLinear = Shader.PropertyToID("_CascadeLinear");
        private static readonly int CascadeInterval = Shader.PropertyToID("_CascadeInterval");
        private static readonly int RadianceMap = Shader.PropertyToID("_RadianceMap");
        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        private void OnEnable()
        {
            Initialize();
        }

        private void OnDisable()
        {
            Release();

            Shader.SetGlobalTexture(RadianceMap, Texture2D.blackTexture);
        }

        private void LateUpdate()
        {
            if (!_isValid)
                return;
            
            _radianceInterval += Keyboard.current.upArrowKey.value - Keyboard.current.downArrowKey.value;
            _radianceInterval = Mathf.Clamp(_radianceInterval, Mathf.Sqrt(2f) * 1.0f, 128);

            Render();
        }

        private void Initialize()
        {
            _renderWidth = _camera.pixelWidth;
            _renderHeight = _camera.pixelHeight;

            _radianceCascades = Mathf.CeilToInt(Mathf.Log(Vector2.Distance(Vector2.zero, new Vector2(_renderWidth, _renderHeight)), 4));
            _renderInterval = Vector2.Distance(Vector2.zero, new Vector2(_renderLinear, _renderLinear)) * 0.5f;

            _radianceLinear = PowerOfN(_renderLinear, 2);
            _radianceInterval = MultipleOfN(_renderInterval, 2);

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // FIXES CASCADE RAY/PROBE TRADE-OFF ERROR RATE FOR NON-POW2 RESOLUTIONS: (very important).
            _errorRate = Mathf.Pow(2, _radianceCascades - 1);
            _errorX = Mathf.CeilToInt(_renderWidth / _errorRate);
            _errorY = Mathf.CeilToInt(_renderHeight / _errorRate);
            _renderWidth = Mathf.FloorToInt(_errorX * _errorRate);
            _renderHeight = Mathf.FloorToInt(_errorY * _errorRate);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            _radianceWidth = Mathf.FloorToInt(_renderWidth / _radianceLinear);
            _radianceHeight = Mathf.FloorToInt(_renderHeight / _radianceLinear);
            _cascadeIndex = 0;

            Debug.Log("Render Extent: " + _renderWidth + " : " + _renderHeight);
            Debug.Log("Radiance Extent: " + _radianceWidth + " : " + _radianceHeight);
            Debug.Log("Radiance Interval: " + _radianceInterval);
            Debug.Log("Radiance Linear: " + _radianceLinear);
            Debug.Log("Radiance Cascades: " + _radianceCascades);

            _sceneTexture = RenderTexture.GetTemporary(_renderWidth, _renderHeight, 0, GraphicsFormat.R16G16B16A16_SFloat);

            _currentRadianceTexture = RenderTexture.GetTemporary(_radianceWidth, _radianceHeight);
            _previousRadianceTexture = RenderTexture.GetTemporary(_radianceWidth, _radianceHeight);

            _currentRadianceTexture.filterMode = FilterMode.Bilinear;
            _previousRadianceTexture.filterMode = FilterMode.Bilinear;

            _camera.targetTexture = _sceneTexture;
            _distanceFieldController.Initialize(_sceneTexture);

            _isValid = true;
        }

        private void Release()
        {
            _distanceFieldController.Release();
            RenderTexture.ReleaseTemporary(_sceneTexture);
            RenderTexture.ReleaseTemporary(_currentRadianceTexture);
            RenderTexture.ReleaseTemporary(_previousRadianceTexture);
            _isValid = false;
        }

        private void Render()
        {
            _camera.Render();
            _distanceFieldController.Render();

            Profiler.BeginSample("Begin Radiance");
            Graphics.Blit(Texture2D.blackTexture, _currentRadianceTexture);
            
            _cascadeMaterial.SetTexture(SceneTexture, _sceneTexture);
            _cascadeMaterial.SetTexture(DistanceField, _distanceFieldController.Texture);
            _cascadeMaterial.SetVector(RenderExtent, new Vector4(_renderWidth, _renderHeight));
            _cascadeMaterial.SetVector(CascadeExtent, new Vector4(_radianceWidth, _radianceHeight));
            _cascadeMaterial.SetFloat(CascadeCount, _radianceCascades);
            _cascadeMaterial.SetFloat(CascadeLinear, _radianceLinear);
            _cascadeMaterial.SetFloat(CascadeInterval, _radianceInterval);

            // Loop through all cascades in reverse and merge radiance down the cascades (one pass merge and interval).
            for (var n = _radianceCascades - 1; n >= 0; n--)
            {
                _cascadeMaterial.SetFloat(CascadeIndex, _cascadeIndex + n);

                Graphics.Blit(_previousRadianceTexture, _currentRadianceTexture, _cascadeMaterial);
                Graphics.CopyTexture(_currentRadianceTexture, _previousRadianceTexture);
            }

            Shader.SetGlobalTexture(RadianceMap, _currentRadianceTexture);
            Profiler.EndSample();
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