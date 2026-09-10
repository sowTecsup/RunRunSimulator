---
tags: [script, world, expedition, placement, landmarks]
---

# ArenaLandmarks.cs

**Ruta:** `World/Expedition/ArenaLandmarks.cs`

**Responsabilidad:** Coloca hitos grandes (boulders, groves, pools) en banda radial alrededor del centro, con validación de separación y proximidad a zonas seguras. S113: nuevo sistema de decoración grande que reemplaza scatter manual.

**Método Principal:**
- `int Place(System.Random rng, Transform parent, ArenaShape shape, Vector3 center, float edgeMargin, float centerRadius, IReadOnlyList<Vector3> safePoints, float safeRadius, List<Vector3> placedOut)` — coloca 1..count hitos, retorna cantidad colocada

**Propiedades:**
- `IReadOnlyList<Vector4> Placed { get; }` — historial de colocadas (XYZ posición + radio)

**Campos Serializados:**
- **Prefabs:**
  - `boulderPrefabs` (List<GameObject>) — rocas grandes
  - `grovePrefabs` (List<GameObject>) — grupos de árboles
  - `poolPrefabs` (List<GameObject>) — charcos/lagos pequeños

- **Cantidad:**
  - `count` (Vector2Int) — rango [min, max] de hitos a colocar

- **Reparto (banda radial + simetría):**
  - `bandInner, bandOuter` (float) — radio mín/máx de colocación (10-24 default)
  - `mirror` (bool) — espeja cada colocación 180° alrededor del centro
  - `minSeparation` (float) — distancia mín entre pares en la misma sesión (11)
  - `minFromSmall` (float) — distancia mín a obstáculos pequeños previos (4)
  - `scale` (Vector2) — rango de escala (0.9-1.25 default)
  - `clearMargin` (float) — margen de borde desde ArenaShape.IsClear (2.5)
  - `attempts` (int) — intentos por punto (14)

**Algoritmo de Colocación:**
1. TryFindPoint(rng, shape, center, safePoints):
   - Loop `attempts` veces
   - shape.TryRandomPoint(margin=edgeMargin) → candidate
   - IsValidCandidate():
     - distancia planar = [bandInner, bandOuter]
     - no cerca de safePoints (safeRadius)
     - no cerca de previos en sesión (minSeparation)
     - no cerca de previos globales (minFromSmall)
     - shape.IsClear(clearMargin)
   - Si mirror: valida también el espejado
2. Spawn instancia + opcional mirror (yaw + 180°)
3. Calcula radio visual desde bounds de renderer (MiniRadius)

**Invariantes:**
- mirror=true → cantidad colocada es par (o impar si falla última)
- MiniRadius ≥ 2f default si sin renderers
- MirrorPoint = center + (-ΔX, ΔY, -ΔZ) ➜ simetría 180° en XZ

**Conexiones:** [[ArenaLayoutBuilder]], [[ArenaShape]], [[ArenaSandbox]]
