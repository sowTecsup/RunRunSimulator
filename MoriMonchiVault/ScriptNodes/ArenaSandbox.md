---
tags: [script, world, expedition, sandbox]
---

# ArenaSandbox.cs

**Ruta:** `World/Expedition/ArenaSandbox.cs`

**Responsabilidad:** Escena sandbox de arena que encapsula flujo: BuildRoom (layout con forma, minerales, pizarrones, planner.Prepare) → SpawnCast (agentes con habilidades) → ResetRoom (limpia/nueva semilla). Delegados: ArenaCastPlanner (elenco), ArenaPaletteApplier (paletas), ArenaLayoutBuilder (layout + landmarks S113). S103: pizarrones. S104: órdenes, lectura de sala. S105: SetSeed. S107: habilidades por DNA. **S111:** center desde layout, ShapeName display, log forma. **S113:** integración de landmarks grandes, rayos de luz (shafts), bosque circundante.

**Métodos públicos:**
- `void BuildRoom()` — construye sala (layout, minerales, exits, pizarrones, landmarks)
- `void SpawnCast()` — spawnea elenco planeado con habilidades resueltas
- `void ResetRoom(bool newSeed)` — limpia cast/minerales/exits, opcionalmente genera nueva semilla
- `void SetSeed(int value)` — fija seed y apaga randomizeEachPlay (S105)
- `void SetPlayerOrders(int index, ArenaOrders orders)` — inyecta órdenes a creature player side
- `void SetOrders(int index, ArenaOrders orders)` — inyecta órdenes a creature (ambos teams)
- `void SetCastMode(ArenaCastMode mode)` — alterna Roster/LocalSave
- `void ShuffleCast()` — nueva selección aleatoria
- `void SelectLocalCast(IReadOnlyList<CreatureDNA> picks)` — selección explícita del picker
- `void SetPaletteIndex(int index)` — fija paleta por índice
- `void CyclePalette()` — alterna paleta circular
- `ArenaRoomRead ReadRoom(ExpeditionTeam team)` — fotografía de sala (distancias, obstáculos, botín)

**Propiedades:**
- `IReadOnlyList<MoriMonchiController> Spawned { get; }`
- `IReadOnlyList<ExitZone> Exits { get; }`
- `IReadOnlyList<ArenaCastEntry> PlannedCast { get; }`
- `IReadOnlyList<CreatureDNA> LocalPool { get; }`
- `int ActiveSeed { get; }` — semilla activa
- `ArenaCastMode CastMode { get; }`
- `bool LocalCastAvailable { get; }`
- `string EntryName { get; }` — nombre de entrada (desde layout)
- `string PaletteName { get; }` — nombre de paleta activa
- **S111:** `Vector3 Center { get; }` — centro de layout.Center (o Vector3.zero si no existe)
- **S111:** `string ShapeName { get; }` — nombre de forma (layout.ShapeName, o "Plazuela")
- **S113:** `ArenaShape ActiveShape { get; }` — forma activa (delegada a layout)
- **S113:** `IReadOnlyList<Vector4> PlacedObstacles { get; }` — hitos grandes (landmarks.Placed)

**Campos Serializados (S107+):**
- `abilityDatabase` (AbilityDatabaseSO) — banco de habilidades, resuelve por partes del DNA

**BuildRoom (S104-S105-S111-S113):**
1. Setea activeSeed = randomizeEachPlay ? Environment.TickCount : seed
2. Layout.Build(activeSeed, filter)
3. Palette.ApplyIndex()
4. **S113:** Palette.SetArenaCenter(Center) — niebla radial centrada
5. SpawnExits() si Planner.HasRoster
6. SpawnMinerals() y marca lode central
7. Pizarrones: BoardFor(Player/Rival).SetSites(minerals)
8. Planner.Prepare(activeSeed, castSeed, count)
9. **S111:** Debug.Log(f"[ArenaSandbox] Forma={ShapeName} · sala {activeSeed}") — log forma para auditoría
10. **S113:** Landscape ya generado por layout (incluyendo landmarks vía ArenaLayoutBuilder)

**SpawnCast (S104, S107, S111):**
- Para cada entry en PlannedCast:
  - SpawnCreature(entry.Dna, ..., entry.Team, entry.Orders)
  - agent.Initialize() con profileTable
  - S107: agent.SetAbilities(abilityDatabase.Resolve(entry.Dna)) — resuelve [Horn, Wings, Back]
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
- **S113:** `layout` (ArenaLayoutBuilder) — genera landmarks + layout + obstacles
- **S113:** `palette` (ArenaPaletteApplier) — aplica niebla radial

**S105 Cambios:**
- `SetSeed(int value)` fija seed y apaga randomizeEachPlay → reproducibilidad para harness
- `activeSeed` guardado y usado por RunLoop/ArenaMatrixDev

**S107 Cambios:**
- Campo `abilityDatabase` [Required] de tipo AbilityDatabaseSO
- En SpawnCast(): tras Initialize(), llama `agent.SetAbilities(abilityDatabase.Resolve(entry.Dna))`
- Permite combo único de habilidades según partes genéticas

**S111 Cambios:**
- `Center` property (delegada a layout.Center)
- `ShapeName` property (delegada a layout.ShapeName, fallback "Plazuela")
- Debug.Log al BuildRoom con forma (permite auditoría de qué forma se generó)

**S113 Cambios:**
- `ActiveShape` property (delegada a layout.ActiveShape)
- `PlacedObstacles` property (delegada a layout.PlacedObstacles, landmarks.Placed)
- BuildRoom() llama `Palette.SetArenaCenter(Center)` para niebla radial
- ArenaLayoutBuilder ahora coloca landmarks (grandes) + spawns + obstacles

**Invariantes:**
- `activeSeed` determinado al BuildRoom (no cambia mid-simulation)
- randomizeEachPlay toggle controla si ignora seed o genera random
- SetSeed() antes de BuildRoom() para fijar semilla en harness
- abilityDatabase puede ser null; si es así, abilities = [null, null, null]
- Center viene de layout (S111); fallback Vector3.zero
- **S113:** Palette.SetArenaCenter() actualiza niebla radial (no es setter, es update de globals)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaCastPlanner]], [[ArenaPaletteApplier]], [[ArenaLayoutBuilder]], [[ArenaMatrixDev]], [[ExpeditionRulesSO]], [[TeamBlackboard]], [[MoriMonchiController]], [[MoriMochiAgent]], [[ArenaOrders]], [[AbilityDatabaseSO]], [[MaterialPickup]], [[ExitZone]], [[ArenaShape]], [[ArenaLandmarks]]
