---
tags: [script, ui, uitk, expedition, panel]
---

# ArenaPlanPanel.cs

**Ruta:** `World/Expedition/ArenaPlanPanel.cs`

**Responsabilidad:** Panel UITK que permite al jugador seleccionar ocupación y sitio para cada criatura propia antes de lanzar la ronda. Se oculta automáticamente cuando la ronda corre, se muestra de nuevo tras resultado con delay configurable, permite cambiar elenco/paleta/sala sin reiniciar.

## Campos Serializados

- `sandbox` (ArenaSandbox, Required) — referencia a gestor de arena
- `round` (ArenaRound, Required) — referencia a gestor de ronda
- `resultHoldSeconds` (float, Min 0, default 4) — segundos a mostrar resultado antes de volver a plan

## Constantes

**Ocupaciones:**
- Array: [Gather, Guard, Break, Decoy]
- Labels: ["Recolecta", "Vigila", "Rompe", "Distrae"]

**Sitios:**
- Array: [Center, NearVein, FarVein]
- Labels: ["Centro", "Veta cercana", "Veta lejana"]

## Ciclo de Vida

**OnEnable():**
- Obtiene rootVisualElement desde UIDocument
- Cachea referencias a VisualElements (castList, labels, botones)
- Suscribe clicks: ToggleCastMode, Shuffle, CyclePalette, NewRoom, Play
- Inicializa estado (-1, MinValue)
- SetVisible(true)

**OnDisable():**
- Desuscribe todos los clicks

**Update():**
- Si round.IsRunning y visible: oculta panel, marca roundEndHandled=false
- Si round.IsOver y !roundEndHandled: guarda resultado, espera resultHoldSeconds
- Pasado delay: round.Reset(false), muestra resultado, SetVisible(true)
- Si visible y cambios en PlannedCast.Count o Seed: Refresh()

## Estados Privados

- `visible` — si panel es visible (clase plan--hidden aplicada si false)
- `roundEndHandled` — bandera para ejecutar lógica de fin una sola vez
- `roundEndedAt` — Time.time cuando round.IsOver se detectó
- `pendingResult` — string de resultado a mostrar
- `lastPlannedCount`, `lastSeed` — para detectar cambios en Refresh()

## Métodos Privados

**Refresh():**
- Actualiza roomLabel: `"sala {seed} · {paletteName} · entrada {entryName}"`
- Actualiza castButton: "Mis MoriMonchis" (o "sin save" si !LocalAvailable)
- BuildCards() — borra castList y recrea tarjetas de criaturas Player
- RefreshRivalLine() — muestra nombres de rivales + "entra por el lado opuesto"

**BuildCards():**
- Por cada entry en PlannedCast con Team=Player y Dna!=null:
  - BuildCard(index, entry) → VisualElement con swatch, nombre, dials, pills

**BuildCard(index, entry):**
```
Card visual:
  ┌─ head (color swatch + nombre + dials)
  ├─ HACE fila: 4 pills (ocupación)
  └─ DÓNDE fila: 3 pills (sitio, disabled si Decoy)
```
- Crea Card { Index, OccupationPills[], SitePills[] }
- Pills son botones que llaman ChooseOccupation/ChooseSite
- RefreshPills() destaca pill activa

**ChooseOccupation/ChooseSite:**
- Llama sandbox.SetPlayerPlan(index, occupation, site)
- RefreshPills() actualiza visuales

**Acciones de Botones:**
- `ToggleCastMode()` — alterna LocalSave ↔ Roster, refresh
- `Shuffle()` — sandbox.ShuffleCast(), refresh
- `CyclePalette()` — sandbox.CyclePalette(), refresh
- `NewRoom()` — round.Reset(true), limpia resultado, refresh
- `Play()` — round.Launch(), oculta panel

## Invariantes S102

- **Auto-ocultarse:** se oculta cuando IsRunning, se muestra tras IsOver + delay
- **Sin persistencia UI:** estado de cards (lastPlannedCount) se resetea en OnEnable
- **Decoy sitios disabled:** sitios deshabilitados si Occupation.Decoy (no aplica)
- **Resultado hold:** espera resultHoldSeconds antes de volver a plan (permite ver número)

## Flujo de Sesión

```
1. Panel visible con elenco (Player + Rival)
2. Jugador ajusta ocupaciones/sitios de propios
3. Jugador presiona ¡A LA SALA! (Play)
4. round.Launch() → Panel oculto
5. Combate corre (IsRunning)
6. round.IsOver → Resultado visible durante resultHoldSeconds
7. Pasa tiempo → round.Reset(false) → vuelve a panel visible
```

## Conexiones

- [[ArenaSandbox]] (PlannedCast, SetPlayerPlan, CastMode, ApplyPalette)
- [[ArenaRound]] (IsRunning, IsOver, Winner, Launch, Reset)
- [[ArenaCastEntry]] (data de tarjetas)
- [[ArenaCastPlanner]] (subyacente en sandbox)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
