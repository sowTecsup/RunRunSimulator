---
tags: [script, world, expedition, visualization, cues]
---

# ArenaRoomCueOverlay.cs

**Ruta:** `World/Expedition/ArenaRoomCueOverlay.cs`

**Responsabilidad:** Dibuja guías visuales en overlay de sala: minerales (discos animados), salidas (anillos giratorio), pizarrones de equipo (anillos de vetas conocidas, pings), contorno y obstáculos grandes (S113), spawns (S113). S113: DrawOutline, DrawSpawns, DrawObstacles nuevos; configuración unificada vía CueStyleSO. **S114:** `outlineInset` para desplazar contorno hacia adentro (evita overlap con borde visual).

**Campos Serializados:**
- `sandbox` [Required] — ArenaSandbox
- `cueMaterial`, `additiveMaterial` [Required] — CueDrawer
- `backMaterial` [Required] — material para renderizado detrás (S110)
- `style` [Required] — CueStyleSO (estilos parametrizados para todos los elementos)
- **S114 NUEVO:** `outlineInset` (float) — inset hacia adentro del contorno (default 0.1 m)
- **Toggles:**
  - `showMinerals` (bool, default true)
  - `showExits` (bool, default true)
  - `showBlackboards` (bool, default true)
  - `showOutline` (bool, default true) — S113
  - `showLode` (bool, default true)
  - `showSpawns` (bool, default true) — S113
  - `showObstacles` (bool, default true) — S113

**Caches Privados:**
- `mineralAnims` (Dictionary<MaterialPickup, MineralAnim>)
- `mineralQueryBuffer`, `mineralLookup` (query reutilizable, no-alloc)

**LateUpdate() (S103-S113-S114):**
1. Setea `CueDrawer.AlphaScale = style.GuideAlpha`
2. Si showMinerals: DrawMinerals()
3. Si showLode: DrawLode()
4. Si showExits: DrawBehind=true, DrawExits(), DrawBehind=false
5. Si showBlackboards: DrawBlackboards()
6. Si showOutline: DrawOutline() — S113, **S114: usa outlineInset**
7. Si showSpawns: DrawSpawns() — S113
8. Si showObstacles: DrawBehind=true, DrawObstacles(), DrawBehind=false — S113
9. Resetea `CueDrawer.AlphaScale = 1f`

**DrawMinerals() (S103+):**
- Query no-alloc de perceivables (MaterialPickup)
- Anima alpha de cada mineral (MoveTowards, Taken → 0, Available → 1)
- Dibuja disco con degradado (MineralColor, inner/outer alpha)
- Si Value > 1, agrega anillo dasheado giratorio (1.6x radio)

**DrawExits() (S103+):**
- Itera sandbox.Exits
- Color según team (FriendColor Player, FoeColor Rival)
- Dibuja disco (ExitAlpha) + anillo + anillo dasheado giratorio
- Renderizado con DrawBehind=true (pasa detrás de otras guías)

**DrawBlackboards() (S103+):**
- Itera teams [Player, Rival]
- Obtiene board = sandbox.BoardFor(team)
- Color según team (FriendColor Player, FoeColor Rival)
- **Vetas conocidas (KnownVeins):**
  - Para cada k en board.KnownVeins (no tomada, activa)
  - Anillo dasheado a distancia (radius + offset + extra si Rival)
  - Spin según team: Player +, Rival - (direcciones opuestas)
  - Alpha = KnownVeinRingAlpha
- **Pings (reportes frescos):**
  - Prune pings viejos con PrunePings(Time.time)
  - Para cada ping:
    - t = (now - ping.Time) / PingSeconds (0 a 1)
    - radius = Lerp(0.4, PingRadius, t) — crece
    - alpha = PingAlpha * (1 - t) — desvanece
    - Dibuja ring expansivo

**DrawLode() (S103+):**
- Busca mineral con IsLode=true en mineralQueryBuffer
- Dibuja anillo dasheado giratorio + pequeño anillo interior
- Color LodeColor, radius LodeRadius

**DrawOutline() (S113, S114 ACTUALIZADO):**
- shape = sandbox.ActiveShape (null → return)
- polygon = shape.OutlinePolygon
- **S114:** contrae polygon hacia el interior por outlineInset (usando normal inward)
- Itera polygon con stride (OutlineStride)
- Dibuja segmentos dasheados con scroll (time * OutlineScrollSpeed)
- Color OutlineColor, thickness OutlineThickness

**DrawSpawns() (S113 NUEVO):**
- Itera teams [Player, Rival]
- Obtiene SpawnPoint(team) desde sandbox
- Dibuja disco pequeño (radius SpawnRadius, alpha 0.08)
- Anillo dasheado giratorio (SpawnDashCount, RingDashRatio, SpawnSpinSpeed)
- Color según team (FriendColor/FoeColor)

**DrawObstacles() (S113 NUEVO):**
- placed = sandbox.PlacedObstacles (landmarks.Placed)
- Para cada obstacle (Vector4: XYZ + radio):
  - Calcula dashCount dinámicamente (perímetro / dash-size)
  - Anillo dasheado con scroll (OutlineScrollSpeed)
  - Color OutlineColor, thickness ObstacleThickness

**CueStyleSO Campos (Referenciados):**

| Sección | Campos |
|---------|--------|
| Minerales | MineralColor, MineralDiscRadius, MineralInnerAlpha, MineralOuterAlpha, MineralRingThickness, MineralRingAlpha |
| Salidas | ExitAlpha, ExitRingThickness |
| Pizarrón | KnownVeinRingAlpha, KnownVeinRingThickness, KnownVeinRingOffset, PingSeconds, PingRadius, PingAlpha, PingThickness |
| Lode | LodeColor, LodeRadius, LodeThickness, LodeDashCount, LodeSpinSpeed |
| Contorno (S113-S114) | OutlineColor, OutlineThickness, OutlineDashLength, OutlineDashGap, OutlineScrollSpeed, OutlineStride, **outlineInset (S114)**, **ObstacleThickness, ObstacleDashLength, ObstacleDashGap** |
| Spawns (S113) | SpawnRadius, SpawnThickness, SpawnDashCount, SpawnSpinSpeed, SpawnAlpha |
| General | GuideAlpha, HeightOffset, RingDashCount, RingDashRatio, RingSpinSpeed, FriendColor, FoeColor |

**Invariantes (S103-S113-S114):**
- Pings descartan tras PingKeepSeconds (TeamBlackboard.PrunePings)
- DrawBehind=true solo para ExitZones y Obstacles (renderizado detrás)
- Heights: todos con HeightOffset
- AlphaScale aplicado globalmente en LateUpdate (multiplica todos los alphas)
- Stride en Outline: si 0 → 1 (cada punto); si 4 → cada 4to punto
- Obstacle dashCount calculado dinámico para evitar distorsión con radios distintos
- **Outline inset (S114):** contrae hacia adentro para evitar overlap visual con borde exterior

**S103-S110-S113-S114 Cambios:**
- S103: DrawBlackboards, showBlackboards, backMaterial (S110)
- S110: CueDrawer.DrawBehind para ExitZones
- S113: DrawOutline, DrawSpawns, DrawObstacles, style centralizados
- S114: outlineInset para DrawOutline

## Vinculado a

[[Index/20 - MVP Combate]], [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox y Expedicion (S102-S103)]], S114

## Conexiones

- [[PerceivableRegistry]] — query de minerales
- [[MaterialPickup]] — mineral entities
- [[ExitZone]] — salida entities
- [[TeamBlackboard]] — vetas conocidas, pings
- [[ArenaSandbox]] — acceso central (Exits, PlacedObstacles, ActiveShape, SpawnPoint, BoardFor)
- [[CueDrawer]] — renderizado de shapes
- [[CueStyleSO]] — configuración unificada
- [[ArenaShape]] — outline polygon, center, sprite
