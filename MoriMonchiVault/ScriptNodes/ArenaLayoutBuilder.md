---
tags: [script, world, expedition, procedural, generation]
---

# ArenaLayoutBuilder.cs

**Ruta:** `World/Expedition/ArenaLayoutBuilder.cs`

**Responsabilidad:** Generador proceduralista de topografía de arena por semilla. Construye obstáculos (árboles/rocas Synty), rebakea NavMesh, genera vetas mineras validadas contra NavMesh. Expone Veins (posición+capacidad), entrada/salida, puntos spawn. Simetría central (mirror) opcional. S104: ObstacleCount para UI.

**Propiedades Públicas:**
- `IReadOnlyList<VeinSpot> Veins { get; }` — vetas generadas
- `bool IsBuilt { get; }` — si generatedRoot != null
- `Vector3 EntryDirection { get; }` — eje de entrada normalizado
- `string EntryName { get; }` — nombre eje
- `Vector3 EntryPoint(ExpeditionTeam team, float inset)` → Vector3
- `Vector3 ExitPoint(ExpeditionTeam team)` → Vector3
- `Vector3 SpawnPoint(ExpeditionTeam team)` → Vector3
- `int ObstacleCount { get; }` — (S104 NUEVO) total de obstáculos colocados (árboles + rocas)

**Métodos Públicos:**
- `void Build(int seed, NavMeshQueryFilter filter)` — genera layout:
  1. Clear()
  2. entryAxis = seed % 4 (selecciona eje 0-3)
  3. BuildObstacles(rng)
  4. surface.BuildNavMesh()
  5. BuildVeins(rng, filter)
- `void Clear()` — destruye generatedRoot, limpia listas

**Invariantes:**
- RNG seeded: determinístico por seed
- Eje entrada: seed % 4 selecciona 1 de 4 ejes (diagonal, diagonal inversa, Z, X)
- Simetría central: mirror=true espeja [-x,-z]
- NavMesh pre-bake: BuildVeins ocurre DESPUÉS de BuildNavMesh
- VeinSpot.Position proyectado al NavMesh (no candidato original)
- 40 intentos máximo por ubicación de obstáculo/veta
- ObstacleCount caché en BuildObstacles (usado por ArenaPlanPanel para descripción)

**Struct:**
```csharp
public struct VeinSpot { Vector3 Position; int Capacity; }
```

**S104 Cambios:**
- ObstacleCount property agregada
- Usado por ArenaOrderCatalog.RoomText() para describir sala ("cubierta (11 obstáculos)", "abierta (4)")

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[MaterialPickup]], [[NavMeshSurface]], [[ArenaOrderCatalog]], [[ArenaPlanPanel]]
