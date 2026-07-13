---
tags: [script, combat, roles, effects, base-class, elements]
---

# RolePassiveBase.cs

**Ruta:** `Data/Combat/RolePassiveBase.cs`

**Responsabilidad:** Clase abstracta base para efectos pasivos de rol serializables en listas polimórficas. **S40:** Abstracción de efectos pre-turno (OnTurnStart) y post-golpe (OnDamageDealt) heredables, eliminando branches enum-based del antes. Dos hooks: `OnTurnStart()` (ejecutado cada turno pre-ataque por `CombatRoleHooks.GrantShield()`) y `OnDamageDealt()` (post-strike por `CombatRoleHooks.HealAfterStrike()`). Soporte polimórfico vía `[Serializable]` + Odin Inspector. **S41:** Parámetro `r` (CombatResolver) nuevo para emitir eventos elementales de energía gasto (EnergySpent) y marca aliada (MarkApplied vía AddMark).

## Métodos Abstractos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `OnTurnStart(actor, allies, config, result, r, rng)` | `void` | **S41 SIG CAMBIÓ** Ejecutado cada turno pre-ataque. Ej: shield aliado, marcar con elemento. Parámetro `r` nuevo S41 para emitir eventos elementales. |
| `OnDamageDealt(actor, allies, damage, config, result, r, rng)` | `void` | **S41 SIG CAMBIÓ** Ejecutado post-strike si damage > 0. Ej: curar aliado más débil. Parámetro `r` nuevo S41 para emitir eventos elementales. |
| `Summary()` | `string` | Retorna descripción UI del efecto (ej: "Escuda +1 por turno a un aliado") |

## Implementaciones Concretas

### ShieldAllyPassive

**Descripción:** Cada turno, elige aliado al azar y otorga `AmountPerTurn` de escudo. Si actor tiene Energía, la decrementa y marca el aliado con elemento de actor (aliada source).

**Campos:**
- `AmountPerTurn` (float, MinValue 0, LabelText "Shield per turn") — escudo otorgado, defecto 1.0

**OnTurnStart (S41 FIRMA CAMBIÓ):**
- Pick aliado vivo al azar: `CombatTargeting.PickAlly(allies, rng)`
- `ally.Shield += AmountPerTurn`
- Log: `"{actor.Name} escuda a {ally.Name} +{AmountPerTurn}"`
- Record en resolver: `r.Record(ModifierEffectKind.Shield, ally, AmountPerTurn)`
- Si actor.Energy > 0:
  - Decrementa: `actor.Energy--`
  - Graba evento: `r.RecordElement(ElementEventKind.EnergySpent, actor, amount: actor.Energy)` **(S41 NEW)**
  - Marca aliado: `CombatElements.AddMark(ally, actor.Element, true, actor, config, result, r, rng)` (parámetro `r` nuevo S41)

**Consumo RNG:** `PickAlly()` consume si hay múltiples candidatos

### HealLowestAllyOnHitPassive

**Descripción:** Post-strike, elige aliado con menor HP y lo cura por porcentaje del daño infligido. Si actor tiene Energía, la decrementa y marca ese aliado con elemento de actor (aliada source).

**Campos:**
- `PercentOfDamage` (float, PropertyRange 0–1, LabelText "% of damage") — defecto 0.5 (50%)

**OnDamageDealt (S41 FIRMA CAMBIÓ):**
- Pick aliado más débil: `CombatTargeting.LowestHpAlly(allies)`
- Si existe:
  - `float heal = damage * PercentOfDamage`
  - `ally.Hp = min(ally.MaxHp, ally.Hp + heal)`
  - Log: `"{actor.Name} cura a {ally.Name} +{heal}"`
  - Record: `r.Record(ModifierEffectKind.Heal, ally, heal)`
  - Si actor.Energy > 0:
    - Decrementa: `actor.Energy--`
    - Graba evento: `r.RecordElement(ElementEventKind.EnergySpent, actor, amount: actor.Energy)` **(S41 NEW)**
    - Marca aliado: `CombatElements.AddMark(ally, actor.Element, true, actor, config, result, r, rng)` (parámetro `r` nuevo S41)

**Consumo RNG:** ninguno (LowestHpAlly es determinista, sin roll)

## Flujo de Integración (S40 + S41)

**En `RoleTableSO.RoleProfile`:**
```csharp
public List<RolePassiveBase> Passives = new List<RolePassiveBase>();
```

**En `CombatService.TakeTurn()` (S41 FIRMA CAMBIÓ):**
```csharp
var profile = config.Roles != null ? config.Roles.GetProfile(actor.Role) : null;
CombatRoleHooks.GrantShield(actor, profile, allies, config, result, r, rng);  // parámetro r nuevo
CombatRoleHooks.HealAfterStrike(actor, profile, allies, strike.Dodged, strike.Damage, config, result, r, rng);  // parámetro r nuevo
```

**En `CombatRoleHooks` (S41):**
```csharp
public static void GrantShield(Combatant actor, RoleProfile profile, ..., CombatResolver r, ...)
{
    if (profile == null || profile.Passives == null) return;
    foreach (var passive in profile.Passives)
        passive.OnTurnStart(actor, allies, config, result, r, rng);  // parámetro r nuevo
}

public static void HealAfterStrike(Combatant actor, RoleProfile profile, ..., float damage, CombatResolver r, ...)
{
    if (dodged || damage <= 0f || profile == null || profile.Passives == null) return;
    foreach (var passive in profile.Passives)
        passive.OnDamageDealt(actor, allies, damage, config, result, r, rng);  // parámetro r nuevo
}
```

## Determinismo

- **Odin Serialization:** Polimórficas en inspector como lista editable (Odin maneja [SerializableField])
- **RNG:** Cada pasiva consume RNG según su lógica (PickAlly, etc); orden sincronizado con rol profile order
- **Energía:** Si actor.Energy > 0, decrementa y marca — determinista sin roll (S41: eventos grabados en orden)

## Cambios S41

**Nuevo parámetro `r` (CombatResolver):**
- Ambos hooks ahora reciben `CombatResolver r` para emitir eventos elementales
- `OnTurnStart` y `OnDamageDealt` emiten `ElementEventKind.EnergySpent` cuando actor gasta energía
- `AddMark()` ahora recibe `r` (parámetro nuevo S41) para grabar eventos de marca/reacción

**Eventos emitidos (S41):**
- `ElementEventKind.EnergySpent` — cuando actor decrementa Energy
- `ElementEventKind.MarkApplied` — cuando se añade marca aliada (emitido dentro de AddMark)

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[RoleTableSO]] — serializadas en `RoleProfile.Passives` lista polimórfica
- [[CombatRoleHooks]] — invocador en `GrantShield()` y `HealAfterStrike()` (parámetro `r` nuevo S41)
- [[CombatResolver]] — receptor de `Record()` y `RecordElement()` (S41 NEW)
- [[CombatElements]] — llamada para `AddMark()` si Energy gasto (parámetro `r` nuevo S41)
- [[Combatant]] — actor/allies context, Shield/Hp/Energy mutados
- [[CombatManagerSO]], [[CombatResult]], [[CombatRng]] — contexto
- [[CombatTargeting]] — PickAlly, LowestHpAlly
