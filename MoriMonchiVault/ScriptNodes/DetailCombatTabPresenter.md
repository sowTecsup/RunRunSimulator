---
tags: [script, ui, presenter]
---

# DetailCombatTabPresenter.cs

**Ruta:** `UI/DetailCombatTabPresenter.cs`

**Responsabilidad (S54):** Presenter colaborador de MorimonchiDetailInfoUITK — tab "Combate" (historial 3v3 con replay, newest first). Implementa ro rebuild() — no navegación.

**Datos UI:**
- `combatHistory` (ScrollView con cards)
- `combatEmpty` (label placeholder si sin historial)

**Cards por registro (CombatRecord):**
- Header: badge (Victoria/Derrota/Empate con color) + fecha local
- Opponent: "vs [Nombre] · [local | de PlayerName]"
- Body (si datos disponibles): 2 columnas (Tú / Rival) con swatch + nombre + 6 stats + tier chips (solo si tier > 1)
- Comment: "Victoria — sin mejora" o "Se mejoró [Parte]" o "Regresó derrotado" o "Murió en combate" o "Empate"
- Footer: botón ▶ Replay (deshabilitado si no verificable via `CombatReplayRequest.CanReplay()`)

**Construcción:**
- `BuildCombatCard()` — crea card completa de un CombatRecord
- `BuildCombatColumn()` — columna con swatch (color snapshot) + nombre + 6 stats + chips tier
- `AddTierChip()` — agrega label si tier > 1 (e.g., "Brazo T3")
- `SnapshotColor()` — parsea ColorHex de snapshot o fallback color (gris para rival)
- `CommentText()` — genera línea de comentario según outcome + EvolvedSlot + Died
- `PartEs()` — traduce "Body"→"Cuerpo", etc.
- `BadgeText()` — traduce outcome a "Victoria"/"Derrota"/"Empate"

**Métodos públicos:**
- `Rebuild(dna)` — limpia + itera `dna.CombatHistory` (newest first), pone label "vacío" si empty

**Conexiones:** [[MorimonchiDetailInfoUITK]], [[CombatReplayRequest]], [[CreatureRegistrySO]]
