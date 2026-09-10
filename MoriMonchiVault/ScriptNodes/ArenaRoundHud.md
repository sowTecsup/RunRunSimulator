---
tags: [script, world, ui, exposition, expedition]
---

# ArenaRoundHud.cs

**Ruta:** `World/Expedition/ArenaRoundHud.cs`

**Responsabilidad:** HUD de ronda UITK en vivo. Muestra tarjetas por equipo (arriba/abajo), score, timer (con pausa/velocidad), equipo rival como fichas. **S107:** Usa ArenaHudCard para tarjetas player. **S111:** Muestra nombre de forma junto a "sala NNNN". Cada tarjeta/ficha sincroniza estado visual cada frame: swatch, nombre, ocupación, minería, carga de habilidades. Cache para evitar DOM ediciones innecesarias.

**Campos Serializados:**
- `round` [Required] — referencia a ArenaRound
- `clock` (ArenaClockControl) — para TogglePause/CycleSpeed
- `director` (ArenaCameraDirector) — para TogglePin
- `warnSeconds` (float, Min 0, default 15) — segundos para activar warn de timer

**Métodos Públicos:**
- `void Update()` — tick principal

**Métodos Privados:**
- **S111:** `RefreshSeed()` — muestra "Forma · sala NNNN · 2×" (shape.Name + seed + speed si no 1×)
- `RefreshClockButtons()` — actualiza labels pauseButton ("▶"/"II") y speedButton ("▶ Nx")
- `RefreshRoster()` — reconstruye cards/chips si count cambió
- `OnCardTapped(MoriMochiAgent agent)` — callback Click, llama director.TogglePin(agent)
- `BuildChip(MoriMochiAgent agent)` — crea VisualElement rival compacto (RivalChip)
- `RefreshCarryLabel(Label label, int carried, ...)` — "N en manos"

**Estructura UIDocument:**
- `hud-root`
  - `hud-header` (seed/forma, scores, timer con warn style)
  - `hud-bar-fill` (barra de tiempo)
  - `hud-player-team` (contenedor de ArenaHudCard.Root)
  - `hud-rival-team` (contenedor de RivalChip.Root)
  - `hud-result` (resultado si IsOver)

**Campos Internos (Caché):**
- `cards` (List<ArenaHudCard>) — tarjetas player
- `chips` (List<RivalChip>) — fichas rival
- `lastRosterCount` — detección de cambios
- `lastSeedText` — caché de "Forma · sala NNNN"
- `lastPlayerScoreText`, `lastTimeText`, `lastRivalScoreText` — caché de strings
- `lastBarPercent`, `lastTimeWarn` — caché de valores
- `resultShown`, `lastShown` — flags de transición

**RivalChip (clase interna):**
- `Agent` — referencia
- `Action` (Label) — intención actual
- `Root` (VisualElement) — contenedor
- `LastAction`, `LastSelected`, `LastChase` — caché

**Ciclo Update():**
1. Si round null, retorna
2. **S111:** RefreshSeed con forma + sala
3. RefreshClockButtons
4. shown = IsRunning || IsOver
5. RefreshRoster (si count cambió)
6. Sincroniza scores, carry counts
7. Timer: MM:SS, warn si <= warnSeconds
8. Bar: width = Remaining/Total * 100%
9. Recorre chips y cards, refresca cada uno
10. Si IsOver, muestra resultado (win/lose/draw styles)

**Integración:**
- OnEnable: resuelve UIDocument, wira callbacks
- OnDisable: desuscribe, limpia cards/chips

**S107 Cambios:**
- `cards` contiene instancias de ArenaHudCard

**S111 Cambios:**
- RefreshSeed() ahora muestra "ShapeName · sala NNNN · Nx"
- Informa qué forma se está jugando en vivo

**Invariantes:**
- Caché de strings evita spam DOM
- Tarjetas y fichas separadas por contenedor
- ArenaHudCard maneja su propio refresh

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaRound]], [[ArenaClockControl]], [[ArenaCameraDirector]], [[ArenaHudCard]], [[MoriMochiAgent]], [[CreatureDNA]], [[ArenaOrderCatalog]], [[LocEnumMaps]], [[ArenaLayoutBuilder]]
