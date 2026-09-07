---
tags: [script, world, ai, expedition, task]
---

# AgentScout.cs

**Ruta:** `World/AI/AgentScout.cs`

**Responsabilidad:** Colaborador de `AgentExpedition` (composición, no partial) que implementa `IExpeditionTask` (S104 adición de interfaz). Maneja fase Exploring (S103): navega a veta según `ctx.Board`, marca visitada, reporta al pizarrón incrementando counter si fresco. State machine: Traveling (navega, detecta arribo), Reporting (se detiene, cuenta segundos, emote). Retorna false cuando se aburre (GiveUpSeconds) o completa ciclo (newCycle). `Cancel()` limpia sin resetear elapsed.

**Constructor:**
- `AgentScout(MoriMochiAgent owner, AgentContext ctx)` — recibe referencias

**Propiedades públicas:**
- `int Reports { get; }` — conteo de reportes emitidos en instancia
- `Transform TargetTransform { get; }` — target veta (null si no hay)
- `CreatureIntent Intent { get; }` — Exploring o Reporting (S104: implementa IExpeditionTask.Intent)

**Métodos públicos:**
- `bool TryEngage(ExpeditionRulesSO rules)` → bool — intenta iniciar explore. Retorna false si cooldown activo (restUntil) o no hay veta. Prepara site, timer, state=Traveling, ctx.State=Expedition
- `bool Tick(ExpeditionRulesSO rules)` → bool — procesa frame (Traveling: repath, arrival detect, report; Reporting: face, countdown). Retorna false si termina o falla (S104: implementa IExpeditionTask.Tick)
- `void Cancel()` — aborta sin resetear elapsed (para cuando clash ocurra) (S104: implementa IExpeditionTask.Cancel)
- `void ResetForReuse()` — limpia todo para pool recycle (elapsed, repathTimer, restUntil, reports) (S104: implementa IExpeditionTask.ResetForReuse)

**Internals:**
- `ReportSeen(TeamBlackboard board, ExpeditionRulesSO rules)` — itera percepts, reporta MaterialPickup visibles (no la veta target)
- `ApproachPoint(ExpeditionRulesSO rules)` → Vector3 — punto de aproximación a la veta

**State Machine:**
- `Traveling` — navega hacia veta con repath cada `RepathInterval`. Detecta arribo por (distancia ≤ rim) O (bloqueado >0.8s cerca). OnArrive: reporta, emote, pasa a Reporting
- `Reporting` — se detiene, mira veta, cuenta `ReportSeconds`, luego retorna false

**Integration:**
- Llamado por `AgentExpedition.TryEngage()` si Occupation=Explore
- Tickeado por `AgentExpedition.TickExpedition()` si phase=Exploring
- Abortado por `AgentExpedition.Cancel()` cuando clash ocurre

**S103 (Exploring introducido):** Fase Exploring delegada a AgentScout (composición limpia). Equipo que explora conoce vetas vía pizarrón.

**S104 (IExpeditionTask):** Implementa interfaz para integración con orquestador.

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[IExpeditionTask]], [[AgentExpedition]], [[MoriMochiAgent]], [[AgentContext]], [[TeamBlackboard]], [[ExpeditionRulesSO]], [[CreatureIntent]]
