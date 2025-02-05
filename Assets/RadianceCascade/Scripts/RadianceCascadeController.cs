using UnityEngine;

namespace RadianceCascade.Scripts
{
    public class RadianceCascadeController : MonoBehaviour
    {
        private int _extent;
        private int _angular;
        private int _interval;
        private int _spacing;

        private float _boost;
        private float _decay;


        private void Initialize(int extent, int angular = 4, int interval = 4, int spacing = 4, float boost = 1f, float decayRate = 0.65f)
        {
            _extent = extent;
            _angular = angular;
            _interval = interval;
            _spacing = spacing;
            _boost = boost;
            _decay = decayRate;
        }

        private int MultipleOf2(int number)
        {
            return (number + (2 - 1)) & ~(2 - 1);
        }

        private float PowerOf4(float number)
        {
            return Mathf.Pow(Mathf.Ceil(Mathf.Log(number, 4)), 4);
        }

        private float PowerOf2(float number)
        {
            return Mathf.Pow(Mathf.Ceil(Mathf.Log(number, 2)), 2);
        }
    }
}