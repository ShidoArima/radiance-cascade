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

        private Camera _camera;
        private CommandBuffer _buffer;
        private RadianceCascadeRenderer _renderer;

        private Shader _occluderShader;
        private RenderTexture _sceneTexture;

        private static readonly int SceneTexture = Shader.PropertyToID("_SceneTexture");
        private static readonly int RadianceMap = Shader.PropertyToID("_RadianceMap");

        private void Awake()
        {
            _renderer = new RadianceCascadeRenderer();
            _camera = GetComponent<Camera>();
            _buffer = new CommandBuffer();
            _buffer.name = "Radiance Cascade";

            _occluderShader = Shader.Find("Hidden/GI/Occluder");
            _sceneTexture = RenderTexture.GetTemporary(_mainCamera.pixelWidth, _mainCamera.pixelHeight, 0);
            _camera.targetTexture = _sceneTexture;
        }

        private void OnDestroy()
        {
            _renderer?.Dispose();
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                Shader.SetGlobalTexture(RadianceMap, Texture2D.whiteTexture);
                return;
            }

            _camera.orthographicSize = _mainCamera.orthographicSize;

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

            //Result _RadianceMap global texture to be used
            _renderer.Render(_buffer, width, height, _linearSize);

            Graphics.ExecuteCommandBuffer(_buffer);
        }
    }
}