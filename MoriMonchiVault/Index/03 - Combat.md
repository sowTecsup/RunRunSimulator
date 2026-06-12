---
tags: [memory-bank, combat, async, scheduler]
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

**Flujo Local:** Validacion stats efectivos loop turnos consecuencias (evolucion/muerte) GameEvents.OnCombatCompleted.

**Flujo Async:** Dos colas: instant_pool (inmediata) y matchmaking_pool (cron hourly). ReconcileGhostsAsync para limpieza.

**Reglas de Oro:**
- Cloud Code JS debe usar PascalCase (misma convencion que CombatTurn C#)
- AsyncCombatService fuerza PushToCloud en cada enqueue/dequeue
- ReconcileGhostsAsync detecta criaturas marcadas Queued pero perdidas en servidor
