---
tags: [script, world, expedition, procedural, brush, editor]
---

# ArenaShapeBrush.cs

**Ruta:** `World/Expedition/ArenaShapeBrush.cs`

**Responsabilidad:** Componente que gestiona máscara binaria de pincelado interactivo para definir regiones (lagos, rocas, pozos). Soporta pintura manual (via ArenaShapeBrushTool), generación procedural por semilla, aplicación a splines de ArenaShape. Delegados: ArenaShapeMask (geometría), ArenaShape (escritura).

**Propiedades Públicas:**
- `int Size { get; }` — resolución de máscara (128 default)
- `float Cell { get; }` — tamaño de celda en metros (0.5 default)
- `Vector2 Origin { get; }` — esquina inferior-izquierda de la grilla
- `float BrushRadius { get; set; }` — radio del pincel (3 default)
- `byte[] Mask { get; }` — máscara binaria (lazy-initialized)
- `bool ProceduralBySeed { get; }` — toggle para generación procedural
- `int Version { get; }` — versión (incrementada en cada cambio)

**Métodos Públicos:**
- `void PaintAt(Vector3 world, bool erase)` — pinta disco en posición world
  - Convierte world a máscara local
  - Si erase=false: simetrizase automáticamente si Symmetric
  - Si erase=true: pinta también la imagen espejada
  - Incrementa version
- `void Apply()` — convierte máscara a contornos y escribe a ArenaShape.WriteOutline/WriteRegions
  - Extrae contornos (marching squares)
  - Identifica exterior (mayor área)
  - Filtra islas (desconectadas del exterior)
  - Simplifica poligonos
  - Llama PlaceEntries() si autoEntries
- `void Regenerate(int seed)` — genera layout procedural por semilla
  - BlueParams con rng seeded
  - ArenaShapeMask.Blobs() para crear manchas
  - Simetrización si Symmetric
  - Generación de agujeros (lakes dentro de rocas)
  - Llama Apply()
- `void Regenerate()` → Regenerate(blobSeed)
- `void Clear()` → ArenaShapeMask.Clear(mask), version++

**Campos Serializados (S111):**
- `size` (int, default 128) — resolución grilla
- `cell` (float, min 0.1, default 0.5) — metros/celda
- `brushRadius` (float, min 0.5, default 3) — radio del pincel (metros)
- `simplifyTolerance` (float, min 0.1, default 0.6) — tolerancia Douglas-Peucker
- `holeKind` (ArenaRegionKind, default Lake) — tipo de región a generar (lakes/rocks/pits)
- `blobSeed` (int, default 1) — semilla procedural
- `proceduralBySeed` (bool) — toggle (si false, usar pincelado manual)
- `centerRadius`, `blobCount`, `blobRadius`, `blobSpread`, `blobOverlap` — parámetros de blobs
- `holeCount`, `holeRadius`, `holeMinFromCenter` — parámetros de agujeros
- `autoEntries` (bool, default true) — auto-place entries si Apply()
- `entryInset`, `entryMinFromCenter` — parámetros de entrada

**PlaceEntries (auto si autoEntries=true):**
1. Define 4 direcciones: diagonal, diagonal-inversa, norte-sur, este-oeste
2. Para cada dirección: busca punto en outline más lejano en esa dirección
3. Filtra por ángulo (< 25°) y distancia mínima
4. Crea GOs "Entries" y coloca entradas con inset hacia adentro

**Ciclo de Uso:**
1. Editor: ArenaShapeBrushTool llama PaintAt() durante drag
2. Al soltar: llama Apply() automáticamente
3. O: llama Regenerate(seed) para generar proceduralmente
4. Apply() escribe splines a ArenaShape, que hace Rebuild()

**Invariantes:**
- Máscara es tamaño*tamaño bytes (lazy-init a new byte[] si null o tamaño incorrecto)
- Origin = Center - (size*cell/2) * Vector2.one
- BrushRadius solo se modifica durante interacción (ArenaShapeBrushTool [ y ])
- Version incrementa con cada cambio (para caché de preview mesh)
- Simetrización ocurre automáticamente si ArenaShape.Symmetric

**S111 Nuevo:**
- Componente de interacción para pincelado y generación procedural de layout

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaShape]], [[ArenaShapeMask]], [[ArenaShapeBrushTool]]
