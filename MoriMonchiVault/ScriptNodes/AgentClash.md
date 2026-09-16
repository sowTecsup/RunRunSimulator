---
tags: [script, world, ai, expedition, internal]
---

# AgentClash.cs

**Ruta:** `World/AI/AgentClash.cs`

**Responsabilidad:** Máquina de estados de choque/combate físico. Maneja enganche automático, combate manual, fases (Anticipating → Holding → Striking → Resolving → Dazed), delegación física a ClashStrike. **S116:** Delegación completa a strike. **S122:** `EnterHolding` (slot Wings) invoca `owner.onDiveLaunch` justo después del despegue vertical de la picada, una vez por picada.

**Estados:** None, Anticipating, Holding, Striking, Resolving, Dazed

**Métodos públicos:**
- `bool TryEngage()` — intenta choque automático; chequea cooldown, boldness, ocupación
- `bool ForceMove(ClashMoveSO move, MoriMochiAgent rival)` — fuerza movimiento (dev)
- `void TickClashing()` — avanza fase
- `void TickAirborne()` — detecta ápice + impacto (Wings)
- `void ReceiveHit(MoriMochiAgent attacker)` — golpeado; timesKnocked++
- `bool IsTargetable { get; }` — si no dazed
- `void Cancel()` — aborta choque

**S116:** Delegación completa a ClashStrike.
**S122:** Evento `onDiveLaunch` (despegue de la picada) → prefab `Feedbacks/OnDiveLaunch` (humo). El juice vive en MMF, no en código.

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]]

**Conexiones:** [[ClashStrike]], [[MoriMochiAgent]], [[AgentAbilities]]
