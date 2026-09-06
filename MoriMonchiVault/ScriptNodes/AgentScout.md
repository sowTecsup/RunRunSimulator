---
tags: [script, world, ai, expedition, internal]
---

# AgentScout.cs

**Ruta:** `World/AI/AgentScout.cs`

**Responsabilidad:** Colaborador interno de `AgentExpedition` (composición, no partial) que maneja fase Exploring (S103). State machine con dos pasos: Traveling (navegar a veta), Reporting (dar parte tras llegar). Consulta `ctx.Board` para elegir veta (`NextSite`), navega con repath, detecta arribo (distancia + bloqueo), marca visitada, reporta veta al pizarrón (incrementa counter si fresco), emote Curioso/Feliz. Retorna false cuando se aburre (`GiveUpSeconds`) o completa ciclo (newCycle). `Cancel()` limpia sin resetear elapsed.

**Constructor:**
- `AgentScout(MoriMochiAgent owner, AgentContext ctx)` — recibe referencias

**Propiedades:**
- `int Reports { get; }` — conteo de reportes emitidos en esta instancia
- `Transform TargetTransform { get; }` — target veta (null si no hay)
- `CreatureIntent Intent { get; }` — Exploring o Reporting

**Métodos públicos:**
- `bool TryEngage(ExpeditionRulesSO rules)` — intenta iniciar explore. Retorna false si cooldown activo (restUntil) o no hay veta. Prepara site, timer, state=Traveling, ctx.State=Expedition
- `bool Tick(ExpeditionRulesSO rules)` — procesa frame (Traveling: repath, arrival detect, report; Reporting: face, countdown). Retorna false si termina o falla
- `Cancel()` — aborta sin resetear elapsed (para cuando clash ocurra)
- `ResetForReuse()` — limpia todo para pool recycle (elapsed, repathTimer, restUntil, reports)

**Internals:**
- `ReportSeen(TeamBlackboard board, ExpeditionRulesSO rules)` — itera percepts, reporta MaterialPickup visibles (no la veta target)
- `ApproachPoint(ExpeditionRulesSO rules)` → Vector3 — punto de aproximación a la veta

**State Machine:**
- `Traveling` — navega hacia veta con repath cada `RepathInterval`. Detecta arribo por (distancia ≤ rim) O (bloqueado >0.8s cerca). OnArrive: reporta, emote, pasa a Reporting
- `Reporting` — se detiene, mira veta, cuenta `ReportSeconds`, luego retorna false

**Integration:**
- Llamado por `AgentExpedition.TryEngage(Explore)` vía `scout.TryEngage(rules)`
- Tickeado por `AgentExpedition.TickExpedition()` si phase=Exploring
- Abortado por `AgentExpedition.Cancel()` cuando clash ocurre

**S103:** Fase Exploring delegada a AgentScout (composición limpia). Equipo que explora conoce vetas vía pizarrón, estrategia de recolección mejora consultando pizarrón en `TryGatherEngage`. Intenciones Exploring/Reporting tienen colores en CueStyleSO.

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[AgentExpedition]], [[MoriMochiAgent]], [[AgentContext]], [[TeamBlackboard]], [[ExpeditionRulesSO]], [[CreatureIntent]]
