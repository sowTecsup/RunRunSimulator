---
tags: [index, combat]
---

# 03 - Combat

**Responsabilidad:** Batallas por turnos entre criaturas (local + async UGS). Calculo stats efectivos, criticos, evolucion, muerte.

**Scripts:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[CombatManagerSO]] | `Data/CombatManagerSO.cs` | Configuracion global (DeathChance, MaxRounds, limite peleas) |
| [[CombatService]] | `Systems/Combat/CombatService.cs` | Simulacion local turn-based |
| [[CombatController]] | `Systems/Combat/CombatController.cs` | Orquestador flujo combate |
| [[AsyncCombatService]] | `Systems/Combat/AsyncCombatService.cs` | Combate async server-side (enqueue, poll, reconcile) |
| [[CombatRecord]] | `Data/CombatRecord.cs` | Registro serializable de combate completo |
| [[CombatTurn]] | `Data/CombatTurn.cs` | Struct de un turno (AttackerIsA, Damage, Crit, DefenderHP) |
| [[CombatLogEntry]] | `Data/CombatLogEntry.cs` | Entrada resumida del combat log para UI |
| [[CombatResult]] | `Data/CombatResult.cs` | Resultado final (winner, exp, loot) |

**Combat Visualizer (replay local, standalone):** reproduce en escena una pelea ya persistida en `CreatureDNA.CombatHistory`. No simula nada: lee el `CombatRecord` turno a turno y lo dramatiza. Bus visual propio (`CombatVisualEvents`), separado de `GameEvents`.

| Script | Ruta | Rol |
|--------|------|-----|
| [[CombatVisualizerService]] | `Systems/CombatVisualizer/CombatVisualizerService.cs` | Apex `.Instance`. Lista doblemente enlazada de estados (`CombatNode`), control Next/Back/auto/speed, A=self / B=opp (mapeo `SelfWasA`), muerte-desaparece, DEV harness Odin |
| [[CombatVisualEvents]] | `Systems/CombatVisualizer/CombatVisualEvents.cs` | Bus estatico visual + DTOs (`CombatVisualContext`, `CombatVisualHit`, `CombatVisualLogLine`/`Kind`, `CombatVisualPanelState`) |
| [[CombatVisualHooks]] | `Systems/CombatVisualizer/CombatVisualHooks.cs` | Puente a `UnityEvent` (Feel/MMFeedbacks) por `HookKind` Global/SideA/SideB |
| [[CombatVisualizerPanelUITK]] | `UI/CombatVisualizerPanelUITK.cs` | Header turno + log en cartas (ScrollView, colores) + controles ◀ ▶❚❚ ▶▶ + slider velocidad |
| [[MoriMonchiCombatVisualizerUITK]] | `UI/MoriMonchiCombatVisualizerUITK.cs` | Barra HP world-space por combatiente (hija del prefab, billboard, driven por el Service) |

**Flujo Visualizer:** `Play(self, opponent, record)` resuelve DBs de `GameManager` `BuildStates()` arma la cadena de `CombatNode` (orientando turnos por `SelfWasA`) spawnea 2 visualizers `VisualCombatStart` arranca en pausa en `head`. Avance (auto o manual `Next`): `TurnStart`+`Attack`(windup)+`Hit`/`Crit`+`PushHp`(impacto)+muerte(`SetActive false`)+`TurnEnd`. `Back` = `Restore(prev)` (estado puro, revive). El panel se reconstruye desde `OnPanelState`; las barras por referencia directa; los hooks escuchan los eventos granulares.

**Flujo Local:** Validacion stats efectivos loop turnos consecuencias (evolucion/muerte) GameEvents.OnCombatCompleted.

**Flujo Async:** Dos colas: instant_pool (inmediata) y matchmaking_pool (cron hourly). ReconcileGhostsAsync para limpieza.

**Reglas de Oro:**
- Cloud Code JS debe usar PascalCase (misma convencion que CombatTurn C#)
- AsyncCombatService fuerza PushToCloud en cada enqueue/dequeue
- ReconcileGhostsAsync detecta criaturas marcadas Queued pero perdidas en servidor
