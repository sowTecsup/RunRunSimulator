---
tags: [script, world, expedition, planning]
---

# ArenaCastPlanner.cs

**Ruta:** `World/Expedition/ArenaCastPlanner.cs`

**Responsabilidad:** Planificador de elenco de arena que construye la lista de criaturas a spawnear según modo (Roster vs LocalSave), aplica planes de ocupación rivales por semilla, y remembers cambios de ocupación/sitio del jugador.

## Campos Privados

- `roster` (ArenaRosterSO) — referencia al roster básico (si null → fallback mint)
- `mint` (Func<CreatureDNA>) — callback para crear DNA (fallback)
- `planned` (List<ArenaCastEntry>) — elenco final a spawnear
- `remembered` (Dictionary<string, ArenaCastEntry>) — caché (CustomName → entry) con cambios previos
- `localPool` (List<CreatureDNA>) — pool local cargado una vez (lazy)

## Propiedades Públicas

- `Planned → IReadOnlyList<ArenaCastEntry>` — elenco final
- `Mode` (ArenaCastMode) — Roster o LocalSave (init Roster)
- `LocalCount` (int) — cuántas criaturas cargar de LocalSave (default 3)
- `LocalAvailable` (bool) — si LocalSave tiene criaturas (se actualiza en Prepare)
- `HasRoster` (bool) — si roster != null && entries.Count > 0

## Métodos Públicos

- `Prepare(int roomSeed, int castSeed, int freeCount) → void` — construye `planned`:
  1. planned.Clear()
  2. UnityEngine.Random.InitState(castSeed)
  3. Si no HasRoster: crea freeCount DNAs vía mint() sin roster
  4. Si Mode=LocalSave: Pick(LocalPool(), LocalCount, castSeed) y Remembered()
  5. Si Mode=Roster o !LocalAvailable: itera roster.Entries con Team=Player
  6. Construye plans rivales: selecciona plan de RivalPlans[Abs(roomSeed) % 5]
  7. Itera roster.Entries con Team=Rival → asigna occupation + site por plan

- `SetPlayerPlan(int index, Occupation occupation, ArenaSite site) → void` — actualiza entry[index]:
  - Valida index y Team=Player
  - Cambia Occupation y Site
  - Guarda en remembered[PlanKey]

## Constantes de Datos

**RivalPlans (5 patrones):**
```
0: [Guard, Gather, Gather]
1: [Break, Gather, Gather]
2: [Decoy, Guard, Gather]
3: [Gather, Gather, Gather]
4: [Break, Decoy, Gather]
```

**GatherSites:** [Center, NearVein, FarVein]

Rival i recibe `RivalPlans[roomSeed % 5][i % 3]` y site según occupation (Gather → GatherSites[i % 3], else Center)

## Flujo Prepare S102

```
1. Limpia planned, seed RNG, marca LocalAvailable=true
2. Si no HasRoster:
   - Agrega freeCount entries con Team=None, Occupation=Gather, Site=Center
   - Retorna (fallback mode)
3. Si Mode=LocalSave:
   - Pick() de LocalPool con castSeed
   - Para cada: Remembered() — aplica cambios previos si existen
   - Team=Player, Occupation=Gather, Site=Center
4. Si Mode=Roster o !LocalAvailable:
   - Itera roster.Entries con Team=Player
   - Remembered() — restaura cambios previos
   - FromRoster() clona DNA con stats del entry
5. Construye rivales:
   - plan = RivalPlans[Abs(roomSeed) % 5]
   - rivalIndex = 0
   - Para cada roster.Entries con Team=Rival:
     - occupation = plan[rivalIndex % 3]
     - site = occupation==Gather ? GatherSites[rivalIndex % 3] : Center
     - FromRoster() + agrega a planned
     - rivalIndex++
```

## Invariantes S102

- **Determinístico:** RNG seeded vía castSeed (Pick es reproducible)
- **Remembered entre sesiones:** cambios previos se restauran por CustomName
- **Uno rosado:** FromRoster() clona Stats/BaseColor/BodyShapeID pero calcula Timestamp nuevo
- **LocalPool lazy:** se carga una sola vez (línea 90)
- **Sin mutación de roster:** FromRoster no modifica el asset

## Construcción Típica

```csharp
var planner = new ArenaCastPlanner(roster, () => mint.MinCreateDNA(DNA_random_stats));
planner.SetMode(ArenaCastMode.LocalSave);
planner.Prepare(roomSeed, castSeed, freeCount);
// ... más tarde, cambios del jugador:
planner.SetPlayerPlan(0, Occupation.Guard, ArenaSite.FarVein);
```

## Conexiones

- [[ArenaCastSource]] (LoadLocal + Pick)
- [[ArenaRosterSO]] (Entries)
- [[ArenaCastEntry]] (struct que forma planned)
- [[ArenaSandbox]] (propietario, llama Prepare + SetPlayerPlan)
- [[WorldEnums]] (ArenaCastMode, Occupation, ArenaSite, ExpeditionTeam)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
