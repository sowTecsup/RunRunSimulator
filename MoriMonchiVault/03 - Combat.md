---
tags: [memory-bank, combat, async, scheduler]
---

# 03 — Combat

## Responsabilidad Core (TL;DR)
Maneja la lógica de batallas por turnos entre criaturas (incluyendo cálculo de stats efectivos, críticos, límite de peleas, empates, evolución y muerte) tanto en modo simulación local como de forma asíncrona mediante UGS.

## Source of Truth & Centralización
- **Configuración Global:** `CombatManagerSO.cs` (Probabilidades de muerte/crítico, `MaxRounds`, límite de peleas).
- **Lógica de Simulación:** `CombatService.cs` (Motor puro de batalla matemática).
- **Lógica de Red (UGS):** `AsyncCombatService.cs` (Encolamiento, polling y reconciliación).
- **Registro:** `CreatureDNA.CombatHistory` contiene una lista de objetos `CombatRecord` que son deterministas y repetibles visualmente (replayables).

## Flujo de Combate Local
1. **Validación:** Se verifica que ambas criaturas no superen `MaxFightCount` (5), estén vivas y no estén ocupadas (`!IsBusy`).
2. **Setup:** Se calculan stats efectivos en runtime: `BaseStat + Σ(part.Stat + (tier-1))`. La estadística *Speed* determina el primer turno.
3. **Loop de Turnos:** Cada turno el atacante hace daño. Existe chance de crítico (×3 daño). El loop termina por muerte o `MaxRounds` (Empate).
4. **Consecuencias:** El ganador evoluciona una parte al azar; el perdedor tira los dados contra la `DeathChance`. El empate consume un combate pero nadie evoluciona ni muere.
5. **Cierre:** Se instancian `CombatRecord`s en los DNAs de los participantes y se dispara `GameEvents.OnCombatCompleted`.

## Flujo de Combate Async (UGS)
Existen dos colas separadas:
- **Modo Instant (`run-combat.js`):** Pool rápida (`instant_pool`) que ejecuta simulación inmediata si hay alguien esperando, devolviendo resultados instantáneos (útil para testing o colas en vivo).
- **Modo Timer (`process-matchmaking.js`):** El cliente se inscribe en la `matchmaking_pool` y la criatura queda bloqueada en `BusyState.QueuedForCombat`. El servidor procesa esta pool de forma masiva cada hora exacta (cron job). El cliente descarga los resultados después invocando `PollResultsAsync()`.

## Reglas de Oro (Invariantes)
- **Historial Formateado:** Los scripts de Cloud Code JS devuelven el historial del combate con la misma convención de nombrado (PascalCase) que `CombatTurn` de C#.
- **Persistencia de Queues:** Para no perder estado, `AsyncCombatService` fuerza un `PushToCloud()` cada vez que se encola o desencola una criatura (salva el `BusyState`).
- **Autolimpieza de Fantasmas:** Si una criatura está marcada como Queued localmente pero el servidor la perdió, `PollResultsAsync` usa `ReconcileGhostsAsync` para detectar la discrepancia y arreglar el estado.
