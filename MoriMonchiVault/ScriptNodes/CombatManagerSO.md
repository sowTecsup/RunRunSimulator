---
tags: [scriptable-object, combat, config]
---

# CombatManagerSO

**Ruta:** `Data/Combat/CombatManagerSO.cs`

**Responsabilidad:** Configuración inmutable de combate: fórmulas, chances, límites, balance, tablas de sinergias y roles. Instancia única referenciada por `CombatService`, `CombatController`, `AsyncCombatService`. `SerializedScriptableObject` sin static; lo expone `CombatController.Config`. **S37:** Nuevo campo `Roles` (tabla de perfiles 3v3).

## Campos Públicos

### Combat Settings

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `EvolutionChance` | `float` | 0.30 | Chance de que ganador evolucione (0–1) |
| `DeathChance` | `float` | 0.05 | **S37 CHANGED** Chance de que 1 unit al azar del equipo perdedor muera (0–1, ahora 5% no 15%) |

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
| `Synergies` | `SynergyTableSO` | null | **(S32)** Ref a tabla de recetas de sinergia. Sin tabla asignada = sin sinergias activas |
| `Roles` | `RoleTableSO` | null | **(S37)** Ref a tabla de perfiles de rol 3v3. Sin tabla asignada = roles sin efecto (stats base sin mods). Mapeada por `CreatureDNA.Role` (enum). |

### Needs

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `EnergyCostToQueue` | `float` | 15f | Energía gastada por MoriMochi al encolarse para combate async |

## Fórmulas Aplicadas

- **HP Combate:** Constitution × 5
- **Daño efectivo:** ATK × (1.0 si hit, 3.0 si crit) × (1 - Defense × 0.08)
- **Crit chance:** CritChance + Luck × LuckCritPerPoint
- **Evasión:** Evasion × EvasionPerPoint

## Cambios S37

**Nuevo campo:**
- `Roles` (RoleTableSO) — tabla de perfiles de rol (Protector/Agresivo/Empático) con modificadores de stats (ConMod, AtkMod, SpdMod) y efectos tácticos (ShieldPerTurn, BacklineHitChance, HealPercentOfDamage).

**Modificación de valor:**
- `DeathChance`: 0.15f → 0.05f (muerte permanente menos frecuente; ahora 5% de probabilidad de que 1 unit perdedor al azar muera)

**Consumo en BuildCombatant (S37):**
```csharp
// En CombatService.BuildCombatant():
var roleProfile = config.Roles?.GetProfile(dna.Role);
if (roleProfile != null)
{
    c.MaxHp = (dnaBaseStats.Constitution + roleProfile.ConMod) * BaseHpCombatMultiplier;
    c.Attack = dnaBaseStats.Attack + roleProfile.AtkMod;
    c.Speed = dnaBaseStats.Speed + roleProfile.SpdMod;
}
```

**Consumo en SimulateCore (S37 muerte):**
```csharp
// Al terminar combate, si no draw:
if (!result.IsDraw && rng.NextFloat() < config.DeathChance)
{
    var loserTeam = result.TeamAWon ? teamB : teamA;
    var loserIdx = rng.Range(0, loserTeam.Count);
    loserTeam[loserIdx].Dna.IsDead = true;
    result.DiedUnitId = loserTeam[loserIdx].Dna.UniqueID;
}
```

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CombatService]] — usa todos los fields de fórmulas, pasa `Synergies` + aplica `Roles` en BuildCombatant
- [[CombatController]] — serializa como componente
- [[AsyncCombatService]] — usa para validación
- [[SynergyTableSO]] — tabla de recetas (S32)
- [[RoleTableSO]] — tabla de perfiles (S37)
- [[CombatResolver]] — recibe `config.Synergies` en constructor

## Conexiones

**Entrada:**
- Scene → asignado en `CombatController.config` field (Odin Inspector)

**Salida:**
- Pasado a `CombatService.Simulate()` y `SimulateCore()`
- `config.Synergies` → `CombatResolver.Synergies` (S32)
- `config.Roles` → usado en `BuildCombatant()` (S37)
- Accedido por `CombatController.Config` getter

## Notas (S32 + S37)

- **Backward compatible:** `Synergies` y `Roles` tienen default null (ambas features opcionales).
- **Deshabilitación:** Si `Synergies == null`, `CombatResolver.CheckSynergies()` retorna temprano. Si `Roles == null`, BuildCombatant no aplica mods de rol (stats base sin cambios).
- **S37 DeathChance:** Reducción a 5% (de 15%) reduce frustración por morte permanente. Se aplica al azar a 1 unit del equipo perdedor (no garantizado).
- **Odin:** Sections con `[Title()]`, `[InfoBox()]`, `[LabelWidth()]` para UI inspector.
