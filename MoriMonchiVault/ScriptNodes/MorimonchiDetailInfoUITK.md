---
tags: [script, ui, creature-detail]
---

# MorimonchiDetailInfoUITK

**Ruta:** `UI/MorimonchiDetailInfoUITK.cs`

**Responsabilidad:** Panel modal detalle de criatura (5 tabs: Info/Combate/Linaje/Descendencia/Equipo). **Tab Info:** stats base con bonus de partes (6: CON/ATK/SPD/DEF/LCK/EVA), identidad, personalidad, partes, progresión. **Combate/Linaje/Descendencia:** historial + árbol genealógico. **Tab Equipo (S26-28):** dos columnas — izquierda ScrollView con card por slot (itera enum `EquipmentSlot`), cada card muestra ícono/nombre colorido/rareza/efectos; borde-izquierdo por slot color (via `EquipmentPaletteSO`), diagonal interior por rareza color (Painter2D). Derecha: swatch MM + stats Base→Final (6 stats) con delta de equipo via `EquipmentStats.Apply()`. `IUINavigable` (A/D cambian tabs).

## Tabs

| Tab | Contenido |
|-----|----------|
| **Info** | Stats (base+equipment), identidad, personalidad, partes, progresión |
| **Combate** | Historial de combates (uno por foldout, más reciente primero) |
| **Linaje** | Árbol genealógico (padres/abuelos) |
| **Descendencia** | Árbol de crías |
| **Equipo** | Slots equipables, stats Base→Final con delta |

## Cambios S32

**Stats refs:** Cambio de `CombatService.GetEffectiveStats()` → `CombatStats.GetEffectiveStats()` y `CombatService.EffectiveStats` → `EffectiveStats` top-level. Usado en cálculo de stats finales (Info + Equipo tabs).

## Organización (partial class)

| Archivo | Responsabilidad |
|---------|-----------------|
| `MorimonchiDetailInfoUITK.cs` | Núcleo, Info, Combat, SetStat |
| `MorimonchiDetailInfoUITK.Trees.cs` | Tabs Linaje/Descendencia |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `database` | `CreatureDatabaseSO` | Partes |
| `equipmentDatabase` | `EquipmentDatabaseSO` | Items |
| `equipmentPalette` | `EquipmentPaletteSO` | Colores rareza/slot |
| `sortingOrder` | `int` | Orden de rendering |

## Vinculado a

- [[Index/05 - UI System]]
- [[CreatureGridUITK]] — abre este panel
- [[CreatureDNA]] — fuente de datos
- [[CombatStats]] — calcula stats base (S32)
- [[EffectiveStats]] — struct stats (S32)
- [[EquipmentStats]] — aplica mods
- [[EquipmentDatabaseSO]], [[EquipmentPaletteSO]] — UI data

## Conexiones

**Entrada:**
- `UIManager.OnCreatureSelected` evento → `Wire()` + `Populate()`

**Salida:**
- UI visual (5 tabs, cards, árboles, stats)

## Notas

- **Stats display:** Base (partes) + Final (con equipment mods) via `EquipmentStats.Apply()` (S32).
- **Equipo cards:** Diagonal Painter2D para rareza, borde para slot.
