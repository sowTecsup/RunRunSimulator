---
tags: [script, world, ai, agent, internal, expedition]
---

# AgentExpedition.cs

**Ruta:** `World/AI/AgentExpedition.cs`

**Responsabilidad:** Colaborador interno (composición, no partial) que orquesta ocupaciones de arena: Gather, Guard, Break, Decoy, **Explore** (S103 NUEVO, delegado a AgentScout). State machine por ocupación. **S103:** Contador `secured` ahora público (no solo `collected`). Fase Exploring delegada a colaborador `AgentScout` para separación limpia. `Cancel()` nuevo, separado de `ResetForReuse()` (llamado cuando clash ocurre mid-expedition). `TryGatherEngage()` ahora consulta pizarrón si existe (`ctx.Board`).

**Métodos públicos:**
- `bool TryEngage()` — entry point. Según Occupation: Guard/Break/Decoy/Explore/Gather. Delega a TryXxxEngage
- `bool Tick()` — (interno, llamado TickExpedition) procesa frame según fase
- `Cancel()` — (S103 NUEVO) aborta sin resetear elapsed (usado cuando clash ocurre)
- `ResetForReuse()` — pooling, limpia todo

**Propiedades públicas:**
- `int Collected { get; }` — acumulativo sesión local (recolectado)
- `int Secured { get; }` — (S103 NUEVO) acumulativo sesión local (asegurado)
- `int Carried { get; }` — carga actual
- `float MiningProgress { get; }` — 0-1 progreso minería
- `Transform TargetTransform { get; }` — transform objetivo o null
- `CreatureIntent Intent { get; }` — según fase actual

**Métodos Privados (por ocupación):**
- `TryGatherEngage(rules)` — itera Percepts × Rules, elige mejor score. **S103:** consulta `ctx.Board.BestKnownVein()` si no hay perceptos vivos (conocimiento de pizarrón). Entra Noticing → Moving → Mining → Returning → Securing
- `TryGuardEngage(rules)` — InjectedPost() o FindPost(). Entra Guarding
- `TryBreakEngage(rules)` — FindPrey() o InjectedPost(). Entra Hunting
- `TryDecoyEngage(rules)` — FindDecoyTarget() o InjectedPost(). Entra Decoying con cooldown
- `TryExploreEngage(rules)` — (S103 NUEVO) `scout.TryEngage(rules)`. Si falla, fallback TryGatherEngage. Si ok: target=null, phase=Exploring

**Fases:**
- **Gather:** Noticing → Moving → Mining → Returning → Securing
  - Mining: carried++, si >= Capacity o agotado → BeginReturn
  - Returning: navega a HomeExit
  - Securing: deposita, incrementa `secured`
- **Guard:** Guarding (planta near post, vigila)
- **Break:** Hunting (persigue prey, si no → planta)
- **Decoy:** Decoying (Approach → Taunt → Flee)
- **Explore:** (S103 NUEVO) delegado a `AgentScout` (Traveling → Reporting)

**Colaborador AgentScout (S103 NUEVO):**
- `scout` (AgentScout) — instanciado en ctor
- Usado en `TryExploreEngage()` y `TickExpedition()` si phase=Exploring
- `scout.Reports` contabilizado en propiedades

**Métodos S103:**
- `TryExploreEngage(rules)` — llama `scout.TryEngage(rules)`. Si false, fallback TryGatherEngage. Si true: target=null, phase=Exploring, return true
- `Cancel()` — limpia site/prey sin resetear elapsed (abort sin penalidad)
- `ResetForReuse()` — limpia todo + `scout.ResetForReuse()`

**Internals (sin cambios S102/S103):**
- ApproachPoint, BeginReturn, FindPost, FindPrey, FindDecoyTarget, GuardPoint, etc.

**S103 Cambios:**
- Colaborador `scout` (AgentScout) — fase Exploring delegada
- Propiedad `Secured { get; }` pública (antes interno)
- `TryExploreEngage()` nuevo (Explore → scout.TryEngage)
- `Cancel()` nuevo (aborta sin ResetForReuse)
- `TryGatherEngage()` consulta `ctx.Board.BestKnownVein()` si no hay perceptos
- `TickExpedition()` si phase=Exploring: `scout.Tick()`, si false: Cancel()
- `TryEngage()` chequea Explore en switch

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[AgentScout]], [[TeamBlackboard]], [[MaterialPickup]], [[ExpeditionRulesSO]], [[CreatureIntent]], [[Occupation]]
