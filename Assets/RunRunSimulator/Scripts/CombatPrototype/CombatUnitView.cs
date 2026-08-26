using System.Collections;
using TMPro;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class CombatUnitView : MonoBehaviour
    {
        public int UnitId { get; private set; }

        private const float BaseYawOffset = 180f;

        private CombatBoard _board;
        private TextMeshPro _label;
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

            if (prefab != null)
            {
                GameObject visual = Instantiate(prefab, transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0f, BaseYawOffset, 0f);
                visual.transform.localScale = Vector3.one * 0.55f;
                _visual = visual.transform;
            }
            else
            {
                GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                capsule.transform.SetParent(transform);
                capsule.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                capsule.transform.localRotation = Quaternion.Euler(0f, BaseYawOffset, 0f);
                capsule.transform.localScale = new Vector3(0.55f, 0.4f, 0.55f);
                capsule.GetComponent<Renderer>().material.color = tint;
                Destroy(capsule.GetComponent<CapsuleCollider>());
                _visual = capsule.transform;
            }

            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.transform.SetParent(transform);
            disc.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            disc.transform.localScale = new Vector3(0.85f, 0.03f, 0.85f);
            disc.GetComponent<Renderer>().material.color = tint;
            Destroy(disc.GetComponent<Collider>());

            GameObject labelObject = new GameObject("Label");
            labelObject.transform.SetParent(transform);
            labelObject.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            labelObject.transform.rotation = Camera.main.transform.rotation;
            _label = labelObject.AddComponent<TextMeshPro>();
            _label.fontSize = 3f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = Color.white;

            transform.position = board.CellToWorld(unit.Cell);

            if (unit is EnemyUnit facingEnemy)
            {
                SetFacingInstant(facingEnemy.Facing);
            }

            RefreshTicks(unit);
        }

        public void SetFacingInstant(Vector2Int facing)
        {
            float yaw = Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg + BaseYawOffset;
            _visual.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        public IEnumerator RotateTo(Vector2Int facing, float duration)
        {
            float yaw = Mathf.Atan2(facing.x, facing.y) * Mathf.Rad2Deg + BaseYawOffset;
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
                _label.text = "0";
                return;
            }

            if (unit is EnemyUnit enemy)
            {
                int finisher = Mathf.Min(unit.Ticks, enemy.Definition.FinisherTicks);
                int guard = unit.Ticks - finisher;
                _label.text = "G" + guard + "·" + finisher;
            }
            else
            {
                _label.text = unit.Ticks.ToString();
            }
        }

        public void SnapTo(Vector3 worldPosition)
        {
            transform.position = worldPosition;
        }

        public IEnumerator MoveTo(Vector3 worldTarget, bool arc, float duration)
        {
            yield return LerpPosition(transform.position, worldTarget, duration, arc ? 0.8f : 0f);
        }

        public IEnumerator LaunchUp(float duration)
        {
            Vector3 start = transform.position;
            Vector3 target = start + new Vector3(0f, 1.3f, 0f);
            yield return LerpPosition(start, target, duration, 0f);
        }

        public IEnumerator LandTo(Vector3 worldTarget, float duration)
        {
            yield return LerpPosition(transform.position, worldTarget, duration, 0.4f);
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
            StartCoroutine(FlashHitRoutine());
        }

        private IEnumerator FlashHitRoutine()
        {
            float half = 0.09f;
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.25f, Mathf.Clamp01(elapsed / half));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.one * Mathf.Lerp(1.25f, 1f, Mathf.Clamp01(elapsed / half));
                yield return null;
            }

            transform.localScale = Vector3.one;
        }

        public void ShowDead()
        {
            gameObject.SetActive(false);
        }
    }
}
