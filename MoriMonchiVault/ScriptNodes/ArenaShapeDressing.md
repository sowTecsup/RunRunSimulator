---
tags: [script, world, expedition, procedural, scatter, dressing]
---

# ArenaShapeDressing.cs

**Ruta:** `World/Expedition/ArenaShapeDressing.cs`

**Responsabilidad:** Componente que genera y coloca elementos decorativos (cover, acentos, borde interior/exterior) dentro y alrededor de una sala usando scatter procedural. Escucha Rebuilt event de ArenaShape y redecora. Determinista por semilla. S113: borde exterior con dos capas (inner + outer).

**Propiedades:**
- (privadas)

**Métodos Públicos:**
- (ninguno; lógica vía OnEnable/OnDisable)

**Ciclo de Vida:**
- OnEnable() → suscribe a shape.Rebuilt, llama Dress()
- OnDisable() → desuscribe, destruye dressing
- Dress() → crea GO "Dressing" (DontSave), llama DressCover/DressAccents/DressBorder

**Campos Serializados:**

| Sección | Campos | Propósito |
|---------|--------|----------|
| Semilla | `seed` (int) | Determinismo |
| Cobertura | `coverPrefabs` (List<GO>), `coverSpacing` (float, min 0.5), `coverSkip` (float, 0-1), `coverScale` (Vector2) | Scatter denso (spacing 1.8, skip 0.2) |
| Acentos | `accentPrefabs`, `accentSpacing` (min 0.5), `accentSkip`, `accentScale` | Scatter disperso (spacing 4.5, skip 0.35) |
| Borde (interior) | `borderTreePrefabs`, `borderRockPrefabs`, `borderStep` (min 0.5), `borderInset`, `borderJitter` (0-1), `borderRockChance`, `borderEntryClear` | Scatter a lo largo del borde interior (2.6 step, 1.3 inset) |
| Borde exterior (S113) | `borderOuterStep`, `borderOuterOffset`, `borderOuterSink` | Scatter a distancia negativa (4 intentos, yOffset = sink) |
| Escalas | `treeScale`, `rockScale` | Rangos de escala por tipo |
| General | `margin` (float, min 0) | Distancia mínima de obstáculos |

**ScatterGrid (cover + acentos):**
1. Para cada celda en grilla AABB con spacing
2. Jitter aleatorio ±0.45*spacing
3. Skip: rng.NextDouble() < skip → continúa
4. IsClear(world, margin) → validar
5. Spawn prefab aleatorio con yaw/scale random
6. Colisores destruidos (sin keepColliders)

**DressBorder (interior + exterior S113):**
1. Interior:
   - AlongEdge(outline, step=borderStep, inset=borderInset, jitter)
   - filterClear=true, distancia mínima 0.3
   - NearEntry() → filtrar (borderEntryClear)
   
2. Exterior (S113, si borderOuterStep > 0):
   - AlongEdge(outline, step=borderOuterStep, inset=-borderOuterOffset, jitter)
   - filterClear=false (no chequea Clear)
   - ScatterBorder(yOffset=borderOuterSink) — desciende cada prefab
   - Colisores se mantienen en borde exterior

**Spawn:**
- Instantiate(prefab, position ± yOffset, rotation, parent)
- hideFlags = DontSave
- Si !keepColliders → DestroyImmediate(colliders) (interior sí, exterior guarda)
- Border exterior usa parent=border.transform (sub-GO de dressing)

**Invariantes:**
- Determinista: mismo seed = mismo output (si ArenaShape.OutlinePolygon igual)
- margin filtra puntos demasiado cerca del borde interior
- borderEntryClear protege entradas de decoración
- yOffset (outer sink) desciende visualmente para dar profundidad
- Dressing GO se destruye completamente al OnDisable

**S111-S113 Cambios:**
- S111: componente base de decoración procedural
- S113: borde exterior con parámetros borderOuter* (step, offset, sink), colisores retenidos, sub-GO "Border"

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaShape]], [[ArenaShapeScatter]]
