---
tags: [script, world, expedition, procedural, generation, spline]
---

# ArenaShape.cs

**Ruta:** `World/Expedition/ArenaShape.cs`

**Responsabilidad:** Componente que define y construye la topografía completa de una sala de expedición usando splines. Gestiona contorno, regiones (rocas, lagos, pozos, bosques), entradas simétricas. Genera mallas de piso, acantilados, rocas, agua, decoración. S111: núcleo proceduralista que expone splines editables y delega pincelado a ArenaShapeBrush.

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
- `Vector3 EntryPoint(int pair, ExpeditionTeam team)` — punto de entrada por índice y team
- `string EntryName(int pair)` — nombre de la entrada
- `void WriteOutline(IReadOnlyList<Vector2> points)` — inyecta nuevo contorno vía spline
- `void WriteRegions(ArenaRegionKind kind, IReadOnlyList<IReadOnlyList<Vector2>> polygons)` — inyecta regiones (rocas/lagos/pozos/bosques)
- `void SetEntries(IReadOnlyList<string> names, IReadOnlyList<Vector3> positions)` — fija entrada points
- `void Rebuild()` — regenera geometría (malla + colisores)

**Eventos:**
- `event System.Action Rebuilt` — disparado tras Rebuild()

**Campos Serializados (S111):**
- `displayName` (string) — nombre de display
- `outline` (SplineContainer, Required) — spline del contorno
- `rocks`, `lakes`, `pits`, `groves` (SplineContainer) — splines de regiones
- `symmetric` (bool) — espejo 180° alrededor del centro
- `samplesPerMeter` (float, min 0.2) — densidad de muestreo de splines (1.5 default)
- `entries` (List<Transform>) — posiciones de entrada (puede ser par simétrico o par no-simétrico)
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
   - CreateGameObject("Generated", hideFlags=DontSave)
   - BuildFloor() (triangulación de contorno − (lagos+pozos))
   - BuildCliff() (acantilado + valla)
   - BuildRocks() (malla + colisión por roca)
   - BuildLakes() (fondo + agua con offset)
   - BuildPits() (fondo + paredes)
   - BuildGroves() (scatter de árboles con ArenaShapeScatter)
   - BuildEdge() (scatter de pasto de borde)
   - Rebuilt?.Invoke()

**Invariantes:**
- `symmetric=true` → mirrors entradas y regiones (excepto grove, que se dupla)
- EntryName(pair) busca en symmetric ? entries[pair] : entries[pair*2]
- Cada región puede tener agujeros internos (islas). Simplify() en ArenaShapeBrush filtra islas
- OutlinePolygon CCW, RockPolygons CW (ej)
- mallas tienen DontSave hideFlags

**S111 Cambios:**
- Núcleo nuevo completo de topografía por splines
- Expone públicamente polygons para ArenaShapeBrush.Contours()
- Simetrización integrada (Symmetric toggle)
- Rebuild() es autoridad única (WriteOutline/WriteRegions inyectan splines, luego Rebuild)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaShapeMesher]], [[ArenaShapeScatter]], [[ArenaShapeGizmos]], [[ArenaShapeBrush]], [[ArenaShapeDressing]], [[ArenaSandbox]], [[MaterialPickup]], [[NavMeshSurface]]
