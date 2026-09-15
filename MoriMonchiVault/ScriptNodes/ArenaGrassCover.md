---
tags: [script, world, expedition, utility, procedural]
---

# ArenaGrassCover.cs

**Ruta:** `World/Expedition/ArenaGrassCover.cs`

**Responsabilidad:** Generador procedural de densidad de pasto. Sampler Voronoi con tres capas (grumos densos, halos difusos, parches Perlin) + variación suelta. Genera campo de valores [0,1] que ArenaGrassField consulta por raíz para decidir probabilidad de brizna y escala de altura. **S117 NUEVO:** reemplaza mapa estático, habilitando pasto dinámico que se limpia alrededor de spawns y conserva huella de pasos.

**Estructura Pública:**

```csharp
[System.Serializable]
public struct Settings
{
    [Min(0.5f)] public float clumpSpacing;      // 3 m default: espaciado de grilla Voronoi
    [Min(0.1f)] public float clumpRadius;       // 1 m default: radio base de grumo denso
    [Range(0f, 1f)] public float haloDensity;   // 0.15 default: factor de densidad en halo
    [Min(0.5f)] public float patchScale;        // 6 m default: escala de ruido Perlin
    [Range(0f, 1f)] public float patchThreshold; // 0.55 default: umbral para parches Perlin
    [Range(0f, 1f)] public float patchDensity;  // 0.55 default: factor máximo de parches
    [Range(0f, 1f)] public float looseDensity;  // 0.06 default: factor de ruido ambiental
    [Range(0f, 1f)] public float looseHeight;   // 0.65 default: escala de altura suelta
}
```

**Métodos Públicos:**

- `static float Sample(Vector2 point, int seed, in Settings s, out float heightScale) → float`
  - Sampler principal: retorna densidad [0,1] en un punto (x,z)
  - Salida secondary: heightScale para variar altura de briznas según densidad
  - Algoritmo en 4 capas:
    1. **Grumos (Voronoi):** busca centro Voronoi más cercano (grilla de clumpSpacing); calcula distancia suave a radio; produce pico denso [0,1]
    2. **Halos:** si en rango [bestRadius, bestRadius*2], aplica halo difuso con densidad reducida
    3. **Parches (Perlin):** ruido Perlin a escala patchScale; umbral patchThreshold; produce manchas orgánicas
    4. **Suelta:** background constant looseDensity
  - Densidad final = max(grumo, halo, parche, suelta), clamped [0,1]
  - heightScale = lerp(looseHeight, 1.1, density) para variar altura según densidad local

**Campos Internos:**
- `cellX`, `cellZ` — celda Voronoi que contiene el punto
- `bestDistance`, `bestRadius` — distancia/radio al centro más cercano
- Loop 3x3 sobre celdas vecinas para garantizar mínimo global
- `clump` — altura suave desde radio (SmoothStep [0,1])
- `halo` — anillo de transición difuso
- `noise` — valor Perlin [0,1]
- `patch` — SmoothStep de noise con umbral

**Métodos Privados:**

- `static float Hash01(int a, int b, int c) → float`
  - Hash determinista seeded (reproductor)
  - Retorna [0,1] reproducible para offset Voronoi, radius, noise
  - Usa FNV-like mixing para dispersión

**Integración:**

- Llamado desde ArenaGrassField.Sow() por cada raíz candidata:
  ```csharp
  float density = ArenaGrassCover.Sample(new Vector2(x, z), seed, in cover, out float heightScale);
  ```
- Densidad determina probabilidad de inclusión: `rng.NextDouble() >= density ? skip : include`
- heightScale se pasa a ArenaGrassBlades.Build() para modular altura final

- Llamado en ArenaGrassField.ClearAround() tras spawn con zones = zonas de despeje

**Invariantes S117:**

- Settings.Default seteada: clumpSpacing=3, clumpRadius=1, haloDensity=0.15, patchScale=6, patchThreshold=0.55, patchDensity=0.55, looseDensity=0.06, looseHeight=0.65
- Hash01 es determinista (mismo seed → mismo mapa entre sesiones)
- Densidad siempre [0,1]; heightScale siempre [looseHeight, 1.1]
- Grumos Voronoi se distribuyen regularmente con aleatoriedad controlada de offset/radius

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion]], [[Index/22 - Bajada Nocturna y Linaje]], S117

**Conexiones:** [[ArenaGrassField]], [[ArenaGrassBlades]], [[ArenaShape]]
