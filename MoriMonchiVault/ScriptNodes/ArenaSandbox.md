---
tags: [script, world, expedition, sandbox]
---

# ArenaSandbox.cs

**Ruta:** `World/Expedition/ArenaSandbox.cs`

**Responsabilidad:** Escena sandbox de arena que encapsula flujo: BuildRoom (layout, minerales, pizarrones, planner.Prepare) → SpawnCast (spawnea agentes con habilidades) → ResetRoom (limpia/nueva semilla). Delegados: ArenaCastPlanner (elenco), ArenaPaletteApplier (paletas), ArenaLayoutBuilder (layout). S103: pizarrones. S104: órdenes, lectura de sala. S105: SetSeed para harness automatizado. **S107:** resuelve habilidades por DNA y asigna a cada agente.

**Métodos públicos:**
- `void BuildRoom()` — construye sala (layout, minerales, exits, pizarrones)
- `void SpawnCast()` — spawnea elenco planeado con habilidades resueltas
- `void ResetRoom(bool newSeed)` — limpia cast/minerales/exits, opcionalmente genera nueva semilla
- `void SetSeed(int value)` — fija seed y apaga randomizeEachPlay (usado por ArenaMatrixDev para reproducibilidad)
- `void SetPlayerOrders(int index, ArenaOrders orders)` — inyecta órdenes a creature índice player side
- `void SetOrders(int index, ArenaOrders orders)` — inyecta órdenes a creature índice (ambos teams)
- `void SetCastMode(ArenaCastMode mode)` — alterna Roster/LocalSave
- `void ShuffleCast()` — nueva selección aleatoria
- `void SelectLocalCast(IReadOnlyList<CreatureDNA> picks)` — selección explícita del picker
- `void SetPaletteIndex(int index)` — fija paleta por índice
- `void CyclePalette()` — alterna paleta circular
- `ArenaRoomRead ReadRoom(ExpeditionTeam team)` — fotografía de sala (distancias, obstáculos, botín) desde perspectiva del team

**Propiedades:**
- `IReadOnlyList<MoriMonchiController> Spawned { get; }`
- `IReadOnlyList<ExitZone> Exits { get; }`
- `IReadOnlyList<ArenaCastEntry> PlannedCast { get; }`
- `IReadOnlyList<CreatureDNA> LocalPool { get; }`
- `int ActiveSeed { get; }` — semilla activa (usado por RunLoop)
- `ArenaCastMode CastMode { get; }`
- `bool LocalCastAvailable { get; }`
- `string EntryName { get; }` — nombre de entrada (desde layout si existe)
- `string PaletteName { get; }` — nombre de paleta activa

**Campos Serializados (S107):**
- `abilityDatabase` (AbilityDatabaseSO) — banco de habilidades, resuelve por partes del DNA

**BuildRoom (S104-S105):**
1. Setea activeSeed = randomizeEachPlay ? Environment.TickCount : seed (para reproducibilidad con SetSeed)
2. Layout.Build(activeSeed, filter)
3. Palette.ApplyIndex()
4. SpawnExits() si Planner.HasRoster
5. SpawnMinerals() y marca lode central
6. Pizarrones: BoardFor(Player/Rival).SetSites(minerals)
7. Planner.Prepare(activeSeed, castSeed, count)

**SpawnCast (S104, S107):**
- Para cada entry en PlannedCast:
  - SpawnCreature(entry.Dna, ..., entry.Team, entry.Orders)
  - agent.Initialize() con profileTable
  - **S107:** agent.SetAbilities(abilityDatabase.Resolve(entry.Dna)) — resuelve [Horn, Wings, Back]
  - agent.SetOrders(entry.Orders)
  - agent.SetBlackboard(BoardFor(entry.Team))
  - agent.SetGuardPost(ResolveSite(entry)) — post inicial según ArenaSite

**ReadRoom (S104):**
- Retorna ArenaRoomRead con:
  - LodeValue (central)
  - VeinCount, VeinTotal (vetas)
  - Obstacles (conteo)
  - CenterDistance (exit → lode)
  - NearVeinDistance (exit → veta cercana)

**Campos Privados:**
- `planner` (ArenaCastPlanner lazy) — con localCastCount serializado
- `boards` (Dictionary<ExpeditionTeam, TeamBlackboard>)
- `spawned`, `minerals`, `exits` (Lists)
- `activeSeed` — semilla reproducible actual
- `seed`, `randomizeEachPlay` — serializados en inspector

**S105 Cambios:**
- `SetSeed(int value)` fija seed y apaga randomizeEachPlay → reproducibilidad para harness
- `activeSeed` guardado y usado por RunLoop/ArenaMatrixDev

**S107 Cambios:**
- Campo `abilityDatabase` [Required] de tipo AbilityDatabaseSO
- En SpawnCast(): tras Initialize(), llama `agent.SetAbilities(abilityDatabase.Resolve(entry.Dna))`
- Permite que cada criatura tenga combo único de habilidades según partes genéticas
- abilityDatabase.Resolve() retorna array [Horn, Wings, Back] determinísticamente

**Invariantes:**
- `activeSeed` determinado al BuildRoom (no puede cambiar mid-simulation)
- randomizeEachPlay toggle controla si ignora seed o genera random
- SetSeed() se usa antes de BuildRoom() para fijar semilla en harness
- abilityDatabase puede ser null; si es así, agents tienen abilities = [null, null, null]

**Vinculado a:** [[Index/23 - Arena Sandbox y Expedicion]]

**Conexiones:** [[ArenaCastPlanner]], [[ArenaPaletteApplier]], [[ArenaLayoutBuilder]], [[ArenaMatrixDev]], [[ExpeditionRulesSO]], [[TeamBlackboard]], [[MoriMonchiController]], [[MoriMochiAgent]], [[ArenaOrders]], [[AbilityDatabaseSO]], [[MaterialPickup]], [[ExitZone]]
