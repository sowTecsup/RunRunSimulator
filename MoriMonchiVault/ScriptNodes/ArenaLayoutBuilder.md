---
tags: [script, world, expedition, procedural, generation]
---

# ArenaLayoutBuilder.cs

**Ruta:** `World/Expedition/ArenaLayoutBuilder.cs`

**Responsabilidad:** Generador proceduralista de topografía de arena por semilla. Estructura: árbol root GeneratedLayout → Build(seed, filter) limpia anterior, construye obstáculos (árboles/rocas de Synty con colliders, espejo central opcional), rebakea NavMesh, luego vetas de minería (validadas contra NavMesh post-bake). Struct VeinSpot(Position, Capacity) almacena minerales. Clear() destruye generatedRoot. Expone `Veins` (IReadOnlyList) y `BuiltSeed` para tracking y debug. Desactiva staticObstacles al generar.

## Campos serializados

- **surface:** NavMeshSurface (Required, modo PhysicsColliders)
- **staticObstacles:** GameObject que desactivar durante generación (Environment/Obstacles)
- **treePrefabs:** lista de GameObject prefabs de árboles Synty (6 variantes)
- **rockPrefabs:** lista de GameObject prefabs de rocas Synty (6 variantes)
- **trees:** cantidad de árboles a instanciar (default 6, Min 0)
- **rocks:** cantidad de rocas a instanciar (default 4, Min 0)
- **veins:** cantidad de vetas de mineral (default 4, Min 0)
- **mirror:** si true, simetría central respecto al centro de arena
- **arenaHalfSize:** media dimensión de arena (default 20f, Min 1f) → arena total ±40x40
- **edgeMargin:** margen desde bordes (default 2.5f, Min 0f)
- **clearCenterRadius:** radio prohibido alrededor del centro (default 6f, Min 0f)
- **clearCornerRadius:** radio prohibido alrededor de esquinas (default 6f, Min 0f)
- **obstacleSpacing:** distancia mínima entre obstáculos (default 3.5f, Min 0.5f)
- **treeScale:** rango de escala para árboles (default 0.8–1.3)
- **rockScale:** rango de escala para rocas (default 0.6–1.2)
- **veinMinFromCenter:** distancia mínima veta-centro (default 7f, Min 0f)
- **veinSpacing:** distancia mínima entre vetas (default 8f, Min 0f)
- **veinFromObstacle:** distancia mínima veta-obstáculo (default 2.5f, Min 0f)
- **veinCapacity:** rango de capacidad mineral por veta (default 4–8)

## Struct público

```csharp
public struct VeinSpot
{
    public Vector3 Position;    // Proyectado a NavMesh
    public int Capacity;        // Unidades de mineral
}
```

## Propiedades públicas

- **Veins → IReadOnlyList<VeinSpot>** — lista de vetas generadas (Read-only)
- **BuiltSeed → int** — semilla del último build (Read-only)

## Métodos públicos

- `Build(int seed, NavMeshQueryFilter filter)` — genera layout completo:
  1. Clear()
  2. desactiva staticObstacles
  3. instancia GeneratedLayout root
  4. BuildObstacles(rng, center) → árboles + rocas
  5. surface.BuildNavMesh()
  6. BuildVeins(rng, filter, center) → vetas
  7. logs seed, counts
- `Clear()` — destruye GeneratedLayout, limpia listas

## Flujo de Build

1. **BuildObstacles(rng, center):**
   - BuildObstacleSet(treePrefabs, trees, treeScale)
   - BuildObstacleSet(rockPrefabs, rocks, rockScale)
   
2. **BuildObstacleSet(prefabs, count, scaleRange):**
   - toPlace = mirror ? ceil(count/2) : count
   - para cada i en [0, toPlace):
     - TryFindObstaclePoint(40 intentos) retorna candidato que:
       - distancia(center) ≥ clearCenterRadius
       - no está en esquina (isNearAnyCorner)
       - no choca con otro obstáculo (isNearAnyObstacle)
     - SpawnObstacle(prefab, point, yaw, scale)
     - si mirror: SpawnObstacle(prefab, mirror_point, yaw+180°, scale)

3. **SpawnObstacle(prefab, position, yaw, scale):**
   - Instantiate(prefab, position, Quaternion.Euler(0, yaw, 0), generatedRoot)
   - scale = one * scale_factor
   - agrega position a obstaclePositions (caché para validaciones futuras)

4. **BuildVeins(rng, filter, center):**
   - toPlace = mirror ? ceil(veins/2) : veins
   - para cada i en [0, toPlace):
     - TryFindVeinPoint(40 intentos) retorna candidato que:
       - distancia(center) ≥ veinMinFromCenter
       - no está en esquina
       - distancia a otras vetas ≥ veinSpacing
       - distancia a obstáculos ≥ veinFromObstacle
     - AddVeinIfOnNavMesh(point, capacity, filter):
       - NavMesh.SamplePosition(point, 3m search radius, filter)
       - si hit: VeinSpot { Position = hit.position, Capacity }
     - si mirror: AddVeinIfOnNavMesh(mirror_point, ...)

5. **RandomPointInSquare(rng, center):**
   - retorna punto aleatorio en cuadrado [min, max] donde:
     - min = -arenaHalfSize + edgeMargin
     - max = arenaHalfSize - edgeMargin

## Invariantes S101

- RNG es seeded (determinístico por seed)
- Simetría central: mirror=[0,0] y [-x,-z] alrededor del centro
- NavMesh se rebakea antes de validar vetas (BuildVeins)
- VeinSpot.Position es proyectado a NavMesh (hit.position, no candidato original)
- obstaclePositions es caché de validación (se limpia en Clear())
- GeneratedLayout es única raíz de instancias (se destruye en Clear())
- staticObstacles se desactiva en Build() (no se destruye)
- 40 intentos por ubicación (fallback si no encuentra punto válido)

## Conexiones

**Entrada:**
- Parámetros de Build: seed (int), filter (NavMeshQueryFilter)
- Lectura: transform.position (centro de arena)
- Lectura: treePrefabs, rockPrefabs (listas)

**Salida:**
- Instancias en escena (root GeneratedLayout + children)
- NavMesh rebuilt (surface.BuildNavMesh())
- Propiedad Veins leída por [[ArenaSandbox.SpawnMinerals]]
- Propiedad BuiltSeed para tracking

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[ArenaSandbox]]
- [[MaterialPickup]]
- [[NavMeshSurface]] (Cinemachine)
