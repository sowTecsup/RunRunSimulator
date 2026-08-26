using System.Collections.Generic;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class ProjectionResult
    {
        public List<CombatSimState> StateAfterBeat = new List<CombatSimState>();
        public CombatSimState EndOfBeatsState;
        public CombatSimState FinalState;
        public List<List<ResolutionEvent>> BeatEvents = new List<List<ResolutionEvent>>();
        public List<ResolutionEvent> EnemyTurnEvents;
    }

    public static class PlanProjection
    {
        public static ProjectionResult Project(CombatSimState canonical, Choreography plan)
        {
            ProjectionResult result = new ProjectionResult();
            CombatSimState sim = canonical.Clone();

            for (int i = 0; i < plan.Beats.Count; i++)
            {
                Beat beat = plan.Beats[i];
                result.BeatEvents.Add(ActionResolver.ResolveBeat(sim, beat));
                result.StateAfterBeat.Add(sim.Clone());
            }

            result.EndOfBeatsState = sim.Clone();
            result.EnemyTurnEvents = ActionResolver.ResolveEnemyTurn(sim);
            result.FinalState = sim;

            return result;
        }
    }
}
