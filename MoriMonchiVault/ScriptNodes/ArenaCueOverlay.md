---
tags: [script, world, expedition, ui, cues]
---

# ArenaCueOverlay.cs

**Ruta:** `World/Expedition/ArenaCueOverlay.cs`

**Responsabilidad:** Presentación de guías visuales sobre terreno de arena. Dibuja en modo inmediato (`Graphics.RenderMesh` vía `CueDrawer`) por criatura: percepción/visión, atención, rutas, líneas a percepciones, retícula objetivo, enlaces sociales, plantilla de choque (telegrafía), minería, huida, custodia, ráfagas de habilidades. S107: Delega guías de criaturas individuales a CreatureCueDrawer (Base, Mining, Clash, Flee, Trust, Social, AbilityBursts); mantiene interna lógica de percepción, reticle, path drawer. **S108:** Reemplaza Clash (flecha roja) por Telegraph (plantilla animada con parpadeo); incorpora estado de telegrafía (CueState.Telegraph, TelegraphPhase) y cálculo de parpadeo (Hz interpolado, onda sinusoidal, blink suave).

**Métodos públicos (Entry):**
- `void LateUpdate()` — dibuja todas las criaturas en escena

**Delegación a CreatureCueDrawer:**
- `CreatureCueDrawer.Base()` — disco base + anillo
- `CreatureCueDrawer.Mining()` — arcos de minería (reveal-dependent para rivales)
- `CreatureCueDrawer.Telegraph()` — **S108 NUEVO** plantilla de choque con parpadeo (reemplaza Clash)
- `CreatureCueDrawer.Flee()` — anillo de huida pulsante
- `CreatureCueDrawer.Trust()` — línea hacia custodio
- `CreatureCueDrawer.Social()` — línea a pareja social
- `CreatureCueDrawer.AbilityBursts()` — ráfagas post-disparo

**Dibujo Interno:**
1. Para ambos equipos (sin rama de rivales en telegrafía):
   - **Telegraph (S108 NUEVO):**
     * Si `showClash == true`:
       - Chequea si `telegraphing` (agent.ClashTelegraphing) y reinicia TelegraphPhase si es nuevo
       - Interpola `tele` alpha suave con Step() (fade in/out TelegraphFadeSeconds)
       - Si tele > 0.01:
         * Calcula Hz interpolado: `Lerp(TelegraphBlinkSpeed, TelegraphBlinkSpeedEnd, ClashTell01)`
         * Avanza fase: `TelegraphPhase += dt * hz`
         * Calcula onda: `wave = 0.5 + 0.5 * sin(phase * 2π)`
         * Calcula blink: `Lerp(TelegraphBlinkMin, 1, SmoothStep(0.25, 0.75, wave))`
         * Llama `CreatureCueDrawer.Telegraph(style, controller, origin, tele, blink)`

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
- `cueMaterial`, `additiveMaterial` [Required] — materiales de renderizado
- `style` (CueStyleSO) — tuning visual de todas las guías
- `director` (ArenaCameraDirector) — para leer Pinned (reveal condition)
- Toggles de visibilidad:
  - `showBase`, `showPerception`, `showPath`, `showPercepts`, `showReticle`, `showSocial`, `showClash` (S108: ahora gobierna telegrafía, no flecha), `showMining`, `showFlee`, `showSelection`, `showAbilities`

**Clases Internas:**
- `CueAnim` — state de fade (Alpha, Visible)
- `CueState` — cache por controller:
  - `Path` (PathCueState)
  - `PerceptionAppear`, `Reticle`, `Reveal`, `Selection` (CueAnim)
  - `Telegraph` (CueAnim) — **S108 NUEVO** estado de fade de telegrafía
  - `TelegraphPhase` (float) — **S108 NUEVO** fase de parpadeo [0, ∞)

**Reveal State (Rivales):**
- `active = ExpeditionNav.IsRevealing(intent) || director.Pinned == agent`
- `reveal = Step(state.Reveal, active, style.RevealSeconds, dt)`
- Si reveal > 0.01, dibuja Mining/AbilityBursts (desvanecimiento suave)

**S108 Cambios:**
- `CueState` gana `Telegraph` (CueAnim) y `float TelegraphPhase`
- LateUpdate: rama de `showClash`:
  - Antes: dibujaba dos Clash (flecha roja para rival dentro reveal + jugador)
  - Ahora (S108): gobierna Telegraph para ambos equipos sin depender de reveal
  - Bloque nuevo de cálculo de Hz, onda, blink (parpadeo)
  - Llama `CreatureCueDrawer.Telegraph()` en lugar de `CreatureCueDrawer.Clash()`
- `Step()` y `AppearScale()` helpers de fade/escala suave

**Métodos Privados:**
- `DrawPerception()` — anillo giratorio dashed O cono de visión suavizado (S102)
- `DrawVisionCone()` — sector relleno con turn smoothing exponencial
- `DrawAttention()` — arcos pulsantes hacia nearest percept
- `DrawPercepts()` — líneas coloreadas (verde aliado, rojo rival)
- `DrawReticle()` — retícula sobre target expedición con pulsación
- `DrawSelection()` — marker simple si Pinned o focused
- `Step()` — fade suave de alpha (entrada/salida)
- `AppearScale()` — interpolación suave de escala de aparición
- `GetCueState()` — lookup/create cache per controller

**Integración:**
- LateUpdate es entry point — recorre Spawned y dibuja según condiciones
- CreatureCueDrawer.Configure(cueMaterial, additiveMaterial) en OnEnable
- Reveal state calculado por rival: si IsRevealing OR Pinned, muestra guías internas
- ArenaRoomCueOverlay renderiza terreno y minerales por separado

**S107 Cambios:**
- Delegación de 7 métodos a CreatureCueDrawer (Base, Mining, Clash, Flee, Trust, Social, AbilityBursts)
- Reduced internal complexity: ArenaCueOverlay ahora solo maneja percepción/atención/reticle/path
- Mejor separación de responsabilidades: CreatureCueDrawer trata guías individuales, ArenaCueOverlay trata terreno/equipo

**S108 Cambios:**
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

**Invariantes:**
- Dibuja solo criaturas activas (Spawned)
- Rivales tienen lógica de reveal separada (reveal state suave)
- Pulsaciones animadas por time (Sin(time * speed)) o state (phase)
- Colors desde CueStyleSO + intención del agente
- Toggles permiten debug granular de cada capa visual
- Parpadeo es suave (onda sinusoidal → SmoothStep, no cuadrado)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[CueDrawer]], [[CuePathDrawer]], [[ArenaRoomCueOverlay]], [[CreatureCueDrawer]], [[MoriMonchiController]], [[MoriMochiAgent]], [[CueStyleSO]], [[ArenaCameraDirector]], [[ExpeditionNav]]
