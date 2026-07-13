---
tags: [script, combat, roles, hooks, elements]
---

# CombatRoleHooks.cs

**Ruta:** `Systems/Combat/CombatRoleHooks.cs`

**Responsabilidad:** Mediador estático que ejecuta efectos polimórficos de rol definidos en `RoleProfile.Passives` y `RoleProfile.Actives`. **S40:** Descomposición de `CombatService.TakeTurn()` — antes la lógica de rol estaba incrustada por rol enum (Protector/Agresivo/Empático); ahora es data-driven vía listas polimórficas `RolePassiveBase`/`RoleActiveBase` serializadas en el asset `RoleTableSO`. Mismo orden de consumo RNG, mismo log strings, cero cambio de gameplay. **S41:** Parámetro `r` (CombatResolver) nuevo para propagar eventos elementales desde las pasivas/activas de rol.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `ResolveTarget(actor, profile, allies, enemies, config, result, r, rng)` | `Combatant` | **S41 SIG CAMBIÓ** Itera `profile.Actives`, cada uno intenta `ResolveTarget()` (retorna `null` si no override). Si ninguno, fallback a `CombatTargeting.PickFrontTarget()`. Parámetro `r` nuevo S41 para propagar a Actives. |
| `GrantShield(actor, profile, allies, config, result, r, rng)` | `void` | **S41 SIG CAMBIÓ** Itera `profile.Passives`, cada uno llama `OnTurnStart()` (shield, marca elemental, etc). Pre-ataque en turn order. Parámetro `r` nuevo S41 para propagar a Passives. |
| `HealAfterStrike(actor, profile, allies, dodged, damage, config, result, r, rng)` | `void` | **S41 SIG CAMBIÓ** Itera `profile.Passives`, cada uno llama `OnDamageDealt()` (heal, marca elemental, etc). Post-strike si hit + damage > 0. Parámetro `r` nuevo S41 para propagar a Passives. |

## Flujo de Integración (S40 + S41)

**En `CombatService.TakeTurn()` (S41 FIRMA CAMBIÓ):**

```csharp
var profile = config.Roles != null ? config.Roles.GetProfile(actor.Role) : null;

// Pre-targeting (active override)
var target = CombatRoleHooks.ResolveTarget(actor, profile, allies, enemies, config, result, r, rng);  // parámetro r nuevo S41

// Pre-attack (passive effects: shield, elemental procs)
CombatRoleHooks.GrantShield(actor, profile, allies, config, result, r, rng);  // parámetro r nuevo S41

// Strike...

// Post-strike (passive effects: heal, elemental procs)
CombatRoleHooks.HealAfterStrike(actor, profile, allies, strike.Dodged, strike.Damage, config, result, r, rng);  // parámetro r nuevo S41
```

**Implementación (S41):**

```csharp
public static Combatant ResolveTarget(Combatant actor, RoleProfile profile, List<Combatant> allies, 
    List<Combatant> enemies, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
{
    if (profile != null && profile.Actives != null)
    {
        foreach (var active in profile.Actives)
        {
            var t = active.ResolveTarget(actor, allies, enemies, config, result, r, rng);  // parámetro r nuevo
            if (t != null) return t;
        }
    }
    return CombatTargeting.PickFrontTarget(enemies, rng);
}

public static void GrantShield(Combatant actor, RoleProfile profile, List<Combatant> allies, 
    CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
{
    if (profile == null || profile.Passives == null) return;

    foreach (var passive in profile.Passives)
        passive.OnTurnStart(actor, allies, config, result, r, rng);  // parámetro r nuevo
}

public static void HealAfterStrike(Combatant actor, RoleProfile profile, List<Combatant> allies, 
    bool dodged, float damage, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
{
    if (dodged || damage <= 0f || profile == null || profile.Passives == null) return;

    foreach (var passive in profile.Passives)
        passive.OnDamageDealt(actor, allies, damage, config, result, r, rng);  // parámetro r nuevo
}
```

**RNG consumption order INVARIANTE:**
- `ResolveTarget()` — si hay Actives, cada uno consume RNG en el orden del profile (típicamente BacklineHunterActive → roll)
- `GrantShield()` — si hay Passives, cada uno consume RNG en el orden del profile (típicamente pick ally, mark)
- `HealAfterStrike()` — si hay Passives, cada uno consume RNG en el orden (típicamente pick heal target, mark)

## Cambios S41

**Nuevo parámetro `r` (CombatResolver):**
- Los tres métodos ahora reciben `CombatResolver r` para propagar a pasivas/activas
- Pasivas emiten eventos elementales (EnergySpent, MarkApplied) via `r`
- Activas emiten eventos elementales (EnergySpent, EnergyGained, MarkApplied) via `r`
- Backward compatible: parámetro requerido pero si es null, nada se graba

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[CombatService]] — llamado en `TakeTurn()` en 3 puntos: targeting, pre-strike, post-strike (parámetro `r` nuevo S41)
- [[CombatResolver]] — receptor de eventos elementales (S41 NEW)
- [[RoleTableSO]] — proveedor de `RoleProfile` con listas polimórficas
- [[RolePassiveBase]] — base de toda pasiva (OnTurnStart, OnDamageDealt; parámetro `r` nuevo S41)
- [[RoleActiveBase]] — base de todo active (ResolveTarget override; parámetro `r` nuevo S41)
- [[CombatTargeting]] — fallback `PickFrontTarget()` si ningun Active override
- [[Combatant]], [[CombatManagerSO]], [[CombatResult]], [[CombatRng]] — context de ejecución
