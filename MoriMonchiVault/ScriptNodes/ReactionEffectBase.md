---
tags: [script, combat, elements, reactions, effects, base-class]
---

# ReactionEffectBase.cs

**Ruta:** `Data/Combat/ReactionEffectBase.cs`

**Responsabilidad:** Clase abstracta base para efectos de reacción elemental serializables en listas polimórficas. **S40:** Abstracción de resolución instantánea de reacciones (marcas de 2 elementos distintos detonando), eliminando hardcoding. Cada `ElementReaction` en `ElementTableSO` lleva lista `Effects` de `ReactionEffectBase` que se aplican en orden cuando la reacción se dispara. **S46:** `GrantEnergyEffect` eliminado (no se usaba en ningún asset). Todos los efectos son ahora orthogonales a Energy. **S62:** `RemoveRandomMarkEffect` refactorizado — ahora solo remueve marcas ALIADAS (AllySource=true); si no hay candidatas, no-op logueado. `DoubleShieldEffect` refactorizado — dual: si portador tiene escudo lo duplica; si no, otorga `GrantAmount` de escudo nuevo con TTL (`ShieldExpiresAfterRound = r.Round + 1`).

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

### DoubleShieldEffect (S62 ACTUALIZADO)

**Descripción (S62):** Duplica el escudo actual del portador si existe, o otorga escudo nuevo con TTL.

**Campos:** 
- `GrantAmount` (float, MinValue 0, LabelText "Escudo si no hay", defecto 2.0) **(S62 NEW)**

**Apply (S62):**
- Si `bearer.Shield > 0f`:
  - Duplica: `bearer.Shield *= 2f`
  - TTL setter: `bearer.ShieldExpiresAfterRound = r.Round + 1` **(S62)**
  - Log duplicación: `"{reactionName} duplica el escudo de {bearer.Name} → {bearer.Shield}"`
  - Evento: `r.RecordElement(ElementEventKind.ShieldDoubled, bearer, amount: bearer.Shield, reactionName: reactionName)`
- Else (no hay escudo):
  - Otorga: `bearer.Shield = GrantAmount`
  - TTL setter: `bearer.ShieldExpiresAfterRound = r.Round + 1` **(S62)**
  - Log otorgamiento: `"{reactionName} escuda a {bearer.Name} +{GrantAmount} → {bearer.Shield}"`
  - Evento: `r.RecordElement(ElementEventKind.ShieldDoubled, bearer, amount: bearer.Shield, reactionName: reactionName)`

### LeechEffect

**Descripción:** Drena HP del portador y lo transfiere a reactor.

**Campos:** `Amount` (float, MinValue 0, LabelText "Drain (flat HP)", defecto 4.0)

**Apply:** `drained = min(bearer.Hp, Amount)`, `bearer.Hp -= Amount`, si reactor no null: `reactor.Hp += drained` (clamped a MaxHp), log drenaje + cura; emite `r.RecordElement(ElementEventKind.Damage, bearer, ...)` + `r.RecordElement(ElementEventKind.Heal, reactor, ...)`.

### RemoveRandomMarkEffect (S62 ACTUALIZADO)

**Descripción (S62):** Remueve una marca elemental ALIADA aleatoria del portador. Solo remueve marcas con AllySource=true.

**Apply (S62):**
- Recolecta índices de todas las marcas ALIADAS: `for i in bearer.Marks: if (bearer.Marks[i].AllySource) candidates.Add(i)`
- Si `candidates.Count == 0`:
  - Log sin efecto: `"{reactionName} sobre {bearer.Name} — sin marcas aliadas que remover"`
  - Return sin consumir rng
- Else:
  - Pick marca random: `idx = candidates[rng.Range(0, candidates.Count)]`
  - Remueve: `var removed = bearer.Marks[idx]; bearer.Marks.RemoveAt(idx)`
  - Log remoción: `"{reactionName} remueve marca {removed.Element} (aliada) de {bearer.Name}"`
  - Evento: `r.RecordElement(ElementEventKind.MarkRemoved, bearer, element: removed.Element, allySource: removed.AllySource, reactionName: reactionName)`

**Cambio S62:** Ahora solo remueve marcas aliadas (AllySource=true). Antes: removía cualquier marca.

**Consumo RNG:** `rng.Range(0, candidates.Count)` solo si hay marcas aliadas; no-op sin consumir si vacío

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
- **RNG:** Solo RemoveRandomMarkEffect consume via `rng.Range()` si hay marcas aliadas (S62)
- **Determinista:** Sin rolls condicionales (state armado vs instantáneo definido por effect type)
- **S62:** DoubleShieldEffect no consume RNG; RemoveRandomMarkEffect solo consume si hay candidatas

## Cambios S62

**DoubleShieldEffect refactorizado (dual mode):**
- Nuevo campo `GrantAmount` (float, defecto 2.0) para escudo otorgado si no hay escudo actual
- Lógica: si `bearer.Shield > 0f`, duplica; else otorga `GrantAmount`
- Ambas ramas setean TTL: `ShieldExpiresAfterRound = r.Round + 1`

**RemoveRandomMarkEffect refactorizado (solo aliadas):**
- Ahora solo remueve marcas con `AllySource = true`
- Si no hay candidatas: log sin efecto, return sin consumir rng
- Si hay candidatas: pick random, remueve, log, graba evento

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
- [[CombatResolver]] — receptor de `RecordElement()`; consulta `r.Round` para TTL de escudos (S62)
- [[Combatant]] — bearer/reactor context, Hp/Shield/ShieldExpiresAfterRound (S62)/States mutados
- [[CombatResult]], [[CombatRng]]
- [[ElementalState]] (enum)
- [[CombatManagerSO]]
