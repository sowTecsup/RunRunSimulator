---
tags: [script, combat, elements, reactions, effects, base-class]
---

# ReactionEffectBase.cs

**Ruta:** `Data/Combat/ReactionEffectBase.cs`

**Responsabilidad:** Clase abstracta base para efectos de reacción elemental serializables en listas polimórficas. **S40:** Abstracción de resolución instantánea de reacciones (marcas de 2 elementos distintos detonando), eliminando hardcoding. Cada `ElementReaction` en `ElementTableSO` lleva lista `Effects` de `ReactionEffectBase` que se aplican en orden cuando la reacción se dispara. **S46:** `GrantEnergyEffect` eliminado (no se usaba en ningún asset). Todos los efectos son ahora orthogonales a Energy.

## Métodos Abstractos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Apply(bearer, reactor, reactionName, result, r, rng)` | `void` | **S41** Aplica el efecto al portador de la reacción. `reactor` es la fuente (puede ser null si reacción aliada). `reactionName` es display name para logs. `r` (CombatResolver) para grabar evento elemental. |
| `Summary()` | `string` | Retorna descripción UI del efecto (ej: "Cura +5 HP") |

## Firma de Apply (S41+)

```csharp
public abstract void Apply(
    Combatant bearer,           // unit portador de la reacción
    Combatant reactor,          // unit reactor (fuente, puede ser null)
    string reactionName,        // nombre display de la reacción
    CombatResult result,        // para logging
    CombatResolver r,           // S41: para emitir RecordElement()
    CombatRng rng               // RNG inyectado (determinista)
);
```

## Implementaciones Concretas

### ArmStateEffect

**Descripción:** Arma un estado elemental one-use al portador.

**Campos:** `State` (ElementalState enum)

**Apply:** Si portador ya tiene estado, log sin efecto y return; else `bearer.States.Add(State)`, log armado, `r.RecordElement(ElementEventKind.StateArmed, bearer, state: State, reactionName: reactionName)`.

### CleanseEffect

**Descripción:** Purga el primer estado negativo del portador, o cura si no hay negativos.

**Campos:** `HealPercent` (float 0–1, PropertyRange, defecto 0.20)

**Apply:** Busca primer estado negativo via `CombatElements.IsNegative()`, lo remueve, log y `r.RecordElement(ElementEventKind.StateRemoved, ...)`; else cura `bearer.MaxHp * HealPercent`, log y `r.RecordElement(ElementEventKind.Heal, ...)`.

### DoubleShieldEffect

**Descripción:** Duplica el escudo actual del portador.

**Apply:** `bearer.Shield *= 2f`, log duplicación, `r.RecordElement(ElementEventKind.ShieldDoubled, bearer, amount: bearer.Shield, ...)`.

### LeechEffect

**Descripción:** Drena HP del portador y lo transfiere a reactor.

**Campos:** `Amount` (float, MinValue 0, LabelText "Drain (flat HP)", defecto 4.0)

**Apply:** `drained = min(bearer.Hp, Amount)`, `bearer.Hp -= Amount`, si reactor no null: `reactor.Hp += drained` (clamped a MaxHp), log drenaje + cura; emite `r.RecordElement(ElementEventKind.Damage, bearer, ...)` + `r.RecordElement(ElementEventKind.Heal, reactor, ...)`.

### RemoveRandomMarkEffect

**Descripción:** Remueve una marca elemental aleatoria del portador.

**Apply:** Si portador sin marcas, log sin efecto; else pick marca random via `rng.Range()`, remueve, log, `r.RecordElement(ElementEventKind.MarkRemoved, bearer, ...)`.

**Consumo RNG:** `rng.Range(0, marks.Count)`

### HealEffect

**Descripción:** Cura cantidad fija al portador.

**Campos:** `Amount` (float, MinValue 0, LabelText "Heal (flat HP)", defecto 5.0)

**Apply:** `before = bearer.Hp`, `bearer.Hp = min(bearer.MaxHp, bearer.Hp + Amount)`, log cura diferencial, `r.RecordElement(ElementEventKind.Heal, bearer, amount: bearer.Hp - before, ...)`.

### DamageEffect

**Descripción:** Inflige daño fijo al portador.

**Campos:** `Amount` (float, MinValue 0, LabelText "Damage (flat)", defecto 5.0)

**Apply:** `bearer.Hp = max(0, bearer.Hp - Amount)`, log daño, `r.RecordElement(ElementEventKind.Damage, bearer, amount: Amount, ...)`.

## Flujo de Integración (S40+)

**En `ElementTableSO.ElementReaction`:**
```csharp
public List<ReactionEffectBase> Effects = new List<ReactionEffectBase>();
```

**En `CombatElements.AddMark()`:**
```csharp
var reaction = config.Elements != null 
    ? config.Elements.FindReaction(otherElement, element, allySource) 
    : null;
if (reaction != null)
{
    // Remove both marks
    target.Marks.Remove(other);
    target.Marks.Remove(justAdded);
    
    result.Log.Add($"    [reacción] ¡{reaction.Name}! ...");
    r.RecordElement(ElementEventKind.Reaction, target, element: other.Element, elementB: element, allySource: allySource, reactionName: reaction.Name);
    
    foreach (var e in reaction.Effects) 
        e.Apply(target, reactor, reaction.Name, result, r, rng);
}
```

## Determinismo

- **Odin Serialization:** Polimórficas en inspector como lista editable
- **RNG:** Solo RemoveRandomMarkEffect consume via `rng.Range()` si hay marcas
- **Determinista:** Sin rolls condicionales (state armado vs instantáneo definido por effect type)

## Cambios S46

**GrantEnergyEffect ELIMINADO:**
- Clase removida de ReactionEffectBase.cs
- No se usaba en ningún asset (búsqueda realizada)
- Energy como recurso ya no existe

**Todos los effectos ahora son independientes de Energy:**
- Ningún effect modifica Energy (que ya no existe en Combatant)
- Solo ArmStateEffect, Cleanse, DoubleShield, Leech, RemoveRandomMark, Heal, Damage

## Cambios S41

**Nuevo parámetro `r` (CombatResolver):**
- Cada `Apply()` ahora emite `r.RecordElement(ElementEventKind.*)` según el tipo de efecto
- Permite que el replay 3v3 visualice qué reacción pasó, qué estado se armó, qué daño/cura ocurrió
- Backward compatible: parámetro requerido pero si es null, nada se graba

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[ElementTableSO]] — serializadas en `ElementReaction.Effects` lista polimórfica
- [[CombatElements]] — invocador en `AddMark()` cuando reacción se dispara
- [[CombatResolver]] — receptor de `RecordElement()`
- [[Combatant]] — bearer/reactor context, Hp/Shield/States mutados
- [[CombatResult]], [[CombatRng]]
- [[ElementalState]] (enum)
- [[CombatManagerSO]]
