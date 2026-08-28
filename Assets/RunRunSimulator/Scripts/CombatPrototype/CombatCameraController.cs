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
        [SerializeField] private float panSpeed = 7f;
        [SerializeField] private float framePadding = 1.05f;
        [SerializeField] private float topBandFraction = 0.12f;
        [SerializeField] private float bottomBandFraction = 0.30f;
        [SerializeField] private float pivotHeight = 0f;
        [SerializeField] private float perspectiveFill = 1.3f;

        private Vector3 _pivot;
        private Vector3 _baseOffset;
        private Quaternion _baseRotation;
        private float _yaw;
        private float _targetYaw;
        private float _zoom;
        private float _targetZoom;
        private Vector2 _islandMin;
        private Vector2 _islandMax;
        private Camera _cam;
        private float _baseOrthoSize;

        private void Start()
        {
            _cam = GetComponent<Camera>();
            CombatBoard board = builder != null ? builder.Board : null;
            bool framed = false;

            if (board != null)
            {
                float minX = float.PositiveInfinity;
                float maxX = float.NegativeInfinity;
                float minZ = float.PositiveInfinity;
                float maxZ = float.NegativeInfinity;
                float maxElevationHeight = 0f;
                bool anyCell = false;

                for (int x = 0; x < board.Width; x++)
                {
                    for (int z = 0; z < board.Depth; z++)
                    {
                        Vector2Int cell = new Vector2Int(x, z);
                        if (!board.InBounds(cell)) continue;

                        anyCell = true;
                        float cellX = x * CombatBoard.CellSize + CombatBoard.CellSize * 0.5f;
                        float cellZ = z * CombatBoard.CellSize + CombatBoard.CellSize * 0.5f;
                        if (cellX < minX) minX = cellX;
                        if (cellX > maxX) maxX = cellX;
                        if (cellZ < minZ) minZ = cellZ;
                        if (cellZ > maxZ) maxZ = cellZ;

                        float elevationHeight = board.GetElevation(cell) * board.LevelHeight;
                        if (elevationHeight > maxElevationHeight) maxElevationHeight = elevationHeight;
                    }
                }

                if (anyCell)
                {
                    minX -= CombatBoard.CellSize * 0.5f;
                    maxX += CombatBoard.CellSize * 0.5f;
                    minZ -= CombatBoard.CellSize * 0.5f;
                    maxZ += CombatBoard.CellSize * 0.5f;

                    _islandMin = new Vector2(minX, minZ);
                    _islandMax = new Vector2(maxX, maxZ);

                    float centroX = (minX + maxX) * 0.5f;
                    float centroZ = (minZ + maxZ) * 0.5f;
                    float centroY = (maxElevationHeight - 0.3f) * 0.5f + pivotHeight;
                    _pivot = new Vector3(centroX, centroY, centroZ);

                    float halfW = (maxX - minX) * 0.5f;
                    float halfD = (maxZ - minZ) * 0.5f;
                    float halfH = maxElevationHeight * 0.5f;
                    float radius = Mathf.Sqrt(halfW * halfW + halfD * halfD + halfH * halfH);

                    Vector3 dir = transform.position - _pivot;
                    dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.back;

                    _baseRotation = Quaternion.LookRotation(-dir);

                    float freeFraction = Mathf.Clamp(1f - topBandFraction - bottomBandFraction, 0.2f, 1f);

                    Vector3 frameUp = _baseRotation * Vector3.up;
                    Vector3 frameRight = _baseRotation * Vector3.right;

                    float hU = 0f;
                    float hR = 0f;
                    float half = CombatBoard.CellSize * 0.5f;
                    for (int x = 0; x < board.Width; x++)
                    {
                        for (int z = 0; z < board.Depth; z++)
                        {
                            Vector2Int cell = new Vector2Int(x, z);
                            if (!board.InBounds(cell)) continue;

                            float cellX = x * CombatBoard.CellSize + half;
                            float cellZ = z * CombatBoard.CellSize + half;
                            float top = board.GetElevation(cell) * board.LevelHeight;
                            for (int ix = -1; ix <= 1; ix += 2)
                            {
                                for (int iz = -1; iz <= 1; iz += 2)
                                {
                                    Vector3 corner = new Vector3(cellX + ix * half, top, cellZ + iz * half);
                                    Vector3 rel = corner - _pivot;
                                    float u = Mathf.Abs(Vector3.Dot(rel, frameUp));
                                    float r = Mathf.Abs(Vector3.Dot(rel, frameRight));
                                    if (u > hU) hU = u;
                                    if (r > hR) hR = r;
                                }
                            }

                            Vector3 peak = new Vector3(cellX, top + 1.6f, cellZ);
                            Vector3 basePoint = new Vector3(cellX, -0.3f, cellZ);
                            float uPeak = Mathf.Abs(Vector3.Dot(peak - _pivot, frameUp));
                            float uBase = Mathf.Abs(Vector3.Dot(basePoint - _pivot, frameUp));
                            if (uPeak > hU) hU = uPeak;
                            if (uBase > hU) hU = uBase;
                        }
                    }

                    float aspect = _cam != null ? _cam.aspect : 1.78f;

                    if (_cam != null && _cam.orthographic)
                    {
                        _baseOrthoSize = Mathf.Max(hU * framePadding / freeFraction, hR * framePadding / aspect);

                        float bandShift = (bottomBandFraction + freeFraction * 0.5f - 0.5f) * 2f * _baseOrthoSize;
                        _baseOffset = dir * (radius * 2f) - frameUp * bandShift;
                    }
                    else
                    {
                        float fov = _cam != null ? _cam.fieldOfView : 60f;
                        float tanV = Mathf.Tan(fov * freeFraction * 0.5f * Mathf.Deg2Rad);
                        float tanH = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * aspect;
                        float distance = Mathf.Max(hU * framePadding / tanV, hR * framePadding / tanH) / Mathf.Max(perspectiveFill, 0.1f);
                        float visibleHalf = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad) * distance;
                        float bandShift = (bottomBandFraction + freeFraction * 0.5f - 0.5f) * 2f * visibleHalf;
                        _baseOffset = dir * distance - frameUp * bandShift;
                    }

                    framed = true;
                }
            }

            if (!framed)
            {
                float width = board != null ? board.Width : 0;
                float depth = board != null ? board.Depth : 0;
                _pivot = new Vector3(
                    width * CombatBoard.CellSize * 0.5f,
                    0f,
                    depth * CombatBoard.CellSize * 0.5f);

                _islandMin = new Vector2(0f, 0f);
                _islandMax = new Vector2(width * CombatBoard.CellSize, depth * CombatBoard.CellSize);

                _baseOffset = transform.position - _pivot;
                _baseRotation = transform.rotation;
            }

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

                Vector3 pan = Vector3.zero;
                if (kb.wKey.isPressed) pan.z += 1f;
                if (kb.sKey.isPressed) pan.z -= 1f;
                if (kb.aKey.isPressed) pan.x -= 1f;
                if (kb.dKey.isPressed) pan.x += 1f;
                if (pan != Vector3.zero)
                {
                    Vector3 forward = transform.forward;
                    forward.y = 0f;
                    if (forward.sqrMagnitude < 0.0001f) forward = transform.up;
                    forward.y = 0f;
                    forward.Normalize();
                    Vector3 right = transform.right;
                    right.y = 0f;
                    right.Normalize();
                    Vector3 move = right * pan.x + forward * pan.z;
                    _pivot += move.normalized * panSpeed * Time.deltaTime;
                    _pivot.x = Mathf.Clamp(_pivot.x, _islandMin.x, _islandMax.x);
                    _pivot.z = Mathf.Clamp(_pivot.z, _islandMin.y, _islandMax.y);
                }
            }

            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouse != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (scroll > 0.01f) _targetZoom = Mathf.Clamp(_targetZoom - zoomStep, minZoom, maxZoom);
                else if (scroll < -0.01f) _targetZoom = Mathf.Clamp(_targetZoom + zoomStep, minZoom, maxZoom);
            }

            _zoom = Mathf.MoveTowards(_zoom, _targetZoom, ((maxZoom - minZoom) / zoomDuration) * Time.deltaTime);

            if (_cam != null && _cam.orthographic && _baseOrthoSize > 0f) _cam.orthographicSize = _baseOrthoSize * _zoom;

            _yaw = Mathf.MoveTowardsAngle(_yaw, _targetYaw, (90f / rotateDuration) * Time.deltaTime);

            Quaternion rot = Quaternion.Euler(0f, _yaw, 0f);
            transform.position = _pivot + rot * (_baseOffset * _zoom);
            transform.rotation = rot * _baseRotation;
        }
    }
}
