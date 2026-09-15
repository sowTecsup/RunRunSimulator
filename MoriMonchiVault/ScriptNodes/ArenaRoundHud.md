---
tags: [script, world, ui, exposition, expedition]
---

# ArenaRoundHud.cs

**Ruta:** `World/Expedition/ArenaRoundHud.cs`

**Responsabilidad:** HUD de ronda UITK en vivo. Muestra tarjetas por equipo (arriba/abajo), score, timer (con pausa/velocidad), equipo rival como fichas. S107: Usa ArenaHudCard para tarjetas player. S111: Muestra nombre de forma junto a "sala NNNN". Cada tarjeta/ficha sincroniza estado visual cada frame: swatch, nombre, ocupación, minería, carga de habilidades. Cache para evitar DOM ediciones innecesarias. **S118:** Sincroniza Armed state de Super abilities, integra click en RadialSlot para Request.

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

- `RefreshRoster()` — reconstruye cards/chips si count cambió; **S118:** wira callback onPowerTapped

- `OnCardTapped(MoriMochiAgent agent)` — callback Click, llama director.TogglePin(agent)

- **S118 NUEVO:** `OnPowerTapped(MoriMochiAgent agent, int index)` — callback RadialSlot click:
  - Delega a round.Request(agent, index) para solicitar Super
  - UI feedback inmediato: Armed state se activa

- `BuildChip(MoriMochiAgent agent)` — crea VisualElement rival compacto (RivalChip)

- `RefreshCarryLabel(Label label, int carried, int capacity)` — **S118:** muestra "N/M en manos" o "vacías"

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
- **S118 NUEVOS:**
  - `lastPlayerCarryText`, `lastRivalCarryText` — caché carry labels
  - `lastPlayerCarrySome`, `lastRivalCarrySome` — flags de cambio

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
6. Sincroniza scores, **S118:** carry counts (N/M en manos)
7. Timer: MM:SS, warn si <= warnSeconds
8. Bar: width = Remaining/Total * 100%
9. Recorre chips y cards, refresca cada uno:
   - **S118:** cards[i].Refresh(selected) incluye sincronización de Armed state
10. Si IsOver, muestra resultado (win/lose/draw styles)

**Integración:**

- OnEnable: resuelve UIDocument, wira callbacks (pauseButton, speedButton, etc.)
  - **S118:** wira onPowerTapped callback en cada card creada
- OnDisable: desuscribe, limpia cards/chips

**S107 Cambios:**
- `cards` contiene instancias de ArenaHudCard

**S111 Cambios:**
- RefreshSeed() ahora muestra "ShapeName · sala NNNN · Nx"
- Informa qué forma se está jugando en vivo

**S118 Cambios:**

- Construcción RefreshRoster:
  ```csharp
  var card = new ArenaHudCard(agent, OnCardTapped, OnPowerTapped);
  ```
  - Wira callback onPowerTapped para clicks en RadialSlots

- Nuevo método OnPowerTapped:
  ```csharp
  private void OnPowerTapped(MoriMochiAgent agent, int index)
  {
      round.Request(agent, index);
  }
  ```
  - Invocado cuando jugador toca RadialSlot de Super ability
  - Solicita disparo a round, que notifica a agent.abilities

- Caché de carry labels:
  - `lastPlayerCarryText`, `lastRivalCarryText` — evita repaint de "N/M"
  - `lastPlayerCarrySome`, `lastRivalCarrySome` — flags de cambio para estilos CSS (bold si carrying, tenue si vacío)

- RefreshPowers() en ArenaHudCard sincroniza Armed state (via callback desde UI, Refresh invoca cada frame)

**Invariantes S118:**

- Caché de strings evita spam DOM
- Tarjetas y fichas separadas por contenedor
- ArenaHudCard maneja su propio refresh (incluyendo Armed)
- Super abilities solo se pueden solicitar si Charge >= 1.0 (validado en agent.abilities.Request)
- Armed state es visual-only, feedback de intención del jugador (no afecta lógica)
- Carry label "N/M en manos" muestra ocupación de capacidad

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion]], [[Index/22 - Bajada Nocturna y Linaje]], S118

**Conexiones:** [[ArenaRound]], [[ArenaClockControl]], [[ArenaCameraDirector]], [[ArenaHudCard]], [[RadialSlot]], [[MoriMochiAgent]], [[AgentAbilities]], [[CreatureDNA]], [[ArenaOrderCatalog]], [[LocEnumMaps]], [[ArenaLayoutBuilder]]
