---
tags: [script, world, expedition, dev, harness]
---

# ArenaMatrixDev.cs

**Ruta:** `World/Expedition/ArenaMatrixDev.cs`

**Responsabilidad:** Harness de desarrollo que ejecuta simulaciones de arena de manera automatizada (matriz de planes × rivales × semillas). Itera sobre combinaciones, aplica órdenes/personalidades (Boldness, Sociability) a DNAs, spawnea, ejecuta ronda, registra resultado en CSV con estadísticas por criatura (secured, collected, hits, knocked, fled). Controla velocidad de simulación mediante Clock o timeScale. Salida: CSV con columnas i;j;player;rival;seed;pScore;rScore;winner;secs;pFled;rFled;pHits;rHits;pKnocked;rKnocked;pCol;rCol;detail.

**Propiedades públicas:**
- `ArenaSandbox Sandbox` — escena sandbox (requerido)
- `ArenaRound Round` — ronda (requerido)
- `ArenaClockControl Clock` — controlador de velocidad (opcional; fallback a timeScale)
- `float Speed = 10f` — multiplicador de velocidad simulación
- `bool IsRunning { get; }` — en ejecución
- `bool Done { get; }` — completado
- `int Completed { get; }` — conteo completado
- `int Total { get; }` — conteo total iteraciones
- `string OutputPath { get; }` — ruta CSV
- `string Progress { get; }` — string para UI progreso

**Métodos públicos:**
- `void Run(IReadOnlyList<ArenaMatrixTeam> players, IReadOnlyList<ArenaMatrixTeam> rivals, IReadOnlyList<int> seeds, string csvPath)` — inicia loop simulación
- `void Stop()` — aborta simulación activa

**Flujo Loop:**
1. Valida Sandbox/Round no null
2. Abre/crea CSV con header
3. Setea Clock/timeScale a Speed
4. Itera semillas → players → rivals:
   - `Sandbox.SetSeed(seed)` — fija semilla, apaga randomizeEachPlay
   - `Round.Reset(false)` — resetea ronda
   - `Apply(player, Player) / Apply(rival, Rival)` — inyecta órdenes y mutaciones DNA
   - `Round.Launch()` → espera IsOver o timeout (roundSeconds × 3 / timeScale + 30s)
   - `AppendLine(csvPath, ...)` — registra resultado + detail string
5. Al final: resetea timeScale, marca Done, escribe `.done`

**Apply internals:**
- Busca entries PlannedCast por team
- Para k < team.Orders.Length: aplica boldness, sociability, órdenes al entry.Dna

**AppendLine internals:**
- Itera Round.Summary, agrupa stats por team
- Traduce ArenaOrders a short code (via ArchetypeShort + 3 chars max)
- Escribe línea: index i/j, nombres, seed, scores, winner, secs reales, estadísticas agregadas, detail por criatura

**OnDisable:**
- Si IsRunning, resetea Clock/timeScale a 1f

**Integración:**
- Usado por devConsole u harness automatizado para balance testing
- Lee datos de `ArenaMatrixPlans.Plans16` / Subset10 / Personalities6
- Genera CSV para análisis estadístico post-simulación

**Invariantes:**
- Solo ejecuta si Sandbox y Round no null
- SetSeed() apaga randomizeEachPlay para reproducibilidad
- Cada iteración: Round.Reset(false) antes de Apply
- timeScale restaurado en OnDisable y al finalizar

**Vinculado a:** [[Index/23 - Arena Sandbox y Expedicion]]

**Conexiones:** [[ArenaSandbox]], [[ArenaMatrixPlans]], [[ArenaRound]], [[ArenaClockControl]], [[ArenaOrders]], [[ArenaOrderCatalog]], [[ExpeditionTeam]]
