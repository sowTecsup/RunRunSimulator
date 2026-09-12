---
tags: [script, world, visualization, expedition, util]
---

# CreatureCueDrawer.cs

**Ruta:** `World/Expedition/CreatureCueDrawer.cs` (clase estática)

**Responsabilidad:** Librería de métodos estáticos para dibujar guías visuales de criaturas individuales (base, minado, huida, confianza, social, explosiones de habilidad, telegrafía de choque, anillo de impacto). Cada método lee estado del agente y delega a CueDrawer para renderizado. **S116:** Telegraph reduce a plantilla única = disco/anillo por golpe (un solo color de movimiento, relleno lineal por Tell01, sin blink/pulso/anillo contrayéndose/color de habilidad/cinta).

**Métodos Públicos:**

- `Base(CueStyleSO style, MoriMonchiController controller, Vector3 origin)` — disco base + anillo:
  - Color según team (FriendColor si Player, FoeColor si Rival, BaseColor sino)
  - BaseRadius + BaseInnerAlpha, BaseRingAlpha

- `Mining(CueStyleSO style, MoriMonchiController controller, Vector3 origin, float alphaMul)` — arcos radiales para carreo:
  - Dibuja N arcos (N = capacity) alrededor de origin
  - Track (Taking, alpha=0.15) para slots vacíos
  - Full (Carrying, alpha=MiningArcAlpha) para slots llenos
  - Live (Taking, alpha=MiningArcAlpha) para slot en progreso

- `Flee(CueStyleSO style, MoriMonchiController controller, Vector3 origin)` — anillo de huida pulsante:
  - Si Intent != Fleeing, retorna sin dibujar
  - FleeColor pulsante
  - Doble anillo

- `Trust(CueStyleSO style, MoriMonchiController controller)` — línea hacia custodio:
  - Si TrustedGuardian == null, retorna sin dibujar
  - DashedSegment desde controller hacia guardian
  - FriendColor pulsante suave

- `Social(CueStyleSO style, MoriMonchiController controller)` — línea entre parejas sociales:
  - Si SocialPartner == null, retorna sin dibujar
  - Deduplicación: solo dibuja si controller.ID < partner.ID
  - Color según intención

- `AbilityBursts(CueStyleSO style, MoriMonchiController controller, Vector3 origin, float alphaMul)` — anillos expansivos post-disparo:
  - Recorre 3 slots de abilities
  - Si FiredAt >= 0 y age < AbilityBurstSeconds, dibuja anillo expansivo
  - Radio interpola, alpha decay

- `Telegraph(CueStyleSO style, MoriMonchiController controller, Vector3 origin, float alpha, float flash)` — **S116 SIMPLIFICADO** plantilla de área de impacto del movimiento de choque vigente:
  - Lee agent.ClashMove (movimiento vigente, null = sin telegrafía)
  - Si alpha <= 0.01 o move == null, retorna sin dibujar
  - **Una forma por golpe = hitbox:**
    * Horn: cápsula desde atacante a ImpactPoint
    * Back: disco bajo atacante
    * Wings: disco en punto de caída
  - **Relleno lineal por ClashTell01:**
    * `k = agent.ClashTell01`
    * `fill = Lerp(0, fullRadius, k)` en cada slot
  - **Un solo color de movimiento:**
    * Color de equipo: FriendColor (Player) / FoeColor (Rival) / BaseColor (neutro)
  - **Sin blink/pulso/anillo contrayéndose/color de habilidad:**
    * Flash: multiplica alfa de bordes linealmente (0→1 durante FlashSeconds post-impacto)
  - **Sin cinta de picada:** Wings es solo disco, no arco parabólico
  - Parámetros de CueStyleSO: EdgeAlpha, EdgeThickness, TrackAlpha, FillAlpha, FadeSeconds, FlashSeconds

- `ImpactRing(CueStyleSO style, MoriMonchiController controller, Vector3 center, float baseRadius, float t01)` — **S116 NUEVO** anillo de impacto post-golpe:
  - Dibuja en `center` (posición real del impacto)
  - Radio interpola: `Lerp(baseRadius, baseRadius * ImpactRingScale, easeOutQuad)`
  - Color: color de equipo
  - Alpha decay: `(1 - t01)`
  - Duración: ImpactRingSeconds

**Parámetros Telegraph():**
- `style` (CueStyleSO) — diccionario de tuning visual
- `controller` (MoriMonchiController) — acceso a DNA, Agent, transform
- `origin` (Vector3) — posición base para dibujos
- `alpha` (float) — opacidad global de la telegrafía (fade in/out suave)
- `flash` (float) — multiplicador lineal de alfa de bordes post-impacto [0,1]

**Integración:**
- Telegraph y ImpactRing llamados desde ArenaCueOverlay.LateUpdate()
- Condiciones de visibilidad controladas por ArenaCueOverlay (showClash)
- Telegraph visible para ambos equipos

## S116 Cambios

**Telegraph() completamente simplificada:**

```csharp
public static void Telegraph(CueStyleSO style, MoriMonchiController controller, Vector3 origin, float alpha, float flash)
{
    var agent = controller.Agent;
    var move = agent.ClashMove;
    if (move == null || alpha <= 0.01f) return;

    float k = agent.ClashTell01;  // 0→1 durante anticipación+bloqueo+impacto
    float appear = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, alpha));

    // Color de equipo único (no color de habilidad)
    var team = agent.Team;
    Color teamColor = team == ExpeditionTeam.Player ? style.FriendColor
            : team == ExpeditionTeam.Rival ? style.FoeColor
            : controller.DNA.BaseColor;

    // Trazos y relleno
    float trackAlpha = style.TelegraphTrackAlpha * alpha;
    Color fill = teamColor;
    float fillAlpha = style.TelegraphFillAlpha * alpha;
    Color rim = teamColor;
    rim.a = Mathf.Lerp(style.TelegraphEdgeAlpha, 1f, flash) * alpha;  // Flash lineal

    float groundY = agent.IsAirborne ? agent.ClashImpactPoint.y : controller.transform.position.y;
    Vector3 impact = new Vector3(agent.ClashImpactPoint.x, groundY + style.HeightOffset, agent.ClashImpactPoint.z);
    Vector3 foot = new Vector3(origin.x, groundY + style.HeightOffset, origin.z);

    // Una forma por golpe = hitbox
    switch (move.Slot)
    {
        case ClashSlot.Horn:
        {
            float r = move.HitRadius * appear;
            Vector3 b = impact;
            Vector3 planar = b - foot;
            planar.y = 0f;
            if (planar.magnitude < 0.05f) b = foot + controller.transform.forward * 0.05f;

            CueDrawer.Capsule(foot, b, r, fill, trackAlpha, trackAlpha);
            if (k > 0.01f)
                CueDrawer.Capsule(foot, Vector3.Lerp(foot, b, k), r, fill, fillAlpha, fillAlpha);
            CueDrawer.CapsuleOutline(foot, b, r, style.TelegraphEdgeThickness, rim, rim, true);
            break;
        }
        case ClashSlot.Back:
        {
            float R = move.SweepRadius * appear;
            CueDrawer.Disc(foot, R, fill, trackAlpha, trackAlpha);
            if (k > 0.01f)
                CueDrawer.Disc(foot, R * k, fill, fillAlpha, fillAlpha);
            CueDrawer.Ring(foot, R, style.TelegraphEdgeThickness, rim, true);
            break;
        }
        case ClashSlot.Wings:
        {
            float r = move.HitRadius * appear;
            CueDrawer.Disc(impact, r, fill, trackAlpha, trackAlpha);
            if (k > 0.01f)
                CueDrawer.Disc(impact, r * k, fill, fillAlpha, fillAlpha);
            CueDrawer.Ring(impact, r, style.TelegraphEdgeThickness, rim, true);
            break;
        }
    }
}
```

**ImpactRing() nueva en S116:**

```csharp
public static void ImpactRing(CueStyleSO style, MoriMonchiController controller, Vector3 center, float baseRadius, float t01)
{
    var team = controller.Agent.Team;
    Color teamColor = team == ExpeditionTeam.Player ? style.FriendColor
            : team == ExpeditionTeam.Rival ? style.FoeColor
            : controller.DNA.BaseColor;

    float e = 1f - (1f - t01) * (1f - t01);  // ease out
    float radius = baseRadius * Mathf.Lerp(1f, style.TelegraphImpactRingScale, e);
    Color color = teamColor;
    color.a = style.TelegraphEdgeAlpha * (1f - t01);
    CueDrawer.Ring(center, radius, style.TelegraphEdgeThickness * 1.5f, color, true);
}
```

## Diferencias S114 → S116

| Aspecto | S114 | S116 |
|--------|------|------|
| Plantilla | Variable (pulso, blink, anillo contrayente) | Unitaria (disco/cápsula, relleno lineal) |
| Color | Color de habilidad | Color de equipo |
| Flash | Parpadeo con onda sinusoidal | Flash lineal post-impacto |
| Cinta parabólica | CueRibbonDrawer.Arc() | Eliminada |
| Anillo contrayente | Sí (RingScale, PulseSpeed, PulseAmount) | No |
| Anillo de impacto | No | Sí (ImpactRing nuevo) |
| Relleno | Progresa con Tell01, pulso | Progresa linealmente con Tell01 |

## Invariantes S116

- Sin estado en el método (stateless)
- Parpadeo y fundido calculados por caller (ArenaCueOverlay)
- Telegraph visible para ambos equipos
- Por slot del movimiento (Horn/Back/Wings) varía plantilla
- Un solo color de movimiento (equipo)
- Relleno lineal por Tell01
- ImpactRing post-golpe en posición real

## Vinculado a

[[Index/20 - MVP Combate]], [[Index/23 - Arena Sandbox y Expedicion]], S116

## Conexiones

- [[ArenaCueOverlay]]
- [[CueDrawer]]
- [[CueStyleSO]]
- [[MoriMonchiController]]
- [[MoriMochiAgent]]
- [[ClashMoveSO]]
