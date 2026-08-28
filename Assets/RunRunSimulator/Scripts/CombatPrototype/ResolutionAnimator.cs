using System.Collections;
using System.Collections.Generic;
using MoreMountains.Feedbacks;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class ResolutionAnimator : MonoBehaviour
    {
        [SerializeField] private BoardImpactFeedback impact;
        [SerializeField] private MMF_Player fizzleFeedback;
        [SerializeField] private float moveDuration = 0.35f;
        [SerializeField] private float pushDuration = 0.25f;
        [SerializeField] private float landDuration = 0.3f;
        [SerializeField] private float rotateDuration = 0.2f;
        [SerializeField] private float hitPause = 0.15f;
        [SerializeField] private float wavePause = 0.1f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private float projectileSpeed = 12f;
        [SerializeField] private float projectileHeight = 0.6f;

        public IEnumerator Play(List<ResolutionEvent> events, Dictionary<int, CombatUnitView> views, CombatBoard board, CombatSimState state)
        {
            SortedDictionary<int, List<ResolutionEvent>> waves = GroupByWave(events);

            foreach (KeyValuePair<int, List<ResolutionEvent>> wave in waves)
            {
                yield return PlayWave(wave.Value, views, board, state);
            }

            SnapAll(views, board, state);
        }

        private static SortedDictionary<int, List<ResolutionEvent>> GroupByWave(List<ResolutionEvent> events)
        {
            SortedDictionary<int, List<ResolutionEvent>> waves = new SortedDictionary<int, List<ResolutionEvent>>();

            for (int i = 0; i < events.Count; i++)
            {
                ResolutionEvent evt = events[i];
                if (!waves.TryGetValue(evt.Wave, out List<ResolutionEvent> wave))
                {
                    wave = new List<ResolutionEvent>();
                    waves[evt.Wave] = wave;
                }
                wave.Add(evt);
            }

            return waves;
        }

        private IEnumerator PlayWave(List<ResolutionEvent> waveEvents, Dictionary<int, CombatUnitView> views, CombatBoard board, CombatSimState state)
        {
            yield return PlayMovementPhase(waveEvents, views, board);

            for (int i = 0; i < waveEvents.Count; i++)
            {
                ResolutionEvent evt = waveEvents[i];
                if (evt.Type == ResolutionEventType.Impact || evt.Type == ResolutionEventType.EnemyAttack)
                {
                    yield return PlayAttackPresentation(evt, views, board);
                }
            }

            if (impact != null)
            {
                for (int i = 0; i < waveEvents.Count; i++)
                {
                    ResolutionEvent evt = waveEvents[i];
                    if (evt.Type == ResolutionEventType.Land)
                    {
                        impact.ShakeAt(evt.To);
                    }
                    else if (evt.Type == ResolutionEventType.Impact)
                    {
                        for (int c = 0; c < evt.Cells.Count; c++) impact.ShakeAt(evt.Cells[c]);
                    }
                }
            }

            yield return PlaySequentialPhase(waveEvents, views, state, board);
            yield return new WaitForSeconds(wavePause);
        }

        private IEnumerator PlayMovementPhase(List<ResolutionEvent> waveEvents, Dictionary<int, CombatUnitView> views, CombatBoard board)
        {
            int[] counter = new int[1];

            for (int i = 0; i < waveEvents.Count; i++)
            {
                ResolutionEvent evt = waveEvents[i];
                if (!IsMovementType(evt.Type)) continue;
                if (!views.TryGetValue(evt.UnitId, out CombatUnitView view)) continue;

                StartCoroutine(Tracked(PlayMovementEvent(view, evt, board), counter));
            }

            while (counter[0] > 0) yield return null;
        }

        private IEnumerator PlayMovementEvent(CombatUnitView view, ResolutionEvent evt, CombatBoard board)
        {
            switch (evt.Type)
            {
                case ResolutionEventType.Move:
                    yield return view.MoveTo(board.CellToWorld(evt.To), true, moveDuration);
                    break;
                case ResolutionEventType.Push:
                    yield return view.MoveTo(board.CellToWorld(evt.To), false, pushDuration);
                    break;
                case ResolutionEventType.Launch:
                    yield return view.LaunchUp(moveDuration);
                    break;
                case ResolutionEventType.Land:
                    yield return view.LandTo(board.CellToWorld(evt.To), landDuration);
                    break;
            }
        }

        private IEnumerator Tracked(IEnumerator inner, int[] counter)
        {
            counter[0]++;
            yield return inner;
            counter[0]--;
        }

        private static bool IsMovementType(ResolutionEventType type)
        {
            return type == ResolutionEventType.Move
                || type == ResolutionEventType.Push
                || type == ResolutionEventType.Launch
                || type == ResolutionEventType.Land;
        }

        private IEnumerator PlayAttackPresentation(ResolutionEvent evt, Dictionary<int, CombatUnitView> views, CombatBoard board)
        {
            int attackerId = evt.Type == ResolutionEventType.EnemyAttack ? evt.SourceId : evt.UnitId;
            if (!views.TryGetValue(attackerId, out CombatUnitView view)) yield break;

            if (evt.Type == ResolutionEventType.Impact && evt.Facing != Vector2Int.zero)
            {
                yield return view.RotateTo(evt.Facing, rotateDuration);
            }

            if (evt.Projectile && evt.Cells != null && evt.Cells.Count > 0)
            {
                Vector3 origin = view.transform.position + Vector3.up * projectileHeight;
                Vector3 destination = board.CellToWorld(evt.Cells[evt.Cells.Count - 1]) + Vector3.up * projectileHeight;

                GameObject projectile;
                if (projectilePrefab != null)
                {
                    projectile = Instantiate(projectilePrefab, origin, Quaternion.identity);
                }
                else
                {
                    projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    projectile.transform.position = origin;
                    projectile.transform.localScale = Vector3.one * 0.22f;
                    Destroy(projectile.GetComponent<Collider>());
                }

                float distance = Vector3.Distance(origin, destination);
                float duration = Mathf.Max(distance / projectileSpeed, 0.05f);
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    projectile.transform.position = Vector3.Lerp(origin, destination, elapsed / duration);
                    yield return null;
                }

                projectile.transform.position = destination;
                Destroy(projectile);
            }
        }

        private IEnumerator PlaySequentialPhase(List<ResolutionEvent> waveEvents, Dictionary<int, CombatUnitView> views, CombatSimState state, CombatBoard board)
        {
            for (int i = 0; i < waveEvents.Count; i++)
            {
                ResolutionEvent evt = waveEvents[i];

                if (evt.Type == ResolutionEventType.Hit)
                {
                    CombatUnit unit = state.GetUnit(evt.UnitId);
                    if (views.TryGetValue(evt.UnitId, out CombatUnitView view))
                    {
                        view.FlashHit();
                        if (unit != null) view.RefreshTicks(unit);
                    }
                    if (evt.Environmental && impact != null && unit != null) impact.ShakeAt(unit.Cell);
                    yield return new WaitForSeconds(hitPause);
                }
                else if (evt.Type == ResolutionEventType.EnemyAttack)
                {
                    if (views.TryGetValue(evt.SourceId, out CombatUnitView attackerView)) attackerView.FlashHit();
                    yield return new WaitForSeconds(hitPause);
                }
                else if (evt.Type == ResolutionEventType.Die)
                {
                    if (views.TryGetValue(evt.UnitId, out CombatUnitView view)) view.ShowDead();
                }
                else if (evt.Type == ResolutionEventType.Rotate)
                {
                    if (views.TryGetValue(evt.UnitId, out CombatUnitView view)) yield return view.RotateTo(evt.Facing, rotateDuration);
                }
                else if (evt.Type == ResolutionEventType.Fizzle)
                {
                    if (impact != null) impact.ShakeAt(evt.To);
                    if (fizzleFeedback != null) fizzleFeedback.PlayFeedbacks(board.CellToWorld(evt.To));
                    yield return new WaitForSeconds(hitPause);
                }
            }
        }

        private void SnapAll(Dictionary<int, CombatUnitView> views, CombatBoard board, CombatSimState state)
        {
            foreach (KeyValuePair<int, CombatUnitView> pair in views)
            {
                CombatUnit unit = state.GetUnit(pair.Key);
                if (unit == null || !unit.Alive || unit.Airborne) continue;

                pair.Value.SnapTo(board.CellToWorld(unit.Cell));
                pair.Value.RefreshTicks(unit);
            }
        }
    }
}
