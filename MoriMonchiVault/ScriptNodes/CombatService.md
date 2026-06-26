---
tags: [script, combat]
---

# CombatService.cs

**Ruta:** `Systems/Combat/CombatService.cs`

**Responsabilidad:** Simulación local turn-based. Genera `CombatTurn`s, determina ganador. Dispara `GameEvents.OnCombatCompleted`. Calcula stats efectivos incluyendo bonos de partes, evasión, defensa, crit/luck. HP pool = Constitution × `BaseHpCombatMultiplier` (5f). Pipeline de ataque: evasión → crit → daño → reducción por defensa.

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
| `Simulate(idA, idB, registry, db, config)` | `CombatResult` | Simula un combate local, devuelve resultado + turnos. |
| `GetEffectiveStats(dna, db)` | `EffectiveStats` | Calcula los 6 stats efectivos (base + partes + tier). |

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatManagerSO]], [[CreatureDNA]], [[CombatRecord]], [[CombatTurn]], [[GameEvents]], [[CombatController]], [[CreatureDatabaseSO]], [[Enums]]
