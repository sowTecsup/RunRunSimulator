---
tags: [script, world, expedition, ui, cues]
---

# ArenaCueOverlay.cs

**Ruta:** `World/Expedition/ArenaCueOverlay.cs`

**Responsabilidad:** Presentación de guías visuales sobre terreno de arena. Dibuja en modo inmediato (`Graphics.RenderMesh` vía `CueDrawer` + `CueRibbonDrawer`) por criatura: percepción/visión, atención, rutas, líneas a percepciones, retícula objetivo, enlaces sociales, plantilla de choque (telegrafía con cinta parabólica S110), minería, huida, custodia, ráfagas de habilidades. S107: Delega guías de criaturas individuales a CreatureCueDrawer (Base, Mining, Clash, Flee, Trust, Social, AbilityBursts); mantiene interna lógica de percepción, reticle, path drawer. S108: Reemplaza Clash (flecha roja) por Telegraph (plantilla animada con parpadeo); incorpora estado de telegrafía (CueState.Telegraph, TelegraphPhase) y cálculo de parpadeo (Hz interpolado, onda sinusoidal, blink suave). S110: Agrega state `DiveArc` (fade de cinta parabólica) y configura `CueRibbonDrawer`. **S114:** Agrega CueState.Hold para fase Holding (embestida bloqueada, lead acotado).

**Métodos públicos (Entry):**
- `void LateUpdate()` — dibuja todas las criaturas en escena

**Delegación a CreatureCueDrawer:**
- `CreatureCueDrawer.Base()` — disco base + anillo
- `CreatureCueDrawer.Mining()` — arcos de minería (reveal-dependent para rivales)
- `CreatureCueDrawer.Telegraph()` — **S108+** plantilla de choque con parpadeo + **S110** cinta parabólica + **S114** fase Hold (reemplaza Clash)
- `CreatureCueDrawer.Flee()` — anillo de huida pulsante
- `CreatureCueDrawer.Trust()` — línea hacia custodio
- `CreatureCueDrawer.Social()` — línea a pareja social
- `CreatureCueDrawer.AbilityBursts()` — ráfagas post-disparo

**Dibujo Interno:**
1. Para ambos equipos (sin rama de rivales en telegrafía):
   - **Telegraph (S108+ MEJORADO S110+S114):**
     * Si `showClash == true`:
       - Chequea si `telegraphing` (agent.ClashTelegraphing) y reinicia TelegraphPhase si es nuevo
       - Interpola `tele` alpha suave con Step() (fade in/out TelegraphFadeSeconds)
       - Interpola `arc` alpha suave para cinta de picada (fade in/out TelegraphFadeSeconds, solo si Tell01 < 1)
       - Calcula `hold` fracción de fase Holding: si AgentClash.phase == Holding, `hold = HoldTimer / HoldSeconds`, sino 0
       - Si tele > 0.01:
         * Calcula Hz interpolado: `Lerp(TelegraphBlinkSpeed, TelegraphBlinkSpeedEnd, ClashTell01)` (S114: Tell01 ahora incluye Holding)
         * Avanza fase: `TelegraphPhase += dt * hz`
         * Calcula onda: `wave = 0.5 + 0.5 * sin(phase * 2π)`
         * Calcula blink: `Lerp(TelegraphBlinkMin, 1, SmoothStep(0.25, 0.75, wave))`
         * Configura alpha scale: `CueDrawer.AlphaScale = 1f` (telegrafía siempre visible)
         * Llama `CreatureCueDrawer.Telegraph(style, controller, origin, tele, blink, arc, eye, hold)` — **S114 parámetro hold**
         * Restaura alpha scale: `CueDrawer.AlphaScale = style.GuideAlpha`

2. Equipo Rival:
   - Base (siempre)
   - Mining/AbilityBursts si reveal > 0.01 (IsRevealing OR Pinned)
   - Selection (marker simple)
   
3. Equipo Jugador:
   - Base (siempre)
   - Percepción (anillo/cono visión con pulsación)
   - Atención (arcos amarillos hacia nearest percept)
   - Path (ruta suavizada vía CuePathDrawer)
   - Percepts (líneas a percepciones coloreadas)
   - Reticle (retícula sobre objetivo expedición)
   - Social (enlace rosa pulsante)
   - Mining (arcos de minería)
   - Flee (anillo amarillo pulsante)
   - Trust (línea de custodia)
   - AbilityBursts (ráfagas radiales)
   - Selection (marker simple)

**Campos Serializados:**
- `sandbox` [Required] — acceso a criaturas
- `cueMaterial`, `additiveMaterial` [Required] — materiales de renderizado CueDrawer
- `ribbonMaterial`, `ribbonAdditiveMaterial` [Required] — **S110 NUEVO** materiales de renderizado CueRibbonDrawer (cinta)
- `style` (CueStyleSO) — tuning visual de todas las guías
- `director` (ArenaCameraDirector) — para leer Pinned (reveal condition)
- Toggles de visibilidad:
  - `showBase`, `showPerception`, `showPath`, `showPercepts`, `showReticle`, `showSocial`, `showClash` (S108: ahora gobierna telegrafía + S110: cinta + S114: Hold), `showMining`, `showFlee`, `showSelection`, `showAbilities`

**Clases Internas:**
- `CueAnim` — state de fade (Alpha, Visible)
- `CueState` — cache por controller:
  - `Path` (PathCueState)
  - `PerceptionAppear`, `Reticle`, `Reveal`, `Selection` (CueAnim)
  - `Telegraph` (CueAnim) — estado de fade de telegrafía
  - `TelegraphPhase` (float) — fase de parpadeo [0, ∞)
  - `DiveArc` (CueAnim) — **S110 NUEVO** estado de fade de cinta parabólica
  - `Hold` (CueAnim) — **S114 NUEVO** estado de fase Holding (bloqueo explícito antes de impacto)

**Reveal State (Rivales):**
- `active = ExpeditionNav.IsRevealing(intent) || director.Pinned == agent`
- `reveal = Step(state.Reveal, active, style.RevealSeconds, dt)`
- Si reveal > 0.01, dibuja Mining/AbilityBursts (desvanecimiento suave)

**OnEnable (S110 MEJORADO):**
- `CueDrawer.Configure(cueMaterial, additiveMaterial)` — configura dibujante de shapes
- `CueRibbonDrawer.Configure(ribbonMaterial, ribbonAdditiveMaterial)` — **S110 NUEVO** configura dibujante de cintas

**LateUpdate (S110 MEJORADO S114):**
- `CueDrawer.AlphaScale = style.GuideAlpha` — multiplicador global (excepto telegrafía)
- `eye = Camera.main.transform.position` — posición cámara para orientar cintas **S110**
- Loop por criaturas:
  - Calcula reveal state (rivales)
  - Si showClash:
    - Calcula `tele` alpha (fade suave)
    - Calcula `arc` alpha (fade suave, Tell01-dependent) — **S110**
    - Calcula `hold` fracción (S114: si Holding, hold = HoldTimer / HoldSeconds; sino 0)
    - Si tele > 0.01:
      - Calcula Hz de parpadeo, onda sinusoidal, blink
      - Setea `AlphaScale = 1f` (excepción: telegrafía siempre visible)
      - Llama `Telegraph(style, controller, origin, tele, blink, arc, eye, hold)` — **S114 hold aditivo**
      - Restaura `AlphaScale = style.GuideAlpha`

## Cambios S107

- Delegación de 7 métodos a CreatureCueDrawer (Base, Mining, Clash, Flee, Trust, Social, AbilityBursts)
- Reduced internal complexity: ArenaCueOverlay ahora solo maneja percepción/atención/reticle/path
- Mejor separación de responsabilidades: CreatureCueDrawer trata guías individuales, ArenaCueOverlay trata terreno/equipo

## Cambios S108

- `CueState` gana Telegraph (CueAnim) y TelegraphPhase (float)
- Reemplazo de rama `showClash`:
  - Antes: dos llamadas a CreatureCueDrawer.Clash (rival en reveal + jugador)
  - Ahora: lógica unificada de Telegraph para ambos equipos, sin reveal gate
  - Calcula Hz dinámico interpolado (TelegraphBlinkSpeed → BlinkSpeedEnd por Tell01)
  - Mantiene phase persistente en CueState (suma dt*hz cada frame)
  - Calcula onda sinusoidal y blink suave (SmoothStep de 0.25 a 0.75 de la onda)
  - Llama Telegraph una sola vez con alpha y blink calculados
- Invariante: presentación solo lee fachadas (nadie escribe en AgentClash desde overlay)
- Telegrafía visible para ambos equipos (excepción deliberada a reveal, usuario necesita ver los golpes)

## Cambios S110

- `CueState.DiveArc` (CueAnim) — fade de cinta parabólica
  - Visible si `telegraphing && Tell01 < 1f` (anticipación, antes del impacto)
  - Alpha interpola suave (TelegraphFadeSeconds)
- Campos serializados: `ribbonMaterial`, `ribbonAdditiveMaterial` [Required]
- `OnEnable()` llama `CueRibbonDrawer.Configure(ribbonMaterial, ribbonAdditiveMaterial)`
- `LateUpdate()`:
  - Calcula `arc = Step(state.DiveArc, telegraphing && Tell01 < 1f, TelegraphFadeSeconds, dt)`
  - Pasa `arc` alpha a Telegraph (parámetro nuevo)
  - Pasa `eye` (Camera.main.position) a Telegraph (parámetro nuevo)
- `Telegraph()` recibe `arcAlpha` y `eye` y usa CueRibbonDrawer.Arc() en rama Wings si arcAlpha > 0.01

## Cambios S114

- `CueState.Hold` (CueAnim) — fase explícita de bloqueo (embestida recta, lead acotado)
  - Visible si fase == Holding (AgentClash), invisible cuando Striking
  - Alpha interpola suave (TelegraphFadeSeconds)
- Cálculo de `hold` en LateUpdate():
  - Si `agent.Clash.phase == Holding`, `hold = agent.Clash.HoldTimer / agent.Clash.Move.HoldSeconds`
  - Sino, `hold = 0`
- `Telegraph()` recibe parámetro `hold` (fracción [0,1]) y lo usa para extender telegrafía (no cambia rampas visuales, solo informa duración)
- Tell01 ahora incluye Holding en la ventana [0,1] (Anticipating=0→Holding=progreso→Striking=1)

## Invariantes S102 + S108 + S110 + S114

- Dibuja solo criaturas activas (Spawned)
- Rivales tienen lógica de reveal separada (reveal state suave)
- Pulsaciones animadas por time (Sin(time * speed)) o state (phase)
- Colors desde CueStyleSO + intención del agente
- Toggles permiten debug granular de cada capa visual
- Parpadeo es suave (onda sinusoidal → SmoothStep, no cuadrado)
- Telegrafía con cinta es aditiva (siempre visible, no afectada por GuideAlpha)
- Cinta parabólica solo se dibuja durante anticipación (Tell01 < 1), desvanece en impacto
- Holding visible durante su duración, sin cambios de color respecto a Anticipating

## Métodos Privados

- `DrawPerception()` — anillo giratorio dashed O cono de visión suavizado (S102)
- `DrawVisionCone()` — sector relleno con turn smoothing exponencial
- `DrawAttention()` — arcos pulsantes hacia nearest percept
- `DrawPercepts()` — líneas coloreadas (verde aliado, rojo rival)
- `DrawReticle()` — retícula sobre target expedición con pulsación
- `DrawSelection()` — marker simple si Pinned o focused
- `Step()` — fade suave de alpha (entrada/salida)
- `AppearScale()` — interpolación suave de escala de aparición
- `GetCueState()` — lookup/create cache per controller

## Vinculado a

- [[Index/20 - MVP Combate]], [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox y Expedicion (S102-S103)]], S114

## Conexiones

- [[ArenaSandbox]] — acceso a criaturas
- [[CueDrawer]] — renderizado de shapes
- [[CueRibbonDrawer]] — **S110** renderizado de cintas
- [[CuePathDrawer]] — renderizado de rutas
- [[ArenaRoomCueOverlay]] — renderiza terreno/minerales por separado
- [[CreatureCueDrawer]] — lógica de guías por criatura
- [[MoriMonchiController]] — control de criatura
- [[MoriMochiAgent]] — estado de criatura, ClashTelegraphing, ClashTell01 (S114: incluye Holding)
- [[CueStyleSO]] — parámetros de estilo
- [[ArenaCameraDirector]] — focalización de cámara
- [[ExpeditionNav]] — reveal conditions
- [[AgentClash]] — lee fase Holding, HoldTimer, Move.HoldSeconds
