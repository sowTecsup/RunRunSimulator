---
tags: [script, world, expedition, graphics, math]
---

# ArenaGrassBlades.cs

**Ruta:** `World/Expedition/ArenaGrassBlades.cs`

**Responsabilidad:** Utilidad estática (matemática pura) que arma una malla de briznas de pasto. Recibe lista de raíces (Vector3), parámetros de altura/ancho/inclinación, y **S117:** opcionalmente lista de heightScales por densidad procedural, genera malla con 3 vértices + 1 triángulo por brizna. Una brizna es un triángulo isóceles simple (base en raíz, punta inclinada), con normal suavizada y color aleatorio por hue. Sin estado, sin referencias.

**Método público estático:**

- `Build(IReadOnlyList<Vector3> roots, System.Random rng, Vector2 heightRange, Vector2 widthRange, float tilt, Vector3 origin, IReadOnlyList<float> heightScales = null) → Mesh`
  - Valida roots != null && Count > 0, retorna null si falla
  - **S117:** heightScales opcional: si presente, aplica scale[i] * height a cada brizna
  - Retorna Mesh(name="ArenaGrassBlades", hideFlags.DontSave) con IndexFormat seleccionado (UInt32 si >65k verts)

**Algoritmo Build():**

1. **Inicialización:**
   - Aloca vertices, normals, uvs, colors, triangles (capacity = roots.Count * 3)

2. **Loop por raíz (i < roots.Count):**
   - Samplea height, width, yaw aleatorios del RNG
   - **S117:** si heightScales != null y i < Count: `height *= heightScales[i]` (modulación por densidad)
   - Calcula sideAxis perpendicular al yaw (horizontal)
   - Samplea leanAngle, leanDirection (inclinación)
   - Computa leanAxis (dirección de inclinación)

3. **Geometría de brizna:**
   - `rootLeft = root + sideAxis * (width * 0.5)`
   - `rootRight = root - sideAxis * (width * 0.5)`
   - `tip = root + up * height + leanAxis * (sin(leanAngle) * height)`
   - 3 vértices: rootLeft, rootRight, tip

4. **Normales y UVs:**
   - Normal (constante para los 3): `(up + sideAxis * 0.25).normalized` — suavizador 0.25 en sideAxis
   - UVs triángulo: (0,0), (1,0), (0.5,1) — base ancha, punta al medio

5. **Color aleatorio:**
   - Samplea hue único por brizna
   - `Color(hue, hue, hue, 1)` — escala de grises procedural

6. **Triángulo:**
   - Winding: baseIndex, baseIndex+2, baseIndex+1 (CCW cuando mira desde adelante)

7. **Finalización:**
   - SetVertices, SetNormals, SetUVs, SetColors, SetTriangles
   - RecalculateBounds()

**Internals:**
- **Coordenadas relativas:** root se calcula como `roots[i] - origin` (para aplicar offset del chunk en Sow)
- **Aproximación visual:** solo 3 verts/brizna, sin LOD (simplicidad vs rendering cost)
- **Hue escala:** color en escala 0-1 (shader debe interpretarlo)
- **S117:** heightScales modula altura per-brizna según densidad procedural (grumos más altos, sueltas más bajas)

**Invariantes:**
- **Determinismo:** RNG producido externamente (Sow pasa seed-based System.Random)
- **Sin estado:** Build() es función pura, ningún side effect
- **Malla única:** se crea una nueva Mesh por cada chunk (no reutilización)
- **IndexFormat adaptive:** elige UInt16 si <= 65k, UInt32 si mayor (evita overflow)
- **Triangulación simple:** 1 triángulo/brizna, sin degenerados
- **Normals constantes:** economía (no cálculo per-vertex), suficiente para look billboard
- **S117:** heightScales alineado con roots (ambos crecen en Build de ArenaGrassField)

**S117 Cambios:**

- Parámetro `IReadOnlyList<float> heightScales` opcional
- Aplicación de scale[i] * height para modular altura según densidad (ArenaGrassCover.Sample)

**Conexiones:** [[ArenaGrassField]] (cliente único)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion]], [[Index/22 - Bajada Nocturna y Linaje]], S117
