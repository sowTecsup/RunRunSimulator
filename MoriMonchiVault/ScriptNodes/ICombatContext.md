---
tags: [script, interface, combat]
---

# ICombatContext.cs

**Ruta:** `Data/Combat/ICombatContext.cs`

**Responsabilidad:** Interfaz de seam para que `CombatProcEffect` emita acciones en combate sin acoplar estado directo. Implementada por la clase interna `Resolver` dentro de `CombatService.Simulate()`, permitiendo que los procs (ReturnDamageEffect, HealEffect, etc.) operen en contexto de turno local. Futuro: reemplazar con stack online/Cloud Code via UGS Cloud Code (mismo contrato), validando acciones server-side.

## Métodos públicos

| Método | Firma | Propósito |
|--------|-------|----------|
| `DamageOpponent` | `void(float amount, string source)` | Reduce HP del oponente (thorns, etc). Loggea en `CombatResult.Log`. |
| `HealSelf` | `void(float amount, string source)` | Recupera HP del atacante. Clampea a MaxHP. Loggea. |
| `ApplyStatusToOpponent` | `void(ModifierEffectKind kind, int turns, int magnitude, string source)` | Aplica estado periódico al oponente (Poison/Burn). Refresca turno/magnitude si existe. |
| `ApplyStatusToSelf` | `void(ModifierEffectKind kind, int turns, int magnitude, string source)` | Aplica estado periódico al atacante (Regen). Refresca turno/magnitude si existe. |
| `StunOpponent` | `void(int turns)` | Congela al oponente por N turnos (salta siguiente ataque). Toma máximo con StunTurns existentes. |

**Vinculado a:** [[Index/04 - Combat]]

**Conexiones:** [[CombatService]], [[CombatProcEffect]], [[Enums]], [[ModifierEffectKind]]
