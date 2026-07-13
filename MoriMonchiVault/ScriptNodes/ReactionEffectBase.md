---
tags: [script, combat, elements, reactions, effects, base-class]
---

# ReactionEffectBase.cs

**Ruta:** `Data/Combat/ReactionEffectBase.cs`

**Responsabilidad:** Clase abstracta base para efectos de reacción elemental serializables en listas polimórficas. **S40:** Abstracción de resolución instantánea de reacciones (marcas de 2 elementos distintos detonando), eliminando hardcoding del antes. Cada `ElementReaction` en `ElementTableSO` lleva lista `Effects` de `ReactionEffectBase` que se aplican en orden cuando la reacción se dispara. Determinista: algunos son estado-únicos (ArmStateEffect), otros instantáneos (Cleanse, OverGrow, Leech, etc). Soporte polimórfico vía `[Serializable]` + Odin Inspector. **S41:** Parámetro `r` (CombatResolver) nuevo en `Apply()` para emitir eventos elementales (`RecordElement()`) al grabar del replay.

## Métodos Abstractos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Apply(bearer, reactor, reactionName, result, r, rng)` | `void` | **S41 SIG CAMBIÓ** Aplica el efecto al portador de la reacción. `reactor` es la fuente (puede ser null si reacción aliada). `reactionName` es display name para logs (ej: "Cleanse"). `r` (CombatResolver) es nuevo S41 para grabar evento elemental. |
| `Summary()` | `string` | Retorna descripción UI del efecto (ej: "Cura +5 HP") |

## Firma de Apply (S41)

```csharp
public abstract void Apply(
    Combatant bearer,           // unit portador de la reacción
    Combatant reactor,          // unit reactor (fuente, puede ser null)
    string reactionName,        // nombre display de la reacción
    CombatResult result,        // para logging
    CombatResolver r,           // S41 NEW: para emitir RecordElement()
    CombatRng rng               // RNG inyectado (determinista)
);
```

## Implementaciones Concretas

### ArmStateEffect

**Descripción:** Arma un estado elemental one-use al portador.

**Campos:** `State` (ElementalState enum)

**Apply:** Si portador ya tiene estado, log sin efecto y return; else `bearer.States.Add(State)`, log armado, `r.RecordElement(ElementEventKind.StateArmed, bearer, state: State, reactionName: reactionName)` **(S41 NEW)**.

### CleanseEffect

**Descripción:** Purga el primer estado negativo (Boiling/Debilidad/Confuso/etc) del portador, o cura si no hay negativos.

**Campos:** `HealPercent` (float 0–1, PropertyRange, defecto 0.20)

**Apply:** Busca primer estado negativo via `CombatElements.IsNegative()`, lo remueve, log y `r.RecordElement(ElementEventKind.StateRemoved, ...)` **(S41 NEW)**; else cura `bearer.MaxHp * HealPercent`, log y `r.RecordElement(ElementEventKind.Heal, ...)`.

### DoubleShieldEffect

**Descripción:** Duplica el escudo actual del portador.

**Apply:** `bearer.Shield *= 2f`, log duplicación, `r.RecordElement(ElementEventKind.ShieldDoubled, bearer, amount: bearer.Shield, ...)` **(S41 NEW)**.

### LeechEffect

**Descripción:** Drena HP del portador y lo transfiere a reactor.

**Campos:** `Amount` (float, MinValue 0, LabelText "Drain (flat HP)", defecto 4.0)

**Apply:** `drained = min(bearer.Hp, Amount)`, `bearer.Hp -= Amount`, si reactor no null: `reactor.Hp += drained` (clamped a MaxHp), log drenaje + cura; emite `r.RecordElement(ElementEventKind.Damage, bearer, ...)` + `r.RecordElement(ElementEventKind.Heal, reactor, ...)` **(S41 NEW)** para ambas mutaciones.

### RemoveRandomMarkEffect

**Descripción:** Remueve una marca elemental aleatoria del portador.

**Apply:** Si portador sin marcas, log sin efecto; else pick marca random via `rng.Range()`, remueve, log, `r.RecordElement(ElementEventKind.MarkRemoved, bearer, element: removed.Element, allySource: removed.AllySource, ...)` **(S41 NEW)**.

**Consumo RNG:** `rng.Range(0, marks.Count)`

### HealEffect

**Descripción:** Cura cantidad fija al portador.

**Campos:** `Amount` (float, MinValue 0, LabelText "Heal (flat HP)", defecto 5.0)

**Apply:** `before = bearer.Hp`, `bearer.Hp = min(bearer.MaxHp, bearer.Hp + Amount)`, log cura diferencial, `r.RecordElement(ElementEventKind.Heal, bearer, amount: bearer.Hp - before, ...)` **(S41 NEW)**.

### DamageEffect

**Descripción:** Inflige daño fijo al portador.

**Campos:** `Amount` (float, MinValue 0, LabelText "Damage (flat)", defecto 5.0)

**Apply:** `bearer.Hp = max(0, bearer.Hp - Amount)`, log daño, `r.RecordElement(ElementEventKind.Damage, bearer, amount: Amount, ...)` **(S41 NEW)**.

### GrantEnergyEffect

**Descripción:** Otorga energía al portador.

**Campos:** `Amount` (int, MinValue 1, LabelText "Energy", defecto 1)

**Apply:** `bearer.Energy += Amount`, log energía, `r.RecordElement(ElementEventKind.EnergyGained, bearer, amount: bearer.Energy, ...)` **(S41 NEW)**.

## Flujo de Integración (S40 + S41)

**En `ElementTableSO.ElementReaction`:**
```csharp
public List<ReactionEffectBase> Effects = new List<ReactionEffectBase>();
```

**En `CombatElements.AddMark()` (S41 FIRMA NUEVA):**
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
    r.RecordElement(ElementEventKind.Reaction, target, element: other.Element, elementB: element, allySource: allySource, reactionName: reaction.Name);  // S41 NEW
    
    foreach (var e in reaction.Effects) 
        e.Apply(target, reactor, reaction.Name, result, r, rng);  // parámetro r nuevo S41
}
```

## Determinismo

- **Odin Serialization:** Polimórficas en inspector como lista editable (Odin maneja [SerializableField])
- **RNG:** Solo RemoveRandomMarkEffect consume via `rng.Range()` si hay marcas
- **Determinista:** Sin rolls condicionales (state armado vs instantáneo definido por effect type)
- **S41:** Orden de consumo RNG intacto; cada effect graba su evento pero sin consumir RNG adicional (excepto RemoveRandomMarkEffect)

## Cambios S40

**Antes:**
- `ReactionFor()` switch con 12 casos
- `ApplyState()` switch para cada estado

**Ahora:**
- Cada efecto es polimórfico, serializables en lista SO Odin
- Config centralizada en `ElementTableSO.Reactions`

## Cambios S41

**Nuevo parámetro `r` (CombatResolver):**
- Cada `Apply()` ahora emite `r.RecordElement(ElementEventKind.*)` según el tipo de efecto
- Permite que el replay 3v3 visualice qué reacción pasó, qué estado se armó, qué daño/cura ocurrió
- Backward compatible: parámetro `r` es requerido, pero si es null, nada se graba (fallback no-op en RecordElement)

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[ElementTableSO]] — serializadas en `ElementReaction.Effects` lista polimórfica
- [[CombatElements]] — invocador en `AddMark()` cuando reacción se dispara (parámetro `r` nuevo S41)
- [[CombatResolver]] — receptor de `RecordElement()` (S41 NEW)
- [[Combatant]] — bearer/reactor context, Hp/Shield/Energy/States mutados
- [[CombatResult]], [[CombatRng]] — log y RNG
- [[ElementalState]] (enum) — estados elementales usados por ArmStateEffect
- [[CombatManagerSO]] — configuración de reacciones vía ElementTableSO
