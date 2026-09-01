---
tags: [enum, items, equipment]
---

# ItemEnums.cs

**Ruta:** `Core/Enums/ItemEnums.cs`

**Responsabilidad:** Enumeraciones para sistema de items y equipo. Contiene: `StatType` (6 estadísticas: Constitution/Attack/Speed/Defense/Luck/Evasion), `ModifierType` (3 tipos de bonificación: Flat/PercentAdd/PercentMult), `EquipmentSlot` (3 slots: Weapon/Armor/Amulet).

**S93:** Consolidación de enums de items en archivo dedicado.

## Enumeraciones

| Enum | Valores | Descripción |
|------|---------|-------------|
| `StatType` | Constitution (0), Attack (1), Speed (2), Defense (3), Luck (4), Evasion (5) | Estadísticas modificables por equipo |
| `ModifierType` | Flat (0), PercentAdd (1), PercentMult (2) | Cómo aplica un modificador: suma directa, +%, o *% |
| `EquipmentSlot` | Weapon (0), Armor (1), Amulet (2) | Slots de equipo en CreatureDNA.Equipped |

## Uso

- `StatType` — campo en `StatModifier`, refieren qué stat modifica un item
- `ModifierType` — aplicación: `Flat` suma directa (ej. +5), `PercentAdd` suma porcentual (ej. +10%), `PercentMult` multiplicador final (ej. *1.2)
- `EquipmentSlot` — indexa `CreatureDNA.Equipped` Dictionary<EquipmentSlot, string>

## Vinculado a

- [[EquipmentSO]] — define slot
- [[StatModifier]] — contiene StatType + ModifierType
- [[CreatureDNA]] — tiene Equipped Dictionary<EquipmentSlot, string>
- [[EquipmentDatabaseSO]] — almacena EquipmentSO por ID

**Conexiones:** [[EquipmentSO]], [[StatModifier]], [[CreatureDNA]], [[EquipmentDatabaseSO]]

