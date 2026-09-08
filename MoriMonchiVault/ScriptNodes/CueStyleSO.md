---
tags: [script, data, scriptableobject, expedition, visualization]
---

# CueStyleSO.cs

**Ruta:** `Data/Expedition/CueStyleSO.cs`

**Responsabilidad:** Gancho de tuning visual centralizado para guías de arena. Diccionario `CreatureIntent → Color`, 60+ parámetros de geometría/animación. S102: visión cono. S103: Pizarrón (vetas conocidas, pings). S104: Órdenes (cono teñido por Contact, anillo de huida). **S107: NUEVOS** secciones Base (disco base + anillo), Habilidades (ráfagas post-disparo), Reveal (fade suave de rivales). Sin lógica; solo lectura desde ArenaCueOverlay, ArenaRoomCueOverlay, CreatureCueDrawer.

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
- `ContactFillAlpha` [Range(0,1)] = 0.12 — opacidad interior del cono de contacto (semitransparente)
- `ContactEdgeAlpha` [Range(0,1)] = 0.9 — opacidad borde del cono
- `FleeRingRadius` = 1.3 — radio del anillo de huida
- `FleeRingThickness` = 0.08
- `FleePulseSpeed` = 8 — velocidad de pulsación del anillo

**Base y descubrimiento (S107 NUEVO):**
- `BaseRadius` = 0.9 — radio del disco base
- `BaseInnerAlpha` [Range(0,1)] = 0.35 — opacidad del disco
- `BaseRingThickness` = 0.05 — grosor del anillo
- `BaseRingAlpha` [Range(0,1)] = 0.6 — opacidad del anillo
- `RevealSeconds` = 0.3 — duración del fade de reveal para rivales
- `ConeDashCount`, `ConeDashRatio`, `ConeDashSpinSpeed` — punteado decorativo

**Habilidades (S107 NUEVO):**
- `AbilityBurstSeconds` = 0.6 — duración de la ráfaga post-disparo
- `AbilityBurstRadiusFrom` = 0.8 — radio inicial de expansión
- `AbilityBurstRadiusTo` = 2.2 — radio final de expansión
- `AbilityBurstThickness` = 0.1 — grosor del anillo
- `AbilityBurstAlpha` [Range(0,1)] = 0.9 — opacidad máxima, decae hasta 0

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

**Invariantes:**
- Diccionario extensible
- Parámetros solo aplican si en expedición (ExpeditionRulesSO.Current != null)
- Colores por intención o por orden (prioridad intención)
- Todos los parámetros son públicos (directamente editables en inspector)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaCueOverlay]], [[ArenaRoomCueOverlay]], [[CueDrawer]], [[CreatureCueDrawer]], [[CreatureIntent]], [[TeamBlackboard]], [[ExpeditionRulesSO]], [[ArenaOrders]]
