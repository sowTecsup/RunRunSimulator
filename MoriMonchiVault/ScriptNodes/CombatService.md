---
tags: [script, combat]
---

# CombatService.cs

**Ruta:** `Systems/Combat/CombatService.cs`

**Responsabilidad:** Simulación local turn-based. Reescrito S28: nuevo parámetro `EquipmentDatabaseSO equipDb`; usa clases internas `Combatant` (stats + procs + efectos activos + stun turns) y `Resolver` (implementa ICombatContext). Flujo de turno: tick de estados periódicos → fire pasivos → roll ofensivos si no aturdido → ataque (evasión → crit → DEF) → apply ofensivos + defensivos on-hit si conecta. Procs vienen de `EquipmentSO.Effects` (CombatProcEffect polimórficos) resueltos contra equipDb. Genera `CombatTurn`s, determina ganador. Dispara `GameEvents.OnCombatCompleted`. Calcula stats efectivos incluyendo bonos de partes + equipo. HP pool = Constitution × `BaseHpCombatMultiplier` (5f).

## Constantes

| Constante | Valor | Propósito |
|-----------|-------|----------|
| `BaseHpCombatMultiplier` | 5.0 | Constitution se multiplica por 5 para pool de HP en combate. |

## Tipos públicos

| Tipo | Propósito |
|------|----------|
| `EffectiveStats` (readonly struct) | 6 campos: Constitution, Attack, Speed, Defense, Luck, Evasion. Display + combat. |

## Métodos públicos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `Simulate(idA, idB, registry, db, config, equipDb)` | `CombatResult` | Simula un combate local, devuelve resultado + turnos. Nuevo parámetro `equipDb` para resolver procs. |
| `GetEffectiveStats(dna, db)` | `EffectiveStats` | Calcula los 6 stats efectivos (base + partes, sin equipo). |

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatManagerSO]], [[CreatureDNA]], [[CombatRecord]], [[CombatTurn]], [[GameEvents]], [[CombatController]], [[CreatureDatabaseSO]], [[EquipmentDatabaseSO]], [[EquipmentSO]], [[CombatProcEffect]], [[ICombatContext]], [[EquipmentStats]], [[Enums]]
