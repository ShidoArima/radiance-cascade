using Shidon.RadianceCascade.Renderers;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shidon.RadianceCascade
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(Camera))]
    public class RadianceCascadeCameraRenderer : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private float _linearSize = 2;
        [SerializeField] private Color _ambient = Color.black;
        [SerializeField] float _gamma = 2;
        [SerializeField] private float _radianceIntensity = 1;
        [SerializeField] private float _radianceInterval = 1;
        [SerializeField] private float _linear = 1;
        [SerializeField] private float _extent;

        [SerializeField] private Texture2D _noiseTexture;
        [SerializeField] private Vector2 _noiseScale;

        private Camera _camera;
        private CommandBuffer _buffer;
        private RadianceCascadeRenderer _renderer;

        private Shader _occluderShader;
        private RenderTexture _sceneTexture;

        private static readonly int SceneTexture = Shader.PropertyToID("_SceneTexture");
        private static readonly int RadianceMap = Shader.PropertyToID("_RadianceMap");
        private static readonly int NoiseMap = Shader.PropertyToID("_NoiseMap");
        private static readonly int NoiseScale = Shader.PropertyToID("_NoiseScale");

        private void Awake()
        {
            _renderer = new RadianceCascadeRenderer();
            _camera = GetComponent<Camera>();
            _buffer = new CommandBuffer();
            _buffer.name = "Radiance Cascade";

            _occluderShader = Shader.Find("Hidden/GI/Occluder");
            _sceneTexture = RenderTexture.GetTemporary(_mainCamera.pixelWidth, _mainCamera.pixelHeight, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            _sceneTexture.filterMode = FilterMode.Point;

            _camera.targetTexture = _sceneTexture;
        }

        private void OnEnable()
        {
            Shader.SetGlobalTexture(RadianceMap, Texture2D.whiteTexture);
            Camera.onPreRender += OnCameraPreRender;
        }

        private void OnDisable()
        {
            Camera.onPreRender -= OnCameraPreRender;
            Shader.SetGlobalTexture(RadianceMap, Texture2D.whiteTexture);
        }

        private void OnDestroy()
        {
            _renderer?.Dispose();
        }

        private void OnCameraPreRender(Camera currentCamera)
        {
            if (!Application.isPlaying)
            {
                Shader.SetGlobalTexture(RadianceMap, Texture2D.whiteTexture);
                return;
            }

            if (currentCamera != _mainCamera)
                return;

            _camera.orthographicSize = _mainCamera.orthographicSize * (1 + _extent);

            _camera.RenderWithShader(_occluderShader, "Occluder");
            Shader.SetGlobalTexture(SceneTexture, _sceneTexture);
            RenderCascades();
        }

        private void RenderCascades()
        {
            _buffer.Clear();

            int width = _camera.pixelWidth;
            int height = _camera.pixelHeight;

            _buffer.SetGlobalColor("_AmbientColor", _ambient);
            _buffer.SetGlobalFloat("_Gamma", _gamma);
            _buffer.SetGlobalFloat("_RadianceIntensity", _radianceIntensity);
            _buffer.SetGlobalFloat("_RadianceMapScale", 1 + _extent);

            _buffer.SetGlobalTexture(NoiseMap, _noiseTexture);
            _buffer.SetGlobalVector(NoiseScale, _noiseScale);

            //Result _RadianceMap global texture to be used
            _renderer.Render(_buffer, width, height, _linearSize, _radianceInterval, _linear);
            Graphics.ExecuteCommandBuffer(_buffer);
        }
    }
}