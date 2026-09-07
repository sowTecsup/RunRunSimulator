---
tags: [script, world, expedition, ui, cues]
---

# ArenaCueOverlay.cs

**Ruta:** `World/Expedition/ArenaCueOverlay.cs`

**Responsabilidad:** Presentación de guías visuales sobre terreno de arena. Dibuja en modo inmediato (`Graphics.RenderMesh` vía `CueDrawer`) por criatura: anillo/cono de percepción, arcos de atención, ruta suavizada (CuePathDrawer), líneas a percepciones, retícula objetivo, enlaces sociales, flecha choque. **S102:** cono de visión suavizado. **S104: NUEVO** cono de contacto teñido por Orders.Contact, anillo de huida pulsante.

**Métodos públicos (Entry):**
- `void Update()` — dibuja todas las criaturas en escena

**Dibujo por Criatura:**
1. `DrawPerception()` — anillo o cono de visión
2. `DrawAttention()` — arcos de atención (amarillos)
3. `DrawPath()` — ruta suavizada (delegada a CuePathDrawer)
4. `DrawPercepts()` — líneas a percepciones coloreadas (verde aliado, rojo enemigo)
5. `DrawReticle()` — retícula sobre objetivo expedición
6. `DrawSocial()` — enlaces sociales (línea rosa pulsante)
7. `DrawClash()` — flecha choque (roja) si ClashTarget
8. **S104:** `DrawOrdersCue()` — cono de contacto + anillo de huida

**DrawVisionCone (S102):**
- Sector suavizado (relleno inner/outer alpha)
- Borde arc
- Lados si no 360°
- Anillo del oído (dashed fino)
- Rumbo suavizado (exponencial negativa)

**DrawOrdersCue (S104 NUEVO):**
- Si Contact=Fight: cono relleno (ContactFillAlpha) teñido por intención (Fight color o rojo)
- Si Contact=Flee:
  - Anillo pulsante (FleeColor amarillo)
  - Radio FleeRingRadius
  - Grosor FleeRingThickness
  - Pulsación FleeRingSpeed

**Campos Serializados:**
- `sandbox` [Required]
- `cueMaterial` [Required]
- `additiveMaterial` [Required]
- `style` (CueStyleSO) — incluye FleeColor, ContactFillAlpha, ContactEdgeAlpha, FleeRingRadius, FleeRingThickness, FleePulseSpeed (S104)
- Toggles: `showPerception`, `showPath`, `showPercepts`, `showReticle`, `showSocial`, `showClash`, `showOrders` (S104 NUEVO)

**Delegaciones:**
- Ruta: CuePathDrawer (estático)
- Sala: ArenaRoomCueOverlay (minerales, salidas, pizarrón)

**S104 Cambios:**
- DrawOrdersCue() método nuevo
- showOrders toggle para habilitar/deshabilitar
- Cono de contacto teñido por intención
- Anillo de huida con pulsación dinámica
- Integration en Update() si showOrders

**Invariantes:**
- Dibuja solo criaturas activas (Spawned)
- Órdenes renderizadas post-percepción (layer ordering)
- Pulsaciones animadas por time
- Colors desde CueStyleSO + ArenaOrders

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[CueDrawer]], [[CuePathDrawer]], [[ArenaRoomCueOverlay]], [[MoriMonchiController]], [[MoriMochiAgent]], [[CueStyleSO]], [[ArenaOrders]], [[CreatureIntent]]
