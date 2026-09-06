---
tags: [script, world, expedition, orchestrator]
---

# ArenaRound.cs

**Ruta:** `World/Expedition/ArenaRound.cs`

**Responsabilidad:** Orquestador de tiempo y puntuación de ronda de arena (S103 actualizado). Máquina de estados: reposo → activa → finalizada. Corre contador, determina ganador por asegurados. Launch = SpawnCast + Begin. Reset(newSeed) = ResetRoom + reset. End() congela puntos y captura `ArenaRoundSummary` (S103 NUEVO) para mostrar resultado. Propiedades: Elapsed, Remaining, PlayerSecured, RivalSecured, Winner, **Summary** (S103 NUEVO).

**Métodos públicos:**
- `Launch()` — SpawnCast() + Begin() (inicia ronda)
- `Reset(bool newSeed)` — ResetRoom(newSeed) + reset contadores
- `Begin()` — Elapsed=0, IsRunning=true, IsOver=false (interno, llamado por Launch)
- `End()` — congela puntos, captura Summary, calcula Winner, IsRunning=false, IsOver=true
- `[Button] Restart()` — Reset(false) + Launch() (debug)

**Propiedades públicas:**
- `bool IsRunning { get; }` — ronda activa
- `bool IsOver { get; }` — ronda terminada
- `float Elapsed { get; }` — segundos transcurridos desde Begin
- `float Remaining { get; }` — Max(0, RoundSeconds - Elapsed)
- `int PlayerSecured { get; }` — lectura viva si IsRunning, congelada si IsOver
- `int RivalSecured { get; }` — lectura viva si IsRunning, congelada si IsOver
- `ExpeditionTeam Winner { get; }` — Player, Rival, o None (empate)
- `IReadOnlyList<ArenaRoundStat> Summary { get; }` — (S103 NUEVO) estadísticas capturadas al End()

**Campos Serializados:**
- `sandbox` [Required] — ArenaSandbox
- `roundSeconds` [Min(10)] = 90 — duración
- `autoStart` (default false)

**Privados:**
- `frozenPlayerSecured`, `frozenRivalSecured` (int) — congeladas en End()
- `summary` (List<ArenaRoundStat>) — (S103 NUEVO) capturado en End()

**Métodos Privados:**
- `SumSecured(ExpeditionTeam team) → int` — suma de .Secured en ExitZone
- `Update()` — si IsRunning: Elapsed += Time.deltaTime, si >= roundSeconds: End()

**S103 Cambios:**
- `IReadOnlyList<ArenaRoundStat> Summary { get; }` — propiedad nueva
- `summary` (List<ArenaRoundStat>) — campo privado
- En `End()`: antes de IsRunning=false, captura: `summary.AddRange(ArenaRoundSummary.Capture(sandbox.Spawned))`
- En `Reset()`: `summary.Clear()`

**Ciclo S103:**
1. ArenaPlanPanel.Play() → Launch()
2. SpawnCast(), Begin() → IsRunning=true
3. Update() cuenta tiempo
4. Elapsed >= RoundSeconds → End()
5. End() congela puntos y captura Summary vía ArenaRoundSummary
6. ArenaPlanPanel detecta IsOver, espera, llama resultPanel.Show(Winner, PlayerSecured, RivalSecured, Summary)
7. Reset(false) → relimpia, vuelve a plan

**Invariantes:**
- Launch único: SpawnCast solo desde Launch
- Reset sin comienza: no inicia contador (Launch lo hace)
- IsRunning y IsOver mutuamente excluyentes
- Summary inmutable tras End()
- Remaining nunca negativo

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[ExitZone]], [[ArenaRoundSummary]], [[ArenaRoundHud]], [[ArenaPlanPanel]], [[ArenaResultPanel]], [[ExpeditionTeam]]
