---
tags: [script, combat, elements, reactions]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatElements.cs

**Ruta:** `Systems/Combat/CombatElements.cs`

**Responsabilidad:** Motorizado de marcas elementales + reacciones 3v3. `ElementMark` (Element + AllySource bool). `AddMark()` impide duplicados del mismo (Element, AllySource); dos elementos distintos en la misma fuente → reacción vía `config.Elements.FindReaction()` (`ElementTableSO`). Reacciones instantáneas (Cleanse/OverGrow/Leech/PisoTierra) se resuelven inmediato; armadas (Energizado/Vaporizado/etc) se añaden a `Combatant.States` (single-use). Determinista: rolls vía `CombatRng` inyectado. **S41:** Graba eventos elementales en `CombatResolver.RecordElement()` (MarkApplied, Reaction, StateArmed, StateConsumed, StateRemoved, Heal, Damage, ShieldDoubled).

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `AddMark(target, element, allySource, reactor, config, result, r, rng)` | `void` | **S41 SIG CAMBIÓ** Agrega marca elemental al portador. Si duplicado mismo (Element+fuente), no-op. Si dos elementos distintos en misma fuente, detona reacción vía `config.Elements.FindReaction()`. Emite `RecordElement()` para MarkApplied y reacción. |
| `IsNegative(state)` | `bool` | Retorna true si el estado es negativo (Boiling/Debilidad/Confuso/Leech/Mareado/PisoTierra). Usado por CleanseEffect. |

## Estructura: ElementMark

```csharp
public class ElementMark
{
    public Element Element;
    public bool AllySource;  // true = marca aliada, false = marca enemiga
}
```

## Flujo de AddMark (S40 + S41)

**Entrada (S41 FIRMA NUEVA):**
- `target` — portador de la marca
- `element` — elemento a marcar (from attacker or skill)
- `allySource` — true si es aliada (ally skill), false si enemiga (opponent attack)
- `reactor` — fuente de la reacción (attacker/defender context)
- `config` — `CombatManagerSO` (acceso a `config.Elements`)
- `result` — `CombatResult` para log
- `r` — `CombatResolver` para grabar eventos elementales (S41 NEW)
- `rng` — `CombatRng` inyectado

**Lógica:**

1. **Check duplicado:** Itera `target.Marks`, si encuentra `ElementMark` con mismo Element+AllySource, log "ya tiene" y retorna (no duplicar)

2. **Agregar marca:** Crea nuevo `ElementMark { Element, AllySource }`, lo agrega a `target.Marks`, log "recibe marca"

3. **Grabar evento (S41 NEW):** `r.RecordElement(ElementEventKind.MarkApplied, target, amount: 0f, element: element, allySource: allySource)`

4. **Check reacción:** Busca otra marca en misma fuente con DISTINTO Element
   - Si existe: `other = find mark where AllySource == allySource && Element != element`
   - Si no existe: retorna (esperando segunda marca)

5. **Reacción encontrada:** Resuelve via config
   ```csharp
   var reaction = config.Elements != null 
       ? config.Elements.FindReaction(other.Element, element, allySource) 
       : null;
   ```
   - Si null (sin tabla, sin receta), retorna (marcas quedan sin consumirse)

6. **Aplicar reacción:**
   - Remueve ambas marcas de la lista
   - Log "¡{ReactionName}! ({Element A} × {Element B}, fuente X) sobre {target.Name}"
   - `r.RecordElement(ElementEventKind.Reaction, target, element: other.Element, elementB: element, allySource: allySource, reactionName: reaction.Name)` (S41 NEW)
   - Itera `reaction.Effects` en orden, cada uno llama `Apply(target, reactor, reactionName, result, r, rng)` — **parámetro `r` nuevo (S41)** para que cada effect grabe su evento

**Determinismo:**
- Sin roll en la búsqueda de reacción
- Único punto RNG: efectos instantáneos que consumen RNG (ej: `RemoveRandomMarkEffect` → `rng.Range()`)

## Integración en CombatService (S40 + S41)

**En `CombatStrike.Execute()`, post-golpe si damage > 0 (S41 FIRMA):**

```csharp
if (!dodged && damage > 0f)
    CombatElements.AddMark(target, actor.Element, false, actor, config, result, r, rng);
```

Parámetro `r` nuevo (CombatResolver) para grabar eventos elementales.

**En `CombatRoleHooks`, passive/active effects con Energy gasto (S41 FIRMA):**

```csharp
if (actor.Energy > 0)
{
    actor.Energy--;
    r.RecordElement(ElementEventKind.EnergySpent, actor, amount: actor.Energy);
    CombatElements.AddMark(ally, actor.Element, true, actor, config, result, r, rng);
}
```

AddMark ahora recibe `r` para grabar internamente.

## IsNegative() (S39)

```csharp
public static bool IsNegative(ElementalState s)
{
    return s == ElementalState.Boiling
        || s == ElementalState.Debilidad
        || s == ElementalState.Confuso
        || s == ElementalState.Leech
        || s == ElementalState.Mareado
        || s == ElementalState.PisoTierra;
}
```

**Usado por:** `CleanseEffect.Apply()` para purgar el primer negativo o curar si no hay negativos.

## Cambios S40

**Antes:**
- `ReactionFor(element1, element2)` switch grande con 12 casos (6 aliadas + 6 ofensivas)
- `ApplyState(state, target, ...)` switch para cada estado
- Knobs elementales (8 floats) en `CombatManagerSO` directamente

**Ahora:**
- `config.Elements.FindReaction()` busca en `ElementTableSO.Reactions` list
- `reaction.Effects` es `List<ReactionEffectBase>` polimórfica, iterada y aplicada
- Knobs en `ElementTableSO.States` (StateDefinition con Percent/Amount)

**Beneficios:**
- **Editable:** Nuevas reacciones sin código, solo Odin Inspector
- **Extensible:** Nuevos ReactionEffectBase subclasses sin tocar CombatElements
- **Legible:** Determinismo intacto, lógica clara

## Cambios S41

**Nuevo parámetro `r` (CombatResolver):**
- `AddMark()` ahora recibe `CombatResolver r` para emitir `RecordElement()`
- Graba `ElementEventKind.MarkApplied` cuando una marca se añade
- Graba `ElementEventKind.Reaction` cuando una reacción se detona
- Pasa `r` a cada `ReactionEffectBase.Apply(bearer, reactor, reactionName, result, r, rng)` — **parámetro nuevo (S41)** — para que cada effect grabe su tipo de evento (StateArmed, Heal, Damage, etc.)

**Flujo de grabación (S41):**
```
AddMark(target, element, allySource, ..., r, rng)
├─ RecordElement(MarkApplied, target, element, allySource)  [si es marca nueva]
├─ if two distinct elements same source:
   │  FindReaction(element1, element2, allySource)
   │  RecordElement(Reaction, target, element, elementB, allySource, reactionName)  [antes de aplicar effects]
   │  foreach effect in reaction.Effects:
   │     effect.Apply(target, reactor, reactionName, result, r, rng)
   │        └─ RecordElement(StateArmed / Heal / Damage / ..., ...)  [effect-specific]
```

**Backward compatible:** Records viejos (S40) no tienen eventos elementales en CombatTurn.Procs; elemento-only logging (no grabación). S41 emite por primera vez.

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[CombatStrike]] — llama `AddMark()` post-golpe si damage > 0 (parámetro `r` nuevo S41)
- [[CombatRoleHooks]] — llama `AddMark()` si role passive/active gasta Energy (parámetro `r` nuevo S41)
- [[ElementTableSO]] — proveedor de reacciones (v2, S40)
- [[ReactionEffectBase]] — efectos polimórficos en reacciones (parámetro `r` nuevo S41)
- [[CombatResolver]] — receptor de `RecordElement()` calls (S41 NEW)
- [[Combatant]] — portador de `Marks` y `States`
- [[CombatManagerSO]] — acceso a `config.Elements`
- [[CombatResult]], [[CombatRng]] — log y RNG
- [[ElementalState]] (enum) — estados elementales
- [[Element]] (enum) — elementos
