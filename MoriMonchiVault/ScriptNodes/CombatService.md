---
tags: [combat, core, deterministic, simulation]
---

# CombatService

**Ruta:** `Systems/Combat/CombatService.cs`

**Responsabilidad:** Servicio estático stateless que simula combate turn-based **3v3 team-based** (tolerancia 1..3 por lado), completamente determinista. Orquesta validación de equipos, simulación core pura (sin registry), construcción de records simétricos. **S40:** Descomposición de `TakeTurn()` — delegación de responsabilidades a `CombatRoleHooks` (targeting + shield/heal pasivos), `CombatItems` (paso N usos + CollectUses) / `CombatStrike` (daño y rolls). CombatService 747→607 líneas, núcleo delgado (validación, orden de ronda, secuencia, consecuencias); componedora de: `CombatRng` (inyectado por seed), `Combatant`, `CombatResolver`, `CombatStats`, `CombatEvolution`, `CombatTargeting`, `RoleTableSO`, `CombatElements` (S39). **S41:** Helper `UnitState()` nuevo para poblar `CombatUnitState` con campos elementales (ElementMarks, ArmedStates, Affinity, Energy); parámetro `r` en llamadas de mediadores. **S46:** Energy completamente eliminado. Affinity refactorizado: cada 2 acciones dispara auto-marca al actor mismo (ally-sourced, mismo turno). Orden de TakeTurn reorganizado: GainAffinity y pasivas se mueven post-strike. **S47:** Escudo ahora expira al cierre de cada ronda (ExpireShields), sin rng; nuevo campo CombatResolver.PassivePhase setea true durante ApplyPassives/HealAfterStrike para marcar procs de pasivas.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Simulate(idsA, idsB, rowsA, rowsB, registry, db, config, equipDb, seed)` | `CombatResult` | **S37** Wrapper 3v3: valida equipos (size match, IDs vivos, busy check), resuelve filas, genera CombatRng(seed), llama SimulateCore, construye records para cada unit, retorna result |
| `SimulateCore(dnasA, dnasB, rowsA, rowsB, db, config, equipDb, rng)` | `CombatResult` | **S37** Puro determinista: sin registry. Muta ambas listas de DNAs (EvolvedSlot, IsDead). Retorna result con turnos simétricos |
| `BuildRecord(result, selfDnas, oppDnas, self, isSelfTeamA, oppPlayerName, oppPlayerId, seed, date)` | `CombatRecord` | **S37** Construye un CombatRecord desde perspectiva de `self` (una DNA de un equipo ganador/perdedor) |

## Cambios S47

**Escudos expiran al cierre de cada ronda:**
- Nuevo helper privado `ExpireShields(List<Combatant> team, CombatResult result)` (sin rng)
- Se llama automático al final de cada ronda: `ExpireShields(A, result); ExpireShields(B, result);`
- Cada unit que tenga `Shield > 0f` ve su escudo reducido a 0f al cerrar la ronda
- Log: `[escudo] {name} pierde su escudo (-{amount:F0})`
- Flujo visual en CombatVisualizerService: la barra azul de escudo desaparece entre turnos sin animación especial

**Campos nuevos en CombatResolver (S47):**
- `public bool PassivePhase` — CombatService seta a true justo antes de `ApplyPassives()` y `HealAfterStrike()`, false después
- Todos los procs grabados dentro de ese bloque llevan `CombatProcEvent.PassivePhase = true`
- Permite visualizador coreografiar las pasivas especialmente (desplazamiento del atacante a aliados objetivo)

**Frases y coreografía de pasivas en CombatVisualizerService:**
- Nuevo campo `attackLine = "¡TOMA, {0}!"` — frase del atacante dirigida al defensor
- Campos nuevos:
  - `protectorSelfLine = "¡Me escudaré!"` — Protector sobre sí mismo
  - `empaticoSelfLine = "¡Qué alivio!"` — Empático sobre sí mismo
  - `agresivoSelfLine = "¡Me toca a mí!"` — Agresivo sobre sí mismo
- Procs con `PassivePhase=true` se agrupan por (targetSide, targetIndex)
- Si el objetivo es el atacante mismo (isSelf), anima procs en su posición sin movimiento
- Si el objetivo es un aliado diferente, el atacante viaja a esa posición (lunge), anima procs, retorna

**Marcos visuales en barras:**
- `SetTargeted(true)` en línea 403 cuando comienza el turno (marco rojo en defensor)
- `SetTargeted(false)` en línea 496 después del golpe
- `SetActiveFrames()` en línea 401 para marcar al atacante (marco dorado)

**API de MoriMonchiCombatVisualizerUITK (S47 CAMBIO):**
- Método `Bind()` sin argumentos (antes tenía parámetros)
- Métodos eliminados: `SetStatus`, `SetElementState`, `FlashReaction` 
- Métodos conservados: `SetHp()`, `SetShield()`, `SetActiveTurn()`, `SetTargeted()`

**Eliminaciones en pipeline visual (S47):**
- `PushStatusAll()` — ya no se llama
- `PushElements()` — ya no se llama
- `FlashReaction()` — ya no se llama

**Nuevos Mutes en CombatFeelDirector (S47):**
- `muteSoporte` (bool) — gate de feedbacks de Shield y Heal
- `muteMarcas` (bool) — gate de feedbacks de MarkApplied
- `muteEstados` (bool) — gate de feedbacks de Reaction/estados elementales

## Cambios S46

**Energy completamente eliminado:**
- Campo `Energy` y todos sus rolls (EnergyGained, EnergySpent, EnergyGrantEffect) eliminados.
- Snapshot `CombatUnitState.Energy` eliminado.
- Gates `if (actor.Energy > 0)` removidos de pasivas: ShieldAllyPassive, HealLowestAllyOnHitPassive, BacklineHunterActive.

**Affinity refactorizado (nuevo modelo):**
- Sigue siendo `int`, rango 0-2.
- Método `GainAffinity(Combatant actor, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)` nueva firma (quitó `energy` y cambió ubicación).
- Cada turno, `GainAffinity()` incrementa +1; al alcanzar 2, se resetea a 0 y dispara `CombatElements.AddMark(actor, actor.Element, true, actor, config, result, r, rng)` (auto-marca, ally-sourced, MISMO TURNO).
- El "beat" visual emite dos eventos: `AffinityGained` con affinity=2 tras marcar, y luego `AffinityGained` con affinity=0 (post-reset).

**Orden de TakeTurn completamente reorganizado (S46):**

Antes (S41):
```
1. TickStatuses()
2. Energizado check / Stun check / Confuso / Mareado
3. Items.UseItems()
4. Strike
5. Pasivas (GrantShield pre-strike)
6. HealAfterStrike
7. Lifesteal
```

Ahora (S46):
```
1. TickStatuses() — tick deaths
2. ResolveTarget() — targeting (Agresivo backline chance, default frontline)
3. Energizado check (consume si present) — log prioridad
4. Stun check — skip turn si stunned, grant immunity if expired
5. Confuso check — consume, skip strike, pero GANA AFFINITY
6. Mareado check — consume, roll damage a aliado al azar, pero GANA AFFINITY
7. Items.UseItems()
8. Strike
9. GainAffinity() — incrementa +1, al alcanzar 2 auto-marca + reset
10. ApplyPassives() — pasivas (incluyendo MarkRandomAllyPassive para Agresivo)
11. HealAfterStrike() — heal-on-damage (Empático)
12. Lifesteal
13. ExpireShields() — **S47 NEW**: al final de cada ronda, no durante turno
14. EmitTurn()
```

**Cambios claves:**
- Pasivas ahora corren POST-STRIKE (paso 10, antes paso 5).
- GainAffinity corre entre strike y pasivas (paso 9).
- Todos los "no action" paths (Confuso, Mareado, Stun) TODAVÍA ganan Affinity (el gate fue removido).
- Snapshot sin `Energy` en `CombatUnitState`.

**Comentario de cabecera reescrito** (líneas 1-24) con el nuevo orden de turno.

## Cambios S41 (Paso 0)

**Eventos elementales al CombatRecord (aditivo, backward compatible):**

- Nuevo helper `UnitState(Combatant c)` poblado en cada turno: captura ElementMarks, ArmedStates, Affinity (Energy en S41, eliminado S46)
- `EmitTurn()` ahora llama `UnitState()` para ambos equipos y los asigna a `CombatTurn.TeamStateA/B`

**Parámetro `r` (CombatResolver) en TakeTurn:**
- Todos los mediadores (CombatRoleHooks, CombatStrike) ahora reciben `r` para propagar eventos elementales
- Eventos se graban en `CombatProcEvent` aditivos dentro de `Turn.Procs`

## Cambios S40

**Descomposición de TakeTurn():**

```csharp
var profile = config.Roles != null ? config.Roles.GetProfile(actor.Role) : null;
var target = CombatRoleHooks.ResolveTarget(actor, profile, allies, enemies, config, result, r, rng);
CombatRoleHooks.ApplyPassives(actor, profile, allies, config, result, r, rng);
CombatItems.UseItems(actor, target, r, result);
var strike = CombatStrike.Execute(actor, target, config, result, r, rng);
CombatRoleHooks.HealAfterStrike(actor, profile, allies, strike.Dodged, strike.Damage, config, result, r, rng);
```

**Beneficios:**
- **Testabilidad:** Cada mediador es unit-testeable independientemente
- **Extensibilidad:** Nuevos roles/efectos sin tocar CombatService
- **Claridad:** TakeTurn() ahora orquesta en lugar de implementar
- **Determinismo:** Orden de consumo RNG intacto

**Mediadores nuevos:**
- [[CombatRoleHooks]] — targeting + ApplyPassives (post-strike S46)
- [[CombatItems]] — colección y uso de items equipados
- [[CombatStrike]] — rolls + daño + estados

**Configuración elemental:**
- Acceso: `config.Elements` (ElementTableSO) en lugar de campos individuales

## Cambios S39

**Sistema de marcas elementales:**

Después de que un golpe conecta (damage > 0), se llama `CombatElements.AddMark(target, attacker.Element, false, attacker, config, result, rng)`
- Impide duplicados (mismo Element+fuente)
- Detecta pares de elementos distintos y detona reacciones
- Reacciones instantáneas (Cleanse, OverGrow, Leech, PisoTierra) se resuelven inmediato
- Reacciones armadas (Energizado, Vaporizado, etc.) se agregan a Combatant.States

**Determinismo:** Todos los rolls de elemento vía CombatElements deterministas o usan CombatRng inyectado.

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

**Resolución de filas:** Si `rowsA == null`, usa `DefaultLineup` (2-3-2: Front, Front, Mid).

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
1. **Speed Tiebreak Roll ONCE PER TEAM:** Para cada unit vivo en A y B, roll tiebreak
2. **Ordenar:** Units por (Energizado present desc, EffSpeed desc, tiebreak desc, team A-before-B, Index asc) — S46: Energizado check removido del orden, queda solo speed/tiebreak
3. **Iterar:** Cada unit toma un turno en ese orden

## Métodos Privados

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `ValidateTeam(ids, registry, config, outDnas)` | `bool` | **S37** Valida team size, IDs vivos, no busy, fights restantes |
| `ResolveRows(count, rows, outResolved)` | `bool` | **S37** Valida/resuelve fila list; default 2-3-2 si null |
| `TakeTurn(atk, myTeam, oppTeam, config, result, round, resolver, rng, teamA, teamB)` | `void` | **S46** Nuevo orden: TickStatuses → ResolveTarget → Energizado → Stun → Confuso → Mareado → Items → Strike → GainAffinity → ApplyPassives → HealAfterStrike → Lifesteal → EmitTurn. **S47:** Seta r.PassivePhase = true/false alrededor de ApplyPassives/HealAfterStrike |
| `GainAffinity(actor, config, result, r, rng)` | `void` | **S46 NEW** Incrementa actor.Affinity +1; al alcanzar 2, resetea a 0 y dispara auto-marca ally-sourced |
| `ExpireShields(team, result)` | `void` | **S47 NEW** Privado static: al final de cada ronda, zeroea Shield de todos los units vivos. Sin rng. |
| `EmitTurn(result, round, atk, def, noAttack, damage, crit, defHpAfter, defShieldAfter, procs, teamA, teamB)` | `void` | **S46** Crea CombatTurn con indices, TeamStateA/B (S41: sin Energy). |
| `TickStatuses(c, result, resolver)` | `void` | Aplica daño/curación por status activo |
| `BuildCombatant(dna, db, equipDb, isA, index, row, roles)` | `Combatant` | **S37** Construye modelo con Row e Index; aplica RoleTableSO mods; colecta uses |
| `Snapshot(Combatant)` | `CombatFighterSnapshot` | Extrae stats + tiers + color + nombre + role + row |
| `UnitState(Combatant)` | `CombatUnitState` | **S41** Extrae Hp/Shield/Marks + ElementMarks/ArmedStates/Affinity (S46: sin Energy) |
| `StatusMarks(Combatant)` | `List<CombatStatusMark>` | Contea efectos activos |

## Ciclo de Determinismo (S32 + S37 + S39 + S40 + S41 + S46 + S47)

1. **Local:** `CombatController.SimulateLocal(idsA, idsB, rowsA, rowsB)`
   - Genera `seed = Guid.NewGuid().GetHashCode()`
   - Llama `Simulate(idsA, idsB, rowsA, rowsB, ..., seed)`
   - Valida registry, construye DNAs, construye Combatants, llama `SimulateCore(..., new CombatRng(seed))`
   - Construye records para cada unit (S46: sin Energy en snapshots)
   - Persiste automático via GameEvents.RegistryChanged

2. **Async:** Cloud Code (JS v2) matchea y emite blob
   - Cliente recibe blob
   - Deserializa DNAs desde JSON snapshot, llama `SimulateCore(..., new CombatRng(blob.Seed))`
   - **Mismo seed + mismo DNA snapshots + mismo rows = resultado idéntico**
   - Construye record desde perspectiva propia vía `BuildRecord(...)`

## Flujo de Turno Completo (S47)

```
Per round per unit in turn order (sorted by EffSpeed desc):
  1. TickStatuses() — apply Poison/Burn/Regen/Pulse damage/heal; unit dies aquí → no gana Affinity
  2. ResolveTarget() — Agresivo backline chance o default frontline
  3. Energizado check — consume si present (log prioridad)
  4. Stun check — skip turn si stunned, grant immunity if expired
  5. Confuso check — consume, action fails, pero GANA AFFINITY
  6. Mareado check — consume, roll: si hit → ataque aliado al azar + GANA AFFINITY, si resist → continúa
  7. Items.UseItems() — equipped uses (deterministic)
  8. Strike — evasion + crit + damage + shield + reflect + enemy mark
  9. GainAffinity() — +1, al alcanzar 2 → reset a 0 + auto-marca (ally-sourced, MISMO TURNO)
 10. r.PassivePhase = true
 11. ApplyPassives() — role passives (Protector/Empático/Agresivo, todas corren post-strike) — los procs se marcan PassivePhase=true
 12. HealAfterStrike() — Empático heal-on-damage — los procs se marcan PassivePhase=true
 13. r.PassivePhase = false
 14. Lifesteal
 15. EmitTurn() → record state (sin Energy)

End of round:
 16. ExpireShields(A) — todos los units con Shield>0 lo setean a 0
 17. ExpireShields(B) — sin rng, sin animación especial
```

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CreatureDNA]] — fuente de verdad, se muta si gana/muere
- [[CreatureDatabaseSO]] — resuelve partes por ID
- [[CombatManagerSO]] — config (MaxRounds, CritChance, Roles, Elements)
- [[EquipmentDatabaseSO]] — resuelve items equipados
- [[EquipmentStats]] — aplica mods de equipment
- [[CombatRng]] — RNG inyectado, determinista
- [[Combatant]], [[CombatResolver]], [[CombatStats]], [[CombatEvolution]], [[EffectiveStats]]
- [[CombatRecord]], [[CombatTurn]], [[CombatProcEvent]], [[CombatStatusMark]], [[CombatUnitState]]
- [[CombatFighterSnapshot]] — snapshot stats + tiers + color + nombre + role + fila
- [[CombatElements]] — marcas y reacciones elementales
- [[CombatTargeting]] — PickFrontTarget, PickBacklineTarget, PickAlly, LowestHpAlly
- [[CombatRoleHooks]] — targeting + ApplyPassives (S46: post-strike) + nueva coreografía S47
- [[CombatItems]] — usos equipados
- [[CombatStrike]] — rolls y daño
- [[ElementTableSO]] — tablas elementales
- [[GameEvents]] — (GameManager/AsyncCombatService orquesta)
- [[CombatVisualizerService]] — consume PassivePhase para coreografía especial S47

## Conexiones

**Entrada:**
- `CombatController.SimulateLocal()` → `Simulate(idsA, idsB, rowsA, rowsB, ..., seed)`
- `AsyncCombatService.ApplyResult()` → `SimulateCore(dnasA, dnasB, rowsA, rowsB, ..., rng)`

**Salida:**
- `CombatResult` — contiene `TeamA/TeamB` (snapshots sin Energy), `Turns` (S46: con Affinity en UnitState), `Log`, outcome, evolución, muerte
- `CombatRecord` — SelfTeam/OpponentTeam/SelfTeamIds, persistido en `CreatureDNA.CombatHistory`

## Notas (S32-S37-S39-S40-S41-S46-S47)

- **S47:** Escudos expiran ronda-a-ronda sin rng. PassivePhase es un flag de timing para coreografía visual.
- **S46:** Energy completamente eliminado como recurso. Affinity es el único mecánica de acumulación para disparar auto-marcas.
- **S46:** Pasivas ahora corren POST-STRIKE y SIEMPRE (sin gate de Energy).
- **S46:** GainAffinity nuevo parámetro y ubicación (post-strike, pre-pasivas).
- **Backward compatible (S37):** Contrato público `Simulate()` es 3v3; sobrecarga transicional en CombatController para 1v1 local
- **S35 Dynamic Properties:** Combatant.EffSpeed/EffDefense/EffEvasion/LifestealPercent se recalculan on-demand
- **S37 Team Lineup:** Snapshots alineadas con indices en Turns
- **S39 Elemental Marks:** Determinista, sin rolls (excepto PisoTierra random removal)
- **S40 Descomposición:** TakeTurn orquesta 3 mediadores; orden RNG intacto; ElementTableSO centraliza config
- **Determinismo total:** Cero UnityEngine.Random; todo vía CombatRng inyectado
