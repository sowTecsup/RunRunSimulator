---
tags: [script, combat, roles, targeting, base-class, elements]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# RoleActiveBase.cs

**Ruta:** `Data/Combat/RoleActiveBase.cs`

**Responsabilidad:** Clase abstracta base para efectos active de rol (override de targeting) serializables en listas polimórficas. **S40:** Abstracción de lógica de targeting heredable, eliminando branches enum-based. Un active decide si OVERRIDE el targeting por defecto (retorna `Combatant` non-null) o pasa (retorna `null`). **S46:** Energy completamente eliminado de BacklineHunterActive — solo hace targeting puro.

## Métodos Abstractos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `ResolveTarget(actor, allies, enemies, config, result, r, rng)` | `Combatant \| null` | **S46** Intenta override targeting. Si retorna non-null, es el objetivo. Si null, continúa con siguiente active o fallback. |
| `Summary()` | `string` | Retorna descripción UI del efecto (ej: "50% caza backline enemiga") |

## Implementaciones Concretas

### BacklineHunterActive

**Descripción:** Rol Agresivo. Pre-targeting, roll chance `Chance` (ej: 50%) para ignorar front-row y golpear backline en su lugar. **S46:** Targeting PURO — se eliminaron las dos ramas de gasto de energía.

**Campos:**
- `Chance` (float, PropertyRange 0–1, LabelText "Backline chance") — defecto 0.5 (50%)

**ResolveTarget (S46):**
1. Si Chance ≤ 0, retorna `null` (no override)
2. Roll `aggroRoll = rng.NextFloat()`
3. Si `aggroRoll < Chance`:
   - Intenta `CombatTargeting.PickBacklineTarget(enemies, rng)`
   - Si existe backline:
     - Log: `"{actor.Name} caza la backline"`
     - Retorna backline target
   - Si NO hay backline:
     - Fallback: `CombatTargeting.PickFrontTarget(enemies, rng)`
     - Retorna frontline
4. Si `aggroRoll >= Chance`: retorna `null` (no override, pasó el roll)

**Consumo RNG:** `rng.NextFloat()` una vez (roll Chance), más picks si aplica

**Cambio S46:** Se eliminó toda la lógica de gasto de energía (dos branches del viejo código). Ahora es PURO TARGETING.

## Flujo de Integración (S46)

**En `RoleTableSO.RoleProfile`:**
```csharp
public List<RoleActiveBase> Actives = new List<RoleActiveBase>();
```

**En `CombatService.TakeTurn()` (S46):**
```csharp
var profile = config.Roles != null ? config.Roles.GetProfile(actor.Role) : null;
var target = CombatRoleHooks.ResolveTarget(actor, profile, allies, enemies, config, result, r, rng);
```

**En `CombatRoleHooks` (S46):**
```csharp
public static Combatant ResolveTarget(Combatant actor, RoleProfile profile, ..., CombatResolver r, ...)
{
    if (profile != null && profile.Actives != null)
    {
        foreach (var active in profile.Actives)
        {
            var t = active.ResolveTarget(actor, allies, enemies, config, result, r, rng);
            if (t != null) return t;
        }
    }
    return CombatTargeting.PickFrontTarget(enemies, rng);
}
```

## Determinismo

- **Odin Serialization:** Polimórficas en inspector como lista editable
- **RNG:** Cada active consume RNG según su lógica (roll Chance, picks); orden sincronizado con profile order
- **Fallback:** Si todos actives retornan null, CombatRoleHooks fallback a frontline

## Cambios S46

**BacklineHunterActive simplificado:**
- Eliminó rama 1: `if (backline exists) → gasta energía + marca aliado`
- Eliminó rama 2: `if (no backline) → gasta energía + comparte energía con aliado`
- Quedó solo: targeting puro (roll chance → pick backline o frontline)
- No más grabación de eventos EnergySpent/EnergyGained vía `r`

**Responsabilidad clara:** Solo targeting, no efectos de rol. Las marcas de rol (si aplican) vienen del paso de pasivas (ApplyPassives), no del targeting.

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[RoleTableSO]] — serializadas en `RoleProfile.Actives` lista polimórfica
- [[CombatRoleHooks]] — invocador en `ResolveTarget()`
- [[CombatService]] — `TakeTurn()` captura resultado y lo usa como target
- [[Combatant]] — actor/allies/enemies context
- [[CombatManagerSO]], [[CombatResult]], [[CombatRng]]
- [[CombatTargeting]] — PickBacklineTarget, PickFrontTarget
