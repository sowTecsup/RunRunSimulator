---
tags: [script, world, expedition, ui, cues]
---

# ArenaCueOverlay.cs

**Ruta:** `World/Expedition/ArenaCueOverlay.cs`

**Responsabilidad:** Presentación de guías visuales sobre terreno de arena. Dibuja en modo inmediato (`Graphics.RenderMesh` vía `CueDrawer`) por criatura: percepción/visión, atención, rutas, líneas a percepciones, retícula objetivo, enlaces sociales, plantilla de choque (telegrafía con hitbox + anillo de impacto), minería, huida, custodia, ráfagas de habilidades. S107: Delega guías de criaturas individuales a CreatureCueDrawer; mantiene interna lógica de percepción, reticle, path drawer. S108+: Telegraph con parpadeo. **S116:** Sin ribbon ni campos ribbonMaterial; CueState con StrikeAt/LastHitAt/HitCenter/HitRadius; flash e ImpactRing; Dazed/Tumbling no se saltan si telegrafían o tienen anillo vivo; ruta en color de equipo y oculta mientras ClashMove != null.

**Métodos públicos (Entry):**
- `void LateUpdate()` — dibuja todas las criaturas en escena

**Delegación a CreatureCueDrawer:**
- `CreatureCueDrawer.Base()` — disco base + anillo
- `CreatureCueDrawer.Mining()` — arcos de minería (reveal-dependent para rivales)
- `CreatureCueDrawer.Telegraph()` — **S116** plantilla de choque con hitbox (disco) + **ImpactRing** post-golpe
- `CreatureCueDrawer.Flee()` — anillo de huida pulsante
- `CreatureCueDrawer.Trust()` — línea hacia custodio
- `CreatureCueDrawer.Social()` — línea a pareja social
- `CreatureCueDrawer.AbilityBursts()` — ráfagas post-disparo
- `CreatureCueDrawer.ImpactRing()` — **S116 NUEVO** anillo de impacto post-golpe exitoso

**Clases Internas:**
- `CueAnim` — state de fade (Alpha, Visible)
- `CueState` — cache por controller:
  - `Path` (PathCueState)
  - `PerceptionAppear`, `Reticle`, `Reveal`, `Selection` (CueAnim)
  - `Telegraph` (CueAnim) — estado de fade de telegrafía
  - **S116:** `StrikeAt` (float) — timestamp cuando transición a Striking completa
  - **S116:** `LastHitAt` (float) — timestamp del último impacto (tracking para ImpactRing)
  - **S116:** `HitCenter` (Vector3) — posición del impacto (centro de ImpactRing)
  - **S116:** `HitRadius` (float) — radio de impacto (tamaño de ImpactRing)

**Reveal State (Rivales):**
- `active = ExpeditionNav.IsRevealing(intent) || director.Pinned == agent`
- `reveal = Step(state.Reveal, active, style.RevealSeconds, dt)`
- Si reveal > 0.01, dibuja Mining/AbilityBursts (desvanecimiento suave)

**OnEnable (S116 SIMPLIFICADO):**
- `CueDrawer.Configure(cueMaterial, additiveMaterial)` — configura dibujante de shapes
- **S116:** Sin `CueRibbonDrawer.Configure()`

**LateUpdate (S116 MEJORADO):**

1. **Loop por criaturas — S116 CAMBIO**: Salta si Dazed/Tumbling Y no están telegrafiendo Y no tienen anillo vivo
   ```csharp
   if ((intent == CreatureIntent.Dazed || intent == CreatureIntent.Tumbling) && 
       !clashVisual) continue;  // clashVisual = ClashTelegraphing || (Time.time - HitAt < ImpactRingSeconds)
   ```

2. **Telegraph (S116 SIMPLIFICADO):**
   - Si `showClash == true`:
     - Chequea si `telegraphing` (agent.ClashTelegraphing)
     - Calcula `tele` alpha suave con Step() (fade in/out TelegraphFadeSeconds)
     - Calcula `flash` normalizado: `1 - Clamp01((Time.time - StrikeAt) / FlashSeconds)` si StrikeAt >= 0, sino 0
     - Si tele > 0.01:
       - Configura alpha scale: `CueDrawer.AlphaScale = 1f` (telegrafía siempre visible)
       - Llama `CreatureCueDrawer.Telegraph(style, controller, origin, tele, flash)`
       - Restaura alpha scale: `CueDrawer.AlphaScale = style.GuideAlpha`

3. **ImpactRing (S116 NUEVO):**
   - Si `showClash == true` y `LastHitAt >= 0`:
     - Calcula `ringT = (Time.time - LastHitAt) / ImpactRingSeconds`
     - Si ringT < 1:
       - Configura alpha scale: `CueDrawer.AlphaScale = 1f` (impacto siempre visible)
       - Llama `CreatureCueDrawer.ImpactRing(style, controller, HitCenter, HitRadius, ringT)`
       - Restaura alpha scale: `CueDrawer.AlphaScale = style.GuideAlpha`

4. **Ruta (S116 CAMBIO):**
   - Color: color de equipo (Player → FriendColor, Rival → FoeColor, neutro → DNA.BaseColor)
   - **Visible solo si `ClashMove == null`** (no se dibuja ruta durante combate)
   - Pasa `OnScreen(controller.transform.position)` para fade out si sale de viewport

5. **Equipo Rival:**
   - Base (siempre)
   - Mining/AbilityBursts si reveal > 0.01
   - Selection (marker simple)
   
6. **Equipo Jugador:**
   - Base (siempre)
   - Percepción (anillo/cono visión con pulsación)
   - Atención (arcos amarillos hacia nearest percept)
   - Path (ruta suavizada, color de equipo, visible si ClashMove == null)
   - Percepts (líneas a percepciones)
   - Reticle (retícula sobre objetivo expedición)
   - Social (enlace pulsante)
   - Mining (arcos de minería)
   - Flee (anillo pulsante)
   - Trust (línea de custodia)
   - AbilityBursts (ráfagas radiales)
   - Selection (marker simple)

**Campos Serializados:**
- `sandbox` [Required] — acceso a criaturas
- `cueMaterial`, `additiveMaterial` [Required] — materiales de renderizado CueDrawer
- `style` (CueStyleSO) — tuning visual de todas las guías
- `director` (ArenaCameraDirector) — para leer Pinned
- Toggles de visibilidad:
  - `showBase`, `showPerception`, `showPath`, `showPercepts`, `showReticle`, `showSocial`, `showClash` (S116: governa telegrafía + ImpactRing), `showMining`, `showFlee`, `showSelection`, `showAbilities`

## S116 Cambios

**Eliminación de ribbon:**
- Se remueven campos `ribbonMaterial`, `ribbonAdditiveMaterial`
- Se elimina llamada `CueRibbonDrawer.Configure()` en OnEnable
- Telegraph ahora es plantilla única: un disco por golpe (no arco parabólico)

**CueState nuevos campos (S116):**
```csharp
public float StrikeAt = -1f;        // timestamp cuando Tell01 alcanza 1 (impacto inminente)
public float LastHitAt = -1f;       // timestamp del último impacto (hit conectado)
public Vector3 HitCenter;           // posición del impacto (para ImpactRing)
public float HitRadius = 1f;        // radio de impacto
```

**Lógica de Dazed/Tumbling (S116):**
- Línea ~82: Chequea `clashVisual = agent.ClashTelegraphing || (Time.time - agent.ClashHitAt < style.ImpactRingSeconds)`
- Si intent es Dazed O Tumbling Y clashVisual == false, salta guías
- Si clashVisual == true, mantiene visualización (usuario ve impacto en noqueado)

**Lógica de Path (S116):**
- Línea ~143: Dibuja ruta solo si `ClashMove == null` (no hay combate activo)
- Color: `style.FriendColor` (Player), `style.FoeColor` (Rival), `DNA.BaseColor` (neutro)
- Contexto: evita clutter visual durante combate

**Lógica de Telegraph (S116 SIMPLIFICADA):**
```csharp
if (showClash)
{
    var agent = controller.Agent;
    bool telegraphing = agent.ClashTelegraphing;
    float tele = Step(state.Telegraph, telegraphing, style.TelegraphFadeSeconds, Time.deltaTime);
    
    bool striking = telegraphing && agent.ClashTell01 >= 1f;
    if (!telegraphing) state.StrikeAt = -1f;
    else if (striking && state.StrikeAt < 0f) state.StrikeAt = Time.time;
    
    float flash = state.StrikeAt >= 0f 
        ? 1f - Mathf.Clamp01((Time.time - state.StrikeAt) / Mathf.Max(0.01f, style.TelegraphFlashSeconds)) 
        : 0f;
    
    if (tele > 0.01f)
    {
        CueDrawer.AlphaScale = 1f;
        CreatureCueDrawer.Telegraph(style, controller, origin, tele, flash);
        CueDrawer.AlphaScale = style.GuideAlpha;
    }
    
    // ImpactRing
    if (agent.ClashHitAt > state.LastHitAt)
    {
        state.LastHitAt = agent.ClashHitAt;
        state.HitCenter = agent.ClashHitPoint + Vector3.up * style.HeightOffset;
        var move = agent.ClashMove;
        state.HitRadius = move == null ? 1f : (move.Slot == ClashSlot.Back ? move.SweepRadius : move.HitRadius);
    }
    
    if (state.LastHitAt >= 0f)
    {
        float ringT = (Time.time - state.LastHitAt) / Mathf.Max(0.01f, style.TelegraphImpactRingSeconds);
        if (ringT < 1f)
        {
            CueDrawer.AlphaScale = 1f;
            CreatureCueDrawer.ImpactRing(style, controller, state.HitCenter, state.HitRadius, ringT);
            CueDrawer.AlphaScale = style.GuideAlpha;
        }
    }
}
```

## Invariantes S116

- Dibuja solo criaturas activas (Spawned)
- Rivales tienen lógica de reveal separada
- Dazed/Tumbling sin guías SALVO si telegrafián o tienen anillo vivo (combate visible)
- Rutas solo si ClashMove == null (no combate)
- Telegrafía con disco unitario (hitbox visual)
- ImpactRing post-impacto en posición real (no predicho)
- Flash lineal sin parpadeo previo
- Todos los colores desde CueStyleSO + intención del agente
- Toggles permiten debug granular

## Métodos Privados

- `DrawPerception()` — anillo giratorio dashed O cono de visión suavizado
- `DrawVisionCone()` — sector relleno con turn smoothing exponencial
- `DrawPercepts()` — líneas coloreadas
- `DrawReticle()` — retícula sobre target expedición
- `DrawSelection()` — marker simple si Pinned
- `Step()` — fade suave de alpha
- `AppearScale()` — interpolación suave de escala
- `GetCueState()` — lookup/create cache per controller
- `OnScreen()` — valida viewport

## Vinculado a

[[Index/20 - MVP Combate]], [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox y Expedicion]], S116

## Conexiones

- [[ArenaSandbox]]
- [[CueDrawer]]
- [[CuePathDrawer]]
- [[ArenaRoomCueOverlay]]
- [[CreatureCueDrawer]]
- [[MoriMonchiController]]
- [[MoriMochiAgent]]
- [[CueStyleSO]]
- [[ArenaCameraDirector]]
- [[ExpeditionNav]]
- [[AgentClash]]
