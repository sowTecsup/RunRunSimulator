---
tags: [combat, data, dto, procs]
---

# CombatProcEvent

DTO [Serializable] que registra un evento de proc dentro de un turno de combate. Captura la magnitud, el objetivo afectado y el timing (antes o después del golpe), junto con el estado HP resultante del objetivo y los status marks tras el proc para la replay del visualizador. **S37:** Campo `TargetIndex` captura el índice (0..2) del unit target dentro su equipo en combate 3v3.

## Responsabilidad

Transportar datos de un proc ejecutado durante `CombatService.TakeTurn()` → `CombatRecord.CombatTurn.Procs`. Fuente de verdad única para la visualización replay: el visualizador lee este DTO (nunca recomputa). **S35:** Captura `TargetStatusAfter` (lista de status marks post-proc) para sincronización exacta de efectos activos con la UI. **S37:** Campo `TargetIndex` identifica qué unit del equipo fue afectado.

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Kind` | `ModifierEffectKind` | Tipo de efecto: ReturnDamage, Heal, Poison, Burn, Stun, Regen, Static, Pulse, Steel, Mist, Lifesteal, Synergy |
| `TargetIsA` | `bool` | Si true, el objetivo pertenece al equipo A (vs B). Usado en 1v1 legacy y 3v3. |
| `TargetIndex` | `int` | **S37 NEW** Índice del unit target dentro su equipo (0..2 en 3v3; legacy 1v1 usa 0) |
| `Amount` | `float` | Magnitud: daño, curación, turnos de stun, etc. |
| `TargetHpAfter` | `float` | HP absoluto del objetivo tras aplicar el proc (para graficar) |
| `BeforeStrike` | `bool` | Si true, el proc ocurrió en fase pre-ataque; false = on-connect (post-golpe) |
| `TargetStatusAfter` | `List<CombatStatusMark>` | **(S35)** Estado de efectos activos sobre el objetivo DESPUÉS de aplicar este proc. Null en records viejos (backward compat). |

## Métodos

N/A (DTO puro, sin lógica)

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CombatRecord]] — `CombatTurn.Procs` es `List<CombatProcEvent>`
- [[CombatService]] — emite los eventos via `Resolver.Record()`
- [[CombatResolver]] — popula `TargetIndex`, `TargetStatusAfter` via `StatusMarks(target)`
- [[CombatVisualizerService]] — consume para animar procs por turno (1v1)

## Conexiones

**Entrada:**
- `CombatResolver.Record()` — crea e inserta en `TurnProcs` con `TargetIndex = target.Index` + `TargetStatusAfter = StatusMarks(target)`

**Salida:**
- `CombatVisualizerService.BuildStates()` — lee `Turn.Procs` y aplica visualmente (1v1)
- `CombatVisualizerService.PlayProc()` — anima un proc y rasura popup; pushea `TargetStatusAfter` a la barra (S35)

## Cambios S37

**Nuevo campo:**
- `TargetIndex` (int) — índice del unit target en equipo 3v3 (0..2). Default 0 para records 1v1 legacy.

**Captura en Record (S37):**
```csharp
public void Record(ModifierEffectKind kind, Combatant target, float amount)
    => TurnProcs?.Add(new CombatProcEvent
    {
        Kind = kind,
        TargetIsA = target.IsA,
        TargetIndex = target.Index,  // S37 new: índice dentro equipo
        Amount = amount,
        TargetHpAfter = target.Hp,
        BeforeStrike = BeforeStrike,
        TargetStatusAfter = StatusMarks(target),
    });
```

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

Captura snapshot de stacks activos + stun turns del combatiente. Se llama en `Record()` automáticamente para cada proc.

## Notas

- Backward compatible: registros viejos sin `TargetStatusAfter` deserializan como null (deserialization default)
- `TargetIndex` siempre es poblado (legacy 1v1 usa 0 para "único unit")
- Orden dentro de `Procs` es significativo: la replay los anima en secuencia
- El `Amount` de un Stun es la cantidad de turnos (flotante casteable a int en display)
- **S35:** PopUp de status ocurre DESPUÉS de que PushStatusSide() sincroniza la UI; el `TargetStatusAfter` es una snapshot point-in-time del estado post-proc
- **S37:** TargetIndex permite 3v3 replay identificar qué unit específico fue afectado
