---
tags: [script, world, expedition, procedural, mask, marching-squares]
---

# ArenaShapeMask.cs

**Ruta:** `World/Expedition/ArenaShapeMask.cs`

**Responsabilidad:** Utilidad estática para manipulación de máscaras binarias 2D (grilla de bytes). Pintura con pincel, simetrización, generación de blobs procedurales, extracción de contornos (marching squares), simplificación de polígonos (Douglas-Peucker). Núcleo de ArenaShapeBrush.

**Estrutura:**
```csharp
public struct BlobParams
{
    float CenterRadius;  // radio de centro
    int Count;           // cantidad de blobs satélites
    Vector2 Radius;      // rango (min, max) de radios
    float Spread;        // radio máximo de expansión
    float Overlap;       // factor de solapamiento (0.3-1.0)
}
```

**Métodos Públicos:**

| Método | Propósito |
|--------|----------|
| `Vector2 CellCenter(int i, int j, float cell, Vector2 origin)` | Centro de celda (i, j) |
| `bool Get(byte[] mask, int size, int i, int j)` | Consulta si celda está activa |
| `void Clear(byte[] mask)` | Llena con 0s |
| `void Paint(byte[] mask, int size, float cell, Vector2 origin, Vector2 center, float radius, bool value)` | Pinta disco en máscara |
| `void Symmetrize(byte[] mask, int size)` | Espeja 180° (central) |
| `void Blobs(byte[] mask, int size, float cell, Vector2 origin, System.Random rng, BlobParams p)` | Genera blobs procedurales |
| `List<List<Vector2>> Contours(byte[] mask, int size, float cell, Vector2 origin)` | Extrae contornos (marching squares) |
| `List<Vector2> Simplify(IReadOnlyList<Vector2> loop, float tolerance)` | Simplifica poligono (Douglas-Peucker) |
| `float SignedArea(IReadOnlyList<Vector2> polygon)` | Área firmada |
| `bool Contains(IReadOnlyList<Vector2> polygon, Vector2 point)` | Point-in-polygon |

**Paint:**
- Itera celdas en AABB del disco (iMin..iMax, jMin..jMax)
- Si distancia(cellCenter, center) <= radius → mask[j*size+i] = value

**Symmetrize:**
- Copia máscara
- Escribe: mask[j*size+i] |= copy[(size-1-j)*size+(size-1-i)]
- Asegura simetría 180° alrededor de (size/2, size/2)

**Blobs:**
1. Clear(mask)
2. Paint centro con CenterRadius
3. Para cada blob satélite (0..Count-1):
   - Radio aleatorio en Radius
   - Ángulo aleatorio (0..2π)
   - Centro = prevCenter + dir * (prevRadius+r)*Overlap
   - Si distancia > Spread → clampea
   - Paint(center, radius, true)
4. Si simétrico → Symmetrize()

**Contours (Marching Squares):**
1. Genera segmentos de línea según tabla de casos (4 bits = CW-ordering de 4 celdas)
2. 16 casos: 0 (nada), 15 (todo) → skip; otros → 1-2 segmentos
3. LinkSegments() encadena segmentos en loops cerrados (búsqueda de vecinos por key)
4. Retorna lista de polígonos

**Simplify (Douglas-Peucker):**
1. Encuentra mayor distancia entre puntos (divide en 2 cadenas)
2. Recursivamente simplifica cada cadena con tolerance
3. Asegura >=6 puntos (reintentos con tolerance/2)

**Invariantes:**
- Máscara: tamaño size*size bytes
- Coordinate system: (i=X, j=Z)
- Contours retorna loops >= 3 puntos
- Simplify si originalmente < 6 puntos → retorna original

**S111 Nuevo:**
- Núcleo de pincelado procedural de ArenaShapeBrush

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaShapeBrush]]
