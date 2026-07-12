---
tags: [scriptable-object, combat, config]
---

# CombatManagerSO

**Ruta:** `Data/Combat/CombatManagerSO.cs`

**Responsabilidad:** Configuración inmutable de combate: fórmulas, chances, límites, balance, tablas de sinergias y roles, y parámetros elementales. Instancia única referenciada por `CombatService`, `CombatController`, `AsyncCombatService`. `SerializedScriptableObject` sin static; lo expone `CombatController.Config`. **S37:** Nuevo campo `Roles` (tabla de perfiles 3v3). **S39:** Nuevo bloque "Elemental" con 8 knobs de balance elemental.

## Campos Públicos

### Combat Settings

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `DeathChance` | `float` | 0.05 | Chance de que 1 unit al azar del equipo perdedor muera (0–1) |

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
| `Roles` | `RoleTableSO` | null | **(S37)** Ref a tabla de perfiles de rol 3v3. Sin tabla asignada = roles sin efecto (stats base sin mods). Mapeada por `CreatureDNA.Role` (enum). |

> S39: el campo `Synergies` (SynergyTableSO) fue RETIRADO junto con el motor de sinergias completo.

### Elemental (S39)

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `VaporizadoEvaBonus` | `float` | 0.30 | Bonus de evasión en estado Vaporizado (0–1) |
| `GolpePrecisoCritBonus` | `float` | 0.25 | Bonus de crit en estado Golpe Preciso (0–1) |
| `BoilingDamageBonus` | `float` | 0.30 | Bonus de daño en estado Hirviendo (0–1) |
| `CharcoalReflectPercent` | `float` | 0.50 | Porcentaje de daño reflejado en estado Carbón (0–1) |
| `CleanseHealPercent` | `float` | 0.20 | Porcentaje de HP curado por Limpiar (0–1) |
| `LeechAmount` | `float` | 4f | Cantidad fija de robo de vida por turno |
| `MareadoChance` | `float` | 0.50 | Chance de que Mareado active (0–1) |
| `MareadoDamage` | `float` | 3f | Daño causado por Mareado |

### Needs

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `EnergyCostToQueue` | `float` | 15f | Energía gastada por MoriMochi al encolarse para combate async |

## Fórmulas Aplicadas

- **HP Combate:** Constitution × 5
- **Daño efectivo:** ATK × (1.0 si hit, 3.0 si crit) × (1 - Defense × 0.08)
- **Crit chance:** CritChance + Luck × LuckCritPerPoint
- **Evasión:** Evasion × EvasionPerPoint

## Cambios S39

**Nuevo bloque "Elemental":**
- 8 knobs de balance para efectos elementales
- Valores 0–1 son porcentajes, excepto LeechAmount y MareadoDamage que son magnitudes fijas
- Aplicados por CombatResolver cuando resuelve transiciones de estado elemental

**Eliminado (S39):**
- `EvolutionChance` fue eliminado (no mencionado en cambios de S37/S38, fue deprecated sin reemplazo)

## Consumo en CombatService (S37 + S39)

```csharp
// En BuildCombatant (S37):
var roleProfile = config.Roles?.GetProfile(dna.Role);
if (roleProfile != null)
{
    c.MaxHp = (dnaBaseStats.Constitution + roleProfile.ConMod) * BaseHpCombatMultiplier;
    c.Attack = dnaBaseStats.Attack + roleProfile.AtkMod;
    c.Speed = dnaBaseStats.Speed + roleProfile.SpdMod;
}

// En SimulateCore (muerte, S37):
if (!result.IsDraw && rng.NextFloat() < config.DeathChance)
{
    var loserTeam = result.TeamAWon ? teamB : teamA;
    var loserIdx = rng.Range(0, loserTeam.Count);
    loserTeam[loserIdx].Dna.IsDead = true;
    result.DiedUnitId = loserTeam[loserIdx].Dna.UniqueID;
}

// En CombatResolver (S39):
// Usa config.VaporizadoEvaBonus, .GolpePrecisoCritBonus, etc. al resolver estados
```

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CombatService]] — usa todos los fields de fórmulas + aplica `Roles` en BuildCombatant; los knobs `Elemental` los consume vía CombatElements/TakeTurn
- [[CombatController]] — serializa como componente
- [[AsyncCombatService]] — usa para validación
- [[CombatElements]] — consume los knobs Elemental (S39)
- [[RoleTableSO]] — tabla de perfiles (S37)
- [[CombatResolver]] — grabación de procs (ya no recibe config, S39)

## Conexiones

**Entrada:**
- Scene → asignado en `CombatController.config` field (Odin Inspector)

**Salida:**
- Pasado a `CombatService.Simulate()` y `SimulateCore()`
- `config.Roles` → usado en `BuildCombatant()` (S37)
- `config.[VaporizadoEvaBonus|...]` → usado en `CombatService.TakeTurn` y `CombatElements.ApplyState` (S39)
- Accedido por `CombatController.Config` getter

## Notas (S32 + S37 + S39)

- **Backward compatible:** `Roles` tiene default null (sin tabla = roles sin efecto). Elemental knobs tienen defaults balanceados.
- **S37 DeathChance:** 5% de probabilidad de que 1 unit del equipo perdedor muera (no garantizado).
- **S39 Elemental:** Los 8 knobs tunean el sistema elemental sin cambiar código; los consumen `CombatService.TakeTurn` (consumo de estados) y `CombatElements.ApplyState` (instantáneos).
- **S39 Synergies:** el campo y su tabla fueron retirados junto con el motor completo.
- **Odin:** Sections con `[Title()]`, `[InfoBox()]`, `[LabelWidth()]` para UI inspector.
