---
tags: [script, world, ai, expedition, internal]
---

# AgentClash.cs

**Ruta:** `World/AI/AgentClash.cs`

**Responsabilidad:** Máquina de estados interna de choque/combate físico. Maneja ciclo: enganche automático (TryEngage con cooldown y boldness), combate manual (ForceMove dev), fases (Anticipating, Striking, Resolving, Dazed), impacto en rivales, knockback, chain immunity. S103: contadores hitsLanded/timesKnocked. S104: Break solo golpea recolectores sin custodio; Contact.Fight salta MinBoldness; Cooldown01 property. S107: TryEngage delega a AgentAbilities.TryFireDamage() para elegir movimiento de habilidad por distancia/rivals cercanos. **S108:** Mantiene `impactPoint` actualizado en cada fase, expone 4 fachadas de solo lectura: `Move`, `Telegraphing`, `Tell01`, `ImpactPoint` (leídas por CreatureCueDrawer.Telegraph para dibujar plantilla de área de impacto).

**Estados internos:**
- None, Anticipating, Striking, Resolving, Dazed

**Métodos públicos:**
- `bool TryEngage() → bool` — intenta choque automático. Chequea: cooldown, boldness (Contact!=Fight ó MinBoldness), ocupación permite (Guard/Break sí, Gather/Decoy/Explore no). S107: Delega a `abilities.TryFireDamage(dist, rivalsNearby, allowBack)` para obtener ClashMoveSO. Break prioriza presa sin custodio. Retorna false si no encuentra rival en EngageRange o si abilities retorna null.
- `bool ForceMove(ClashMoveSO move, MoriMochiAgent rival) → bool` — fuerza movimiento (dev tools); solicita ReleaseStation y Roam primero
- `void TickClashing()` — avanza fase cada frame (Anticipating face→Striking seek impact→Resolving→Dazed decision)
- `void TickAirborne()` — detecta impacto si vuela (Wings dive, y<0.5); Impact + diving=false
- `void ReceiveHit(MoriMochiAgent attacker)` — golpeado, activa chain immunity (ChainImmunitySeconds), timesKnocked++
- `bool IsTargetable { get; }` — si no dazed Y time >= targetableAt (gracia post-knockback)
- `bool IgnoresChainKnock(MoriMochiAgent other) → bool` — immune a 2do golpe en cadena
- `void Cancel()` — aborta choque sin cambiar cooldown
- `void OnRecovered()` — post-ragdoll: si dazed, Decides; sino Roam. Si diving: cooldown largo
- `void ResetForReuse()` — limpia (pooling); reseta impactPoint

**Fachadas Públicas de Solo Lectura (S108 NUEVAS):**
- `ClashMoveSO Move { get; }` — movimiento actual (null si phase == None)
- `bool Telegraphing { get; }` — true si Anticipating, o Striking salvo que sea Wings y !diving
- `float Tell01 { get; }` — 0→1 durante Anticipating; 1 en Striking/Resolving; 0 en None/Dazed
- `Vector3 ImpactPoint { get; }` — punto de impacto calculado, actualizado cada frame (atacante para Horn/Back, rival para Wings)

**Propiedades internas:**
- `target`, `move`, `phase` — estado vivo
- `impactPoint` (Vector3) — punto de impacto actualizado en Anticipating (posición rival), Striking (rival para Horn, cuerpo para Back, fijo en StartStrike para Wings), Resolving (sin cambios)
- `hitsLanded` (int) — conteo de golpes exitosos (S103)
- `timesKnocked` (int) — conteo de veces derribado (S103)
- `cooldownUntil` — timestamp de fin de cooldown
- `diving` — si en dive animation
- `lastAttacker` — para chain immunity
- `chainImmuneUntil` — timestamp de fin de chain immunity
- `targetableAt` — timestamp de fin de gracia post-knockback
- `phaseTimer` — contador de fase para anticipación/ataque

**S103 Cambios:**
- `hitsLanded`, `timesKnocked` contadores
- `TryEngage()` rechaza Explore (scouts no chocan automáticamente)

**S104 Cambios:**
- Break: solo golpea rivals sin `TrustedGuardian` (custodia = defensivo)
- Contact.Fight: ignora MinBoldness check (Enfrentar está desbloqueado por personalidad)
- Cooldown01: `float Cooldown01 { get; }` — normalized [0,1] cooldown post-choque

**S107 Cambios:**
- TryEngage() ahora llama `abilities.TryFireDamage(dist, rivalsNearby, allowBack)` en lugar de usar movimiento fijo
- Permite elegir habilidad de daño dinámicamente según:
  - Distancia al rival (MinDistance threshold)
  - Número de rivales cercanos (MinRivalsNearby threshold)
  - Disponibilidad de habilidad (Cooldown listo)
  - Permite que cada parte (Horn/Wings/Back) tenga habilidad única
- Si abilities.TryFireDamage() retorna null, TryEngage retorna false (no hay habilidad disponible)

**S108 Cambios:**
- `impactPoint` (Vector3) nuevo campo privado:
  - Inicializado en Begin() a posición rival (o cuerpo si Back)
  - Actualizado en Anticipating: Horn → rival.position, Back → ctx.Body.position
  - Actualizado en Striking (Horn): rival.position cada frame; (Back): ctx.Body.position; (Wings): fijo en StartStrike (anticipado)
  - Resetea a cero en ResetForReuse()
- Cuatro fachadas públicas nuevas: Move, Telegraphing, Tell01, ImpactPoint
- Tell01 = 1 - (phaseTimer / AnticipationSeconds) durante Anticipating, 1 en Striking+Resolving, 0 en None/Dazed
- Telegraphing indica si se debe dibujar la plantilla en ArenaCueOverlay

**Gating por Ocupación:**
- Guard → puede chocar (defiende puesto)
- Break → puede chocar (ofensivo), pero solo desprotegidos
- Gather/Decoy/Explore → no inicia automático

**Integración:**
- Llamado desde MoriMochiAgent.Update() en AgentState.Expedition (prioridad sobre expedición)
- Si TryEngage ok: expedition.Cancel()
- TryFireDamage() retorna ClashMoveSO que se asigna a move

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[AgentPhysics]], [[AgentExpedition]], [[AgentAbilities]], [[ClashTuningSO]], [[ClashMoveSO]], [[AbilitySO]], [[ArenaOrders]], [[CreatureCueDrawer]]
