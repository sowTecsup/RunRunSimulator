using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public static class CombatAutoTester
    {
        public class MatchResult
        {
            public string Profile;
            public string Outcome = "sin resolver";
            public int Turns;
            public int SeedTicksLost;
            public int DragonTicksLost;
            public int DragonsDead;
            public int EnemyAttacks;
            public int EnemyHitsOnSeed;
            public int EnemyHitsOnDragons;
            public int EnemiesSpawned;
            public int EnemiesKilled;
            public int ActionsUsed;
        }

        public static MatchResult RunMatch(BoardLayoutSO layout, PlayerUnitDefinitionSO[] loadout, EnemyDefinitionSO[] enemyDefs, string profile, int rngSeed, int seedTicks, int germinationTurn, int baseWaveSize, int extraEveryWaves, int maxTurns)
        {
            MatchResult result = new MatchResult { Profile = profile };
            System.Random rng = new System.Random(rngSeed);
            CombatBoard board = new CombatBoard(layout, 1f);
            CombatSimState state = new CombatSimState { Board = board };
            int nextId = 0;

            Vector2Int seedCell = FindClosestFree(state, new Vector2Int(board.Width / 2, board.Depth / 2));
            SeedUnit seed = new SeedUnit { Id = nextId++, IsPlayer = false, Cell = seedCell, MaxTicks = seedTicks, Ticks = seedTicks };
            state.Units.Add(seed);

            List<Vector2Int> deploy = NightWaves.FindSpawnCells(state, seedCell, loadout.Length, 1, null);
            if (deploy.Count < loadout.Length) { result.Outcome = "sin espacio de despliegue"; return result; }
            for (int i = 0; i < loadout.Length; i++)
            {
                PlayerUnitDefinitionSO def = loadout[i];
                state.Units.Add(new PlayerUnit { Id = nextId++, IsPlayer = true, Cell = deploy[i], MaxTicks = def.MaxTicks, Ticks = def.MaxTicks, Definition = def });
            }

            int spawnCounter = 0;
            int waveNumber = 0;
            List<EnemySpawn> pending = new List<EnemySpawn>();
            PrepareWave(state, seedCell, ref waveNumber, baseWaveSize, extraEveryWaves, pending);
            result.EnemiesSpawned += SpawnPending(state, pending, enemyDefs, ref spawnCounter, ref nextId, seedCell);
            PrepareWave(state, seedCell, ref waveNumber, baseWaveSize, extraEveryWaves, pending);
            CommitIntents(state);

            int dragonTicksStart = TotalPlayerTicks(state);
            Vector2Int[] dirs = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };
            HashSet<int> spent = new HashSet<int>();
            int cycleTurn = 0;

            for (int turn = 1; turn <= maxTurns; turn++)
            {
                result.Turns = turn;
                cycleTurn++;
                int actions = 0;
                while (actions < 2)
                {
                    PlannedAction action = ChooseAction(state, seedCell, profile, spent, rng, dirs);
                    if (action == null) break;
                    Beat beat = new Beat();
                    beat.Actions.Add(action);
                    ActionResolver.ResolveBeat(state, beat);
                    spent.Add(SpentKey(action.UnitId, action.AbilityIndex));
                    actions++;
                    result.ActionsUsed++;
                }

                if (!seed.Alive || seed.Ticks <= 0) { result.Outcome = "DERROTA-semilla"; break; }

                if (turn >= germinationTurn) { ActionResolver.ResolveGermination(state); result.Outcome = "VICTORIA"; break; }

                if (cycleTurn >= 3)
                {
                    List<ResolutionEvent> events = ActionResolver.ResolveEnemyTurn(state);
                    CountEvents(result, events, state);

                    if (!seed.Alive || seed.Ticks <= 0) { result.Outcome = "DERROTA-semilla"; break; }
                    if (state.GetPlayers().Count == 0) { result.Outcome = "DERROTA-dragones"; break; }

                    spent.Clear();
                    cycleTurn = 0;
                    result.EnemiesSpawned += SpawnPending(state, pending, enemyDefs, ref spawnCounter, ref nextId, seedCell);
                    PrepareWave(state, seedCell, ref waveNumber, baseWaveSize, extraEveryWaves, pending);
                }
                else
                {
                    List<ResolutionEvent> events = ActionResolver.ResolveEnemyReactions(state);
                    CountEvents(result, events, state);

                    if (!seed.Alive || seed.Ticks <= 0) { result.Outcome = "DERROTA-semilla"; break; }
                    if (state.GetPlayers().Count == 0) { result.Outcome = "DERROTA-dragones"; break; }
                }

                CommitIntents(state);
            }

            result.SeedTicksLost = seedTicks - Mathf.Max(0, seed.Ticks);
            result.DragonTicksLost = dragonTicksStart - TotalPlayerTicks(state);
            result.DragonsDead = loadout.Length - state.GetPlayers().Count;
            result.EnemiesKilled = result.EnemiesSpawned - state.GetEnemies().Count;
            return result;
        }

        private static void PrepareWave(CombatSimState state, Vector2Int seedCell, ref int waveNumber, int baseWaveSize, int extraEveryWaves, List<EnemySpawn> pending)
        {
            waveNumber++;
            int size = NightWaves.WaveSize(waveNumber, baseWaveSize, extraEveryWaves);
            pending.Clear();
            List<Vector2Int> cells = NightWaves.FindEdgeSpawnCells(state, seedCell, size, null);
            for (int i = 0; i < cells.Count; i++)
                pending.Add(new EnemySpawn { Cell = cells[i], Facing = AbilityTargeting.DominantCardinal(cells[i], seedCell) });
        }

        private static int SpawnPending(CombatSimState state, List<EnemySpawn> pending, EnemyDefinitionSO[] enemyDefs, ref int spawnCounter, ref int nextId, Vector2Int seedCell)
        {
            int spawned = 0;
            List<Vector2Int> taken = new List<Vector2Int>();
            for (int i = 0; i < pending.Count; i++)
            {
                EnemySpawn spawn = pending[i];
                if (!state.IsCellFree(spawn.Cell) || taken.Contains(spawn.Cell))
                {
                    List<Vector2Int> alt = NightWaves.FindEdgeSpawnCells(state, seedCell, 1, taken);
                    if (alt.Count == 0) continue;
                    spawn = new EnemySpawn { Cell = alt[0], Facing = AbilityTargeting.DominantCardinal(alt[0], seedCell) };
                }
                taken.Add(spawn.Cell);
                EnemyDefinitionSO def = enemyDefs[spawnCounter % enemyDefs.Length];
                spawnCounter++;
                int maxTicks = def.GuardTicks + def.FinisherTicks;
                state.Units.Add(new EnemyUnit { Id = nextId++, IsPlayer = false, Cell = spawn.Cell, Facing = spawn.Facing, MaxTicks = maxTicks, Ticks = maxTicks, Definition = def });
                spawned++;
            }
            pending.Clear();
            return spawned;
        }

        private static void CountEvents(MatchResult result, List<ResolutionEvent> events, CombatSimState state)
        {
            for (int i = 0; i < events.Count; i++)
            {
                ResolutionEvent evt = events[i];
                if (evt.Type == ResolutionEventType.EnemyAttack) result.EnemyAttacks++;
                if (evt.Type == ResolutionEventType.Hit)
                {
                    CombatUnit victim = state.GetUnit(evt.UnitId);
                    if (victim is SeedUnit) result.EnemyHitsOnSeed++;
                    else if (victim is PlayerUnit) result.EnemyHitsOnDragons++;
                }
            }
        }

        private static void CommitIntents(CombatSimState state)
        {
            List<EnemyUnit> enemies = state.GetEnemies();
            for (int i = 0; i < enemies.Count; i++)
                enemies[i].Intent = EnemyBrain.ComputeIntent(state, enemies[i]);
        }

        private static PlannedAction ChooseAction(CombatSimState state, Vector2Int seedCell, string profile, HashSet<int> spent, System.Random rng, Vector2Int[] dirs)
        {
            if (profile == "pasivo") return null;

            if (profile == "distraido")
            {
                List<PlayerUnit> players = state.GetPlayers();
                if (players.Count == 0) return null;
                for (int attempt = 0; attempt < 60; attempt++)
                {
                    PlayerUnit pl = players[rng.Next(players.Count)];
                    int ab = 1 + rng.Next(2);
                    if (spent.Contains(SpentKey(pl.Id, ab))) continue;
                    CombatAbilitySO ability = pl.Definition.Abilities[ab];
                    if (ability == null || ability.Targeting != TargetingMode.DirectionalTemplate) continue;
                    Vector2Int cursor = new Vector2Int(rng.Next(state.Board.Width), rng.Next(state.Board.Depth));
                    Vector2Int d = dirs[rng.Next(4)];
                    PlannedAction candidate = new PlannedAction { UnitId = pl.Id, AbilityIndex = ab, TargetCell = AbilityTargeting.GetAnchorForCursor(ability, cursor, d), Direction = d };
                    if (AbilityTargeting.IsValidTarget(state, pl, ability, candidate)) return candidate;
                }
                return null;
            }

            List<EnemyUnit> targets = state.GetEnemies();
            if (profile == "defensor")
            {
                List<EnemyUnit> ordered = new List<EnemyUnit>();
                for (int i = 0; i < targets.Count; i++)
                    if (ThreatensSeed(targets[i], seedCell)) ordered.Add(targets[i]);
                for (int i = 0; i < targets.Count; i++)
                    if (!ordered.Contains(targets[i])) ordered.Add(targets[i]);
                targets = ordered;
            }

            List<PlayerUnit> attackers = state.GetPlayers();
            for (int t = 0; t < targets.Count; t++)
            {
                EnemyUnit enemy = targets[t];
                if (enemy.Airborne) continue;
                for (int p = 0; p < attackers.Count; p++)
                {
                    PlayerUnit pl = attackers[p];
                    for (int ab = 1; ab <= 2; ab++)
                    {
                        if (spent.Contains(SpentKey(pl.Id, ab))) continue;
                        CombatAbilitySO ability = pl.Definition.Abilities[ab];
                        if (ability == null || ability.Targeting != TargetingMode.DirectionalTemplate) continue;
                        for (int d = 0; d < dirs.Length; d++)
                        {
                            PlannedAction candidate = new PlannedAction { UnitId = pl.Id, AbilityIndex = ab, TargetCell = enemy.Cell, Direction = dirs[d] };
                            if (!AbilityTargeting.IsValidTarget(state, pl, ability, candidate)) continue;
                            if (!AbilityTargeting.GetAffectedCells(state, pl, ability, candidate).Contains(enemy.Cell)) continue;
                            return candidate;
                        }
                    }
                }
            }
            return null;
        }

        private static int SpentKey(int unitId, int abilityIndex)
        {
            return unitId * 8 + abilityIndex;
        }

        private static bool ThreatensSeed(EnemyUnit enemy, Vector2Int seedCell)
        {
            if (AbilityTargeting.Chebyshev(enemy.Cell, seedCell) <= 1) return true;
            if (enemy.Intent == null || !enemy.Intent.HasAttack) return false;
            List<Vector2Int> cells = enemy.Intent.GetAttackCells(enemy.Cell);
            for (int i = 0; i < cells.Count; i++)
                if (cells[i] == seedCell || AbilityTargeting.Chebyshev(cells[i], seedCell) <= 1) return true;
            return false;
        }

        private static int TotalPlayerTicks(CombatSimState state)
        {
            int total = 0;
            List<PlayerUnit> players = state.GetPlayers();
            for (int i = 0; i < players.Count; i++) total += players[i].Ticks;
            return total;
        }

        private static Vector2Int FindClosestFree(CombatSimState state, Vector2Int center)
        {
            Vector2Int best = center;
            int bestDist = int.MaxValue;
            for (int x = 0; x < state.Board.Width; x++)
                for (int y = 0; y < state.Board.Depth; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!state.Board.InBounds(cell) || !state.IsCellFree(cell)) continue;
                    int dist = AbilityTargeting.Chebyshev(cell, center);
                    if (dist < bestDist) { bestDist = dist; best = cell; }
                }
            return best;
        }
    }
}
