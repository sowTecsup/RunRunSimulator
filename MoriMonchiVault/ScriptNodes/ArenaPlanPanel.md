---
tags: [script, world, ui, uitk, expedition]
---

# ArenaPlanPanel.cs

**Ruta:** `World/Expedition/ArenaPlanPanel.cs`

**Responsabilidad:** Panel UITK de planificación pre-ronda (S103 actualizado). Permite seleccionar ocupación y sitio por criatura, alternar entre modo save local (picker) vs roster básico. Integra `ArenaCastPicker` (S103 NUEVO) para elegir MoriMonchis del save. Integra `ArenaResultPanel` (S103 NUEVO) para mostrar resultado tras combate. Botones: Mis MoriMonchis (togglea modo), Picker (abre selector), Shuffle (aleatoriza), Paleta (cicla), Sala (nueva seed), ¡A LA SALA! (lanza). La quinta píldora "Explora" activa ocupación Explore.

**Métodos públicos:**
- `Update()` — gestiona transición visible/oculto y delay de resultado

**Constantes:**
- Ocupaciones: [Gather, Guard, Break, Decoy, Explore] → etiquetas españolas
- Sitios: [Center, NearVein, FarVein]

**Campos Serializados:**
- `sandbox` [Required] — ArenaSandbox
- `round` [Required] — ArenaRound
- `picker` [Required] — ArenaCastPicker (S103 NUEVO)
- `resultPanel` [Required] — ArenaResultPanel (S103 NUEVO)
- `resultHoldSeconds` [Min(0)] = 4 — delay antes de ocultar resultado

**UI Structure (UXML):**
- `plan-root` (plan--hidden clase)
  - `plan-room` (Label) — "sala NNNN · PaletteName · entrada Entry"
  - `plan-rival` (Label) — "Rival: nombre1 · nombre2 · ... · entra por lado opuesto"
  - `cast-list` (VisualElement) — lista de tarjetas de criaturas player
    - Cada `cast-card` — swatch, nombre, dials, dos filas de pills
      - `plan-row` ocupaciones [Recolecta, Vigila, Rompe, Distrae, Explora]
      - `plan-row` sitios [Centro, Veta cercana, Veta lejana] (deshabilitados si Decoy/Explore)
  - Botones: `btn-cast`, `btn-pick`, `btn-shuffle`, `btn-palette`, `btn-room`, `btn-play`

**Métodos Privados:**
- `SetVisible(bool)` — aplica plan--hidden, llama Refresh si visible
- `Refresh()` — actualiza room label, cast button, construye cards, rival line
- `BuildCards()` — itera PlannedCast, solo Player entries
- `BuildCard(int index, ArenaCastEntry entry)` → VisualElement — swatch + nombre + dials + pills ocupación/sitio
- `ChooseOccupation/ChooseSite(Card state, int choice)` — llamadas de pills, actualiza `sandbox.SetPlayerPlan()`
- `RefreshPills(Card)` — destaca pills activas (pill--on clase)
- `RefreshRivalLine()` — lista nombres rivales
- `ToggleCastMode()` — alterna ArenaCastMode.LocalSave ↔ Roster
- `OpenPicker()` — picker.Open(Refresh) (S103 NUEVO)
- `Shuffle()` — sandbox.ShuffleCast()
- `CyclePalette()` — sandbox.CyclePalette()
- `NewRoom()` — round.Reset(true), resultPanel.Hide()
- `Play()` — resultPanel.Hide(), round.Launch()

**S103 Cambios:**
- `ArenaCastPicker picker` [Required] — selector modal (S103 NUEVO)
- `ArenaResultPanel resultPanel` [Required] — panel resultado (S103 NUEVO)
- Quinta píldora "Explora" (Occupation.Explore) con etiqueta correspondiente
- `OpenPicker()` nuevo, invocado por `btn-pick`
- En `RefreshPills()`: sitios deshabilitados si Explore además de Decoy (Explore no elige sitio)
- En `Update()`: llama `resultPanel.Show(pendingWinner, pendingMine, pendingTheirs, round.Summary)` tras `round.Reset(false)`
- `btn-pick` habilitado solo si CastMode=LocalSave y LocalAvailable

**Ciclo S103:**
1. Panel visible, elenco mostrado
2. Jugador elige ocupación/sitio O abre picker (btn-pick → ArenaCastPicker)
3. Picker cierra con SelectLocalCast → Refresh automática
4. Jugador presiona Play → oculta panel, round.Launch()
5. Ronda corre
6. round.IsOver → resultPanel.Show() tras resultHoldSeconds, visible=true
7. Jugador presiona Sala (nuevo) o cierra → vuelve a flow 1

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[ArenaRound]], [[ArenaCastPicker]], [[ArenaResultPanel]], [[ArenaCastEntry]], [[Occupation]], [[ArenaSite]]
