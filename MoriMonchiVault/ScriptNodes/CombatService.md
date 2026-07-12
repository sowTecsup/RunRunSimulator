---
tags: [combat, core, deterministic, simulation]
---

# CombatService

**Ruta:** `Systems/Combat/CombatService.cs`

**Responsabilidad:** Servicio estático stateless que simula combate turn-based **3v3 team-based** (tolerancia 1..3 por lado), completamente determinista. Orquesta validación de equipos, simulación core pura (sin registry), construcción de records simétricos. Componedora de: `CombatRng` (inyectado por seed), `Combatant`, `CombatResolver`, `CombatStats`, `CombatEvolution`, `CombatTargeting`, `RoleWorldProfileSO`, **`CombatElements` (S39)**. **S35:** Propiedades dinámicas de Combatant (EffSpeed, EffDefense, EffEvasion, LifestealPercent). **S37:** Equipos 3v3, filas (2-3-2 grid), efectos de rol (Protector escudo, Agresivo backline hit, Empático heal). **S39:** Marcas y reacciones elementales vía `CombatElements.AddMark()`.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Simulate(idsA, idsB, rowsA, rowsB, registry, db, config, equipDb, seed)` | `CombatResult` | **S37** Wrapper 3v3: valida equipos (size match, IDs vivos, busy check), resuelve filas, genera CombatRng(seed), llama SimulateCore, construye records para cada unit, retorna result |
| `SimulateCore(dnasA, dnasB, rowsA, rowsB, db, config, equipDb, rng)` | `CombatResult` | **S37** Puro determinista: sin registry. Muta ambas listas de DNAs (EvolvedSlot, IsDead). Retorna result con turnos simétricos |
| `BuildRecord(result, selfDnas, oppDnas, self, isSelfTeamA, oppPlayerName, oppPlayerId, seed, date)` | `CombatRecord` | **S37** Construye un CombatRecord desde perspectiva de `self` (una DNA de un equipo ganador/perdedor) |

## Cambios S33

**Snapshot Poblado en SimulateCore():** Helper privado `Snapshot(Combatant)` extrae 6 stats post-equipment en un `CombatFighterSnapshot`. En SimulateCore:
```csharp
result.StatsA = Snapshot(A);
result.StatsB = Snapshot(B);
```

**BuildRecord copia por perspectiva:** Cuando `Simulate()` llama `BuildRecord(result, ...)`:
- `record.SelfStats = Snapshot(self)`
- `record.OpponentStats = Snapshot(opponent first)`

## Cambios S34

**Snapshot extendido:** `Snapshot(Combatant)` ahora también captura tiers de evolución, color, nombre, rol, fila:
```csharp
BodyTier  = (int)c.Dna.BodyTier,
ArmTier   = (int)c.Dna.ArmTier,
EyeTier   = (int)c.Dna.EyeTier,
MouthTier = (int)c.Dna.MouthTier,
ColorHex  = ColorUtility.ToHtmlStringRGB(c.Dna.BaseColor),
Name      = c.Dna.CustomName,
Role      = c.Dna.Role,
Row       = (int)c.Row,
```

**EmitTurn poblado con StatusA/StatusB:** El método `EmitTurn()` puebla el estado de efectos activos tras cada turno (1v1 legacy) o TeamStateA/B (3v3).

**StatusMarks Helper:** Contea efectos activos por `Kind` en orden de enum, más Stun si `c.StunTurns > 0`.

**RNG neutro:** No hay consumo de RNG adicional. Se añade solo recolección de estado.

## Cambios S35

**Stats dinámicos:** TakeTurn usa propiedades de Combatant que incorporan stacks de elementos en tiempo real:
- **EffSpeed** — Speed - suma de Static stacks (clamped a 0). Afecta orden de turno.
- **EffEvasion** — Evasion + suma de Mist stacks. Afecta chance de esquiva.
- **EffDefense** — Defense + suma de Steel stacks. Afecta mitigación de daño.
- **LifestealPercent** — Suma de Lifesteal stacks / 100f, clamped a 1. Afecta curación post-golpe.

**Pulse + Regen mismo trato:** En `TickStatuses()`, Pulse se procesa igual que Regen.

**Lifesteal post-strike:** Nuevo bloque post-golpe (post-impacto, pre-procs defensivos).

## Cambios S37

**RESHAPE COMPLETO a 3v3 team-based:**

### Método Simulate() — Firma Cambiada

```csharp
public static CombatResult Simulate(
    List<string>        idsA,
    List<string>        idsB,
    List<int>           rowsA,
    List<int>           rowsB,
    CreatureRegistrySO  registry,
    CreatureDatabaseSO  db,
    CombatManagerSO     config,
    EquipmentDatabaseSO equipDb,
    int                 seed)
```

**Validaciones nuevas:**
- Ambos equipos deben tener el mismo tamaño (1-3)
- Ningún ID duplicado entre A y B
- Todas las DNAs deben estar vivas, no busy, con fights restantes

**Resolución de filas:** Si `rowsA == null`, usa `DefaultLineup` (2-3-2: Front, Front, Mid). Luego construye `Combatant` list con Row asignado.

### Método SimulateCore() — Firma Cambiada

```csharp
public static CombatResult SimulateCore(
    List<CreatureDNA>   dnasA,
    List<CreatureDNA>   dnasB,
    List<int>           rowsA,
    List<int>           rowsB,
    CreatureDatabaseSO  db,
    CombatManagerSO     config,
    EquipmentDatabaseSO equipDb,
    CombatRng           rng)
```

**Construcción de equipos:**
1. Itera dnasA con rowsA → crea `Combatant` list teamA con Row + Index (0..2)
2. Itera dnasB con rowsB → crea `Combatant` list teamB
3. Popula `result.TeamA` = snapshots de teamA, `result.TeamB` = snapshots de teamB

**Orden de turnos S37 (determinista):**
1. **Speed Tiebreak Roll ONCE PER TEAM:** Para cada unit vivo en A y B, roll tiebreak (1 call a rng.NextFloat() por unit, total 2×n = 2..6 rolls)
2. **Ordenar:** Units se ordenan por (EffSpeed desc, tiebreak desc, team A-before-B, Index asc)
3. **Iterar:** Cada unit toma un turno en ese orden

### Flujo de Turno (TakeTurn) — S37

**Por atacante:**
1. `TickStatuses()` — aplica daño/curación de status (Poison/Burn/Regen/Pulse)
2. `FireProcs(..., Passive)` — procs pasivos
3. Stun check — si stunned, skip; decrementa; grant inmunidad si expira
4. **Role: Protector** — pick aliado, otorga Shield (no roll si ShieldPerTurn=0)
5. **Role: Agresivo** — 50% chance hit backline vs. frontline (roll rng.NextFloat())
6. Elige objetivo: `CombatTargeting.PickFrontTarget()` o `PickBacklineTarget()` según roll
7. Fire procs ofensivos (roll cada uno)
8. Roll evasión usando `def.EffEvasion`
9. Roll crit
10. Aplica daño si hit usando `def.EffDefense`
11. **Role: Empático** — si golpea, cura aliado más débil (no roll): `LowestHpAlly()` recibe `damage * HealPercentOfDamage`
12. Lifesteal post-strike (si hit y daño > 0)
13. Fire procs defensivos del defensor
14. `EmitTurn()` — registra turno + TeamStateA/B

## Cambios S39

**Sistema de marcas elementales:**

En el flujo de turno S37, el paso 13 (Fire procs defensivos) se extiende con:

```csharp
// (13) elemental mark — if the strike connected (not dodged, damage>0),
// mark the target with the attacker's Element as an enemy-sourced mark;
```

**Integración:**
- Después de que un golpe conecta (damage > 0), se llama `CombatElements.AddMark(target, attacker.Dna.Element, false, attacker, config, result, rng)`
- El método AddMark maneja la lógica: impide duplicados (mismo Element+fuente), detecta pares de elementos distintos y detona reacciones
- Reacciones instantáneas (Cleanse, OverGrow, Leech, PisoTierra) se resuelven inmediato
- Reacciones armadas (Energizado, Vaporizado, etc.) se agregan a Combatant.States como estados single-use

**Determinismo:** Todos los rolls de elemento vía CombatElements.ReactionFor() y ApplyState() son deterministas (sin rolls) o usan CombatRng inyectado (PisoTierra random mark removal).

**Header del archivo actualizado:** Documenta que el paso 13 incluye marca elemental y reacción.

## Métodos Privados

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `ValidateTeam(ids, registry, config, outDnas)` | `bool` | **S37** Valida team size, IDs vivos, no busy, fights restantes |
| `ResolveRows(count, rows, outResolved)` | `bool` | **S37** Valida/resuelve fila list; default 2-3-2 si null |
| `TakeTurn(atk, myTeam, oppTeam, config, result, round, resolver, rng, teamA, teamB)` | `void` | **S37/S39** Resuelve turno: role logic, targeting, damage, status, procs, marcas elementales. |
| `EmitTurn(result, round, atk, def, myTeamA, myTeamB, noAttack, damage, crit, shieldAfter, procs)` | `void` | **S37** Crea CombatTurn con indices, TeamStateA/B |
| `TickStatuses(c, result, resolver)` | `void` | Aplica daño/curación por status activo |
| `FireProcs(owner, opponent, trigger, result, resolver, roll, rng)` | `void` | Itera procs del tipo trigger, aplica |
| `RollProc(p, owner, result, rng)` | `bool` | Tira chance proc |
| `BuildCombatant(dna, db, equipDb, isA, index, row, roles)` | `Combatant` | **S37/S39** Construye modelo con Row e Index; aplica RoleTableSO mods |
| `CollectProcs(dna, equipDb)` | `List<CombatProcEffect>` | Recolecta procs de equipment |
| `Snapshot(Combatant)` | `CombatFighterSnapshot` | Extrae stats + tiers + color + nombre + role + row |
| `StatusMarks(Combatant)` | `List<CombatStatusMark>` | Contea efectos activos |
| `Clip(id)` | `string` | Trunca ID a 14 chars para logging |

## Ciclo de Determinismo (S32 + S37 + S39)

1. **Local:** `CombatController.SimulateLocal(idsA, idsB, rowsA, rowsB)`
   - Genera `seed = Guid.NewGuid().GetHashCode()`
   - Llama `Simulate(idsA, idsB, rowsA, rowsB, ..., seed)`
   - Valida registry, construye DNAs, construye Combatants, llama `SimulateCore(..., new CombatRng(seed))`
   - Construye records para cada unit
   - Persiste automático via GameEvents.RegistryChanged

2. **Async:** Cloud Code (JS v2) matchea y emite `CloudMatchBlob { Seed, CreatureJsonsA, CreatureJsonsB, RowsA, RowsB, ... }`
   - Cliente recibe blob
   - Deserializa DNAs desde JSON snapshot, llama `SimulateCore(..., new CombatRng(blob.Seed), blob.RowsA, blob.RowsB)`
   - **Mismo seed + mismo DNA snapshots + mismo rows = resultado idéntico**
   - Construye record desde perspectiva propia vía `BuildRecord(result, selfDnas, oppDnas, self, ...)`

## Flujo de Turno Completo (S37/S39)

```
Per round per unit in turn order (sorted by EffSpeed):
  1. TickStatuses() — apply Poison/Burn/Regen/Pulse damage/heal
  2. FireProcs(Passive)
  3. Stun check + wake-up immunity
  4. [Role Protector] PickAlly() + ShieldTarget(ally, profile.ShieldPerTurn)
  5. [Role Agresivo] if (rng.NextFloat() < 0.5f) target = PickBacklineTarget() else PickFrontTarget()
  6. [Default] target = PickFrontTarget()
  7. FireProcs(Offensive) — roll each, build armed list
  8. Roll evasion using def.EffEvasion
  9. Roll crit
  10. Apply damage (Shield absorbs first) using def.EffDefense
  11. [Role Empatico] LowestHpAlly(myTeam).Hp += damage * profile.HealPercentOfDamage
  12. Lifesteal post-strike
  13. FireProcs(Defensive) from defender
  14. [S39] CombatElements.AddMark(target, attacker.Element, false, attacker, config, result, rng)
  15. EmitTurn() → record AttackerIndex/DefenderIndex, DefenderShieldAfter, TeamStateA/B
  16. If either unit or team dead, break loop or flag round end
```

## Orden de Rol (S37)

**Protector (Tanque):** Cada turno, pre-ataque, pick aliado random vivo → `resolver.ShieldTarget(ally, ShieldPerTurn)`. Sin roll (si ShieldPerTurn = 0, noop).

**Agresivo (Pegador):** Pre-targeting, roll `rng.NextFloat() < BacklineHitChance (0.5)`; si yes, `CombatTargeting.PickBacklineTarget()`, else `PickFrontTarget()`. Si no hay backline, fallback a frontline.

**Empático (Soporte):** Post-strike (si hit + damage > 0), `LowestHpAlly(myTeam)` recibe `damage * HealPercentOfDamage`. Sin roll.

## Snapshot Helper — S34 + S37

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
    Name      = c.Dna.CustomName,                          // S37 new
    Role      = c.Dna.Role,                                // S37 new
    Row       = (int)c.Row,                                // S37 new
};
```

**Importante (S35):** Snapshot captura stats BASE post-equipment, no dinámicos. Propiedades dinámicas (EffSpeed, EffDefense, etc.) son on-the-fly durante turnos y NO se graban.

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CreatureDNA]] — fuente de verdad, se muta si gana/muere
- [[CreatureDatabaseSO]] — resuelve partes por ID
- [[CombatManagerSO]] — config (MaxRounds, CritChance, knobs Elemental, Roles)
- [[EquipmentDatabaseSO]] — resuelve items equipados → procs
- [[EquipmentStats]] — aplica mods de equipment
- [[CombatRng]] — RNG inyectado, determinista
- [[Combatant]], [[CombatResolver]], [[CombatStats]], [[CombatEvolution]], [[EffectiveStats]] — clases extraídas
- [[CombatRecord]], [[CombatTurn]], [[CombatProcEvent]], [[CombatStatusMark]], [[CombatUnitState]] — DTO salida
- [[CombatFighterSnapshot]] — snapshot stats + tiers + color + nombre + role + fila
- [[CombatElements]] — marcas y reacciones elementales (S39; el motor de sinergias S32 fue retirado en S39)
- [[GameEvents]] — (no dispara directo, GameManager/AsyncCombatService orquesta)
- [[CombatTargeting]] — PickFrontTarget, PickBacklineTarget, PickAlly, LowestHpAlly (S37)
- [[RoleWorldProfileSO]] — perfiles de rol (S37)

## Conexiones

**Entrada:**
- `CombatController.SimulateLocal()` → `Simulate(idsA, idsB, rowsA, rowsB, ..., seed)`
- `AsyncCombatService.ApplyResult()` → `SimulateCore(dnasA, dnasB, rowsA, rowsB, ..., rng)`

**Salida:**
- `CombatResult` — contiene `TeamA/TeamB` (snapshots), `Turns` (list con CombatTurn + indices + TeamStateA/B), `Log`, outcome, evolución, muerte
- `CombatRecord` — poblado via `BuildRecord()` con SelfTeam/OpponentTeam/SelfTeamIds (S37) o SelfStats/OpponentStats (1v1 legacy), persistido en `CreatureDNA.CombatHistory`
- `CombatTurn.TeamStateA/TeamStateB` — consumido por visualizador 3v3 (futuro Fase 4) para replay con HP/Shield/status por unit

## Notas (S32-S37-S39)

- **Backward compatible (transicional S37):** Contrato público `Simulate()` cambiado a 3v3; sobrecarga transicional en CombatController para 1v1 local (equipos de 1)
- **S35 Dynamic Properties:** Combatant.EffSpeed/EffDefense/EffEvasion/LifestealPercent se recalculan on-demand, transparentes
- **S36 Backward Stats:** Snapshot captura stats BASE post-equipment, no dinámicos (simetría async)
- **S37 Team Lineup:** Snapshots alineadas con indices en Turns (AttackerIndex/DefenderIndex mapean a TeamA[i]/TeamB[j])
- **S37 Role Metadata:** Role incluido en snapshot para UI (chip de rol en card)
- **S39 Elemental Marks:** Determinista, sin rolls (excepto PisoTierra que remueve marca random). Reacciones instantáneas vs armadas, múltiples disparos por turno posibles.
- **Determinismo total:** Cero UnityEngine.Random; todo vía `CombatRng` inyectado
- **Procs:** Colectados en orden de slot (Body→Arm→Eye→Mouth) para determinismo
- **Logging:** `result.Log` contiene trazas debug de rolls, daños, evasiones, statuses, sinergias, evolución, muerte, efectos de rol, marcas elementales
- **Sinergias:** Integradas en `CombatResolver`; se disparan automáticamente al agregar status. Cap 8 iteraciones previene loops
- **Anti-Permastun:** `CombatResolver` rechaza re-stun; grant inmunidad al despertar
