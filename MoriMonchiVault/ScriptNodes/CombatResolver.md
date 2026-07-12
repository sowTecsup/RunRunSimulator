---
tags: [combat, resolver, context, equipment]
---

# CombatResolver

**Ruta:** `Systems/Combat/CombatResolver.cs`

**Responsabilidad:** Implementa `ICombatContext`, el contrato por el que los efectos de ítem (`ItemUseEffect`, S39) emiten acciones sin mutar el estado del combate directamente. Centraliza salvaguardas anti-permastun (no re-stun si ya stunned, inmunidad post-despertar), stacking independiente de estados (`AddStatus`), y la grabación de `CombatProcEvent` para el replay (`Record`, con `TargetIndex` S37 y `TargetStatusAfter` S35). **S39: el motor de sinergias fue RETIRADO COMPLETO** — `Synergies`/`CheckSynergies`/`FirstSatisfiedRule`/`ConsumeStacks` y los helpers bearer (`StunBearer`/`DamageBearer`/`HealBearer`/`AddStatusTo`, que solo usaban los `SynergyEffectBase` borrados) ya no existen. Las reacciones elementales NO pasan por acá: viven en `CombatElements` (mutación directa + logs propios).

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `DamageOpponent(amount, source)` | Reduce HP del oponente, graba proc `ReturnDamage`. Usado por `DamageUseEffect`. |
| `HealSelf(amount, source)` | Cura al self (cap MaxHp), graba proc `Heal`. Usado por `HealUseEffect`. |
| `ApplyStatusToOpponent(kind, turns, magnitude, source)` | Status al oponente → `AddStatus()`. Sin consumidores activos post-S39 (reservado para ítems con estados, spec §7). |
| `ApplyStatusToSelf(kind, turns, magnitude, source)` | Ídem sobre el self. |
| `StunOpponent(int turns)` | Anti-permastun: rechaza si ya stunned o inmune; si acepta, aplica y graba `Stun`. |
| `Record(ModifierEffectKind, Combatant, float amount)` | Crea `CombatProcEvent` (TargetIsA, `TargetIndex` S37, Amount, TargetHpAfter, BeforeStrike, `TargetStatusAfter` S35) y lo agrega a `TurnProcs`. |
| `Record(ModifierEffectKind, Combatant)` | Sobrecarga sin amount (0f). |
| `StatusMarks(Combatant)` (static) | Snapshot `List<CombatStatusMark>` de stacks activos por Kind + stun. |

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Result` | `CombatResult` | Para logging. |
| `Self` / `Opponent` | `Combatant` | Contexto del efecto en curso (los setea `CombatService` antes de cada `Apply`). |
| `TurnProcs` | `List<CombatProcEvent>` | Buffer del turno actual (fresh cada turno). |
| `BeforeStrike` | `bool` | true antes del golpe del turno, false después. |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `StunTarget(t, turns)` | Guard anti-permastun compartido. |
| `AddStatus(t, kind, turns, magnitude, source)` | Crea `ActiveEffect` (stacking por instancias independientes), loguea y graba. S39: ya NO llama a CheckSynergies. |

## Vinculado a

- [[Index/03 - Combat]] · [[Index/13 - Combat Design Direction]]
- [[ICombatContext]] — interfaz que implementa
- [[CombatService]] — instancia un resolver por simulación (`new CombatResolver { Result = result }`)
- [[ItemUseEffect]] — recibe `this` (ICombatContext) en `Apply()` (S39)
- [[CombatElements]] — la capa elemental hermana (NO pasa por el resolver)
- [[CombatTurn]] — los procs grabados terminan en `Turn.Procs`

## Conexiones

**Entrada:** `CombatService.SimulateCore()` lo crea; cada `ItemUseEffect.Apply(ICombatContext)` llama a sus métodos; `CombatService` llama `Record` directo para eventos del sim (Shield, Heal del Empático, Lifesteal, Stun).

**Salida:** mutaciones a `Self`/`Opponent` (Hp, StunTurns, Active) + `CombatProcEvent` en `TurnProcs` → `CombatTurn.Procs`.

## Cambios por Sesión

- **S32:** anti-permastun compartido (`StunTarget`), stacking por instancias, motor de sinergias (retirado en S39).
- **S35:** `StatusMarks` static + captura automática de `TargetStatusAfter` en cada `Record`.
- **S37:** `TargetIndex` en `Record` para 3v3.
- **S39:** retirado el motor de sinergias completo y los helpers bearer; el contrato pasa de `CombatProcEffect` (borrado) a `ItemUseEffect`. Las reacciones elementales viven en `CombatElements`, no acá.

## Notas

- No es stateless: acumula `TurnProcs` durante el turno.
- Post-S39 nada aplica statuses vía `ApplyStatusTo*` (los ítems v1 solo curan/dañan) — el engine de `Active`/`TickStatuses` queda como sustrato para ítems con estados futuros.
- Los eventos elementales (marcas/reacciones/estados/energía) NO se graban como `CombatProcEvent` todavía — son log-only hasta el paso 0 de Fase 4.
