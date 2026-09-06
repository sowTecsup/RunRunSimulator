---
tags: [script, data, scriptableobject, expedition]
---

# CueStyleSO.cs

**Ruta:** `Data/Expedition/CueStyleSO.cs`

**Responsabilidad:** Gancho de datos: contiene todos los knobs de presentación de guías visuales. Diccionario Odin `CreatureIntent → Color` para colorear rutas/anillos. 50+ parámetros de geometría, animación y velocidad. **S102 NUEVO:** sección Cono de visión (VisionFillInnerAlpha, VisionFillOuterAlpha, VisionEdgeAlpha, VisionSideAlpha, NearRingAlpha, VisionTurnSmoothing). Cero lógica; solo lectura desde ArenaCueOverlay. Botón `PopulateDefaults()` para precargar.

## Campos Públicos

**Diccionario (Odin):**
- `intentColors` (Dict<CreatureIntent, Color>) — mapping intención → color. S101: Carrying, Securing, Guarding, Hunting, Taunting. S100: Clashing, Dazed.

**Colores predefinidos:**
- `DefaultIntentColor`, `FriendColor` (verde), `FoeColor` (rojo), `MineralColor` (cyan), `SocialLinkColor` (rosa), `FightColor` (rojo oscuro)

**Cono de Visión S102 NUEVO:**
- `VisionFillInnerAlpha` (float, Range 0–1, default 0.25) — alfa en el centro del sector (cono relleno interior)
- `VisionFillOuterAlpha` (float, Range 0–1, default 0.08) — alfa en el perímetro del sector (cono relleno exterior)
- `VisionEdgeAlpha` (float, Range 0–1, default 0.8) — alfa del arco del borde del cono
- `VisionSideAlpha` (float, Range 0–1, default 0.35) — alfa de los segmentos laterales (si sweep < 360°)
- `NearRingAlpha` (float, Range 0–1, default 0.5) — alfa del anillo de audición (dashed)
- `VisionTurnSmoothing` (float, Min 0, default 5) — exponente de Lerp para suavizado de giro (rumbo interpolado)

**Parámetros de Ruta (sin cambios conceptuales S102):**
- `PathSmoothing`, `PathFadeSeconds`, `CurveSamples`, `StartTangent`, `PathThickness`, `PathDashLength`, `PathDashGap`, `PathFlowSpeed`, `PathTailAlpha`, `HeadLength`, `HeadWidth`

**Parámetros de Anillo (sin cambios S102):**
- `RingThickness`, `RingAlpha`, `RingSpinSpeed`, `RingDashCount`, `RingDashRatio`, `AppearSeconds`, `AppearScale`, `PulseSeconds`, `PulseAmount`, `AttentionArcDegrees`, `AttentionAlpha`

**Parámetros de Retícula (sin cambios S102):**
- `ReticleRadius`, `ReticleThickness`, `ReticleSpinSpeed`, `ReticleSweepDegrees`, `ReticleAppearScale`

**Parámetros de Mineral (sin cambios S102):**
- `MineralDiscRadius`, `MineralColor`, `MineralInnerAlpha`, `MineralOuterAlpha`, `MineralRingAlpha`, `MineralRingThickness`

**Parámetros de Minería (sin cambios S102):**
- `MiningArcRadius`, `MiningArcThickness`, `MiningArcAlpha`

**Parámetros de Percept (sin cambios S102):**
- `PerceptAlpha`, `PerceptFarAlpha`, `PerceptThickness`, `PerceptDashLength`, `PerceptDashGap`, `PerceptFlowSpeed`

## Métodos Públicos

- `ColorFor(CreatureIntent intent) → Color` — busca en diccionario, fallback a DefaultIntentColor

- `PopulateDefaults()` → void — **Botón Odin:** inicializa diccionario con intents S101/S100

## PopulateDefaults() — Colores S101 + S100

| CreatureIntent | Color | Significado |
|---|---|---|
| Idle/Wandering | (0.75, 0.75, 0.75) | Neutral gris |
| Collecting | (0, 1, 1) | Cyan recolección |
| Carrying | (1, 0.8, 0.25) | Amarillo-naranja |
| Taking | (0.4, 1, 0.9) | Cyan claro |
| Securing | (1, 0.92, 0.45) | Naranja claro |
| Guarding | (0.45, 0.65, 0.95) | Azul claro |
| Hunting | (0.9, 0.3, 0.1) | Naranja oscuro |
| Fleeing | (0.9, 0.2, 0.2) | Rojo miedo |
| Taunting | (0.95, 0.3, 0.75) | Rosa intenso |
| Losing | (0.6, 0.62, 0.72) | Gris azulado |
| Clashing | (1, 0.45, 0.15) | Naranja combate |
| Dazed | (0.75, 0.6, 0.95) | Violeta |

## Invariantes S102

- **Diccionario extensible:** nuevos intents se agregan a PopulateDefaults sin cambiar ArenaCueOverlay
- **Paleta coherente:** cálidos (amarillo/naranja) para progreso; fríos (azul) para defensa; rojos para agresión
- **Visión condicional:** parámetros solo aplican si agente.HasVisionCone (ExpeditionRulesSO.Current != null)
- **Suavizado de giro:** VisionTurnSmoothing = exponente negativo para Lerp(curr, target, 1 - Exp(-smoothing * dt))

## Conexiones

- [[ArenaCueOverlay]] — lector de todos los parámetros
- [[CueDrawer]] — recibe valores para Disc, Ring, Arc, Sector, DashedSegment
- [[DrawVisionCone]] — usa VisionFillInnerAlpha, VisionFillOuterAlpha, VisionEdgeAlpha, VisionSideAlpha, NearRingAlpha, VisionTurnSmoothing
- [[CreatureIntent]] — keys del diccionario
- [[VisionProfile]] — no accede CueStyleSO, pero ArenaCueOverlay usa ambos

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
