---
tags: [combat, data, dto, procs, elements]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatProcEvent

DTO [Serializable] que registra un evento de proc dentro de un turno de combate. Captura la magnitud, el objetivo afectado y el timing (antes o después del golpe), junto con el estado HP resultante del objetivo y los status marks tras el proc para la replay del visualizador. **S37:** Campo `TargetIndex` captura el índice (0..2) del unit target dentro su equipo en combate 3v3. **S41:** Campos aditivos para eventos elementales (marca, reacción, estado armado/consumido, energía gasto). **S47:** Nuevo campo `PassivePhase` marca si el proc proviene de una pasiva de rol (Protector shield, Empático heal, Agresivo mark) durante el step 14 (post-strike).

## Responsabilidad

Transportar datos de un proc ejecutado durante `CombatService.TakeTurn()` → `CombatRecord.CombatTurn.Procs`. Fuente de verdad única para la visualización replay: el visualizador lee este DTO (nunca recomputa). **S35:** Captura `TargetStatusAfter` (lista de status marks post-proc) para sincronización exacta de efectos activos con la UI. **S37:** Campo `TargetIndex` identifica qué unit del equipo fue afectado. **S41:** Campos elementales (`ElementEvent`, `Element`, `ElementB`, `AllySource`, `State`, `ReactionName`) enriquecen el proc con contexto elemental, coexistiendo con procs clásicos (`Kind`, `ModifierEffectKind`) — backward compatible. **S47:** Campo `PassivePhase` permite al visualizador ejecutar coreografía especial para pasivas (el atacante se mueve a la posición del aliado objetivo, anima el efecto, retorna).

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Kind` | `ModifierEffectKind` | Tipo de efecto clásico: ReturnDamage, Heal, Poison, Burn, Stun, Regen, Static, Pulse, Steel, Mist, Lifesteal, Synergy, Shield. Null/None en eventos elementales puros. |
| `TargetIsA` | `bool` | Si true, el objetivo pertenece al equipo A (vs B). Usado en 1v1 legacy y 3v3. |
| `TargetIndex` | `int` | **S37 NEW** Índice del unit target dentro su equipo (0..2 en 3v3; legacy 1v1 usa 0) |
| `Amount` | `float` | Magnitud: daño, curación, turnos de stun, etc.; en eventos elementales: afinidad/energía total resultante, daño/cura magnitud, escudo resultante |
| `TargetHpAfter` | `float` | HP absoluto del objetivo tras aplicar el proc (para graficar) |
| `BeforeStrike` | `bool` | Si true, el proc ocurrió en fase pre-ataque; false = on-connect (post-golpe) |
| `PassivePhase` | `bool` | **S47 NEW** Si true, el proc fue generado por una pasiva de rol (Protector shield, Empático heal, Agresivo mark) durante el step 14 (TakeTurn). La replay los coreografía separadamente: el atacante viaja a la posición del aliado objetivo antes del golpe, anima los procs, retorna. Falso = strike clásico o procs ordinarios. |
| `TargetStatusAfter` | `List<CombatStatusMark>` | **(S35)** Estado de efectos activos sobre el objetivo DESPUÉS de aplicar este proc. Null en records viejos (backward compat). Null en eventos elementales (no aplica stacks clásicos). |
| **Campos elementales (S41):**
| `ElementEvent` | `ElementEventKind` | **S41 NEW** Tipo de evento elemental (None, MarkApplied, Reaction, StateArmed, etc.). Significativo SOLO si != None. |
| `Element` | `Element` | **S41 NEW** Primer elemento (marca aplicada o reacción). Ignorar si ElementEvent == None. |
| `ElementB` | `Element` | **S41 NEW** Segundo elemento (en reacciones dobles). Ignorar si ElementEvent == None. |
| `AllySource` | `bool` | **S41 NEW** true = marca aliada, false = marca enemiga. Ignorar si ElementEvent == None. |
| `State` | `ElementalState` | **S41 NEW** Estado elemental (Energizado, Vaporizado, etc.) en eventos de estado (StateArmed, StateConsumed, StateRemoved). Ignorar si ElementEvent == None. |
| `ReactionName` | `string` | **S41 NEW** Nombre de reacción (p.ej. "Vaporizado") en eventos de reacción y consumo de estado. Null si no aplica. |

## Métodos

N/A (DTO puro, sin lógica)

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CombatRecord]] — `CombatTurn.Procs` es `List<CombatProcEvent>`
- [[CombatService]] — emite los eventos via `Resolver.Record()` y `Resolver.RecordElement()`
- [[CombatResolver]] — popula ambos tipos de evento, stampa PassivePhase
- [[CombatVisualizerService]] — consume PassivePhase para coreografía especial de pasivas
- [[CombatElements]] — emite `RecordElement()` para cada marca/reacción/estado

## Conexiones

**Entrada:**
- `CombatResolver.Record()` — crea e inserta en `TurnProcs` con fields clásicos (Kind, TargetIndex, TargetStatusAfter, **S47: PassivePhase**)
- `CombatResolver.RecordElement()` — crea e inserta en `TurnProcs` con fields elementales (ElementEvent, Element, ElementB, AllySource, State, ReactionName, **S47: PassivePhase**)

**Salida:**
- `CombatVisualizerService.PlayTurn()` — filtra procs por PassivePhase para coreografiar por separado
  - Procs pre-strike (BeforeStrike=true, cualquier PassivePhase)
  - Procs pasivos (PassivePhase=true, !BeforeStrike) — se animan en el step 14, grupo por (targetSide, targetIndex)
  - Procs post-strike (PassivePhase=false, !BeforeStrike) — se animan después del golpe

## Cambios S47

**Nuevo campo:**
- `PassivePhase` (bool) — marcado por `CombatResolver` cuando se graban procs generados por ApplyPassives() y HealAfterStrike().

**Captura en Record (S47):**
```csharp
public void Record(ModifierEffectKind kind, Combatant target, float amount)
    => TurnProcs?.Add(new CombatProcEvent
    {
        Kind = kind,
        TargetIsA = target.IsA,
        TargetIndex = target.Index,
        Amount = amount,
        TargetHpAfter = target.Hp,
        BeforeStrike = BeforeStrike,
        PassivePhase = PassivePhase,  // S47 new: stamped by TakeTurn
        TargetStatusAfter = StatusMarks(target),
    });
```

**Captura en RecordElement (S47):**
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
        PassivePhase = PassivePhase,  // S47 new: stamped by TakeTurn
        Element = element,
        ElementB = elementB,
        AllySource = allySource,
        State = state,
        ReactionName = reactionName,
    });
```

**Flujo en CombatService (S47):**

En `TakeTurn()`, antes de `ApplyPassives()` y `HealAfterStrike()`:
```csharp
r.PassivePhase = true;
CombatRoleHooks.ApplyPassives(actor, profile, allies, config, result, r, rng);
CombatRoleHooks.HealAfterStrike(actor, profile, allies, strike.Dodged, strike.Damage, config, result, r, rng);
r.PassivePhase = false;
```

Todos los procs emitidos dentro de ese bloque (Shield grants, Heal, marks) llevan `PassivePhase = true`.

## Cambios S41

**Nuevos campos (aditivos, backward compatible):**
- `ElementEvent` (ElementEventKind) — enum de tipo de evento elemental
- `Element`, `ElementB` — primer y segundo elemento de marca/reacción
- `AllySource` (bool) — fuente de la marca (aliada vs enemiga)
- `State` (ElementalState) — estado elemental en eventos de estado
- `ReactionName` (string) — nombre de reacción

**Lógica de lectura en replay:**
- Leer `ElementEvent` PRIMERO. Si == None, el evento es clásico (leer Kind, Amount, etc.)
- Si != None, ignorar Kind y TargetStatusAfter; leer Element, ElementB, AllySource, State, ReactionName según tipo.

**Convención de Amount en eventos elementales:**
- Afinidad/Energía → TOTAL resultante (p.ej. "Affinity 3/2" = 3 afinidad, 1 energía ganada)
- Heal/Damage → magnitud del efecto (p.ej. "Heal 4.5")
- ShieldDoubled → escudo resultante (p.ej. "Shield 8.0")
- Consumo de estado → magnitud de impacto (p.ej. Boiling consume + "daño amplificado a 12.5")

**Emisión en CombatResolver.RecordElement() (nuevo método S41):**
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
        PassivePhase = PassivePhase,
        Element = element,
        ElementB = elementB,
        AllySource = allySource,
        State = state,
        ReactionName = reactionName,
    });
```

**Puntos de emisión en S41:**
- `CombatElements.AddMark()` → `RecordElement(ElementEventKind.MarkApplied, ...)`
- `ReactionEffectBase.Apply()` → `RecordElement(ElementEventKind.StateArmed, ...)` / `StateConsumed` / `StateRemoved` / `Heal` / `Damage` / `ShieldDoubled`
- `CombatStrike.Execute()` → `RecordElement(ElementEventKind.StateConsumed, ...)` para cada estado consumido (Vaporizado, GolpePreciso, Debilidad, Boiling, Charcoal)
- `CombatRoleHooks` / `RolePassiveBase.OnDamageDealt` → `RecordElement(ElementEventKind.EnergySpent, ...)` / `EnergyGained`
- `CombatService.GainAffinity()` → `RecordElement(ElementEventKind.AffinityGained, ...)`

## Estructura de StatusMarks (S35)

`StatusMarks()` retorna una lista de `CombatStatusMark`:

```csharp
public static List<CombatStatusMark> StatusMarks(Combatant c)
{
    var counts = new Dictionary<ModifierEffectKind, int>();
    foreach (var a in c.Active)
        counts[a.Kind] = counts.TryGetValue(a.Kind, out var n) ? n + 1 : 1;

    var marks = new List<CombatStatusMark>();
    foreach (ModifierEffectKind kind in System.Enum.GetValues(typeof(ModifierEffectKind)))
        if (counts.TryGetValue(kind, out var stacks))
            marks.Add(new CombatStatusMark { Kind = kind, Stacks = stacks });

    if (c.StunTurns > 0)
        marks.Add(new CombatStatusMark { Kind = ModifierEffectKind.Stun, Stacks = c.StunTurns });

    return marks;
}
```

Captura snapshot de stacks activos + stun turns del combatiente. Se llama en `Record()` automáticamente para cada proc clásico.

## Notas

- Backward compatible: registros viejos sin campos elementales deserializan con default values (ElementEvent == None)
- Backward compatible: registros viejos sin PassivePhase deserializan con false (comportamiento pre-S47)
- El lector SIEMPRE gatea por `ElementEvent` primero — jamás asume campos elementales si ElementEvent == None
- TargetStatusAfter es null en eventos elementales (los estados elementales van en CombatUnitState.ArmedStates del record)
- Orden dentro de `Procs` es significativo: la replay los anima en secuencia
- El `Amount` de un Stun es la cantidad de turnos (flotante casteable a int en display)
- **S35:** PopUp de status ocurre DESPUÉS de que PushStatusSide() sincroniza la UI; el `TargetStatusAfter` es una snapshot point-in-time del estado post-proc
- **S37:** TargetIndex permite 3v3 replay identificar qué unit específico fue afectado
- **S41:** Los eventos elementales son log-only si el visualizer 1v1 los ignora; el visualizer 3v3 usa estos campos para narrar marcas/reacciones/estados
- **S47:** PassivePhase permite coreografía especial: si PassivePhase=true && !BeforeStrike, agrupa procs por (targetSide, targetIndex) y ejecuta desplazamiento+animación+retorno del atacante. Procs no-pasivos se animan en lugar (sin desplazamiento).
