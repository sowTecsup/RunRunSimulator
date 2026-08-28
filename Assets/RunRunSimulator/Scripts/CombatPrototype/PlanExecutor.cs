using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class PlanExecutor : MonoBehaviour
    {
        [SerializeField] private ResolutionAnimator animator;

        public IEnumerator ExecuteChoreography(CombatSimState canonical, Choreography plan, Dictionary<int, CombatUnitView> views, CombatBoard board, Action<List<ResolutionEvent>> onComplete)
        {
            List<ResolutionEvent> allEvents = new List<ResolutionEvent>();

            for (int i = 0; i < plan.Beats.Count; i++)
            {
                Beat beat = plan.Beats[i];
                List<ResolutionEvent> events = ActionResolver.ResolveBeat(canonical, beat);
                allEvents.AddRange(events);
                yield return animator.Play(events, views, board, canonical);
            }

            if (onComplete != null) onComplete(allEvents);
        }
    }
}
