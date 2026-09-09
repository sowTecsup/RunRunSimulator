---
tags: [script, world, expedition, rendering, static]
---

# CueDrawer.cs

**Ruta:** `World/Expedition/CueDrawer.cs`

**Responsabilidad:** Dibujante estático en modo inmediato (`Graphics.RenderMesh`). No instancia GameObjects; en cada llamada calcula un quad en mundo, asigna propiedades al shader vía `MaterialPropertyBlock`, y renderiza. Soporta 9 formas (ring, disc, segment, arrow, dashed ring, arc, dashed segment, sector, **S108 NUEVO:** capsule outline) con opcionales de color degradado, dash offset animable, y blend (opaco u aditivo). Contrato: se llama a `Configure(material, additiveMaterial)` en `OnEnable`, luego cada frame en `LateUpdate()` se invocan los métodos de dibujo; el shader evalúa SDF en espacio de mundo sobre XZ con anti-aliasing por `fwidth`.

## Métodos Estáticos Públicos

**Configuración:**
- `Configure(Material material)` — establece material (opaco) global.
- `Configure(Material material, Material additiveMaterial)` — establece material opaco y aditivo.

**Dibujo:**
- `Ring(Vector3 center, float radius, float thickness, Color color, bool additive = false)` — **Forma 0**: anillo lleno, grosor constante. `_Shape=0`.
- `Disc(Vector3 center, float radius, Color color, bool additive = false)` — **Forma 1**: disco plano sin degradado. `_Shape=1`.
- `Disc(Vector3 center, float radius, Color color, float innerAlpha, float outerAlpha, bool additive = false)` — **Forma 1**: disco con degradado radial de alfa (centro opaco → borde transparente). `_InnerAlpha`, `_OuterAlpha`.
- `Segment(Vector3 a, Vector3 b, float thickness, Color color, bool additive = false)` — **Forma 2**: línea recta entre dos puntos. `_Shape=2`.
- `Segment(Vector3 a, Vector3 b, float thickness, Color colorA, Color colorB, bool additive = false)` — **Forma 2**: línea con degradado de color. `_ColorB`.
- `Capsule(Vector3 a, Vector3 b, float radius, Color color, float innerAlpha, float outerAlpha, bool additive = false)` — **Forma 2 (S108 NUEVO)**: cápsula (línea + extremos redondeados) con degradado radial. Usa _PointA/_PointB, _Thickness=radius*2, _InnerAlpha/_OuterAlpha. Función helper para legibilidad.
- `Arrow(Vector3 a, Vector3 b, float thickness, float headLength, float headWidth, Color color, bool additive = false)` — **Forma 3**: línea + punta de flecha. `_Shape=3`.
- `Arrow(Vector3 a, Vector3 b, float thickness, float headLength, float headWidth, Color colorA, Color colorB, bool additive = false)` — **Forma 3**: flecha con degradado. `_ColorB`.
- `DashedRing(Vector3 center, float radius, float thickness, int dashCount, float dashRatio, float rotation, Color color, bool additive = false)` — **Forma 4**: anillo punteado giratorio. `_Shape=4`, `_DashCount`, `_DashRatio`, `_Rotation`.
- `Arc(Vector3 center, float radius, float thickness, float startAngle, float sweep, Color colorA, Color colorB, bool additive = false)` — **Forma 5**: arco parcial con degradado angular. `_Shape=5`, `_ArcStart`, `_ArcSweep`, `_ColorB`.
- `DashedSegment(Vector3 a, Vector3 b, float thickness, float dashLength, float dashGap, float dashOffset, Color colorA, Color colorB, bool additive = false)` — **Forma 6**: línea punteada con offset (para flujo animable) y degradado de color. `_Shape=6`, `_DashLength`, `_DashGap`, `_DashOffset`, `_ColorB`.
- `Sector(Vector3 center, float radius, float startAngle, float sweep, Color color, float innerAlpha, float outerAlpha, bool additive = false)` — **Forma 7 (S102)**: sector relleno (cono de visión, pie de pastel). `_Shape=7`, `_ArcStart`, `_ArcSweep`, `_InnerAlpha`, `_OuterAlpha`.
- `DashedArc(Vector3 center, float radius, float thickness, float startAngle, float sweep, int dashCount, float dashRatio, float rotation, Color colorA, Color colorB, bool additive = false)` — **Forma 8 (S102)**: arco punteado. `_Shape=8`.
- `CapsuleOutline(Vector3 a, Vector3 b, float radius, float thickness, Color colorA, Color colorB, bool additive = false)` — **Forma 9 (S108 NUEVO)**: contorno de cápsula (línea con puntas redondeadas, trazo de grosor variable). Dibuja el perímetro de una cápsula con degradado de color. `_Shape=9`, `_PointA/_PointB` extremos, `_Radius` radio de la cápsula, `_Thickness` grosor del trazo, `_Color/_ColorB` degradado. Usado por CreatureCueDrawer.Telegraph para bordes de plantillas de choque.

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
| `_Shape` | 0-9 | — | 0 Ring, 1 Disc, 2 Segment/Capsule, 3 Arrow, 4 DashedRing, 5 Arc, 6 DashedSegment, 7 Sector, 8 DashedArc, **9 CapsuleOutline (S108)** |
| `_Color` | RGBA | todas | Color primario. |
| `_ColorB` | RGBA | Segment, Arrow, Arc, DashedSegment, DashedArc, **CapsuleOutline** | Degradado: borde, cabeza, angular, lejano, perímetro. |
| `_Center` | XYZ | Ring, DashedRing, Disc, Arc, Sector | Centro en espacio de mundo. |
| `_PointA`, `_PointB` | XYZ | Segment, Arrow, DashedSegment, **Capsule/CapsuleOutline** | Extremos A y B. |
| `_Radius` | float | Ring, DashedRing, Disc, Arc, Sector, **CapsuleOutline** | Radio. |
| `_Thickness` | float | todas excepto Disc/Sector | Grosor de línea (m); Capsule: radio*2. |
| `_InnerAlpha` | 0-1 | Disc, Sector, **Capsule** | Alfa en el centro (Disc) o interior del sector/cápsula. |
| `_OuterAlpha` | 0-1 | Disc, Sector, **Capsule** | Alfa en el borde. |
| `_DashCount` | int | DashedRing | Cantidad de dashes. |
| `_DashRatio` | 0-1 | DashedRing | Proporción on:off. |
| `_Rotation` | radianes | DashedRing, DashedArc | Ángulo de rotación. |
| `_ArcStart` | radianes | Arc, Sector, DashedArc | Ángulo inicial (0 = +X). |
| `_ArcSweep` | radianes | Arc, Sector, DashedArc | Amplitud del arco/sector. |
| `_DashLength` | float | DashedSegment | Largo del dash (m). |
| `_DashGap` | float | DashedSegment | Separación entre dashes (m). |
| `_DashOffset` | float | DashedSegment | Offset de fase (cambia cada frame para flujo). |
| `_HeadLength` | float | Arrow | Largo de la punta. |
| `_HeadWidth` | float | Arrow | Ancho de la punta. |
| `_SrcBlend` | blend | material | Generalmente One (aditivo) o SrcAlpha (opaco). |
| `_DstBlend` | blend | material | Generalmente One (aditivo) o OneMinusSrcAlpha (opaco). |

## Capsule (Shape 2, S108 NUEVO - Helper)

```csharp
public static void Capsule(Vector3 a, Vector3 b, float radius, Color color, float innerAlpha, float outerAlpha, bool additive = false)
{
    Material mat = additive ? additiveMaterial : material;
    if (mat == null) return;
    EnsureResources();

    mpb.Clear();
    mpb.SetColor(ColorID, color);
    mpb.SetColor(ColorBID, color);
    mpb.SetFloat(InnerAlphaID, innerAlpha);
    mpb.SetFloat(OuterAlphaID, outerAlpha);
    mpb.SetFloat(ShapeID, 2f);
    mpb.SetVector(PointAID, a);
    mpb.SetVector(PointBID, b);
    mpb.SetFloat(ThicknessID, radius * 2f);

    Vector3 mid = new Vector3((a.x + b.x) * 0.5f, a.y, (a.z + b.z) * 0.5f);
    float pad = radius * 2f;
    Vector3 scale = new Vector3(Mathf.Abs(b.x - a.x) + pad, 1f, Mathf.Abs(b.z - a.z) + pad);
    Draw(mat, mid, scale);
}
```

**Significado:**
- Dibuja una cápsula (línea + puntas redondeadas) con degradado radial de alfa
- Relleno: `innerAlpha` en el centro, `outerAlpha` en el perímetro
- Usado por CreatureCueDrawer.Telegraph para pista de Horn (desde atacante a rival)

## CapsuleOutline (Shape 9, S108 NUEVO)

```csharp
public static void CapsuleOutline(Vector3 a, Vector3 b, float radius, float thickness, Color colorA, Color colorB, bool additive = false)
{
    Material mat = additive ? additiveMaterial : material;
    if (mat == null) return;
    EnsureResources();

    mpb.Clear();
    mpb.SetColor(ColorID, colorA);
    mpb.SetColor(ColorBID, colorB);
    mpb.SetFloat(InnerAlphaID, 1f);
    mpb.SetFloat(OuterAlphaID, 1f);
    mpb.SetFloat(ShapeID, 9f);
    mpb.SetVector(PointAID, a);
    mpb.SetVector(PointBID, b);
    mpb.SetFloat(RadiusID, radius);
    mpb.SetFloat(ThicknessID, thickness);

    Vector3 mid = new Vector3((a.x + b.x) * 0.5f, a.y, (a.z + b.z) * 0.5f);
    float pad = radius * 2f + thickness;
    Vector3 scale = new Vector3(Mathf.Abs(b.x - a.x) + pad, 1f, Mathf.Abs(b.z - a.z) + pad);
    Draw(mat, mid, scale);
}
```

**Significado:**
- Dibuja el contorno (perímetro) de una cápsula
- `radius` = radio de la cápsula, `thickness` = grosor del trazo
- `colorA/_Color` = color en A, `colorB/_ColorB` = color en B (degradado)
- Usado por CreatureCueDrawer.Telegraph para borde del área de impacto (Horn, Back, Wings)

## Invariantes S102 + S108

- **Sin GameObjects:** `Graphics.RenderMesh` + MPB es más eficiente que Gizmos u objetos.
- **Mesh reutilizable:** un quad unitario se escala/posiciona vía matriz TRS.
- **SDF anti-aliasing:** shader usa `fwidth()` para suavizar bordes.
- **Sector fill:** innerAlpha ≠ outerAlpha permite gradiente radial (común en conos de visión)
- **Capsule fill:** innerAlpha ≠ outerAlpha permite degradado desde eje a borde (común en pistas)
- **CapsuleOutline:** contorno con grosor variable, ideal para bordes de plantillas de combate
- **Convención de ángulos:** `_ArcStart` y `_Rotation` en radianes; 0 = +X, π/2 = +Z.
- **Lazy resources:** quad y MPB se crean bajo demanda.

## Conexiones

- [[ArenaCueOverlay]] (usuario, llama Configure + Draw methods en LateUpdate)
- [[CuePathDrawer]] (delegado a Draw, usa arc/segment)
- [[ArenaRoomCueOverlay]] (delegado a Draw, usa disc/ring/dashed)
- [[CreatureCueDrawer]] (delegado a Draw, usa todos los métodos)
- [[MonchiCue.shader]] (contrato de shader, shapes 0-9)
- [[CueStyleSO]] (usuarios finales pasan valores de estilo)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

**S108:** nuevos métodos `Capsule` (helper, Forma 2) y `CapsuleOutline` (Forma 9) para telegrafía de choque. Lo usa [[CreatureCueDrawer.Telegraph]] para dibujar plantillas de área de impacto con bordes dinámicos y relleno que crece con Tell01.
