---
tags: [script, data, scriptableobject, expedition, visualization]
---

# CueStyleSO.cs

**Ruta:** `Data/Expedition/CueStyleSO.cs`

**Responsabilidad:** Gancho de tuning visual para guías de arena. Diccionario `CreatureIntent → Color`, 50+ parámetros de geometría/animación. S102: visión cono. **S103:** Colores Exploring/Reporting, **sección Pizarrón** (vetas conocidas, pings de reportes). Sin lógica; solo lectura desde ArenaCueOverlay, ArenaRoomCueOverlay.

**Campos Principales:**

**Diccionario (Odin):**
- `intentColors` (Dict<CreatureIntent, Color>)
- S101: Collecting, Carrying, Taking, Securing, Guarding, Hunting, Taunting
- S100: Clashing, Dazed
- **S103:** Exploring (verde azulado 0.55, 0.9, 0.6), Reporting (amarillo verde 0.75, 1, 0.45)

**Colores predefinidos:**
- `DefaultIntentColor`, `FriendColor`, `FoeColor`, `MineralColor`, `SocialLinkColor`, `FightColor`

**Secciones de Tuning:**

**Intención (diccionario):**
- ColorFor(CreatureIntent) → color

**Aparición:**
- AppearSeconds, AppearScale (fade in)

**Geometría básica:**
- HeightOffset, RingThickness, RingAlpha, PathThickness, HeadLength, HeadWidth, PerceptThickness

**Percepción:**
- FriendColor (verde), FoeColor (rojo), PerceptAlpha, PerceptFarAlpha
- AttentionArcDegrees, AttentionAlpha
- PulseSeconds, PulseAmount

**Anillo de percepción:**
- RingDashCount, RingDashRatio, RingSpinSpeed

**Cono de visión (S102):**
- VisionFillInnerAlpha, VisionFillOuterAlpha, VisionEdgeAlpha, VisionSideAlpha
- NearRingAlpha, VisionTurnSmoothing

**Retícula:**
- ReticleRadius, ReticleThickness, ReticleSpinSpeed, ReticleSweepDegrees, ReticleAppearScale

**Ruta:**
- PathFadeSeconds, PathSmoothing, CurveSamples, StartTangent, PathFlowSpeed, PathDashLength, PathDashGap
- PathTailAlpha, DestMarkerRadius, DestPulseSpeed, DestPulseAmount

**Salidas y minado:**
- ExitAlpha, ExitRingThickness
- MiningArcRadius, MiningArcThickness, MiningArcAlpha

**Minerales:**
- MineralColor (cyan), MineralDiscRadius, MineralInnerAlpha, MineralOuterAlpha, MineralRingThickness, MineralRingAlpha

**Pizarrón S103 NUEVO:**
- `KnownVeinRingAlpha` [Range(0,1)] = 0.45 — visibilidad anillos de vetas conocidas
- `KnownVeinRingThickness` = 0.05 — grosor del anillo
- `KnownVeinRingOffset` = 0.35 — distancia extra desde mineral
- `PingSeconds` = 1.4 — duración visible del ping (expansión + fade)
- `PingRadius` = 2.6 — radio máximo del ping
- `PingAlpha` [Range(0,1)] = 0.8 — opacidad inicial ping
- `PingThickness` = 0.08 — grosor del ring ping

**Social:**
- SocialLinkColor (rosa), FightColor (rojo), SocialLinkThickness, FightPulseSpeed

**Métodos Públicos:**
- `ColorFor(CreatureIntent intent) → Color` — lookup + fallback
- `PopulateDefaults() [Button]` — inicializa diccionario con S103 intents (agrega Exploring, Reporting)

**S103 Cambios:**
- Colores nuevos en diccionario: Exploring, Reporting
- Sección Pizarrón agregada (7 campos)
- PopulateDefaults() actualizado para Exploring (0.55, 0.9, 0.6), Reporting (0.75, 1, 0.45)
- ArenaRoomCueOverlay.DrawBlackboards() consume estos valores

**Invariantes:**
- Diccionario extensible
- Parámetros solo aplican si en expedición (Current != null)
- Spin opuesto por team en anillos (PlayerCW, RivalCCW)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaCueOverlay]], [[ArenaRoomCueOverlay]], [[CueDrawer]], [[DrawVisionCone]], [[CreatureIntent]], [[TeamBlackboard]], [[ExpeditionRulesSO]]
