---
tags: [script, world, expedition, rendering, static]
---

# CueDrawer.cs

**Ruta:** `World/Expedition/CueDrawer.cs`

**Responsabilidad:** Dibujante estático en modo inmediato (`Graphics.RenderMesh`). No instancia GameObjects; en cada llamada calcula un quad en mundo, asigna propiedades al shader vía `MaterialPropertyBlock`, y renderiza. Soporta 7 formas (ring, disc, segment, arrow, dashed ring, arc, dashed segment) con opcionales de color degradado, dash offset animable, y blend (opaco u aditivo). Contrato: se llama a `Configure(material, additiveMaterial)` en `OnEnable`, luego cada frame en `LateUpdate()` se invocan los métodos de dibujo; el shader evalúa SDF en espacio de mundo sobre XZ con anti-aliasing por `fwidth`.

## Métodos Estáticos Públicos

**Configuración:**
- `Configure(Material material)` — establece material (opaco) global.
- `Configure(Material material, Material additiveMaterial)` — establece material opaco y aditivo.

**Dibujo:**
- `Ring(Vector3 center, float radius, float thickness, Color color, bool additive = false)` — **Forma 0**: anillo lleno, grosor constante. `_Shape=0`.
- `DashedRing(Vector3 center, float radius, float thickness, int dashCount, float dashRatio, float rotation, Color color, bool additive = false)` — **Forma 4**: anillo punteado giratorio. `_Shape=4`, `_DashCount`, `_DashRatio`, `_Rotation`.
- `Arc(Vector3 center, float radius, float thickness, float startAngle, float sweep, Color colorA, Color colorB, bool additive = false)` — **Forma 5**: arco parcial con degradado angular. `_Shape=5`, `_ArcStart`, `_ArcSweep`, `_ColorB`.
- `Disc(Vector3 center, float radius, Color color, bool additive = false)` — **Forma 1**: disco plano sin degradado. `_Shape=1`.
- `Disc(Vector3 center, float radius, Color color, float innerAlpha, float outerAlpha, bool additive = false)` — **Forma 1**: disco con degradado radial de alfa (centro opaco → borde transparente). `_InnerAlpha`, `_OuterAlpha`.
- `Segment(Vector3 a, Vector3 b, float thickness, Color color, bool additive = false)` — **Forma 2**: línea recta entre dos puntos. `_Shape=2`.
- `Segment(Vector3 a, Vector3 b, float thickness, Color colorA, Color colorB, bool additive = false)` — **Forma 2**: línea con degradado de color. `_ColorB`.
- `Arrow(Vector3 a, Vector3 b, float thickness, float headLength, float headWidth, Color color, bool additive = false)` — **Forma 3**: línea + punta de flecha. `_Shape=3`.
- `Arrow(Vector3 a, Vector3 b, float thickness, float headLength, float headWidth, Color colorA, Color colorB, bool additive = false)` — **Forma 3**: flecha con degradado. `_ColorB`.
- `DashedSegment(Vector3 a, Vector3 b, float thickness, float dashLength, float dashGap, float dashOffset, Color colorA, Color colorB, bool additive = false)` — **Forma 6**: línea punteada con offset (para flujo animable) y degradado de color. `_Shape=6`, `_DashLength`, `_DashGap`, `_DashOffset`, `_ColorB`.

## Campos Internos

**Estado global:**
- `material`, `additiveMaterial` (static Material) — materiales configurados.
- `quadMesh` (static Mesh) — quad unitario (-0.5 a +0.5 en XZ) reutilizado, construido lazy.
- `mpb` (static MaterialPropertyBlock) — bloque de propiedades reutilizado.

**Property IDs (cached):**
- `ColorID`, `ColorBID`, `ShapeID`, `CenterID`, `RadiusID`, `ThicknessID`
- `PointAID`, `PointBID`, `HeadLengthID`, `HeadWidthID`
- `DashCountID`, `DashRatioID`, `RotationID`
- `ArcStartID`, `ArcSweepID`, `DashLengthID`, `DashGapID`, `DashOffsetID`
- `InnerAlphaID`, `OuterAlphaID`

## Métodos Privados

- `Draw(Material mat, Vector3 center, Vector3 scale)` — núcleo: calcula matriz TRS, crea `RenderParams`, llama `Graphics.RenderMesh`.
- `EnsureResources()` — lazy init de `mpb` y `quadMesh`.
- `BuildQuadMesh() → Mesh` — crea quad unitario con vértices, normales, UVs, triángulos; `hideFlags=HideAndDontSave`.

## Contrato del Shader (`MonchiCue.shader`)

| Propiedad | Rango | Forma | Significado |
|---|---|---|---|
| `_Shape` | 0-6 | — | 0 Ring, 1 Disc, 2 Segment, 3 Arrow, 4 DashedRing, 5 Arc, 6 DashedSegment |
| `_Color` | RGBA | todas | Color primario. |
| `_ColorB` | RGBA | Segment, Arrow, Arc, DashedSegment | Degradado: borde, cabeza, angular, lejano. |
| `_Center` | XYZ | Ring, DashedRing, Disc, Arc | Centro en espacio de mundo. |
| `_PointA`, `_PointB` | XYZ | Segment, Arrow, DashedSegment | Extremos A y B. |
| `_Radius` | float | Ring, DashedRing, Disc, Arc | Radio. |
| `_Thickness` | float | todas | Grosor de línea (m). |
| `_InnerAlpha` | 0-1 | Disc | Alfa en el centro. |
| `_OuterAlpha` | 0-1 | Disc | Alfa en el borde. |
| `_DashCount` | int | DashedRing | Cantidad de dashes. |
| `_DashRatio` | 0-1 | DashedRing | Proporción on:off. |
| `_Rotation` | radianes | DashedRing | Ángulo de rotación. |
| `_ArcStart` | radianes | Arc | Ángulo inicial (0 = +X). |
| `_ArcSweep` | radianes | Arc | Amplitud del arco. |
| `_DashLength` | float | DashedSegment | Largo del dash (m). |
| `_DashGap` | float | DashedSegment | Separación entre dashes (m). |
| `_DashOffset` | float | DashedSegment | Offset de fase (cambia cada frame para flujo). |
| `_HeadLength` | float | Arrow | Largo de la punta. |
| `_HeadWidth` | float | Arrow | Ancho de la punta. |
| `_SrcBlend` | blend | material | Generalmente One (aditivo) o SrcAlpha (opaco). |
| `_DstBlend` | blend | material | Generalmente One (aditivo) o OneMinusSrcAlpha (opaco). |

## Invariantes S97

- **Sin GameObjects:** `Graphics.RenderMesh` + MPB es más eficiente que Gizmos u objetos con Renderer.
- **Mesh reutilizable:** un quad unitario centra en origen y se escala/posiciona vía matriz TRS; el shader lo interpreta en espacio de mundo.
- **SDF anti-aliasing:** el shader usa `fwidth()` para suavizar bordes a distancia; evita aliasing de líneas finas.
- **Matriz TRS:** `Matrix4x4.TRS(center, Quaternion.identity, scale)` posiciona el quad sin rotar (el shader maneja rotación).
- **RenderParams worldBounds:** se pasa `new Bounds(center, scale)` como volumen de renderizado; suficientemente conservador para guías.
- **Additive blend:** para resaltes (retícula, arco de atención) se usa `additiveMaterial` (One/One); ambiente usa alpha (SrcAlpha/OneMinusSrcAlpha).
- **Convención de ángulos:** `_ArcStart` y `_Rotation` en radianes; 0 = +X, π/2 = +Z.
- **Lazy resources:** quad y MPB se crean bajo demanda en `EnsureResources()`.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

- [[ArenaCueOverlay]] (usuario, llama Configure + Draw methods en LateUpdate)
- [[MonchiCue.shader]] (contrato de shader, propiedad IDs)
- [[CueStyleSO]] (usuarios finales pasan valores de estilo a los métodos)
