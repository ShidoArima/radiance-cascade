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
        [SerializeField] private float _radianceIntensity = 1;
        [SerializeField] private float _radianceInterval = 1;
        [SerializeField] private float _linear = 1;
        [SerializeField] private int _extent = 50;

        private Camera _camera;
        private CommandBuffer _buffer;
        private RadianceCascadeRenderer _renderer;

        private Shader _occluderShader;
        private RenderTexture _sceneTexture;

        private Vector2 _scale;

        private static readonly int SceneTexture = Shader.PropertyToID("_SceneTexture");
        private static readonly int RadianceMap = Shader.PropertyToID("_RadianceMap");

        private void Awake()
        {
            _renderer = new RadianceCascadeRenderer();
            _camera = GetComponent<Camera>();
            _buffer = new CommandBuffer();
            _buffer.name = "Radiance Cascade";

            int width = Mathf.FloorToInt(_mainCamera.pixelWidth + _extent * _mainCamera.aspect);
            int height = Mathf.FloorToInt(_mainCamera.pixelHeight + _extent);

            var cascades = Mathf.CeilToInt(Mathf.Log(Vector2.Distance(Vector2.zero, new Vector2(width, height)), 4));

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //FIXES CASCADE RAY/PROBE TRADE-OFF ERROR RATE FOR NON-POW2 RESOLUTIONS: (very important).
            var errorRate = Mathf.Pow(2, cascades - 1);
            var errorX = Mathf.CeilToInt(width / errorRate);
            var errorY = Mathf.CeilToInt(height / errorRate);
            width = Mathf.FloorToInt(errorX * errorRate);
            height = Mathf.FloorToInt(errorY * errorRate);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            _occluderShader = Shader.Find("Hidden/GI/Occluder");
            _sceneTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
            _sceneTexture.filterMode = FilterMode.Point;

            _scale = new Vector2((float) width / _mainCamera.pixelWidth, (float) height / _mainCamera.pixelHeight);
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

            _camera.orthographicSize = _mainCamera.orthographicSize * _scale.y;

            _camera.RenderWithShader(_occluderShader, "Occluder");
            Shader.SetGlobalTexture(SceneTexture, _sceneTexture);
            RenderCascades();
        }

        private void RenderCascades()
        {
            _buffer.Clear();

            int width = _sceneTexture.width;
            int height = _sceneTexture.height;

            _buffer.SetGlobalColor("_AmbientColor", _ambient);
            _buffer.SetGlobalFloat("_RadianceIntensity", _radianceIntensity);
            _buffer.SetGlobalVector("_RadianceMapScale", _scale);

            //Result _RadianceMap global texture to be used
            _renderer.Render(_buffer, width, height, _linearSize, _radianceInterval, _linear);
            Graphics.ExecuteCommandBuffer(_buffer);
        }
    }
}