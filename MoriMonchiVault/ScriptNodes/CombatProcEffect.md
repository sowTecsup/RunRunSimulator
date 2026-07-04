---
tags: [script, equipment, combat]
---

# CombatProcEffect.cs

**Ruta:** `Data/Equipment/CombatProcEffect.cs`

**Responsabilidad:** Clase abstracta base para efectos polimórficos de combate (procs) que viven inline en `EquipmentSO.Effects`. Cada proc define un `Trigger` (Offensive/Defensive/Passive), `ProcChance` (0-100%, ocultado si es Passive), `Kind` (enum ModifierEffectKind), y método `Apply(ICombatContext)` que emite acciones sin mutar estado (seam para arquitectura futura). Subclases concretas: `ReturnDamageEffect`, `HealEffect`, `StunEffect`, `PoisonEffect`, `BurnEffect`, `RegenEffect`, **`StaticEffect`, `PulseEffect`, `SteelEffect`, `MistEffect` (S35)**. Etapa 1: display y triggerado en combate local vía CombatService.Simulate (offensive roll al start de turno, defensive roll on-hit si conecta, passive cada turno). Futuro: online parity (UGS Cloud Code).

## Enum ProcTarget (S35)

Nivel de archivo, define dónde aplica un efecto:

```csharp
public enum ProcTarget { Opponent, Self }
```

Usado por StaticEffect, PulseEffect, SteelEffect, MistEffect para permitir que un item aplique el efecto a sí mismo o al oponente. Default varía por efecto:
- **StaticEffect:** default `Opponent` (reduce SPD del rival)
- **PulseEffect:** default `Self` (cura al portador)
- **SteelEffect:** default `Self` (suma DEF al portador)
- **MistEffect:** default `Self` (suma EVA al portador)

## Campos principales (base abstracta)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `Trigger` | `TriggerType` | Cuándo dispara: Offensive (inicio turno si conecta), Defensive (cuando es golpeado), Passive (siempre). |
| `ProcChance` | `int` | Probabilidad 0-100% de ocurrir (si Trigger != Passive). |
| `Kind` | `ModifierEffectKind` (propiedad abstract) | Tipo de efecto (polimórfico, implementado en cada subclase). |

## Subclases concretas

| Subclase | Trigger default | Campos especiales | `Apply()` | Summary |
|----------|-----------------|------------------|----------|---------|
| `ReturnDamageEffect` | Offensive | `Amount` (flat dmg) | `DamageOpponent(Amount, "thorns")` | `[trigger] reflects X damage` |
| `HealEffect` | Passive | `Amount` (flat HP) | `HealSelf(Amount, "heal")` | `[trigger] heals X HP` |
| `StunEffect` | Defensive | `DurationTurns` (1-10) | `StunOpponent(DurationTurns)` | `[trigger] stuns X turn(s)` |
| `PeriodicProcEffect` (abstracta) | — | `DurationTurns`, `Magnitude` (per turn) | — | — |
| `PoisonEffect` | Defensive | hereda Periodic | `ApplyStatusToOpponent(Poison, ...)` | `[trigger] poison X/turn for Y turn(s)` |
| `BurnEffect` | Defensive | hereda Periodic | `ApplyStatusToOpponent(Burn, ...)` | `[trigger] burn X/turn for Y turn(s)` |
| `RegenEffect` | Passive | hereda Periodic | `ApplyStatusToSelf(Regen, ...)` | `[trigger] regen X/turn for Y turn(s)` |
| `StaticEffect` | Offensive | `Target` (S35), `DurationTurns`, `Magnitude` (−SPD) | `ApplyStatus{ToOpponent\|ToSelf}(Static, ...)` | `[trigger] static −X SPD for Y turn(s) on {opponent\|self}` |
| `PulseEffect` | Passive | `Target` (S35), `DurationTurns`, `Magnitude` (heal/turn) | `ApplyStatus{ToSelf\|ToOpponent}(Pulse, ...)` | `[trigger] pulse +X HP/turn for Y turn(s) on {self\|opponent}` |
| `SteelEffect` | Passive | `Target` (S35), `DurationTurns`, `Magnitude` (+DEF) | `ApplyStatus{ToSelf\|ToOpponent}(Steel, ...)` | `[trigger] steel +X DEF for Y turn(s) on {self\|opponent}` |
| `MistEffect` | Passive | `Target` (S35), `DurationTurns`, `Magnitude` (+EVA) | `ApplyStatus{ToSelf\|ToOpponent}(Mist, ...)` | `[trigger] mist +X EVA for Y turn(s) on {self\|opponent}` |

## Métodos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `Kind { get; }` | `ModifierEffectKind` | Tipo de efecto (polimórfico, implementado en cada subclase). |
| `Apply(ICombatContext ctx)` | `void` (abstracto) | Emite acciones en el contexto de combate (ctx.DamageOpponent, ctx.HealSelf, ctx.ApplyStatusTo*, etc). |
| `Summary()` | `string` | Resumen legible del efecto (incluye trigger tag, target label para S35 effects). |
| `TriggerTag` | `string` (propiedad) | Etiqueta localizada del trigger: "on hit", "when hit", "passive". |

## Nuevos Effects (S35)

### StaticEffect

Reduce SPD del rival (o porta) vía stacks. Aplicable pre/post-turno según Trigger.

```csharp
[Serializable]
public class StaticEffect : CombatProcEffect
{
    [LabelText("Target")]
    public ProcTarget Target = ProcTarget.Opponent;

    [PropertyRange(1, 10), LabelText("Duration (turns)")]
    public int DurationTurns = 3;

    [MinValue(0), LabelText("-SPD (points)")]
    public int Magnitude = 1;

    public override ModifierEffectKind Kind => ModifierEffectKind.Static;
    public override void Apply(ICombatContext ctx)
    {
        if (Target == ProcTarget.Self) ctx.ApplyStatusToSelf(ModifierEffectKind.Static, DurationTurns, Magnitude, "static");
        else ctx.ApplyStatusToOpponent(ModifierEffectKind.Static, DurationTurns, Magnitude, "static");
    }
    public override string Summary() => $"[{TriggerTag}] static −{Magnitude} SPD for {DurationTurns} turn(s) on {(Target == ProcTarget.Self ? "self" : "opponent")}";
}
```

### PulseEffect

Cura por turno al porta (o rival). Estado emergente de sinergia "Regeneración" cuando PUL×3+STE×1 se detona.

```csharp
[Serializable]
public class PulseEffect : CombatProcEffect
{
    [LabelText("Target")]
    public ProcTarget Target = ProcTarget.Self;

    [PropertyRange(1, 10), LabelText("Duration (turns)")]
    public int DurationTurns = 3;

    [MinValue(0), LabelText("Heal per turn (flat)")]
    public int Magnitude = 2;

    public override ModifierEffectKind Kind => ModifierEffectKind.Pulse;
    public override void Apply(ICombatContext ctx)
    {
        if (Target == ProcTarget.Self) ctx.ApplyStatusToSelf(ModifierEffectKind.Pulse, DurationTurns, Magnitude, "pulse");
        else ctx.ApplyStatusToOpponent(ModifierEffectKind.Pulse, DurationTurns, Magnitude, "pulse");
    }
    public override string Summary() => $"[{TriggerTag}] pulse +{Magnitude} HP/turn for {DurationTurns} turn(s) on {(Target == ProcTarget.Self ? "self" : "opponent")}";
}
```

### SteelEffect

Suma DEF al porta (o rival) vía stacks. Estado emergente de sinergia "Regeneración".

```csharp
[Serializable]
public class SteelEffect : CombatProcEffect
{
    [LabelText("Target")]
    public ProcTarget Target = ProcTarget.Self;

    [PropertyRange(1, 10), LabelText("Duration (turns)")]
    public int DurationTurns = 3;

    [MinValue(0), LabelText("+DEF (points)")]
    public int Magnitude = 1;

    public override ModifierEffectKind Kind => ModifierEffectKind.Steel;
    public override void Apply(ICombatContext ctx)
    {
        if (Target == ProcTarget.Self) ctx.ApplyStatusToSelf(ModifierEffectKind.Steel, DurationTurns, Magnitude, "steel");
        else ctx.ApplyStatusToOpponent(ModifierEffectKind.Steel, DurationTurns, Magnitude, "steel");
    }
    public override string Summary() => $"[{TriggerTag}] steel +{Magnitude} DEF for {DurationTurns} turn(s) on {(Target == ProcTarget.Self ? "self" : "opponent")}";
}
```

### MistEffect

Suma EVA al porta (o rival) vía stacks. Estado emergente de sinergia "Cortocircuito".

```csharp
[Serializable]
public class MistEffect : CombatProcEffect
{
    [LabelText("Target")]
    public ProcTarget Target = ProcTarget.Self;

    [PropertyRange(1, 10), LabelText("Duration (turns)")]
    public int DurationTurns = 3;

    [MinValue(0), LabelText("+EVA (points)")]
    public int Magnitude = 1;

    public override ModifierEffectKind Kind => ModifierEffectKind.Mist;
    public override void Apply(ICombatContext ctx)
    {
        if (Target == ProcTarget.Self) ctx.ApplyStatusToSelf(ModifierEffectKind.Mist, DurationTurns, Magnitude, "mist");
        else ctx.ApplyStatusToOpponent(ModifierEffectKind.Mist, DurationTurns, Magnitude, "mist");
    }
    public override string Summary() => $"[{TriggerTag}] mist +{Magnitude} EVA for {DurationTurns} turn(s) on {(Target == ProcTarget.Self ? "self" : "opponent")}";
}
```

## Dinámica en Combate (S35)

Los 4 nuevos effects (Static, Pulse, Steel, Mist) se aplican como stacks en `CombatResolver.AddStatus()`. Su `Magnitude` se suma en propiedades dinámicas de `Combatant`:

- **Static:** Magnitudes sumados en `EffSpeed` y restados de Speed (clamped a 0)
- **Steel:** Magnitudes sumados en `EffDefense` y agregados a Defense
- **Mist:** Magnitudes sumados en `EffEvasion` y agregados a Evasion
- **Pulse:** `Magnitude` es curación por turno, procesada en `TickStatuses()` igual que Regen

No hay rolls nuevos. El determinismo es idéntico.

**Vinculado a:** [[Index/04 - Combat]], [[Index/06 - Equipment]]

**Conexiones:** [[EquipmentSO]], [[ICombatContext]], [[CombatService]], [[Enums]], [[EquipmentEffectBase]], [[ModifierEffectKind]], [[Combatant]]

## Cambios S35

- **Enum ProcTarget:** Nueva enumeración a nivel de archivo
- **StaticEffect, PulseEffect, SteelEffect, MistEffect:** 4 nuevas subclases con campo `Target`
- **Integración:** Cada effect aplica status vía `ApplyStatus{Self|Opponent}`, que auto-suma Magnitudes en propiedades dinámicas
- **Sin cambios API:** Patrón `Apply(ICombatContext)` sin cambios; los 4 effects siguen el mismo contrato
