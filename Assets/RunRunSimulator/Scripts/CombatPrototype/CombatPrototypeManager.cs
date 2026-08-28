using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public enum CombatPhase { Planning, Executing, EnemyTurn, Victory, Defeat, Setup, Spawning, Reacting }

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
        [SerializeField] private ResolutionAnimator animator;
        [SerializeField] private NightSpawner spawner;
        [SerializeField] private CombatUnitView viewPrefab;
        [SerializeField] private int seedTicks = 6;
        [SerializeField] private int germinationTurn = 8;
        [SerializeField] private BoardImpactFeedback impact;
        [SerializeField] private float spawnJumpDuration = 0.4f;
        [SerializeField] private float spawnJumpFromCells = 1.5f;
        [SerializeField] private int cycleLength = 3;

        public CombatPhase Phase { get; private set; }
        public CombatSimState Canonical { get; private set; }
        public Choreography Plan { get; private set; }
        public ProjectionResult Projection { get; private set; }
        public Dictionary<int, CombatUnitView> Views { get; private set; }
        public List<TurnLogEntry> TurnLog { get; } = new List<TurnLogEntry>();
        public event System.Action TurnLogChanged;

        public int SeedId { get; private set; } = -1;
        public int DeployedCount { get; private set; }
        public bool AwaitingSeed => Phase == CombatPhase.Setup && SeedId < 0;
        public int GerminationTurn => germinationTurn;
        public int TurnNumber => turnCounter;
        public CombatUnit Seed => SeedId >= 0 && Canonical != null ? Canonical.GetUnit(SeedId) : null;
        public PlayerUnitDefinitionSO NextDeployDefinition => Phase == CombatPhase.Setup && SeedId >= 0 && DeployedCount < playerLoadout.Length ? playerLoadout[DeployedCount] : null;
        public EnemyDefinitionSO[] EnemyLoadout => enemyLoadout;

        public int CycleTurn => cycleTurn;
        public int TurnsUntilEnemyAttack => Mathf.Max(1, cycleLength - cycleTurn + 1);
        public bool IsAbilitySpent(int unitId, int abilityIndex) => spentAbilities.Contains(unitId * 8 + abilityIndex);
        public bool HasAvailableAbility(int unitId)
        {
            if (Canonical == null) return false;
            CombatUnit unit = Canonical.GetUnit(unitId);
            if (!(unit is PlayerUnit player) || !player.Alive || player.Definition == null) return false;

            CombatAbilitySO[] abilities = player.Definition.Abilities;
            if (abilities == null) return false;

            for (int i = 0; i < abilities.Length; i++)
            {
                if (abilities[i] == null) continue;
                if (!IsAbilitySpent(unitId, i)) return true;
            }
            return false;
        }
        public bool AnyUsableAbility()
        {
            if (Canonical == null) return false;
            List<PlayerUnit> players = Canonical.GetPlayers();
            for (int i = 0; i < players.Count; i++)
            {
                if (HasAvailableAbility(players[i].Id)) return true;
            }
            return false;
        }

        private int cycleTurn = 1;
        private int turnCounter;
        private int nextUnitId;
        private int spawnCounter;
        private readonly HashSet<int> spentAbilities = new HashSet<int>();

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
            Plan = new Choreography();
            TurnLog.Clear();
            turnCounter = 0;
            TurnLogChanged?.Invoke();

            nextUnitId = 0;
            spawnCounter = 0;
            spentAbilities.Clear();
            cycleTurn = 1;
            SeedId = -1;
            DeployedCount = 0;
            if (spawner != null) spawner.ResetForEncounter();
            Phase = CombatPhase.Setup;

            hud.Bind(this);
            hud.Refresh();
        }

        public void PlaceAt(Vector2Int cell)
        {
            if (Phase != CombatPhase.Setup) return;
            if (!Canonical.Board.InBounds(cell) || !Canonical.IsCellFree(cell)) return;

            if (SeedId < 0)
            {
                SeedUnit unit = new SeedUnit
                {
                    Id = nextUnitId++,
                    IsPlayer = false,
                    Cell = cell,
                    MaxTicks = seedTicks,
                    Ticks = seedTicks
                };
                Canonical.Units.Add(unit);

                SpawnView(unit);

                SeedId = unit.Id;
                hud.Refresh();
                return;
            }

            if (DeployedCount < playerLoadout.Length)
            {
                PlayerUnitDefinitionSO def = playerLoadout[DeployedCount];
                PlayerUnit playerUnit = new PlayerUnit
                {
                    Id = nextUnitId++,
                    IsPlayer = true,
                    Cell = cell,
                    MaxTicks = def.MaxTicks,
                    Ticks = def.MaxTicks,
                    Definition = def
                };
                Canonical.Units.Add(playerUnit);

                SpawnView(playerUnit);

                DeployedCount++;

                if (DeployedCount == playerLoadout.Length)
                {
                    spawner.ResetForEncounter();
                    spawner.PrepareNextWave(Canonical, Seed.Cell);
                    StartCoroutine(RunSpawnPhase());
                }
                else
                {
                    hud.Refresh();
                }
            }
        }

        private IEnumerator PlaySpawnWave(List<EnemySpawn> spawns)
        {
            for (int i = 0; i < spawns.Count; i++)
            {
                EnemySpawn spawn = spawns[i];
                EnemyDefinitionSO def = enemyLoadout[spawnCounter % enemyLoadout.Length];
                spawnCounter++;
                int maxTicks = def.GuardTicks + def.FinisherTicks;
                EnemyUnit unit = new EnemyUnit
                {
                    Id = nextUnitId++,
                    IsPlayer = false,
                    Cell = spawn.Cell,
                    Facing = spawn.Facing,
                    MaxTicks = maxTicks,
                    Ticks = maxTicks,
                    Definition = def
                };
                Canonical.Units.Add(unit);

                CombatUnitView view = SpawnView(unit);
                Vector2Int outward = NightWaves.EdgeOutwardDirection(builder.Board, spawn.Cell);
                Vector3 target = builder.Board.CellToWorld(spawn.Cell);
                Vector3 from = target + new Vector3(outward.x, 0f, outward.y) * (CombatBoard.CellSize * spawnJumpFromCells);
                view.SnapTo(from);
                yield return view.MoveTo(target, true, spawnJumpDuration);
                if (impact != null) impact.ShakeAt(spawn.Cell);
            }
        }

        private IEnumerator RunSpawnPhase()
        {
            Phase = CombatPhase.Spawning;
            hud.Refresh();
            yield return PlaySpawnWave(spawner.ConsumeWave(Canonical, Seed.Cell));
            spawner.PrepareNextWave(Canonical, Seed.Cell);
            BeginPlanningTurn();
        }

        private CombatUnitView SpawnView(CombatUnit unit)
        {
            CombatUnitView view = Instantiate(viewPrefab);
            view.gameObject.name = "Unit_" + unit.Id;
            view.Init(unit, builder.Board);
            Views[unit.Id] = view;
            return view;
        }

        public void ConfirmAction()
        {
            if (Phase != CombatPhase.Planning) return;
            if (Plan.TotalActions >= Choreography.MaxActions) return;

            PlannedAction action = targeting.TryConfirm();
            if (action == null) return;

            if (Plan.IsAbilityUsed(action.UnitId, action.AbilityIndex)) return;
            if (IsAbilitySpent(action.UnitId, action.AbilityIndex)) return;

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
            if (Phase != CombatPhase.Planning) return;
            if (Plan.TotalActions <= 0 && AnyUsableAbility()) return;

            turnCounter++;
            TurnLog.Add(BuildTurnLogEntry());
            TurnLogChanged?.Invoke();

            Phase = CombatPhase.Executing;
            targeting.ClearSelection();
            highlighter.Clear(HighlightKind.Template);
            highlighter.Clear(HighlightKind.Landing);
            highlighter.Clear(HighlightKind.Path);

            StartCoroutine(executor.ExecuteChoreography(Canonical, Plan, Views, builder.Board, OnChoreographyDone));
            hud.Refresh();
        }

        private TurnLogEntry BuildTurnLogEntry()
        {
            TurnLogEntry entry = new TurnLogEntry { Turn = turnCounter };
            for (int b = 0; b < Plan.Beats.Count; b++)
            {
                List<PlannedAction> actions = Plan.Beats[b].Actions;
                for (int i = 0; i < actions.Count; i++)
                {
                    PlannedAction action = actions[i];
                    string unitName = "?";
                    string abilityName = "?";
                    if (Canonical.GetUnit(action.UnitId) is PlayerUnit player && player.Definition != null)
                    {
                        unitName = player.Definition.DisplayName;
                        CombatAbilitySO[] abilities = player.Definition.Abilities;
                        if (abilities != null && action.AbilityIndex >= 0 && action.AbilityIndex < abilities.Length && abilities[action.AbilityIndex] != null)
                            abilityName = abilities[action.AbilityIndex].DisplayName;
                    }
                    entry.Lines.Add("B" + (b + 1) + " · " + unitName + " → " + abilityName + " (" + action.TargetCell.x + "," + action.TargetCell.y + ")");
                }
            }
            return entry;
        }

        private void OnChoreographyDone(List<ResolutionEvent> events)
        {
            if (TurnLog.Count > 0)
            {
                TurnLogEntry lastEntry = TurnLog[TurnLog.Count - 1];
                bool anyFizzle = false;
                for (int i = 0; i < events.Count; i++)
                {
                    ResolutionEvent evt = events[i];
                    if (evt.Type != ResolutionEventType.Fizzle) continue;

                    string unitName = "?";
                    if (Canonical.GetUnit(evt.UnitId) is PlayerUnit player && player.Definition != null)
                        unitName = player.Definition.DisplayName;

                    lastEntry.Lines.Add("FIZZLE · " + unitName + " — acción cancelada: el aterrizaje se ocupó");
                    anyFizzle = true;
                }

                if (anyFizzle) TurnLogChanged?.Invoke();
            }

            if (SeedDead())
            {
                EndEncounter(false);
                return;
            }

            foreach (PlannedAction action in Plan.AllActions) spentAbilities.Add(action.UnitId * 8 + action.AbilityIndex);

            if (turnCounter >= germinationTurn)
            {
                StartCoroutine(PlayGermination());
                return;
            }

            bool cycleEnd = cycleTurn >= cycleLength;

            if (cycleEnd)
            {
                if (TurnLog.Count > 0)
                {
                    TurnLog[TurnLog.Count - 1].Lines.Add("⚔ ATAQUE ENEMIGO — fin del ciclo");
                    TurnLogChanged?.Invoke();
                }

                Phase = CombatPhase.EnemyTurn;
                hud.Refresh();
                StartCoroutine(enemyTurn.RunTurn(Canonical, Views, builder.Board, OnEnemyTurnDone));
            }
            else
            {
                if (HasPendingReactions())
                {
                    Phase = CombatPhase.Reacting;
                    hud.Refresh();
                }
                StartCoroutine(enemyTurn.RunReactions(Canonical, Views, builder.Board, OnReactionsDone));
            }
        }

        private bool HasPendingReactions()
        {
            for (int i = 0; i < Canonical.Units.Count; i++)
            {
                CombatUnit unit = Canonical.Units[i];
                if (unit.Alive && unit.Airborne) return true;
                if (unit is EnemyUnit enemy && enemy.Alive && enemy.WasHitThisTurn) return true;
            }
            return false;
        }

        private void OnReactionsDone()
        {
            if (SeedDead())
            {
                EndEncounter(false);
                return;
            }

            if (Canonical.GetPlayers().Count == 0)
            {
                EndEncounter(false);
                return;
            }

            cycleTurn++;
            BeginPlanningTurn();
        }

        private void BeginPlanningTurn()
        {
            Plan = new Choreography();
            enemyTurn.CommitIntents(Canonical);
            Phase = CombatPhase.Planning;
            RefreshProjection();
        }

        private void OnEnemyTurnDone()
        {
            if (SeedDead())
            {
                EndEncounter(false);
                return;
            }

            if (Canonical.GetPlayers().Count == 0)
            {
                EndEncounter(false);
                return;
            }

            spentAbilities.Clear();
            cycleTurn = 1;
            StartCoroutine(RunSpawnPhase());
        }

        private bool SeedDead()
        {
            CombatUnit seed = Seed;
            return seed == null || !seed.Alive || seed.Ticks <= 0;
        }

        private IEnumerator PlayGermination()
        {
            List<ResolutionEvent> events = ActionResolver.ResolveGermination(Canonical);
            yield return animator.Play(events, Views, builder.Board, Canonical);
            EndEncounter(true);
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
