---
tags: [interface, combat, context]
---

# ICombatContext

**Ruta:** `Data/Combat/ICombatContext.cs`

**Responsabilidad:** Interfaz de seam para que `CombatProcEffect` emita acciones en combate sin acoplar estado directo. **S32:** Implementada por la clase pública `CombatResolver` (antes era interna `Resolver` de `CombatService`). Permitiendo que procs (ReturnDamageEffect, HealEffect, etc.) operen en contexto de turno local.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `DamageOpponent(float amount, string source)` | Reduce HP oponente, graba ReturnDamage proc |
| `HealSelf(float amount, string source)` | Recupera HP atacante (clamped a MaxHP), graba Heal proc |
| `ApplyStatusToOpponent(ModifierEffectKind kind, int turns, int magnitude, string source)` | Aplica estado periódico al oponente (Poison/Burn), graba proc |
| `ApplyStatusToSelf(ModifierEffectKind kind, int turns, int magnitude, string source)` | Aplica estado periódico al atacante (Regen), graba proc |
| `StunOpponent(int turns)` | Congela oponente por N turnos. **Anti-permastun:** rechaza si ya stunned o en inmunidad |

## Implementación: CombatResolver (S32)

Clase pública que implementa `ICombatContext`:

```csharp
public class CombatResolver : ICombatContext
{
    public CombatResult Result;
    public Combatant    Self;
    public Combatant    Opponent;
    public List<CombatProcEvent> TurnProcs;
    public bool                  BeforeStrike;
    
    // Métodos de ICombatContext:
    public void DamageOpponent(float amount, string source) { ... }
    public void HealSelf(float amount, string source) { ... }
    // etc.
}
```

**Cambio S32:** Extraído de `CombatService` a clase pública reutilizable.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatResolver]] — implementador público (S32)
- [[CombatService]] — instancia resolver, lo usa
- [[CombatProcEffect]] — recibe `this` (ICombatContext) en `Apply()`
- [[ModifierEffectKind]] — enum de tipos de efecto

## Conexiones

**Entrada:**
- `CombatProcEffect.Apply(ICombatContext)` → llama métodos de interfaz

**Salida:**
- Mutaciones en `Combatant.Hp`, `StunTurns`, `Active` (lista de status)
- `CombatProcEvent` enumerados en `TurnProcs`

## Notas

- **Contrato:** No cambia entre local y async; ambos usan `CombatResolver`.
- **Anti-permastun:** Implementado en `StunOpponent()` via `CombatResolver`.
- **Stacking:** `ApplyStatusToOpponent/Self` permite múltiples instancias del mismo tipo.
