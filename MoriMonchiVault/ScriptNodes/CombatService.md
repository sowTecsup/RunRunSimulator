---
tags: [combat, core, deterministic, simulation]
---

# CombatService

**Ruta:** `Systems/Combat/CombatService.cs`

**Responsabilidad:** Servicio estático stateless que simula combate turn-based local, completamente determinista. Orquesta validación, simulación core pura (sin registry), construcción de records simétricos. Componedora de: `CombatRng` (inyectado por seed), `Combatant`, `CombatResolver`, `CombatStats`, `CombatEvolution`.

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

**StatusMarks Helper:** Nuevo método privado que contea efectos activos por `Kind` en el orden del enum, más Stun si `c.StunTurns > 0`:
```csharp
private static List<CombatStatusMark> StatusMarks(Combatant c)
{
    var counts = new Dictionary<ModifierEffectKind, int>();
    foreach (var a in c.Active)
        counts[a.Kind] = counts.TryGetValue(a.Kind, out var n) ? n + 1 : 1;
    
    var marks = new List<CombatStatusMark>();
    foreach (ModifierEffectKind kind in Enum.GetValues(typeof(ModifierEffectKind)))
        if (counts.TryGetValue(kind, out var stacks))
            marks.Add(new CombatStatusMark { Kind = kind, Stacks = stacks });
    
    if (c.StunTurns > 0)
        marks.Add(new CombatStatusMark { Kind = ModifierEffectKind.Stun, Stacks = c.StunTurns });
    
    return marks;
}
```

**RNG neutro:** No hay consumo de RNG adicional. El orden de rolls es idéntico a S33; se añade únicamente recolección de estado sin aleatoriedad.

## Header Actualizado (S32)

**Un único motor C# seedeado, corriendo en ambos clientes (local y async):** el servidor ya no simula, solo proporciona seed + snapshots. Ambos clientes corren `SimulateCore` con la misma seed, derivan idéntico `CombatRecord`, y lo persisten.

## Métodos Privados

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `TakeTurn(atk, def, config, result, round, resolver, rng)` | `bool` | Resuelve turno de atacante; retorna true si alguien llegó a 0 HP. Muta Combatant y acumula procs |
| `EmitTurn(result, round, atk, def, noAttack, damage, crit, defHp, procs)` | `void` | Crea CombatTurn y lo agrega a `result.Turns`; **S34** puebla StatusA/StatusB vía `StatusMarks()` |
| `TickStatuses(c, result, resolver)` | `void` | Aplica daño/curación por status activo (Poison/Burn/Regen); graba procs via `resolver.Record()` |
| `FireProcs(owner, opponent, trigger, result, resolver, roll, rng)` | `void` | Itera procs del tipo trigger, los aplica via `CombatProcEffect.Apply(ICombatContext)` |
| `RollProc(p, owner, result, rng)` | `bool` | Tira chance proc con rng inyectado, loguea roll |
| `BuildCombatant(dna, db, equipDb, isA)` | `Combatant` | Construye modelo de combatiente con stats efectivos y procs |
| `CollectProcs(dna, equipDb)` | `List<CombatProcEffect>` | Recolecta todos los procs del equipment equipado, ordenados por slot |
| `Snapshot(Combatant)` | `CombatFighterSnapshot` | **S33/S34** Extrae 6 stats finales + 4 tiers + ColorHex en un snapshot |
| `StatusMarks(Combatant)` | `List<CombatStatusMark>` | **S34** Contea efectos activos por `Kind`, retorna listas de `CombatStatusMark` |
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

## Flujo de Turno (TakeTurn)

**Orden determinista, por atacante cada round:**
1. `TickStatuses()` — aplica daño/curación de status activos (Poison/Burn/Regen), graba en procs
2. `FireProcs(..., Passive)` — dispara procs pasivos (siempre, sin roll)
3. Stun check — si stunned, decrementa, grant inmunidad si expira, graba, **skip resto de turno**
4. Decrementa `StunImmunityTurns` si > 0
5. Arm procs ofensivos (roll cada uno, acumula armed list)
6. Roll evasión (EVA * EvasionPerPoint)
7. Roll crit (CritChance + LCK * LuckCritPerPoint)
8. Aplica daño si hit (ATK * (crit ? CritMult : 1) * (1 - DEF_reduction))
9. `FireProcs(..., Defensive)` — procs defensivos del defensor (al recibir golpe)
10. `EmitTurn()` — registra todo en `CombatTurn` + `procs` + **StatusA/StatusB** ← S34

## Anti-Permastun & Stacking

- **Anti-permastun:** `CombatResolver.StunOpponent()` rechaza re-stun si ya stunned. Al despertar, otorga `StunImmunityTurns` (default 1 turno).
- **Stacking:** `CombatResolver.AddStatus()` permite múltiples instancias del mismo `Kind`; cada una con su contador independiente. Se aplican todas en paralelo en `TickStatuses()`.

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
    Speed     = c.Speed,
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

Extrae los 6 stats finales (post-equipment), 4 tiers y color al inicio de la simulación. No cambian durante los turnos — es snapshot momento-de-inicio.

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
- [[CombatFighterSnapshot]] — S33/S34, snapshot stats + tiers + color
- [[SynergyTableSO]], [[SynergyRule]], [[SynergyEffectBase]] — motor de sinergias (S32)
- [[GameEvents]] — (no dispara directo, GameManager/AsyncCombatService orquesta)

## Conexiones

**Entrada:**
- `CombatController.SimulateLocal()` → `Simulate(idA, idB, registry, db, config, equipDb, seed)`
- `AsyncCombatService.ApplyResult()` → `SimulateCore(dnaA, dnaB, db, config, equipDb, new CombatRng(seed))`

**Salida:**
- `CombatResult` — contiene `StatsA/StatsB` (S33/S34), `Turns` (list de `CombatTurn` con StatusA/StatusB S34), `Log`, outcome
- `CombatRecord` — poblado de `result.StatsA/StatsB` + tiers + color vía `BuildRecord()`, persistido en `CreatureDNA.CombatHistory` vía `GameManager`
- `CombatTurn.StatusA/StatusB` — consumido por `CombatVisualizerService` para renderizar estado visual

## Notas (S32-S34)

- **Backward compatible:** Contrato público `Simulate()` sin cambios; `StatusMarks()` es privado y RNG-neutral.
- **S33 Snapshot:** Helper `Snapshot()` extrae stats finales post-equipment en un `CombatFighterSnapshot` para persistencia + display en UI.
- **S34 Tiers + Color:** Snapshot ahora captura estado visual completo (evolución + color) para visualización offline.
- **S34 StatusMarks:** `StatusMarks()` contea efectos activos sin consumir RNG; orden de enum determinista.
- **Clases extraídas:** `Combatant`, `CombatResolver`, `CombatStats`, `CombatEvolution`, `EffectiveStats` ahora son públicas, reutilizables.
- **Determinismo total:** Cero UnityEngine.Random; todo vía `CombatRng` inyectado.
- **Procs:** Colectados en orden de slot (Body→Arm→Eye→Mouth) en `CollectProcs()` para determinismo.
- **Logging:** `result.Log` contiene trazas debug de rolls, daños, evasiones, statuses, sinergias, evolución, muerte.
- **Sinergias:** Integradas en `CombatResolver`; se disparan automáticamente al agregar status. Cap 8 iteraciones previene loops infinitos.
