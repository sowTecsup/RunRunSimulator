---
tags: [script, combat]
---

# CombatManagerSO.cs

**Ruta:** `Data/Combat/CombatManagerSO.cs`

**Responsabilidad:** Configuración global de combate. SerializedScriptableObject sin `static Current`; lo posee y expone CombatController vía su getter `Config`.

## Campos

| Campo | Tipo | Default | Propósito |
|-------|------|---------|----------|
| `EvolutionChance` | float | 0.30 | Probabilidad de evolucionar (0–1). |
| `DeathChance` | float | 0.15 | Probabilidad de que el perdedor muera (0–1). |
| `CritChance` | float | 0.10 | Probabilidad base de crit (0–1). |
| `CritMultiplier` | float | 3.0 | Daño multiplicado en crit. |
| `LuckCritPerPoint` | float | 0.03 | Bonus de crit por punto de Luck. |
| `DefenseReductionPerPoint` | float | 0.08 | Reducción de daño por punto de Defense. |
| `EvasionPerPoint` | float | 0.10 | Chance de esquivar por punto de Evasion. |
| `MaxRounds` | int | 50 | Máximo de rondas antes de draw. |
| `MaxFightCount` | int | 5 | Máximo de peleas por criatura. |
| `EnergyCostToQueue` | float | 15 | Energía gastada al encolar para combate async. |

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatService]], [[AsyncCombatService]], [[Enums]], [[CombatController]]
