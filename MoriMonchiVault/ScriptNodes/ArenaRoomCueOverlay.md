---
tags: [script, world, expedition, visualization, cues]
---

# ArenaRoomCueOverlay.cs

**Ruta:** `World/Expedition/ArenaRoomCueOverlay.cs`

**Responsabilidad:** Dibuja guías visuales en overlay de sala: minerales (discos animados), salidas (anillos giratorio), **pizarrones de equipo** (S103 NUEVO: anillos de vetas conocidas, pings de reportes), **salidas detrás de guías** (S110 NUEVO: renderizado con `CueDrawer.DrawBehind`). `DrawMinerals()` anima opacity de minerales según Taken. `DrawExits()` anillos de salidas con dasheado, renderizados detrás de todas las guías. **S103 NUEVO:** `DrawBlackboards()` itera pizarrones de cada team, dibuja anillos para vetas conocidas (con offset diferente por team), dibuja pings expansivos. **S110 NUEVO:** Flag `CueDrawer.DrawBehind` controla orden de renderizado.

**Campos Serializados:**
- `sandbox` [Required] — ArenaSandbox
- `cueMaterial`, `additiveMaterial` [Required] — CueDrawer
- `backMaterial` [Required] — **S110 NUEVO** material para renderizado detrás (cola de render 2990)
- `style` [Required] — CueStyleSO
- `showMinerals`, `showExits`, `showBlackboards` (bool, default true) — (S103 NUEVO: showBlackboards)

**Caches Privados:**
- `mineralAnims` (Dictionary<MaterialPickup, MineralAnim>)
- `mineralQueryBuffer`, `mineralLookup` (query reutilizable, no-alloc)

**LateUpdate():**
- Si showMinerals: DrawMinerals()
- Si showExits: 
  - **S110 NUEVO:** Setea `CueDrawer.DrawBehind = true` (usa backMaterial)
  - DrawExits()
  - Setea `CueDrawer.DrawBehind = false` (vuelve a material normal)
- Si showBlackboards: DrawBlackboards() — (S103 NUEVO)

**DrawMinerals():**
- Query no-alloc de perceivables (MaterialPickup)
- Anima alpha de cada mineral (MoveTowards, Taken → 0, Available → 1)
- Dibuja disco con degradado (MineralColor, inner/outer alpha)
- Si Value > 1 (múltiples muestras), agrega anillo dasheado giratorio

**DrawExits():**
- Itera sandbox.Exits
- Color según team (FriendColor Player, FoeColor Rival)
- Dibuja disco (ExitAlpha) + anillo
- Anillo dasheado con spin (opcional)
- **S110 MEJORADO:** Renderizado con `CueDrawer.DrawBehind = true` para que no solape guías

**DrawBlackboards() S103 NUEVO:**
- Itera teams [Player, Rival]
- Obtiene board = `sandbox.BoardFor(team)`
- Color según team (FriendColor Player, FoeColor Rival)
- **Vetas conocidas (KnownVeins):**
  - Para cada k en board.KnownVeins (no tomada, activa)
  - Dibuja anillo dasheado a distancia (radius + KnownVeinRingOffset + offset extra si Rival)
  - Rotación según team: Player +spin, Rival -spin (direcciones opuestas)
  - Alpha = KnownVeinRingAlpha
- **Pings (reportes frescos):**
  - Prune pings viejos con PrunePings(Time.time)
  - Para cada ping en board.Pings:
    - t = (now - ping.Time) / PingSeconds (0 a 1)
    - radius = Lerp(0.4, PingRadius, t) — crece
    - alpha = PingAlpha * (1 - t) — desvanece
    - Dibuja ring expansivo

**S103 Cambios:**
- Campo `showBlackboards` toggle
- Método `DrawBlackboards()` nuevo
- LateUpdate() llama DrawBlackboards() si habilitado
- Integración con `TeamBlackboard.KnownVeins`, `TeamBlackboard.Pings`

**S110 Cambios:**
- Campo `backMaterial` [Required] para renderizado detrás
- `OnEnable()` llama `CueDrawer.Configure(cueMaterial, additiveMaterial, backMaterial)` (sobrecarga nueva)
- `LateUpdate()` en rama DrawExits:
  - Antes de DrawExits(): `CueDrawer.DrawBehind = true`
  - DrawExits()
  - Después: `CueDrawer.DrawBehind = false`
- Invariante: solo ExitZones usan DrawBehind (orden: salidas → resto de guías)

**CueStyleSO Campos S103 (usados por DrawBlackboards):**
- `KnownVeinRingAlpha`, `KnownVeinRingThickness`, `KnownVeinRingOffset` — anillos de vetas
- `PingSeconds`, `PingRadius`, `PingAlpha`, `PingThickness` — pings

**Invariantes:**
- Pings se descartan tras `PingKeepSeconds` (TeamBlackboard.PrunePings)
- Spin opuesto por team (Player CW, Rival CCW) crea simetría visual
- Offset de Rival > Player para evitar solapamiento si ambos conocen veta
- Heights: todos con HeightOffset
- **S110:** backMaterial cola renderizado 2990 (siempre detrás, pre-compute no se muta)
- **S110:** DrawBehind flag es temporal (se resetea cada LateUpdate), no stale

**Métodos Privados (sin cambios S103-S110):**
- `DrawMinerals()` — discos animados (alpha MoveTowards, dasheado si multi-mineral)
- `DrawBlackboards()` (S103 NUEVO) — iterador de pizarrones y pings
- `GetMineralAnim()`, `GetMineralPickup()` — caché

## Vinculado a

- [[Index/22 - Arena (S103-S104)]]
- [[Index/23 - Arena Sandbox y Expedicion (S102-S103)]]

## Conexiones

- [[PerceivableRegistry]] — query de minerales
- [[MaterialPickup]] — mineral entities
- [[ExitZone]] — salida entities
- [[TeamBlackboard]] — estado de vetas conocidas y pings
- [[ArenaSandbox]] — acceso a sandbox
- [[CueDrawer]] — renderizado de shapes (con DrawBehind S110)
- [[CueStyleSO]] — parámetros de estilo
- [[ExpeditionTeam]] — team colors

