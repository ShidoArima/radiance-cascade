using UnityEngine;
using UnityEngine.InputSystem;

namespace Samples
{
    public class ObjectDrag : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private bool _autoFollow;

        private Vector3 _offset;
        private bool _isDragging;

        private void OnMouseDown()
        {
            _offset = transform.position - GetPointerWorldPosition();
            _isDragging = true;
        }

        private void OnMouseUp()
        {
            _offset = Vector3.zero;
            _isDragging = false;
        }

        private void Update()
        {
            if (!_isDragging && !_autoFollow)
                return;

            var pointerPos = GetPointerWorldPosition();
            var newPosition = pointerPos + _offset;
            transform.position = new Vector3(newPosition.x, newPosition.y, transform.position.z);
        }

        private Vector3 GetPointerWorldPosition()
        {
            var pointerScreen = Mouse.current.position.value;
            var pointerWorld = _camera.ScreenToWorldPoint(pointerScreen);
            return pointerWorld;
        }
    }
}