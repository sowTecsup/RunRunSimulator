using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class TargetingController : MonoBehaviour
    {
        [SerializeField] private BoardHighlighter highlighter;

        public event System.Action SelectionChanged;

        public int SelectedUnitId { get; private set; } = -1;
        public int SelectedAbilityIndex { get; private set; } = -1;
        public Vector2Int CursorCell { get; private set; }
        public Vector2Int CurrentDirection { get; private set; } = new Vector2Int(1, 0);
        public bool AwaitingSlamCell { get { return pendingSlamTarget != null; } }

        private CombatSimState projectedState;
        private Vector2Int? pendingSlamTarget;

        public void SetProjectedState(CombatSimState state)
        {
            projectedState = state;
            RefreshHighlights();
        }

        public void SelectUnit(int slotIndex)
        {
            if (projectedState == null)
                return;

            List<PlayerUnit> players = projectedState.GetPlayers();
            if (slotIndex < 0 || slotIndex >= players.Count)
                return;

            SelectedUnitId = players[slotIndex].Id;
            SelectedAbilityIndex = -1;
            pendingSlamTarget = null;
            RefreshHighlights();
            SelectionChanged?.Invoke();
        }

        public void SelectAbility(int abilityIndex)
        {
            if (SelectedUnitId < 0)
                return;

            SelectedAbilityIndex = abilityIndex;
            pendingSlamTarget = null;
            RefreshHighlights();
            SelectionChanged?.Invoke();
        }

        public void SetCursor(Vector2Int cell)
        {
            if (projectedState == null)
                return;

            CombatBoard board = projectedState.Board;
            CursorCell = new Vector2Int(Mathf.Clamp(cell.x, 0, board.Width - 1), Mathf.Clamp(cell.y, 0, board.Depth - 1));

            CombatAbilitySO ability = GetSelectedAbility();
            CombatUnit unit = ability != null ? projectedState.GetUnit(SelectedUnitId) : null;
            if (ability != null && ability.Type == AbilityType.Attack && ability.Targeting == TargetingMode.DirectionalTemplate && unit != null && CursorCell != unit.Cell)
                CurrentDirection = AbilityTargeting.DominantCardinal(unit.Cell, CursorCell);

            RefreshHighlights();
        }

        public void MoveCursor(Vector2Int delta)
        {
            SetCursor(CursorCell + delta);
        }

        public void Rotate(int steps)
        {
            Vector2Int[] order = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };
            int index = 0;
            for (int i = 0; i < order.Length; i++)
                if (order[i] == CurrentDirection)
                    index = i;

            int next = ((index + steps) % order.Length + order.Length) % order.Length;
            CurrentDirection = order[next];
            RefreshHighlights();
        }

        public PlannedAction TryConfirm()
        {
            if (SelectedUnitId < 0 || SelectedAbilityIndex < 0 || projectedState == null)
                return null;

            CombatUnit unit = projectedState.GetUnit(SelectedUnitId);
            CombatAbilitySO ability = GetSelectedAbility();
            if (unit == null || !unit.Alive || ability == null)
                return null;

            if (ability.SlamTargeted)
                return TryConfirmSlam(unit, ability);

            PlannedAction action = new PlannedAction { UnitId = unit.Id, AbilityIndex = SelectedAbilityIndex, TargetCell = CursorCell, Direction = CurrentDirection };
            return AbilityTargeting.IsValidTarget(projectedState, unit, ability, action) ? action : null;
        }

        public void ClearSelection()
        {
            SelectedUnitId = -1;
            SelectedAbilityIndex = -1;
            pendingSlamTarget = null;
            highlighter.Clear(HighlightKind.Template);
            highlighter.Clear(HighlightKind.Landing);
            highlighter.Clear(HighlightKind.Selection);
            SelectionChanged?.Invoke();
        }

        private PlannedAction TryConfirmSlam(CombatUnit unit, CombatAbilitySO ability)
        {
            if (pendingSlamTarget == null)
            {
                if (!HasAirborneTargetAt(CursorCell))
                    return null;

                pendingSlamTarget = CursorCell;
                RefreshHighlights();
                SelectionChanged?.Invoke();
                return null;
            }

            PlannedAction action = new PlannedAction
            {
                UnitId = unit.Id,
                AbilityIndex = SelectedAbilityIndex,
                TargetCell = pendingSlamTarget.Value,
                SlamCell = CursorCell,
                Direction = AbilityTargeting.DominantCardinal(pendingSlamTarget.Value, CursorCell)
            };

            if (!AbilityTargeting.IsValidTarget(projectedState, unit, ability, action))
                return null;

            pendingSlamTarget = null;
            SelectionChanged?.Invoke();
            return action;
        }

        private bool HasAirborneTargetAt(Vector2Int cell)
        {
            for (int i = 0; i < projectedState.Units.Count; i++)
            {
                CombatUnit candidate = projectedState.Units[i];
                if (candidate.Airborne && candidate.Alive && candidate.Cell == cell)
                    return true;
            }

            return false;
        }

        private void RefreshHighlights()
        {
            CombatUnit selectedUnit = projectedState != null && SelectedUnitId >= 0 ? projectedState.GetUnit(SelectedUnitId) : null;
            if (selectedUnit != null && selectedUnit.Alive)
                highlighter.Show(HighlightKind.Selection, new List<Vector2Int> { selectedUnit.Cell });
            else
                highlighter.Clear(HighlightKind.Selection);

            CombatAbilitySO ability = GetSelectedAbility();
            if (ability == null)
            {
                highlighter.Clear(HighlightKind.Template);
                highlighter.Clear(HighlightKind.Landing);
                return;
            }

            CombatUnit unit = projectedState.GetUnit(SelectedUnitId);

            if (ability.Type == AbilityType.Movement)
            {
                highlighter.Clear(HighlightKind.Template);

                List<Vector2Int> movementLanding = new List<Vector2Int>();
                if (projectedState.IsCellFree(CursorCell))
                    movementLanding.Add(CursorCell);
                highlighter.Show(HighlightKind.Landing, movementLanding);
                return;
            }

            if (ability.Targeting == TargetingMode.DirectionalTemplate)
            {
                PlannedAction tempAction = new PlannedAction { UnitId = SelectedUnitId, AbilityIndex = SelectedAbilityIndex, TargetCell = CursorCell, Direction = CurrentDirection };
                List<Vector2Int> templateCells = AbilityTargeting.GetAffectedCells(projectedState, ability, tempAction);
                highlighter.Show(HighlightKind.Template, templateCells);

                Vector2Int landingCell = AbilityTargeting.GetLandingCell(unit, ability, tempAction);
                List<Vector2Int> landingCells = new List<Vector2Int>();
                if (projectedState.Board.InBounds(landingCell))
                    landingCells.Add(landingCell);
                highlighter.Show(HighlightKind.Landing, landingCells);
                return;
            }

            if (ability.Targeting == TargetingMode.AirborneEnemy)
            {
                if (pendingSlamTarget == null)
                {
                    List<Vector2Int> airborneCells = new List<Vector2Int>();
                    for (int i = 0; i < projectedState.Units.Count; i++)
                    {
                        CombatUnit candidate = projectedState.Units[i];
                        if (candidate.Airborne && candidate.Alive)
                            airborneCells.Add(candidate.Cell);
                    }
                    highlighter.Show(HighlightKind.Template, airborneCells);
                    highlighter.Clear(HighlightKind.Landing);
                    return;
                }

                List<Vector2Int> slamCells = GetSlamCandidateCells(ability, pendingSlamTarget.Value);
                highlighter.Show(HighlightKind.Template, slamCells);

                Vector2Int slamLanding = pendingSlamTarget.Value - AbilityTargeting.DominantCardinal(pendingSlamTarget.Value, CursorCell);
                List<Vector2Int> slamLandingCells = new List<Vector2Int>();
                if (projectedState.Board.InBounds(slamLanding))
                    slamLandingCells.Add(slamLanding);
                highlighter.Show(HighlightKind.Landing, slamLandingCells);
                return;
            }

            highlighter.Clear(HighlightKind.Template);
            highlighter.Clear(HighlightKind.Landing);
        }

        private List<Vector2Int> GetSlamCandidateCells(CombatAbilitySO ability, Vector2Int origin)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            CombatBoard board = projectedState.Board;

            for (int d = 0; d < directions.Length; d++)
                for (int dist = 1; dist <= ability.SlamRange; dist++)
                {
                    Vector2Int cell = origin + directions[d] * dist;
                    if (board.InBounds(cell))
                        cells.Add(cell);
                }

            return cells;
        }

        private CombatAbilitySO GetSelectedAbility()
        {
            if (projectedState == null || SelectedUnitId < 0 || SelectedAbilityIndex < 0)
                return null;
            if (!(projectedState.GetUnit(SelectedUnitId) is PlayerUnit unit))
                return null;
            if (SelectedAbilityIndex >= unit.Definition.Abilities.Length)
                return null;

            return unit.Definition.Abilities[SelectedAbilityIndex];
        }
    }
}
