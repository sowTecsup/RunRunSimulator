---
tags: [script, equipment, combat]
---

# CombatProcEffect.cs

**Ruta:** `Data/Equipment/CombatProcEffect.cs`

**Responsabilidad:** Clase abstracta base para efectos polimórficos de combate (procs) que viven inline en `EquipmentSO.Effects`. Cada proc define un `Trigger` (Offensive/Defensive/Passive), `ProcChance` (0-100%, ocultado si es Passive), `Kind` (enum ModifierEffectKind), y método `Apply(ICombatContext)` que emite acciones sin mutar estado (seam para arquitectura futura). Subclases concretas: `ReturnDamageEffect`, `HealEffect`, `StunEffect`, `PoisonEffect`, `BurnEffect`, `RegenEffect`. Etapa 1: display y triggerado en combate local vía CombatService.Simulate (offensive roll al start de turno, defensive roll on-hit si conecta, passive cada turno). Futuro: online parity (UGS Cloud Code).

## Campos principales

| Campo | Tipo | Propósito |
|-------|------|----------|
| `Trigger` | `TriggerType` | Cuándo dispara: Offensive (inicio turno si conecta), Defensive (cuando es golpeado), Passive (siempre). |
| `ProcChance` | `int` | Probabilidad 0-100% de ocurrir (si Trigger != Passive). |

## Subclases concretas

| Subclase | Campos especiales | `Apply()` | Summary |
|----------|-------------------|----------|---------|
| `ReturnDamageEffect` | `Amount` (flat dmg reflect) | `DamageOpponent(Amount, "thorns")` | `[trigger] reflects X damage` |
| `HealEffect` | `Amount` (flat HP) | `HealSelf(Amount, "heal")` | `[trigger] heals X HP` |
| `StunEffect` | `DurationTurns` (1-10) | `StunOpponent(DurationTurns)` | `[trigger] stuns X turn(s)` |
| `PeriodicProcEffect` (abstracta) | `DurationTurns`, `Magnitude` (per turn) | — | — |
| `PoisonEffect` | hereda Periodic | `ApplyStatusToOpponent(Poison, ...)` | `[trigger] poison X/turn for Y turn(s)` |
| `BurnEffect` | hereda Periodic | `ApplyStatusToOpponent(Burn, ...)` | `[trigger] burn X/turn for Y turn(s)` |
| `RegenEffect` | hereda Periodic | `ApplyStatusToSelf(Regen, ...)` | `[trigger] regen X/turn for Y turn(s)` |

## Métodos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `Kind { get; }` | `ModifierEffectKind` | Tipo de efecto (polimórfico, implementado en cada subclase). |
| `Apply(ICombatContext ctx)` | `void` (abstracto) | Emite acciones en el contexto de combate sin mutar (ctx.DamageOpponent, ctx.HealSelf, etc). |
| `Summary()` | `string` | Resumen legible del efecto (incluye trigger tag vía `TriggerTag`). |
| `TriggerTag` | `string` (propiedad) | Etiqueta localizada del trigger: "on hit", "when hit", "passive". |

**Vinculado a:** [[Index/04 - Combat]], [[Index/06 - Equipment]]

**Conexiones:** [[EquipmentSO]], [[ICombatContext]], [[CombatService]], [[Enums]], [[EquipmentEffectBase]], [[ModifierEffectKind]]
