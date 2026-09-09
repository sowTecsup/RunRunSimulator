---
tags: [script, world, visualization, expedition, util]
---

# CreatureCueDrawer.cs

**Ruta:** `World/Expedition/CreatureCueDrawer.cs` (clase estática)

**Responsabilidad:** Librería de métodos estáticos para dibujar guías visuales de criaturas individuales (base, minado, huida, confianza, social, explosiones de habilidad, telegrafía de choque). Cada método lee estado del agente y delega a CueDrawer para renderizado.

**Métodos Públicos:**

- `Base(CueStyleSO style, MoriMonchiController controller, Vector3 origin)` — disco base + anillo:
  - Color según team (FriendColor si Player, FoeColor si Rival, BaseColor sino)
  - BaseRadius + BaseInnerAlpha, BaseRingAlpha

- `Mining(CueStyleSO style, MoriMonchiController controller, Vector3 origin, float alphaMul)` — arcos radiales para carreo:
  - Dibuja N arcos (N = capacity) alrededor de origin con slot angular uniforme
  - Track (Taking, alpha=0.15) para slots vacíos
  - Full (Carrying, alpha=MiningArcAlpha) para slots llenos
  - Live (Taking, alpha=MiningArcAlpha) para slot en progreso si Mining && MiningProgress > 0

- `Flee(CueStyleSO style, MoriMonchiController controller, Vector3 origin)` — anillo de huida pulsante:
  - Si Intent != Fleeing, retorna sin dibujar
  - FleeColor pulsante (0.45 + 0.45*sin wave)
  - Doble anillo (FleeRingRadius + FleeRingRadius*1.5)

- `Trust(CueStyleSO style, MoriMonchiController controller)` — línea hacia custodio:
  - Si TrustedGuardian == null, retorna sin dibujar
  - DashedSegment desde controller hacia guardian (ambos a HeightOffset)
  - FriendColor pulsante suave

- `Social(CueStyleSO style, MoriMonchiController controller)` — línea entre parejas sociales:
  - Si SocialPartner == null, retorna sin dibujar
  - Deduplicación: solo dibuja si controller.ID < partner.ID
  - Color según intención: FightColor pulsante si Fighting, SocialLinkColor sino
  - Parámetro `fighting` en DashedSegment para anotación visual

- `AbilityBursts(CueStyleSO style, MoriMonchiController controller, Vector3 origin, float alphaMul)` — anillos expansivos post-disparo:
  - Recorre 3 slots de abilities
  - Si FiredAt >= 0 y age < AbilityBurstSeconds, dibuja anillo expansivo
  - Radio interpola AbilityBurstRadiusFrom→To, alpha decay por (1-k)
  - Color de ability.Color

- `Telegraph(CueStyleSO style, MoriMonchiController controller, Vector3 origin, float alpha, float blink)` — **S108 NUEVO** plantilla de área de impacto del movimiento de choque vigente:
  - Lee agent.ClashMove (movimiento vigente, null = sin telegrafía)
  - Si alpha <= 0.01 o move == null, retorna sin dibujar
  - `appear` escala 0.85→1 suave; `pulse` escala durante impacto (TelegraphPulseSpeed/Amount)
  - `blink` multiplica alfa de bordes (0.55→1) y relleno (0.55→1) para efecto de parpadeo
  - `groundY` = y del atacante, o y de ImpactPoint si atacante está en aire
  - Por slot del movimiento:
    * **Horn**: cápsula desde atacante a ImpactPoint
      - Pista (Capsule trazo tenue): `trackAlpha`
      - Relleno (Capsule, progresa desde 0 a r*k): `fillAlpha/_OuterAlpha`
      - Contorno (CapsuleOutline): color del poder, aditivo, con parpadeo `blink`
    * **Back**: disco bajo atacante
      - Pista (Disc): `trackAlpha`
      - Relleno (Disc, crece desde 0 a R*k): `fillAlpha/_OuterAlpha`
      - Contorno (Ring): color del poder, aditivo, con parpadeo y pulso
    * **Wings**: disco en punto de caída
      - Pista (Disc): `trackAlpha`
      - Relleno (Disc, crece desde 0 a r*k): `fillAlpha/_OuterAlpha`
      - Anillo contrayéndose (desde r*RingScale a r*appear): color del poder, aditivo, con parpadeo
      - Línea punteada (DashedSegment) desde atacante a caída si Tell01 < 1
  - **Color del poder:** AbilitySO.Color del slot cuya Move coincide; fallback FightColor
  - **Color de equipo:** FriendColor (Player) / FoeColor (Rival) / BaseColor (neutro)
  - Parámetros de CueStyleSO: TelegraphEdgeAlpha, EdgeThickness, TrackAlpha, FillAlpha, FillOuterAlpha, RingScale, PulseSpeed, PulseAmount, BlinkMin

**Parámetros Comunes:**
- `style` (CueStyleSO) — diccionario de tuning visual
- `controller` (MoriMonchiController) — acceso a DNA, Agent, transform
- `origin` (Vector3) — posición base para dibujos (típicamente transform.position + Vector3.up * HeightOffset)
- `alphaMul` (float) — multiplicador de alpha por reveal state (rival)
- `alpha` (float) — opacidad global de la telegrafía (fade in/out suave)
- `blink` (float) — onda de parpadeo [0,1], multiplica alfa de bordes y relleno para efecto pulsante

**Integración:**
- Llamados desde ArenaCueOverlay.LateUpdate() en el loop por criaturas
- Condiciones de visibilidad controladas por ArenaCueOverlay (showClash, showMining, etc.)
- Telegraph se llama independientemente de reveal state (ambos equipos ven la telegrafía siempre)

**S107 (ANTIGUO):**
- Extrae lógica de guías de criaturas que antes estaba en ArenaCueOverlay
- Agrupa métodos por aspecto visual (base, carreo, combate, habilidades, sociales)
- Facilita testing y reutilización
- `Clash(style, controller, alphaMul)` — flecha roja hacia rival en combate (S107 ELIMINADO en S108)

**S108 Cambios:**
- Se ELIMINA `Clash()` — la flecha roja se reemplaza por la plantilla de telegrafía
- Se AGREGA `Telegraph()` — nueva plantilla animada que muestra área de impacto
- `showClash` en ArenaCueOverlay ahora gobierna la telegrafía, no la flecha
- Invariante: sin estado (el parpadeo y fundido se calculan en ArenaCueOverlay como CueState)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaCueOverlay]], [[CueDrawer]], [[CueStyleSO]], [[MoriMonchiController]], [[MoriMochiAgent]], [[ClashMoveSO]], [[AbilitySO]]
