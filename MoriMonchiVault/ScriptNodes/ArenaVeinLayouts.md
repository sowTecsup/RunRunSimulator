---
tags: [script, utility, expedition]
---

# ArenaVeinLayouts.cs

**Ruta:** `World/Expedition/ArenaVeinLayouts.cs`

**Responsabilidad:** Utilidad estática que genera patrones de distribución de vetas de minerales dentro de un radio anular (entre `innerRadius` y `outerRadius`). Enum `Pattern` con cuatro tipos: Racimos (dos grupos opuestos), Anillo (distribución regular), Franja (línea con jitter lateral), Lobulos (radios alternados). Método `Candidates()` genera lista de puntos candidatos; todos se restringen a un radio mínimo después. Determinista por `System.Random`.

**Vinculado a:** [[Index/06 - World Architecture]], S114

**Conexiones:** [[ArenaLayoutBuilder]], [[MaterialPickup]]

**Enum y Métodos Públicos**

| Miembro | Descripción |
|---------|-------------|
| `Pattern.Racimos` | Dos clusters opuestos con distribución local |
| `Pattern.Anillo` | Puntos dispuestos en círculo regular |
| `Pattern.Franja` | Línea a lo largo de una dirección con variación lateral |
| `Pattern.Lobulos` | Radios dirigidos desde el centro |
| `Pick(rng)` | Elige un patrón aleatorio (0-3) |
| `Name(pattern)` | Retorna nombre legible del patrón |
| `Candidates(pattern, rng, center, acrossDirection, innerRadius, outerRadius, count)` | Lista de puntos; todos clampeados a `innerRadius` mínimo |

**Detalles S114**

Cada patrón usa jitter controlado (`RandomJitter`, `RandomAngle`) para variación sin perder el aspecto general. La dirección `acrossDirection` (del eje largo de la sala) orienta la franja. Los pares de vetas en `ArenaLayoutBuilder` siempre son pares (count siempre par por validación) y se validan contra el radio de los hitos (`ArenaLandmarks`). S114 introduce garantía de pares válidos.
