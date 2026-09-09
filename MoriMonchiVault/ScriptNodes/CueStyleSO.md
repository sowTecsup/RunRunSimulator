---
tags: [script, data, scriptableobject, expedition, visualization]
---

# CueStyleSO.cs

**Ruta:** `Data/Expedition/CueStyleSO.cs`

**Responsabilidad:** Gancho de tuning visual centralizado para guías de arena. Diccionario `CreatureIntent → Color`, 70+ parámetros de geometría/animación. S102: visión cono. S103: Pizarrón (vetas conocidas, pings). S104: Órdenes (cono teñido por Contact, anillo de huida). S107: Base (disco base + anillo), Habilidades (ráfagas post-disparo), **Telegrafía (plantillas de choque con parpadeo)**. **S110:** Flecha de la picada (arco parabólico con cinta para Wings). Sin lógica; solo lectura desde ArenaCueOverlay, ArenaRoomCueOverlay, CreatureCueDrawer.

**Diccionario (Odin):**
- `intentColors` (Dict<CreatureIntent, Color>) — mapea intención a color de guía
- Contiene intenciones: Collecting, Carrying, Taking, Securing, Guarding, Hunting, Taunting, Exploring, Reporting, Clashing, Dazed, Fighting, etc.

**Secciones de Tuning:**

**Intención (diccionario):**
- `ColorFor(CreatureIntent) → Color` — lookup + fallback DefaultIntentColor

**Aparición:**
- `AppearSeconds`, `AppearScale` (fade in)
- `GuideAlpha` [Range(0,1)] = 0.5 — multiplicador global de alpha para guías

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

**Telegrafía (S108):**
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

**Flecha de la picada (S110 NUEVO):**
- `DiveArcWidth` = 0.1 — ancho de la cinta
- `DiveArcTailScale` [Range(0,1)] = 0.35 — escala de cola (narrowing progresivo)
- `DiveArcHeadWidth` = 0.34 — ancho de la punta de flecha
- `DiveArcHeadLength` = 0.55 — largo de la punta (desde el end)
- `DiveArcSamples` [Range(4,64)] = 28 — muestras del arco parabólico
- `DiveArcDashLength` = 0.45 — largo de cada dash (trazo)
- `DiveArcDashGap` = 0.22 — separación entre dashes
- `DiveArcFlowSpeed` = 2.5 — Hz del flujo de offset (anima trazos)
- `DiveArcTailAlpha` [Range(0,1)] = 0.08 — transparencia de la cola
- `DiveArcStartHeight` = 0.55 — altura Y inicial (desde atacante)
- `DiveArcHeightScale` [Min(0.1f)] = 1.3 — escala de altura del ápice parabólico

**Selección:**
- `SelectColor`, `SelectRadius`, `SelectThickness`, `SelectDashCount`, `SelectDashRatio`, `SelectSpinSpeed`, `SelectAppearScale`, `SelectGlowAlpha`, `SelectPulseSpeed`, `SelectPulseAmount`

**Social:**
- `SocialLinkColor` (rosa), `FightColor` (rojo), `SocialLinkThickness`, `FightPulseSpeed`

**Métodos Públicos:**
- `Color ColorFor(CreatureIntent intent) → Color` — lookup + fallback DefaultIntentColor
- `void PopulateDefaults() [Button]` — inicializa diccionario con todos los intents

## Cambios S104

- Sección Órdenes agregada (6 campos)
- FleeColor teñe anillo de huida (diferente a FoeColor)
- ContactFillAlpha/EdgeAlpha controlan opacidad del cono de contacto

## Cambios S107

- Sección "Base y descubrimiento" agregada (6 campos) — para CreatureCueDrawer.Base()
- Sección "Habilidades" agregada (5 campos) — para CreatureCueDrawer.AbilityBursts()
- RevealSeconds permite fade suave de rivales al appear/disappear
- ConeDash* permite decorar cono de visión si necesario

## Cambios S108

- Sección "Telegrafía" agregada (12 campos) — para CreatureCueDrawer.Telegraph()
- TelegraphBlinkSpeed/BlinkSpeedEnd/BlinkMin controlan el parpadeo (Hz y amplitud)
- TelegraphPulseSpeed/Amount, TelegraphRingScale tunan el pulso y anillo de cierre
- TelegraphFadeSeconds controla la duración del fundido post-telegrafía

## Cambios S110

- Sección "Flecha de la picada" agregada (11 campos) — para CreatureCueDrawer.Telegraph() en Wings
- DiveArcWidth/TailScale/HeadWidth/HeadLength controlan geometría de la cinta
- DiveArcSamples muestras del arco parabólico
- DiveArcDashLength/Gap/FlowSpeed animan los trazos
- DiveArcTailAlpha transparencia de la cola
- DiveArcStartHeight/HeightScale tunan altura del ápice
- Parámetros consumidos por CueRibbonDrawer.Arc() en la rama Wings de Telegraph

## Invariantes

- Diccionario extensible
- Parámetros solo aplican si en expedición (ExpeditionRulesSO.Current != null)
- Colores por intención o por orden (prioridad intención)
- Todos los parámetros son públicos (directamente editables en inspector)
- GuideAlpha multiplica todos los alfas de guías (exceptuando telegrafía que mantiene alpha=1 durante impacto)

## Vinculado a

- [[Index/22 - Arena (S103-S104)]]
- [[Index/23 - Arena Sandbox y Expedicion (S102-S103)]]

## Conexiones

- [[ArenaCueOverlay]]
- [[ArenaRoomCueOverlay]]
- [[CueDrawer]]
- [[CueRibbonDrawer]] — (S110 NUEVO) parámetros DiveArc*
- [[CreatureCueDrawer]]
- [[CreatureIntent]]
- [[TeamBlackboard]]
- [[ExpeditionRulesSO]]
- [[ArenaOrders]]

