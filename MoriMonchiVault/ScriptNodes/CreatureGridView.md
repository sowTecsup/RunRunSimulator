---
tags: [script, ui]
---

# CreatureGridView.cs

**Ruta:** `UI/CreatureGridView.cs`

**Responsabilidad:** Herramienta dev de inspector (Odin TableList), NO el grid de cartas del jugador que es [[CreatureGridUITK]]. Grid read-only de todas las criaturas registradas. Impulsado por eventos `GameEvents.OnRegistryChanged/OnRegistryReloaded`. Reconstruye cada cambio. Muestra tabla de rows con: nombre, color swatch, género, 6 stats base (CON/ATK/SPD/DEF/LCK/EVA), columna Equip (items equipados resueltos desde EquipmentDatabaseSO), combates, crianzas, padres, estado, fecha de nacimiento.

**Campos principales**

| Campo | Tipo | Propósito |
|-------|------|----------|
| `equipmentDb` | `EquipmentDatabaseSO` | Ref para resolver items equipados a nombres. |
| `source` | `CreatureRegistrySO` | Último registry recibido vía evento (caché para Refresh manual). |
| `rows` | `List<CreatureRow>` | Filas de la tabla (read-only TableList de Odin). |

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[CreatureRegistrySO]], [[GameEvents]], [[CreatureDNA]], [[CreatureGridUITK]], [[EquipmentDatabaseSO]], [[EquipmentSO]]

**CreatureRow struct (inner class):**

| Campo | Tipo | Propósito |
|-------|------|----------|
| `Name` | string | CustomName o ToStringID(). |
| `Color` | Color | BaseColor (swatch visual). |
| `Gender` | CreatureGender | Género. |
| `CON` | float | BaseConstitution. |
| `ATK` | float | BaseAttack. |
| `SPD` | float | BaseSpeed. |
| `DEF` | float | BaseDefense. |
| `LCK` | float | BaseLuck. |
| `EVA` | float | BaseEvasion. |
| `Equip` | string | Resumen de items equipados (nombres resueltos o IDs). |
| `Fights` | string | "X (Y)" = FightCount (WinCount). |
| `Breeds` | int | BreedCount. |
| `Mother` | string | CustomName del MotherID o "—" / "???". |
| `Father` | string | CustomName del FatherID o "—" / "???". |
| `State` | string | "SOLD" / "DEAD" / "Breeding" / "In Queue" / "Free". |
| `Born` | string | "dd/MM/yyyy HH:mm" o "—". |

**Métodos principales**

| Método | Retorna | Propósito |
|--------|---------|----------|
| `From(dna, registry, equipmentDb)` | `CreatureRow` | Constructor estático; resuelve padre/madre/equipo vía referencias. |
| `Rebuild()` | void | Reconstruye lista desde `source`; ordena por BirthDate descendente. |
| `RefreshGrid(registry)` | void | Event handler; cachea fuente + llamea Rebuild(). |
