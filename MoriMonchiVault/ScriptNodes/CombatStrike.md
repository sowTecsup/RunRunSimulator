---
tags: [script, combat, strike, damage, elements]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatStrike.cs

**Ruta:** `Systems/Combat/CombatStrike.cs`

**Responsabilidad:** Mediador estático que ejecuta un golpe básico: roll de evasión (con bonus Vaporizado), roll de crítico (con bonus GolpePreciso), cálculo de daño (con Debilidad anulando DEF, Boiling amplificando, Sudden Death multiplicando), absorción de escudo, reflejos (Charcoal), marca elemental. **S40:** Extraída de `CombatService.TakeTurn()` toda la matemática de golpe. Retorna `StrikeOutcome` con flags Dodged/Crit y valores finales de HP/Shield. Log consumido por `CombatResult`. Determinista: todos los rolls vía `CombatRng` inyectado. **S41:** Parámetro `r` (CombatResolver) nuevo para grabar eventos elementales de consumos de estado (Vaporizado, GolpePreciso, Debilidad, Boiling, Charcoal) y marca elemental. **S62:** Sudden Death multiplier consultado vía `config.SuddenDeathMultiplier(r.Round)` — daño se multiplica post-DEF, pre-Boiling, con log de marca "MSx{n}".

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Execute(actor, target, config, result, r, rng)` | `StrikeOutcome` | **S41 SIG CAMBIÓ** Resuelve un golpe completo: evasión → crit → daño → absorción escudo → reflejo → marca. Retorna outcome con flags y valores finales. Log agregado a result. Parámetro `r` nuevo S41 para emitir eventos elementales. **S62:** Sudden Death multiplier aplicado post-DEF reduction. |

## Estructura: StrikeOutcome

```csharp
public class StrikeOutcome
{
    public bool  Dodged;
    public bool  Crit;
    public float Damage;
    public float DefenderHpAfter;
    public float DefenderShieldAfter;
}
```

## Flujo de Execute (S40 + S41 + S62)

1. **Evasión:** `evaChance = target.EffEvasion * config.EvasionPerPoint`
   - Suma bonus `Vaporizado` si target lo tiene (vía `config.Elements.StatePercent(ElementalState.Vaporizado)`)
   - Roll `rng.NextFloat()`; si < `evaChance`, esquiva
   - Si dodged y tiene Vaporizado, consume estado, log, `r.RecordElement(ElementEventKind.StateConsumed, target, state: ElementalState.Vaporizado)` **(S41 NEW)**

2. **Crítico (si no dodged):** `critChance = config.CritChance + actor.Luck * config.LuckCritPerPoint`
   - Suma bonus `GolpePreciso` si actor lo tiene
   - Roll `rng.NextFloat()` ; si < `critChance`, crit
   - Si crit y tiene GolpePreciso, consume estado, log, `r.RecordElement(ElementEventKind.StateConsumed, actor, state: ElementalState.GolpePreciso)` **(S41 NEW)**

3. **Daño (si no dodged):**
   - Raw = `actor.Attack * (crit ? config.CritMultiplier : 1f)`
   - Reducción = `target.EffDefense * config.DefenseReductionPerPoint`, clamped [0,1]
   - Si target tiene Debilidad:
     - Reducción = 0 (ignora DEF)
     - Consume estado, log
     - `r.RecordElement(ElementEventKind.StateConsumed, target, state: ElementalState.Debilidad)` **(S41 NEW)**
   - Daño = `raw * (1 - reducción)`
   - **Muerte Súbita (S62):** `suddenDeath = config.SuddenDeathMultiplier(r.Round)` (consulta tabla en CombatManagerSO, clamp a 1.0 si antes del round crítico)
     - Si `suddenDeath > 1f`, multiplica daño: `damage *= suddenDeath`
     - Log marca con "MSx{n}" (ej: "MSx1.4")
   - Si target tiene Boiling:
     - Daño *= (1 + `config.Elements.StatePercent(ElementalState.Boiling)`)
     - Log daño amplificado
     - Consume estado
     - `r.RecordElement(ElementEventKind.StateConsumed, target, state: ElementalState.Boiling, amount: damage)` **(S41 NEW)**

4. **Escudo:**
   - `absorbed = min(target.Shield, damage)`
   - `target.Shield -= absorbed`
   - `target.Hp -= (damage - absorbed)`

5. **Reflejo (Charcoal, S41 GRABA):**
   - Si target tiene Charcoal y damage > 0:
     - `reflect = damage * config.Elements.StatePercent(ElementalState.Charcoal)`
     - `actor.Hp -= reflect`
     - Log reflejo
     - Consume estado
     - `r.RecordElement(ElementEventKind.StateConsumed, target, state: ElementalState.Charcoal, amount: reflect)` **(S41 NEW)**
     - `r.RecordElement(ElementEventKind.Damage, actor, amount: reflect)` **(S41 NEW)** — daño al atacante

6. **Marca elemental (si hit + damage > 0, S41 FIRMA CAMBIÓ):**
   - Delega a `CombatElements.AddMark(target, actor.Element, false, actor, config, result, r, rng)`
   - Parámetro `r` nuevo S41 para que AddMark grabe eventos de marca/reacción

7. **Log:**
   - Dodged: `"[dir] DODGE!  ... (eva X% vs Y%)"`
   - Hit: `"[dir] CRIT? dmg:X MSx{n}? (escudo...)?  ... (eva X% vs Y% · crit X% vs Y%)"`
   - El marker "MSx{n}" solo aparece si `suddenDeath > 1.0`

## Determinismo

- **RNG consumption order:** evasión roll, crit roll (solo si no dodged)
- **Estado consumo:** sincronizado con rolls y daño; eventos grabados en orden de consumo (S41)
- **Configuración:** todos los coeficientes vía `config` (ElementTableSO para magnitudes de estado, CombatManagerSO para Sudden Death)
- **Backward compatible:** events elementales nuevos no afectan order de consumo RNG (estructura aditiva)

## Cambios S62

**Sudden Death multiplier:**
- Nueva línea: `suddenDeath = config.SuddenDeathMultiplier(r.Round)`
- Consulta tabla en `CombatManagerSO.SuddenDeathMultipliers` (default {1.4, 1.8, 2.2, 2.6, 3})
- Aplicado post-DEF reduction, pre-Boiling amplification
- Si > 1.0, daño se multiplica y se loga con marker "MSx{valor}"
- `config.SuddenDeathStartRound` define cuándo comienza (default 6; round 1-5 no tienen multiplicador)

## Cambios S41

**Nuevo parámetro `r` (CombatResolver):**
- Cada consumo de estado emite `RecordElement(ElementEventKind.StateConsumed, ...)`
- Reflejo Charcoal emite dos eventos: uno para el consumo del estado, otro para el daño al atacante
- Marca enemiga se graba dentro de AddMark (parámetro `r` pasado a ese método)

**Eventos emitidos:**
- `ElementEventKind.StateConsumed` — Vaporizado, GolpePreciso, Debilidad, Boiling, Charcoal
- `ElementEventKind.Damage` — reflejo de Charcoal al atacante
- `ElementEventKind.MarkApplied` — marca enemiga (emitido dentro de AddMark)

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

- [[CombatService]] — `TakeTurn()` llama `Execute()` y captura `StrikeOutcome` (parámetro `r` nuevo S41)
- [[CombatElements]] — `AddMark()` llamado post-strike si damage > 0 (parámetro `r` nuevo S41)
- [[CombatResolver]] — receptor de `RecordElement()` (S41 NEW), consulta `r.Round` (S62)
- [[CombatManagerSO]] — `config.SuddenDeathMultiplier(r.Round)` para multiplicador por ronda (S62), `config.EvasionPerPoint`, `config.LuckCritPerPoint`, `config.CritChance`, `config.CritMultiplier`, `config.Elements` (StatePercent)
- [[Combatant]] — actor/target, propiedades Attack/EffDefense/EffEvasion/Luck/Element/Shield/Hp/States
- [[CombatResult]] — agregado de log
- [[CombatRng]] — consumo de rolls (evasión, crit)
