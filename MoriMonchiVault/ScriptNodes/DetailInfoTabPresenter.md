---
tags: [script, ui, presenter]
---

# DetailInfoTabPresenter.cs

**Ruta:** `UI/DetailInfoTabPresenter.cs`

**Responsabilidad (S54):** Presenter colaborador de MorimonchiDetailInfoUITK (no implementa ITabPresenter, sin navegación) — tab "Información" (stats base+bonificación, gender/state/nacimiento, rol+elemento, partes, contadores combate/cría). Renderiza datos sin interacción (ro, solo display). **S68:** RoleName y ElementName eliminados — delegan en LocEnumMaps; todos los strings vía Loc.Tr/LocEnumMaps.

## Cambios S68 (Localization-ready)

**Métodos eliminados:**
- `RoleName()` privado → ahora `LocEnumMaps.RoleName(dna.Role)` (línea 54)
- `ElementName()` privado → ahora `LocEnumMaps.ElementName(dna.Element)` (línea 54)

**Cambios en `AddPartRow()`:**
- Firma antes: `AddPartRow(string slot, BodyPart part, Tier tier)`
- Firma ahora: `AddPartRow(PartRole slot, BodyPart part, Tier tier)` (línea 83, recibe enum en lugar de string)
- Llamadas en `BuildParts()` (líneas 77-80) pasan `PartRole.Body`, `PartRole.Arm`, etc. en lugar de strings

**Líneas de localización agregadas:**
- Línea 51: `Loc.Tr("ui.detail.identity", LocEnumMaps.GenderName(dna.Gender), StateOf(dna), Born(dna))`
- Línea 54: `Loc.Tr("ui.detail.roleline", LocEnumMaps.RoleName(dna.Role), LocEnumMaps.ElementName(dna.Element), RoleDesc(dna.Role))`
- Línea 59: `Loc.Tr("ui.detail.progression", dna.FightCount, dna.WinCount, dna.BreedCount)`
- Línea 96: `Loc.Tr("ui.detail.partrow", SlotName(slot), part.Name, part.Set, LocEnumMaps.RarityName(part.Rarity), (int)tier)`
- Línea 97: `Loc.Tr("ui.detail.partrow.empty", SlotName(slot))`
- Línea 107-110: `Loc.Tr("ui.detail.slot.body")`, etc. (nombres de slot vía localización)
- Línea 116-118: `Loc.Tr("ui.detail.roledesc.protector")`, etc. (descripciones de rol)
- Línea 123-127: `Loc.Tr("status.sold")`, `Loc.Tr("status.dead")`, etc. (estados)

**Métodos públicos:**
- `Rebuild(dna)` — recalcula y actualiza todos los labels/visuals con datos de `dna`

**Datos UI:**
- 6 labels stat: CON/ATK/SPD/DEF/LCK/EVA (muestran final + desglose base+bonus via equipo/partes/tier)
- `identityLabel` — Género, Estado (SOLD/DEAD/Breeding/In Queue/Free), Nacimiento (fecha local)
- `roleElementLabel` — Nombre rol (via LocEnumMaps) + descripción (1 línea) + Elemento (via LocEnumMaps)
- `partsContainer` — 4 filas (Cuerpo/Brazos/Ojos/Boca) con swatch color set + nombre parte + set + rarity + tier
- `progressionLabel` — Combates (X victorias), Crías (X)

**Construcción:**
- `SetStat()` — label "NAME final (base + bonus)" para cada stat
- `BuildParts()` — itera 4 slots (BodyShape/Arm/Eye/Mouth), agrega fila vía `AddPartRow(PartRole, part, tier)` (S68: ahora recibe enum)
- `AddPartRow()` — swatch color + texto localizado "Slot: Part · Set · Rarity · TierN" via Loc.Tr + LocEnumMaps

**Vinculaciones estáticas (S68 actualizado):**
- `RoleDesc()` — 1 línea de flavor por rol, via `Loc.Tr("ui.detail.roledesc.*")`
- `SlotName()` — nombre slot (Body/Arm/Eye/Mouth), via `Loc.Tr("ui.detail.slot.*")` (líneas 105-112)
- `StateOf()` — estado (SOLD → DEAD → Breeding → In Queue → Free), via `Loc.Tr("status.*")`
- `Born()` — formatea BirthDate a "dd/MM/yyyy HH:mm" o "—"

**Conexiones:** [[MorimonchiDetailInfoUITK]], [[CombatStats]], [[EquipmentStats]], [[CreatureDatabaseSO]], [[EquipmentDatabaseSO]], [[Loc]], [[LocEnumMaps]]
