---
tags: [script, world, expedition, procedural, generation, shapes]
---

# ArenaLayoutBuilder.cs

**Ruta:** `World/Expedition/ArenaLayoutBuilder.cs`

**Responsabilidad:** Generador proceduralista de topografía de arena por semilla. **S111:** soporte para múltiples formas (ArenaShape). Construye obstáculos (árboles/rocas Synty), hitos grandes (landmarks), rebakea NavMesh, genera vetas mineras. Expone Veins, obstáculos, puntos spawn, forma activa. Delega generación de contorno a ArenaShape.ProceduralBySeed. **S113:** integración de landmarks para decoración grande.

**Propiedades Públicas:**
- `IReadOnlyList<VeinSpot> Veins { get; }` — vetas generadas
- `int ObstacleCount { get; }` — total de obstáculos pequeños colocados
- `IReadOnlyList<Vector4> PlacedObstacles { get; }` — hitos grandes (landmarks.Placed, XYZ+radio)
- `bool IsBuilt { get; }` — si generatedRoot != null
- `Vector3 Center { get; }` — centro de forma activa (o default)
- `string ShapeName { get; }` — nombre display de forma activa (o "cuadrado")
- `Vector3 EntryDirection { get; }` — eje de entrada normalizado
- `string EntryName { get; }` — nombre eje (o desde forma)
- `Vector3 EntryPoint(ExpeditionTeam team, float inset)` → Vector3
- `Vector3 ExitPoint(ExpeditionTeam team)` → Vector3
- `Vector3 SpawnPoint(ExpeditionTeam team)` → Vector3

**Métodos Públicos:**
- `void Build(int seed, NavMeshQueryFilter filter)` — genera layout completo:
  1. Clear()
  2. Desactiva obstáculos estáticos
  3. Crea GeneratedLayout GO
  4. Selecciona forma activa (shapes[shapeIndex % shapes.Count])
  5. Si forma.brush.ProceduralBySeed: forma.brush.Regenerate(seed)
  6. BuildObstacleSet(treePrefabs, rockPrefabs) con simetría
  7. **S113:** `landmarks.Place(rng, generatedRoot.transform, activeShape, center, edgeMargin, clearCenterRadius, safePoints, clearEntryRadius, obstaclePositions)` — coloca grandes con exclusiones
  8. surface.BuildNavMesh()
  9. BuildVeins(rng, filter) — genera vetas post-bake
  10. Log: seed, entrada, obstáculos, decorado, vetas, grandes, mirror, forma

- `void Clear()` — destruye generatedRoot, limpia listas (veins, obstaclePositions, decorCenters)

**Campos Serializados:**
- **Requeridos:**
  - `surface` (NavMeshSurface)
  - `shapes` (List<ArenaShape>) — formas disponibles

- **S113 NUEVO:**
  - `landmarks` (ArenaLandmarks) — componente de grandes

- **Densidad:**
  - `treeCount`, `rockCount`, `veinCount` (Vector2Int) — rangos por seed
  - `decorClusters`, `decorPerCluster`, `decorClusterRadius` (float) — clusters de decoración

- **Forma:**
  - `shapeIndex` (int) — índice de forma seleccionada
  - `legacySquare` (GameObject, opcional) — geometría pre-S111

- **Geometría:**
  - `arenaHalfSize` (float) — tamaño legacy
  - `edgeMargin` (float) — margen de borde (2.5 default)
  - `clearCenterRadius` (float) — radio limpio del centro (6)
  - `clearEntryRadius` (float) — radio limpio de entradas (5)
  - `spawnDistance` (float) — distancia spawn desde entrada (8.5)
  - `exitInset` (float) — inset de salida desde entrada (4)
  - `obstacleSpacing` (float) — separación mínima entre obstáculos (3.5)
  - Escalas: `treeScale`, `rockScale`, `decorScale`

- **Vetas:**
  - `veinMinFromCenter`, `veinSpacing`, `veinFromObstacle` (float)
  - `veinCapacity` (Vector2Int)

- **Extras:**
  - `mirror` (bool) — espeja obstáculos/décor
  - `staticObstacles`, `staticDecor` (List<GameObject>) — desactiva al generar

**Build (S113):**
1. BuildObstacleSet(trees, rocks) — scatter simétrico si mirror
2. BuildDecor() — clusters simétricos
3. landmarks.Place():
   - safePoints = [spawn player, spawn rival, exit player, exit rival]
   - edgeMargin = borde mínimo
   - centerRadius = clearCenterRadius
   - safeRadius = clearEntryRadius
   - obstaclePositions actualizada con positions nuevas
4. surface.BuildNavMesh() después (landmarks ya colocados)
5. BuildVeins() con NavMesh post-bake

**Invariantes:**
- RNG seeded: determinístico por seed
- Landmarks se valida antes de NavMesh bake (orden: obstáculos → landmarks → bake → vetas)
- ObstacleCount cuenta pequeños; PlacedObstacles cuenta grandes (landmarks)
- mirror=true → cantidad par de colocadas (o impar si falla)
- VeinSpot.Position proyectado al NavMesh post-bake

**S104-S111-S113 Cambios:**
- S104: ObstacleCount property
- S111: formas shape.Contains() / shape.IsClear() / shape.Center
- S113: landmarks.Place() integrado, log con "grandes"

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[ArenaShape]], [[ArenaLandmarks]], [[ArenaShapeBrush]], [[MaterialPickup]], [[NavMeshSurface]]
