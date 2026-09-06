---
tags: [script, world, expedition, sandbox]
---

# ArenaSandbox.cs

**Ruta:** `World/Expedition/ArenaSandbox.cs`

**Responsabilidad:** Escena de pruebas `ArenaSandbox.unity` para observar comportamientos emergentes. Encapsula flujo: BuildRoom (layout, paleta, minerales, planner.Prepare) → SpawnCast (itera PlannedCast, spawnea agentes) → ResetRoom (limpia elenco/minerales/salidas, opcionalmente nueva semilla). Delegado de elenco: ArenaCastPlanner. Delegado de paleta: ArenaPaletteApplier. Activa/desactiva ExpeditionRulesSO en ciclo de escena.

## Campos Serializados

**Referencias requeridas:**
- `creaturePrefab` (MoriMonchiController)
- `profileTable` (RoleWorldProfileSO)
- `socialTuning` (SocialTuningSO)
- `expeditionRules` (ExpeditionRulesSO) — se activa/desactiva en OnEnable/OnDisable
- `clashTuning` (ClashTuningSO)
- `visualBank` (MonchiVisualBankSO)
- `furDatabase` (FurTypeDatabaseSO)
- `creatureDatabase` (CreatureDatabaseSO)

**Configuración de escena:**
- `observer` (Transform) — cámara/punto focal
- `targetGroup` (CinemachineTargetGroup) — grupo de tracking
- `spawnCenter` (Transform) — centro de arena

**Configuración de seed:**
- `seed` (int, default 4242)
- `castSeed` (int, default 1) — seed secundaria para elenco
- `randomizeEachPlay` (bool) — si true, ignora seed

**Configuración de elenco (S102 delegado a ArenaCastPlanner):**
- `roster` (ArenaRosterSO) — tabla de criaturas
- `castMode` (ArenaCastMode, default Roster) — Roster vs LocalSave
- `localCastCount` (int, default 3)
- `autoSpawnCast` (bool, default true) — si true, SpawnCast() en Start
- `teamSpawnInset` (float, default 9) — distancia de esquina a spawn
- `teamSpawnRadius` (float, default 2.5) — radio de scatter spawn
- `exitPrefab` (ExitZone, Required)
- `exitInset` (float, default 4)

**Configuración de sala:**
- `mineralPrefab` (MaterialPickup)
- `layout` (ArenaLayoutBuilder) — generador de obstáculos/vetas
- `palette` (ArenaPaletteApplier) — gestor de paletas
- `paletteIndex` (int, default -1) — índice paleta (-1 = por semilla)
- `centerMineralScale`, `centerMineralValue` (float/int)
- `arenaHalfSize` (float, default 20)

**Otros:**
- `keepNeedsFull` (bool, default true) — si true, Health/Energy/Affect = 100
- `count` (int, default 3) — cantidad de criaturas fallback

## Propiedades Públicas

- `Spawned → IReadOnlyList<MoriMonchiController>` — criaturas vivas
- `Exits → IReadOnlyList<ExitZone>` — salidas por equipo
- `PlannedCast → IReadOnlyList<ArenaCastEntry>` — elenco planeado (desde Planner)
- `ActiveSeed → int` — semilla activa
- `CastMode → ArenaCastMode` — modo elenco (Roster/LocalSave)
- `LocalCastAvailable → bool` — si LocalSave tiene criaturas
- `EntryName → string` — nombre del eje de entrada
- `PaletteName → string` — nombre de paleta activa
- `ExitFor(ExpeditionTeam team) → ExitZone` — getter de salida por equipo

## Privadas

- `planner` (ArenaCastPlanner) — gestor lazy de elenco
- `activeSeed`, `center`, `rng`, `filter` — estado de sesión
- `roomBuilt` (bool) — si BuildRoom completó
- `spawnHolder` (Transform) — padre de spawns
- `minerals`, `exits` (List) — listas de instancias

## Ciclo de Vida (S102)

**OnEnable():**
- ExpeditionRulesSO.Activate(expeditionRules) — establece Current

**OnDisable():**
- ExpeditionRulesSO.Deactivate(expeditionRules) — borra Current

**Start():**
- BuildRoom()
- Si autoSpawnCast: SpawnCast()

**Update():**
- Si keepNeedsFull: mantiene Health/Energy/Affect = 100 en todos

## Métodos Públicos (S102 refactor)

**BuildRoom() → void** — construye sala sin criaturas:
1. Inicializa spawnHolder (GameObject lazy)
2. activeSeed = randomizeEachPlay ? TickCount : seed
3. NavMeshQueryFilter por agentTypeID
4. layout.Build(activeSeed, filter)
5. palette.ApplyIndex (si paletteIndex ≥ 0, else por semilla)
6. Si Planner.HasRoster: SpawnExits()
7. SpawnMinerals()
8. Planner.Prepare(activeSeed, castSeed, count)
9. roomBuilt = true

**SpawnCast() → void** — spawnea elenco planeado:
1. Si !roomBuilt: BuildRoom()
2. Si spawned.Count > 0: ClearCast()
3. Para cada entry en PlannedCast:
   - around = (Team==None ? center : TeamCorner), radius = (Team==None ? spawnRadius : teamSpawnRadius)
   - controller = SpawnCreature(entry.Dna, around, radius, entry.Team, entry.Occupation, ExitFor(entry.Team))
   - controller.Agent.SetGuardPost(ResolveSite(entry))

**ClearCast() → void** — destruye elenco vivo

**ResetRoom(bool newSeed) → void** — limpia total y reconstruye:
1. ClearCast()
2. Destruye minerals y loose Materials (raycast PerceivableRegistry)
3. Destruye exits
4. layout.Clear()
5. Si newSeed: genera seed random nuevo, randomizeEachPlay = false
6. BuildRoom()

**SetPlayerPlan(int index, Occupation occupation, ArenaSite site) → void**
- Delega: Planner.SetPlayerPlan()

**SetCastMode(ArenaCastMode mode) → void**
- Planner.SetMode(mode)
- Planner.Prepare() con seed actual

**ShuffleCast() → void**
- castSeed++
- Planner.Prepare() (new elenco con nuevo castSeed)

**SetPaletteIndex(int index) → void**
- paletteIndex = index
- palette.ApplyIndex(index)

**CyclePalette() → void**
- NextIndex = (CurrentIndex + 1) % Palettes.Count
- SetPaletteIndex(NextIndex)

**[Button] Respawn() → void**
- ResetRoom(false) + SpawnCast()

**[Button] Reseed() → void**
- ResetRoom(true) + SpawnCast()

## Métodos Privados

**Planner (property lazy):**
- Si null: crea ArenaCastPlanner(useRoster ? roster : null, MintRandom)
- Retorna instancia

**SpawnExits() → void** — spawnea ExitZones por equipo

**SpawnMinerals() → void** — siembra minerales

**SpawnCreature(...) → MoriMonchiController** — instancia e inicializa agente

**ResolveSite(ArenaCastEntry) → Vector3** — mapea ArenaSite a GuardPost (layout veta center/near/far)

**TeamCorner(ExpeditionTeam) → Vector3** — esquina según equipo

**MintRandom() → CreatureDNA** — crea DNA aleatorio

## Invariantes S102

- **BuildRoom determinístico:** seed fijo produce layout/paleta idénticos
- **Planner lazy:** se crea al primer acceso, se reutiliza
- **ExpeditionRulesSO.Current activo:** disponible para AgentSenses/AgentExpedition mientras escena activa
- **Transición BuildRoom → SpawnCast → ResetRoom:** orden estricto
- **PlannedCast desde Planner:** sandbox no construye elenco, solo accede
- **ResetRoom(newSeed):** opción para cambiar seed sin resetear sandbox

## Conexiones

- [[ArenaCastPlanner]] (Planner lazy, Prepare, SetPlayerPlan)
- [[ArenaPaletteApplier]] (palette, ApplyIndex)
- [[ArenaLayoutBuilder]] (layout, Build, Veins)
- [[ExpeditionRulesSO]] (Activate/Deactivate en OnEnable/OnDisable)
- [[ArenaCastEntry]] (data de PlannedCast)
- [[MoriMonchiController]], [[MoriMonchiAgent]] (spawned)
- [[ExitZone]] (exits)
- [[MaterialPickup]] (minerals)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
