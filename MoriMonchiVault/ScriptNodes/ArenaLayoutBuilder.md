---
tags: [script, world, expedition, procedural, generation, shapes]
---

# ArenaLayoutBuilder.cs

**Ruta:** `World/Expedition/ArenaLayoutBuilder.cs`

**Responsabilidad:** Generador proceduralista de topografía de arena por semilla. **S111:** soporte para múltiples formas (ArenaShape). Construye obstáculos (árboles/rocas Synty), hitos grandes (landmarks), rebakea NavMesh, genera vetas mineras. Expone Veins, obstáculos, puntos spawn, forma activa. Delega generación de contorno a ArenaShape.ProceduralBySeed. **S113:** integración de landmarks para decoración grande. **S114:** patrones de vetas pareados (ArenaVeinLayouts), ExitPoint == SpawnPoint, validación de vetas contra radius de hitos, Clear() por jerarquía.

**Propiedades Públicas:**
- `IReadOnlyList<VeinSpot> Veins { get; }` — vetas generadas (S114: pares siempre)
- `int ObstacleCount { get; }` — total de obstáculos pequeños colocados
- `IReadOnlyList<Vector4> PlacedObstacles { get; }` — hitos grandes (landmarks.Placed, XYZ+radio)
- `bool IsBuilt { get; }` — si generatedRoot != null
- `Vector3 Center { get; }` — centro de forma activa (o default)
- `string ShapeName { get; }` — nombre display de forma activa (o "cuadrado")
- `Vector3 EntryDirection { get; }` — eje de entrada normalizado
- `string EntryName { get; }` — nombre eje (o desde forma)
- `Vector3 EntryPoint(ExpeditionTeam team, float inset)` → Vector3
- `Vector3 ExitPoint(ExpeditionTeam team)` → Vector3 (S114: **igual a SpawnPoint**)
- `Vector3 SpawnPoint(ExpeditionTeam team)` → Vector3

**Métodos Públicos:**
- `void Build(int seed, NavMeshQueryFilter filter)` — genera layout completo:
  1. Clear() — destruye por jerarquía (S114)
  2. Desactiva obstáculos estáticos
  3. Crea GeneratedLayout GO
  4. Selecciona forma activa (shapes[shapeIndex % shapes.Count] ó legacySquare)
  5. Si forma.brush.ProceduralBySeed: forma.brush.Regenerate(seed)
  6. BuildObstacleSet(treePrefabs, rockPrefabs) con simetría
  7. **S113:** `landmarks.Place()` — coloca grandes con exclusiones
  8. surface.BuildNavMesh()
  9. BuildVeins(rng, filter) con ArenaVeinLayouts — genera pares de vetas post-bake (S114)
  10. Log: seed, entrada, obstáculos, decorado, vetas, grandes, mirror, forma

- `void Clear()` — **S114:** destruye generatedRoot por búsqueda en jerarquía, limpia listas (veins, obstaclePositions, decorCenters)

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
  - `shapeIndex` (int) — índice de forma seleccionada (-1 = por semilla)
  - `legacySquare` (GameObject, opcional) — geometría pre-S111

- **Geometría:**
  - `arenaHalfSize` (float) — tamaño legacy
  - `edgeMargin` (float) — margen de borde (2.5 default)
  - `clearCenterRadius` (float) — radio limpio del centro (6)
  - `clearEntryRadius` (float) — radio limpio de entradas (5)
  - `spawnDistance` (float) — **S114:** `spawnReach` — distancia spawn desde entrada
  - `exitInset` (float) — inset de salida desde entrada (4)
  - `obstacleSpacing` (float) — separación mínima entre obstáculos (3.5)
  - Escalas: `treeScale`, `rockScale`, `decorScale`

- **Vetas (S114 ACTUALIZADO):**
  - `veinMinFromCenter`, `veinSpacing`, `veinFromObstacle` (float)
  - `veinCapacity` (Vector2Int)
  - `veinPattern` (ArenaVeinLayouts.Pattern) — patrón de distribución (Racimos, Anillo, Franja, Lobulos)

- **Extras:**
  - `mirror` (bool) — espeja obstáculos/décor
  - `staticObstacles`, `staticDecor` (List<GameObject>) — desactiva al generar

**Build (S114 ACTUALIZADO):**
1. BuildObstacleSet(trees, rocks) — scatter simétrico si mirror
2. BuildDecor() — clusters simétricos
3. landmarks.Place():
   - safePoints = [spawn player, spawn rival, exit player, exit rival]
   - edgeMargin = borde mínimo
   - centerRadius = clearCenterRadius
   - safeRadius = clearEntryRadius
   - obstaclePositions actualizada con positions nuevas
4. surface.BuildNavMesh() después (landmarks ya colocados)
5. **BuildVeins()** con ArenaVeinLayouts (S114):
   - Genera pares de vetas siempre (count par)
   - Elige patrón veinPattern (default Racimos)
   - Valida cada veta contra radius de landmarks (minDistanceLandmark)
   - Si rechazo, reintenta hasta 20 veces o skip
   - Usa `ArenaLayoutBuilder.Longest(outline)` para orientar patrón Franja

**Invariantes S114:**
- RNG seeded: determinístico por seed
- Landmarks se valida antes de NavMesh bake (orden: obstáculos → landmarks → bake → vetas)
- ObstacleCount cuenta pequeños; PlacedObstacles cuenta grandes (landmarks)
- mirror=true → cantidad par de colocadas (o impar si falla)
- VeinSpot.Position proyectado al NavMesh post-bake
- **Vetas en pares siempre** (count par, ambas validadas contra landmarks)
- **ExitPoint == SpawnPoint** (misma ubicación entrada/salida)
- **Patrón de vetas** elegido por forma o global (ArenaVeinLayouts.Pattern)

**S104-S111-S113-S114 Cambios:**
- S104: ObstacleCount property
- S111: formas shape.Contains() / shape.IsClear() / shape.Center
- S113: landmarks.Place() integrado, log con "grandes"
- S114: ArenaVeinLayouts.Pattern, ExitPoint == SpawnPoint, pares siempre, validación landmark, Clear() por jerarquía

**Vinculado a:** [[Index/20 - MVP Combate]], [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], S114

**Conexiones:** [[ArenaSandbox]], [[ArenaShape]], [[ArenaLandmarks]], [[ArenaShapeBrush]], [[ArenaVeinLayouts]], [[ArenaShapeAxes]], [[MaterialPickup]], [[NavMeshSurface]]
