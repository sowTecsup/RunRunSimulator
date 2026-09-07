---
tags: [script, world, expedition, sandbox]
---

# ArenaSandbox.cs

**Ruta:** `World/Expedition/ArenaSandbox.cs`

**Responsabilidad:** Escena sandbox de arena que encapsula flujo: BuildRoom (layout, minerales, pizarrones, planner.Prepare) → SpawnCast (spawnea agentes) → ResetRoom (limpia/nueva semilla). Delegados: ArenaCastPlanner (elenco), ArenaPaletteApplier (paletas), ArenaLayoutBuilder (layout). S103: pizarrones. **S104: órdenes, lectura de sala, clasificación lode**.

**Métodos públicos:**
- `void BuildRoom()` — construye sala
- `void SpawnCast()` — spawnea elenco
- `void ResetRoom(bool newSeed)` — limpia
- `void SetPlayerOrders(int index, ArenaOrders orders)` — (S104 NUEVO) inyecta órdenes
- `void SetPlayerPlan(int index, Occupation occupation, ArenaSite site)` — (legacy) traduce a órdenes
- `void SetCastMode(ArenaCastMode mode)` — alterna Roster/LocalSave
- `void ShuffleCast()` — nueva selección aleatoria
- `void SelectLocalCast(IReadOnlyList<CreatureDNA> picks)` — (S103) selección explícita del picker
- `void SetLode(MaterialPickup mineral)` — (S104 NUEVO) marca como lode central
- `ArenaRoomRead ReadRoom()` — (S104 NUEVO) fotografía de sala (distancias, obstáculos, botín)

**Propiedades:**
- `IReadOnlyList<MoriMonchiController> Spawned { get; }`
- `IReadOnlyList<ExitZone> Exits { get; }`
- `IReadOnlyList<ArenaCastEntry> PlannedCast { get; }`
- `IReadOnlyList<CreatureDNA> LocalPool { get; }` — (S103)
- `int ActiveSeed { get; }`
- `ArenaCastMode CastMode { get; }`
- `bool LocalCastAvailable { get; }`

**BuildRoom (S104):**
1. Layout, paleta, exits, minerales
2. SetLode(mineralCentral) — marca como lode
3. Pizarrones: BoardFor(Player/Rival).SetSites(minerals)
4. Planner.Prepare()

**SpawnCast (S104):**
- Para cada entry en PlannedCast:
  - SpawnCreature(entry.Dna, ..., entry.Team, entry.Orders) — (S104: órdenes)
  - agent.SetOrders(entry.Orders) — (S104 NUEVO) inyecta órdenes
  - agent.SetBlackboard(BoardFor(entry.Team)) — (S103) pizarrón

**ReadRoom (S104 NUEVO):**
- Retorna ArenaRoomRead con:
  - LodeValue (central)
  - VeinCount, VeinTotal (vetas esquinas)
  - Obstacles (conteo)
  - Distancias (home → lode, home → veta cercana)
- Usado por ArenaPlanPanel para mostrar estadísticas sala

**Campos Privados:**
- `planner` (ArenaCastPlanner lazy)
- `boards` (Dictionary<ExpeditionTeam, TeamBlackboard>) — (S103)
- `spawned`, `minerals`, `exits` (Lists)

**S104 Cambios:**
- SetPlayerOrders() + SetOrders() para inyectar órdenes pre-spawn
- SetLode() clasifica mineral central en MaterialPickup
- ReadRoom() fotografía sala (usado por UI)
- SpawnCreature ahora recibe/aplica ArenaOrders
- Pizarrones inicializados con toda mineral (lode + vetas)

**Invariantes:**
- Pizarrones lazy por team (creados en BuildRoom)
- Lode clasificado antes de SpawnCast
- Órdenes inmutables durante SpawnCast (ya clampeadas)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaCastPlanner]], [[ArenaPaletteApplier]], [[ArenaLayoutBuilder]], [[ExpeditionRulesSO]], [[ArenaCastPicker]], [[TeamBlackboard]], [[MoriMonchiController]], [[MoriMochiAgent]], [[ArenaOrders]], [[MaterialPickup]], [[ExitZone]], [[ArenaPlanPanel]]
