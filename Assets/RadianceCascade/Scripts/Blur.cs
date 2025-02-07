using UnityEngine;

namespace RadianceCascade.Scripts
{
    public class Blur : MonoBehaviour
    {
        [SerializeField] private int _blurIterations = 3;
        [SerializeField] private float _blurSpread = 0.6f;

        private Material _blurMaterial = null;
        
        private void OnEnable()
        {
            _blurMaterial = new Material(Shader.Find("Hidden/BlurConeTap"));
        }

        private void OnDisable()
        {
            Destroy(_blurMaterial);
        }

        private void FourTapCone(RenderTexture source, RenderTexture dest, int iteration)
        {
            float off = 0.5f + iteration * _blurSpread;
            Graphics.BlitMultiTap(source, dest, _blurMaterial,
                new Vector2(-off, -off),
                new Vector2(-off, off),
                new Vector2(off, off),
                new Vector2(off, -off)
            );
        }

        public void PerformBlur(RenderTexture source, RenderTexture dest)
        {
            if (_blurIterations == 0)
                return;

            int rtW = source.width / 4;
            int rtH = source.height / 4;
            RenderTexture buffer = RenderTexture.GetTemporary(rtW, rtH, 0);

            DownSample4X(source, buffer);

            // Blur the small texture
            for (int i = 0; i < _blurIterations; i++)
            {
                RenderTexture buffer2 = RenderTexture.GetTemporary(rtW, rtH, 0);
                FourTapCone(buffer, buffer2, i);
                RenderTexture.ReleaseTemporary(buffer);
                buffer = buffer2;
            }

            Graphics.Blit(buffer, dest);
        }

        private void DownSample4X(RenderTexture source, RenderTexture dest)
        {
            float off = 1.0f;
            Graphics.BlitMultiTap(source, dest, _blurMaterial,
                new Vector2(-off, -off),
                new Vector2(-off, off),
                new Vector2(off, off),
                new Vector2(off, -off)
            );
        }
    }
}