---
tags: [script, world, ui, expedition]
---

# ArenaCastPicker.cs

**Ruta:** `World/Expedition/ArenaCastPicker.cs`

**Responsabilidad:** Panel modal UITK para seleccionar MoriMonchis del save local antes de la expedición (S103). Muestra grid de criaturas disponibles en `sandbox.LocalPool` con color, nombre y atributos (osadía, sociabilidad). Permite togglear selección hasta `maxPick` (default 3), confirma o cancela. Emite callback al cerrar. Referencia integrada a `ArenaSandbox` para acceder al pool local y confirmar selección. **S119:** sin cambios directos; integrado en flujo ArenaPlanPanel.

**Métodos públicos:**
- `void Open(System.Action closedCallback)` — abre el panel, construye grid, invoca callback al cerrar
- `bool IsOpen { get; }` — estado actual del panel

**Métodos internos:**
- `void BuildGrid()` — genera tarjetas de criatura desde `sandbox.LocalPool`, marca ya planeadas
- `bool IsPlanned(CreatureDNA dna)` — revisa si dna ya está en `sandbox.PlannedCast` (match por ref o CustomName)
- `Button BuildCard(CreatureDNA dna)` — crea tarjeta con swatch de color, nombre, diales
- `void TogglePick(CreatureDNA dna)` — añade/quita de selección si hay capacidad
- `void Refresh()` — actualiza visual de pills y conteo
- `void Confirm()` / `void Cancel()` — llama `sandbox.SelectLocalCast()` o cierra sin cambios
- `void Close()` — oculta panel, limpia callback

**Campos Serializados:**
- `sandbox` [Required] — acceso a LocalPool y SelectLocalCast
- `maxPick` [Min(1)] = 3 — máximo de selecciones

**UI Structure (UXML):**
- `picker-root` (picker--hidden clase)
  - `picker-count` (Label) — "N / maxPick"
  - `picker-grid` (Grid) — contenedor de tarjetas
  - `btn-picker-ok`, `btn-picker-cancel` (Button)

**S103:** Integrada con `ArenaPlanPanel` para selección de equipo, permite al jugador elegir creatures del save local antes de lanzar ronda.

**Invariantes:**
- Panel oculto por defecto (picker--hidden)
- Callback invocado solo al cerrar (no null-check protegido)
- IsPlanned() previene duplicados visuales

**Vinculado a:** [[Index/23 - Arena Sandbox y Expedicion]], [[Index/24 - Puente Tienda-Arena]]

**Conexiones:** [[ArenaSandbox]], [[ArenaPlanPanel]], [[CreatureDNA]], [[ArenaCastSource]]

## S119 · Identidad por UniqueID
- `IsPlanned` compara por referencia o por `UniqueID` (ambos no vacíos); ya no por `CustomName`, que no es único.
