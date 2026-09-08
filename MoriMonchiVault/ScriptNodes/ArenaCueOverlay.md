---
tags: [script, world, expedition, ui, cues]
---

# ArenaCueOverlay.cs

**Ruta:** `World/Expedition/ArenaCueOverlay.cs`

**Responsabilidad:** Presentación de guías visuales sobre terreno de arena. Dibuja en modo inmediato (`Graphics.RenderMesh` vía `CueDrawer`) por criatura: percepción/visión, atención, rutas, líneas a percepciones, retícula objetivo, enlaces sociales, flecha choque, minería, huida, custodia, ráfagas de habilidades. **S107:** Delega guías de criaturas individuales a CreatureCueDrawer (Base, Mining, Clash, Flee, Trust, Social, AbilityBursts); mantiene interna lógica de percepción, reticle, path drawer.

**Métodos públicos (Entry):**
- `void LateUpdate()` — dibuja todas las criaturas en escena

**Delegación a CreatureCueDrawer (S107):**
- `CreatureCueDrawer.Base()` — disco base + anillo
- `CreatureCueDrawer.Mining()` — arcos de minería (reveal-dependent para rivales)
- `CreatureCueDrawer.Clash()` — flecha hacia combatiente
- `CreatureCueDrawer.Flee()` — anillo de huida pulsante
- `CreatureCueDrawer.Trust()` — línea hacia custodio
- `CreatureCueDrawer.Social()` — línea a pareja social
- `CreatureCueDrawer.AbilityBursts()` — ráfagas post-disparo

**Dibujo Interno:**
1. Equipo Rival:
   - Base (siempre)
   - Mining/Clash/AbilityBursts si reveal > 0.01 (IsRevealing OR Pinned)
   - Selection (marker simple)
   
2. Equipo Jugador:
   - Base (siempre)
   - Percepción (anillo/cono visión con pulsación)
   - Atención (arcos amarillos hacia nearest percept)
   - Path (ruta suavizada vía CuePathDrawer)
   - Percepts (líneas a percepciones coloreadas)
   - Reticle (retícula sobre objetivo expedición)
   - Social (enlace rosa pulsante)
   - Clash (flecha roja)
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
  - `showBase`, `showPerception`, `showPath`, `showPercepts`, `showReticle`, `showSocial`, `showClash`, `showMining`, `showFlee`, `showSelection`, `showAbilities`

**Clases Internas:**
- `CueAnim` — state de fade (Alpha, Visible)
- `CueState` — cache por controller (Path, PerceptionAppear, Reticle, Facing, Selection, Reveal)

**Reveal State (Rivales):**
- `active = ExpeditionNav.IsRevealing(intent) || director.Pinned == agent`
- `reveal = Step(state.Reveal, active, style.RevealSeconds, dt)`
- Si reveal > 0.01, dibuja Mining/Clash/Abilities (desvanecimiento suave)

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

**Invariantes:**
- Dibuja solo criaturas activas (Spawned)
- Rivales tienen lógica de reveal separada (reveal state suave)
- Pulsaciones animadas por time (Sin(time * speed))
- Colors desde CueStyleSO + intención del agente
- Toggles permiten debug granular de cada capa visual

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[CueDrawer]], [[CuePathDrawer]], [[ArenaRoomCueOverlay]], [[CreatureCueDrawer]], [[MoriMonchiController]], [[MoriMochiAgent]], [[CueStyleSO]], [[ArenaCameraDirector]], [[ExpeditionNav]]
