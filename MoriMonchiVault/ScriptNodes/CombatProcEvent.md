---
tags: [combat, data, dto, procs]
---

# CombatProcEvent

DTO [Serializable] que registra un evento de proc dentro de un turno de combate. Captura la magnitud, el objetivo afectado y el timing (antes o después del golpe), junto con el estado HP resultante del objetivo y los status marks tras el proc para la replay del visualizador.

## Responsabilidad

Transportar datos de un proc ejecutado durante `CombatService.TakeTurn()` → `CombatRecord.CombatTurn.Procs`. Fuente de verdad única para la visualización replay: el visualizador lee este DTO (nunca recomputa). **S35:** Captura `TargetStatusAfter` (lista de status marks post-proc) para sincronización exacta de efectos activos con la UI.

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Kind` | `ModifierEffectKind` | Tipo de efecto: ReturnDamage, Heal, Poison, Burn, Stun, Regen, Static, Pulse, Steel, Mist, Lifesteal, Synergy |
| `TargetIsA` | `bool` | Si true, el objetivo es combatante A; false = combatante B |
| `Amount` | `float` | Magnitud: daño, curación, turnos de stun, etc. |
| `TargetHpAfter` | `float` | HP absoluto del objetivo tras aplicar el proc (para graficar) |
| `BeforeStrike` | `bool` | Si true, el proc ocurrió en fase pre-ataque; false = on-connect (post-golpe) |
| `TargetStatusAfter` | `List<CombatStatusMark>` | **(S35)** Estado de efectos activos sobre el objetivo DESPUÉS de aplicar este proc. Null en records viejos (backward compat). |

## Métodos

N/A (DTO puro, sin lógica)

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatRecord]] — `CombatTurn.Procs` es `List<CombatProcEvent>`
- [[CombatService]] — emite los eventos via `Resolver.Record()`
- [[CombatResolver]] — popula `TargetStatusAfter` via `StatusMarks(target)`
- [[CombatVisualizerService]] — consume para animar procs por turno, sincroniza status

## Conexiones

**Entrada:**
- `CombatResolver.Record()` — crea e inserta en `TurnProcs` con `TargetStatusAfter = StatusMarks(target)`

**Salida:**
- `CombatVisualizerService.BuildStates()` — lee `Turn.Procs` y aplica visualmente
- `CombatVisualizerService.PlayProc()` — anima un proc y rasura popup; pushea `TargetStatusAfter` a la barra (S35)
- `CombatDamageNumbers` — suscriptor de `CombatVisualEvents.OnPopup` (indirecto vía visualizador)

## Estructura de StatusMarks (S35)

`StatusMarks()` es un método estático en `CombatResolver` que retorna una lista de `CombatStatusMark`:

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

Captura snapshot de stacks activos + stun turns del combatiente. Se llama en `Record()` automáticamente para cada proc.

## Captura en Record (S35)

En `CombatResolver.Record()`:

```csharp
public void Record(ModifierEffectKind kind, Combatant target, float amount)
    => TurnProcs?.Add(new CombatProcEvent
    {
        Kind = kind, TargetIsA = target.IsA, Amount = amount,
        TargetHpAfter = target.Hp, BeforeStrike = BeforeStrike,
        TargetStatusAfter = StatusMarks(target),  // S35: siempre captura
    });
```

La captura es automática y transparente, no requiere código especial en cada sitio.

## Notas

- Backward compatible: registros viejos sin `TargetStatusAfter` deserializan como null (deserialization default)
- Orden dentro de `Procs` es significativo: la replay los anima en secuencia
- El `Amount` de un Stun es la cantidad de turnos (flotante casteable a int en display)
- **S35:** PopUp de status ocurre DESPUÉS de que PushStatusSide() sincroniza la UI; el `TargetStatusAfter` es una snapshot point-in-time del estado post-proc
