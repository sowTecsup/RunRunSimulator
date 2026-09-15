---
tags: [script, world, ai, expedition, internal]
---

# AgentClash.cs

**Ruta:** `World/AI/AgentClash.cs`

**Responsabilidad:** Máquina de estados de choque/combate físico. Maneja ciclo: enganche automático (TryEngage con cooldown y boldness), combate manual (ForceMove dev), fases (Anticipating, Holding, Striking, Resolving, Dazed), delegación de ejecución física a ClashStrike, impacto sobre rivales, knockback, chain immunity. S103: contadores hitsLanded/timesKnocked. S104: Break solo golpea recolectores sin custodio; Contact.Fight salta MinBoldness. S107: TryEngage delega a AgentAbilities.TryFireDamage(). S108+: Impactpoint mantenido en cada fase. **S116:** delegación física a ClashStrike (strike.Begin/Tick/TickAirborne/Cancel); fachadas HitAt/HitPoint expuestas al público.

**Estados internos:**
- None, Anticipating, Holding, Striking, Resolving, Dazed

**Métodos públicos:**

- `bool TryEngage() → bool` — intenta choque automático. Chequea: cooldown, boldness (Contact!=Fight ó MinBoldness), ocupación permite (Guard/Break sí, Gather/Decoy/Explore no). S107: Delega a `abilities.TryFireDamage(dist, rivalsNearby, allowBack)` para obtener ClashMoveSO. Break prioriza presa sin custodio. Retorna false si no encuentra rival en EngageRange o si abilities retorna null.

- `bool ForceMove(ClashMoveSO move, MoriMochiAgent rival) → bool` — fuerza movimiento (dev tools); solicita ReleaseStation y Roam primero

- `void TickClashing()` — avanza fase cada frame (Anticipating → Holding → Striking seek impact → Resolving → Dazed decision)

- `void TickAirborne()` — **S116:** delega a `strike.TickAirborne()` que detecta ápice en vuelo (Wings: velocidad.y ≤ 0.05 y altura > riseFromY + 0.3); luego detecta impacto (y ≤ impactPoint.y + 0.4 O distancia planar ≤ 0.5); llama Land

- `void ReceiveHit(MoriMochiAgent attacker)` — golpeado, activa chain immunity (ChainImmunitySeconds), timesKnocked++

- `bool IsTargetable { get; }` — si no dazed Y time >= targetableAt (gracia post-knockback)

- `bool IgnoresChainKnock(MoriMochiAgent other) → bool` — immune a 2do golpe en cadena

- `void Cancel()` — aborta choque sin cambiar cooldown; delega a `strike.Cancel()`

- `void OnRecovered()` — post-ragdoll: si dazed, Decides; sino Roam. Si diving: cooldown largo

- `void ResetForReuse()` — limpia (pooling); delega a `strike.ResetForReuse()`

**Fachadas Públicas de Solo Lectura:**

- `ClashMoveSO Move { get; }` — movimiento actual (null si phase == None)
- `bool Telegraphing { get; }` — true si Anticipating/Holding, o Striking salvo que sea Wings y !diving
- `float Tell01 { get; }` — 0→1 durante Anticipating+Holding; 1 en Striking/Resolving; 0 en None/Dazed
- `Vector3 ImpactPoint { get; }` — punto de impacto calculado, actualizado cada frame (delegado a strike si Striking)
- **`float HitAt { get; }`** (S116) — timestamp del último impacto exitoso (-1 si nunca) — delegado a strike.HitAt
- **`Vector3 HitPoint { get; }`** (S116) — posición del rival en el que se conectó el golpe — delegado a strike.HitPoint

**Propiedades internas:**

- `target`, `move`, `phase` — estado vivo
- `lockedForward` (Vector3) — dirección del atacante bloqueada al final de Anticipating
- `impactPoint` (Vector3) — punto de impacto actualizado en cada fase (delegado a strike en Striking)
  - **S116 Horn:** `ctx.Body.position + dir * move.Range` (pies + dir·alcance)
  - **S116 Wings:** calculado en EnterHolding, fijo hasta impacto
  - **S116 Back:** posición actual del atacante
- `hitsLanded` (int) — conteo de golpes exitosos (delegado a strike)
- `timesKnocked` (int) — conteo de veces derribado
- **`strike`** (ClashStrike, S116) — ejecutor de movimiento físico
- `cooldownUntil` — timestamp de fin de cooldown
- `diving` — si en dive animation (S116: parte de strike.Diving)
- `riseFromY` (float, S116) — Y inicial de despegue para detectar ápice
- `lastAttacker` — para chain immunity
- `chainImmuneUntil` — timestamp de fin de chain immunity
- `targetableAt` — timestamp de fin de gracia post-knockback
- `phaseTimer` — contador de fase

## S116 Cambios

**Arquitectura nueva: Delegación a ClashStrike**

- **Instanciación:** strike creado en Awake
- **Begin fase Holding:** lanza `strike.Begin(move, target, lockedForward, impactPoint, phaseTimer)`
- **Fase Striking:** llama `strike.Tick(dt)` cada frame, retorna true si terminó
- **Wings TickAirborne:** delega a `strike.TickAirborne()`
- **Cancel/ResetForReuse:** delega a strike

**Horn:**
- Anticipating: `impactPoint = ctx.Body.position + dir * move.Range` (pies + dirección × alcance)
- Holding: dirección bloqueada, impactPoint sin cambios
- Striking: búsqueda radial desde posición del atacante (delegada a strike)
- Visualización: disco a impactPoint con radio HitRadius

**Wings (Picada):**
- Anticipating: calcula impactPoint rival (con lead acotado)
- EnterHolding: **NUEVO** despeja vertical `velocity = Vector3.up * riseSpeed`, `damping = 0`, posición fija en `riseFromY`
- TickAirborne: **NUEVO** delega a strike que espera ápice, luego caída
- StartStrike: **NUEVO** fija velocidad exacta `v = (d - 0.5*g*T²) / T` para caer en `DiveSeconds` segundos
- Land: **NUEVO** zonal con `QueryInRadius(impactPoint, move.HitRadius)`
- Visualización: disco en impactPoint con radio HitRadius (sin arco parabólico)

**Back:**
- Striking: búsqueda zonal desde posición actual del atacante (delegada a strike)

**Damping:**
- EnterHolding (Wings): `ctx.Rb.linearDamping = 0f` (sin fricción en vuelo)
- Land (Wings): `ctx.Rb.linearDamping = owner.thrownLinearDamping` (restaurado)

**HitAt/HitPoint (S116):**
- Inicializados en ResetForReuse: delegados a strike.ResetForReuse()
- Actualizados en Impact(): delegados a ClashStrike.Impact()
- Expuestos como propiedades readonly para telegrafía visual

## Ciclo de timing

1. **Anticipating:** `AnticipationSeconds`, Tell01 sube 0→1, lockedForward se fija, impactPoint calculado
2. **Holding:** `HoldSeconds` (embestida recta Horn, despegue vertical Wings, bloqueo Back)
3. **Striking:** `StrikeSeconds` (búsqueda radial Horn/Back, caída Wings) — delegada a strike
4. **Resolving:** knockback y efectos
5. **Dazed:** gracia post-golpe

## Gating por Ocupación

- Guard → puede chocar (defiende puesto)
- Break → puede chocar (ofensivo), pero solo desprotegidos
- Gather/Decoy/Explore → no inicia automático

## Integración

- Llamado desde MoriMochiAgent.Update() en AgentState.Expedition (prioridad sobre expedición)
- Si TryEngage ok: expedition.Cancel()
- Holding es visible en telegraph desde CreatureCueDrawer.Telegraph()
- HitAt/HitPoint leídos por ArenaCueOverlay para ImpactRing post-golpe

**Vinculado a:** [[Index/20 - MVP Combate]], [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion]], S116

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[AgentPhysics]], [[AgentExpedition]], [[AgentAbilities]], [[ClashTuningSO]], [[ClashMoveSO]], [[AbilitySO]], [[ArenaOrders]], [[CreatureCueDrawer]], [[ArenaCueOverlay]], [[ClashStrike]]
