---
tags: [script, world, visualization, expedition, util]
---

# CreatureCueDrawer.cs

**Ruta:** `World/Expedition/CreatureCueDrawer.cs` (clase estática)

**Responsabilidad:** Librería de métodos estáticos para dibujar guías visuales de criaturas individuales (base, minado, huida, confianza, social, explosiones de habilidad, choque). Cada método lee estado del agente y delega a CueDrawer para renderizado.

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

- `Clash(CueStyleSO style, MoriMonchiController controller, float alphaMul)` — flecha hacia rival en combate:
  - Si ClashTarget == null, retorna sin dibujar
  - Flecha (Arrow) desde controller hacia target, color FightColor pulsante
  - PathThickness*1.5, con cabeza (HeadLength, HeadWidth), tail alpha reducida

**Parámetros Comunes:**
- `style` (CueStyleSO) — diccionario de tuning visual
- `controller` (MoriMonchiController) — acceso a DNA, Agent, transform
- `origin` (Vector3) — posición base para dibujos (típicamente transform.position + Vector3.up * HeightOffset)
- `alphaMul` (float) — multiplicador de alpha por reveal state (rival)

**Integración:**
- Llamados desde ArenaCueOverlay.LateUpdate() en el loop por criaturas
- Condiciones de visibilidad controladas por ArenaCueOverlay (showMining, showFlee, etc.)

**S107 (NUEVO):**
- Extrae lógica de guías de criaturas que antes estaba en ArenaCueOverlay
- Agrupa métodos por aspecto visual (base, carreo, combate, habilidades, sociales)
- Facilita testing y reutilización

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaCueOverlay]], [[CueDrawer]], [[CueStyleSO]], [[MoriMonchiController]], [[MoriMochiAgent]]
