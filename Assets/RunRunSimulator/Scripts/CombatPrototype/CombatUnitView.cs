using System.Collections;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class CombatUnitView : MonoBehaviour
    {
        public int UnitId { get; private set; }

        [SerializeField] private Transform visualMount;
        [SerializeField] private Renderer discRenderer;
        [SerializeField] private TextMeshPro label;
        [SerializeField] private MMF_Player onHit;
        [SerializeField] private Color seedTint = new Color(0.55f, 0.9f, 0.4f);
        [SerializeField] private float baseYawOffset = 180f;
        [SerializeField] private float visualScale = 0.55f;
        [SerializeField] private float launchHeight = 1.3f;
        [SerializeField] private float moveArcHeight = 0.8f;
        [SerializeField] private float landArcHeight = 0.4f;

        private CombatBoard _board;
        private Transform _visual;

        public void Init(CombatUnit unit, CombatBoard board)
        {
            UnitId = unit.Id;
            _board = board;

            GameObject prefab = null;
            Color tint = Color.white;

            if (unit is PlayerUnit player)
            {
                prefab = player.Definition.VisualPrefab;
                tint = player.Definition.Tint;
            }
            else if (unit is EnemyUnit enemy)
            {
                prefab = enemy.Definition.VisualPrefab;
                tint = enemy.Definition.Tint;
            }
            else if (unit is SeedUnit)
            {
                tint = seedTint;
            }

            if (prefab != null)
            {
                GameObject visual = Instantiate(prefab, visualMount);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0f, baseYawOffset, 0f);
                visual.transform.localScale = Vector3.one * visualScale;
                _visual = visual.transform;
            }
            else
            {
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.transform.SetParent(visualMount);
                capsule.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                capsule.transform.localRotation = Quaternion.Euler(0f, baseYawOffset, 0f);
                capsule.transform.localScale = new Vector3(0.55f, 0.4f, 0.55f);
                capsule.GetComponent<Renderer>().material.color = tint;
                Destroy(capsule.GetComponent<CapsuleCollider>());
                _visual = capsule.transform;
            }

            discRenderer.material.color = tint;
            label.transform.rotation = Camera.main.transform.rotation;

            transform.position = board.CellToWorld(unit.Cell);

            if (unit is EnemyUnit facingEnemy)
            {
                SetFacingInstant(facingEnemy.Facing);
            }

            RefreshTicks(unit);
        }

        public void SetFacingInstant(Vector2Int facing)
        {
            float yaw = Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg + baseYawOffset;
            _visual.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        public IEnumerator RotateTo(Vector2Int facing, float duration)
        {
            float yaw = Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg + baseYawOffset;
            Quaternion target = Quaternion.Euler(0f, yaw, 0f);
            Quaternion start = _visual.localRotation;

            if (duration <= 0f)
            {
                _visual.localRotation = target;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                _visual.localRotation = Quaternion.Slerp(start, target, normalized);
                yield return null;
            }

            _visual.localRotation = target;
        }

        public void RefreshTicks(CombatUnit unit)
        {
            if (unit.Ticks <= 0)
            {
                label.text = "0";
                return;
            }

            if (unit is EnemyUnit enemy)
            {
                int finisher = Mathf.Min(unit.Ticks, enemy.Definition.FinisherTicks);
                int guard = unit.Ticks - finisher;
                label.text = "G" + guard + "·" + finisher;
            }
            else
            {
                label.text = unit.Ticks.ToString();
            }
        }

        public void SnapTo(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        public IEnumerator MoveTo(Vector3 worldTarget, bool arc, float duration)
        {
            yield return LerpPosition(transform.position, worldTarget, duration, arc ? moveArcHeight : 0f);
        }

        public IEnumerator LaunchUp(float duration)
        {
            Vector3 start = transform.position;
            Vector3 target = start + new Vector3(0f, launchHeight, 0f);
            yield return LerpPosition(start, target, duration, 0f);
        }

        public IEnumerator LandTo(Vector3 worldTarget, float duration)
        {
            yield return LerpPosition(transform.position, worldTarget, duration, landArcHeight);
        }

        private IEnumerator LerpPosition(Vector3 start, Vector3 target, float duration, float arcHeight)
        {
            if (duration <= 0f)
            {
                transform.position = target;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalized = Mathf.Clamp01(elapsed / duration);
                Vector3 position = Vector3.Lerp(start, target, normalized);
                if (arcHeight > 0f)
                {
                    position.y += 4f * arcHeight * normalized * (1f - normalized);
                }
                transform.position = position;
                yield return null;
            }

            transform.position = target;
        }

        public void FlashHit()
        {
            if (onHit != null) onHit.PlayFeedbacks();
        }

        public void ShowDead()
        {
            gameObject.SetActive(false);
        }
    }
}
