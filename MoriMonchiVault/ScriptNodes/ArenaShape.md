---
tags: [script, world, expedition, procedural, generation, spline]
---

# ArenaShape.cs

**Ruta:** `World/Expedition/ArenaShape.cs`

**Responsabilidad:** Componente que define y construye la topografía completa de una sala de expedición usando splines. Gestiona contorno, regiones (rocas, lagos, pozos, bosques), entradas simétricas. Genera mallas de piso, acantilados, rocas, agua, decoración. S111: núcleo proceduralista que expone splines editables y delega pincelado a ArenaShapeBrush. S113: arena 40% más grande; topografía central con colisión invisible (faldon del acantilado). **S114:** una sola entrada por eje (ArenaShapeAxes.Longest), PlaceEntries automático, validación de pares de vetas.

**Propiedades Públicas:**
- `string DisplayName { get; }` — nombre de la sala
- `bool Symmetric { get; }` — si la sala es simétrica (espejo 180°)
- `Vector3 Center { get; }` — posición del centro (transform.position)
- `Bounds Bounds { get; }` — AABB del contorno
- `IReadOnlyList<Vector2> OutlinePolygon { get; }` — contorno de la sala (XZ)
- `IReadOnlyList<IReadOnlyList<Vector2>> RockPolygons { get; }` — regiones de rocas
- `IReadOnlyList<IReadOnlyList<Vector2>> LakePolygons { get; }` — regiones de lagos
- `IReadOnlyList<IReadOnlyList<Vector2>> PitPolygons { get; }` — regiones de pozos
- `IReadOnlyList<IReadOnlyList<Vector2>> GrovePolygons { get; }` — regiones de bosques
- `int EntryPairCount { get; }` — número de pares de entrada (si simétrica)

**Métodos Públicos:**
- `bool Contains(Vector3 world)` — si el punto está dentro de la sala (excluyendo rocas, lagos, pozos)
- `bool IsClear(Vector3 world, float margin)` — si el punto está libre con margen de borde
- `bool TryRandomPoint(System.Random rng, float margin, out Vector3 point)` — busca punto aleatorio libre (40 intentos)
- `Vector3 EntryPoint(int pair, ExpeditionTeam team, float inset)` — punto de entrada por índice, team e inset (S114: **una sola entrada por eje**)
- `string EntryName(int pair)` — nombre de la entrada (ej. "norte-sur", "este-oeste", "diagonal")
- `void WriteOutline(IReadOnlyList<Vector2> points)` — inyecta nuevo contorno vía spline
- `void WriteRegions(ArenaRegionKind kind, IReadOnlyList<IReadOnlyList<Vector2>> polygons)` — inyecta regiones (rocas/lagos/pozos/bosques)
- `void SetEntries(IReadOnlyList<string> names, IReadOnlyList<Vector3> positions)` — fija entrada points (S114: **valida pares**)
- `void Rebuild()` — regenera geometría (malla + colisores)

**Eventos:**
- `event System.Action Rebuilt` — disparado tras Rebuild()

**Campos Serializados (S111-S114):**
- `displayName` (string) — nombre de display
- `outline` (SplineContainer, Required) — spline del contorno
- `rocks`, `lakes`, `pits`, `groves` (SplineContainer) — splines de regiones
- `symmetric` (bool) — espejo 180° alrededor del centro
- `samplesPerMeter` (float, min 0.2) — densidad de muestreo de splines (1.5 default)
- `entries` (List<Transform>) — posiciones de entrada (puede ser par simétrico o par no-simétrico, S114: **validadas como pares**)
- `cliffHeight`, `cliffDepth`, `fenceHeight`, `rockHeight`, `lakeDepth`, `waterLevel`, `pitDepth` — alturas de relieve
- `uvScale` (float, min 0.01) — escala UV de mallas
- `groundMaterial`, `cliffMaterial`, `rockMaterial`, `lakeBedMaterial`, `waterMaterial`, `pitMaterial` — materiales
- `grovePrefabs` (List<GameObject>) — prefabs de bosques
- `groveSpacing`, `groveSeed`, `groveScale`, `groveEdgeMargin` — parámetros de bosques
- `edgePrefabs` (List<GameObject>) — prefabs de borde (pasto)
- `edgeStep`, `edgeInset`, `edgeJitter`, `edgeScale` — parámetros de borde
- `extras` (List<GameObject>) — GOs extras que se activan/desactivan con la sala

**Ciclo de Construcción:**
1. OnEnable() suscribe a Spline.Changed y SplineContainer events, marca dirty
2. RequestRebuild() en OnValidate() y cuando splines cambian
3. Update() ejecuta Rebuild() si dirty
4. Rebuild():
   - RecalculatePolygons() (muestreo desde splines, simetrización, validación)
   - **S114:** si entries vacías, calcula automático con ArenaShapeAxes.Longest + PlaceEntries
   - CreateGameObject("Generated", hideFlags=DontSave)
   - BuildFloor() (triangulación de contorno − (lagos+pozos))
   - BuildCliff() (acantilado + valla)
   - BuildRocks() (malla + colisión por roca)
   - BuildLakes() (fondo + agua con offset)
   - BuildPits() (fondo + paredes)
   - BuildGroves() (scatter de árboles con ArenaShapeScatter)
   - BuildEdge() (scatter de pasto de borde, S114: con FoliageVariants)
   - Rebuilt?.Invoke() (S114: ahora incluye SetEntries si fue automático)

**Métodos Privados (S114 NUEVOS):**
- `PlaceEntries()` — encuentra automaticamente entradas por eje:
  - Obtiene dos ejes longos vía `ArenaShapeAxes.Longest(outline, center, max=2, ...)`
  - Para cada eje, busca punto del contorno más lejano dentro de ±25° del eje
  - Acerca punto hacia centro por `entryInset` metros
  - Crea pares simétricamente (si symmetric)
  - Dispara `SetEntries(names, positions)` con nombres de ejes ("norte-sur", "este-oeste", etc)

**Invariantes (S111-S113-S114):**
- `symmetric=true` → mirrors entradas y regiones (excepto grove, que se dupla)
- **S114:** EntryName(pair) usa nombre de eje (ej. "norte-sur") en lugar de índice
- **S114:** entries validadas en pares (cantidad siempre par o automático)
- Cada región puede tener agujeros internos (islas). Simplify() en ArenaShapeBrush filtra islas
- OutlinePolygon CCW, RockPolygons CW (ej)
- mallas tienen DontSave hideFlags
- **S114:** PlaceEntries automático si entries está vacío al Rebuild

**S111-S113-S114 Cambios:**
- S111: Núcleo nuevo completo de topografía por splines
- S111: Expone públicamente polygons para ArenaShapeBrush.Contours()
- S111: Simetrización integrada (Symmetric toggle)
- S111: Rebuild() es autoridad única (WriteOutline/WriteRegions inyectan splines, luego Rebuild)
- **S114:** PlaceEntries() automático si entries vacías; ArenaShapeAxes.Longest para encontrar ejes; nombres de ejes en lugar de índices

**Vinculado a:** [[Index/20 - MVP Combate]], [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], S114

**Conexiones:** [[ArenaShapeMesher]], [[ArenaShapeScatter]], [[ArenaShapeGizmos]], [[ArenaShapeBrush]], [[ArenaShapeDressing]], [[ArenaShapeShafts]], [[ArenaShapeSurround]], [[ArenaShapeAxes]], [[ArenaVeinLayouts]], [[ArenaSandbox]], [[MaterialPickup]], [[NavMeshSurface]]
