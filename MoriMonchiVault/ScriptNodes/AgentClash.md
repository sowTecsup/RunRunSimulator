---
tags: [script, world, ai, expedition, internal]
---

# AgentClash.cs

**Ruta:** `World/AI/AgentClash.cs`

**Responsabilidad:** Máquina de estados interna de choque/combate físico. Maneja ciclo: enganche automático (TryEngage con cooldown y boldness), combate manual (ForceMove dev), fases (Anticipating, Holding, Striking, Resolving, Dazed), impacto en rivales, knockback, chain immunity. S103: contadores hitsLanded/timesKnocked. S104: Break solo golpea recolectores sin custodio; Contact.Fight salta MinBoldness; Cooldown01 property. S107: TryEngage delega a AgentAbilities.TryFireDamage() para elegir movimiento de habilidad por distancia/rivals cercanos. S108: Mantiene `impactPoint` actualizado en cada fase, expone 4 fachadas de solo lectura: `Move`, `Telegraphing`, `Tell01`, `ImpactPoint`. **S114:** fase Holding: embestida recta multi-golpe con dirección bloqueada, lead acotado en picada.

**Estados internos:**
- None, Anticipating, Holding (S114), Striking, Resolving, Dazed

**Métodos públicos:**
- `bool TryEngage() → bool` — intenta choque automático. Chequea: cooldown, boldness (Contact!=Fight ó MinBoldness), ocupación permite (Guard/Break sí, Gather/Decoy/Explore no). S107: Delega a `abilities.TryFireDamage(dist, rivalsNearby, allowBack)` para obtener ClashMoveSO. Break prioriza presa sin custodio. Retorna false si no encuentra rival en EngageRange o si abilities retorna null.
- `bool ForceMove(ClashMoveSO move, MoriMochiAgent rival) → bool` — fuerza movimiento (dev tools); solicita ReleaseStation y Roam primero
- `void TickClashing()` — avanza fase cada frame (Anticipating → Holding (S114) → Striking seek impact → Resolving → Dazed decision)
- `void TickAirborne()` — detecta impacto si vuela (Wings dive, y<0.5); Impact + diving=false
- `void ReceiveHit(MoriMochiAgent attacker)` — golpeado, activa chain immunity (ChainImmunitySeconds), timesKnocked++
- `bool IsTargetable { get; }` — si no dazed Y time >= targetableAt (gracia post-knockback)
- `bool IgnoresChainKnock(MoriMochiAgent other) → bool` — immune a 2do golpe en cadena
- `void Cancel()` — aborta choque sin cambiar cooldown
- `void OnRecovered()` — post-ragdoll: si dazed, Decides; sino Roam. Si diving: cooldown largo
- `void ResetForReuse()` — limpia (pooling); reseta impactPoint

**Fachadas Públicas de Solo Lectura:**
- `ClashMoveSO Move { get; }` — movimiento actual (null si phase == None)
- `bool Telegraphing { get; }` — true si Anticipating/Holding (S114), o Striking salvo que sea Wings y !diving
- `float Tell01 { get; }` — 0→1 durante Anticipating+Holding; 1 en Striking/Resolving; 0 en None/Dazed (S114: amplía ventana)
- `Vector3 ImpactPoint { get; }` — punto de impacto calculado, actualizado cada frame (atacante para Horn/Back, rival para Wings)

**Propiedades internas:**
- `target`, `move`, `phase` — estado vivo
- `lockedForward` (Vector3, S114) — dirección del atacante bloqueada al final de Anticipating, usada en Holding para alineación
- `impactPoint` (Vector3) — punto de impacto actualizado en Anticipating (posición rival), Holding (S114: sin cambios), Striking (rival para Horn, cuerpo para Back, fijo en StartStrike para Wings), Resolving (sin cambios)
- `hitsLanded` (int) — conteo de golpes exitosos (S103)
- `timesKnocked` (int) — conteo de veces derribado (S103)
- `cooldownUntil` — timestamp de fin de cooldown
- `diving` — si en dive animation
- `lastAttacker` — para chain immunity
- `chainImmuneUntil` — timestamp de fin de chain immunity
- `targetableAt` — timestamp de fin de gracia post-knockback
- `phaseTimer` — contador de fase para anticipación/bloqueo/ataque

**Ciclo de timing S114**

1. **Anticipating:** `AnticipationSeconds`, Tell01 sube 0→1, `lockedForward` se fija al término
2. **Holding:** `HoldSeconds` (nuevo en S114), embestida recta multi-golpe con dirección bloqueada, lead acotado en picada
3. **Striking:** `StrikeSeconds`, impacto y resolución
4. **Resolving:** knockback y efectos
5. **Dazed:** gracia post-golpe

**S114 Cambios**

Fase Holding nueva:
- Duración: `ClashMoveSO.HoldSeconds`
- Horn: embestida recta a lo largo de `lockedForward` (multi-golpe contra una línea de rivales)
- Wings: picada con lead acotado (no gira en vuelo, impactPoint fijo anticipado)
- Back: bloqueo sin cambios (se ejecuta en Striking)
- Telegrafía: incluye Holding en ventana de Tell (Tell01 sigue 0→1 durante Anticipating+Holding)
- Visualización: telegraph con hold en CreatureCueDrawer y CueAnim Hold en ArenaCueOverlay

**Gating por Ocupación:**
- Guard → puede chocar (defiende puesto)
- Break → puede chocar (ofensivo), pero solo desprotegidos
- Gather/Decoy/Explore → no inicia automático

**Integración:**
- Llamado desde MoriMochiAgent.Update() en AgentState.Expedition (prioridad sobre expedición)
- Si TryEngage ok: expedition.Cancel()
- TryFireDamage() retorna ClashMoveSO que se asigna a move
- Holding es visible en telegraph desde CreatureCueDrawer.Telegraph(..., hold)

**Vinculado a:** [[Index/20 - MVP Combate]], [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], S114

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[AgentPhysics]], [[AgentExpedition]], [[AgentAbilities]], [[ClashTuningSO]], [[ClashMoveSO]], [[AbilitySO]], [[ArenaOrders]], [[CreatureCueDrawer]], [[ArenaCueOverlay]]
