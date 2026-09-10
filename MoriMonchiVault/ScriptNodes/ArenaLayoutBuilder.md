---
tags: [script, world, expedition, procedural, generation, shapes]
---

# ArenaLayoutBuilder.cs

**Ruta:** `World/Expedition/ArenaLayoutBuilder.cs`

**Responsabilidad:** Generador proceduralista de topografía de arena por semilla. **S111:** soporte para múltiples formas (ArenaShape). Construye obstáculos (árboles/rocas Synty), rebakea NavMesh, genera vetas mineras. Expone Veins, obstáculos, puntos spawn, forma activa. Delega generación de contorno a ArenaShape.ProceduralBySeed.

**Propiedades Públicas:**
- `IReadOnlyList<VeinSpot> Veins { get; }` — vetas generadas
- `bool IsBuilt { get; }` — si generatedRoot != null
- `Vector3 EntryDirection { get; }` — eje de entrada normalizado
- `string EntryName { get; }` — nombre eje (o desde forma si S111)
- `Vector3 EntryPoint(ExpeditionTeam team, float inset)` → Vector3
- `Vector3 ExitPoint(ExpeditionTeam team)` → Vector3
- `Vector3 SpawnPoint(ExpeditionTeam team)` → Vector3
- `int ObstacleCount { get; }` — total de obstáculos colocados (S104)
- **S111:** `IReadOnlyList<ArenaShape> Shapes { get; }` — formas disponibles
- **S111:** `ArenaShape ActiveShape { get; }` — forma actual (null si legacySquare)
- **S111:** `Vector3 Center { get; }` — centro de forma activa (o legacy default)
- **S111:** `string ShapeName { get; }` — nombre display de forma activa (o "Plazuela")
- **S111:** `int EntryPair { get; }` — índice de par de entrada
- **S111:** `bool MirrorActive { get; }` — si forma es simétrica

**Métodos Públicos:**
- `void Build(int seed, NavMeshQueryFilter filter)` — genera layout:
  1. Clear()
  2. Si shapes.Count > 0 y !legacySquare: BuildFromShape(seed)
  3. Sino: BuildLegacy(seed) [pre-S111 path]
  4. surface.BuildNavMesh()
  5. BuildVeins(seed, filter)
- `void Clear()` — destruye generatedRoot, limpia listas
- **S111:** `void SetActiveShape(int index)` — selecciona forma por índice, caché shapeIndex/mirrorActive
- **S111:** `void SetEntryPair(int pair)` — selecciona par de entrada
- **S111:** `void Regenerate(int seed)` — si ActiveShape.brush.ProceduralBySeed: forma.brush.Regenerate(seed), sino BuildObstacles(seed)
- **S111:** `bool TryRandomPoint(System.Random rng, float margin, out Vector3 point)` — delega a forma.TryRandomPoint si existe

**Campos Serializados (S111):**
- Legacy (pre-S111):
  - `surface` (NavMeshSurface)
- **S111 NUEVO:**
  - `shapes` (List<ArenaShape>, Required) — formas disponibles
  - `shapeIndex` (int, cached) — forma actual
  - `legacySquare` (bool) — toggle: si true, usar topografía legacy; si false, usar forma
  - `entryPair` (int, cached) — par de entrada seleccionado
  - `mirrorActive` (bool, ReadOnly, cached) — forma.Symmetric

**BuildFromShape (S111):**
1. Valida ActiveShape != null
2. Si forma.brush.ProceduralBySeed: forma.brush.Regenerate(seed)
3. Usa forma.generated como GeneratedRoot (o vincula geometría)
4. BuildVeins respeta forma.Contains() y forma.IsClear()
5. Usa forma.Center para referencia de ejes

**BuildLegacy (pre-S111):**
1. entryAxis = seed % 4
2. BuildObstacles(rng)
3. GeneratedRoot = GO "ArenaLayout"

**EntryPoint/ExitPoint/SpawnPoint (S111):**
- Si ActiveShape != null: EntryPoint(entryPair, team)
- Sino: BuildLegacy axes (0..3 ejes)

**Center (S111):**
- Si ActiveShape != null: forma.Center
- Sino: Vector3.zero

**ShapeName (S111):**
- Si ActiveShape != null: forma.DisplayName
- Sino: "Plazuela" (default legacy)

**Invariantes:**
- RNG seeded: determinístico por seed
- Pre-S111: eje entrada seed%4 (diagonal, diagonal-inv, Z, X)
- S111: eje entrada 0..forma.EntryPairCount-1
- VeinSpot.Position proyectado al NavMesh post-bake
- 40 intentos máximo por ubicación
- ObstacleCount caché en BuildObstacles

**S104 Cambios:**
- ObstacleCount property
- Usado por ArenaOrderCatalog.RoomText()

**S111 Cambios:**
- Soporte para múltiples formas (shapes list)
- ProceduralBySeed integrado: si forma.brush.ProceduralBySeed → Regenerate(seed)
- Center, ShapeName, EntryPair, MirrorActive públicas
- TryRandomPoint delega a forma
- Dual-path: shapes + legacy (toggle legacySquare)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[ArenaShape]], [[ArenaShapeBrush]], [[MaterialPickup]], [[NavMeshSurface]], [[ArenaOrderCatalog]], [[ArenaPlanPanel]]
