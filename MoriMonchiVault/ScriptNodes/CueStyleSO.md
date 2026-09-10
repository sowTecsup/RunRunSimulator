---
tags: [script, data, scriptableobject, expedition, visualization]
---

# CueStyleSO.cs

**Ruta:** `Data/Expedition/CueStyleSO.cs`

**Responsabilidad:** Gancho de tuning visual centralizado para guías de arena. Diccionario `CreatureIntent → Color`, 100+ parámetros de geometría/animación. Cubre: percepción, orden, base, habilidades, telegrafía, **S113: contorno/obstáculos/lode/spawns**. Sin lógica; solo lectura desde ArenaCueOverlay, ArenaRoomCueOverlay, CreatureCueDrawer.

**Diccionario (Odin):**
- `intentColors` (Dict<CreatureIntent, Color>) — mapea intención a color de guía
- `DefaultIntentColor` (Color) — fallback

**Secciones de Tuning:**

| Sección | Campos | Propósito |
|---------|--------|----------|
| **Intención** | `ColorFor(intent)` | Lookup + fallback |
| **Aparición** | `AppearSeconds`, `AppearScale`, `GuideAlpha` [0-1] | Fade in, multiplicador global |
| **Geometría** | `HeightOffset`, `RingThickness`, `RingAlpha`, `PathThickness`, `HeadLength`, `HeadWidth`, `PerceptThickness` | Bases de render |
| **Percepción** | `FriendColor`, `FoeColor`, `PerceptAlpha`, `AttentionArcDegrees`, `AttentionAlpha`, `PulseSeconds`, `PulseAmount` | Cono de visión |
| **Anillo percepción** | `RingDashCount`, `RingDashRatio`, `RingSpinSpeed` | Punteado giratorio |
| **Cono visión** | `VisionFillInnerAlpha`, `VisionFillOuterAlpha`, `VisionEdgeAlpha`, `VisionSideAlpha`, `NearRingAlpha`, `VisionTurnSmoothing` | Interior + borde + sides |
| **Retícula** | `ReticleRadius`, `ReticleThickness`, `ReticleSweepDegrees`, `ReticleSpinSpeed`, `ReticleAppearScale` | Target lock visuals |
| **Ruta** | `PathFadeSeconds`, `PathSmoothing`, `CurveSamples`, `StartTangent`, `PathFlowSpeed`, `PathDashLength`, `PathDashGap`, `PathTailAlpha`, `DestMarkerRadius`, `DestPulseSpeed`, `DestPulseAmount` | Ruta de movimiento |
| **Salida/minado** | `ExitAlpha`, `ExitRingThickness`, `MiningArcRadius`, `MiningArcThickness`, `MiningArcAlpha` | Zonas de acción |
| **Minerales** | `MineralColor`, `MineralDiscRadius`, `MineralInnerAlpha`, `MineralOuterAlpha`, `MineralRingThickness`, `MineralRingAlpha` | Depósitos de minería |
| **Pizarrón (S103)** | `KnownVeinRingAlpha`, `KnownVeinRingThickness`, `KnownVeinRingOffset`, `PingSeconds`, `PingRadius`, `PingAlpha`, `PingThickness` | Vetas conocidas, pings |
| **Órdenes (S104)** | `FleeColor`, `ContactFillAlpha`, `ContactEdgeAlpha`, `FleeRingRadius`, `FleeRingThickness`, `FleePulseSpeed` | Conos de orden |
| **Base (S107)** | `BaseRadius`, `BaseInnerAlpha`, `BaseRingThickness`, `BaseRingAlpha`, `RevealSeconds`, `ConeDashCount`, `ConeDashRatio`, `ConeDashSpinSpeed` | Disco base + anillo |
| **Habilidades (S107)** | `AbilityBurstSeconds`, `AbilityBurstRadiusFrom`, `AbilityBurstRadiusTo`, `AbilityBurstThickness`, `AbilityBurstAlpha` [0-1] | Ráfaga post-disparo |
| **Telegrafía (S108)** | `TelegraphEdgeAlpha`, `TelegraphEdgeThickness`, `TelegraphTrackAlpha`, `TelegraphFillAlpha`, `TelegraphFillOuterAlpha`, `TelegraphRingScale`, `TelegraphPulseSpeed`, `TelegraphPulseAmount`, `TelegraphFadeSeconds`, `TelegraphBlinkSpeed`, `TelegraphBlinkSpeedEnd`, `TelegraphBlinkMin` | Plantilla choque con parpadeo |
| **Flecha picada (S110)** | `DiveArcWidth`, `DiveArcTailScale`, `DiveArcHeadWidth`, `DiveArcHeadLength`, `DiveArcSamples`, `DiveArcDashLength`, `DiveArcDashGap`, `DiveArcFlowSpeed`, `DiveArcTailAlpha`, `DiveArcStartHeight`, `DiveArcHeightScale` | Arco parabólico Wings |
| **Selección** | `SelectColor`, `SelectRadius`, `SelectThickness`, `SelectDashCount`, `SelectDashRatio`, `SelectSpinSpeed`, `SelectAppearScale`, `SelectGlowAlpha`, `SelectPulseSpeed`, `SelectPulseAmount` | Anillo selección |
| **Social** | `SocialLinkColor`, `FightColor`, `SocialLinkThickness`, `FightPulseSpeed` | Vínculos |
| **Contorno y puntos (S113)** | `OutlineColor`, `OutlineThickness`, `OutlineDashLength`, `OutlineDashGap`, `OutlineScrollSpeed`, `OutlineStride`, `ObstacleThickness`, `ObstacleDashLength`, `ObstacleDashGap`, `LodeColor`, `LodeRadius`, `LodeThickness`, `LodeDashCount`, `LodeSpinSpeed`, `SpawnRadius`, `SpawnThickness`, `SpawnDashCount`, `SpawnSpinSpeed`, `SpawnAlpha` [0-1] | Forma, hitos, spawns |

**Métodos Públicos:**
- `Color ColorFor(CreatureIntent intent) → Color` — lookup + fallback DefaultIntentColor
- `void PopulateDefaults() [Button]` — inicializa diccionario con todos los intents (Odin button)

**Invariantes:**
- Diccionario extensible (Odin permite agregar intents en runtime)
- GuideAlpha multiplica globalmente (excepto telegrafía en impacto)
- Todos los parámetros públicos (edición directa en Inspector)
- **S113:** Outline parámetros reutilizados para Obstacles (mismo color/scroll, pero distinto dashLength/gap)
- **S113:** Lode y Spawn separados para custom styling

**S113 Cambios:**

| Título | Campos | Descripción |
|--------|--------|-------------|
| **Contorno** | `OutlineColor`, `OutlineThickness`, `OutlineDashLength`, `OutlineDashGap`, `OutlineScrollSpeed`, `OutlineStride` | Punteado del contorno de sala (ArenaRoomCueOverlay.DrawOutline) |
| **Obstáculos** | `ObstacleThickness`, `ObstacleDashLength`, `ObstacleDashGap` | Anillos de hitos grandes (landmarks, ArenaRoomCueOverlay.DrawObstacles) |
| **Lode** | `LodeColor`, `LodeRadius`, `LodeThickness`, `LodeDashCount`, `LodeSpinSpeed` | Mineral central (ArenaRoomCueOverlay.DrawLode) |
| **Spawns** | `SpawnRadius`, `SpawnThickness`, `SpawnDashCount`, `SpawnSpinSpeed`, `SpawnAlpha` [0-1] | Puntos de entrada (ArenaRoomCueOverlay.DrawSpawns) |

**S102-S108-S110-S113 Historial:**
- S102: Cono visión
- S103: Pizarrón (vetas, pings)
- S104: Órdenes
- S107: Base, Habilidades
- S108: Telegrafía (12 campos)
- S110: Flecha picada (11 campos)
- S113: Contorno/Obstáculos/Lode/Spawns (19 campos)

## Vinculado a

[[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox y Expedicion (S102-S103)]]

## Conexiones

- [[ArenaCueOverlay]]
- [[ArenaRoomCueOverlay]] — lee todos los campos S113
- [[CueDrawer]]
- [[CueRibbonDrawer]] — S110 parámetros DiveArc*
- [[CreatureCueDrawer]]
- [[CreatureIntent]]
- [[TeamBlackboard]]
- [[ExpeditionRulesSO]]
- [[ArenaOrders]]
