using System.Collections.Generic;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class Beat
    {
        public List<PlannedAction> Actions = new List<PlannedAction>();
    }

    public class Choreography
    {
        public const int MaxActions = 2;

        public List<Beat> Beats;

        public Choreography()
        {
            Beats = new List<Beat> { new Beat() };
        }

        public Beat CurrentBeat => Beats[Beats.Count - 1];

        public int TotalActions
        {
            get
            {
                int total = 0;
                foreach (Beat beat in Beats) total += beat.Actions.Count;
                return total;
            }
        }

        public IEnumerable<PlannedAction> AllActions
        {
            get
            {
                foreach (Beat beat in Beats)
                    foreach (PlannedAction action in beat.Actions)
                        yield return action;
            }
        }

        public bool IsAbilityUsed(int unitId, int abilityIndex)
        {
            foreach (Beat beat in Beats)
                foreach (PlannedAction action in beat.Actions)
                    if (action.UnitId == unitId && action.AbilityIndex == abilityIndex) return true;
            return false;
        }

        public void Add(PlannedAction action) => CurrentBeat.Actions.Add(action);
        public void AddBeat() => Beats.Add(new Beat());

        public PlannedAction UndoLast()
        {
            Beat last = Beats[Beats.Count - 1];
            if (last.Actions.Count == 0)
            {
                if (Beats.Count > 1) Beats.RemoveAt(Beats.Count - 1);
                return null;
            }

            PlannedAction action = last.Actions[last.Actions.Count - 1];
            last.Actions.RemoveAt(last.Actions.Count - 1);
            if (last.Actions.Count == 0 && Beats.Count > 1) Beats.RemoveAt(Beats.Count - 1);
            return action;
        }

        public void Clear() => Beats = new List<Beat> { new Beat() };
    }
}
