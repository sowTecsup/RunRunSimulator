---
tags: [script, world, expedition, procedural, scatter, dressing]
---

# ArenaShapeDressing.cs

**Ruta:** `World/Expedition/ArenaShapeDressing.cs`

**Responsabilidad:** Componente que genera y coloca elementos decorativos (cover, acentos, borde interior/exterior) dentro y alrededor de una sala usando scatter procedural. Escucha Rebuilt event de ArenaShape y redecora. Determinista por semilla. S113: borde exterior con dos capas (inner + outer). **S115:** Método público nuevo `ClearAround(IReadOnlyList<Vector4> zones)` que destruye hijos directos del GO Dressing (salvo border), cuya distancia planar a alguna zona sea menor que radio de zona; usado por ArenaLayoutBuilder al final para limpiar piezas decorativas que caen dentro de exclusiones de vetas/centro.

**Propiedades:**
- (privadas)

**Métodos Públicos:**
- `void ClearAround(IReadOnlyList<Vector4> zones)` — **S115 NUEVO** destruye piezas decorativas dentro de zonas de exclusión

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

## Cambios S115

**ClearAround(IReadOnlyList<Vector4> zones) — NUEVO MÉTODO PÚBLICO (línea 200-222):**
```csharp
public void ClearAround(IReadOnlyList<Vector4> zones)
{
    if (dressing == null || zones == null || zones.Count == 0) return;

    for (int i = dressing.transform.childCount - 1; i >= 0; i--)
    {
        var child = dressing.transform.GetChild(i);
        if (child.gameObject == border) continue;  // Salta border

        for (int z = 0; z < zones.Count; z++)
        {
            var zone = zones[z];
            var center = new Vector3(zone.x, zone.y, zone.z);

            if (PlanarDistance(child.position, center) < zone.w)  // Validación XZ
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
                break;
            }
        }
    }
}
```

**Propósito S115:**
- Llamado por ArenaLayoutBuilder.Build() al final tras BuildDecor
- Recibe lista de Vector4: (center.x, center.y, center.z, radius)
- Itera hijos directos de Dressing GO
- Salta GO "Border" (borde exterior se protege)
- Para cada zona: calcula distancia planar (XZ, ignorar Y) entre posición de hijo y centro de zona
- Si distancia < radius de zona: destruye el hijo
- Contexto: limpia piezas decorativas (cover/acentos/borde interior) que caen dentro de radio de vetas o doble-radio del centro

**Uso en BuildDecor:**
- ArenaLayoutBuilder.Build() línea 188:
  ```csharp
  if (activeShape != null)
  {
      var dressing = activeShape.GetComponent<ArenaShapeDressing>();
      if (dressing != null) dressing.ClearAround(DecorClearZones(center));
  }
  ```
- Se ejecuta DESPUÉS de BuildDecor (que ya validó InsideAnyZone en spawn)
- ClearAround() es segunda pasada: destruye cualquier pieza que haya entrado (edge cases planar)

**PlanarDistance helper (línea 195-198):**
```csharp
private static float PlanarDistance(Vector3 a, Vector3 b)
{
    return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
}
```
- Calcula distancia XZ (horizontal, ignorar altura)
- Usado por ClearAround y NearEntry

**Invariantes S115:**
- ClearAround valida distancia planar (XZ): islas flotantes no interfieren
- Border siempre se preserva (no se limpia)
- Destroy vs DestroyImmediate: branching por Play/Edit mode
- Zonas son siempre Vector4 con (x, y, z, radius)

**S111-S113-S115 Cambios:**
- S111: componente base de decoración procedural
- S113: borde exterior con parámetros borderOuter* (step, offset, sink), colisores retenidos, sub-GO "Border"
- S115: método público ClearAround(zones) con validación planar, llamado al final de Build()

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], S115

**Conexiones:** [[ArenaShape]], [[ArenaShapeScatter]], [[ArenaLayoutBuilder]]

