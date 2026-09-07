---
tags: [script, world, ui, expedition]
---

# ArenaResultPanel.cs

**Ruta:** `World/Expedition/ArenaResultPanel.cs`

**Responsabilidad:** Panel UITK que muestra resultados finales de ronda: ganador, scores, estadísticas por criatura. Ordena filas por Secured descendente, columnas por equipo. Consume ArenaRoundStat. **S104:** verbo derivado de órdenes (no ocupación), huidas agregadas a stats.

**Métodos públicos:**
- `void Show(ExpeditionTeam winner, int mine, int theirs, IReadOnlyList<ArenaRoundStat> stats)` — muestra panel con resultado
- `void Hide()` — oculta panel

**Internals:**
- `BuildRow(ArenaRoundStat stat) → VisualElement` — crea fila con swatch, nombre, verbo, stats
- `Verb(ArenaOrders orders) → string` — (S104 NUEVO) verbo desde órdenes: "vigiló", "cazó", "recolectó", "distrajo"

**UI Structure:**
- `result-root`
  - `result-title` (Label) — "Ganaste N-M" / "Perdiste N-M" / "Empate N-M"
  - `result-player` (VisualElement) — columna jugador
  - `result-rival` (VisualElement) — columna rival

**Row (S104):**
- Swatch color
- Nombre
- **Verbo por órdenes** (S104): `PastVerb(orders)`
- Stats: "aseguró N · minó N · huyó N · avisó N · tumbó X · cayó Y"

**S104 Cambios:**
- Verb(orders) en lugar de Verb(occupation)
- Huyó N agregado a stats
- ArenaRoundStat.Orders leído en BuildRow

**Invariantes:**
- Filas ordenadas por Secured (ranking)
- Verbo único por estrategia (no ocupación)
- Stats completo (asegurado, minado, huida, reportes, impacto)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaRound]], [[ArenaRoundSummary]], [[ArenaPlanPanel]], [[ArenaOrders]], [[ArenaOrderCatalog]], [[ExpeditionTeam]]
