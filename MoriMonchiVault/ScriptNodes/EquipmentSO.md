---
tags: [script, equipment, asset]
---

# EquipmentSO.cs

**Ruta:** `Data/Equipment/EquipmentSO.cs`

**Responsabilidad:** ScriptableObject de un item equipable. Lleva sprite icon, nombre display, `EquipmentSlot` (Weapon/Armor/Amulet), rareza visual, descripción, y lista polimórfica `[OdinSerialize] List<EquipmentEffectBase> Effects` que mezcla modificadores pasivos (StatModifierEffect, heredan de EquipmentEffectBase) y procs de combate (CombatProcEffect con trigger/procChance/Apply). Su `ID` es asignado por `EquipmentDatabaseSO` al poblarse (ej: "EQ0", "EQ1"…). Las criaturas lo equipan guardando el `ID` en `CreatureDNA.Equipped[Slot]`.

## Campos principales

| Campo | Tipo | Propósito |
|-------|------|----------|
| `Icon` | `Sprite` | Icono visual (preview en inspector). |
| `ID` | `string` | Identificador único asignado por la base de datos ("EQ0", "EQ1"…). |
| `Name` | `string` | Nombre display (ej: "Espada del Fuego"). |
| `Slot` | `EquipmentSlot` | Dónde se equipa (Weapon / Armor / Amulet). |
| `Rarity` | `Rarity` | Rareza visual (Common/Uncommon/Rare/Epic/Legendary). |
| `IconColor` | `Color` | Color del ícono cuando el ítem no tiene sprite asignado (default gris). |
| `Description` | `string` | Descripción multilínea del ítem, mostrada en la card de equipo en la tab Equipo. |
| `Effects` | `List<EquipmentEffectBase>` | Lista polimórfica de efectos (stat mods StatModifierEffect + procs CombatProcEffect). |
| `EffectsSummary` | `string` (show-only) | Resumen de todos los efectos (`Summary()` de cada uno). |

**Métodos**

| Método | Retorna | Propósito |
|--------|---------|----------|
| `GetRarityColor()` | `Color` | Color de rareza (reusa la lógica de `BodyPart.RarityColor()`). |

**Vinculado a:** [[Index/04 - Combat]] (sistema de modificadores)

**Conexiones:** [[EquipmentDatabaseSO]], [[CreatureDNA]], [[EquipmentEffectBase]], [[StatModifierEffect]], [[CombatProcEffect]], [[Enums]], [[BodyPart]]
