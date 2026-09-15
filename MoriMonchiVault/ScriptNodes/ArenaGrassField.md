---
tags: [script, world, expedition, graphics, mesh, procedural]
---

# ArenaGrassField.cs

**Ruta:** `World/Expedition/ArenaGrassField.cs`

**Responsabilidad:** Siembra pasto procedural por parcelas al reconstruirse la forma de arena. Divide bounds en chunks cuadrados, valida cada chunk contra bordes (distancia a edge) y obstáculos (rocas, lagos, pits, bosques), genera raíces por densidad/chunk, **S117:** usa ArenaGrassCover.Sample() para densidad procedural y height scale, delega a `ArenaGrassBlades.Build()` para armar malla, instantia GameObjects con MeshFilter/MeshRenderer por chunk. Reinicia al evento shape.Rebuilt. Destruye field completo en OnDisable. **S117:** expone ClearAround() para limpiar pasto alrededor de spawns (ArenaSandbox.S117).

**Campos serializados:**

**Semilla y Material:**
- `seed` (int, default 11) — semilla RNG para determinismo (posición/tamaño/rotación de briznas)
- `bladeMaterial` (Material) — material para los chunks (recibe ramp de ArenaPaletteApplier)

**Parcelas (Chunks):**
- `chunkSize` (float, Min 2, default 5) — tamaño de parcela cuadrada
- `density` (float, Min 0.1, default 50) — briznas/unidad² (candidatas por chunk)
- `maxBlades` (int, default 120000) — límite total de briznas generadas

**Briznas (Blades):**
- `heightRange` (Vector2, default 0.12 - 0.26) — rango de altura de brizna
- `widthRange` (Vector2, default 0.045 - 0.075) — rango de ancho de brizna
- `tilt` (float, default 0.25) — ángulo máximo de inclinación/lean de brizna

**Cobertura — S117 NUEVA:**
- `cover` (ArenaGrassCover.Settings, default ArenaGrassCover.Settings.Default) — parámetros de densidad procedural:
  - clumpSpacing, clumpRadius, haloDensity, patchScale, patchThreshold, patchDensity, looseDensity, looseHeight
  - Determina densidad y altura de cada brizna por posición

**Márgenes:**
- `edgeMargin` (float, default 0.6) — distancia de seguridad a bordes de arena
- `obstacleMargin` (float, default 0.5) — distancia de seguridad a obstáculos

**Métodos públicos:**

- **`void ClearAround(IReadOnlyList<Vector4> zones)`** — **S117 NUEVO:** limpia pasto alrededor de zonas (spawn points, obstáculos)
  - Itera chunks cercanos a alguna zone (distancia <= zone.w + chunkSize * 0.7071)
  - Para cada raíz dentro de zone (distancia < zone.w): filtra fuera
  - Reconstruye malla con raíces restantes (ArenaGrassBlades.Build)
  - Destruye chunk si todas las raíces fueron removidas

**Métodos privados:**

**Sow() (inicialmente llamado en OnEnable, luego en shape.Rebuilt):**

1. DestroyGrassField() — limpia anterior
2. Valida shape.OutlinePolygon.Count >= 3, bladeMaterial != null
3. Crea GameObject("GrassField", hideFlags.DontSave)
4. Inicializa RNG con seed
5. Recolecta obstacle bounds (rocks, lakes, pits, groves) con margen
6. Itera chunks en bounds:
   - Calcula chunkCenter, chunkCenter2D
   - Detecta si centro está dentro (shape.Contains)
   - Calcula distancia a edge
   - Si está fuera de circumRadius (chunkSize * 0.7071), skipa
   - Genera ~density * chunkSize² candidatos por raíz
   - Para cada raíz:
     - **S117:** `float density = ArenaGrassCover.Sample(pos, seed, in cover, out float heightScale)`
     - Si `rng.NextDouble() >= density`, skipa (probabilidad dinámica)
     - Añade raíz + heightScale a listas
   - Valida: dentro de shape, claro de obstáculos, y lejos de edge (si no bordeSeguro)
   - Llama ArenaGrassBlades.Build(roots, rng, ranges, tilt, chunkCenter, **heights**) → Mesh
   - Crea chunk GameObject con MeshFilter(mesh) + MeshRenderer(bladeMaterial)
   - Stoppe si totalBlades >= maxBlades

**DestroyGrassField():**
- Destruye GameObject grassField (y sus mallas)
- Limpia hijos históricos con nombre "GrassField"
- Libera mallas de MeshFilter (DestroyImmediate)

**CollectObstacleBounds(polygons, margin, bounds):**
- Por cada polígono en lista: agrega PolygonBounds con margen

**PolygonBounds(polygon, margin) → Rect:**
- Computa AABB de polígono, expande por margin
- Retorna Rect

**OnValidate() (editor-only):**
- En edit mode: scheduleDelayCall → SowIfAlive (regenera preview)

**DestroyChunks(Transform field):**
- Limpia mallas de MeshFilter en hijos

**PlanarDistance(Vector3 a, Vector4 b) — S117 NUEVO:**
- Distancia 2D entre punto 3D y Vector4(x,z,_,_)

**Invariantes S117:**

- **Procedural per seed:** RNG determinista por seed → reproducible
- **Densidad dinámica:** ArenaGrassCover.Sample() por posición → grumos + halos + parches + suelta
- **Height scale dinámico:** ArenaGrassCover.Sample() out heightScale → altura de brizna varía con densidad
- **Límite de briznas:** soft cap maxBlades (early exit si se alcanzó)
- **Edge margin:** evita briznas al filo del mapa
- **Obstacle avoidance:** rechaza candidatos en radio de rocas/lagos/pits/bosques
- **Chunk-based:** paralelizable, permite regeneración selectiva
- **ClearAround:** limpia pasto alrededor de spawns post-siembra (ArenaSandbox.BuildRoom S117)
- **Editor preview:** OnValidate permite pre-visualizar en edit mode
- **Destrucción lazy:** gameobjects transitorios (hideFlags.DontSave)

**S117 Cambios:**

- Integración de ArenaGrassCover.Sample() para densidad procedural
- heightScale aplicado a cada brizna (ArenaGrassBlades.Build ahora recibe lista de scales)
- ClearAround() expuesto públicamente para ArenaSandbox
- Pasto dinámico que mantiene huella de pasos (área despejada por zonas)

**Conexiones:** [[ArenaShape]], [[ArenaGrassBlades]], [[ArenaGrassCover]], [[ArenaPaletteApplier]], [[ArenaSandbox]]

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion]], [[Index/22 - Bajada Nocturna y Linaje]], S117
