---
tags: [script, world, ai, expedition, task]
---

# AgentHunter.cs

**Ruta:** `World/AI/AgentHunter.cs`

**Responsabilidad:** Colaborador de `AgentExpedition` que implementa `IExpeditionTask`. Persigue recolectores sin custodio para forzar que suelten carga. Valida cooldown de retiro, busca presa (solo Intent thief, sin guardián), persigue (GiveUpSeconds), o posta en post si sin presa. Al golpearse (`OnKnocked`), se retira (HunterRetreatSeconds) hacia salida. Emote Molesto al empezar persecución.

**Lógica:**
1. `TryEngage()` — si en cooldown retiro, intenta BeginRetreat; sino busca presa o post
2. `Tick()`:
   - Si retreating: navega a salida, termina en cooldown
   - Si prey: persigue (HuntRepathInterval) hasta GiveUpSeconds; si pierde Intent thief, fallback a post
   - Si post: HoldAtPost, busca presa cada HuntRepathInterval
3. `OnKnocked()` — activa retiro si es Break

**Propiedades internas:**
- `prey` — recolector perseguido
- `post` — post de espera (cuando sin presa)
- `retreating` — en modo retiro post-golpeo
- `retreatUntil` — timestamp de fin de retiro
- `idle` — segundos ociosos en post

**Métodos público:**
- `bool TryEngage(ExpeditionRulesSO rules)` → bool
- `bool Tick(ExpeditionRulesSO rules)` → bool
- `void OnKnocked(ExpeditionRulesSO rules)` — activa retiro si Break
- `void Cancel()` — borra prey, post, state
- `void ResetForReuse()` — completo reset
- `float IdleSeconds { get; }`
- `float Retreat01 { get; }` — normalized [0,1] cooldown retiro
- `CreatureIntent Intent` — Retreating o Hunting
- `Transform TargetTransform` — prey/post/exit

**Integración:**
- Ocupación `Break` → prueba HunterTryEngage() antes que fallback a Gatherer
- FindPrey solo retorna sin Thief intent ni Trusted Guardian
- Al golpearse, inicia retiro hacia HomeExit (como salvoconducto anti-spam)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[IExpeditionTask]], [[AgentExpedition]], [[ExpeditionNav]], [[AgentContext]]
