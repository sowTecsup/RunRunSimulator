---
tags: [script, combat, roles, effects, base-class, elements]
---

# RolePassiveBase.cs

**Ruta:** `Data/Combat/RolePassiveBase.cs`

**Responsabilidad:** Clase abstracta base para efectos pasivos de rol serializables en listas polimórficas. **S40:** Abstracción de efectos pre-turno (OnTurnStart) y post-golpe (OnDamageDealt) heredables, eliminando branches enum-based. **S46:** Hook renombrado `OnTurnStart()` → `OnAfterStrike()` y relocado post-strike (paso 10). Ambos hooks corre SIEMPRE sin gate de Energy. `AddMark()` sin gates.

## Métodos Abstractos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `OnAfterStrike(actor, allies, config, result, r, rng)` | `void` | **S46** Ejecutado cada turno post-strike. Ej: shield aliado, marcar con elemento. Corre SIEMPRE (sin gate de Energy S46). |
| `OnDamageDealt(actor, allies, damage, config, result, r, rng)` | `void` | **S41** Ejecutado post-strike si damage > 0. Ej: curar aliado más débil. |
| `Summary()` | `string` | Retorna descripción UI del efecto (ej: "Escuda +1 por turno a un aliado") |

## Implementaciones Concretas

### ShieldAllyPassive

**Descripción:** Cada turno (post-strike), elige aliado al azar y otorga `AmountPerTurn` de escudo. Marca el aliado con elemento de actor (aliada source).

**Campos:**
- `AmountPerTurn` (float, MinValue 0, LabelText "Shield per turn") — escudo otorgado, defecto 1.0

**OnAfterStrike (S46):**
- Si `AmountPerTurn <= 0`, retorna sin efecto
- Pick aliado vivo al azar: `CombatTargeting.PickAlly(allies, rng)`
- `ally.Shield += AmountPerTurn`
- Log: `"{actor.Name} escuda a {ally.Name} +{AmountPerTurn}"`
- Record en resolver: `r.Record(ModifierEffectKind.Shield, ally, AmountPerTurn)`
- Marca aliado: `CombatElements.AddMark(ally, actor.Element, true, actor, config, result, r, rng)` (ally-sourced)

**Consumo RNG:** `PickAlly()` consume si hay múltiples candidatos

**Cambio S46:** Se eliminó el gate `if (actor.Energy > 0)` — la marca ahora se aplica SIEMPRE

### HealLowestAllyOnHitPassive

**Descripción:** Post-strike, elige aliado con menor HP y lo cura por porcentaje del daño infligido. Marca ese aliado con elemento de actor (aliada source).

**Campos:**
- `PercentOfDamage` (float, PropertyRange 0–1, LabelText "% of damage") — defecto 0.5 (50%)

**OnDamageDealt (S41):**
- Si `PercentOfDamage <= 0`, retorna sin efecto
- Pick aliado más débil: `CombatTargeting.LowestHpAlly(allies)`
- Si existe:
  - `float heal = damage * PercentOfDamage`
  - `ally.Hp = min(ally.MaxHp, ally.Hp + heal)`
  - Log: `"{actor.Name} cura a {ally.Name} +{heal}"`
  - Record: `r.Record(ModifierEffectKind.Heal, ally, heal)`
  - Marca aliado: `CombatElements.AddMark(ally, actor.Element, true, actor, config, result, r, rng)` (ally-sourced)

**Consumo RNG:** ninguno (LowestHpAlly es determinista, sin roll)

**Cambio S46:** Se eliminó el gate `if (actor.Energy > 0)` — la marca ahora se aplica SIEMPRE

### MarkRandomAllyPassive (S46 NUEVO)

**Descripción:** Pasiva del Agresivo. Cada turno (post-strike), elige aliado al azar y lo marca con elemento de actor.

**Campos:** ninguno (sin parámetros configurables)

**OnAfterStrike (S46):**
- Pick aliado vivo al azar: `CombatTargeting.PickAlly(allies, rng)`
- Si existe:
  - Log: `"{actor.Name} comparte su elemento con {ally.Name}"`
  - Marca aliado: `CombatElements.AddMark(ally, actor.Element, true, actor, config, result, r, rng)` (ally-sourced)

**Consumo RNG:** `PickAlly()` consume si hay múltiples candidatos

**Rol asignado:** Agresivo (via `RoleTableSO.PopulateV2()`)

## Flujo de Integración (S46)

**En `RoleTableSO.RoleProfile`:**
```csharp
public List<RolePassiveBase> Passives = new List<RolePassiveBase>();
```

**En `CombatService.TakeTurn()` nuevo orden (S46):**
```csharp
// Strike
var strike = CombatStrike.Execute(...);

// Affinity
GainAffinity(actor, config, result, r, rng);

// Pasivas (POST-STRIKE)
CombatRoleHooks.ApplyPassives(actor, profile, allies, config, result, r, rng);

// Heal-on-damage
CombatRoleHooks.HealAfterStrike(actor, profile, allies, strike.Dodged, strike.Damage, config, result, r, rng);
```

**En `CombatRoleHooks` (S46):**
```csharp
public static void ApplyPassives(Combatant actor, RoleProfile profile, ..., CombatResolver r, ...)
{
    if (profile == null || profile.Passives == null) return;
    foreach (var passive in profile.Passives)
        passive.OnAfterStrike(actor, allies, config, result, r, rng);
}

public static void HealAfterStrike(Combatant actor, RoleProfile profile, ..., float damage, CombatResolver r, ...)
{
    if (dodged || damage <= 0f || profile == null || profile.Passives == null) return;
    foreach (var passive in profile.Passives)
        passive.OnDamageDealt(actor, allies, damage, config, result, r, rng);
}
```

## Determinismo

- **Odin Serialization:** Polimórficas en inspector como lista editable
- **RNG:** Cada pasiva consume RNG según su lógica (PickAlly, LowestHpAlly); orden sincronizado con rol profile order
- **Marcas:** Sin rolls, determinista (AddMark es puro y usa CombatRng para reacciones internas)

## Cambios S46

**OnTurnStart → OnAfterStrike:**
- Renombrado para reflejar que corre POST-STRIKE, no pre-turno
- Misma firma de parámetros

**Relocación post-strike:**
- Antes: pre-strike (paso 5-6 en viejo TakeTurn)
- Ahora: post-strike (paso 10 en nuevo TakeTurn), después de GainAffinity

**Sin gates de Energy:**
- Eliminou `if (actor.Energy > 0)` de ShieldAllyPassive y HealLowestAllyOnHitPassive
- Ambas pasivas ahora aplican SIEMPRE (sin condición de recurso)

**Nueva pasiva:** `MarkRandomAllyPassive`
- Pasiva del Agresivo (rol)
- Marca aliado al azar con elemento de actor cada turno
- Reemplaza la antigua rama de "gasto energia + marcar" del Agresivo

## Cambios S41

**Nuevo parámetro `r` (CombatResolver):**
- Ambos hooks ahora reciben `CombatResolver r` para emitir eventos elementales
- `AddMark()` ahora recibe `r` (parámetro nuevo S41) para grabar eventos de marca/reacción

**Eventos emitidos:**
- `ElementEventKind.MarkApplied` — cuando se añade marca aliada (emitido dentro de AddMark)

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[RoleTableSO]] — serializadas en `RoleProfile.Passives` lista polimórfica
- [[CombatRoleHooks]] — invocador en `ApplyPassives()` (S46: renombrado de GrantShield) y `HealAfterStrike()`
- [[CombatResolver]] — receptor de `Record()` y `RecordElement()`
- [[CombatElements]] — llamada para `AddMark()`
- [[Combatant]] — actor/allies context, Shield/Hp mutados
- [[CombatManagerSO]], [[CombatResult]], [[CombatRng]]
- [[CombatTargeting]] — PickAlly, LowestHpAlly
