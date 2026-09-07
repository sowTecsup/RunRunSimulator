---
tags: [script, world, ai, expedition, internal]
---

# AgentClash.cs

**Ruta:** `World/AI/AgentClash.cs`

**Responsabilidad:** Máquina de estados interna de choque/combate físico. Maneja ciclo: enganche automático (TryEngage con cooldown y boldness), combate manual (ForceMove dev), fases (Anticipating, Striking, Resolving, Dazed), impacto en rivales, knockback, chain immunity. S103: contadores hitsLanded/timesKnocked. S104: Break solo golpea recolectores sin custodio; Contact.Fight salta MinBoldness; Cooldown01 property.

**Estados internos:**
- None, Anticipating, Striking, Resolving, Dazed

**Métodos públicos:**
- `bool TryEngage() → bool` — intenta choque automático. Chequea: cooldown, (Contact!=Fight AND Boldness<MinBoldness), ocupación permite (Guard/Break sí, Gather/Decoy/Explore no). Break prioriza presa sin custodio. Retorna false si no encuentra rival en EngageRange
- `bool ForceMove(ClashMoveSO move, MoriMochiAgent rival) → bool` — fuerza movimiento (dev tools); solicita ReleaseStation y Roam primero
- `void TickClashing()` — avanza fase cada frame (Anticipating face→Striking seek impact→Resolving→Dazed decision)
- `void TickAirborne()` — detecta impacto si vuela (Wings dive, y<0.5); Impact + diving=false
- `void ReceiveHit(MoriMochiAgent attacker)` — golpeado, activa chain immunity (ChainImmunitySeconds), timesKnocked++
- `bool IsTargetable { get; }` — si no dazed Y time >= targetableAt (gracia post-knockback)
- `bool IgnoresChainKnock(MoriMochiAgent other) → bool` — immune a 2do golpe en cadena
- `void Cancel()` — aborta choque sin cambiar cooldown
- `void OnRecovered()` — post-ragdoll: si dazed, Decides; sino Roam. Si diving: cooldown largo
- `void ResetForReuse()` — limpia (pooling)

**Propiedades internas:**
- `target`, `move`, `phase` — estado vivo
- `hitsLanded` (int) — conteo de golpes exitosos (S103)
- `timesKnocked` (int) — conteo de veces derribado (S103)
- `cooldownUntil` — timestamp de fin de cooldown
- `diving` — si en dive animation
- `lastAttacker` — para chain immunity
- `chainImmuneUntil` — timestamp de fin de chain immunity
- `targetableAt` — timestamp de fin de gracia post-knockback

**S103 Cambios:**
- `hitsLanded`, `timesKnocked` contadores
- `TryEngage()` rechaza Explore (scouts no chocan automáticamente)

**S104 Cambios:**
- Break: solo golpea rivals sin `TrustedGuardian` (custodia = defensivo)
- Contact.Fight: ignora MinBoldness check (Enfrentar está desbloqueado por personalidad)
- Cooldown01: `float Cooldown01 { get; }` — normalized [0,1] cooldown post-choque (S104 NUEVO)

**Gating por Ocupación:**
- Guard → puede chocar (defiende puesto)
- Break → puede chocar (ofensivo), pero solo desprotegidos
- Gather/Decoy/Explore → no inicia automático

**Integración:**
- Llamado desde MoriMochiAgent.Update() en AgentState.Expedition (prioridad sobre expedición)
- Si TryEngage ok: expedition.Cancel()

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[AgentPhysics]], [[AgentExpedition]], [[ClashTuningSO]], [[ClashMoveSO]], [[ArenaOrders]]
