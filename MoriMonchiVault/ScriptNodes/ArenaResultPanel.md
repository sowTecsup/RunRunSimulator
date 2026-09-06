---
tags: [script, world, ui, expedition]
---

# ArenaResultPanel.cs

**Ruta:** `World/Expedition/ArenaResultPanel.cs`

**Responsabilidad:** Panel UITK que muestra resultados finales de la ronda (S103): ganador (Ganaste/Perdiste/Empate), scores finales, y estadísticas por criatura (ocupación, asegurados, minados, reportes, golpes dados, caídas). Ordena filas por `Secured` descendente, columnas separadas por equipo (Jugador vs Rival). Consume `ArenaRoundStat` capturado por `ArenaRoundSummary`.

**Métodos públicos:**
- `Show(ExpeditionTeam winner, int mine, int theirs, IReadOnlyList<ArenaRoundStat> stats)` — muestra panel con resultado y estadísticas
- `Hide()` — oculta panel

**Internals:**
- `BuildRow(ArenaRoundStat stat) → VisualElement` — crea fila con swatch de color, nombre, ocupación, stats detalladas
- `Verb(Occupation occupation) → string` — mapea ocupación a verbo en español (vigiló, rompió, distrajo, exploró, recolectó)

**UI Structure (UXML):**
- `result-root` (result--show clase)
  - `result-title` (Label) — "Ganaste N-M", "Perdiste N-M", "Empate N-M" + clases result__title--win/lose/draw
  - `result-player` (VisualElement) — columna izquierda (equipo del jugador)
  - `result-rival` (VisualElement) — columna derecha (equipo rival)

**Row Structure:**
- `result-row` (VisualElement per criatura)
  - `result-row__swatch` (color de DNA)
  - `result-row__name`, `result-row__stats` (Labels)
    - Stats: "HACE · aseguró N · minó N · [avisó N] \n tumbó N · cayó N"

**S103:** Mostrada tras `resultHoldSeconds` (default 4s) cuando `ArenaRound.IsOver`, captura stats del sandbox.

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaRound]], [[ArenaRoundSummary]], [[ArenaPlanPanel]], [[Occupation]], [[ExpeditionTeam]]
