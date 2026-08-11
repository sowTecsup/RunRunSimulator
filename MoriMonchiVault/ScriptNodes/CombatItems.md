---
tags: [script, combat, equipment, items]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatItems.cs

**Ruta:** `Systems/Combat/CombatItems.cs`

**Responsabilidad:** Mediador estático para usos de equipamiento durante combate. **S40:** Extraída de `CombatService.TakeTurn()` la lógica de ítems equipados. `CollectUses()` recolecta `ItemUseEffect` con contador de usos restantes desde la plantilla de creatura. `UseItems()` itera usos y aplica determinísticamente (sin roll) si la regla se cumple (ej: `UseRule.SelfHpBelow` + HP threshold), decrementa contador, ejecuta `ItemUseEffect.Apply()`.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `CollectUses(dna, equipDb)` | `List<ItemUseState>` | Itera slots equipados en dna.Equipped, resuelve items desde equipDb, extrae List<EquipmentEffectBase>, filtra `ItemUseEffect` y agrupa con contador `.Uses` inicial. Retorna lista vacía si equipDb null o Equipped null. |
| `UseItems(actor, target, r, result)` | `void` | Itera `actor.Uses` (recolectados en BuildCombatant), aplica determinísticamente si `Effect.Rule` se cumple (ej: HPBelow), decrementa `Remaining`, ejecuta `Effect.Apply(r)`. Log cada uso. |

## Estructura: ItemUseState

```csharp
public class ItemUseState
{
    public ItemUseEffect Effect;
    public int Remaining;
}
```

## Flujo de Integración (S40)

**En `CombatService.BuildCombatant()`:**

```csharp
var combatant = new Combatant { ... };
combatant.Uses = CombatItems.CollectUses(dna, equipDb);
```

**En `CombatService.TakeTurn()`:**

```csharp
CombatItems.UseItems(actor, target, r, result);
// Si algún item causó muerte, turno termina sin ataque
if (actor.Hp <= 0f || target.Hp <= 0f)
{
    EmitTurn(result, round, actor, target, true, 0f, false, target.Hp, target.Shield, procs, teamA, teamB);
    return;
}
```

## Determinismo

- **Sin roll:** Los usos se aplican cuando la regla se cumple (ej: `SelfHpBelow` si actor.Hp < threshold).
- **Consumo RNG:** Solo si el `Effect` mismo es un `ItemUseEffect` que dispara un proc con roll; el `Apply()` consume RNG si corresponde (delegado al efecto polimórfico).
- **Orden:** Itera `Uses` en orden de collection (slot order), consistente.

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[CombatService]] — `BuildCombatant()` llama `CollectUses()` una vez; `TakeTurn()` llama `UseItems()` cada turno
- [[EquipmentDatabaseSO]] — proveedor de items equipados
- [[ItemUseEffect]] — efecto polimórfico consumido; define Rule y applies
- [[CreatureDNA]] — `Equipped` dict de slots a IDs
- [[Combatant]] — portador de `Uses` list; mutado in-place en `UseItems()`
- [[CombatResolver]], [[CombatResult]] — context para Apply()
