---
tags: [script, world, expedition, procedural, generation, light-shafts]
---

# ArenaShapeShafts.cs

**Ruta:** `World/Expedition/ArenaShapeShafts.cs`

**Responsabilidad:** Coloca rayos de luz falsos (light shafts) en la sala de expedición de forma determinista por semilla. Componente decorativo que escucha ArenaShape.Rebuilt y se regenera. S113: rayos direccionales con align-to-sun o tilt configurable.

**Métodos Públicos:** ninguno (solo Rebuild via event)

**Campos Serializados:**
- **Semilla:**
  - `seed` (int, default 3) — hash base para RNG

- **Rayos:**
  - `shaftPrefab` (GameObject) — modelo del rayo (requiere ser light shaft visual)
  - `count` (Vector2Int) — rango [min, max] de rayos (1-2 default)
  - `scale` (Vector2) — rango de escala del rayo (0.9-1.5)
  - `margin` (float) — margen de borde al muestrear puntos (6)
  - `alignToSun` (bool) — alinea con RenderSettings.sun.forward si existe
  - `tilt` (float) — inclinación base si no hay sun (14°)
  - `spread` (float) — variación de tilt por rayo (10°)

**Ciclo de Regeneración:**
1. OnEnable(): suscribe a shape.Rebuilt += Dress
2. Dress() (llamado por Rebuilt event o OnValidate):
   - DestroyShafts() (limpia generados previos)
   - Crea GameObject("Shafts") con hideFlags=DontSave
   - Calcula shapeHash = seed ^ outlineCount ^ boundsSize (determinista)
   - Itera `amount` veces (1-2):
     - TryRandomPoint(4 intentos) → world
     - Resuelve rotación: si alignToSun → Quaternion.FromToRotation(up, -sun.forward) * Euler(var, yaw)
     - Si no sun → Euler(tilt + var, yaw)
     - Spawn instancia + escala + destruye colliders

**Invariantes:**
- shapeHash usa OutlinePolygon.Count + tamaño de bounds (reproducible)
- Rayos son puramente visuales (colisiones destruidas)
- hideFlags=DontSave → no se guardan en escena

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaShape]]
