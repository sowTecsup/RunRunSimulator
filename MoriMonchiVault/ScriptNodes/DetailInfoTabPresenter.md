---
tags: [script, ui, presenter]
---

# DetailInfoTabPresenter.cs

**Ruta:** `UI/DetailInfoTabPresenter.cs`

**Responsabilidad (S54):** Presenter colaborador de MorimonchiDetailInfoUITK (no implementa ITabPresenter, sin navegación) — tab "Información" (stats base+bonificación, gender/state/nacimiento, rol+elemento, partes, contadores combate/cría). Renderiza datos sin interacción (ro, solo display).

**Métodos públicos:**
- `Rebuild(dna)` — recalcula y actualiza todos los labels/visuals con datos de `dna`

**Datos UI:**
- 6 labels stat: CON/ATK/SPD/DEF/LCK/EVA (muestran final + desglose base+bonus via equipo/partes/tier)
- `identityLabel` — Género, Estado (SOLD/DEAD/Breeding/In Queue/Free), Nacimiento (fecha local)
- `roleElementLabel` — Nombre rol (Protector/Agresivo/Empático) + descripción (1 línea) + Elemento
- `partsContainer` — 4 filas (Cuerpo/Brazos/Ojos/Boca) con swatch color set + nombre parte + set + rarity + tier
- `progressionLabel` — Combates (X victorias), Crías (X)

**Construcción:**
- `SetStat()` — label "NAME final (base + bonus)" para cada stat
- `BuildParts()` — itera 4 slots (BodyShape/Arm/Eye/Mouth), agrega fila vía `AddPartRow()`
- `AddPartRow()` — swatch color + texto "Slot: Part · Set · Rarity · TierN"

**Vinculaciones estáticas:**
- `RoleName()` — Protector/Agresivo/Empático (español)
- `RoleDesc()` — 1 línea de flavor por rol
- `ElementName()` — Agua/Fuego/Electricidad/Planta
- `StateOf()` — SOLD → DEAD → Breeding → In Queue → Free
- `Born()` — formatea BirthDate a "dd/MM/yyyy HH:mm" o "—"

**Conexiones:** [[MorimonchiDetailInfoUITK]], [[CombatStats]], [[EquipmentStats]], [[CreatureDatabaseSO]], [[EquipmentDatabaseSO]]
