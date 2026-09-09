---
tags: [script, world, expedition, rendering, static]
---

# CueDrawer.cs

**Ruta:** `World/Expedition/CueDrawer.cs`

**Responsabilidad:** Dibujante estático en modo inmediato (`Graphics.RenderMesh`). No instancia GameObjects; en cada llamada calcula un quad en mundo, asigna propiedades al shader vía `MaterialPropertyBlock`, y renderiza. Soporta 9 formas (ring, disc, segment, arrow, dashed ring, arc, dashed segment, sector, capsule outline) con opcionales de color degradado, dash offset animable, y blend (opaco u aditivo). **S110 NUEVO:** flags públicos `AlphaScale` (multiplicador global de alfa) y `DrawBehind` (selector de material opaco/trasero). Contrato: se llama a `Configure(material, additiveMaterial)` o `Configure(material, additiveMaterial, backMaterial)` en `OnEnable`, luego cada frame en `LateUpdate()` se invocan los métodos de dibujo; el shader evalúa SDF en espacio de mundo sobre XZ con anti-aliasing por `fwidth`.

## Métodos Estáticos Públicos

**Configuración:**
- `Configure(Material material)` — establece material (opaco) global.
- `Configure(Material material, Material additiveMaterial)` — establece material opaco y aditivo.
- `Configure(Material material, Material additiveMaterial, Material backMaterial)` — **S110 NUEVO** establece material opaco, aditivo y trasero (para renderizado detrás).

**Propiedades Públicas (Flags):**
- `static float AlphaScale` — **S110 NUEVO** multiplicador global de alfa (1.0 por defecto, multiply en cada SetColor del MPB). Usado por ArenaCueOverlay para guiar GuideAlpha, excepto telegrafía que mantiene alpha=1.
- `static bool DrawBehind` — **S110 NUEVO** selector de material: si true, usa backMaterial; sino, elige entre material/additiveMaterial. Usado por ArenaRoomCueOverlay.DrawExits() para renderizar salidas detrás de guías.

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
- `material`, `additiveMaterial`, `backMaterial` (static Material) — **S110: backMaterial NUEVO** materiales configurados.
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
- `Pick(bool additive)` — **S110 NUEVO** helper que elige material según `DrawBehind` y `additive`:
  - Si `DrawBehind && backMaterial != null`: retorna backMaterial
  - Sino si `additive && additiveMaterial != null`: retorna additiveMaterial
  - Sino: retorna material
  - Usado por todos los 12 métodos de dibujo para elegir material dinámicamente
- `EnsureResources()` — lazy init de `mpb` y `quadMesh`.
- `BuildQuadMesh() → Mesh` — crea quad unitario con vértices, normales, UVs, triángulos; `hideFlags=HideAndDontSave`.

## Contrato del Shader (`MonchiCue.shader`)

| Propiedad | Rango | Forma | Significado |
|---|---|---|---|
| `_Shape` | 0-9 | — | 0 Ring, 1 Disc, 2 Segment/Capsule, 3 Arrow, 4 DashedRing, 5 Arc, 6 DashedSegment, 7 Sector, 8 DashedArc, 9 CapsuleOutline |
| `_Color` | RGBA | todas | Color primario; alfa se multiplica por AlphaScale |
| `_ColorB` | RGBA | Segment, Arrow, Arc, DashedSegment, DashedArc, CapsuleOutline | Degradado: borde, cabeza, angular, lejano, perímetro; alfa se multiplica por AlphaScale |
| `_Center` | XYZ | Ring, DashedRing, Disc, Arc, Sector | Centro en espacio de mundo. |
| `_PointA`, `_PointB` | XYZ | Segment, Arrow, DashedSegment, Capsule/CapsuleOutline | Extremos A y B. |
| `_Radius` | float | Ring, DashedRing, Disc, Arc, Sector, CapsuleOutline | Radio. |
| `_Thickness` | float | todas excepto Disc/Sector | Grosor de línea (m); Capsule: radio*2. |
| `_InnerAlpha` | 0-1 | Disc, Sector, Capsule | Alfa en el centro (Disc) o interior del sector/cápsula; se multiplica por AlphaScale |
| `_OuterAlpha` | 0-1 | Disc, Sector, Capsule | Alfa en el borde; se multiplica por AlphaScale |
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

## Invariantes S102 + S108 + S110

- **Sin GameObjects:** `Graphics.RenderMesh` + MPB es más eficiente que Gizmos u objetos.
- **Mesh reutilizable:** un quad unitario se escala/posiciona vía matriz TRS.
- **SDF anti-aliasing:** shader usa `fwidth()` para suavizar bordes.
- **Sector fill:** innerAlpha ≠ outerAlpha permite gradiente radial (común en conos de visión)
- **Capsule fill:** innerAlpha ≠ outerAlpha permite degradado desde eje a borde (común en pistas)
- **CapsuleOutline:** contorno con grosor variable, ideal para bordes de plantillas de combate
- **Convención de ángulos:** `_ArcStart` y `_Rotation` en radianes; 0 = +X, π/2 = +Z.
- **Lazy resources:** quad y MPB se crean bajo demanda.
- **AlphaScale (S110):** multiplicador global que NO afecta shapes específicos (se multiplica en SetColor). Llamador (ArenaCueOverlay) setea AlphaScale = style.GuideAlpha, luego en Telegraph setea = 1f.
- **DrawBehind (S110):** flag temporal (se resetea cada LateUpdate si es necesario). backMaterial tiene cola renderizado 2990 (siempre detrás).
- **Pick helper (S110):** elige material dinámicamente; permite ExitZones renderizar detrás mientras otros usan material normal.

## Cambios S108

- **Métodos Capsule y CapsuleOutline:** nuevos (shapes 2 y 9)
- Línea 31 en MD: actualiza Forma a 9 para CapsuleOutline

## Cambios S110

- **Campos públicos AlphaScale y DrawBehind:** nuevos flags
- **Sobrecarga Configure(material, additiveMaterial, backMaterial):** nueva
- **Método Pick(additive):** helper privado nuevo
- Todos los 12 métodos de dibujo usan `Pick(additive)` en lugar de seleccionar directamente material/additiveMaterial
- SetColor: `color.a *= AlphaScale` (multiplicación antes de escribir en MPB)
- SetColor (ColorB): `colorB.a *= AlphaScale` (multiplicación)
- SetFloat (InnerAlpha/OuterAlpha): `value *= AlphaScale` (multiplicación)

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

- [[ArenaCueOverlay]] — usuario, configura y llama métodos en LateUpdate (S110: setea AlphaScale/DrawBehind)
- [[ArenaRoomCueOverlay]] — usuario, configura con backMaterial, setea DrawBehind para DrawExits
- [[CueRibbonDrawer]] — hermano estático, renderiza cintas parabólicas
- [[CuePathDrawer]] — usa métodos de CueDrawer para rutas
- [[CreatureCueDrawer]] — usa métodos de CueDrawer para guías individuales
- [[MonchiCue.shader]] — contrato de shader, shapes 0-9

