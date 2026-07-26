---
tags: [script, ui, presenter]
---

# DetailCombatTabPresenter.cs

**Ruta:** `UI/DetailCombatTabPresenter.cs`

**Responsabilidad (S54):** Presenter colaborador de MorimonchiDetailInfoUITK — tab "Combate" (historial 3v3 con replay, newest first). Implementa ro rebuild() — no navegación. **S68:** PartEs() eliminado — delega en LocEnumMaps.PartRoleName(Enum.TryParse).

## Cambios S68 (Localization-ready)

**Método eliminado:**
- `PartEs()` privado (antes: traducía "Body"→"Cuerpo", etc.) → ahora `PartName()` hace `Enum.TryParse<PartRole>(slot, out var role) ? LocEnumMaps.PartRoleName(role) : slot` (línea 167)

**Líneas de localización agregadas:**
- Línea 60: `Loc.Tr("ui.detail.combat.opponent.local", rec.OpponentName)`
- Línea 61: `Loc.Tr("ui.detail.combat.opponent.online", rec.OpponentName, rec.OpponentPlayerName)`
- Línea 69: `Loc.Tr("ui.detail.combat.you")`
- Línea 70: `Loc.Tr("ui.detail.combat.rival")`
- Línea 75: `Loc.Tr("ui.detail.combat.nostats")`
- Línea 80: `CommentText()` genera frases via Loc.Tr
- Línea 150: `Loc.Tr("ui.detail.combat.tierchip", LocEnumMaps.PartRoleName(part), tier)`
- Línea 159: `Loc.Tr("ui.detail.combat.win.noupgrade")`
- Línea 160: `Loc.Tr("ui.detail.combat.win.upgraded", PartName(rec.EvolvedSlot))`
- Línea 162: `Loc.Tr("ui.detail.combat.died")`
- Línea 162: `Loc.Tr("ui.detail.combat.retreated")`
- Línea 163: `Loc.Tr("ui.detail.combat.draw")`
- Línea 171-173: `Loc.Tr("ui.detail.combat.badge.won")`, `.badge.lost`, `.badge.draw`

**Datos UI:**
- `combatHistory` (ScrollView con cards)
- `combatEmpty` (label placeholder si sin historial)

**Cards por registro (CombatRecord):**
- Header: badge (Victoria/Derrota/Empate con color) + fecha local
- Opponent: "vs [Nombre] · [local | de PlayerName]" (S68: via Loc.Tr)
- Body (si datos disponibles): 2 columnas (Tú / Rival) con swatch + nombre + 6 stats + tier chips (solo si tier > 1)
- Comment: "Victoria — sin mejora" o "Se mejoró [Parte]" o "Regresó derrotado" o "Murió en combate" o "Empate" (S68: via Loc.Tr)
- Footer: botón ▶ Replay (deshabilitado si no verificable via `CombatReplayRequest.CanReplay()`)

**Construcción:**
- `BuildCombatCard()` — crea card completa de un CombatRecord
- `BuildCombatColumn()` — columna con swatch (color snapshot) + nombre + 6 stats + chips tier
- `AddTierChip()` — agrega label si tier > 1, e.g., "Brazo T3" (S68: PartRoleName via LocEnumMaps)
- `SnapshotColor()` — parsea ColorHex de snapshot o fallback color (gris para rival)
- `CommentText()` — genera línea de comentario según outcome + EvolvedSlot + Died (S68: via Loc.Tr)
- `PartName()` — **S68 NUEVO** traduce "Body" (string) → Enum.TryParse a PartRole → LocEnumMaps.PartRoleName (antes era método `PartEs()`)
- `BadgeText()` — traduce outcome a badge string via Loc.Tr (S68)

**Métodos públicos:**
- `Rebuild(dna)` — limpia + itera `dna.CombatHistory` (newest first), pone label "vacío" si empty

**Conexiones:** [[MorimonchiDetailInfoUITK]], [[CombatReplayRequest]], [[CreatureRegistrySO]], [[Loc]], [[LocEnumMaps]]
