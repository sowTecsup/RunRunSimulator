---
tags: [combat, resolver, context, equipment, elements]
---

# CombatResolver

**Ruta:** `Systems/Combat/CombatResolver.cs`

**Responsabilidad:** Implementa `ICombatContext`, el contrato por el que los efectos de ítem (`ItemUseEffect`, S39) emiten acciones sin mutar el estado del combate directamente. Centraliza salvaguardas anti-permastun (no re-stun si ya stunned, inmunidad post-despertar), stacking independiente de estados (`AddStatus`), y la grabación de `CombatProcEvent` para el replay (`Record`, con `TargetIndex` S37 y `TargetStatusAfter` S35). **S39: el motor de sinergias fue RETIRADO COMPLETO** — `Synergies`/`CheckSynergies`/`FirstSatisfiedRule`/`ConsumeStacks` y los helpers bearer ya no existen. **S41:** Nuevo método `RecordElement()` para grabar eventos elementales en `CombatProcEvent` (marcas, reacciones, estados, energía) — coexisten con procs clásicos en `Turn.Procs`. **S47:** Nuevo campo público `PassivePhase` (bool) que CombatService seta a true durante ApplyPassives/HealAfterStrike y a false después — marca si el evento fue generado por una pasiva.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `DamageOpponent(amount, source)` | Reduce HP del oponente, graba proc `ReturnDamage`. Usado por `DamageUseEffect`. |
| `HealSelf(amount, source)` | Cura al self (cap MaxHp), graba proc `Heal`. Usado por `HealUseEffect`. |
| `ApplyStatusToOpponent(kind, turns, magnitude, source)` | Status al oponente → `AddStatus()`. Sin consumidores activos post-S39 (reservado para ítems con estados, spec §7). |
| `ApplyStatusToSelf(kind, turns, magnitude, source)` | Ídem sobre el self. |
| `StunOpponent(int turns)` | Anti-permastun: rechaza si ya stunned o inmune; si acepta, aplica y graba `Stun`. |
| `Record(ModifierEffectKind, Combatant, float amount)` | **S35/S37/S39/S47** Crea `CombatProcEvent` clásico (TargetIsA, `TargetIndex` S37, Amount, TargetHpAfter, BeforeStrike, `PassivePhase` S47, `TargetStatusAfter` S35) y lo agrega a `TurnProcs`. |
| `Record(ModifierEffectKind, Combatant)` | Sobrecarga sin amount (0f). |
| `RecordElement(ElementEventKind, Combatant, float, Element, Element, bool, ElementalState, string)` | **S41/S47 NEW** Crea `CombatProcEvent` elemental (ElementEvent, Element, ElementB, AllySource, State, ReactionName, `PassivePhase` S47) y lo agrega a `TurnProcs`. Coexiste con procs clásicos. |
| `StatusMarks(Combatant)` (static) | Snapshot `List<CombatStatusMark>` de stacks activos por Kind + stun. |

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Result` | `CombatResult` | Para logging. |
| `Self` / `Opponent` | `Combatant` | Contexto del efecto en curso (los setea `CombatService` antes de cada `Apply`). |
| `TurnProcs` | `List<CombatProcEvent>` | Buffer del turno actual (fresh cada turno). Mixto: procs clásicos + eventos elementales (S41). |
| `BeforeStrike` | `bool` | true antes del golpe del turno, false después. |
| `PassivePhase` | `bool` | **S47 NEW** true durante ApplyPassives() y HealAfterStrike(), false en otros momentos. Todos los procs emitidos en PassivePhase=true llevan ese flag en CombatProcEvent, permitiendo al visualizador coreografía especial. |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `StunTarget(t, turns)` | Guard anti-permastun compartido. |
| `AddStatus(t, kind, turns, magnitude, source)` | Crea `ActiveEffect` (stacking por instancias independientes), loguea y graba. S39: ya NO llama a CheckSynergies. |

## Métodos Públicos Detallados

### Record (S35/S37/S39/S47)

Graba un proc clásico:

```csharp
public void Record(ModifierEffectKind kind, Combatant target, float amount)
    => TurnProcs?.Add(new CombatProcEvent
    {
        Kind = kind,
        TargetIsA = target.IsA,
        TargetIndex = target.Index,           // S37
        Amount = amount,
        TargetHpAfter = target.Hp,
        BeforeStrike = BeforeStrike,
        PassivePhase = PassivePhase,          // S47
        TargetStatusAfter = StatusMarks(target),  // S35
    });
```

**Emisión en** `CombatService`: 
- Pre-strike (TickStatuses, Stun check, Confuso/Mareado states)
- Post-strike (shield grants, heal, lifesteal)
- **S47:** Si se graba dentro del bloque `r.PassivePhase = true`, lleva PassivePhase=true

### RecordElement (S41/S47 NEW)

Graba un evento elemental:

```csharp
public void RecordElement(ElementEventKind ev, Combatant target, float amount = 0f, 
    Element element = default, Element elementB = default, bool allySource = false, 
    ElementalState state = default, string reactionName = null)
    => TurnProcs?.Add(new CombatProcEvent
    {
        ElementEvent = ev,
        TargetIsA = target.IsA,
        TargetIndex = target.Index,
        Amount = amount,
        TargetHpAfter = target.Hp,
        BeforeStrike = BeforeStrike,
        PassivePhase = PassivePhase,          // S47
        Element = element,
        ElementB = elementB,
        AllySource = allySource,
        State = state,
        ReactionName = reactionName,
    });
```

**Significado de parámetros:**
- `ev` — tipo de evento elemental (MarkApplied, Reaction, StateArmed, etc.)
- `target` — unit afectado (portador de marca, reactor de reacción, etc.)
- `amount` — magnitud (afinidad/energía total, daño/cura, escudo resultante)
- `element`, `elementB` — elementos de marca o reacción
- `allySource` — true si la marca es aliada, false si enemiga
- `state` — estado elemental (Energizado, Vaporizado, etc.)
- `reactionName` — nombre legible de la reacción ("Vaporizado", "Boiling", etc.)

**Emisión en (S41/S47):**
- `CombatElements.AddMark()` → `RecordElement(ElementEventKind.MarkApplied, ...)` (puede ser dentro PassivePhase si la marca es aliada post-Affinity)
- `ReactionEffectBase.Apply()` (8 leaves) → `RecordElement(ElementEventKind.StateArmed, ...)` / `StateConsumed` / `StateRemoved` / `Heal` / `Damage` / `ShieldDoubled`
- `CombatStrike.Execute()` → `RecordElement(ElementEventKind.StateConsumed, ...)` para cada estado consumido (Vaporizado, GolpePreciso, Debilidad, Boiling, Charcoal)
- `CombatRoleHooks` → `RecordElement(ElementEventKind.EnergySpent, ...)` / `EnergyGained` (si aplicara, S46 eliminó Energy)
- `CombatService.GainAffinity()` → `RecordElement(ElementEventKind.AffinityGained, ...)`

**Backward compat:** Eventos elementales coexisten con procs clásicos en `Turn.Procs`; el lector gatea por `ElementEvent` primero para diferenciar tipos. PassivePhase default false en records viejos.

## Vinculado a

- [[Index/03 - Combat]] · [[Index/13 - Combat Design Direction]]
- [[ICombatContext]] — interfaz que implementa
- [[CombatService]] — instancia un resolver por simulación (`new CombatResolver { Result = result, TurnProcs = turnProcs }`) y seta PassivePhase (S47)
- [[ItemUseEffect]] — recibe `this` (ICombatContext) en `Apply()` (S39)
- [[CombatElements]] — emite `RecordElement()` para marcas/reacciones (S41)
- [[ReactionEffectBase]] — emite `RecordElement()` en `Apply()` (S41)
- [[CombatStrike]] — emite `RecordElement()` para consumos de estado (S41)
- [[CombatRoleHooks]] — emite `RecordElement()` para energía (S41) y genera pasivas durante PassivePhase (S47)
- [[CombatRecord]] — los procs grabados terminan en `CombatTurn.Procs`

## Conexiones

**Entrada:** `CombatService.SimulateCore()` lo crea y seta BeforeStrike/PassivePhase; cada `ItemUseEffect.Apply(ICombatContext)` llama a sus métodos; `CombatElements`/`ReactionEffectBase`/`CombatStrike`/`CombatRoleHooks` llaman `RecordElement` directo (S41/S47).

**Salida:** mutaciones a `Self`/`Opponent` (Hp, StunTurns, Active) + `CombatProcEvent` en `TurnProcs` → `CombatTurn.Procs` (mixto clásico + elemental S41, con PassivePhase S47).

## Cambios por Sesión

- **S32:** anti-permastun compartido (`StunTarget`), stacking por instancias, motor de sinergias (retirado en S39).
- **S35:** `StatusMarks` static + captura automática de `TargetStatusAfter` en cada `Record`.
- **S37:** `TargetIndex` en `Record` para 3v3.
- **S39:** retirado el motor de sinergias completo y los helpers bearer; el contrato pasa de `CombatProcEffect` (borrado) a `ItemUseEffect`. Las reacciones elementales viven en `CombatElements`, no acá.
- **S41:** Nuevo método `RecordElement()` para grabar eventos elementales. Coexisten con procs clásicos en `Turn.Procs`.
- **S47:** Nuevo campo `PassivePhase` público. CombatService lo seta a true durante `ApplyPassives()` y `HealAfterStrike()`, permitiendo que todos los procs generados en esa fase sean marcados para coreografía especial en la replay.

## Notas

- No es stateless: acumula `TurnProcs` durante el turno.
- Post-S39 nada aplica statuses vía `ApplyStatusTo*` (los ítems v1 solo curan/dañan) — el engine de `Active`/`TickStatuses` queda como sustrato para ítems con estados futuros.
- **S41:** Eventos clásicos y elementales coexisten sin conflicto — el lector de `CombatTurn.Procs` gatea por `ElementEvent` primero.
- **S47:** PassivePhase es un boolean flag de timing, no afecta la simulación — solo importa para la visualización. CombatService lo controla estrictamente.
- Los `TargetStatusAfter` son null en eventos elementales (los estados elementales van en `CombatUnitState.ArmedStates` del record).
