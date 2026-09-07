---
tags: [script, world, ai, expedition, task]
---

# AgentDecoy.cs

**Ruta:** `World/AI/AgentDecoy.cs`

**Responsabilidad:** Colaborador de `AgentExpedition` que implementa `IExpeditionTask`. Provoca rivales para distraerlos de aliados. Máquina de 3 pasos: Approach (navega a rival o posta), Taunt (se detiene, mira, emote Molesto), Flee (corre away+hacia home, DecoyFleeSeconds). Cooldown post-huida (DecoyCooldown) evita spam. Si rival se pierde, retorna a posta a buscar otro. Prioriza peleadores sobre cualquier rival.

**Máquina de pasos:**
- `Approach` — navega a rival (HuntRepathInterval) hasta DecoyRange, o HoldAtPost si sin rival; busca nuevo cada HuntRepathInterval
- `Taunt` — se detiene, mira rival, TauntSeconds; emote Molesto
- `Flee` — corre away de rival + direction a home, DecoyFleeSeconds

**Propiedades internas:**
- `prey` — rival provocado
- `post` — post de espera (cuando sin rival)
- `step` — Approach/Taunt/Flee
- `cooldownUntil` — bloquea TryEngage
- `idle` — segundos ociosos en post

**Métodos público:**
- `bool TryEngage(ExpeditionRulesSO rules)` → bool — retorna false si en cooldown
- `bool Tick(ExpeditionRulesSO rules)` → bool
- `Cancel()` — borra prey, post, step, idle
- `void ResetForReuse()` — completo reset (incluyendo cooldownUntil)
- `float IdleSeconds { get; }`
- `float Cooldown01 { get; }` — normalized [0,1] post-huida
- `CreatureIntent Intent` — Taunting (Approach/Taunt) o Fleeing
- `Transform TargetTransform` — prey o post

**Integración:**
- Ocupación `Decoy` → prueba DecoyTryEngage() antes que fallback a Gatherer
- FindDecoyTarget() prioriza peleadores (Guard/Break) sobre cualquier rival
- No persiste rival tras golpeo (Cancel)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[IExpeditionTask]], [[AgentExpedition]], [[ExpeditionNav]], [[AgentContext]]
