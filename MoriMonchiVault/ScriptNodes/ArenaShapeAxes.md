---
tags: [script, utility, expedition]
---

# ArenaShapeAxes.cs

**Ruta:** `World/Expedition/ArenaShapeAxes.cs`

**Responsabilidad:** Utilidad estática que encuentra los ejes principales de un polígono (para orientar la cámara RTS y nombrar direcciones de entrada). Estructura `Axis` (puntos A y B + longitud). Método `Longest()` muestrea pares de puntos del contorno, filtra por ángulo mínimo entre direcciones y longitud relativa, prioriza ejes que pasan cerca del centro. Método `Name()` clasifica una dirección como "norte-sur", "este-oeste" o una diagonal.

**Vinculado a:** [[Index/06 - World Architecture]], S114

**Conexiones:** [[ArenaLayoutBuilder]], [[ArenaRtsCamera]]

**Métodos Públicos**

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Longest(polygon, center, max, minAngleDegrees, minLengthRatio, maxMidOffsetRatio)` | `List<Axis>` | Ejes principales del polígono (down-sampled si >256 vértices) |
| `Name(direction)` | `string` | Etiqueta de la dirección más cercana (norte-sur, este-oeste, diagonal, diagonal inversa) |

**Detalles S114**

Búsqueda de ejes por comparación de pares: O(n²) pero reducida a max 256 vértices muestreados. El eje más largo pasa a `result` siempre; los siguientes se añaden solo si mantienen un ángulo mínimo con los ya seleccionados y superan una fracción de la longitud principal. Usado por `ArenaLayoutBuilder` para rotar la vista inicial de la cámara alineada al eje largo de la sala.
