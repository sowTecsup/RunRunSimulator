---
tags: [script, combat, synergy, effects]
---

# SynergyEffectBase.cs

**Ruta:** `Data/Combat/SynergyEffectBase.cs`

**Responsabilidad:** Clase abstracta que define el contrato para efectos aplicados por recetas de sinergia cuando detonan. Cuatro implementaciones concretas: daño, curación, status, stun. Cada efecto exporta un método `Apply(CombatResolver, Combatant)` y un resumen textual para UI.

## Jerarquía de Clases

### SynergyEffectBase (abstracta)

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Apply(CombatResolver r, Combatant bearer)` | `void` | Aplica el efecto sobre el portador (bearer) usando resolver |
| `Summary()` | `string` | Resumen textual corto para UI (ej. "deals 10 damage to the bearer") |

### SynergyDamageEffect

**Campos:**
- `Amount` (float, MinValue 0) — daño a aplicar

**Comportamiento:** Llama `r.DamageBearer(bearer, Amount, "synergy")` → reduce HP, graba `ModifierEffectKind.Synergy` proc.

### SynergyHealEffect

**Campos:**
- `Amount` (float, MinValue 0) — curación a aplicar

**Comportamiento:** Llama `r.HealBearer(bearer, Amount, "synergy")` → incrementa HP (capped MaxHp), graba proc.

### SynergyStatusEffect

**Campos:**
- `Kind` (ModifierEffectKind) — tipo de status (Poison, Burn, Regen, etc.)
- `Turns` (int, MinValue 1) — duración en turnos
- `Magnitude` (int, MinValue 1) — daño/curación por turno

**Comportamiento:** Llama `r.AddStatusTo(bearer, Kind, Turns, Magnitude, "synergy")` → crea `ActiveEffect` sobre el portador.

### SynergyStunEffect

**Campos:**
- `Turns` (int, PropertyRange 1–10) — duración de stun

**Comportamiento:** Llama `r.StunBearer(bearer, Turns)` → aplica anti-permastun guard.

## Vinculado a

- [[Index/03 - Combat]]
- [[SynergyRule]] — lista de efectos a disparar
- [[SynergyTableSO]] — tabla que contiene reglas con efectos
- [[CombatResolver]] — receptor, aplica efectos polimórficamente

## Conexiones

**Entrada:**
- `SynergyRule.Effects` — lista de instancias `SynergyEffectBase`

**Salida:**
- `CombatResolver.{DamageBearer, HealBearer, AddStatusTo, StunBearer}` — mutaciones de combate

## Notas

- **Polimorfismo:** Cada subclase autorables inline en Odin (no SO por efecto).
- **Portador:** El efecto siempre afecta al portador (bearer), no al oponente.
- **Logging:** `CombatResolver` centraliza log; el efecto solo llama métodos.
- **NUEVO S32:** Parte de la fase de sinergias del balance de combate.
