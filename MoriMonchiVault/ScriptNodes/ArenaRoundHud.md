---
tags: [script, world, ui, exposition, expedition]
---

# ArenaRoundHud.cs

**Ruta:** `World/Expedition/ArenaRoundHud.cs`

**Responsabilidad:** HUD de ronda UITK en vivo. Muestra tarjetas estilo Pokémon Quest por equipo (arriba/abajo o lado), score, timer (S104: con control de pausa/velocidad vía ArenaClockControl), equipo rival como fichas. **S107:** Usa ArenaHudCard para tarjetas player (componente reutilizable); conserva RivalChip interno simplificado para fichas rival. Cada tarjeta/ficha sincroniza estado visual cada frame: swatch, nombre, ocupación+intención, minería en progreso, carga de habilidades. Cache de strings y valores para evitar DOM ediciones innecesarias.

**Campos Serializados:**
- `round` [Required] — referencia a ArenaRound
- `clock` (ArenaClockControl) — para TogglePause/CycleSpeed
- `director` (ArenaCameraDirector) — para TogglePin
- `warnSeconds` (float, Min 0, default 15) — segundos para activar warn de timer

**Métodos Públicos:**
- `void Update()` — tick principal

**Métodos Privados:**
- `RefreshSeed()` — muestra "sala NNNN · 2×" (seed + speed si no 1×)
- `RefreshClockButtons()` — actualiza labels pauseButton ("▶"/"II") y speedButton ("▶ Nx")
- `RefreshRoster()` — reconstruye cards/chips si count cambió. Para Player: `new ArenaHudCard(agent, OnCardTapped)`, para Rival: `BuildChip(agent)`
- `OnCardTapped(MoriMochiAgent agent)` — callback Click, llama `director.TogglePin(agent)`
- `BuildChip(MoriMochiAgent agent)` — crea VisualElement rival compacto (interno, RivalChip)
- `RefreshCarryLabel(Label label, int carried, ref string lastText, ref bool lastSome)` — "N en manos" con clase CSS si > 0

**Estructura UIDocument:**
- `hud-root`
  - `hud-header` (seed, scores, timer con warn style)
  - `hud-bar-fill` (barra de tiempo, fill width = %)
  - `hud-player-team` (contenedor de ArenaHudCard.Root)
  - `hud-rival-team` (contenedor de RivalChip.Root)
  - `hud-result` (mostrado si IsOver con win/lose/draw styles)

**Campos Internos (Caché):**
- `cards` (List<ArenaHudCard>) — tarjetas player, refrescadas cada frame
- `chips` (List<RivalChip>) — fichas rival, refrescadas cada frame
- `lastRosterCount` — detección de cambios en count
- `lastSeedText`, `lastPlayerScoreText`, `lastTimeText`, `lastRivalScoreText`, `lastResultText` — caché de strings
- `lastBarPercent`, `lastTimeWarn` — caché de valores numéricos
- `resultShown`, `lastShown` — flags de transición

**RivalChip (clase interna):**
- `Agent` — referencia
- `Action` (Label) — intención actual
- `Root` (VisualElement) — contenedor clickable
- `LastAction`, `LastSelected`, `LastChase` — caché para refresh

**Ciclo Update():**
1. Si round null, retorna
2. RefreshSeed, RefreshClockButtons
3. shown = IsRunning || IsOver
4. Si shown cambió: actualiza clase hud--idle, reset roster si now shown
5. Si IsOver pero resultShown es false, oculta resultado
6. Si no shown, retorna
7. RefreshRoster (construye cards/chips si necesario)
8. Sincroniza scores, carry counts (playerCarried/rivalCarried sumados del Spawned)
9. Timer: convierte Remaining a MM:SS, warn si <= warnSeconds
10. Bar: width = Clamp01(Remaining/RoundSeconds) * 100%
11. Recorre chips y cards, refresca cada uno vía métodos internos
12. Si IsOver, muestra resultado con color según Winner (Player=win, Rival=lose, None=draw)

**Integración:**
- OnEnable: resuelve UIDocument, wira callbacks de pauseButton/speedButton
- OnDisable: desuscribe callbacks, limpia cards/chips/contenedores
- Refrescado cada frame en Update()

**S107 Cambios:**
- `cards` ahora contiene instancias de ArenaHudCard (clase pública reutilizable)
- Cambio en RefreshRoster(): `var card = new ArenaHudCard(agent, OnCardTapped)` en lugar de BuildCard interno
- Callback `OnCardTapped` pasa card.Agent a director
- RivalChip se simplificó a clase interna (solo acción, sin radiales/carry)

**Invariantes:**
- Caché de strings evita spam DOM
- Tarjetas y fichas separadas por contenedor (playerTeam/rivalTeam)
- ArenaHudCard maneja su propio refresh (radiales, carry, hit feedback)
- RivalChip sigue siendo interna (no reutilizable, propósito específico)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaRound]], [[ArenaClockControl]], [[ArenaCameraDirector]], [[ArenaHudCard]], [[MoriMochiAgent]], [[CreatureDNA]], [[ArenaOrderCatalog]], [[LocEnumMaps]]
