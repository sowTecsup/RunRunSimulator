---
tags: [script, data, scriptableobject, expedition]
---

# CueStyleSO.cs

**Ruta:** `Data/Expedition/CueStyleSO.cs`

**Responsabilidad:** **Gancho de datos:** contiene todos los knobs de presentación de guías visuales: diccionario Odin `CreatureIntent → Color` para colorear rutas y anillos, y 50+ parámetros de geometría, animación y velocidad (espesores, radios, tiempos, velocidades de spin). **Cero lógica**; solo lectura desde `ArenaCueOverlay`. Heredero de `SerializedScriptableObject` para poder serializar dicts. Botón `PopulateDefaults()` para precargar colores y valores razonables. Uso: `CueStyleSO.style` (asset referenciado en `ArenaCueOverlay`). **S100:** Agregados intents `Clashing` (naranja) y `Dazed` (violeta).

## Campos Públicos

**Diccionario (Odin):**
- `intentColors` (Dict<CreatureIntent, Color>) — mapping intención → color de ruta/anillo. Poblada por `PopulateDefaults()`. **S100:** incluye `Clashing` y `Dazed`.

**Colores:**
- `DefaultIntentColor` (Color) — fallback si una intención no está en el diccionario (default gris 0.6/0.6/0.6).
- `FriendColor` (Color) — percepto amistoso (verde).
- `FoeColor` (Color) — percepto hostil (rojo).
- `MineralColor` (Color) — minerales (cyan).
- `SocialLinkColor` (Color) — enlace social peaceful (rosa).
- `FightColor` (Color) — enlace social peleando (rojo).

**Geometría:**
- `HeightOffset` (float, default 0.03) — elevación de todas las guías sobre el suelo (z-fighting).
- `RingThickness` (float, default 0.06) — grosor del anillo de percepción (m).
- `RingAlpha` (0-1, default 0.35) — opacidad del anillo.
- `PathThickness` (float, default 0.08) — grosor de la ruta (m).
- `HeadLength` (float, default 0.5) — largo de punta de flecha.
- `HeadWidth` (float, default 0.4) — ancho de punta.
- `PerceptThickness` (float, default 0.03) — grosor de línea a percepto.
- `MineralDiscRadius` (float, default 0.6) — radio del disco mineral.
- `MineralRingThickness` (float, default 0.04) — anillo alrededor del mineral.
- `ReticleRadius` (float, default 0.9) — radio de los 4 arcos de retícula.
- `ReticleThickness` (float, default 0.06) — grosor de retícula.
- `SocialLinkThickness` (float, default 0.05) — grosor del enlace social.

**Aparición (entrada/salida suave):**
- `AppearSeconds` (float, default 0.25) — duración de fade-in (s).
- `AppearScale` (float, default 0.85) — escala inicial (0.85→1.0).

**Percepción:**
- `PerceptAlpha` (0-1, default 0.6) — opacidad base de líneas a percepto.
- `PerceptFarAlpha` (0-1, default 0.1) — opacidad del extremo lejano (degradado).
- `AttentionArcDegrees` (0-180, default 50) — amplitud del arco de atención hacia el percepto más cercano.
- `AttentionAlpha` (0-1, default 0.9) — opacidad del arco de atención (aditivo).
- `PulseSeconds` (float, default 0.35) — duración de la respiración del anillo (ciclo).
- `PulseAmount` (0-1, default 0.05) — amplitud de escala (1 ± amount).

**Percepto (línea punteada):**
- `PerceptDashLength` (float, default 0.2) — largo del dash (m).
- `PerceptDashGap` (float, default 0.2) — separación (m).
- `PerceptFlowSpeed` (float, default 1) — velocidad de flujo del offset (unidades/s).

**Anillo de percepción:**
- `RingDashCount` (int, min 4, default 28) — cantidad de dashes.
- `RingDashRatio` (0-1, default 0.55) — proporción on:off.
- `RingSpinSpeed` (float, default 0.35) — velocidad de rotación (rad/s).

**Retícula:**
- `ReticleSweepDegrees` (0-180, default 50) — amplitud de cada arco (mitad a cada lado del centro).
- `ReticleSpinSpeed` (float, default -0.6) — velocidad de rotación (neg = contrarrota).
- `ReticleAppearScale` (float, default 1.4) — escala inicial de entrada.

**Ruta (Catmull-Rom suavizada):**
- `PathFadeSeconds` (float, default 0.35) — duración del fade in/out de la ruta.
- `PathSmoothing` (float, default 8) — constante exponencial de suavizado destino (higher = más rápido).
- `CurveSamples` (int, 2-24, default 10) — cantidad de segmentos por curva Catmull-Rom.
- `StartTangent` (float, default 1.2) — extensión virtual del inicio para tangente natural.
- `PathFlowSpeed` (float, default 1.5) — velocidad de flujo de dashes en la ruta (unidades/s).
- `PathDashLength` (float, default 0.35) — largo del dash en ruta.
- `PathDashGap` (float, default 0.25) — gap en ruta.
- `PathTailAlpha` (0-1, default 0.15) — opacidad del extremo trasero de la ruta (degradado).
- `DestMarkerRadius` (float, default 0.35) — radio del disco de destino.
- `DestPulseSpeed` (float, default 2.5) — velocidad de pulsación del destino.
- `DestPulseAmount` (0-1, default 0.15) — amplitud de escala (1 ± amount).

**Minerales:**
- `MineralInnerAlpha` (0-1, default 0.35) — opacidad del centro del disc.
- `MineralOuterAlpha` (0-1, default 0) — opacidad del borde (degradado radial).
- `MineralRingAlpha` (0-1, default 0.5) — opacidad del anillo fino.

**Social (enlace punteado):**
- `FightPulseSpeed` (float, default 6) — velocidad de parpadeo en pelea (Hz).

## Métodos Públicos

- `ColorFor(CreatureIntent intent) → Color` — getter: busca en dict, fallback a `DefaultIntentColor`.
- `PopulateDefaults()` — botón Odin: precarga diccionario con 22 intenciones (Idle, Wandering, Following, ..., **Taking, Losing, Clashing, Dazed S100**) + sus colores por defecto; marca dirty.

## PopulateDefaults() — Colores S100

| CreatureIntent | Color (RGB) | Significado |
|---|---|---|
| Idle/Wandering | (0.75, 0.75, 0.75) | Neutral gris |
| Following/Approaching | (0.3, 0.85, 0.3) | Verde amistoso |
| Fleeing/Retreating | (0.9, 0.2, 0.2) | Rojo miedo |
| Chasing | (1, 0.55, 0.1) | Naranja persecución |
| SeekingFood/Eating | (0.95, 0.85, 0.15) | Amarillo hambre |
| SeekingRest/Resting/SleepingTogether | (0.25, 0.5, 0.95) | Azul descanso |
| SeekingPlay/Playing/Socializing | (0.95, 0.5, 0.8) | Rosa social |
| Fighting | (0.55, 0.05, 0.05) | Rojo oscuro pelea |
| Held/Tumbling | (1, 1, 1) | Blanco especial |
| Collecting | (0, 1, 1) | Cyan recolección |
| Taking | (0.4, 1, 0.9) | Cyan claro — beat de consumo (S98) |
| Losing | (0.6, 0.62, 0.72) | Gris azulado — beat rival toma (S99) |
| **Clashing** | **(1, 0.45, 0.15)** | **Naranja combate — estado de lucha (S100)** |
| **Dazed** | **(0.75, 0.6, 0.95)** | **Violeta aturdimiento — post-golpe (S100)** |

## Cambios S100: Combate Físico

**Líneas 114-115 en PopulateDefaults():**
```csharp
AddIfMissing(CreatureIntent.Clashing, new Color(1f, 0.45f, 0.15f));      // naranja
AddIfMissing(CreatureIntent.Dazed, new Color(0.75f, 0.6f, 0.95f));       // violeta
```

**Razón:**
- `Clashing` usa naranja (combate activo, similar a Chasing). Permite distinguir visualmente combate vs. persecución.
- `Dazed` usa violeta (aturdimiento post-golpe, onírico). Refuerza estado "fuera de control" visualmente.

**Consumo:**
- [[ArenaCueOverlay.DrawClash()]] — dibuja flecha roja pulsante del atacante al objetivo
- [[ArenaCueOverlay.DrawPath()]] — ruta al destino coloreada por ColorFor(intent), incluyendo Clashing/Dazed
- [[MonchiMoodDriver]] — mapea intent a mood; Clashing/Dazed disparan cambios emocionales

## Invariantes S100 + S98

- **Diccionario Odin:** `[OdinSerialize]` + `[DictionaryDrawerSettings]` permite serializar diccionarios no-serializable en Unity.
- **Cero lógica:** solo almacenamiento. Toda evaluación geométrica, animación y renderizado vive en `ArenaCueOverlay` y `CueDrawer`.
- **Fallback seguro:** `ColorFor()` nunca falla; retorna default si intención no existe.
- **Asset único:** típicamente un solo asset `CueStyle.asset` por proyecto; `ArenaCueOverlay` lo referencia directamente.
- **Edición viva:** cambiar parámetros en Inspector durante Play mode afecta inmediatamente el render de guías (útil para tuning).
- **S100:** nuevos intents `Clashing` y `Dazed` se añaden automáticamente a `PopulateDefaults()` con colores distintivos (naranja y violeta).

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

- [[ArenaCueOverlay]] (lector de todos los parámetros)
- [[CueDrawer]] (usuario final de espesores, radios, etc.)
- [[CreatureIntent]] (keys del diccionario, **S100:** incluye Clashing/Dazed)
- [[MoriMonchiAgent]] (agente cuyo intent se busca en colorMap, **S100:** incluye Clashing/Dazed)
- [[ExpeditionRulesSO]] (beat timers que disparan transiciones de intent)
- **S100:** [[AgentClash]] (genera Clashing/Dazed intents)
- **S100:** [[MonchiMoodDriver]] (mapea colores/intents a moods)
