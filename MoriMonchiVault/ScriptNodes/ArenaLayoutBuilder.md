---
tags: [script, world, expedition, procedural, generation]
---

# ArenaLayoutBuilder.cs

**Ruta:** `World/Expedition/ArenaLayoutBuilder.cs`

**Responsabilidad:** Generador proceduralista de topografía de arena por semilla. Estructura: árbol root GeneratedLayout → Build(seed, filter) limpia anterior, selecciona eje de entrada por semilla, construye obstáculos (árboles/rocas Synty con colliders), rebakea NavMesh, luego vetas de minería (validadas contra NavMesh post-bake). Struct VeinSpot(Position, Capacity) almacena minerales. Clear() destruye generatedRoot. Expone Veins (IReadOnlyList), EntryDirection, EntryName, EntryPoint, ExitPoint, SpawnPoint. Desactiva staticObstacles al generar.

## Constantes

**Ejes de entrada (4 totales):**
```csharp
Vector3[] EntryAxes =
{
    new Vector3(1f, 0f, 1f).normalized,    // diagonal (NE-SW)
    new Vector3(-1f, 0f, 1f).normalized,   // diagonal inversa (NW-SE)
    Vector3.forward,                        // norte-sur (Z axis)
    Vector3.right,                          // este-oeste (X axis)
};

string[] EntryNames = 
{ "diagonal", "diagonal inversa", "norte-sur", "este-oeste" };
```

EntryAxis se determina por `seed % 4` en Build().

## Campos Serializados

- **surface:** NavMeshSurface (Required, modo PhysicsColliders)
- **staticObstacles:** GameObject que desactivar durante generación (Environment/Obstacles)
- **staticDecor:** lista de GameObject decorativos a desactivar
- **treePrefabs, rockPrefabs, decorPrefabs:** listas de GameObject prefabs Synty
- **treeCount, rockCount, veinCount, decorClusters:** Vector2Int rangos (min, max) para determinismo por semilla
- **decorPerCluster, decorClusterRadius:** parámetros de agrupación
- **mirror:** si true, simetría central respecto al centro de arena

**Geometría:**
- **arenaHalfSize:** media dimensión de arena (default 20f) → arena ±20x20 en XZ
- **edgeMargin:** margen desde bordes (default 2.5f)
- **clearCenterRadius:** radio prohibido alrededor del centro (default 6f)
- **clearEntryRadius:** radio prohibido alrededor del eje de entrada (default 5f)
- **spawnDistance:** distancia de spawn desde eje (default 8.5f)
- **exitInset:** distancia de salida desde eje (default 4f)
- **obstacleSpacing:** distancia mínima entre obstáculos (default 3.5f)
- **treeScale, rockScale, decorScale:** Vector2 rangos (min, max) de escala

**Vetas:**
- **veinMinFromCenter:** distancia mínima veta-centro (default 7f)
- **veinSpacing:** distancia mínima entre vetas (default 8f)
- **veinFromObstacle:** distancia mínima veta-obstáculo (default 2.5f)
- **veinCapacity:** Vector2Int rango (min, max) de capacidad mineral

## Struct Público

```csharp
public struct VeinSpot
{
    public Vector3 Position;    // Proyectado a NavMesh
    public int Capacity;        // Unidades de mineral
}
```

## Propiedades Públicas

- **Veins → IReadOnlyList<VeinSpot>** — lista de vetas generadas
- **IsBuilt → bool** — si generatedRoot != null
- **EntryDirection → Vector3** — eje de entrada normalizado (depende de entryAxis)
- **EntryName → string** — nombre del eje ("diagonal", etc.)
- **EntryPoint(ExpeditionTeam team, float insetFromBorder) → Vector3** — entrada según equipo (sign = Rival:+1, Player:-1)
- **ExitPoint(ExpeditionTeam team) → Vector3** — ExitPoint(team, exitInset)
- **SpawnPoint(ExpeditionTeam team) → Vector3** — punto de spawn a spawnDistance del eje

## Métodos Públicos

- `Build(int seed, NavMeshQueryFilter filter)` — genera layout completo:
  1. Clear()
  2. Desactiva staticObstacles y staticDecor
  3. entryAxis = seed % 4 (selecciona eje de entrada)
  4. new GameObject("GeneratedLayout") como raíz
  5. Random(seed) para rng determinístico
  6. BuildObstacles(rng) → árboles + rocas
  7. surface.BuildNavMesh()
  8. BuildVeins(rng, filter) → vetas + decorado
  9. logs seed, counts

- `Clear()` — DestroyImmediate(generatedRoot), limpia veins_, obstaclePositions, decorCenters, generatedRoot = null

## Flujo de Build

1. **BuildObstacles(rng, center):**
   - BuildObstacleSet(treePrefabs, treeCount, treeScale)
   - BuildObstacleSet(rockPrefabs, rockCount, rockScale)

2. **BuildObstacleSet(prefabs, countRange, scaleRange):**
   - count = rng.Next(countRange.x, countRange.y + 1)
   - toPlace = mirror ? (count + 1) / 2 : count
   - para cada i en [0, toPlace):
     - TryFindObstaclePoint(40 intentos) retorna candidato que:
       - distancia(center) ≥ clearCenterRadius
       - No cerca del eje de entrada (clearEntryRadius)
       - No choca con otro obstáculo (obstacleSpacing)
     - SpawnObstacle(prefab, point, yaw, scale)
     - Si mirror: SpawnObstacle(prefab, -point, yaw+180°, scale)

3. **SpawnObstacle(prefab, position, yaw, scale):**
   - Instantiate(prefab, position, Quaternion.Euler(0, yaw, 0), generatedRoot)
   - scale = localScale * factor
   - Agrega position a obstaclePositions

4. **BuildVeins(rng, filter, center):**
   - count = rng.Next(veinCount.x, veinCount.y + 1)
   - toPlace = mirror ? (count + 1) / 2 : count
   - Para cada i:
     - TryFindVeinPoint(40 intentos)
     - AddVeinIfOnNavMesh(point, capacity, filter)
     - Si mirror: AddVeinIfOnNavMesh(-point, ...)
   - BuildDecor(rng, center) después de vetas
   - Limpia cristales caídos (DestroyImmediate con tag o rayo)

5. **RandomPointInSquare(rng, center):**
   - Retorna punto aleatorio en cuadrado [min, max] donde:
     - min = center - arenaHalfSize + edgeMargin
     - max = center + arenaHalfSize - edgeMargin

## Invariantes S102

- **RNG seeded:** determinístico por seed
- **Eje de entrada por semilla:** `seed % 4` selecciona uno de 4 ejes
- **Simetría central:** mirror=true espeja en [-x, -z] alrededor del centro
- **NavMesh pre-bake:** BuildVeins ocurre DESPUÉS de surface.BuildNavMesh()
- **VeinSpot.Position proyectado:** NavMesh.SamplePosition, no candidato original
- **obstaclePositions caché:** se limpia en Clear()
- **GeneratedLayout única raíz:** se destruye con DestroyImmediate
- **staticObstacles desactivo:** no se destruye (se reactiva al Clear si necesario)
- **40 intentos por ubicación:** fallback si no encuentra punto válido
- **DecorClusters:** después de vetas, sin colliders

## Conexiones

**Entrada:**
- Parámetros Build: seed (int), filter (NavMeshQueryFilter)
- transform.position (centro de arena)
- Listas de prefabs

**Salida:**
- Instancias en escena (GeneratedLayout + children)
- NavMesh rebuilt
- Propiedad Veins leída por ArenaSandbox
- EntryDirection/EntryName para UI

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[ArenaSandbox]]
- [[MaterialPickup]]
- [[NavMeshSurface]]
