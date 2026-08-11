---
tags: [script, combat, roles, hooks, elements]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatRoleHooks.cs

**Ruta:** `Systems/Combat/CombatRoleHooks.cs`

**Responsabilidad:** Mediador estático que ejecuta efectos polimórficos de rol definidos en `RoleProfile.Passives` y `RoleProfile.Actives`. **S40:** Descomposición de `CombatService.TakeTurn()` — antes la lógica de rol estaba incrustada por rol enum; ahora es data-driven vía listas polimórficas `RolePassiveBase`/`RoleActiveBase` serializadas en el asset `RoleTableSO`. **S41:** Parámetro `r` (CombatResolver) nuevo para propagar eventos elementales. **S46:** Método `GrantShield` renombrado a `ApplyPassives`. Llama `passive.OnAfterStrike()` (ya no `OnTurnStart`). Corre POST-STRIKE (después de daño y Affinity, antes de heal).

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `ResolveTarget(actor, profile, allies, enemies, config, result, r, rng)` | `Combatant` | **S41** Itera `profile.Actives`, cada uno intenta `ResolveTarget()` (retorna `null` si no override). Si ninguno, fallback a `CombatTargeting.PickFrontTarget()`. Parámetro `r` para propagar. |
| `ApplyPassives(actor, profile, allies, config, result, r, rng)` | `void` | **S46** Itera `profile.Passives`, cada uno llama `OnAfterStrike()` (marca, shield, etc). Post-strike (paso 10 de TakeTurn). Parámetro `r` para propagar. |
| `HealAfterStrike(actor, profile, allies, dodged, damage, config, result, r, rng)` | `void` | **S41** Itera `profile.Passives`, cada uno llama `OnDamageDealt()` (heal, marca elemental, etc). Post-strike si hit + damage > 0. |

## Flujo de Integración (S46)

**En `CombatService.TakeTurn()` nuevo orden:**

```csharp
var profile = config.Roles != null ? config.Roles.GetProfile(actor.Role) : null;

// Targeting (antes de strike)
var target = CombatRoleHooks.ResolveTarget(actor, profile, allies, enemies, config, result, r, rng);

// Strike...
var strike = CombatStrike.Execute(actor, target, config, result, r, rng);

// Affinity (post-strike)
GainAffinity(actor, config, result, r, rng);

// Pasivas (post-strike, post-affinity)
CombatRoleHooks.ApplyPassives(actor, profile, allies, config, result, r, rng);

// Heal-on-damage (post-strike)
CombatRoleHooks.HealAfterStrike(actor, profile, allies, strike.Dodged, strike.Damage, config, result, r, rng);

// Lifesteal
```

**Implementación (S46):**

```csharp
public static void ApplyPassives(Combatant actor, RoleProfile profile, List<Combatant> allies, 
    CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
{
    if (profile == null || profile.Passives == null) return;

    foreach (var passive in profile.Passives)
        passive.OnAfterStrike(actor, allies, config, result, r, rng);
}

public static void HealAfterStrike(Combatant actor, RoleProfile profile, List<Combatant> allies, 
    bool dodged, float damage, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
{
    if (dodged || damage <= 0f || profile == null || profile.Passives == null) return;

    foreach (var passive in profile.Passives)
        passive.OnDamageDealt(actor, allies, damage, config, result, r, rng);
}
```

**RNG consumption order INVARIANTE:**
- `ResolveTarget()` — si hay Actives, cada uno consume RNG en orden del profile
- `ApplyPassives()` — si hay Passives, cada uno consume RNG en orden (típicamente pick ally, mark)
- `HealAfterStrike()` — si hay Passives, cada uno consume RNG en orden

## Cambios S46

**GrantShield → ApplyPassives:**
- Renombrado porque ahora hace más que solo shield: también marca y heal (polimórfico vía RolePassiveBase.OnAfterStrike).

**Relocación post-strike:**
- Antes (S40): GrantShield corre PRE-STRIKE (paso 5-6)
- Ahora (S46): ApplyPassives corre POST-STRIKE (paso 10), después de GainAffinity
- Todas las pasivas ahora leen el mismo "post-strike" estado (daño aplicado, Affinity actualizado)

**Sin gates de Energy:**
- Pasivas ya no tienen `if (actor.Energy > 0)` — aplican SIEMPRE
- El rol Agresivo con `MarkRandomAllyPassive` marca cada turno

**Hook renombrado:** `OnTurnStart()` → `OnAfterStrike()`
- La firma es idéntica, solo el nombre cambia semántica
- Corre una sola vez por turno, post-strike

## Cambios S41

**Nuevo parámetro `r` (CombatResolver):**
- Los tres métodos ahora reciben `CombatResolver r` para propagar a pasivas/activas
- Backward compatible: parámetro requerido pero si es null, nada se graba

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[CombatService]] — llamado en `TakeTurn()` (S46: dos puntos: targeting pre-strike, ApplyPassives post-strike)
- [[CombatResolver]] — receptor de eventos elementales
- [[RoleTableSO]] — proveedor de `RoleProfile` con listas polimórficas
- [[RolePassiveBase]] — base de toda pasiva (OnAfterStrike S46, OnDamageDealt)
- [[RoleActiveBase]] — base de todo active (ResolveTarget override)
- [[CombatTargeting]] — fallback `PickFrontTarget()` si ningún Active override
- [[Combatant]], [[CombatManagerSO]], [[CombatResult]], [[CombatRng]]
