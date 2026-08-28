using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class EnemyTurnController : MonoBehaviour
    {
        [SerializeField] private ResolutionAnimator animator;
        [SerializeField] private BoardHighlighter highlighter;

        public void CommitIntents(CombatSimState canonical)
        {
            List<EnemyUnit> enemies = canonical.GetEnemies();
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                enemy.Intent = EnemyBrain.ComputeIntent(canonical, enemy);
            }
        }

        public void PaintIntents(CombatSimState state)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            List<EnemyUnit> enemies = state.GetEnemies();
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                if (enemy.Intent != null && enemy.Intent.HasAttack) cells.AddRange(enemy.Intent.GetAttackCells(enemy.Cell));
            }

            highlighter.Show(HighlightKind.Intent, cells);
        }

        public IEnumerator RunTurn(CombatSimState canonical, Dictionary<int, CombatUnitView> views, CombatBoard board, Action onComplete)
        {
            highlighter.Clear(HighlightKind.Intent);
            List<ResolutionEvent> events = ActionResolver.ResolveEnemyTurn(canonical);
            yield return animator.Play(events, views, board, canonical);
            if (onComplete != null) onComplete();
        }

        public IEnumerator RunReactions(CombatSimState canonical, Dictionary<int, CombatUnitView> views, CombatBoard board, Action onComplete)
        {
            List<ResolutionEvent> events = ActionResolver.ResolveEnemyReactions(canonical);
            yield return animator.Play(events, views, board, canonical);
            if (onComplete != null) onComplete();
        }
    }
}
