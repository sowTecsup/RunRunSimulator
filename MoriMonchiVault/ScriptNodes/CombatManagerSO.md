---
tags: [scriptable-object, combat, config]
---

# CombatManagerSO

**Ruta:** `Data/Combat/CombatManagerSO.cs`

**Responsabilidad:** Configuración inmutable de combate: fórmulas, chances, límites, balance, tablas de roles y tablas elementales. Instancia única referenciada por `CombatService`, `CombatController`, `AsyncCombatService`. `SerializedScriptableObject` sin static; lo expone `CombatController.Config`. **S37:** Nuevo campo `Roles` (tabla de perfiles 3v3). **S40:** Eliminación de 8 knobs elementales hardcoded; nuevo campo `Elements` apunta a `ElementTableSO` (tabla centralizada de identidades, estados, reacciones). **S62:** Nuevos campos `SuddenDeathStartRound` y `SuddenDeathMultipliers` para escalada de daño en rondas tardías.

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
| `MaxRounds` | `int` | 50 | Rounds máximos; si se alcanzan → TIEBREAK (gana mayor HP%) |
| `MaxFightCount` | `int` | 5 | Combates máximos por criatura por sesión |

### Sudden Death (S62)

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `SuddenDeathStartRound` | `int` | 6 | **S62 NEW** Ronda en que comienza escalada de daño (rondas 1-5 = multiplicador 1.0, sin cambio) |
| `SuddenDeathMultipliers` | `List<float>` | {1.4, 1.8, 2.2, 2.6, 3.0} | **S62 NEW** Tabla de multiplicadores por ronda. Índice 0 = round SuddenDeathStartRound, índice 1 = round+1, etc. Última entrada se mantiene para rondas posteriores. |

### Status / Balance

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `StunImmunityTurns` | `int` | 1 | Turnos de inmunidad a stun tras despertar. Anti-permastun: cuando StunTurns llega a 0, se asigna este valor para impedir re-stun inmediato |
| `Roles` | `RoleTableSO` | null | **(S37)** Ref a tabla de perfiles de rol 3v3. Sin tabla asignada = roles sin efecto (stats base sin mods). Mapeada por `CreatureDNA.Role` (enum). |

### Elemental (S40)

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `Elements` | `ElementTableSO` | null | **(S40)** Ref a tabla elemental centralizada: identidades de elementos, definiciones de estados (Percent/Amount), y 12 reacciones con efectos polimórficos. Sin tabla = sin reacciones ni bonus de estados. Reemplaza los 8 knobs individuales. |

### Needs

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `EnergyCostToQueue` | `float` | 15f | Energía gastada por MoriMochi al encolarse para combate async |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `SuddenDeathMultiplier(int round)` | `float` | **S62 NEW** Retorna multiplicador de daño para la ronda dada. Si round < SuddenDeathStartRound, retorna 1.0. Si round >= SuddenDeathStartRound, indexa en SuddenDeathMultipliers (clamped al último valor si excede). |

**Implementación (S62):**
```csharp
public float SuddenDeathMultiplier(int round)
{
    if (SuddenDeathMultipliers == null || SuddenDeathMultipliers.Count == 0 || round < SuddenDeathStartRound) 
        return 1f;
    int idx = Mathf.Min(round - SuddenDeathStartRound, SuddenDeathMultipliers.Count - 1);
    return Mathf.Max(1f, SuddenDeathMultipliers[idx]);
}
```

## Fórmulas Aplicadas

- **HP Combate:** Constitution × 5
- **Daño efectivo (S62):** ATK × (1.0 si hit, 3.0 si crit) × (1 - Defense × 0.08) × SuddenDeathMultiplier(round) × (1 + Boiling%)
- **Crit chance:** CritChance + Luck × LuckCritPerPoint
- **Evasión:** Evasion × EvasionPerPoint

## Cambios S62

**Sudden Death:**
- Dos nuevos campos: `SuddenDeathStartRound` (int, default 6) y `SuddenDeathMultipliers` (List<float>, default {1.4, 1.8, 2.2, 2.6, 3.0})
- A partir de la ronda 6, cada golpe multiplica su daño post-DEF según la tabla
- Ronda 6 → 1.4x, Ronda 7 → 1.8x, Ronda 8 → 2.2x, Ronda 9 → 2.6x, Ronda 10+ → 3.0x
- Método `SuddenDeathMultiplier(round)` calcula automáticamente basado en la ronda (clamp al último valor)
- Log de golpe incluye marker "MSx{valor}" cuando multiplicador > 1.0
- Aplicado en `CombatStrike.Execute()` post-DEF, pre-Boiling

## Cambios S40

**Eliminación de 8 knobs individuales:**
- Antes: `VaporizadoEvaBonus`, `GolpePrecisoCritBonus`, `BoilingDamageBonus`, `CharcoalReflectPercent`, `CleanseHealPercent`, `LeechAmount`, `MareadoChance`, `MareadoDamage`
- Ahora: Centralizados en `ElementTableSO.States` (dict por ElementalState)

**Nuevo campo Elements:**
```csharp
[Title("Elemental")]
[InfoBox("Tabla elemental: identidad de elementos, estados y reacciones...")]
public ElementTableSO Elements;
```

**Acceso en tiempo de ejecución:**
```csharp
// En CombatStrike.Execute():
evaChance += target.HasState(ElementalState.Vaporizado) 
    ? (config.Elements != null ? config.Elements.StatePercent(ElementalState.Vaporizado) : 0f) 
    : 0f;

// En CombatElements.AddMark():
var reaction = config.Elements != null ? config.Elements.FindReaction(otherElement, element, allySource) : null;
```

## Consumo en CombatService (S37 + S40 + S62)

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

// En CombatStrike (S40):
float evaChance = target.EffEvasion * config.EvasionPerPoint;
evaChance += target.HasState(ElementalState.Vaporizado) 
    ? (config.Elements != null ? config.Elements.StatePercent(ElementalState.Vaporizado) : 0f) 
    : 0f;

// En CombatStrike (S62):
float suddenDeath = config.SuddenDeathMultiplier(r.Round);
if (suddenDeath > 1f) damage *= suddenDeath;

// En CombatElements.AddMark (S40):
var reaction = config.Elements != null ? config.Elements.FindReaction(...) : null;
if (reaction != null)
{
    foreach (var e in reaction.Effects) e.Apply(...);
}
```

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CombatService]] — usa todos los fields de fórmulas + aplica `Roles` en BuildCombatant; accede `Elements` vía `CombatStrike`/`CombatElements`, `SuddenDeathMultiplier()` en CombatStrike (S62)
- [[CombatController]] — serializa como componente
- [[AsyncCombatService]] — usa para validación
- [[CombatStrike]] — consume `config.Elements.StatePercent()` para magnitudes de estado (S40), `config.SuddenDeathMultiplier(r.Round)` (S62)
- [[CombatElements]] — consume `config.Elements.FindReaction()` para reacciones (S40)
- [[ElementTableSO]] — tabla centralizada (S40)
- [[RoleTableSO]] — tabla de perfiles (S37)
- [[CombatResolver]] — grabación de procs (ya no recibe config); consulta para Sudden Death vía config en CombatStrike

## Conexiones

**Entrada:**
- Scene → asignado en `CombatController.config` field (Odin Inspector)

**Salida:**
- Pasado a `CombatService.Simulate()` y `SimulateCore()`
- `config.Roles` → usado en `BuildCombatant()` (S37)
- `config.Elements` → usado en `CombatStrike.Execute()` (S40) y `CombatElements.AddMark()` (S40)
- `config.SuddenDeathMultiplier(round)` → usado en `CombatStrike.Execute()` (S62)
- Accedido por `CombatController.Config` getter

## Notas (S32 + S37 + S39 + S40 + S62)

- **Backward compatible:** `Roles` tiene default null (sin tabla = roles sin efecto). `Elements` tiene default null (sin tabla = sin reacciones ni bonus de estados). `SuddenDeathMultipliers` puede estar vacío (multiplicador 1.0 siempre).
- **S37 DeathChance:** 5% de probabilidad de que 1 unit del equipo perdedor muera (no garantizado).
- **S40 Elemental:** Centralización de config en `ElementTableSO` vs. knobs individuales. `StatePercent()` y `StateAmount()` abstraen acceso a magnitudes.
- **S62 Sudden Death:** Escalada opcional de daño en rondas tardías. Si `SuddenDeathMultipliers` está vacío o `SuddenDeathStartRound` > MaxRounds, el multiplicador siempre es 1.0 (sin efecto).
- **Odin:** Sections con `[Title()]`, `[InfoBox()]`, `[LabelWidth()]` para UI inspector.
- **Editor-safe:** PopulateV1/PopulateV2 buttons en ElementTableSO/RoleTableSO permiten defaults sin edición manual.
