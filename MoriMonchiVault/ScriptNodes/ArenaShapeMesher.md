---
tags: [script, world, expedition, procedural, mesh, utility]
---

# ArenaShapeMesher.cs

**Ruta:** `World/Expedition/ArenaShapeMesher.cs`

**Responsabilidad:** Utilidad estática para generación de mallas y operaciones geométricas 2D. Muestrea splines a polígonos, valida contornos (CW/CCW), triangula con agujeros, genera skirts (paredes verticales), caps (techos). Usada por ArenaShape en construcción y por ArenaShapeBrush en contornos.

**Métodos Públicos:**

| Método | Propósito |
|--------|----------|
| `List<Vector2> Sample(Spline, Transform, float samplesPerMeter, bool closed)` | Muestrea spline a puntos 2D con densidad |
| `List<Vector2> Symmetrize(IReadOnlyList<Vector2> half, Vector2 center)` | Espeja poligono 180° y lo concatena |
| `List<Vector2> Rotate180(IReadOnlyList<Vector2> polygon, Vector2 center)` | Espeja poligono alrededor de center |
| `float SignedArea(IReadOnlyList<Vector2> polygon)` | Área firmada (>0=CCW, <0=CW) |
| `void MakeCounterClockwise(List<Vector2> polygon)` | Invierte si es CW |
| `void MakeClockwise(List<Vector2> polygon)` | Invierte si es CCW |
| `bool Contains(IReadOnlyList<Vector2> polygon, Vector2 point)` | Point-in-polygon (ray casting) |
| `float DistanceToEdge(IReadOnlyList<Vector2> polygon, Vector2 point)` | Distancia a borde más cercano |
| `Bounds Bounds(IReadOnlyList<Vector2> polygon)` | AABB 2D del poligono |
| `Mesh Floor(IReadOnlyList<Vector2> outline, IReadOnlyList<IReadOnlyList<Vector2>> holes, float y, float uvScale)` | Triangula contorno con agujeros (ProBuilder) |
| `Mesh Cap(IReadOnlyList<Vector2> polygon, float y, float uvScale)` | Floor sin agujeros (techo) |
| `Mesh Skirt(IReadOnlyList<Vector2> loop, float top, float bottom, bool faceInterior, float uvScale)` | Paredes verticales (4 vértices/borde) |

**Triangulación (Floor):**
1. Copia outline (CCW)
2. Copia holes (CW por convención)
3. Usa ProBuilder.CreateShapeFromPolygon() (earcut interno)
4. Reorienta triángulos si cross product < 0 (winding)
5. Genera UVs: (x, z) * uvScale
6. Normals: todas (0, 1, 0)

**Skirt (paredes):**
- 4 vértices por borde (aBottom, bBottom, bTop, aTop)
- Normal perpendicular al borde (CCW→interior, CW→exterior)
- UVs: u = longitud acumulada, v = 0..1 (bottom..top)
- Winding se valida vs normal (cross product)

**Invariantes:**
- Sample() con closed=true: [0, n) puntos
- Sample() con closed=false: [0, n] puntos (incluye endpoint)
- Rotate180() pre-multiplica por center, post suma
- SignedArea() es shoelace: (a.x*b.y - b.x*a.y)*0.5
- DistanceToSegment() proyecta con Dot product

**S111 Nuevo:**
- Utilities reutilizables de S110 (piso, acantilado, rocas, agua)
- ProBuilder earcut integrado (sin dependencia externa)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaShape]], [[ArenaShapeBrush]], [[ArenaShapeMask]]
