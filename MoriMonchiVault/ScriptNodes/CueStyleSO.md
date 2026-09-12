---
tags: [script, data, scriptableobject, expedition, visualization]
---

# CueStyleSO.cs

**Ruta:** `Data/Expedition/CueStyleSO.cs`

**Responsabilidad:** Gancho de tuning visual centralizado para guías de arena. Diccionario `CreatureIntent → Color`, 100+ parámetros de geometría/animación. Cubre: percepción, orden, base, habilidades, telegrafía, contorno/obstáculos/lode/spawns. Sin lógica; solo lectura desde ArenaCueOverlay, ArenaRoomCueOverlay, CreatureCueDrawer. **S116:** sección Telegrafía podada a 8 campos; "Flecha de la picada" (DiveArc*) eliminada.

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
| **Telegrafía (S116)** | `EdgeAlpha`, `EdgeThickness`, `TrackAlpha`, `FillAlpha`, `FadeSeconds`, `FlashSeconds`, `ImpactRingSeconds`, `ImpactRingScale` | Plantilla unitaria = hitbox |
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

## S116 Cambios

**Sección Telegrafía podada a 8 campos:**

| Campo | S114 | S116 | Descripción |
|-------|------|------|-------------|
| `TelegraphEdgeAlpha` | ✓ | ✓ | Opacidad de bordes (ring/capsule outline) |
| `TelegraphEdgeThickness` | ✓ | ✓ | Grosor de contorno |
| `TelegraphTrackAlpha` | ✓ | ✓ | Opacidad de pista/track (área barrida sin progreso) |
| `TelegraphFillAlpha` | ✓ | ✓ | Opacidad de relleno (progresa desde 0 a radio con ClashTell01) |
| `TelegraphFadeSeconds` | ✓ | ✓ | Duración de fade in/out |
| `TelegraphFlashSeconds` | ✓ | ✓ | Duración del flash en impacto |
| `TelegraphImpactRingSeconds` | ✓ | ✓ | Duración del anillo de impacto tras golpe |
| `TelegraphImpactRingScale` | ✓ | ✓ | Escala máxima del anillo de impacto |
| ~~`TelegraphFillOuterAlpha`~~ | ✓ | ✗ | ELIMINADO en S116 |
| ~~`TelegraphRingScale`~~ | ✓ | ✗ | ELIMINADO en S116 |
| ~~`TelegraphPulseSpeed`~~ | ✓ | ✗ | ELIMINADO en S116 |
| ~~`TelegraphPulseAmount`~~ | ✓ | ✗ | ELIMINADO en S116 |
| ~~`TelegraphBlinkSpeed`~~ | ✓ | ✗ | ELIMINADO en S116 |
| ~~`TelegraphBlinkSpeedEnd`~~ | ✓ | ✗ | ELIMINADO en S116 |
| ~~`TelegraphBlinkMin`~~ | ✓ | ✗ | ELIMINADO en S116 |

**Sección "Flecha de la picada" eliminada por completo:**
- ~~`DiveArcWidth`~~ (ELIMINADO)
- ~~`DiveArcTailScale`~~ (ELIMINADO)
- ~~`DiveArcHeadWidth`~~ (ELIMINADO)
- ~~`DiveArcHeadLength`~~ (ELIMINADO)
- ~~`DiveArcSamples`~~ (ELIMINADO)
- ~~`DiveArcDashLength`~~ (ELIMINADO)
- ~~`DiveArcDashGap`~~ (ELIMINADO)
- ~~`DiveArcFlowSpeed`~~ (ELIMINADO)
- ~~`DiveArcTailAlpha`~~ (ELIMINADO)
- ~~`DiveArcStartHeight`~~ (ELIMINADO)
- ~~`DiveArcHeightScale`~~ (ELIMINADO)

**Contexto S116:**
- Plantilla única = hitbox: un disco por golpe, sin decoración parabólica
- Un solo color de movimiento: color de equipo (no color de habilidad diferenciado)
- Flash lineal en impacto sin parpadeo previo (sin Blink)
- Anillo de impacto tras golpe (ImpactRing solo post-impacto, no previo)

## Vinculado a

[[Index/20 - MVP Combate]], [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox y Expedicion (S102-S103)]], S116

## Conexiones

- [[ArenaCueOverlay]]
- [[ArenaRoomCueOverlay]]
- [[CueDrawer]]
- [[CreatureCueDrawer]]
- [[CreatureIntent]]
- [[TeamBlackboard]]
- [[ExpeditionRulesSO]]
- [[ArenaOrders]]
