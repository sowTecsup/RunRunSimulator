---
tags: [script, world, expedition, visualization, cues]
---

# ArenaRoomCueOverlay.cs

**Ruta:** `World/Expedition/ArenaRoomCueOverlay.cs`

**Responsabilidad:** Dibuja guías visuales en overlay de sala: minerales (discos animados), salidas (anillos giratorio), **pizarrones de equipo** (S103 NUEVO: anillos de vetas conocidas, pings de reportes). `DrawMinerals()` anima opacity de minerales según Taken. `DrawExits()` anillos de salidas con dasheado. **S103 NUEVO:** `DrawBlackboards()` itera pizarrones de cada team, dibuja anillos para vetas conocidas (con offset diferente por team), dibuja pings expansivos.

**Campos Serializados:**
- `sandbox` [Required] — ArenaSandbox
- `cueMaterial`, `additiveMaterial` [Required] — CueDrawer
- `style` [Required] — CueStyleSO
- `showMinerals`, `showExits`, `showBlackboards` (bool, default true) — (S103 NUEVO: showBlackboards)

**Caches Privados:**
- `mineralAnims` (Dictionary<MaterialPickup, MineralAnim>)
- `mineralQueryBuffer`, `mineralLookup` (query reutilizable, no-alloc)

**LateUpdate():**
- Si showMinerals: DrawMinerals()
- Si showExits: DrawExits()
- Si showBlackboards: DrawBlackboards() — (S103 NUEVO)

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

**Otros métodos (sin cambios S103):**
- `DrawMinerals()` — discos animados (alpha MoveTowards, dasheado si multi-mineral)
- `DrawExits()` — anillos de salidas (friend/foe color)
- `GetMineralAnim()`, `GetMineralPickup()` — caché

**S103 Cambios:**
- Campo `showBlackboards` toggle
- Método `DrawBlackboards()` nuevo
- LateUpdate() llama DrawBlackboards() si habilitado
- Integración con `TeamBlackboard.KnownVeins`, `TeamBlackboard.Pings`

**CueStyleSO Campos S103 (usados por DrawBlackboards):**
- `KnownVeinRingAlpha`, `KnownVeinRingThickness`, `KnownVeinRingOffset` — anillos de vetas
- `PingSeconds`, `PingRadius`, `PingAlpha`, `PingThickness` — pings

**Invariantes:**
- Pings se descartan tras `PingKeepSeconds` (TeamBlackboard.PrunePings)
- Spin opuesto por team (Player CW, Rival CCW) crea simetría visual
- Offset de Rival > Player para evitar solapamiento si ambos conocen veta
- Heights: todos con HeightOffset

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[PerceivableRegistry]], [[MaterialPickup]], [[ExitZone]], [[TeamBlackboard]], [[ArenaSandbox]], [[CueDrawer]], [[CueStyleSO]], [[ExpeditionTeam]]
