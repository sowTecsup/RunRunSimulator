---
tags: [script, data, expedition]
---

# ArenaRoundSummary.cs

**Ruta:** `World/Expedition/ArenaRoundSummary.cs`

**Responsabilidad:** Captura estadísticas de una ronda de arena (S103) en un struct `ArenaRoundStat` por criatura. Clase estática `ArenaRoundSummary` con método `Capture()` que itera `MoriMonchiController` spawned, extrae DNA, ocupación, team, counters (asegurados, minados, golpes, caídas, reportes) y color. Consumida por `ArenaRound.End()` y `ArenaResultPanel.Show()`.

**Struct ArenaRoundStat:**
- `string Name` — CustomName de DNA
- `ExpeditionTeam Team` — Player o Rival
- `Occupation Occupation` — qué hace
- `Color Color` — BaseColor (alpha forzado a 1)
- `int Secured` — `agent.SecuredMaterial`
- `int Collected` — `agent.CollectedMaterial`
- `int HitsLanded` — `agent.ClashHitsLanded`
- `int TimesKnocked` — `agent.ClashTimesKnocked`
- `int Reports` — `agent.ScoutReports`

**Métodos públicos:**
- `static List<ArenaRoundStat> Capture(IReadOnlyList<MoriMonchiController> spawned)` — itera controllers, extrae stats de cada agente

**S103:** Llamado por `ArenaRound.End()` para freezar stats finales antes de mostrar resultado. Panel de resultados ordena por `Secured` y renderiza filas con verbo de ocupación.

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMonchiController]], [[MoriMochiAgent]], [[ArenaRound]], [[ArenaResultPanel]], [[CreatureDNA]]
