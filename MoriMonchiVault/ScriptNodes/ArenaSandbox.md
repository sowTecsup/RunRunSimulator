---
tags: [script, world, expedition, sandbox]
---

# ArenaSandbox.cs

**Ruta:** `World/Expedition/ArenaSandbox.cs`

**Responsabilidad:** Escena sandbox de arena que encapsula flujo completo: BuildRoom (layout, paleta, minerales, pizarrones, planner.Prepare) → SpawnCast (itera PlannedCast, spawnea agentes con inyección de pizarrón) → ResetRoom (limpia elenco/minerales/salidas, opcionalmente nueva semilla). Delegados: `ArenaCastPlanner` (elenco), `ArenaPaletteApplier` (paletas), `ArenaLayoutBuilder` (layout). Activa/desactiva `ExpeditionRulesSO` en ciclo. **S103:** Expone `LocalPool` pública, `SelectLocalCast()` para picker, `BoardFor(team)` para pizarrones.

**Métodos públicos:**
- `BuildRoom()` — construye sala sin criaturas (layout, paleta, minerales, pizarrones, Prepare planner)
- `SpawnCast()` — spawnea elenco planeado, inyecta pizarrón a agentes por team
- `ResetRoom(bool newSeed)` — limpia total, opcionalmente nueva semilla
- `SetPlayerPlan(int index, Occupation occupation, ArenaSite site)` — delega a Planner
- `SetCastMode(ArenaCastMode mode)` — alterna Roster/LocalSave, Prepare
- `ShuffleCast()` — castSeed++, Prepare (new elenco aleatorio)
- `SelectLocalCast(IReadOnlyList<CreatureDNA> picks)` — (S103 NUEVO) picker → SelectLocal + Prepare
- `SetPaletteIndex(int index)` — aplica paleta
- `CyclePalette()` — siguiente paleta

**Propiedades Públicas:**
- `Spawned → IReadOnlyList<MoriMonchiController>`
- `Exits → IReadOnlyList<ExitZone>`
- `PlannedCast → IReadOnlyList<ArenaCastEntry>`
- `ActiveSeed → int`
- `CastMode → ArenaCastMode`
- `LocalCastAvailable → bool`
- `LocalPool → IReadOnlyList<CreatureDNA>` — (S103 NUEVO) propiedad pública para picker
- `EntryName → string`
- `PaletteName → string`

**Métodos Nuevos S103:**
- `TeamBlackboard BoardFor(ExpeditionTeam team)` — retorna o crea pizarrón por team, lazy-instantiated
- `SelectLocalCast(IReadOnlyList<CreatureDNA> picks)` — llama `Planner.SelectLocal(picks)` + `Prepare()` (integración con ArenaCastPicker)

**Campos Serializados:**
- Refs core: `creaturePrefab`, `profileTable`, `socialTuning`, `expeditionRules`, `clashTuning`, `visualBank`, `furDatabase`, `creatureDatabase`
- Escena: `observer`, `targetGroup`, `spawnCenter`
- Elenco (delegado a Planner): `roster`, `castMode`, `localCastCount`, `autoSpawnCast`, `teamSpawnInset`, `teamSpawnRadius`, `exitPrefab`
- Sala: `mineralPrefab`, `layout`, `palette`, `paletteIndex`, `centerMineralScale`, `centerMineralValue`, `arenaHalfSize`

**Privados:**
- `planner` (ArenaCastPlanner lazy)
- `boards` (Dictionary<ExpeditionTeam, TeamBlackboard>) — (S103 NUEVO) pizarrones
- `spawned`, `minerals`, `exits` (Lists)
- `activeSeed`, `center`, `rng`, `roomBuilt`

**BuildRoom S103:**
1. Setup spawnHolder, activeSeed, center, filter
2. layout.Build(), palette.Apply()
3. SpawnExits()
4. SpawnMinerals()
5. **S103 NUEVO:** `BoardFor(Player).SetSites(minerals)` + `BoardFor(Rival).SetSites(minerals)` — inicializa pizarrones
6. Planner.Prepare()

**SpawnCast S103:**
- Para cada entry en PlannedCast:
  - SpawnCreature(entry.Dna, around, radius, entry.Team, entry.Occupation, ExitFor(entry.Team))
  - **S103 NUEVO:** `controller.Agent.SetBlackboard(BoardFor(entry.Team))` — inyecta pizarrón al agente

**S103 Cambios:**
- `BoardFor(ExpeditionTeam team)` lazy-dictionary de pizarrones (creados on-demand)
- `LocalPool` propiedad pública (antes privada)
- `SelectLocalCast()` nuevo, complementa `ArenaCastPicker` (picker llama esto tras confirmar)
- `ShuffleCast()` ahora llama `Planner.ClearLocalSelection()` para resetear selección
- Pizarrones inicializados en BuildRoom y limpiados en ResetRoom (BoardFor lazy crea nuevo)

**Integración S103:**
1. ArenaCastPicker.Open() accede `sandbox.LocalPool`
2. Picker.Confirm() llama `sandbox.SelectLocalCast(selection)`
3. SelectLocalCast inyecta selección en planner y re-prepara
4. Pizarrones poblados en BuildRoom, inyectados en SpawnCast
5. Scouts consultan pizarrón para exploración inteligente

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaCastPlanner]], [[ArenaPaletteApplier]], [[ArenaLayoutBuilder]], [[ExpeditionRulesSO]], [[ArenaCastPicker]], [[TeamBlackboard]], [[MoriMonchiController]], [[MoriMochiAgent]], [[ArenaRosterSO]], [[MaterialPickup]], [[ExitZone]]
