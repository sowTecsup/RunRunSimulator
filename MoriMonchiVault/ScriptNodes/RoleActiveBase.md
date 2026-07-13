---
tags: [script, combat, roles, targeting, base-class, elements]
---

# RoleActiveBase.cs

**Ruta:** `Data/Combat/RoleActiveBase.cs`

**Responsabilidad:** Clase abstracta base para efectos active de rol (override de targeting) serializables en listas polimórficas. **S40:** Abstracción de lógica de targeting heredable, eliminando branches enum-based del antes. Un active decide si OVERRIDE el targeting por defecto (retorna `Combatant` non-null) o pasa (retorna `null`). Primero active que retorna non-null gana; fallback a front-row si todos retornan `null`. Soporte polimórfico vía `[Serializable]` + Odin Inspector. **S41:** Parámetro `r` (CombatResolver) nuevo para emitir eventos elementales de energía gasto/ganancia (EnergySpent, EnergyGained).

## Métodos Abstractos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `ResolveTarget(actor, allies, enemies, config, result, r, rng)` | `Combatant \| null` | **S41 SIG CAMBIÓ** Intenta override targeting. Si retorna non-null, es el objetivo. Si null, continúa con siguiente active o fallback. Parámetro `r` nuevo S41 para emitir eventos elementales. |
| `Summary()` | `string` | Retorna descripción UI del efecto (ej: "50% caza backline enemiga") |

## Implementaciones Concretas

### BacklineHunterActive

**Descripción:** Rol Agresivo. Pre-targeting, roll chance `Chance` (ej: 50%) para ignorar front-row y golpear backline en su lugar. Si no hay backline, puede gastar Energía para aumentar energía de un aliado (efecto bonus).

**Campos:**
- `Chance` (float, PropertyRange 0–1, LabelText "Backline chance") — defecto 0.5 (50%)

**ResolveTarget (S41 FIRMA CAMBIÓ):**
1. Si Chance ≤ 0, retorna `null` (no override)
2. Roll `aggroRoll = rng.NextFloat()`
3. Si `aggroRoll < Chance`:
   - Intenta `CombatTargeting.PickBacklineTarget(enemies, rng)`
   - Si existe backline:
     - Log "caza backline"
     - Retorna target
     - Si actor.Energy > 0:
       - Decrementa: `actor.Energy--`
       - Graba evento: `r.RecordElement(ElementEventKind.EnergySpent, actor, amount: actor.Energy)` **(S41 NEW)**
       - Pick aliado random: `CombatTargeting.PickAlly(allies, rng)`
       - Marca aliado: `CombatElements.AddMark(ally, actor.Element, true, actor, config, result, r, rng)` (parámetro `r` nuevo S41)
   - Si NO hay backline:
     - Si actor.Energy > 0:
       - Decrementa: `actor.Energy--`
       - Graba evento: `r.RecordElement(ElementEventKind.EnergySpent, actor, amount: actor.Energy)` **(S41 NEW)**
       - Pick aliado random y aumenta su energía: `mate.Energy++`
       - Log "comparte energía con X"
       - Graba evento: `r.RecordElement(ElementEventKind.EnergyGained, mate, amount: mate.Energy)` **(S41 NEW)**
     - Si no Energy: log "sin backline — comparte energía (sin efecto)"
     - Retorna fallback `PickFrontTarget()` (via CombatRoleHooks)
4. Si `aggroRoll >= Chance`: retorna `PickFrontTarget()` (fallback clamped a default, no override)

**Consumo RNG:** `rng.NextFloat()` una vez (roll Chance), más picks si aplica

## Flujo de Integración (S40 + S41)

**En `RoleTableSO.RoleProfile`:**
```csharp
public List<RoleActiveBase> Actives = new List<RoleActiveBase>();
```

**En `CombatService.TakeTurn()` (S41 FIRMA CAMBIÓ):**
```csharp
var profile = config.Roles != null ? config.Roles.GetProfile(actor.Role) : null;
var target = CombatRoleHooks.ResolveTarget(actor, profile, allies, enemies, config, result, r, rng);  // parámetro r nuevo
```

**En `CombatRoleHooks` (S41):**
```csharp
public static Combatant ResolveTarget(Combatant actor, RoleProfile profile, ..., CombatResolver r, ...)
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
```

## Determinismo

- **Odin Serialization:** Polimórficas en inspector como lista editable (Odin maneja [SerializableField])
- **RNG:** Cada active consume RNG según su lógica (roll Chance, picks); orden sincronizado con profile order
- **Energía:** Si actor.Energy > 0 y aplica efecto, decrementa — determinista sin roll (S41: eventos grabados en orden)
- **Fallback:** Si todos actives retornan null, CombatRoleHooks fallback a frontline

## Cambios S41

**Nuevo parámetro `r` (CombatResolver):**
- `ResolveTarget()` ahora recibe `CombatResolver r` para emitir eventos elementales
- Cuando actor gasta Energy, emite `ElementEventKind.EnergySpent`
- Cuando aliado gana Energy (en BacklineHunter fallback), emite `ElementEventKind.EnergyGained`
- `AddMark()` ahora recibe `r` (parámetro nuevo S41) para grabar eventos de marca/reacción

**Eventos emitidos (S41):**
- `ElementEventKind.EnergySpent` — cuando actor decrementa Energy
- `ElementEventKind.EnergyGained` — cuando aliado incrementa Energy (BacklineHunter fallback)
- `ElementEventKind.MarkApplied` — cuando se añade marca aliada (emitido dentro de AddMark)

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[RoleTableSO]] — serializadas en `RoleProfile.Actives` lista polimórfica
- [[CombatRoleHooks]] — invocador en `ResolveTarget()` (parámetro `r` nuevo S41)
- [[CombatResolver]] — receptor de `RecordElement()` (S41 NEW)
- [[CombatService]] — `TakeTurn()` captura resultado y lo usa como target (parámetro `r` nuevo S41)
- [[CombatElements]] — llamada para `AddMark()` si Energy gasto (parámetro `r` nuevo S41)
- [[Combatant]] — actor/allies/enemies context, Energy mutado si aplica
- [[CombatManagerSO]], [[CombatResult]], [[CombatRng]] — contexto
- [[CombatTargeting]] — PickBacklineTarget, PickFrontTarget, PickAlly
