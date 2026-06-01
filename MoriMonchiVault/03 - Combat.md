---
tags: [memory-bank, combat, async, scheduler]
---

# 03 — Combat

> Relacionados: [[04 - UGS & Cloud]] (auth + Cloud Save), [[02 - Genetics & Breeding]] (stats efectivos), [[08 - Known Bugs & Checkpoints]] (bugs activos).

## Combate Local (`CombatService`)

- **Stats efectivos** = `BaseStat (DNA) + Σ(part.Stat + (tier-1))` por slot. Calculados en runtime, no almacenados.
- Orden por `Speed`; empates aleatorios. **20% crit = ×3 daño**. Safety cap: `MaxRounds = 50`.
- **Límite de peleas**: `MaxFightCount = 5` (en `CombatManagerSO`). `CombatService.Simulate()` valida `FightCount < MaxFightCount` antes de simular.
- **Empate**: si ninguno llega a 0 HP antes de `MaxRounds` → `IsDraw = true`, `FightCount++` en ambos, sin evolución ni muerte.
- **Log por turno**: cada round loguea quién ataca primero, daño, si fue crit, y HP restante del defensor. La línea final siempre incluye nombre, UniqueID y parte evolucionada.
- **Evolución (ganador)**: el ganador **siempre** evoluciona una parte aleatoria elegible (`< Tier3`). Si todas están en Tier3, se loguea que no hay más evolución posible.
- **Evolución futura (pendiente)**: la parte a evolucionar se elegirá por peso según su tier actual. Regla diseñada: **70% peso Tier1 → Tier2 / 20% peso Tier2 → Tier3 / 10% peso Tier3** (tier máximo, sin efecto). Si un pool está vacío se excluye y los pesos restantes se renormalizan. `CombatManagerSO.EvolutionChance` queda reservado para reglas futuras.
- **Muerte (perdedor)**: probabilidad configurable `DeathChance` en `CombatManagerSO`.
- `CombatManagerSO.Current` — singleton configurable. Asignar en `GameManager → Setup`.
- `GameManager`: botón **Fill Random Fighters** — selecciona 2 criaturas vivas con peleas disponibles.

## Combate Async — arquitectura dual

Dos modos coexistentes que comparten el mismo pool en Cloud Save Custom Data.

### Modo Instant — `run-combat.js`

Botón naranja "Enqueue for Combat (Instant)". El cliente llama `run-combat`, que en un mismo request:
1. Lee el pool. Si hay opponent → simula y escribe resultados a ambos players. Returns `{status: "matched"}` y el cliente hace `PollResultsAsync()` inmediato.
2. Si no hay opponent → enqueue y returns `{status: "waiting"}`.

**Use case**: testing rápido entre dos cuentas activas.

### Modo Timer/Scheduled — `enqueue-combat.js` + `process-matchmaking.js`

Botón morado "Enqueue for Combat (Timer)". Flow:
1. Cliente llama `enqueue-combat` → solo agrega al pool, returns `{status: "queued"}` inmediato. Cliente puede cerrar el juego.
2. Unity Scheduler dispara `process-matchmaking` cada hora UTC (cron `0 * * * *`). El script drena el pool, hace shuffle, empareja evitando self-match, simula cada par y escribe `combat_results` en el Player Data de cada jugador. Leftover odd-one-out vuelve al pool.
3. Cliente vuelve al juego, presiona "Check Pending Results" → `PollResultsAsync()` aplica los resultados localmente.

### Custom Data — quirks descubiertos

- `setCustomItem` firma correcta: **3 args** `(projectId, customId, body)` donde body es `{key, value}`. La firma de 4 args silently corrompe el body.
- `value` **NO acepta arrays top-level** — siempre envolver en `{ entries: [...] }`. Empírico, la doc no lo dice.
- El método se llama `getCustomItems` (plural con array de keys), NO `getCustomItem` singular.
- Auth via `accessToken: context.serviceToken` en el constructor del `DataApi`.

### Identificación de oponentes en logs

`CombatResult` incluye `OpponentName` (criatura), `OpponentPlayerId` (UUID), `OpponentPlayerName` (display name de `AuthenticationService.GetPlayerNameAsync()`). El log final (resuelto en `AsyncCombatService.ApplyResult`, que también fetchea el `playerName` local):

```
[AsyncCombat]  Sowtank => "Fuzzy Blob"  vs  "Slimy Goo" <= Manolito  ——  ¡Ganaste!  |  Evolved: Arm
```

El `OpponentPlayerId` ya **no** se imprime en el log (ruido visual). Se conserva en el `CombatResult` por si se necesita para futuras features (venganza, perfiles).

## Estado Busy persistente (encolado entre sesiones)

`CreatureDNA.BusyState` (`BusyReason.QueuedForCombat`) marca una criatura como ocupada. **Debe persistir en Cloud Save** para sobrevivir logout/login, si no la criatura reaparece libre al reconectarse.

**Reglas:**
- `AsyncCombatService.EnqueueInternal` → tras setear `BusyState`, llama `SaveSystem.SaveDatabase` **+ `GameManager.Instance.PushToCloud()`**. Sin el push, `PullAsync` en el próximo login baja el estado viejo (no-busy) y pisa el local.
- `PollResultsAsync` y `DequeueAsync` → tras limpiar `BusyState`, también pushean.
- `CombatService.Simulate` y `BreedingService.Breed` validan `IsBusy` (además de `IsDead`): una criatura encolada no puede pelear localmente ni criar.
- `dequeue-combat.js` filtra el pool por `creatureId + playerId` (solo desencola criaturas propias). El cliente limpia `BusyState` local **siempre**, aunque el server responda `not_found` (ya fue matcheada → el resultado llegará por `PollResultsAsync`).

## Reconciliación de fantasmas (Queued local sin estar en el pool)

**Síntoma**: una criatura aparece `QueuedForCombat` en local aunque no está en el pool ni tiene resultado pendiente (dequeue da `not_found`, check results no muestra nada).

**Causa**: el `BusyState=Queued` se setea y pushea **antes** de confirmar el enqueue server-side, y los pushes fire-and-forget pueden llegar fuera de orden (el "Queued" pisa al "None" en el cloud → revive en el próximo `PullAsync`). El caso `default` de `EnqueueInternal` tampoco hace rollback.

**Fix (self-healing, no elimina la causa raíz)**: `PollResultsAsync` ahora hace **apply-then-reconcile** — aplica resultados pendientes y SIEMPRE llama `ReconcileGhostsAsync`, que vía `get-queue-status` (1 read) limpia el `BusyState` de toda criatura Queued que el pool no tiene.

**Salvaguardas:**
- Si el server no responde (`null`) no toca nada.
- Salta las que están `inFlightEnqueues` (ventana de 5s del enqueue).
- `Show Queued MoriMonchis` ahora consulta el pool + resultados y rotula cada una: **In Queue / Result Ready / GHOST / ?(offline)**.

→ El usuario ya no necesita Dequeue para fantasmas. Costo: 1 Cloud Code call + 1 read por botón, solo on-demand.

> Para erradicar la causa raíz, ver [[08 - Known Bugs & Checkpoints]].

## Scheduler — arquitectura de 3 piezas (CRÍTICO)

El Scheduler de Unity **NO invoca el script de Cloud Code directamente**. Emite un evento al servicio **Triggers**, y un Trigger separado redirige ese evento al script. Faltaba esta pieza → el schedule existía (`ugs sched list` lo mostraba) pero nunca ejecutaba (`progress: 0`, logs vacíos).

```
matchmaking-tick.sched  →  emite evento  →  Triggers service  →  matchmaking-trigger.tr  →  process-matchmaking.js
   cron "0 * * * *"         "process-matchmaking.v1"                (el enlace)               (el script)
```

**El `eventType` del Trigger sigue un patrón obligatorio:**
```
com.unity.services.scheduler.<eventName>.v<payloadVersion>
```
Con `eventName: process-matchmaking` + `payloadVersion: 1` → `com.unity.services.scheduler.process-matchmaking.v1`.

**Formatos de archivo CLI (¡difieren entre sí!):**
- `.sched` — `Configs` es un **objeto** `{ "nombre": { ... } }`, campos PascalCase (`EventName`, `Type`, `Schedule`, `PayloadVersion`, `Payload`).
- `.tr` — `Configs` es un **array** `[ { ... } ]`, cada item con `Name`, `EventType`, `ActionType: "cloud-code"`, `ActionUrn: "urn:ugs:cloud-code:<script>"`. (Para módulos C#: `urn:ugs:cloud-code:<modulo>/<funcion>`.)

### Setup minimal (CLI)

1. Bajar `ugs-windows-x64.exe`, renombrar a `ugs.exe`, agregar al PATH.
2. Crear Service Account en Dashboard → Organization → Administration → Service Accounts → generar Keys.
3. Asignar roles: project (`Cloud Code Editor/Viewer/Publisher`, `Unity Environments Admin`) + organization (`Owner`).
4. `ugs login` + `ugs config set project-id <id>` + `ugs config set environment-name production`.
5. Editar `CloudCode/matchmaking-tick.sched` (cron) y `CloudCode/matchmaking-trigger.tr` (enlace).
6. Deployar **ambos**: `ugs deploy CloudCode/matchmaking-tick.sched` + `ugs deploy CloudCode/matchmaking-trigger.tr`.
7. Verificar con `ugs sched list` (el CLI NO lista triggers; verlos vía REST API o esperar logs de ejecución).

### Gestión vía REST API

El CLI solo tiene `sched list` y `new-file` (sin `delete`/`enable`). Para borrar/listar schedules usar la Scheduler Admin API con Basic Auth (`base64(<KEY_ID>:<SECRET_KEY>)`):

```
GET/DELETE https://services.api.unity.com/scheduler/v1/projects/<PROJECT_ID>/environments/<ENV_ID>/configs[/<CONFIG_ID>]
```

- Project ID: `14ef2aa0-ac88-457a-be73-9164939d87b0`
- Environment `production`: `6f9c7d83-1396-4de7-ba1c-ba01cec186df`

⚠️ **Restricción de Unity**: schedule mínimo cada 1 hora, siempre UTC. Para testing inmediato hay un botón **"Force Matchmaking Tick (DEV)"** en `CloudCodeTester.cs` que llama directo a `process-matchmaking` (bypasea scheduler+trigger).

## Service Account ≠ Project Secrets

- **Service Account Keys** (Organization → Administration → Service Accounts → Keys): para autenticar la CLI/herramientas externas.
- **Project Secrets** (Proyecto → Cloud Code → Secrets): variables de entorno para que los scripts JS accedan a APIs externas en runtime.

NO confundirlas — son cosas distintas.

## Historial de combate replayable (transversal combate ↔ world)

- Cada pelea (local **y** async) escribe un `CombatRecord` con `Turns` estructurados en `CreatureDNA.CombatHistory`. **El server manda**: `process-matchmaking.js`/`run-combat.js` emiten los turnos en el mismo formato PascalCase que el `CombatTurn` de C#; el cliente solo lee y almacena (`AsyncCombatService.ApplyResult`). El combate local lo arma `CombatService.Simulate` (ambos peleadores).
- `SelfWasA` en cada record + `AttackerIsA` en cada turno desambiguan quién es "yo" en el replay simétrico.
- **El Combat Visualizer (futuro, solo local)** se alimentará puramente de esta data almacenada. La tab Combate del detalle será su hogar.

## Archivos clave

```
CloudCode/
├── enqueue-combat.js                 # append entry a matchmaking_pool. Returns {status,poolSize}
├── dequeue-combat.js                 # remueve una criatura del pool por creatureId+playerId
├── get-queue-status.js               # devuelve creatureIds del caller que están realmente en el pool. 1 read
├── process-matchmaking.js            # SCHEDULED: drena pool, empareja, simula, escribe combat_results
├── run-combat.js                     # MODO INSTANT: enqueue + match + simulate en una sola llamada
├── matchmaking-tick.sched            # Scheduler: cron "0 * * * *" → emite "process-matchmaking.v1"
└── matchmaking-trigger.tr            # Trigger: escucha el evento → invoca process-matchmaking

Assets/RunRunSimulator/Scripts/Systems/Combat/
├── CombatService.cs                  # static: Simulate() — combate local por turnos. Emite CombatTurn + CombatRecord en ambos
├── CombatController.cs               # MonoBehaviour: UI local combat + Async Combat (Instant + Timer)
└── AsyncCombatService.cs             # MonoBehaviour: EnqueueInstantAsync / EnqueueScheduledAsync / PollResultsAsync / FetchQueuedIdsAsync / FetchPendingResultIdsAsync / DequeueAsync

Assets/RunRunSimulator/Scripts/Data/
├── CombatManagerSO.cs                # SO singleton: EvolutionChance, DeathChance, CritChance, MaxRounds, MaxFightCount(5)
├── CombatResult.cs                   # Data (local): WinnerID, LoserID, Log, LoserDied, WinnerEvolved, IsDraw + Turns
├── CombatRecord.cs                   # Data: CombatRecord (POV de UNA criatura) + CombatTurn. Replayable
└── CombatLogEntry.cs                 # Data (display): CreatureId/Name, OpponentLabel, Lines, Won, Died
```
