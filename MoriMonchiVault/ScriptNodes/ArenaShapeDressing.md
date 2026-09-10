---
tags: [script, world, expedition, procedural, scatter, dressing]
---

# ArenaShapeDressing.cs

**Ruta:** `World/Expedition/ArenaShapeDressing.cs`

**Responsabilidad:** Componente que genera y coloca elementos decorativos (cover, acentos, borde) dentro de una sala usando scatter procedural. Escucha Rebuilt event de ArenaShape y redecora. Determinista por semilla.

**Propiedades:**
- (privadas)

**Métodos Públicos:**
- (ninguno; lógica vía OnEnable/OnDisable)

**Ciclo de Vida:**
- OnEnable() → suscribe a shape.Rebuilt, llama Dress()
- OnDisable() → desuscribe, destruye dressing
- Dress() → crea GO "Dressing" (DontSave), llama DressCover/DressAccents/DressBorder

**Campos Serializados (S111):**

| Sección | Campos | Propósito |
|---------|--------|----------|
| Semilla | `seed` (int) | Determinismo |
| Cobertura | `coverPrefabs` (List<GO>), `coverSpacing` (float, min 0.5), `coverSkip` (float, 0-1), `coverScale` (Vector2) | Scatter denso (spacing 1.8, skip 0.2) |
| Acentos | `accentPrefabs`, `accentSpacing` (min 0.5), `accentSkip`, `accentScale` | Scatter disperso (spacing 4.5, skip 0.35) |
| Borde | `borderTreePrefabs`, `borderRockPrefabs`, `borderStep` (min 0.5), `borderInset`, `borderJitter`, `borderRockChance`, `borderEntryClear`, `treeScale`, `rockScale` | Scatter a lo largo del borde |
| General | `margin` (float, min 0) | Distancia mínima de obstáculos |

**ScatterGrid (cover + acentos):**
1. Para cada celda en grilla AABB con spacing
2. Jitter aleatorio ±0.45*spacing
3. Skip: rng.NextDouble() < skip → continúa
4. IsClear(world, margin) → validar
5. Spawn prefab aleatorio con yaw/scale random

**DressBorder (borde):**
1. ArenaShapeScatter.AlongEdge() → puntos a lo largo del contorno
2. Para cada punto:
   - IsClear(0.3) → validar
   - NearEntry() → filtrar cerca de entradas (borderEntryClear)
   - Elige tree o rock por borderRockChance
   - Spawn con escala/yaw random

**Spawn:**
- Instantiate(prefab, position, rotation, dressing.transform)
- hideFlags = DontSave
- Si !keepColliders → DestroyImmediate(colliders)

**Invariantes:**
- Determinista: mismo seed = mismo output (si ArenaShape.OutlinePolygon igual)
- margin filtra puntos demasiado cerca del borde
- borderEntryClear protege entradas de decoración
- Dressing GO se destruye completamente al OnDisable

**S111 Nuevo:**
- Componente de decoración procedural

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaShape]], [[ArenaShapeScatter]]
