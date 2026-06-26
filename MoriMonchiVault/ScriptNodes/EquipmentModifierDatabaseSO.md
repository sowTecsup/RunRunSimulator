---
tags: [script, database, asset]
---

# EquipmentModifierDatabaseSO.cs

**Ruta:** `Data/Databases/EquipmentModifierDatabaseSO.cs`

**Responsabilidad:** Base de datos única: fuente de verdad de todos los modificadores de equipo. Espejo de `EquipmentDatabaseSO` / `PartDatabaseSO`. Indexa la jerarquía de modificadores en un doble diccionario: `Kind → (Tier → ModifierTierDef)`. El equipo solo guarda referencias livianas `EquipmentModifierRef(Kind, Tier)` y resuelve aquí para display ahora y para procs en Etapa 2. Ofrece `TryResolve` sobrecargado (por ref o por Kind+Tier), `Summary()` para texto display (traduce Kind y magnitud al español), y getter estático `Editor` para que `EquipmentSO` resuelva IDs en el editor sin `GameManager` vivo (ej: editando el item asset directamente). En runtime, `GameManager` asigna la instancia.

## Campos principales

| Campo | Tipo | Propósito |
|-------|------|----------|
| `catalog` | `Dictionary<ModifierEffectKind, Dictionary<ModifierTier, ModifierTierDef>>` | Doble diccionario: Kind → Tier → config. Donde viven los números de tuning. |

## Métodos públicos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `TryResolve(EquipmentModifierRef r, out ModifierTierDef def)` | `bool` | Resuelve una ref liviana a su `ModifierTierDef`; false si no existe. |
| `TryResolve(ModifierEffectKind kind, ModifierTier tier, out ModifierTierDef def)` | `bool` | Resuelve por componentes; false si no existe. |
| `Summary(EquipmentModifierRef r)` | `string` | Texto display en español (ej: "Cura: 25 HP"). Fallback si no resuelve. |
| `KindLabel(ModifierEffectKind k)` | `string` (static) | Nombre display del Kind (ej: "Regresa daño", "Cura", "Aplica estado"). |
| `KindCount` | `int` | Contador de Kinds poblados (read-only, editor-only show). |

## Editor-only

| Método | Propósito |
|--------|----------|
| `Editor` (static property) | Busca la instancia en AssetDatabase; permite resolver refs en editor sin `GameManager`. |

**Vinculado a:** [[Index/04 - Combat]] (sistema de modificadores Etapa 1: data + display)

**Conexiones:** [[EquipmentModifier]], [[EquipmentSO]], [[CreatureDNA]], [[GameManager]], [[Enums]]
