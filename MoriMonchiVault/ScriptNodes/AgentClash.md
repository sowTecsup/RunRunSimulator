---
tags: [script, world, ai, agent, internal, expedition]
---

# AgentClash.cs

**Ruta:** `World/AI/AgentClash.cs`

**Responsabilidad:** Máquina de estados interna de choque/combate físico. Maneja ciclo: enganche automático (TryEngage), combate manual (ForceMove), fases (Anticipating, Striking, Resolving, Dazed), impacto en rivales, knockback, cooldowns. **S103:** Campos `hitsLanded`, `timesKnocked` contadores (exportados a MoriMochiAgent para stats). Explora gating: rechaza Explore en TryEngage (scouts no chocan automáticamente).

**Estados internos:**
- None, Anticipating, Striking, Resolving, Dazed

**Métodos públicos:**
- `TryEngage() → bool` — intenta choque automático (cooldown, Boldness, dentro de rango). **S103:** rechaza Gather, Decoy, Explore
- `ForceMove(ClashMoveSO move, MoriMochiAgent rival) → bool` — fuerza movimiento (dev tools)
- `TickClashing()` — avanza fase cada frame (Anticipating → Striking → Resolving → Dazed)
- `TickAirborne()` — detecta impacto si vuela (Wings dive)
- `ReceiveHit(MoriMochiAgent attacker)` — golpeado, activa chain immunity
- `bool IsTargetable { get; }` — si no dazed y gracia vencida
- `IgnoresChainKnock(MoriMochiAgent other) → bool` — immune a 2do golpe en cadena
- `Cancel()` — aborta choque
- `ResetForReuse()` — limpia (pooling)

**Propiedades Internas:**
- `hitsLanded` (int) — conteo de golpes exitosos (S103 NUEVO)
- `timesKnocked` (int) — conteo de veces derribado (S103 NUEVO)
- `target`, `move`, `phase`, `diving`, `knockedByClash`, `lastAttacker` — estado vivo

**S103 Cambios:**
- `hitsLanded`, `timesKnocked` contadores públicos (exposición a MoriMochiAgent para stats)
- En `ReceiveHit()`: si golpeado, `timesKnocked++`
- En `StartStrike()` o al impactar: `hitsLanded++`
- `TryEngage()` rechaza `Occupation.Explore` (scouts no inician choques)

**Gating por Ocupación (S103):**
```
Gather → no inicia choque
Guard → puede chocar (defiende puesto)
Break → puede chocar (ofensivo)
Decoy → no inicia choque (solo taunting)
Explore → no inicia choque (scouts solo reportan)
```

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[AgentPhysics]], [[ClashTuningSO]], [[ClashMoveSO]], [[Occupation]]
