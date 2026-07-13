---
tags: [combat, core, deterministic, simulation]
---

# CombatService

**Ruta:** `Systems/Combat/CombatService.cs`

**Responsabilidad:** Servicio estático stateless que simula combate turn-based **3v3 team-based** (tolerancia 1..3 por lado), completamente determinista. Orquesta validación de equipos, simulación core pura (sin registry), construcción de records simétricos. **S40:** Descomposición de `TakeTurn()` — delegación de responsabilidades a `CombatRoleHooks` (targeting + shield/heal pasivos), `CombatItems` (paso N usos + CollectUses) / `CombatStrike` (daño y rolls). CombatService 747→607 líneas, núcleo delgado (validación, orden de ronda, secuencia, consecuencias); componedora de: `CombatRng` (inyectado por seed), `Combatant`, `CombatResolver`, `CombatStats`, `CombatEvolution`, `CombatTargeting`, `RoleWorldProfileSO`, `CombatElements` (S39). **S41:** Helper `UnitState()` nuevo para poblar `CombatUnitState` con campos elementales (ElementMarks, ArmedStates, Affinity, Energy); parámetro `r` en llamadas de mediadores para propagar eventos elementales.

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

## Cambios S39

**Sistema de marcas elementales:**

En el flujo de turno S37, el paso 13 (Fire procs defensivos) se extiende con:

```csharp
// (13) elemental mark — if the strike connected (not dodged, damage>0),
// mark the target with the attacker's Element as an enemy-sourced mark;
```

**Integración:**
- Después de que un golpe conecta (damage > 0), se llama `CombatElements.AddMark(target, attacker.Element, false, attacker, config, result, rng)`
- El método AddMark maneja la lógica: impide duplicados (mismo Element+fuente), detecta pares de elementos distintos y detona reacciones
- Reacciones instantáneas (Cleanse, OverGrow, Leech, PisoTierra) se resuelven inmediato
- Reacciones armadas (Energizado, Vaporizado, etc.) se agregan a Combatant.States como estados single-use

**Determinismo:** Todos los rolls de elemento vía CombatElements son deterministas (sin rolls) o usan CombatRng inyectado (PisoTierra random mark removal).

## Cambios S40

**Descomposición de TakeTurn():**

Antes `TakeTurn()` contenía toda la lógica inline. Ahora delega 3 operaciones críticas a mediadores estáticos:

```csharp
var profile = config.Roles != null ? config.Roles.GetProfile(actor.Role) : null;
var target = CombatRoleHooks.ResolveTarget(actor, profile, allies, enemies, config, result, rng);
// ...
CombatRoleHooks.GrantShield(actor, profile, allies, config, result, r, rng);
CombatItems.UseItems(actor, target, r, result);
var strike = CombatStrike.Execute(actor, target, config, result, rng);
CombatRoleHooks.HealAfterStrike(actor, profile, allies, strike.Dodged, strike.Damage, config, result, r, rng);
```

**Beneficios:**
- **Testabilidad:** Cada mediador es unit-testeable independientemente
- **Extensibilidad:** Nuevos roles/efectos sin tocar CombatService
- **Claridad:** TakeTurn() ahora orquesta en lugar de implementar
- **Determinismo:** Orden de consumo RNG intacto (verificado por paridad de log al hash)

**Mediadores nuevos:**
- [[CombatRoleHooks]] — targeting + shield/heal pasivos
- [[CombatItems]] — colección y uso de items equipados
- [[CombatStrike]] — rolls + daño + estados

**Configuración elemental:**
- Antes: 8 knobs hardcoded (VaporizadoEvaBonus, etc) en CombatManagerSO
- Ahora: Tablas en [[ElementTableSO]] (identidades, estados, reacciones con efectos polimórficos)
- Acceso: `config.Elements` (ElementTableSO) en lugar de campos individuales

## Cambios S41 (Paso 0)

**Eventos elementales al CombatRecord (aditivo, backward compatible):**

- Nuevo helper `UnitState(Combatant c)` poblado en cada turno: captura ElementMarks, ArmedStates, Affinity, Energy (además de Hp/Shield/Marks clásicos)
- `EmitTurn()` ahora llama `UnitState()` para ambos equipos y los asigna a `CombatTurn.TeamStateA/B`

**Parámetro `r` (CombatResolver) en TakeTurn:**
- Todos los mediadores (CombatRoleHooks, CombatStrike) ahora reciben `r` para propagar eventos elementales
- Eventos se graban en `CombatProcEvent` aditivos dentro de `Turn.Procs` (coexisten con procs clásicos)

**Flujo de grabación:**
```
TakeTurn()
├─ CombatRoleHooks.ResolveTarget(..., r, ...)
├─ CombatRoleHooks.GrantShield(..., r, ...)
│  ├─ Pasive.OnTurnStart(..., r, ...)
│  │  └─ RecordElement(EnergySpent, ...) si Energy gasto
│  │  └─ AddMark(..., r, ...) si Energy
│  │     └─ RecordElement(MarkApplied, ...)
├─ CombatStrike.Execute(..., r, ...)
│  ├─ RecordElement(StateConsumed, ...) para Vaporizado/GolpePreciso/Debilidad/Boiling/Charcoal
│  └─ AddMark(..., r, ...) si damage > 0
│     └─ RecordElement(MarkApplied, ...)
│     └─ FindReaction → Effects.Apply(..., r, ...)
│        └─ RecordElement(StateArmed / Heal / Damage / ...)
├─ CombatRoleHooks.HealAfterStrike(..., r, ...)
│  └─ Pasive.OnDamageDealt(..., r, ...)
│     └─ RecordElement(EnergySpent, ...) si Energy gasto
│     └─ AddMark(..., r, ...) si Energy
└─ EmitTurn() → UnitState() para TeamStateA/B (captura marcas, estados, afinidad, energía)
```

**Backward compatible:**
- Records viejos (S40) no tienen elementos en CombatTurn.Procs
- CombatUnitState.ElementMarks/ArmedStates/Affinity/Energy son nuevos campos (null/empty deserializa OK)
- Lector siempre gatea por `CombatProcEvent.ElementEvent` primero para diferenciar tipos

## Métodos Privados

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `ValidateTeam(ids, registry, config, outDnas)` | `bool` | **S37** Valida team size, IDs vivos, no busy, fights restantes |
| `ResolveRows(count, rows, outResolved)` | `bool` | **S37** Valida/resuelve fila list; default 2-3-2 si null |
| `TakeTurn(atk, myTeam, oppTeam, config, result, round, resolver, rng, teamA, teamB)` | `void` | **S37/S39/S40/S41** Resuelve turno: role logic (delegada a CombatRoleHooks), items (delegada a CombatItems), targeting, damage (delegada a CombatStrike), status, procs, marcas elementales. Parámetro `r` nuevo S41. |
| `EmitTurn(result, round, atk, def, myTeamA, myTeamB, noAttack, damage, crit, shieldAfter, procs)` | `void` | **S37/S41** Crea CombatTurn con indices, TeamStateA/B. S41 popula con UnitState(). |
| `TickStatuses(c, result, resolver)` | `void` | Aplica daño/curación por status activo |
| `BuildCombatant(dna, db, equipDb, isA, index, row, roles)` | `Combatant` | **S37/S39/S40** Construye modelo con Row e Index; aplica RoleTableSO mods; colecta uses via `CombatItems.CollectUses()` |
| `Snapshot(Combatant)` | `CombatFighterSnapshot` | Extrae stats + tiers + color + nombre + role + row |
| `UnitState(Combatant)` | `CombatUnitState` | **S41 NEW** Extrae Hp/Shield/Marks (clásicos) + ElementMarks/ArmedStates/Affinity/Energy (elementales) |
| `StatusMarks(Combatant)` | `List<CombatStatusMark>` | Contea efectos activos |
| `Clip(id)` | `string` | Trunca ID a 14 chars para logging |

## Ciclo de Determinismo (S32 + S37 + S39 + S40 + S41)

1. **Local:** `CombatController.SimulateLocal(idsA, idsB, rowsA, rowsB)`
   - Genera `seed = Guid.NewGuid().GetHashCode()`
   - Llama `Simulate(idsA, idsB, rowsA, rowsB, ..., seed)`
   - Valida registry, construye DNAs, construye Combatants, llama `SimulateCore(..., new CombatRng(seed))`
   - Construye records para cada unit (S41: incluye eventos elementales en CombatTurn.Procs)
   - Persiste automático via GameEvents.RegistryChanged

2. **Async:** Cloud Code (JS v2) matchea y emite `CloudMatchBlob { Seed, CreatureJsonsA, CreatureJsonsB, RowsA, RowsB, ... }`
   - Cliente recibe blob
   - Deserializa DNAs desde JSON snapshot, llama `SimulateCore(..., new CombatRng(blob.Seed), blob.RowsA, blob.RowsB)`
   - **Mismo seed + mismo DNA snapshots + mismo rows = resultado idéntico (incluyendo eventos elementales S41)**
   - Construye record desde perspectiva propia vía `BuildRecord(result, selfDnas, oppDnas, self, ...)`

## Flujo de Turno Completo (S37/S39/S40/S41)

```
Per round per unit in turn order (sorted by EffSpeed):
  1. TickStatuses() — apply Poison/Burn/Regen/Pulse damage/heal
  2. FireProcs(Passive)
  3. Stun check + wake-up immunity
  4. [Role Actives] CombatRoleHooks.ResolveTarget() — Agresivo backline chance (S41: + parámetro r)
  5. [Default] target = PickFrontTarget()
  6. [Role Passives Pre] CombatRoleHooks.GrantShield() — Protector shield/mark (S41: + parámetro r)
  7. CombatItems.UseItems() — equipped uses (deterministic) (S41: + parámetro r)
  8. If dead, EmitTurn and return
  9. [Strike] CombatStrike.Execute() — evasion + crit + damage + shield + reflect + mark (S41: + parámetro r)
  10. [Role Passives Post] CombatRoleHooks.HealAfterStrike() — Empático heal/mark (S41: + parámetro r)
  11. Lifesteal post-strike
  12. EmitTurn() → record AttackerIndex/DefenderIndex, DefenderShieldAfter, TeamStateA/B (S41: UnitState con elementales)
  13. If either unit or team dead, break loop or flag round end
```

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CreatureDNA]] — fuente de verdad, se muta si gana/muere
- [[CreatureDatabaseSO]] — resuelve partes por ID
- [[CombatManagerSO]] — config (MaxRounds, CritChance, Roles, Elements)
- [[EquipmentDatabaseSO]] — resuelve items equipados → procs
- [[EquipmentStats]] — aplica mods de equipment
- [[CombatRng]] — RNG inyectado, determinista
- [[Combatant]], [[CombatResolver]], [[CombatStats]], [[CombatEvolution]], [[EffectiveStats]] — clases extraídas
- [[CombatRecord]], [[CombatTurn]], [[CombatProcEvent]], [[CombatStatusMark]], [[CombatUnitState]] — DTO salida
- [[CombatFighterSnapshot]] — snapshot stats + tiers + color + nombre + role + fila
- [[CombatElements]] — marcas y reacciones elementales (S39, S41 con parámetro r)
- [[CombatTargeting]] — PickFrontTarget, PickBacklineTarget, PickAlly, LowestHpAlly (S37)
- [[RoleWorldProfileSO]] — perfiles de rol (S37, deprecated S40)
- [[CombatRoleHooks]] — targeting + shield/heal (S40, S41 con parámetro r)
- [[CombatItems]] — usos equipados (S40, S41 con parámetro r)
- [[CombatStrike]] — rolls y daño (S40, S41 con parámetro r)
- [[ElementTableSO]] — tablas elementales (S40)
- [[GameEvents]] — (no dispara directo, GameManager/AsyncCombatService orquesta)

## Conexiones

**Entrada:**
- `CombatController.SimulateLocal()` → `Simulate(idsA, idsB, rowsA, rowsB, ..., seed)`
- `AsyncCombatService.ApplyResult()` → `SimulateCore(dnasA, dnasB, rowsA, rowsB, ..., rng)`

**Salida:**
- `CombatResult` — contiene `TeamA/TeamB` (snapshots), `Turns` (list con CombatTurn + indices + TeamStateA/B + eventos elementales S41), `Log`, outcome, evolución, muerte
- `CombatRecord` — poblado via `BuildRecord()` con SelfTeam/OpponentTeam/SelfTeamIds (S37) o SelfStats/OpponentStats (1v1 legacy), persistido en `CreatureDNA.CombatHistory`
- `CombatTurn.TeamStateA/TeamStateB` — consumido por visualizador 3v3 (futuro Fase 4) para replay con HP/Shield/status + marcas/estados elementales (S41)

## Notas (S32-S37-S39-S40-S41)

- **Backward compatible (transicional S37):** Contrato público `Simulate()` cambiado a 3v3; sobrecarga transicional en CombatController para 1v1 local (equipos de 1)
- **S35 Dynamic Properties:** Combatant.EffSpeed/EffDefense/EffEvasion/LifestealPercent se recalculan on-demand, transparentes
- **S36 Backward Stats:** Snapshot captura stats BASE post-equipment, no dinámicos (simetría async)
- **S37 Team Lineup:** Snapshots alineadas con indices en Turns (AttackerIndex/DefenderIndex mapean a TeamA[i]/TeamB[j])
- **S37 Role Metadata:** Role incluido en snapshot para UI (chip de rol en card)
- **S39 Elemental Marks:** Determinista, sin rolls (excepto PisoTierra que remueve marca random). Reacciones instantáneas vs armadas, múltiples disparos por turno posibles.
- **S40 Descomposición:** TakeTurn ahora orquesta 3 mediadores (CombatRoleHooks, CombatItems, CombatStrike); orden RNG intacto; ElementTableSO centraliza config elemental.
- **S41 Eventos elementales:** Paso 0 (F4) — eventos se graban en CombatRecord de manera aditiva; CombatUnitState capetura marcas/estados/afinidad/energía por turno; backward compatible; lector gatea por ElementEvent para diferenciar.
- **Determinismo total:** Cero UnityEngine.Random; todo vía `CombatRng` inyectado
- **Procs:** Colectados en orden de slot (Body→Arm→Eye→Mouth) para determinismo
- **Logging:** `result.Log` contiene trazas debug de rolls, daños, evasiones, statuses, sinergias, evolución, muerte, efectos de rol, marcas elementales
- **Anti-Permastun:** `CombatResolver` rechaza re-stun; grant inmunidad al despertar
