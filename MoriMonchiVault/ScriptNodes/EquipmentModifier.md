---
tags: [script, equipment, data]
---

# EquipmentModifier.cs

**Ruta:** `Data/Equipment/EquipmentModifier.cs`

**Responsabilidad:** Define dos structs inmutables que forman el contrato de modificadores de equipo: `ModifierTierDef` (la configuración concreta de un efecto en un tier específico) y `EquipmentModifierRef` (la referencia liviana que el equipo almacena). `ModifierTierDef` es la unidad de tunning: label display, magnitud (daño retornado / cantidad curada / daño por turno), duración en turnos y estado de condición aplicable. `EquipmentModifierRef` es el serializable cloud-safe — solo guarda la pareja (Kind, Tier) enumerada, resolviendo los números concretos en runtime contra `EquipmentModifierDatabaseSO`. Espejo del patrón `StatModifier`: una unidad mínima + desacoplamiento de la fuente de verdad (la BD).

**Campos**

| Struct | Campo | Tipo | Propósito |
|--------|-------|------|----------|
| `ModifierTierDef` | `Label` | `string` | Display label para la UI (ej: "Retorno III"). |
| `ModifierTierDef` | `Magnitude` | `float` | Número base: daño retornado, HP curado, daño/turno de status. |
| `ModifierTierDef` | `DurationTurns` | `int` | Turnos que dura el status (solo relevante si Kind=ApplyStatus). |
| `ModifierTierDef` | `Status` | `StatusEffect` | Qué status infligir (None si Kind≠ApplyStatus). |
| `EquipmentModifierRef` | `Kind` | `ModifierEffectKind` | Qué tipo de efecto (ReturnDamage / Heal / ApplyStatus). |
| `EquipmentModifierRef` | `Tier` | `ModifierTier` | Poder del efecto (I, II, III, IV, V). |

**Vinculado a:** [[Index/04 - Combat]] (sistema de modificadores Etapa 1: data + display)

**Conexiones:** [[EquipmentSO]], [[EquipmentModifierDatabaseSO]], [[Enums]]
