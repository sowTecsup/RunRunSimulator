---
tags: [script, world, expedition, planning]
---

# ArenaCastPlanner.cs

**Ruta:** `World/Expedition/ArenaCastPlanner.cs`

**Responsabilidad:** Planificador de elenco de arena que construye lista de criaturas a spawnear según modo (Roster vs LocalSave), aplica planes de órdenes rivales por semilla (S104), remembers cambios del jugador, **S109:** copia IDs de partes genéticas del Roster Entry al DNA. **S119:** Constructor acepta `Func<CreatureDNA> mint`. **S120:** Integra `SelectedIds` del handoff para bloquear equipo elegido.

**Constructor:**
- `ArenaCastPlanner(ArenaRosterSO roster, Func<CreatureDNA> mint, ExpeditionRulesSO rules)` — **S119:** mint es callback para generar DNAs

**Métodos públicos:**
- `void Prepare(int roomSeed, int castSeed, int freeCount)` — construye planned. **S120:** Si SelectedIds no vacío, fuerza jugador a esas 3 criaturas (bloquea Elenco/Elegir/Otros 3)
- `void SetPlayerOrders(int index, ArenaOrders orders)` — (S104) actualiza entry[index].Orders, clampeado
- `void SelectLocal(IReadOnlyList<CreatureDNA> picks)` — (S103) carga selección explícita del picker
- `void SetMode(ArenaCastMode mode)` — (S119) setter explícito
- `IReadOnlyList<string> GetTeamIds(ExpeditionTeam team)` — **(S120)** Retorna lista de UniqueID por equipo

**Propiedades:**
- `IReadOnlyList<ArenaCastEntry> Planned { get; }`
- `ArenaCastMode Mode` — Roster o LocalSave
- `bool HasTeams` — true si modo es LocalSave o HasRoster; false = elenco libre sin equipos
- `int LocalCount`, `bool LocalAvailable`, `bool HasRoster`, `bool HasLocalSelection`
- `IReadOnlyList<CreatureDNA> LocalPool { get; }`

**Flujo Prepare (S104-S119-S120):**
1. Si SelectedIds no vacío (**S120**): busca esas 3 criaturas en registry, carga como Player team
2. Sino: fromRoster/LocalSave, Team=Player, Orders=Default
3. Rival: FromRoster/mint con órdenes de RivalPlans[roomSeed % 6]
4. **S122:** Rival minteado con Role asignado (abierto desde bases)

**S120-S122:**
- **S120:** SelectedIds check en Prepare(). Si hay IDs: LocalPool filtrada a esos UniqueID, bloquea elenco, nueva sala (MinDistance entre rivales). Rivales minteados con `Timestamp + i + 1` para IDs únicos.
- **S121:** Sin cambios en Prepare (energía gastada en ExpeditionBridge).
- **S122:** Prepare() asigna Role a rivales (de RivalPlans[...] clampado por base, no por diales). **S122:** RememberedIds incluyendo rival (para relectura de base).

**Invariantes:**
- Clamping automático en Prepare()
- LocalSelection priorizada sobre aleatorio
- **S120:** SelectedIds + LocalPool = elenco forzado al principio
- **S122:** Role determinístico por roomSeed % 6 + base abierta

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]], [[Index/22 - Bajada Nocturna y Linaje]]

**Conexiones:** [[ExpeditionHandoff]], [[ExpeditionBridge]], [[ArenaSandbox]], [[ArenaBases]], [[ArenaOrderRules]]
