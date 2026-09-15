---
tags: [script, world, expedition, sandbox]
---

# ArenaSandbox.cs

**Ruta:** `World/Expedition/ArenaSandbox.cs`

**Responsabilidad:** Escena sandbox de arena que encapsula flujo: BuildRoom (layout con forma, minerales, pizarrones, planner.Prepare) → SpawnCast (agentes con habilidades resueltas) → ResetRoom (limpia/nueva semilla). Delegados: ArenaCastPlanner (elenco), ArenaPaletteApplier (paletas), ArenaLayoutBuilder (layout + landmarks S113). S103: pizarrones. S104: órdenes, lectura de sala. S105: SetSeed. S107: habilidades por DNA. S111: center desde layout, ShapeName display, log forma. S113: integración de landmarks grandes, rayos de luz (shafts), bosque circundante. S114: Discard/limpieza por jerarquía, SpawnCast solo en Play, ExitPoint == SpawnPoint. **S118:** AbilityDatabaseSO resuelve abilities por partes (Horn/Wings/Back) y las asigna a agentes vía agent.SetAbilities().

**Métodos públicos:**
- `void BuildRoom()` — construye sala (layout, minerales, exits, pizarrones, landmarks)
- `void SpawnCast()` — spawnea elenco planeado con habilidades resueltas (solo si Application.isPlaying, S114)
- `void ResetRoom(bool newSeed)` — limpia cast/minerales/exits por barrido de jerarquía (S114), opcionalmente genera nueva semilla
- `void SetSeed(int value)` — fija seed y apaga randomizeEachPlay (S105)
- `void SetPlayerOrders(int index, ArenaOrders orders)` — inyecta órdenes a creature player side
- `void SetOrders(int index, ArenaOrders orders)` — inyecta órdenes a creature (ambos teams)
- `void SetCastMode(ArenaCastMode mode)` — alterna Roster/LocalSave
- `void ShuffleCast()` — nueva selección aleatoria
- `void SelectLocalCast(IReadOnlyList<CreatureDNA> picks)` — selección explícita del picker
- `void SetPaletteIndex(int index)` — fija paleta por índice
- `void CyclePalette()` — alterna paleta circular
- `ArenaRoomRead ReadRoom(ExpeditionTeam team)` — fotografía de sala (distancias, obstáculos, botín)

**Propiedades Públicas:**
- `IReadOnlyList<MoriMonchiController> Spawned { get; }`
- `IReadOnlyList<ExitZone> Exits { get; }`
- `IReadOnlyList<ArenaCastEntry> PlannedCast { get; }`
- `IReadOnlyList<CreatureDNA> LocalPool { get; }`
- `int ActiveSeed { get; }` — semilla activa
- `ArenaCastMode CastMode { get; }`
- `bool LocalCastAvailable { get; }`
- `string EntryName { get; }` — nombre de entrada (desde layout)
- `string PaletteName { get; }` — nombre de paleta activa
- `Vector3 Center { get; }` — centro de layout.Center (o Vector3.zero si no existe, S111)
- `string ShapeName { get; }` — nombre de forma (layout.ShapeName, o "cuadrado", S111)
- `ArenaShape ActiveShape { get; }` — forma activa (delegada a layout, S113)
- `IReadOnlyList<Vector4> PlacedObstacles { get; }` — hitos grandes (landmarks.Placed, S113)
- `Vector3 SpawnPoint(ExpeditionTeam team)` — punto de spawn (S114: == ExitPoint)
- `Vector3 ExitPoint(ExpeditionTeam team)` — punto de salida (S114: == SpawnPoint, delegado a layout)

**Campos Serializados:**

**Arena Setup:**
- `creaturePrefab` (MoriMonchiController, Required) — prefab de criatura spawneable
- `profileTable` (RoleWorldProfileSO, Required) — tabla de perfiles por rol
- `socialTuning` (SocialTuningSO, Required) — parámetros de socialización
- `expeditionRules` (ExpeditionRulesSO, Required) — reglas de expedición
- `clashTuning` (ClashTuningSO, Required) — parámetros de combate
- **`abilityDatabase`** (AbilityDatabaseSO, S107+S118) — banco de habilidades, resuelve por partes del DNA
- `visualBank` (MonchiVisualBankSO, Required) — banco visual
- `furDatabase` (FurTypeDatabaseSO, Required) — banco de pelajes
- `creatureDatabase` (CreatureDatabaseSO, Required) — base de criaturas

**Spawn Setup:**
- `observer` (Transform) — cámara observadora
- `targetGroup` (CinemachineTargetGroup) — grupo de seguimiento
- `spawnCenter` (Transform) — centro de spawn
- `seed`, `castSeed`, `randomizeEachPlay`, `count`, `spawnRadius`
- `keepNeedsFull` — llenar necesidades al spawn
- `tagShowDistance`, `tagReferenceDistance` — distancia etiqueta

**Cast Setup:**
- `roster` (ArenaRosterSO) — elenco premade
- `useRoster` (bool) — usar roster o local
- `castMode` (ArenaCastMode) — Roster/LocalSave
- `localCastCount` — cantidad si local
- `autoSpawnCast` — auto-spawn tras build
- `teamSpawnInset`, `teamSpawnRadius` — geometría de spawn
- `exitPrefab` (ExitZone, Required) — prefab salida
- `exitInset` — distancia salida

**Layout Setup:**
- `mineralPrefab` (MaterialPickup, Required) — prefab mineral
- `layout` (ArenaLayoutBuilder) — constructor sala
- `palette` (ArenaPaletteApplier) — aplicador paleta
- `paletteIndex` — índice inicial
- `centerMineralScale`, `centerMineralValue` — lode central
- `arenaHalfSize` — tamaño arena

**BuildRoom (S104-S105-S111-S113-S114-S117):**

1. Setea activeSeed = randomizeEachPlay ? Environment.TickCount : seed
2. Layout.Build(activeSeed, filter)
3. Palette.ApplyIndex()
4. **S113:** Palette.SetArenaCenter(Center) — niebla radial centrada
5. SpawnExits() si Planner.HasRoster (S114: ExitPoint == SpawnPoint)
6. SpawnMinerals() y marca lode central
7. Pizarrones: BoardFor(Player/Rival).SetSites(minerals)
8. Planner.Prepare(activeSeed, castSeed, count)
9. **S111:** Debug.Log(f"[ArenaSandbox] Forma={ShapeName} · sala {activeSeed}") — log forma para auditoría
10. **S113:** Landscape ya generado por layout (incluyendo landmarks vía ArenaLayoutBuilder)
11. **S117:** GrassField.ClearAround(PlacedObstacles) — limpia pasto alrededor de spawns y obstáculos

**SpawnCast (S104, S107, S111, S114, S118):**

- **S114:** Ejecuta solo si `Application.isPlaying` (no en editor play mode setup)
- Para cada entry en PlannedCast:
  - SpawnCreature(entry.Dna, ..., entry.Team, entry.Orders)
  - agent.Initialize() con profileTable
  - **S107:** agent.SetAbilities(abilityDatabase.Resolve(entry.Dna)) — resuelve [Horn, Wings, Back] por partes (S118: ahora include Super abilities con carga)
  - agent.SetOrders(entry.Orders)
  - agent.SetBlackboard(BoardFor(entry.Team))
  - agent.SetGuardPost(ResolveSite(entry)) — post inicial según ArenaSite

**ResetRoom (S114 ACTUALIZADO):**

- Limpia por barrido de jerarquía:
  - Destruir spawned (navegar tree, buscar MoriMonchiController)
  - Destruir minerals (idem)
  - Destruir exits (idem)
  - Limpiar listas (spawned, minerals, exits)
- Si newSeed: genera nueva semilla y BuildRoom()

**ReadRoom (S104):**

- Retorna ArenaRoomRead con:
  - LodeValue (central)
  - VeinCount, VeinTotal (vetas)
  - Obstacles (conteo)
  - CenterDistance (exit → lode)

**S118 Cambios:**

- **abilityDatabase.Resolve(entry.Dna)** retorna array [Horn, Wings, Back] de habilidades, ahora incluyendo Super abilities con carga inicial = 0
- Cada agente recibe 3 abilities (una por parte corporal), algunas pueden ser Super (si DefiningParts del DNA incluyen partes con Super)
- Super abilities comienzan sin carga, pero acumulan desde Mined/Secured/Hit en combate

**Integración:**

- Llamado desde Dev Console o Editor UI
- BuildRoom() crea layout, minerales, pizarrones, landmarks
- SpawnCast() crea agentes con habilidades resueltas
- ResetRoom() limpia para nueva sesión o semilla

**Invariantes S117+S118:**

- GrassField limpia pasto alrededor de spawn points para visibilidad
- Abilities resueltas dinámicamente por partes del DNA del agente
- Super abilities comienzan con Charge = 0, no Requested
- Layout y Palette inmutables hasta Reset

**Vinculado a:** [[Index/23 - Arena Sandbox y Expedicion]], [[Index/22 - Bajada Nocturna y Linaje]], S117, S118

**Conexiones:** [[ArenaCastPlanner]], [[ArenaPaletteApplier]], [[ArenaLayoutBuilder]], [[AbilityDatabaseSO]], [[MoriMonchiController]], [[ExitZone]], [[MaterialPickup]], [[ArenaGrassField]], [[ArenaRound]], [[ArenaRoundHud]]
