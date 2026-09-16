---
tags: [script, world, expedition, sandbox]
---

# ArenaSandbox.cs

**Ruta:** `World/Expedition/ArenaSandbox.cs`

**Responsabilidad:** Escena sandbox de arena que encapsula flujo: BuildRoom (layout con forma, minerales, pizarrones, planner.Prepare) → SpawnCast → ResetRoom. S120: lee SelectedIds de ExpeditionHandoff; si no vacío, fuerza esas 3 criaturas del jugador. S122: asigna Role a rivales (que abre 2 bases). **S124:** Expone FloorKind (Enemies/Buff) y AllMaterialTaken; SetFloor(floorSeed, kind) configura tipo de piso; Buff pisos no spawean salida rival.

**Métodos clave:**

| Método | Descripción |
|--------|-------------|
| `void BuildRoom()` | Construye sala. Planner.Prepare(activeSeed, castSeed, count) respeta SelectedIds |
| `void SpawnCast()` | Spawnea elenco. Lee Role, asigna base abierta |
| `void ResetRoom(bool newSeed)` | Limpia cast/minerales/exits; genera nueva semilla si newSeed=true |
| `void SetFloor(int floorSeed, ArenaFloorKind kind)` | **(S124)** Configura semilla y tipo piso; deshabilita rivales si Buff |
| `void SetCastMode(ArenaCastMode mode)` | Setter explícito de modo (Roster vs LocalSave) |

**Propiedades Públicas:**

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `IReadOnlyList<MoriMonchiController> Spawned { get; }` | readonly list | Criaturas spaweadas activas |
| `IReadOnlyList<ExitZone> Exits { get; }` | readonly list | Salidas (Player, Rival si Enemies) |
| `IReadOnlyList<MaterialPickup> Minerals { get; }` | readonly list | Minerales en escena |
| `int ActiveSeed { get; }` | int | Semilla activa |
| `bool HasTeams { get; }` | bool | True si LocalSave o HasRoster |
| `bool TeamLocked { get; }` | bool | True si SelectedIds no vacío (equipo forzado) |
| `ArenaCastPlanner Planner { get; }` | component | Planificador de elenco |
| `ArenaFloorKind FloorKind { get; }` | enum | **S124:** Tipo piso (Enemies/Buff) |
| `bool AllMaterialTaken { get; }` | bool | **S124:** True si todos los minerales recogidos |

## S124 Cambios

### FloorKind (Enemies vs Buff)

```csharp
public ArenaFloorKind FloorKind { get; private set; } = ArenaFloorKind.Enemies;
```

**Enemigos:** Combate normal, rival presente, salidas ambos equipos.
**Buff:** Recuperación, no hay rival, solo salida del jugador, todo material es gratis.

**Setter:** `SetFloor(int floorSeed, ArenaFloorKind kind)`
```csharp
public void SetFloor(int floorSeed, ArenaFloorKind kind)
{
    seed = floorSeed;
    randomizeEachPlay = false;
    FloorKind = kind;
    Planner.RivalsEnabled = (kind == ArenaFloorKind.Enemies);  // desactiva IA rival si Buff
}
```

### AllMaterialTaken (Verificación de Fin Temprano)

```csharp
public bool AllMaterialTaken
{
    get
    {
        foreach (var mineral in minerals)
            if (mineral == null || !mineral.Taken) return false;
        return true;
    }
}
```

**Propósito:** ArenaRound lo consulta en Update; si Buff && AllMaterialTaken, termina ronda anticipadamente (no espera 90s).

### SpawnExits()

En Buff pisos, solo spawea salida Player; rival exit omitido:
```csharp
if (FloorKind == ArenaFloorKind.Enemies) 
    SpawnExit(ExpeditionTeam.Rival, new Vector3(1f, 0f, 1f));
```

## Ciclo de Vida S124

1. **Start():** Si CameFromStore, llama `SetFloor(FloorSeedOf(RunSeed, 1), Enemies)` → piso 1 de la bajada
2. **BuildRoom():** Construye layout, minerales, planner
3. **SpawnCast():** Genera elenco
4. **Durante ronda:** ArenaRound monitorea FloorKind + AllMaterialTaken
5. **Fin piso:** ArenaRunDirector.Continue() llama `SetFloor(nextFloorSeed, nextFloorKind)` + ResetRoom

## Invariantes S120-S124

- SelectedIds no vacío = elenco forzado (3 criaturas jugador + 2 rivales minteados)
- Buff pisos: RivalsEnabled=false (Planner.RivalsEnabled = false)
- AllMaterialTaken evalúa cada frame (O(n) pero pequeño)
- FloorKind determina lógica de salidas y fin anticipado

## Campos Serializados Clave

| Campo | Descripción |
|-------|-------------|
| `expeditionRules` | ExpeditionRulesSO con parámetros de ocupación |
| `abilityDatabase` | AbilitySO para habilidades de criaturas |
| `roster` | ArenaRosterSO; null si LocalSave |
| `layout` | ArenaLayoutBuilder para geometría |
| `palette` | ArenaPaletteApplier para texturas |

## Vinculado a

[[Index/24 - Puente Tienda-Arena]], [[Index/22 - Bajada Nocturna y Linaje]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ExpeditionHandoff]], [[ArenaCastPlanner]], [[ArenaBases]], [[ArenaRound]], [[ArenaRunDirector]] (S124), [[MoriMochiAgent]], [[WorldEnums]]
