---
tags: [script, world, ui, expedition]
---

# ArenaCastPicker.cs

**Ruta:** `World/Expedition/ArenaCastPicker.cs`

**Responsabilidad:** Panel modal UITK para seleccionar MoriMonchis del save local antes de la expedición (S103). Muestra grid de criaturas disponibles en `sandbox.LocalPool` con color, nombre y atributos (osadía, sociabilidad). Permite togglear selección hasta `maxPick` (default 3), confirma o cancela. Emite callback al cerrar. Referencia integrada a `ArenaSandbox` para acceder al pool local y confirmar selección.

**Métodos públicos:**
- `Open(System.Action closedCallback)` — abre el panel, construye grid, invoca callback al cerrar
- `bool IsOpen { get; }` — estado actual del panel

**Métodos internos:**
- `BuildGrid()` — genera tarjetas de criatura desde `sandbox.LocalPool`, marca ya planeadas
- `bool IsPlanned(CreatureDNA dna)` — revisa si dna ya está en `sandbox.PlannedCast` (match por ref o CustomName)
- `Button BuildCard(CreatureDNA dna)` — crea tarjeta con swatch de color, nombre, diales
- `TogglePick(CreatureDNA dna)` — añade/quita de selección si hay capacidad
- `Refresh()` — actualiza visual de pills y conteo
- `Confirm()` / `Cancel()` — llama `sandbox.SelectLocalCast()` o cierra sin cambios
- `Close()` — oculta panel, limpia callback

**Campos:**
- `sandbox` [Required] — acceso a LocalPool y SelectLocalCast
- `maxPick` [Min(1)] = 3 — máximo de selecciones

**UI Structure (UXML):**
- `picker-root` (picker--hidden clase)
  - `picker-count` (Label) — "N / maxPick"
  - `picker-grid` (Grid) — contenedor de tarjetas
  - `btn-picker-ok`, `btn-picker-cancel` (Button)

**S103:** Integrada con `ArenaPlanPanel` para quinta píldora "Explora", permite al jugador elegir equipo del save local antes de lanzar ronda.

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[ArenaPlanPanel]], [[CreatureDNA]]
