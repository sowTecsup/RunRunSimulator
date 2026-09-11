---
tags: [script, world, expedition, procedural, generation, shapes]
---

# ArenaLayoutBuilder.cs

**Ruta:** `World/Expedition/ArenaLayoutBuilder.cs`

**Responsabilidad:** Generador proceduralista de topografía de arena por semilla. **S111:** soporte para múltiples formas (ArenaShape). Construye obstáculos (árboles/rocas Synty), hitos grandes (landmarks), rebakea NavMesh, genera vetas mineras. Expone Veins, obstáculos, puntos spawn, forma activa. Delega generación de contorno a ArenaShape.ProceduralBySeed. **S113:** integración de landmarks para decoración grande. **S114:** patrones de vetas pareados (ArenaVeinLayouts), ExitPoint == SpawnPoint, validación de vetas contra radius de hitos, Clear() por jerarquía. **S115:** Campo `decorClearAroundVein` (1.4); `BuildDecor` corre después de `BuildVeins`; `DecorClearZones(center)` calcula zonas de exclusión (vetas + doble en centro); `InsideAnyZone` es validación planar; `TryFindDecorCenter` recibe las zonas; piezas decorativas dentro de zona no se instancian; al final llama `ArenaShapeDressing.ClearAround(zonas)` para limpiar decorado de la forma activa.

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
  10. **S115:** `BuildDecor(rng, center, clusters)` — clusters decorativos DESPUÉS de BuildVeins
  11. **S115:** Si forma activa, llama `dressing.ClearAround(DecorClearZones(center))`
  12. Log: seed, entrada, obstáculos, decorado, vetas, grandes, mirror, forma

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

- **Decorado (S115 NUEVO):**
  - `decorClearAroundVein` (float, default 1.4) — radio de exclusión alrededor de cada veta

- **Extras:**
  - `mirror` (bool) — espeja obstáculos/décor
  - `staticObstacles`, `staticDecor` (List<GameObject>) — desactiva al generar

**Build (S114-S115 ACTUALIZADO):**
1. BuildObstacleSet(trees, rocks) — scatter simétrico si mirror
2. landmarks.Place() — coloca grandes con exclusiones
3. surface.BuildNavMesh() — bake NavMesh con obstáculos ya colocados
4. BuildVeins(rng, filter) — genera pares de vetas post-bake con validación landmark
5. **S115:** BuildDecor(rng, center, clusters) — clusters decorativos DESPUÉS de vetas
6. **S115:** Si forma activa, llama dressing.ClearAround(DecorClearZones(center)) — limpia decorado de la forma

**BuildDecor (S115 NUEVO):**
- Línea 243-277
- Itera clusters, para cada uno intenta encontrar centro válido
- Llama `DecorClearZones(center)` para obtener zonas de exclusión (vetas + doble en centro)
- Pasa zonas a `TryFindDecorCenter(rng, center, zones, out Vector3)`
- Para cada pieza generada en el cluster, valida `InsideAnyZone(piecePos, zones)` antes de Spawn
- Si está dentro de zona, no se instancia
- Resuelve espejo también con validación planar

**DecorClearZones (S115 NUEVO):**
- Línea 279-286
- Crea lista de Vector4: (center.x, center.y, center.z, radius)
- Itera vetas: agrega `(vein.Position, decorClearAroundVein)` — cada veta con radio 1.4
- Agrega centro arena: `(center, decorClearAroundVein * 2)` — doble radius en centro
- Retorna lista para uso en BuildDecor y dressing.ClearAround()

**InsideAnyZone (S115 NUEVO):**
- Línea 288-294
- Método estático que valida punto contra lista de zonas
- `Planar(point, zone.xyz) < zone.w` — distancia XZ (sin Y) menor que radio
- Si punto dentro de ALGUNA zona, retorna true
- Usado por BuildDecor para excluir piezas y por TryFindDecorCenter para excluir centros

**TryFindDecorCenter (S115 ACTUALIZADO):**
- Línea 322-340
- Ahora recibe `List<Vector4> zones` como parámetro
- Valida contra `InsideAnyZone(candidate, zones)` — si candidato está en zona, rechaza
- Contexto: asegura que cluster center no está dentro de radio de veta o centro

**Build log (S115):**
- Línea 191: incluye forma y patrón de vetas en log final

**Invariantes S114-S115:**
- RNG seeded: determinístico por seed
- Landmarks se valida antes de NavMesh bake (orden: obstáculos → landmarks → bake → vetas → decor)
- ObstacleCount cuenta pequeños; PlacedObstacles cuenta grandes (landmarks)
- mirror=true → cantidad par de colocadas (o impar si falla)
- VeinSpot.Position proyectado al NavMesh post-bake
- **Vetas en pares siempre** (count par, ambas validadas contra landmarks)
- **ExitPoint == SpawnPoint** (misma ubicación entrada/salida)
- **Patrón de vetas** elegido por forma o global (ArenaVeinLayouts.Pattern)
- **S115:** Decorado respeta exclusiones: vetas (radius 1.4) + centro (radius 2.8)
- **S115:** Validación planar: distancia XZ (ignorar Y) para calcular dentro/fuera
- **S115:** dressing.ClearAround() se ejecuta al final para limpiar piezas de decorado de la forma que caen en zonas

**S104-S111-S113-S114-S115 Cambios:**
- S104: ObstacleCount property
- S111: formas shape.Contains() / shape.IsClear() / shape.Center
- S113: landmarks.Place() integrado, log con "grandes"
- S114: ArenaVeinLayouts.Pattern, ExitPoint == SpawnPoint, pares siempre, validación landmark, Clear() por jerarquía
- S115: decorClearAroundVein, BuildDecor después BuildVeins, DecorClearZones, InsideAnyZone planar, TryFindDecorCenter con zonas, dressing.ClearAround() final

**Vinculado a:** [[Index/20 - MVP Combate]], [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], S114, S115

**Conexiones:** [[ArenaSandbox]], [[ArenaShape]], [[ArenaLandmarks]], [[ArenaShapeBrush]], [[ArenaShapeDressing]], [[ArenaVeinLayouts]], [[ArenaShapeAxes]], [[MaterialPickup]], [[NavMeshSurface]]

