using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class CombatCameraController : MonoBehaviour
    {
        [SerializeField] private CombatBoardBuilder builder;
        [SerializeField] private float rotateDuration = 0.35f;
        [SerializeField] private float zoomStep = 0.15f;
        [SerializeField] private float minZoom = 0.5f;
        [SerializeField] private float maxZoom = 1.6f;
        [SerializeField] private float zoomDuration = 0.25f;

        private Vector3 _pivot;
        private Vector3 _baseOffset;
        private Quaternion _baseRotation;
        private float _yaw;
        private float _targetYaw;
        private float _zoom;
        private float _targetZoom;

        private void Start()
        {
            _pivot = new Vector3(
                builder.Board.Width * CombatBoard.CellSize * 0.5f,
                0f,
                builder.Board.Depth * CombatBoard.CellSize * 0.5f);

            _baseOffset = transform.position - _pivot;
            _baseRotation = transform.rotation;
            _yaw = 0f;
            _targetYaw = 0f;
            _zoom = 1f;
            _targetZoom = 1f;
        }

        private void Update()
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;

            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame) _targetYaw -= 90f;
                if (kb.rightArrowKey.wasPressedThisFrame) _targetYaw += 90f;
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.01f) _targetZoom = Mathf.Clamp(_targetZoom - zoomStep, minZoom, maxZoom);
                else if (scroll < -0.01f) _targetZoom = Mathf.Clamp(_targetZoom + zoomStep, minZoom, maxZoom);
            }

            _zoom = Mathf.MoveTowards(_zoom, _targetZoom, ((maxZoom - minZoom) / zoomDuration) * Time.deltaTime);

            _yaw = Mathf.MoveTowardsAngle(_yaw, _targetYaw, (90f / rotateDuration) * Time.deltaTime);

            Quaternion rot = Quaternion.Euler(0f, _yaw, 0f);
            transform.position = _pivot + rot * (_baseOffset * _zoom);
            transform.rotation = rot * _baseRotation;
        }
    }
}
