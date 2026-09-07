---
tags: [script, world, ai, expedition, task]
---

# AgentGuard.cs

**Ruta:** `World/AI/AgentGuard.cs`

**Responsabilidad:** Colaborador de `AgentExpedition` que implementa `IExpeditionTask`. Custodia un post (veta central o inyectado) e intercepta provocadores. Navega al post, mantiene posición dentro de GuardRadius con repath, detecta tauntadores cercanos y los persigue (GuardChaseSeconds), luego retorna. Emite Molesto al comenzar persecución.

**Lógica:**
1. `TryEngage()` — valida post usable, establece destino
2. `Tick()` — mientras no esté en Chasing:
   - Busca tauntador en GuardChaseRadius
   - Si encuentra: persigue (GuardChaseSeconds, HuntRepathInterval)
   - Si no: HoldAtPost() en GuardRadius con repath
   - Mira rival cercano o post
   - Accumula IdleSeconds si sin rivales visibles

**Propiedades internas:**
- `post` — MaterialPickup custodiado
- `chasing` — rival actual en persecución (null si idle en post)
- `chaseUntil` — tiempo límite de persecución
- `idle` — contador de segundos ociosos sin rivales

**Métodos público:**
- `bool TryEngage(ExpeditionRulesSO rules)` → bool
- `bool Tick(ExpeditionRulesSO rules)` → bool — retorna false si post no es usable
- `Cancel()` — borra chasing, post, timers
- `void ResetForReuse()` — completo reset
- `bool IsChasing { get; }` — si persiguiendo tauntador
- `float IdleSeconds { get; }` — segundos sin rival visible
- `CreatureIntent Intent` — Chasing o Guarding
- `Transform TargetTransform` — rival (si chasing) o post

**Integración:**
- Ocupación `Guard` → prueba GuardTryEngage() antes que fallback a Gatherer
- Persecución se pierde por timeout, airborne, held, o distancia > GuardChaseRadius*1.75

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[IExpeditionTask]], [[AgentExpedition]], [[ExpeditionNav]], [[AgentContext]]
