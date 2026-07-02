---
tags: [script, combat, synergy, recipe]
---

# SynergyRule.cs

**Ruta:** `Data/Combat/SynergyRule.cs`

**Responsabilidad:** Define una receta de sinergia: requisitos de stacks (variedades y cantidades), y lista de efectos a disparar cuando la receta se satisface. Clase de datos pure, serializable, editable en inspector Odin dentro de `SynergyTableSO`.

## Estructura

### SynergyStackRequirement

Uno o más requisitos especifican **qué kind de status** y **cuántos stacks** se necesitan.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Kind` | `ModifierEffectKind` | Tipo de status (Poison, Burn, Regen, etc.) |
| `Stacks` | `int` (MinValue 1) | Cantidad mínima de ese tipo |

**Ejemplo:** `Poison x3` = 3 stacks de Poison activos simultáneamente.

### SynergyRule

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Name` | `string` | Nombre temático (ej. "Explosión Tóxica") |
| `Requirements` | `List<SynergyStackRequirement>` | Receta: qué stacks se necesitan |
| `Effects` | `List<SynergyEffectBase>` | Payloads a aplicar cuando se cumple |

## Lógica de Detección

`CombatResolver.CheckSynergies()` itera sobre todas las reglas en `SynergyTableSO.Rules`:

1. **FirstSatisfiedRule()** — busca la primera regla cuya receta esté satisfecha:
   - Por cada `Requirement` en la regla, cuenta stacks activos del `Kind`
   - Si el contador >= `Stacks` requerido, la regla está satisfecha
2. **ConsumeStacks()** — quema stacks FIFO: por cada requisito, remueve exactamente esa cantidad de instancias `ActiveEffect` del tipo correspondiente
3. **Aplica Effects** — itera `rule.Effects`, llama `e.Apply(resolver, bearer)` por cada uno

## Método Summary()

Genera texto descriptivo para UI/debugging:

```
"Explosión tóxica: 3x Poison → deals 10 damage to the bearer, applies Stun 1 turn"
```

Usado por `SynergyTableSO.RulesSummary` (propiedad computed).

## Vinculado a

- [[Index/03 - Combat]]
- [[SynergyTableSO]] — contenedor y editor
- [[CombatResolver]] — ejecuta detección/consumo/aplicación
- [[SynergyEffectBase]] — los efectos de la receta

## Conexiones

**Entrada:**
- `SynergyTableSO.Rules` — lista de instancias `SynergyRule`

**Salida:**
- Ninguna (puro dato; `CombatResolver` es quien actúa)

## Notas

- **Requisitos múltiples:** Una regla puede pedir `Poison x2 + Burn x1` (AND lógico).
- **Consumo FIFO:** Al quemarse, se toman los primeros stacks de la lista activa (FIFO).
- **Reusabilidad:** La misma regla puede dispararse múltiples veces si se cumple de nuevo tras consumir.
- **NUEVO S32:** Parte de la fase de sinergias del balance de combate.
