using UnityEngine;

namespace Shidon.RadianceCascade
{
    [ExecuteInEditMode]
    public class OccluderCamera : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Camera _mainCamera;

        private Shader _occluderShader;
        private RenderTexture _sceneTexture;
        
        private static readonly int SceneTexture = Shader.PropertyToID("_SceneTexture");

        private void OnEnable()
        {
            _occluderShader = Shader.Find("Hidden/GI/Occluder");
            _sceneTexture = RenderTexture.GetTemporary(_mainCamera.pixelWidth, _mainCamera.pixelHeight, 0);
            _camera.targetTexture = _sceneTexture;
        }

        private void OnDisable()
        {
            RenderTexture.ReleaseTemporary(_sceneTexture);
        }

        private void LateUpdate()
        {
            RefreshCamera();
            _camera.RenderWithShader(_occluderShader, "Occluder");
            Shader.SetGlobalTexture(SceneTexture, _sceneTexture);
        }

        private void RefreshCamera()
        {
            _camera.cullingMask = _mainCamera.cullingMask;
            _camera.orthographicSize = _mainCamera.orthographicSize;
        }
    }
}