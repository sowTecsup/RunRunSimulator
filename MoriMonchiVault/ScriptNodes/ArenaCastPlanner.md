---
tags: [script, world, expedition, planning]
---

# ArenaCastPlanner.cs

**Ruta:** `World/Expedition/ArenaCastPlanner.cs`

**Responsabilidad:** Planificador de elenco de arena que construye la lista de criaturas a spawnear según modo (Roster vs LocalSave), aplica planes rivales por semilla, remembers cambios de ocupación/sitio del jugador (S103). Soporta selección explícita de criaturas locales vía `SelectLocal()` (S103 NUEVO), complementado por `ArenaCastPicker` UI. `LocalPool` propiedad pública (S103 NUEVO) para exposición a picker.

**Métodos públicos:**
- `Prepare(int roomSeed, int castSeed, int freeCount)` — construye `planned`
- `SetPlayerPlan(int index, Occupation occupation, ArenaSite site)` — actualiza entry[index] + remembered
- `SelectLocal(IReadOnlyList<CreatureDNA> picks)` — (S103 NUEVO) carga selección explícita de picker (capped a LocalCount)
- `ClearLocalSelection()` — (S103 NUEVO) limpia selección local
- `IReadOnlyList<CreatureDNA> LocalPool { get; }` — (S103 NUEVO) propiedad para acceso a pool local

**Propiedades:**
- `Planned → IReadOnlyList<ArenaCastEntry>` — elenco final
- `Mode` (ArenaCastMode) — Roster o LocalSave
- `LocalCount` (int) — cuántas criaturas de LocalSave (default 3)
- `LocalAvailable` (bool) — si LocalSave tiene criaturas
- `HasRoster` (bool) — si roster != null && entries.Count > 0
- `HasLocalSelection` (bool) — (S103 NUEVO) si localSelection.Count > 0

**Campos Privados:**
- `localSelection` (List<CreatureDNA>) — (S103 NUEVO) selección explícita del picker
- `localPool` (List<CreatureDNA>) — cache lazy del pool local

**Constantes de Datos (S103 actualizado):**

**RivalPlans (7 patrones con Explore S103):**
```
0: [Guard, Gather, Gather]
1: [Break, Gather, Gather]
2: [Decoy, Guard, Gather]
3: [Gather, Gather, Gather]
4: [Break, Decoy, Gather]
5: [Explore, Gather, Gather]     ← S103 NUEVO
6: [Guard, Explore, Gather]       ← S103 NUEVO
```

**GatherSites:** [Center, NearVein, FarVein]

Rival i recibe `RivalPlans[roomSeed % 7][i % 3]` y site según occupation (Gather → GatherSites[i % 3], else Center).

**Flujo Prepare (S103 actualizado):**
1. Limpia planned, seed RNG, marca LocalAvailable=true
2. Si no HasRoster: agrega freeCount minted entries, retorna
3. Si Mode=LocalSave:
   - Si `localSelection.Count > 0`: usa localSelection (picker)
   - Si no: Pick(LocalPool, LocalCount, castSeed)
   - Para cada: Remembered() → restaura cambios previos
   - Team=Player, Occupation=Gather, Site=Center
4. Si Mode=Roster o !LocalAvailable:
   - Itera roster.Entries con Team=Player
   - Remembered() → restaura cambios previos
5. Construye rivales:
   - plan = RivalPlans[Abs(roomSeed) % 7] (S103: 7 no 5)
   - rivalIndex = 0
   - Para cada roster.Entries con Team=Rival:
     - occupation = plan[rivalIndex % 3]
     - site = occupation==Gather ? GatherSites[rivalIndex % 3] : Center
     - FromRoster() → agrega a planned
     - rivalIndex++

**S103 Cambios:**
- RivalPlans expandido de 5 a 7 patrones (agregó Explore combinaciones)
- `SelectLocal(IReadOnlyList<CreatureDNA> picks)` — permite picker pasar selección directamente
- `ClearLocalSelection()` — limpia si necesario
- `LocalPool` propiedad pública (antes era método privado Pool())
- `HasLocalSelection` propiedad para verificar si hay picks del picker
- Prepare ahora prioriza `localSelection` si está poblada

**Integración S103:**
1. `ArenaCastPicker` llama `sandbox.SelectLocalCast(selection)`
2. Sandbox llama `planner.SelectLocal(selection)`
3. Próximo `Prepare()` usa localSelection en lugar de Pick aleatorio
4. `ClearLocalSelection()` disponible para "Shuffle" button si desea retornar a aleatorio

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaCastSource]], [[ArenaRosterSO]], [[ArenaCastEntry]], [[ArenaSandbox]], [[ArenaCastPicker]], [[CreatureDNA]], [[Occupation]], [[ArenaSite]], [[ExpeditionTeam]]
