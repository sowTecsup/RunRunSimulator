---
tags: [combat, data, mutable-state]
---

# Combatant

**Ruta:** `Systems/Combat/Combatant.cs`

**Responsabilidad:** Modelo mutable de un combatiente *durante* la simulación 3v3. Almacena snapshot de DNA, stats finales (después de equipment + role mods), HP presente, escudo (S37), rol (S37), fila (S37), índice (S37), stun/immunity counters, y lista de procs/efectos activos. **S35:** Propiedades dinámicas (`EffDefense`, `EffEvasion`, `EffSpeed`, `LifestealPercent`) que suman stacks de elementos activos en tiempo real.

## Estructura

### Combatant (clase pública)

**Campos públicos:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Dna` | `CreatureDNA` | Referencia a la criatura (se mutará si gana/muere) |
| `Name` | `string` | Nombre para logging y snapshots |
| `IsA` | `bool` | Si true, pertenece al equipo A (vs B). Usado en ambos 1v1 legacy y 3v3. |
| `Role` | `Role` | **S37** Rol de combate (Protector, Agresivo, Empático). Determina efectos de rol en TakeTurn. |
| `Row` | `CombatRow` | **S37** Fila ocupada (Front=0, Mid=1, Back=2). Define quién puede ser target por backline/frontline hits. |
| `Index` | `int` | **S37** Índice dentro del equipo (0..2). Usado en CombatTurn para identificar al unit. |
| `Hp` | `float` | HP actual durante combate |
| `MaxHp` | `float` | HP máximo = (Constitution + RoleConMod) * BaseHpCombatMultiplier |
| `Shield` | `float` | **S37** Escudo de rol Protector (acumula ShieldPerTurn, absorbido antes de Hp, decrementa por daño) |
| `Attack` | `float` | Ataque total (base + role mods + equipment) |
| `Speed` | `float` | Velocidad total (base + role mods + equipment) |
| `Defense` | `float` | Defensa total (base + equipment) |
| `Luck` | `float` | Suerte total |
| `Evasion` | `float` | Evasión total base |
| `StunTurns` | `int` | Turnos de stun activos (decrementa cada turno) |
| `StunImmunityTurns` | `int` | Turnos de inmunidad a stun post-despertar (decrementa) |
| `Procs` | `List<CombatProcEffect>` | Todos los procs del equipment equipado |
| `Active` | `List<ActiveEffect>` | Estados en curso (Poison, Burn, Regen, Static, Pulse, Steel, Mist, Lifesteal, etc.) |

**Cambios S37:** Role, Row, Index, Shield son nuevos. IsA sigue siendo poblado (legacy 1v1 compat + identificación de equipo en 3v3).

**Propiedades dinámicas (S35):**

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `IsAlive` | `bool` | Hp > 0f |
| `EffDefense` | `float` | Defense + suma de Magnitude de stacks Steel activos |
| `EffEvasion` | `float` | Evasion + suma de Magnitude de stacks Mist activos |
| `EffSpeed` | `float` | Speed - suma de Magnitude de stacks Static, clamped a 0 |
| `LifestealPercent` | `float` | Suma de Magnitude de stacks Lifesteal / 100f, clamped a 1 |

**Cálculo de propiedades dinámicas:**
```csharp
public float EffDefense => Defense + StackSum(ModifierEffectKind.Steel);
public float EffEvasion => Evasion + StackSum(ModifierEffectKind.Mist);
public float EffSpeed   => Mathf.Max(0f, Speed - StackSum(ModifierEffectKind.Static));
public float LifestealPercent => Mathf.Min(1f, StackSum(ModifierEffectKind.Lifesteal) / 100f);

private float StackSum(ModifierEffectKind kind)
{
    float sum = 0f;
    foreach (var a in Active)
        if (a.Kind == kind) sum += a.Magnitude;
    return sum;
}
```

Las propiedades se recalculan en cada acceso (no cacheadas) porque `Active` muta durante el turno.

### ActiveEffect (clase interna)

Estructura de un status activo durante combate.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Kind` | `ModifierEffectKind` | Tipo (Poison, Burn, Regen, Static, Pulse, Steel, Mist, Lifesteal, etc.) |
| `RemainingTurns` | `int` | Turnos restantes (decrementa) |
| `Magnitude` | `int` | Daño/curación/bonus por turno o para cálculos dinámicos |

## Ciclo de Vida (S37)

1. `CombatService.BuildCombatant(dna, db, equipDb, isA, row, index)` — crea instancia, carga DNA/equipment, asigna Role/Row/Index/Shield=0
2. `CombatService.SimulateCore()` — itera ambos equipos en orden de EffSpeed
3. `TakeTurn()` muta: `Hp`, `Shield` (role Protector), `StunTurns`, `StunImmunityTurns`, `Active` (lista crece/shrinks)
4. Las propiedades dinámicas (`EffSpeed`, `EffDefense`, etc.) se leen durante orden de turno y en fórmulas de daño
5. `EmitTurn()` captura estado final de `Hp`/`Shield`/`Active` en `CombatUnitState` (S37)
6. Final de combate: si ganó/perdió, mutaciones vuelven al DNA persistente via `CombatEvolution.AdvanceTier()` o muerte

## Cambios S37

**Nuevos campos:**
- `Role` — determina efectos de rol (escudo Protector, backline Agresivo, heal Empático)
- `Row` — fila del grid 2-3-2 (define quién es frontline/backline)
- `Index` — posición en equipo (0..2, mapea a CombatTurn.AttackerIndex/DefenderIndex)
- `Shield` — pool de escudo acumulado (aplicado vía Protector role, absorbido pre-HP)

**Cálculo de stats con role mods (S37):**
En `BuildCombatant()`:
```csharp
var statsBefore = CombatStats.GetEffectiveStats(dna, db);  // Base + parts
var profile = config.RoleProfiles.GetProfile(dna.Role);
c.MaxHp   = (statsBefore.Constitution + profile.ConMod) * BaseHpCombatMultiplier;
c.Attack  = statsBefore.Attack + profile.AtkMod;
c.Speed   = statsBefore.Speed + profile.SpdMod;
// Luego se aplican equipment stats (EquipmentStats.Apply) que modifican estos valores
```

**Nota:** Role mods se aplican POST-acumulación de partes (via CombatStats) pero PRE-equipment. El pipeline es: DNA base → partes acumuladas → role mods → equipment mods → final stats.

**Uso de Role/Row en TakeTurn (S37):**
- `Protector`: `PickAlly(myTeam)` + `ShieldTarget(ally, profile.ShieldPerTurn)` → suma al `Shield` del aliado
- `Agresivo`: si `rng.NextFloat() < profile.BacklineHitChance` → `PickBacklineTarget(oppTeam)`, else `PickFrontTarget(oppTeam)`
- `Empático`: post-strike si golpea → `LowestHpAlly(myTeam).Hp += damage * profile.HealPercentOfDamage`

## Uso de Propiedades Dinámicas

### EffSpeed — Orden de turno (S37)

En `CombatService.SimulateCore()`, orden de turnos:
```csharp
// Pre-sort: one speed tiebreak roll per unit (both teams)
foreach (var c in teamA) c.TiebreakerRoll = rng.NextFloat();
foreach (var c in teamB) c.TiebreakerRoll = rng.NextFloat();

// Sort by: EffSpeed desc, TiebreakerRoll desc, team A-before-B, Index asc
List<Combatant> turnOrder = units.OrderByDescending(c => c.EffSpeed)
    .ThenByDescending(c => c.TiebreakerRoll)
    .ThenBy(c => c.IsA ? 0 : 1)
    .ThenBy(c => c.Index)
    .ToList();
```

Static reduce Speed en tiempo real; un combatiente con Static×2 puede perder el orden si Speed es similar.

### EffDefense — Mitigación de daño

En `CombatService.TakeTurn()`:
```csharp
float reduction = Mathf.Clamp01(def.EffDefense * config.DefenseReductionPerPoint);
damage          = raw * (1f - reduction);
```

Steel apila DEF dinámicamente, aumentando la mitigación.

### EffEvasion — Evasión de golpes

En `CombatService.TakeTurn()`:
```csharp
float evaChance = def.EffEvasion * config.EvasionPerPoint;
bool  dodged    = evaRoll < evaChance;
```

Mist apila EVA dinámicamente, aumentando chance de esquivar.

### LifestealPercent — Curación post-golpe

En `CombatService.TakeTurn()`:
```csharp
if (!dodged && damage > 0f && atk.LifestealPercent > 0f)
{
    float steal = damage * atk.LifestealPercent;
    atk.Hp = Mathf.Min(atk.MaxHp, atk.Hp + steal);
    result.Log.Add($"    [Lifesteal] {atk.Name} +{steal:F1} → {atk.Hp:F1}");
    r.Record(ModifierEffectKind.Lifesteal, atk, steal);
}
```

El % de daño infligido vuelve como cura al atacante (solo si golpea y no es esquivado).

### Shield — Absorción de daño (S37)

En `CombatService.TakeTurn()`, post-daño:
```csharp
float absorbed = Mathf.Min(def.Shield, damage);
def.Shield -= absorbed;
damage -= absorbed;  // El remainder va a HP
if (damage > 0f) def.Hp -= damage;
```

El escudo actúa como pool de absorción antes de HP. Acumulado vía Protector role (`ShieldPerTurn`), decrementa con daño recibido.

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[CombatService]] — construye y muta durante simulación
- [[CombatResolver]] — accede para aplicar acciones
- [[ActiveEffect]] — lista de efectos activos
- [[ModifierEffectKind]] — enum de tipos de efectos
- [[Role]] — enum, determina comportamiento
- [[CombatRow]] — enum, fila en grid
- [[RoleTableSO]] — perfil de role, mods de stats

## Conexiones

**Entrada:**
- `CombatService.BuildCombatant(dna, db, equipDb, isA, row, index)` — crea instancia con Role/Row/Index del DNA
- `CombatResolver.AddStatus()` — agrega a `Active` list
- `CombatResolver.ShieldTarget()` — suma a `Shield` (role Protector)

**Salida:**
- Cambios en `Hp`, `Shield`, stun counters, `Active` vía CombatResolver
- Las propiedades dinámicas se leen en Speed order, daño, evasión, lifesteal
- Final de combat: mutation vuelve a DNA vía CombatEvolution (tiers) o CombatRecord (historia)

## Notas

- No se serializa; es exclusivamente un modelo de simulación en tiempo real.
- Su `Dna` apunta a la misma criatura que en la registry, y se mutará solo si el combate modifica tiers/muerte.
- `StunImmunityTurns` implementa anti-permastun (ver `CombatManagerSO.StunImmunityTurns`).
- **S35:** Las propiedades dinámicas permiten que stacks de elementos *en curso* modifiquen el comportamiento en tiempo real. No hay pre-cálculo; se leen on-demand cada turno.
- **S37:** Role/Row/Index definen semántica 3v3. Shield es pool de absorción (Protector role). IsA se mantiene para backward compat + identificación de equipo.
