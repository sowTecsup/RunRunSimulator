---
tags: [script, world, expedition, rendering, static]
---

# CueDrawer.cs

**Ruta:** `World/Expedition/CueDrawer.cs`

**Responsabilidad:** Dibujante estático en modo inmediato (`Graphics.RenderMesh`). No instancia GameObjects; en cada llamada calcula un quad en mundo, asigna propiedades al shader vía `MaterialPropertyBlock`, y renderiza. Soporta 8 formas (ring, disc, segment, arrow, dashed ring, arc, dashed segment, **S102 NUEVO:** sector) con opcionales de color degradado, dash offset animable, y blend (opaco u aditivo). Contrato: se llama a `Configure(material, additiveMaterial)` en `OnEnable`, luego cada frame en `LateUpdate()` se invocan los métodos de dibujo; el shader evalúa SDF en espacio de mundo sobre XZ con anti-aliasing por `fwidth`.

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
- `Arrow(Vector3 a, Vector3 b, float thickness, float headLength, float headWidth, Color color, bool additive = false)` — **Forma 3**: línea + punta de flecha. `_Shape=3`.
- `Arrow(Vector3 a, Vector3 b, float thickness, float headLength, float headWidth, Color colorA, Color colorB, bool additive = false)` — **Forma 3**: flecha con degradado. `_ColorB`.
- `DashedRing(Vector3 center, float radius, float thickness, int dashCount, float dashRatio, float rotation, Color color, bool additive = false)` — **Forma 4**: anillo punteado giratorio. `_Shape=4`, `_DashCount`, `_DashRatio`, `_Rotation`.
- `Arc(Vector3 center, float radius, float thickness, float startAngle, float sweep, Color colorA, Color colorB, bool additive = false)` — **Forma 5**: arco parcial con degradado angular. `_Shape=5`, `_ArcStart`, `_ArcSweep`, `_ColorB`.
- `DashedSegment(Vector3 a, Vector3 b, float thickness, float dashLength, float dashGap, float dashOffset, Color colorA, Color colorB, bool additive = false)` — **Forma 6**: línea punteada con offset (para flujo animable) y degradado de color. `_Shape=6`, `_DashLength`, `_DashGap`, `_DashOffset`, `_ColorB`.
- `Sector(Vector3 center, float radius, float startAngle, float sweep, Color color, float innerAlpha, float outerAlpha, bool additive = false)` — **Forma 7 (S102 NUEVO)**: sector relleno (cono de visión, pie de pastel). `_Shape=7`, `_ArcStart`, `_ArcSweep`, `_InnerAlpha`, `_OuterAlpha`.

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
| `_Shape` | 0-7 | — | 0 Ring, 1 Disc, 2 Segment, 3 Arrow, 4 DashedRing, 5 Arc, 6 DashedSegment, **7 Sector (S102)** |
| `_Color` | RGBA | todas | Color primario. |
| `_ColorB` | RGBA | Segment, Arrow, Arc, DashedSegment | Degradado: borde, cabeza, angular, lejano. |
| `_Center` | XYZ | Ring, DashedRing, Disc, Arc, **Sector** | Centro en espacio de mundo. |
| `_PointA`, `_PointB` | XYZ | Segment, Arrow, DashedSegment | Extremos A y B. |
| `_Radius` | float | Ring, DashedRing, Disc, Arc, **Sector** | Radio. |
| `_Thickness` | float | todas excepto Disc/Sector | Grosor de línea (m). |
| `_InnerAlpha` | 0-1 | Disc, **Sector** | Alfa en el centro (Disc) o interior del sector. |
| `_OuterAlpha` | 0-1 | Disc, **Sector** | Alfa en el borde (Disc) o exterior del sector. |
| `_DashCount` | int | DashedRing | Cantidad de dashes. |
| `_DashRatio` | 0-1 | DashedRing | Proporción on:off. |
| `_Rotation` | radianes | DashedRing | Ángulo de rotación. |
| `_ArcStart` | radianes | Arc, **Sector** | Ángulo inicial (0 = +X). |
| `_ArcSweep` | radianes | Arc, **Sector** | Amplitud del arco/sector. |
| `_DashLength` | float | DashedSegment | Largo del dash (m). |
| `_DashGap` | float | DashedSegment | Separación entre dashes (m). |
| `_DashOffset` | float | DashedSegment | Offset de fase (cambia cada frame para flujo). |
| `_HeadLength` | float | Arrow | Largo de la punta. |
| `_HeadWidth` | float | Arrow | Ancho de la punta. |
| `_SrcBlend` | blend | material | Generalmente One (aditivo) o SrcAlpha (opaco). |
| `_DstBlend` | blend | material | Generalmente One (aditivo) o OneMinusSrcAlpha (opaco). |

## Sector (Shape 7, S102 NUEVO)

```csharp
public static void Sector(Vector3 center, float radius, float startAngle, float sweep, 
                          Color color, float innerAlpha, float outerAlpha, bool additive = false)
{
    mpb.SetVector(CenterID, center);
    mpb.SetFloat(RadiusID, radius);
    mpb.SetFloat(ArcStartID, startAngle);
    mpb.SetFloat(ArcSweepID, sweep);
    mpb.SetFloat(InnerAlphaID, innerAlpha);
    mpb.SetFloat(OuterAlphaID, outerAlpha);
    mpb.SetColor(ColorID, color);
    mpb.SetFloat(ShapeID, 7f);
    
    Draw(additive ? additiveMaterial : material, center, new Vector3(radius * 2f, 1f, radius * 2f));
}
```

**Significado:**
- Dibuja un sector (pie de pastel) desde `startAngle`, barriendo `sweep` radianes
- Relleno: `innerAlpha` en el centro (origin), `outerAlpha` en el perímetro
- Usado por [[ArenaCueOverlay]] → DrawVisionCone para cono de visión
- Shader evalúa: distancia del punto a los dos radios delimitantes del arco, interpola alpha radialmente

## Invariantes S102 + S97

- **Sin GameObjects:** `Graphics.RenderMesh` + MPB es más eficiente que Gizmos u objetos.
- **Mesh reutilizable:** un quad unitario se escala/posiciona vía matriz TRS.
- **SDF anti-aliasing:** shader usa `fwidth()` para suavizar bordes.
- **Sector fill:** innerAlpha ≠ outerAlpha permite gradiente radial (común en conos de visión)
- **Convención de ángulos:** `_ArcStart` y `_Rotation` en radianes; 0 = +X, π/2 = +Z.
- **Lazy resources:** quad y MPB se crean bajo demanda.

## Conexiones

- [[ArenaCueOverlay]] (usuario, llama Configure + Draw methods en LateUpdate)
- **S102:** [[CuePathDrawer]] (delegado a Draw, usa arc/segment)
- **S102:** [[ArenaRoomCueOverlay]] (delegado a Draw, usa disc/ring/dashed)
- [[MonchiCue.shader]] (contrato de shader, shapes 0-7)
- [[CueStyleSO]] (usuarios finales pasan valores de estilo)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
