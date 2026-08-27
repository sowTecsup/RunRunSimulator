using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype.EditorTools
{
    public static class PrototypeSimBridge
    {
        public static CombatPrototypeManager FindManager()
        {
            return UnityEngine.Object.FindFirstObjectByType<CombatPrototypeManager>();
        }

        public static JToken ReadPlanToken(JObject parameters)
        {
            JToken token = parameters?["plan"];
            if (token == null || token.Type == JTokenType.Null) return null;
            if (token.Type != JTokenType.String) return token;

            string raw = token.ToString();
            if (string.IsNullOrWhiteSpace(raw)) return null;
            try { return JToken.Parse(raw); }
            catch { return null; }
        }

        public static Choreography ParsePlan(JToken planToken)
        {
            JArray beats = planToken as JArray;
            if (beats == null && planToken is JObject planObject) beats = planObject["beats"] as JArray;
            if (beats == null) return null;

            Choreography plan = new Choreography();
            plan.Beats.Clear();

            foreach (JToken beatToken in beats)
            {
                Beat beat = new Beat();
                JArray actions = beatToken as JArray;
                if (actions == null && beatToken is JObject beatObject) actions = beatObject["actions"] as JArray;

                if (actions != null)
                {
                    foreach (JToken actionToken in actions)
                    {
                        JObject action = actionToken as JObject;
                        if (action == null) continue;
                        beat.Actions.Add(new PlannedAction
                        {
                            UnitId = ReadInt(action["unitId"]),
                            AbilityIndex = ReadInt(action["abilityIndex"]),
                            TargetCell = ReadCell(action["targetCell"]),
                            Direction = ReadCell(action["direction"]),
                            SlamCell = ReadCell(action["slamCell"])
                        });
                    }
                }

                plan.Beats.Add(beat);
            }

            if (plan.Beats.Count == 0) plan.Beats.Add(new Beat());
            return plan;
        }

        public static int ReadInt(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return 0;
            try { return token.Value<int>(); }
            catch { return 0; }
        }

        public static bool ReadBool(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return false;
            try { return token.Value<bool>(); }
            catch { return false; }
        }

        public static Vector2Int ReadCell(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null) return Vector2Int.zero;
            if (token is JArray array) return array.Count >= 2 ? new Vector2Int(ReadInt(array[0]), ReadInt(array[1])) : Vector2Int.zero;
            if (token is JObject cell) return new Vector2Int(ReadInt(cell["x"]), ReadInt(cell["y"]));
            return Vector2Int.zero;
        }

        public static string UnitKey(CombatUnit unit)
        {
            EnemyUnit enemy = unit as EnemyUnit;
            string facing = enemy != null ? $"|f{enemy.Facing.x},{enemy.Facing.y}" : string.Empty;
            return $"{unit.Id}@{unit.Cell.x},{unit.Cell.y}|t{unit.Ticks}|a{(unit.Alive ? 1 : 0)}|air{(unit.Airborne ? 1 : 0)}{facing}";
        }

        public static string StateSignature(CombatSimState state)
        {
            List<string> keys = new List<string>();
            foreach (CombatUnit unit in state.Units) keys.Add(UnitKey(unit));
            return string.Join(";", keys);
        }

        public static string EventKey(ResolutionEvent resolution)
        {
            int cells = resolution.Cells != null ? resolution.Cells.Count : 0;
            return $"{resolution.Type}#{resolution.UnitId}<{resolution.SourceId}>({resolution.From.x},{resolution.From.y})->({resolution.To.x},{resolution.To.y})|face{resolution.Facing.x},{resolution.Facing.y}|w{resolution.Wave}|t{resolution.TicksAfter}|env{(resolution.Environmental ? 1 : 0)}|cells{cells}";
        }

        public static List<string> DescribeEvents(List<ResolutionEvent> events)
        {
            List<string> described = new List<string>();
            if (events == null) return described;
            foreach (ResolutionEvent resolution in events) described.Add(EventKey(resolution));
            return described;
        }

        public static List<object> DescribeUnits(CombatSimState state)
        {
            List<object> described = new List<object>();
            foreach (CombatUnit unit in state.Units)
            {
                EnemyUnit enemy = unit as EnemyUnit;
                described.Add(new
                {
                    id = unit.Id,
                    isPlayer = unit.IsPlayer,
                    cell = new[] { unit.Cell.x, unit.Cell.y },
                    ticks = unit.Ticks,
                    maxTicks = unit.MaxTicks,
                    alive = unit.Alive,
                    airborne = unit.Airborne,
                    facing = enemy != null ? new[] { enemy.Facing.x, enemy.Facing.y } : null
                });
            }
            return described;
        }

        public static List<string> DiffStates(CombatSimState projected, CombatSimState executed)
        {
            List<string> diffs = new List<string>();
            if (projected == null || executed == null)
            {
                diffs.Add("estado nulo en uno de los dos caminos");
                return diffs;
            }

            if (projected.Units.Count != executed.Units.Count)
            {
                diffs.Add($"cantidad de unidades {projected.Units.Count} vs {executed.Units.Count}");
                return diffs;
            }

            for (int i = 0; i < projected.Units.Count; i++)
            {
                string projectedKey = UnitKey(projected.Units[i]);
                string executedKey = UnitKey(executed.Units[i]);
                if (projectedKey != executedKey) diffs.Add($"proyeccion {projectedKey} vs ejecucion {executedKey}");
            }

            return diffs;
        }

        public static List<string> DiffEvents(List<ResolutionEvent> projected, List<ResolutionEvent> executed)
        {
            List<string> diffs = new List<string>();
            List<string> projectedKeys = DescribeEvents(projected);
            List<string> executedKeys = DescribeEvents(executed);

            if (projectedKeys.Count != executedKeys.Count)
            {
                diffs.Add($"cantidad de eventos {projectedKeys.Count} vs {executedKeys.Count}");
                return diffs;
            }

            for (int i = 0; i < projectedKeys.Count; i++)
            {
                if (projectedKeys[i] != executedKeys[i]) diffs.Add($"evento {i}: proyeccion {projectedKeys[i]} vs ejecucion {executedKeys[i]}");
            }

            return diffs;
        }
    }
}
