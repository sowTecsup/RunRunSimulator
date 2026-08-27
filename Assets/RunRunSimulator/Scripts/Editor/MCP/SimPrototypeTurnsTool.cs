using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace MoriMonchiSimulator.CombatPrototype.EditorTools
{
    [McpForUnityTool("sim_prototype_turns", Description = "Corre un plan del prototipo de combate sobre un clon del estado canonico y devuelve los ResolutionEvents beat por beat, el turno enemigo y los turnos enemigos extra que se pidan. Nunca toca el estado canonico ni la partida en curso. Requiere la escena CombatPrototype en Play mode.")]
    public static class SimPrototypeTurnsTool
    {
        public class Parameters
        {
            [ToolParameter("Plan opcional en JSON: {\"beats\":[{\"actions\":[{\"unitId\":0,\"abilityIndex\":0,\"targetCell\":[3,4],\"direction\":[1,0],\"slamCell\":[5,4]}]}]}. Si se omite usa el plan vivo del manager.", Required = false)]
            public string plan { get; set; }

            [ToolParameter("Turnos enemigos adicionales a correr despues del turno enemigo del plan, para observar el desgaste sin jugador.", Required = false, DefaultValue = "0")]
            public int extraEnemyTurns { get; set; }

            [ToolParameter("Incluye la lista completa de eventos de cada fase.", Required = false, DefaultValue = "true")]
            public bool includeEvents { get; set; }
        }

        public static object HandleCommand(JObject @params)
        {
            CombatPrototypeManager manager = PrototypeSimBridge.FindManager();
            if (manager == null) return new ErrorResponse("No hay CombatPrototypeManager en la escena activa. Abri CombatPrototype.unity.");

            CombatSimState canonical = manager.Canonical;
            if (canonical == null) return new ErrorResponse("El manager no tiene estado canonico todavia. Entra a Play mode.");

            JToken planToken = PrototypeSimBridge.ReadPlanToken(@params);
            Choreography plan = planToken != null ? PrototypeSimBridge.ParsePlan(planToken) : manager.Plan;
            if (plan == null) return new ErrorResponse("Plan invalido: se esperaba {\"beats\":[{\"actions\":[...]}]}.");

            int extraEnemyTurns = PrototypeSimBridge.ReadInt(@params?["extraEnemyTurns"]);
            if (extraEnemyTurns < 0) extraEnemyTurns = 0;
            if (extraEnemyTurns > 20) extraEnemyTurns = 20;

            bool includeEvents = @params?["includeEvents"] == null || PrototypeSimBridge.ReadBool(@params["includeEvents"]);

            CombatSimState sim = canonical.Clone();
            List<object> phases = new List<object>();

            for (int i = 0; i < plan.Beats.Count; i++)
            {
                List<ResolutionEvent> events = ActionResolver.ResolveBeat(sim, plan.Beats[i]);
                phases.Add(new
                {
                    phase = $"beat{i}",
                    actions = plan.Beats[i].Actions.Count,
                    eventCount = events.Count,
                    events = includeEvents ? PrototypeSimBridge.DescribeEvents(events) : null,
                    units = PrototypeSimBridge.DescribeUnits(sim)
                });
            }

            for (int turn = 0; turn <= extraEnemyTurns; turn++)
            {
                List<ResolutionEvent> events = ActionResolver.ResolveEnemyTurn(sim);
                phases.Add(new
                {
                    phase = $"enemyTurn{turn}",
                    actions = 0,
                    eventCount = events.Count,
                    events = includeEvents ? PrototypeSimBridge.DescribeEvents(events) : null,
                    units = PrototypeSimBridge.DescribeUnits(sim)
                });
            }

            List<PlayerUnit> players = sim.GetPlayers();
            List<EnemyUnit> enemies = sim.GetEnemies();
            string outcome = enemies.Count == 0 ? "victoria" : players.Count == 0 ? "derrota" : "en curso";

            return new SuccessResponse($"Simulacion en frio: {plan.Beats.Count} beats + {extraEnemyTurns + 1} turnos enemigos, resultado {outcome}.", new
            {
                outcome,
                playersAlive = players.Count,
                enemiesAlive = enemies.Count,
                beatCount = plan.Beats.Count,
                actionCount = plan.TotalActions,
                phases
            });
        }
    }
}
