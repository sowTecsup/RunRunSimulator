---
tags: [script, world, expedition, orchestrator]
---

# ArenaRound.cs

**Ruta:** `World/Expedition/ArenaRound.cs`

**Responsabilidad:** Orquestador de tiempo y puntuación de una ronda de arena (duración: `roundSeconds`, default 90s). Mantiene máquina de estados (reposo → activa → finalizada) con propiedades de lectura: `Elapsed`, `Remaining`, `PlayerSecured`, `RivalSecured` (suman `Secured` desde todas las ExitZones). Determina ganador por mayoría de puntos asegurados. Expone API pública: `Begin()`, `End()`, `Restart()` (debug). Congela puntuaciones en `End()` para evitar cambios post-round.

## Campos serializados

- **sandbox:** referencia a [[ArenaSandbox]] para acceder lista de salidas y respawnear
- **roundSeconds:** duración del round en segundos (default 90f, Min 10f)
- **autoStart:** si true, `Begin()` se llama automáticamente en Start()

## Propiedades públicas

- **Sandbox → ArenaSandbox** — referencia al sandbox (Read-only)
- **RoundSeconds → float** — duración configurada (Read-only)
- **Elapsed → float** — tiempo transcurrido desde Begin() (Read/Write)
- **Remaining → float** — tiempo restante (read-only, calcula `Max(0, roundSeconds - Elapsed)`)
- **IsRunning → bool** — true si el round está en marcha (Read/Write)
- **IsOver → bool** — true si el round ha terminado (Read/Write)
- **PlayerSecured → int** — unidades aseguradas por equipo Player (Read-only, suma viva o congelada)
- **RivalSecured → int** — unidades aseguradas por equipo Rival (Read-only, suma viva o congelada)
- **Winner → ExpeditionTeam** — ganador determinado en End() (Player, Rival, None para empate)

## Métodos públicos

- `Begin()` — reinicia Elapsed=0, IsRunning=true, IsOver=false, Winner=None
- `End()` — congela puntos en frozenPlayerSecured/frozenRivalSecured, calcula Winner, IsRunning=false, IsOver=true
- `Restart()` — sandbox.Respawn() + Begin() (botón Odin)

## Flujo

1. **Inicialización:** autoStart=true → Start() → Begin() (Elapsed=0, IsRunning=true)
2. **Durante round:** Update() incrementa Elapsed += Time.deltaTime mientras IsRunning
3. **Chequeo de tiempo:** si Elapsed >= roundSeconds → End()
4. **End():**
   - congela puntos: frozenPlayerSecured = SumSecured(Player), frozenRivalSecured = SumSecured(Rival)
   - calcula Winner: si frozenPlayerSecured == frozenRivalSecured → None, sino Player o Rival
   - IsRunning = false, IsOver = true
   - logs: `"[ArenaRound] fin: Player {pts} - Rival {pts} → {winner}"`
5. **Lectura de puntos:**
   - si IsRunning: PlayerSecured/RivalSecured leen vivo desde ExitZones.Secured
   - si !IsRunning: PlayerSecured/RivalSecured retornan congelados

## Invariantes S101

- Una sola instancia de ArenaRound por escena (el objeto "ArenaRound" en la escena)
- IsRunning y IsOver son mutuamente excluyentes (cuando IsOver=true, IsRunning=false)
- frozenPlayerSecured y frozenRivalSecured se calculan solo en End(), nunca se modifican post-End()
- Winner solo cambia en End()
- Remaining nunca es negativo (Mathf.Max con 0)
- SumSecured() itera ExitZones que coinciden con equipo (busca por exit.Team == team)

## Conexiones

**Entrada:**
- Lectura: `Time.deltaTime`, `sandbox.Exits` (lista de ExitZones)
- Setter público: `Begin()`, `End()`, `Restart()`

**Salida:**
- Propiedades `PlayerSecured`, `RivalSecured`, `Winner`, `IsRunning`, `IsOver` leídas por [[ArenaRoundHud]]
- Sandbox respawneado en `Restart()`

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[ArenaSandbox]]
- [[ExitZone]]
- [[ArenaRoundHud]]
