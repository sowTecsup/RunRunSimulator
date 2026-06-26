---
tags: [script, equipment]
---

# StatModifier.cs

**Ruta:** `Data/Equipment/StatModifier.cs`

**Responsabilidad:** Struct atómico que describe una modificación a un stat. Transporta `StatType` (cuál stat), `ModifierType` (cómo se apila: Flat/PercentAdd/PercentMult), y `float Value` (cantidad). Es la unidad mínima de cambio stat que usan los efectos de equipo. El pipeline de combate en `CombatService` agrega una lista de estos por stat: primero Flat, luego PercentAdd sumados, luego cada PercentMult compuesto.

**Campos**

| Campo | Tipo | Propósito |
|-------|------|----------|
| `Stat` | `StatType` | Cuál de los 6 stats se modifica (Constitution/Attack/Speed/Defense/Luck/Evasion). |
| `Type` | `ModifierType` | Cómo se apila (Flat / PercentAdd / PercentMult). |
| `Value` | `float` | Cantidad (+10 ATK, -5 SPD, +10% DEF, etc.). |

**Vinculado a:** [[Index/04 - Combat]] (sistema de modificadores)

**Conexiones:** [[EquipmentEffectBase]], [[StatModifierEffect]], [[CombatService]], [[Enums]]
