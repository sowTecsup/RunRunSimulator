---
tags: [script, world, ai, expedition, interface]
---

# IExpeditionTask.cs

**Ruta:** `World/AI/IExpeditionTask.cs`

**Responsabilidad:** Interfaz interna que define el contrato de un colaborador de ocupación (recolector, guardia, cazador, señuelo, explorador). Cada implementación maneja su propia máquina de estados y retorna fases de intención (`Intent`) y target visual.

**Métodos:**
- `bool Tick(ExpeditionRulesSO rules)` — avanza lógica un frame; retorna false cuando termina ocupación
- `void Cancel()` — detiene sin resetear elapsed (para cuando clash ocurra)
- `void ResetForReuse()` — limpia todo para pool recycle (elapsed, timers, contadores)
- `CreatureIntent Intent { get; }` — intención actual (Collecting, Taking, Guarding, Hunting, Taunting, Fleeing, etc.)
- `Transform TargetTransform { get; }` — target visual (MaterialPickup, rival, salida, null si idle)

**Implementaciones:**
- [[AgentGatherer]] — recolecta material, maneja huida
- [[AgentGuard]] — custodia post, persigue provocadores
- [[AgentHunter]] — persigue recolectores desprotegidos, se retira si golpeado
- [[AgentDecoy]] — provoca rivales, huye
- [[AgentScout]] — explora y reporta vetas

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[AgentExpedition]], [[MoriMochiAgent]], [[AgentContext]], [[ExpeditionRulesSO]]
