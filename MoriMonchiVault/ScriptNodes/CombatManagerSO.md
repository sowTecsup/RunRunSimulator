---
tags: [scriptable-object, combat, config]
---

# CombatManagerSO

**Ruta:** `Data/Combat/CombatManagerSO.cs`

**Responsabilidad:** Configuración inmutable de combate: fórmulas, chances, límites, balance. Instancia única referenciada por `CombatService`, `CombatController`, `AsyncCombatService`. `SerializedScriptableObject` sin static; lo expone `CombatController.Config`.

## Campos Públicos

### Combat Settings

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `EvolutionChance` | `float` | 0.30 | Chance de que ganador evolucion (0–1) |
| `DeathChance` | `float` | 0.15 | Chance de que perdedor muera (0–1) |

### Hit Settings

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `CritChance` | `float` | 0.10 | Chance base de crítico (0–1) |
| `CritMultiplier` | `float` | 3f | Multiplicador de daño si crit |

### Derived Stat Coefficients

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `LuckCritPerPoint` | `float` | 0.03 | Incremento de crit chance por punto de Luck |
| `DefenseReductionPerPoint` | `float` | 0.08 | Reducción de daño por punto de Defense (0–1) |
| `EvasionPerPoint` | `float` | 0.10 | Chance de esquivar por punto de Evasion (0–1) |

### Safety

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `MaxRounds` | `int` | 50 | Rounds máximos; si se alcanzan → DRAW |
| `MaxFightCount` | `int` | 5 | Combates máximos por criatura por sesión |

### Status / Balance

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `StunImmunityTurns` | `int` | 1 | Turnos de inmunidad a stun tras despertar. Anti-permastun: cuando StunTurns llega a 0, se asigna este valor para impedir re-stun inmediato |
| `Synergies` | `SynergyTableSO` | null | **(NUEVO S32)** Ref a tabla de recetas de sinergia. Sin tabla asignada = sin sinergias activas |

### Needs

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `EnergyCostToQueue` | `float` | 15f | Energía gastada por MoriMochi al encolarse para combate async |

## Fórmulas Aplicadas

- **HP Combate:** Constitution × 5
- **Daño efectivo:** ATK × (1.0 si hit, 3.0 si crit) × (1 - Defense × 0.08)
- **Crit chance:** CritChance + Luck × LuckCritPerPoint
- **Evasión:** Evasion × EvasionPerPoint

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatService]] — usa todos los fields de fórmulas, pasa `Synergies` a `CombatResolver`
- [[CombatController]] — serializa como componente
- [[AsyncCombatService]] — usa para validación
- [[SynergyTableSO]] — tabla de recetas (S32)
- [[CombatResolver]] — recibe `config.Synergies` en constructor

## Conexiones

**Entrada:**
- Scene → asignado en `CombatController.config` field

**Salida:**
- Pasado a `CombatService.Simulate()` y `SimulateCore()`
- `config.Synergies` → `CombatResolver.Synergies`
- Accedido por `CombatController.Config` getter

## Notas (S32)

- **Backward compatible:** `Synergies` es nuevo con default null (sensato: feature opcional).
- **Deshabilitación:** Si `Synergies == null`, `CombatResolver.CheckSynergies()` retorna temprano sin hacer nada.
- **Deuda (Sesión 33):** `EvolutionChance` y `DeathChance` hoy no se usan (siempre suceden si hay slot elegible). Roadmap: hacer probabilísticas.
- **Odin:** Sections con `[Title()]`, `[InfoBox()]`, `[LabelWidth()]` para UI inspector.
