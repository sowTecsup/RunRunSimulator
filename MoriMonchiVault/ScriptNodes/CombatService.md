---
tags: [combat, core, deterministic, simulation]
---

# CombatService

**Ruta:** `Systems/Combat/CombatService.cs`

**Responsabilidad:** Servicio estático stateless que simula combate turn-based local, completamente determinista. Orquesta validación, simulación core pura (sin registry), construcción de records simétricos. Componedora de: `CombatRng` (inyectado por seed), `Combatant`, `CombatResolver`, `CombatStats`, `CombatEvolution`. **S35:** Usa propiedades dinámicas de Combatant (EffSpeed, EffDefense, EffEvasion, LifestealPercent) que incorporan stacks de elementos en tiempo real.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Simulate(idA, idB, registry, db, config, equipDb, seed)` | `CombatResult` | Wrapper local: valida, genera CombatRng(seed), llama SimulateCore, construye records para ambos, retorna result |
| `SimulateCore(dnaA, dnaB, db, config, equipDb, rng)` | `CombatResult` | Puro determinista: sin registry, sin validación. Muta ambas DNAs. Retorna result con turnos |
| `BuildRecord(result, self, opponent, selfWasA, oppPlayerName, oppPlayerId, seed, date)` | `CombatRecord` | Construye un CombatRecord desde la perspectiva de `self` |

## Cambios S33

**Snapshot Poblado en SimulateCore():** Helper privado `Snapshot(Combatant)` extrae 6 stats post-equipment en un `CombatFighterSnapshot`. En SimulateCore:
```csharp
result.StatsA = Snapshot(A);
result.StatsB = Snapshot(B);
```

**BuildRecord copia por perspectiva:** Cuando `Simulate()` llama `BuildRecord(result, dnaA, dnaB, true, ...)`:
- `record.SelfStats = Snapshot(A)`
- `record.OpponentStats = Snapshot(B)`

Y `BuildRecord(result, dnaB, dnaA, false, ...)`:
- `record.SelfStats = Snapshot(B)`
- `record.OpponentStats = Snapshot(A)`

## Cambios S34

**Snapshot extendido:** `Snapshot(Combatant)` ahora también captura tiers de evolución y color:
```csharp
BodyTier  = (int)c.Dna.BodyTier,
ArmTier   = (int)c.Dna.ArmTier,
EyeTier   = (int)c.Dna.EyeTier,
MouthTier = (int)c.Dna.MouthTier,
ColorHex  = ColorUtility.ToHtmlStringRGB(c.Dna.BaseColor),
```

**EmitTurn poblado con StatusA/StatusB:** El método `EmitTurn()` ahora puebla el estado de efectos activos de ambos luchadores tras cada turno, llamando al helper `StatusMarks()`:
```csharp
StatusA = StatusMarks(atk.IsA ? atk : def),
StatusB = StatusMarks(atk.IsA ? def : atk),
```

**StatusMarks Helper:** Nuevo método privado que contea efectos activos por `Kind` en el orden del enum, más Stun si `c.StunTurns > 0`.

**RNG neutro:** No hay consumo de RNG adicional. El orden de rolls es idéntico a S33; se añade únicamente recolección de estado sin aleatoriedad.

## Cambios S35

**Stats dinámicos:** TakeTurn ahora usa propiedades de Combatant que incorporan stacks de elementos en tiempo real:
- **EffSpeed** (línea ~105) — Speed - suma de Static stacks (clamped a 0). Afecta orden de turno.
- **EffEvasion** (línea ~226) — Evasion + suma de Mist stacks. Afecta chance de esquiva.
- **EffDefense** (línea ~239) — Defense + suma de Steel stacks. Afecta mitigación de daño.
- **LifestealPercent** (línea ~251) — Suma de Lifesteal stacks / 100f, clamped a 1. Afecta curación post-golpe.

**Pulse + Regen mismo trato:** En `TickStatuses()`, Pulse se procesa igual que Regen — cura `c.Hp` en `a.Magnitude` por turno y se registra.

**Lifesteal post-strike:** Nuevo bloque post-golpe (línea ~251):
```csharp
if (!dodged && damage > 0f && atk.LifestealPercent > 0f)
{
    float steal = damage * atk.LifestealPercent;
    atk.Hp = Mathf.Min(atk.MaxHp, atk.Hp + steal);
    result.Log.Add($"    [Lifesteal] {atk.Name} +{steal:F1} → {atk.Hp:F1}");
    r.Record(ModifierEffectKind.Lifesteal, atk, steal);
}
```

Ocurre post-impacto, pre-procs defensivos, solo si golpea y daño > 0.

## Header Actualizado (S32)

**Un único motor C# seedeado, corriendo en ambos clientes (local y async):** el servidor ya no simula, solo proporciona seed + snapshots. Ambos clientes corren `SimulateCore` con la misma seed, derivan idéntico `CombatRecord`, y lo persisten.

## Métodos Privados

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `TakeTurn(atk, def, config, result, round, resolver, rng)` | `bool` | Resuelve turno de atacante; retorna true si alguien llegó a 0 HP. **S35:** Usa EffSpeed, EffDefense, EffEvasion, LifestealPercent en cálculos |
| `EmitTurn(result, round, atk, def, noAttack, damage, crit, defHp, procs)` | `void` | Crea CombatTurn y lo agrega a `result.Turns`; puebla StatusA/StatusB vía `StatusMarks()` |
| `TickStatuses(c, result, resolver)` | `void` | Aplica daño/curación por status activo (Poison/Burn/Regen/**Pulse** S35); graba procs via `resolver.Record()` |
| `FireProcs(owner, opponent, trigger, result, resolver, roll, rng)` | `void` | Itera procs del tipo trigger, los aplica via `CombatProcEffect.Apply(ICombatContext)` |
| `RollProc(p, owner, result, rng)` | `bool` | Tira chance proc con rng inyectado, loguea roll |
| `BuildCombatant(dna, db, equipDb, isA)` | `Combatant` | Construye modelo de combatiente con stats efectivos y procs (base + equipment) |
| `CollectProcs(dna, equipDb)` | `List<CombatProcEffect>` | Recolecta todos los procs del equipment equipado, ordenados por slot |
| `Snapshot(Combatant)` | `CombatFighterSnapshot` | Extrae 6 stats finales (base post-equipment, no dinámicos) + 4 tiers + ColorHex en un snapshot |
| `StatusMarks(Combatant)` | `List<CombatStatusMark>` | Contea efectos activos por `Kind`, retorna listas de `CombatStatusMark` |
| `Clip(id)` | `string` | Trunca ID a 14 chars para logging |

## Ciclo de Determinismo (S32)

1. **Local:** `CombatController.SimulateLocal(aID, bID)`
   - Genera `seed = Guid.NewGuid().GetHashCode()`
   - Llama `Simulate(aID, bID, ..., seed)`
   - Valida registry, builds combatants, llama `SimulateCore(..., new CombatRng(seed))`
   - Construye records, persistencia automática

2. **Async:** Cloud Code (JS) matchea y emite `CloudMatchBlob { Seed, CreatureJsonA, CreatureJsonB, ... }`
   - Cliente recibe blob, llama `ApplyResult()`
   - Deserializa snapshots, llama `SimulateCore(..., new CombatRng(blob.Seed))`
   - **Mismo seed + mismo DNA snapshots = resultado idéntico**
   - Construye record desde perspectiva propia

## Flujo de Turno (TakeTurn) — S35

**Orden determinista, por atacante cada round:**
1. `TickStatuses()` — aplica daño/curación de status activos (Poison/Burn/Regen/Pulse), graba en procs
2. `FireProcs(..., Passive)` — dispara procs pasivos (siempre, sin roll)
3. Stun check — si stunned, decrementa, grant inmunidad si expira, graba, **skip resto de turno**
4. Decrementa `StunImmunityTurns` si > 0
5. Arm procs ofensivos (roll cada uno, acumula armed list)
6. Roll evasión usando `def.EffEvasion` ← **S35 dynamic** (EVA + Mist stacks)
7. Roll crit (CritChance + LCK * LuckCritPerPoint)
8. Aplica daño si hit usando `def.EffDefense` ← **S35 dynamic** (DEF + Steel stacks)
9. **Lifesteal post-strike** ← **S35 new** (si hit y daño > 0): `steal = damage * atk.LifestealPercent`, graba Lifesteal proc
10. `FireProcs(..., Defensive)` — procs defensivos del defensor (al recibir golpe)
11. `EmitTurn()` — registra todo en `CombatTurn` + `procs` + `StatusA/StatusB`

## Orden de turno con EffSpeed — S35

```csharp
bool aFirst = A.EffSpeed > B.EffSpeed ||
              (Mathf.Approximately(A.EffSpeed, B.EffSpeed) && rng.NextFloat() < 0.5f);
```

Si A tiene Static×2 (−2 SPD), su `EffSpeed` disminuye, pudiendo perder el orden de turno dinámicamente. **No hay precálculo:** el orden se re-evalúa cada ronda según estado actual.

## Evasión con EffEvasion — S35

```csharp
float evaChance = def.EffEvasion * config.EvasionPerPoint;
float evaRoll   = rng.NextFloat();
bool  dodged    = evaRoll < evaChance;
```

Si defensor tiene Mist×2 (+2 EVA), la chance de esquiva aumenta dinámicamente.

## Defensa con EffDefense — S35

```csharp
float reduction = Mathf.Clamp01(def.EffDefense * config.DefenseReductionPerPoint);
damage          = raw * (1f - reduction);
```

Si defensor tiene Steel×2 (+2 DEF), la mitigación aumenta dinámicamente.

## Lifesteal post-strike — S35

```csharp
r.BeforeStrike = false;
if (!dodged && damage > 0f && atk.LifestealPercent > 0f)
{
    float steal = damage * atk.LifestealPercent;
    atk.Hp = Mathf.Min(atk.MaxHp, atk.Hp + steal);
    result.Log.Add($"    [Lifesteal] {atk.Name} +{steal:F1} → {atk.Hp:F1}");
    r.Record(ModifierEffectKind.Lifesteal, atk, steal);
}
```

Post-impacto (tras establecer `def.Hp` post-daño). Solo si:
- No fue esquivado (`!dodged`)
- Daño > 0
- `LifestealPercent > 0` (hay stacks de Lifesteal)

El proc se registra vía `Resolver.Record()` con `TargetStatusAfter`.

## TickStatuses con Pulse — S35

```csharp
case ModifierEffectKind.Regen:
case ModifierEffectKind.Pulse:  // S35 new
    c.Hp = Mathf.Min(c.MaxHp, c.Hp + a.Magnitude);
    result.Log.Add($"    [{a.Kind}] {c.Name} regenerates {a.Magnitude} HP → {c.Hp:F1} ({left}t left)");
    r.Record(a.Kind, c, a.Magnitude);
    break;
```

Pulse se procesa idéntico a Regen: cura cada turno en el monto de `Magnitude`. Se registra como proc Pulse.

## Anti-Permastun & Stacking

- **Anti-permastun:** `CombatResolver.StunOpponent()` rechaza re-stun si ya stunned. Al despertar, otorga `StunImmunityTurns` (default 1 turno).
- **Stacking:** `CombatResolver.AddStatus()` permite múltiples instancias del mismo `Kind`; cada una con su contador independiente. Se aplican todas en paralelo en `TickStatuses()`, y sus Magnitudes se suman en propiedades dinámicas.

## Sinergias (S32)

```csharp
// En SimulateCore:
var resolver = new CombatResolver { Result = result, Synergies = config.Synergies };
```

La tabla de sinergias (`SynergyTableSO`) se pasa en la construcción del resolver. Cada vez que `CombatResolver.AddStatus()` agrega un efecto, llama `CheckSynergies()` automáticamente, que detona recetas satisfechas y aplica `SynergyEffectBase` polimórficamente.

## Snapshot Helper — S33 + S34

```csharp
private static CombatFighterSnapshot Snapshot(Combatant c) => new CombatFighterSnapshot
{
    MaxHp     = c.MaxHp,
    Attack    = c.Attack,
    Speed     = c.Speed,                                    // Base, no dinámico
    Defense   = c.Defense,
    Luck      = c.Luck,
    Evasion   = c.Evasion,
    BodyTier  = (int)c.Dna.BodyTier,
    ArmTier   = (int)c.Dna.ArmTier,
    EyeTier   = (int)c.Dna.EyeTier,
    MouthTier = (int)c.Dna.MouthTier,
    ColorHex  = ColorUtility.ToHtmlStringRGB(c.Dna.BaseColor),
};
```

**Importante (S35):** Snapshot captura stats BASE post-equipment, no dinámicos. Las propiedades dinámicas (EffSpeed, EffDefense, etc.) son caídas en tiempo real durante turnos y NO se graban en el snapshot. El snapshot es punto-en-tiempo al inicio de combate.

## Vinculado a

- [[Index/03 - Combat]]
- [[CreatureDNA]] — fuente de verdad, se muta si gana/muere
- [[CreatureDatabaseSO]] — resuelve partes por ID
- [[CombatManagerSO]] — config (MaxRounds, CritChance, DEF reduction, StunImmunityTurns, **Synergies**)
- [[EquipmentDatabaseSO]] — resuelve items equipados → procs
- [[EquipmentStats]] — aplica mods de equipment
- [[CombatRng]] — RNG inyectado, determinista
- [[Combatant]], [[CombatResolver]], [[CombatStats]], [[CombatEvolution]], [[EffectiveStats]] — clases extraídas
- [[CombatRecord]], [[CombatTurn]], [[CombatProcEvent]], [[CombatStatusMark]] — DTO salida
- [[CombatFighterSnapshot]] — snapshot stats base + tiers + color
- [[SynergyTableSO]], [[SynergyRule]], [[SynergyEffectBase]] — motor de sinergias (S32)
- [[GameEvents]] — (no dispara directo, GameManager/AsyncCombatService orquesta)

## Conexiones

**Entrada:**
- `CombatController.SimulateLocal()` → `Simulate(idA, idB, registry, db, config, equipDb, seed)`
- `AsyncCombatService.ApplyResult()` → `SimulateCore(dnaA, dnaB, db, config, equipDb, new CombatRng(seed))`

**Salida:**
- `CombatResult` — contiene `StatsA/StatsB` (snapshot base), `Turns` (list de `CombatTurn` con StatusA/StatusB), `Log`, outcome
- `CombatRecord` — poblado de `result.StatsA/StatsB` + tiers + color vía `BuildRecord()`, persistido en `CreatureDNA.CombatHistory` vía `GameManager`
- `CombatTurn.StatusA/StatusB` — consumido por `CombatVisualizerService` para renderizar estado visual

## Notas (S32-S35)

- **Backward compatible:** Contrato público `Simulate()` sin cambios; stats dinámicos son propiedades de Combatant, transparentes.
- **S33 Snapshot:** Helper `Snapshot()` extrae stats finales post-equipment en un `CombatFighterSnapshot` para persistencia + display en UI.
- **S34 Tiers + Color:** Snapshot ahora captura estado visual completo (evolución + color) para visualización offline.
- **S34 StatusMarks:** `StatusMarks()` contea efectos activos sin consumir RNG; orden de enum determinista.
- **S35 Dynamic Properties:** Combatant.EffSpeed/EffDefense/EffEvasion/LifestealPercent se recalculan on-demand cada turno, incorporando stacks activos. NO son precalculados, permiten que elementos dinámicos afecten combate en tiempo real.
- **S35 TickStatuses:** Pulse procesado como Regen, idéntica lógica de curación.
- **S35 Lifesteal:** Nuevo bloque post-strike, pre-procs defensivos. Solo si golpea y daño > 0.
- **Clases extraídas:** `Combatant`, `CombatResolver`, `CombatStats`, `CombatEvolution`, `EffectiveStats` ahora son públicas, reutilizables.
- **Determinismo total:** Cero UnityEngine.Random; todo vía `CombatRng` inyectado.
- **Procs:** Colectados en orden de slot (Body→Arm→Eye→Mouth) en `CollectProcs()` para determinismo.
- **Logging:** `result.Log` contiene trazas debug de rolls, daños, evasiones, statuses, sinergias, evolución, muerte.
- **Sinergias:** Integradas en `CombatResolver`; se disparan automáticamente al agregar status. Cap 8 iteraciones previene loops infinitos.
