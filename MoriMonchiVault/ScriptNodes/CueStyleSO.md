---
tags: [script, data, scriptableobject, expedition, visualization]
---

# CueStyleSO.cs

**Ruta:** `Data/Expedition/CueStyleSO.cs`

**Responsabilidad:** Gancho de tuning visual para guías de arena. Diccionario `CreatureIntent → Color`, 60+ parámetros de geometría/animación. S102: visión cono. S103: Pizarrón (vetas conocidas, pings). **S104: Órdenes (NUEVO)** — cono teñido por Contact, anillo de huida. Sin lógica; solo lectura desde ArenaCueOverlay, ArenaRoomCueOverlay, ArenaCueOverlay (S104).

**Diccionario (Odin):**
- `intentColors` (Dict<CreatureIntent, Color>)
- S101+: Collecting, Carrying, Taking, Securing, Guarding, Hunting, Taunting, etc.
- S103: Exploring, Reporting
- S100: Clashing, Dazed

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

**Pizarrón (S103):**
- KnownVeinRingAlpha [Range(0,1)] = 0.45
- KnownVeinRingThickness = 0.05
- KnownVeinRingOffset = 0.35
- PingSeconds = 1.4
- PingRadius = 2.6
- PingAlpha [Range(0,1)] = 0.8
- PingThickness = 0.08

**Órdenes (S104 NUEVO):**
- `FleeColor` — color del anillo de huida (amarillo 1, 0.85, 0.2)
- `ContactFillAlpha` [Range(0,1)] = 0.12 — opacidad interior del cono de contacto (semitransparente)
- `ContactEdgeAlpha` [Range(0,1)] = 0.9 — opacidad borde del cono
- `FleeRingRadius` = 1.3 — radio del anillo de huida
- `FleeRingThickness` = 0.08
- `FleePulseSpeed` = 8 — velocidad de pulsación del anillo

**Social:**
- SocialLinkColor (rosa), FightColor (rojo), SocialLinkThickness, FightPulseSpeed

**Métodos Públicos:**
- `ColorFor(CreatureIntent intent) → Color` — lookup + fallback DefaultIntentColor
- `PopulateDefaults() [Button]` — inicializa diccionario con todos los intents

**S103 Cambios:**
- Sección Pizarrón agregada (7 campos)
- Colores nuevos en diccionario: Exploring, Reporting

**S104 Cambios:**
- Sección Órdenes agregada (6 campos)
- FleeColor teñe anillo de huida (diferente a FoeColor)
- ContactFillAlpha/EdgeAlpha controlan opacidad del cono de contacto por Orders.Contact
- ArenaCueOverlay.DrawOrdersCue() consume estos valores
- FleeRingRadius + FleePulseSpeed rinden anillo pulsante cuando Flee es detectada

**Invariantes:**
- Diccionario extensible
- Parámetros solo aplican si en expedición (ExpeditionRulesSO.Current != null)
- Colores por intención o por orden (prioridad intención)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaCueOverlay]], [[ArenaRoomCueOverlay]], [[CueDrawer]], [[DrawVisionCone]], [[CreatureIntent]], [[TeamBlackboard]], [[ExpeditionRulesSO]], [[ArenaOrders]]
