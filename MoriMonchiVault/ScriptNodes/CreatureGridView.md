---
tags: [script, ui]
---

# CreatureGridView.cs

**Ruta:** `UI/CreatureGridView.cs`

**Responsabilidad:** Herramienta dev de inspector (Odin TableList), NO el grid de cartas del jugador que es [[CreatureGridUITK]]. Grid read-only de todas las criaturas registradas. Impulsado por eventos `GameEvents.OnRegistryChanged/OnRegistryReloaded`. Reconstruye cada cambio. Muestra tabla de rows con: nombre, color swatch, género, crianzas, padres, estado, fecha de nacimiento. **S75:** Removidas columnas `Fights` e "In Queue" state (demolición del combate). **S93:** `State` usa `CreatureDisplay.StateOf()` localizado; `RowTint` compara contra `Loc.Tr()`. **S129:** Removidas 6 columnas de stats (CON/ATK/SPD/DEF/LCK/EVA), equipo resumen y campo `equipmentDb`.

**Campos principales**

| Campo | Tipo | Propósito |
|-------|------|----------|
| `source` | `CreatureRegistrySO` | Último registry recibido vía evento (caché para Refresh manual). |
| `rows` | `List<CreatureRow>` | Filas de la tabla (read-only TableList de Odin). |

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[CreatureRegistrySO]], [[GameEvents]], [[CreatureDNA]], [[CreatureGridUITK]], [[CreatureDisplay]], [[Loc]]

**CreatureRow struct (inner class) — S129:**

| Campo | Tipo | Propósito |
|-------|------|----------|
| `Name` | string | CustomName o ToStringID(). |
| `Color` | Color | BaseColor (swatch visual). |
| `Gender` | CreatureGender | Género. |
| `Breeds` | int | BreedCount. |
| `Mother` | string | CustomName del MotherID o "—" / "???". |
| `Father` | string | CustomName del FatherID o "—" / "???". |
| `State` | string | Localizado: "SOLD" / "DEAD" / "Breeding" / "Free" (via `CreatureDisplay.StateOf()`). |
| `Born` | string | "dd/MM/yyyy HH:mm" o "—". |

**Métodos principales**

| Método | Retorna | Propósito |
|--------|---------|----------|
| `From(dna, registry)` | `CreatureRow` | Constructor estático; resuelve padre/madre vía referencias. |
| `Rebuild()` | void | Reconstruye lista desde `source`; ordena por BirthDate descendente. |
| `RefreshGrid(registry)` | void | Event handler; cachea fuente + llamea Rebuild(). |
