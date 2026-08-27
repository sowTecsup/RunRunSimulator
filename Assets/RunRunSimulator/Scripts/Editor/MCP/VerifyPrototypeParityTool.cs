using System.Collections.Generic;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Tools;
using Newtonsoft.Json.Linq;

namespace MoriMonchiSimulator.CombatPrototype.EditorTools
{
    [McpForUnityTool("verify_prototype_parity", Description = "Verifica la regla innegociable del prototipo de combate: proyeccion y ejecucion resuelven identico. Corre el plan por los dos caminos sobre clones, compara estado y eventos beat por beat mas el turno enemigo, y confirma que el estado canonico no se filtro. Requiere la escena CombatPrototype en Play mode.")]
    public static class VerifyPrototypeParityTool
    {
        public class Parameters
        {
            [ToolParameter("Plan opcional en JSON: {\"beats\":[{\"actions\":[{\"unitId\":0,\"abilityIndex\":0,\"targetCell\":[3,4],\"direction\":[1,0],\"slamCell\":[5,4]}]}]}. Si se omite usa el plan vivo del manager.", Required = false)]
            public string plan { get; set; }

            [ToolParameter("Incluye la lista completa de eventos por beat en la respuesta.", Required = false, DefaultValue = "false")]
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

            bool includeEvents = PrototypeSimBridge.ReadBool(@params?["includeEvents"]);

            string canonicalBefore = PrototypeSimBridge.StateSignature(canonical);
            ProjectionResult projection = PlanProjection.Project(canonical, plan);

            CombatSimState executed = canonical.Clone();
            List<List<ResolutionEvent>> executedBeatEvents = new List<List<ResolutionEvent>>();
            List<CombatSimState> executedAfterBeat = new List<CombatSimState>();

            foreach (Beat beat in plan.Beats)
            {
                executedBeatEvents.Add(ActionResolver.ResolveBeat(executed, beat));
                executedAfterBeat.Add(executed.Clone());
            }

            List<ResolutionEvent> executedEnemyEvents = ActionResolver.ResolveEnemyTurn(executed);
            string canonicalAfter = PrototypeSimBridge.StateSignature(canonical);

            bool match = true;
            List<object> beatReports = new List<object>();

            for (int i = 0; i < plan.Beats.Count; i++)
            {
                List<string> stateDiffs = PrototypeSimBridge.DiffStates(projection.StateAfterBeat[i], executedAfterBeat[i]);
                List<string> eventDiffs = PrototypeSimBridge.DiffEvents(projection.BeatEvents[i], executedBeatEvents[i]);
                if (stateDiffs.Count > 0 || eventDiffs.Count > 0) match = false;

                beatReports.Add(new
                {
                    beat = i,
                    actions = plan.Beats[i].Actions.Count,
                    events = executedBeatEvents[i].Count,
                    stateDiffs,
                    eventDiffs,
                    eventDetail = includeEvents ? PrototypeSimBridge.DescribeEvents(executedBeatEvents[i]) : null
                });
            }

            List<string> enemyStateDiffs = PrototypeSimBridge.DiffStates(projection.FinalState, executed);
            List<string> enemyEventDiffs = PrototypeSimBridge.DiffEvents(projection.EnemyTurnEvents, executedEnemyEvents);
            if (enemyStateDiffs.Count > 0 || enemyEventDiffs.Count > 0) match = false;

            bool canonicalIntact = canonicalBefore == canonicalAfter;
            string message = match && canonicalIntact
                ? $"Paridad OK: {plan.Beats.Count} beats + turno enemigo identicos, canonico intacto."
                : "DIVERGENCIA detectada: revisar diffs.";

            return new SuccessResponse(message, new
            {
                match,
                canonicalIntact,
                beatCount = plan.Beats.Count,
                actionCount = plan.TotalActions,
                beats = beatReports,
                enemyTurn = new
                {
                    events = executedEnemyEvents.Count,
                    stateDiffs = enemyStateDiffs,
                    eventDiffs = enemyEventDiffs,
                    eventDetail = includeEvents ? PrototypeSimBridge.DescribeEvents(executedEnemyEvents) : null
                },
                finalUnits = PrototypeSimBridge.DescribeUnits(executed)
            });
        }
    }
}
