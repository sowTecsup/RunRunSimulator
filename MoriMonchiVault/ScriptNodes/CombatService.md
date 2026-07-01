---
tags: [combat, core, stateless, simulation]
---

# CombatService

Servicio estático stateless que simula un combate por turnos local, completamente determinista. Orquesta el flujo: orden de ataque por Speed, tick de status, procs defensivos/ofensivos, cálculos de daño (evasión, crit, defensa), emite record simétrico de turnos.

## Responsabilidad

Ser la única autoridad de simulación local: validar combatientes, ejecutar el loop de rondas, aplicar modificadores (equipment, statuses), grabar cada turno en `CombatRecord` para replay/persistencia. Contrato público sin cambios (backward compatible). Emit todos los procs vía `Resolver.Record()` para captura en `CombatTurn.Procs`.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Simulate(idA, idB, registry, db, config, equipDb)` | `CombatResult` | Simula pelea full, retorna result + turns + log |
| `GetEffectiveStats(dna, db)` | `EffectiveStats` | Calcula stats finales de una criatura (sin equipment aún) |

## Métodos Privados (Statics)

| Método | Descripción |
|--------|-------------|
| `TakeTurn(atk, def, config, result, round, r)` | bool | Resuelve un turno de un combatiente; retorna true si alguien llegó a 0 HP |
| `EmitTurn(result, round, atk, def, noAttack, damage, crit, defHp, procs)` | void | Crea `CombatTurn` con todos los campos y lo agrega a `result.Turns` |
| `TickStatuses(c, result, r)` | void | Aplica daño/curación por status activo (Poison/Burn/Regen), graba procs |
| `FireProcs(owner, opponent, trigger, result, r, roll)` | void | Itera procs del tipo trigger, los aplica via `ICombatContext` |
| `RollProc(p, owner, result)` | bool | Tira chance proc, loguea roll |
| `BuildCombatant(dna, db, equipDb, isA)` | `Combatant` | Construye modelo interno de combatiente con stats/procs |
| `CollectProcs(dna, equipDb)` | `List<CombatProcEffect>` | Recolecta todos los procs del equipment equipado |
| `ComputeStats(dna, db)` | `Stats` | Calcula stats base + acumulación de partes |
| `AccumulatePart(part, tier, ref con, ref atk, ref spd)` | void | Suma bonificación de parte a stats |
| `RecordHistory(self, opponent, outcome, died, evolvedSlot, selfIsA, turns)` | void | Crea `CombatRecord` e inserta en `self.CombatHistory` |
| `TryEvolveRandomSlot(dna)` | string | Elige slot aleatorio no-Tier3 y lo evoluciona; retorna nombre del slot o null |
| `GetSlotTier(dna, slot)` | int | Retorna tier actual del slot |
| `Clip(id)` | string | Trunca ID a 14 chars para logging |

## Clases Internas

### Resolver : ICombatContext

Implementa el contexto de aplicación de procs. Mantiene referencias a combatientes y buffer de `CombatProcEvent`.

**Campos:**
- `CombatResult Result`
- `Combatant Self`, `Opponent`
- `List<CombatProcEvent> TurnProcs` — buffer acumulado durante turno
- `bool BeforeStrike` — true si estamos en fase pre-ataque

**Métodos (ICombatContext):**
- `DamageOpponent(amount, source)` — daña opponent, graba proc ReturnDamage
- `HealSelf(amount, source)` — cura self, graba proc Heal
- `ApplyStatusToOpponent(kind, turns, mag, source)` → `AddStatus()`
- `ApplyStatusToSelf(kind, turns, mag, source)` → `AddStatus()`
- `StunOpponent(turns)` — seteea turns de stun, graba proc Stun
- `Record(kind, target, amount)` — crea `CombatProcEvent` e inserta en `TurnProcs`

### Combatant (interna)

Modelo de combatiente durante simulación.

**Campos:**
- `CreatureDNA Dna`
- `string Name`
- `bool IsA`
- `float Hp, MaxHp`
- `float Attack, Speed, Defense, Luck, Evasion`
- `int StunTurns`
- `List<CombatProcEffect> Procs`
- `List<ActiveEffect> Active` — statuses activos

### ActiveEffect (interna)

Estado de un status en proceso.

**Campos:**
- `ModifierEffectKind Kind`
- `int RemainingTurns`
- `int Magnitude`

## Constantes

| Constante | Valor | Uso |
|-----------|-------|-----|
| `BaseHpCombatMultiplier` | 5f | HP en combate = Constitution * 5 |

## Vinculado a

- [[Enums]] — `ModifierEffectKind`, `CombatOutcome`, `Tier`
- [[CreatureDNA]] — fuente de verdad de stats
- [[CreatureDatabaseSO]] — resuelve partes por ID
- [[CombatManagerSO]] — config (MaxRounds, CritChance, DEF reduction, etc.)
- [[EquipmentDatabaseSO]] — resuelve items equipados
- [[EquipmentStats]] — aplica modifiers estatales
- [[CombatRecord]] — estructura de salida
- [[CombatProcEvent]] — DTO de procs
- [[GameEvents]] — (no dispara directo, GameManager orquesta persistencia)

## Conexiones

**Entrada:**
- `CombatManagerSO` (config)
- `CreatureRegistrySO` (búsqueda DNAs por ID)
- `CreatureDatabaseSO` (partes)
- `EquipmentDatabaseSO` (items equipados → procs)

**Salida:**
- `CombatResult` — contiene `Turns` (lista de `CombatTurn`)
- `CombatRecord` — inserto en `CreatureDNA.CombatHistory` (persistencia)

## Cambios Sesión 31

**MODIFICADO:** `TakeTurn()` y helpers relacionados

1. Nuevo buffer `procs = List<CombatProcEvent>()` por turno
2. `Resolver.TurnProcs = procs` antes de lógica de turno
3. `Resolver.BeforeStrike = true/false` para marcar fase
4. Cada mutacion (damage, heal, stun, status) ahora graba `Resolver.Record(kind, target, amount)`
5. `TickStatuses()` firma cambia a `(c, result, r)` y graba procs via `r.Record()`
6. `EmitTurn()` nueva firma con `noAttack` y `procs` parámetros
7. Turnos sin golpe (`NoAttack=true`): muerte por aflicción, muerte por passive, stun-skip

**Backward compatible:** Contrato público `Simulate()`/`GetEffectiveStats()` sin cambios. El `CombatResult` siempre emite `Turns` bien formados.

## Notas

- **Determinismo:** Usa `UnityEngine.Random` (no seedeable aún; roadmap etapa 2.2)
- **Stats:** Constitution → HP, resto aplicados directamente (DEF/LCK/EVA suman de equipment)
- **Procs:** Ordenados before-strike, golpe (si !NoAttack), after-strike (visible en visualizador)
- **Logging:** `result.Log` contiene trazas debug de todo (rolls, daños, evaciones, statuses)
