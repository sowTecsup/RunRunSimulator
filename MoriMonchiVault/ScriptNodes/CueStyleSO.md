---
tags: [script, data, scriptableobject, expedition, visualization]
---

# CueStyleSO.cs

**Ruta:** `Data/Expedition/CueStyleSO.cs`

**Responsabilidad:** Gancho de tuning visual centralizado para guías de arena. Diccionario `CreatureIntent → Color`, 70+ parámetros de geometría/animación. S102: visión cono. S103: Pizarrón (vetas conocidas, pings). S104: Órdenes (cono teñido por Contact, anillo de huida). S107: Base (disco base + anillo), Habilidades (ráfagas post-disparo), **Telegrafía (plantillas de choque con parpadeo)**. Sin lógica; solo lectura desde ArenaCueOverlay, ArenaRoomCueOverlay, CreatureCueDrawer.

**Diccionario (Odin):**
- `intentColors` (Dict<CreatureIntent, Color>) — mapea intención a color de guía
- Contiene intenciones: Collecting, Carrying, Taking, Securing, Guarding, Hunting, Taunting, Exploring, Reporting, Clashing, Dazed, Fighting, etc.

**Secciones de Tuning:**

**Intención (diccionario):**
- `ColorFor(CreatureIntent) → Color` — lookup + fallback DefaultIntentColor

**Aparición:**
- `AppearSeconds`, `AppearScale` (fade in)

**Geometría básica:**
- `HeightOffset`, `RingThickness`, `RingAlpha`, `PathThickness`, `HeadLength`, `HeadWidth`, `PerceptThickness`

**Percepción:**
- `FriendColor` (verde), `FoeColor` (rojo), `PerceptAlpha`, `PerceptFarAlpha`
- `AttentionArcDegrees`, `AttentionAlpha`
- `PulseSeconds`, `PulseAmount`

**Anillo de percepción:**
- `RingDashCount`, `RingDashRatio`, `RingSpinSpeed`

**Cono de visión (S102):**
- `VisionFillInnerAlpha`, `VisionFillOuterAlpha`, `VisionEdgeAlpha`, `VisionSideAlpha`
- `NearRingAlpha`, `VisionTurnSmoothing`

**Retícula:**
- `ReticleRadius`, `ReticleThickness`, `ReticleSweepDegrees`, `ReticleSpinSpeed`, `ReticleAppearScale`

**Ruta:**
- `PathFadeSeconds`, `PathSmoothing`, `CurveSamples`, `StartTangent`, `PathFlowSpeed`, `PathDashLength`, `PathDashGap`
- `PathTailAlpha`, `DestMarkerRadius`, `DestPulseSpeed`, `DestPulseAmount`

**Salidas y minado:**
- `ExitAlpha`, `ExitRingThickness`
- `MiningArcRadius`, `MiningArcThickness`, `MiningArcAlpha`

**Minerales:**
- `MineralColor` (cyan), `MineralDiscRadius`, `MineralInnerAlpha`, `MineralOuterAlpha`, `MineralRingThickness`, `MineralRingAlpha`

**Pizarrón (S103):**
- `KnownVeinRingAlpha`, `KnownVeinRingThickness`, `KnownVeinRingOffset`
- `PingSeconds`, `PingRadius`, `PingAlpha`, `PingThickness`

**Órdenes (S104):**
- `FleeColor` — color del anillo de huida (amarillo 1, 0.85, 0.2)
- `ContactFillAlpha` [Range(0,1)] = 0.12 — opacidad interior del cono de contacto
- `ContactEdgeAlpha` [Range(0,1)] = 0.9 — opacidad borde del cono
- `FleeRingRadius` = 1.3 — radio del anillo de huida
- `FleeRingThickness` = 0.08
- `FleePulseSpeed` = 8 — velocidad de pulsación del anillo

**Base y descubrimiento (S107):**
- `BaseRadius` = 0.9 — radio del disco base
- `BaseInnerAlpha` [Range(0,1)] = 0.35 — opacidad del disco
- `BaseRingThickness` = 0.05 — grosor del anillo
- `BaseRingAlpha` [Range(0,1)] = 0.6 — opacidad del anillo
- `RevealSeconds` = 0.3 — duración del fade de reveal para rivales
- `ConeDashCount`, `ConeDashRatio`, `ConeDashSpinSpeed` — punteado decorativo

**Habilidades (S107):**
- `AbilityBurstSeconds` = 0.6 — duración de la ráfaga post-disparo
- `AbilityBurstRadiusFrom` = 0.8 — radio inicial de expansión
- `AbilityBurstRadiusTo` = 2.2 — radio final de expansión
- `AbilityBurstThickness` = 0.1 — grosor del anillo
- `AbilityBurstAlpha` [Range(0,1)] = 0.9 — opacidad máxima, decae hasta 0

**Telegrafía (S108 NUEVO):**
- `TelegraphEdgeAlpha` [Range(0,1)] = 0.85 — opacidad del contorno del área de impacto
- `TelegraphEdgeThickness` = 0.08 — grosor del contorno
- `TelegraphTrackAlpha` [Range(0,1)] = 0.1 — opacidad de la pista (plantilla de referencia)
- `TelegraphFillAlpha` [Range(0,1)] = 0.3 — opacidad del relleno central
- `TelegraphFillOuterAlpha` [Range(0,1)] = 0.08 — opacidad gradiente exterior del relleno
- `TelegraphRingScale` [Min(1f)] = 2 — escala del anillo inicial en Wings (se contrae hasta 1)
- `TelegraphPulseSpeed` = 6 — velocidad del pulso de escala durante el impacto
- `TelegraphPulseAmount` [Range(0,1)] = 0.06 — amplitud del pulso
- `TelegraphFadeSeconds` = 0.15 — duración del fundido al terminar telegrafía
- `TelegraphBlinkSpeed` = 2.5 — Hz de parpadeo al empezar anticipación
- `TelegraphBlinkSpeedEnd` = 8 — Hz de parpadeo en el frame del impacto
- `TelegraphBlinkMin` [Range(0,1)] = 0.3 — alfa mínimo del parpadeo (0.3 a 1.0)

**Selección:**
- `SelectColor`, `SelectRadius`, `SelectThickness`, `SelectDashCount`, `SelectDashRatio`, `SelectSpinSpeed`, `SelectAppearScale`, `SelectGlowAlpha`, `SelectPulseSpeed`, `SelectPulseAmount`

**Social:**
- `SocialLinkColor` (rosa), `FightColor` (rojo), `SocialLinkThickness`, `FightPulseSpeed`

**Métodos Públicos:**
- `Color ColorFor(CreatureIntent intent) → Color` — lookup + fallback DefaultIntentColor
- `void PopulateDefaults() [Button]` — inicializa diccionario con todos los intents

**S104 Cambios:**
- Sección Órdenes agregada (6 campos)
- FleeColor teñe anillo de huida (diferente a FoeColor)
- ContactFillAlpha/EdgeAlpha controlan opacidad del cono de contacto

**S107 Cambios:**
- Sección "Base y descubrimiento" agregada (6 campos) — para CreatureCueDrawer.Base()
- Sección "Habilidades" agregada (5 campos) — para CreatureCueDrawer.AbilityBursts()
- RevealSeconds permite fade suave de rivales al appear/disappear
- ConeDash* permite decorar cono de visión si necesario

**S108 Cambios:**
- Sección "Telegrafía" agregada (12 campos) — para CreatureCueDrawer.Telegraph()
- TelegraphBlinkSpeed/BlinkSpeedEnd/BlinkMin controlan el parpadeo (Hz y amplitud)
- TelegraphPulseSpeed/Amount, TelegraphRingScale tunan el pulso y anillo de cierre
- TelegraphFadeSeconds controla la duración del fundido post-telegrafía

**Invariantes:**
- Diccionario extensible
- Parámetros solo aplican si en expedición (ExpeditionRulesSO.Current != null)
- Colores por intención o por orden (prioridad intención)
- Todos los parámetros son públicos (directamente editables en inspector)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaCueOverlay]], [[ArenaRoomCueOverlay]], [[CueDrawer]], [[CreatureCueDrawer]], [[CreatureIntent]], [[TeamBlackboard]], [[ExpeditionRulesSO]], [[ArenaOrders]]
