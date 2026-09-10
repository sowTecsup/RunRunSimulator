---
tags: [script, world, expedition, procedural, scatter, utility]
---

# ArenaShapeScatter.cs

**Ruta:** `World/Expedition/ArenaShapeScatter.cs`

**Responsabilidad:** Utilidad estática para scatter (distribución) de puntos dentro o a lo largo de polígonos. Genera posiciones para objetos decorativos (árboles, rocas, pasto de borde) respetando espaciado y márgenes.

**Métodos Públicos:**

| Método | Propósito |
|--------|----------|
| `List<Vector2> InPolygon(IReadOnlyList<Vector2> polygon, System.Random rng, float spacing, float edgeMargin)` | Scatter dentro de poligono |
| `List<Vector2> AlongEdge(IReadOnlyList<Vector2> loop, System.Random rng, float step, float inset, float jitter)` | Scatter a lo largo del borde (inset) |

**InPolygon:**
- Random en AABB del poligono (400 intentos máximo)
- Valida: Contains(polygon), DistanceToEdge >= edgeMargin, TooClose(result, candidate, spacing)
- Rompe si 60 fracasos consecutivos
- Retorna lista de puntos que cumplen restricciones

**AlongEdge:**
- Itera bordes del poligono (CCW o CW)
- Calcula normal perpendicular (inset hacia interior)
- Marca de paso inicial con jitter
- Mientras marca <= traveled+edgeLength: interpola punto, agrega, suma step
- AlongEdge retorna puntos desplazados por normal*inset

**Invariantes:**
- spacing es radio mínimo de exclusión (TooClose comprueba distancia euclidea)
- edgeMargin es separación mínima del borde
- jitter es fracción de step (±jitter*step)
- Determinista: mismo rng seed = mismo output

**S111 Nuevo:**
- Utilities de distribución (usado por ArenaShape para groves/edge, ArenaShapeDressing para cover/accents/border)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaShape]], [[ArenaShapeDressing]], [[ArenaShapeMesher]]
