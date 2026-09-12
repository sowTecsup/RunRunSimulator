---
tags: [script, world, expedition, graphics, mesh]
---

# ArenaGrassField.cs

**Ruta:** `World/Expedition/ArenaGrassField.cs`

**Responsabilidad:** Siembra pasto procedural por parcelas al reconstruirse la forma de arena. Divide bounds en chunks cuadrados, valida cada chunk contra bordes (distancia a edge) y obstáculos (rocas, lagos, pits, bosques), genera raíces por densidad/chunk, delega a `ArenaGrassBlades.Build()` para armar malla, instantia GameObjects con MeshFilter/MeshRenderer por chunk. Reinicia al evento shape.Rebuilt. Destruye field completo en OnDisable.

**Campos serializados:**
- `seed` (int, default 11) — semilla RNG para determinismo (posición/tamaño/rotación de briznas)
- `bladeMaterial` (Material) — material para los chunks (recibe ramp de ArenaPaletteApplier)
- `chunkSize` (float, Min 2, default 5) — tamaño de parcela cuadrada
- `density` (float, Min 0.1, default 50) — briznas/unidad² (candidatas por chunk)
- `maxBlades` (int, default 120000) — límite total de briznas generadas
- `heightRange` (Vector2, default 0.12 - 0.26) — rango de altura de brizna
- `widthRange` (Vector2, default 0.045 - 0.075) — rango de ancho de brizna
- `tilt` (float, default 0.25) — ángulo máximo de inclinación/lean de brizna
- `edgeMargin` (float, default 0.6) — distancia de seguridad a bordes de arena
- `obstacleMargin` (float, default 0.5) — distancia de seguridad a obstáculos

**Métodos públicos:**
- Ninguno (componente de escena singleton, vía shape.Rebuilt event)

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
   - Genera ~density * chunkSize² candidatos
   - Valida cada candidato: dentro de shape, claro de obstáculos, y lejos de edge (si no bordeSeguro)
   - Llama ArenaGrassBlades.Build(roots, rng, ranges, tilt, chunkCenter) → Mesh
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

**Invariantes:**
- **Procedural per seed:** RNG determinista por seed → reproducible
- **Límite de briznas:** soft cap maxBlades (early exit si se alcanzo)
- **Edge margin:** evita briznas al filo del mapa
- **Obstacle avoidance:** rechaza candidatos en radio de rocas/lagos/pits/bosques
- **Chunk-based:** paralelizable, permite regeneración selectiva
- **Editor preview:** OnValidate permite pre-visualizar en edit mode
- **Destrucción lazy:** gameobjects transitorios (hideFlags.DontSave)

**Conexiones:** [[ArenaShape]], [[ArenaGrassBlades]], [[ArenaPaletteApplier]]

**Vinculado a:** [[Index/22 - MVP Combate]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]
