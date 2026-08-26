using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public enum CombatPhase { Planning, Executing, EnemyTurn, Victory, Defeat }

    public class CombatPrototypeManager : MonoBehaviour
    {
        [SerializeField] private CombatBoardBuilder builder;
        [SerializeField] private BoardLayoutSO boardLayout;
        [SerializeField] private BoardHighlighter highlighter;
        [SerializeField] private TargetingController targeting;
        [SerializeField] private PlanExecutor executor;
        [SerializeField] private EnemyTurnController enemyTurn;
        [SerializeField] private CombatPrototypeHUD hud;
        [SerializeField] private EnemyBriefPanel brief;
        [SerializeField] private PlayerUnitDefinitionSO[] playerLoadout;
        [SerializeField] private EnemyDefinitionSO[] enemyLoadout;

        public CombatPhase Phase { get; private set; }
        public CombatSimState Canonical { get; private set; }
        public Choreography Plan { get; private set; }
        public ProjectionResult Projection { get; private set; }
        public Dictionary<int, CombatUnitView> Views { get; private set; }

        private void Start()
        {
            RestartEncounter();
        }

        public void RestartEncounter()
        {
            StopAllCoroutines();

            if (Views != null)
            {
                foreach (KeyValuePair<int, CombatUnitView> pair in Views)
                {
                    if (pair.Value != null) Destroy(pair.Value.gameObject);
                }
            }

            highlighter.ClearAll();

            Canonical = new CombatSimState { Board = builder.Board };
            Views = new Dictionary<int, CombatUnitView>();

            int nextId = 0;

            List<Vector2Int> playerSpawns = boardLayout.GetPlayerSpawns();
            for (int i = 0; i < playerSpawns.Count; i++)
            {
                PlayerUnitDefinitionSO def = playerLoadout[i % playerLoadout.Length];
                PlayerUnit unit = new PlayerUnit
                {
                    Id = nextId++,
                    IsPlayer = true,
                    Cell = playerSpawns[i],
                    MaxTicks = def.MaxTicks,
                    Ticks = def.MaxTicks,
                    Definition = def
                };
                Canonical.Units.Add(unit);

                GameObject go = new GameObject("Unit_" + unit.Id);
                CombatUnitView view = go.AddComponent<CombatUnitView>();
                view.Init(unit, builder.Board);
                Views[unit.Id] = view;
            }

            List<Vector2Int> enemySpawns = boardLayout.GetEnemySpawns();
            for (int i = 0; i < enemySpawns.Count; i++)
            {
                EnemyDefinitionSO def = enemyLoadout[i % enemyLoadout.Length];
                int maxTicks = def.GuardTicks + def.FinisherTicks;
                EnemyUnit unit = new EnemyUnit
                {
                    Id = nextId++,
                    IsPlayer = false,
                    Cell = enemySpawns[i],
                    MaxTicks = maxTicks,
                    Ticks = maxTicks,
                    Definition = def
                };
                Canonical.Units.Add(unit);

                GameObject go = new GameObject("Unit_" + unit.Id);
                CombatUnitView view = go.AddComponent<CombatUnitView>();
                view.Init(unit, builder.Board);
                Views[unit.Id] = view;
            }

            Plan = new Choreography();
            enemyTurn.CommitIntents(Canonical);
            if (hud != null) hud.Bind(this);
            Phase = CombatPhase.Planning;
            RefreshProjection();
        }

        public void ConfirmAction()
        {
            if (Phase != CombatPhase.Planning) return;

            PlannedAction action = targeting.TryConfirm();
            if (action == null) return;

            if (Plan.IsAbilityUsed(action.UnitId, action.AbilityIndex)) return;

            Plan.Add(action);
            RefreshProjection();
        }

        public void NewBeat()
        {
            if (Phase != CombatPhase.Planning) return;

            Plan.AddBeat();
            RefreshProjection();
        }

        public void UndoLast()
        {
            if (Phase != CombatPhase.Planning) return;

            Plan.UndoLast();
            RefreshProjection();
        }

        public void ExecutePlan()
        {
            if (Phase != CombatPhase.Planning || Plan.TotalActions <= 0) return;

            Phase = CombatPhase.Executing;
            targeting.ClearSelection();
            highlighter.Clear(HighlightKind.Template);
            highlighter.Clear(HighlightKind.Landing);
            highlighter.Clear(HighlightKind.Path);

            StartCoroutine(executor.ExecuteChoreography(Canonical, Plan, Views, builder.Board, OnChoreographyDone));
            hud.Refresh();
        }

        private void OnChoreographyDone()
        {
            if (Canonical.GetEnemies().Count == 0)
            {
                EndEncounter(true);
                return;
            }

            Phase = CombatPhase.EnemyTurn;
            hud.Refresh();
            StartCoroutine(enemyTurn.RunTurn(Canonical, Views, builder.Board, OnEnemyTurnDone));
        }

        private void OnEnemyTurnDone()
        {
            if (Canonical.GetEnemies().Count == 0)
            {
                EndEncounter(true);
                return;
            }

            if (Canonical.GetPlayers().Count == 0)
            {
                EndEncounter(false);
                return;
            }

            Plan = new Choreography();
            enemyTurn.CommitIntents(Canonical);
            Phase = CombatPhase.Planning;
            RefreshProjection();
        }

        private void EndEncounter(bool victory)
        {
            Phase = victory ? CombatPhase.Victory : CombatPhase.Defeat;
            highlighter.ClearAll();
            hud.Refresh();
        }

        private void RefreshProjection()
        {
            Projection = PlanProjection.Project(Canonical, Plan);
            CombatSimState beatStart = Plan.Beats.Count >= 2 ? Projection.StateAfterBeat[Plan.Beats.Count - 2] : Canonical;
            targeting.SetProjectedState(beatStart);

            List<Vector2Int> movedCells = new List<Vector2Int>();
            List<PlayerUnit> projectedPlayers = Projection.EndOfBeatsState.GetPlayers();
            for (int i = 0; i < projectedPlayers.Count; i++)
            {
                PlayerUnit projectedPlayer = projectedPlayers[i];
                CombatUnit canonicalUnit = Canonical.GetUnit(projectedPlayer.Id);
                if (canonicalUnit != null && canonicalUnit.Cell != projectedPlayer.Cell)
                {
                    movedCells.Add(projectedPlayer.Cell);
                }
            }
            highlighter.Show(HighlightKind.Path, movedCells);

            enemyTurn.PaintIntents(Projection.EndOfBeatsState);

            List<Vector2Int> landingCells = new List<Vector2Int>();
            List<CombatUnit> projectedUnits = Projection.EndOfBeatsState.Units;
            for (int i = 0; i < projectedUnits.Count; i++)
            {
                CombatUnit unit = projectedUnits[i];
                if (unit.Alive && unit.Airborne)
                {
                    landingCells.Add(unit.AirborneLandingCell);
                }
            }
            highlighter.Show(HighlightKind.Landing, landingCells);

            hud.Refresh();
        }

        public void ShowBriefAt(Vector2Int cell, Vector2 screenPosition)
        {
            CombatUnit unit = Canonical.GetUnitAt(cell);
            if (unit is EnemyUnit enemy)
            {
                brief.Show(enemy, screenPosition);
            }
            else
            {
                brief.Hide();
            }
        }

        public void HideBrief()
        {
            brief.Hide();
        }
    }
}
