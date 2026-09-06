---
tags: [script, world, ui, expedition, presentation]
---

# ArenaRoundHud.cs

**Ruta:** `World/Expedition/ArenaRoundHud.cs`

**Responsabilidad:** HUD de ronda UITK en vivo (S103 reescrito). Muestra arriba: seed (sala NNNN), player score, timer con aviso de tiempo bajo (warnSeconds), rival score. Abajo: dos columnas (player team | rival team) con roster vivo. Cada fila por criatura: swatch de color, nombre, ocupación+intención, minería en progreso (barra), materiales en mano. Si IsOver, overlay resultado (Gana tu equipo/Gana rival/Empate). Cache de strings y valores para evitar ediciones DOM innecesarias.

**Métodos públicos:**
- `Update()` — tick principal (refresh si IsRunning o IsOver, actualiza scores, tiempo, roster, resultado)

**Métodos privados:**
- `RefreshSeed()` — "sala {ActiveSeed}"
- `RefreshRoster()` — itera Spawned, construye rows separadas por team, crea/limpia buffer de Rows
- `BuildRow(MoriMochiAgent agent)` — crea VisualElement con componentes (swatch, name, sub, mine bar, carry label)
- `Verb(Occupation)` → string — Guard→"vigila", Break→"rompe", Decoy→"distrae", Explore→"explora", default→"recolecta"

**UI Structure (UXML/USS S103):**
- `hud-root` (hud--idle cuando no running/over)
  - `hud-seed` (Label) — "sala NNNN" (siempre visible)
  - Marcador: `hud-player-score` | `hud-time` | `hud-rival-score` (columnas)
    - `hud-time` (Label con clases hud-time--warn cuando ≤ warnSeconds)
    - `hud-bar-fill` (relleno de progreso con clases hud-bar__fill--warn)
  - Columnas: `hud-player-team` (VisualElement) | `hud-rival-team` (VisualElement)
    - Cada uno contiene múltiples `hud-row` (hud-row--rival para rival)
      - `hud-row__swatch` (color DNA)
      - `hud-row__text` (flex column)
        - `hud-row__name` (Label)
        - `hud-row__sub` (Label) — "ocupación · intención"
        - `hud-row__mine` (barra de minería con `hud-row__mine-fill`)
      - `hud-row__carry` (Label) — "◆ N" o vacío

**Campos Serializados:**
- `round` [Required] — ArenaRound
- `warnSeconds` [Min(0)] = 15 — threshold para activar aviso de tiempo

**Internals (Class Row):**
- MoriMochiAgent Agent
- Label Sub, Carry
- VisualElement Mine, MineFill
- string LastSub, int LastCarried, float LastProgress (caché)

**S103:** Reescrita con UXML/USS para mejor control visual. Cronómetro con warn al acercarse el fin (rojo). Estadísticas vivas: ocupación + intención (intent names desde LocEnumMaps), minería en progreso, carga. Resultado overlay opcional. Columnas por equipo visualizan estrategia en vivo.

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaRound]], [[ArenaSandbox]], [[MoriMochiAgent]], [[CreatureDNA]], [[Occupation]], [[CreatureIntent]], [[LocEnumMaps]]
