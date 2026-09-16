---
tags: [script, world, expedition, sandbox]
---

# ArenaSandbox.cs

**Ruta:** `World/Expedition/ArenaSandbox.cs`

**Responsabilidad (S120-S122):** Escena sandbox de arena que encapsula flujo: BuildRoom (layout con forma, minerales, pizarrones, planner.Prepare) → SpawnCast → ResetRoom. **S120:** Lee SelectedIds de ExpeditionHandoff; si no vacío, fuerza esas 3 criaturas del jugador. **S122:** AsignaRole a rivales (que abre 2 bases). Prefab MorimonchiAgent con Feedbacks OnDiveLaunch/OnDiveSlam.

**Métodos clave:**
- `void BuildRoom()` — construye sala. **S120:** Planner.Prepare(activeSeed, castSeed, count) respeta SelectedIds
- `void SpawnCast()` — spawnea elenco. **S122:** Lee Role de planned, asigna base abierta (de ArenaBases.Default o seleccionada)
- `void ResetRoom(bool newSeed)` — limpia cast/minerales/exits

**Propiedades:**
- `bool HasTeams { get; }` — true si LocalSave o HasRoster (para SpawnExits())
- `IReadOnlyList<MoriMonchiController> Spawned { get; }`
- `int ActiveSeed { get; }`

**Campos Serializados:**
- `expeditionRules` (ExpeditionRulesSO)
- `abilityDatabase` (AbilityDatabaseSO)
- `roster` (ArenaRosterSO) — puede ser null

**S119:** Planner creado con mint callback MintRandom (no registry).

**S120-S122:**
- **S120:** BuildRoom → Planner.Prepare respeta ExpeditionHandoff.SelectedIds (bloquea elenco). Si hay IDs, nueva sala (MinDistance). Rivales minteados con `Timestamp + i + 1`.
- **S121:** Sin cambios en ArenaSandbox (energía gastada en ExpeditionBridge).
- **S122:** SpawnCast → agentes reciben Role del planned.Dna; prefab MorimonchiAgent expone `onDiveLaunch` y `onDiveSlam` feedbacks (OnDiveLaunch prefab GO en Feedbacks/, OnDiveSlam idem).

**Invariantes:**
- SelectedIds no vacío = elenco forzado (3 criaturas, libre + 2 rivales minteados)
- HasTeams determina si SpawnExits() corre
- Role asignado a través del DNAgeneration, no modificable en BuildRoom

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]], [[Index/22 - Bajada Nocturna y Linaje]]

**Conexiones:** [[ExpeditionHandoff]], [[ArenaCastPlanner]], [[ArenaBases]], [[MoriMochiAgent]]
