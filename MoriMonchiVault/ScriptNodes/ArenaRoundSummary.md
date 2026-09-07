---
tags: [script, data, expedition]
---

# ArenaRoundSummary.cs

**Ruta:** `World/Expedition/ArenaRoundSummary.cs`

**Responsabilidad:** Captura estadísticas finales de ronda en struct `ArenaRoundStat` por criatura. Extrae DNA, **órdenes (S104)**, team, counters (asegurados, minados, huidas, golpes, caídas, reportes). Consumida por ArenaRound.End() y ArenaResultPanel.Show().

**Struct ArenaRoundStat:**
- `string Name` — CustomName de DNA
- `ExpeditionTeam Team` — Player o Rival
- `ArenaOrders Orders` — órdenes activas (S104 NUEVO)
- `Color Color` — BaseColor
- `int Secured` — material depositado en salida
- `int Collected` — material minado total
- `int Fled` — conteo de huidas (S104 NUEVO)
- `int HitsLanded` — golpes exitosos en clash
- `int TimesKnocked` — veces derribado
- `int Reports` — vetas reportadas (scouts)

**Métodos públicos:**
- `static List<ArenaRoundStat> Capture(IReadOnlyList<MoriMonchiController> spawned)` → List<ArenaRoundStat> — itera controllers, extrae stats de cada agente

**S104 Cambios:**
- ArenaRoundStat.Orders agregado (extrae agent.Orders)
- ArenaRoundStat.Fled agregado (extrae agent.TimesFled)
- Usado por ArenaResultPanel para mostrar "huyó N veces"

**Invariantes:**
- Stats congelados post-ronda (snapshot)
- Ordenados por Secured en resultado (ranking)
- Verbo de ocupación + huyó N en descripción

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMonchiController]], [[MoriMochiAgent]], [[ArenaRound]], [[ArenaResultPanel]], [[CreatureDNA]], [[ArenaOrders]]
