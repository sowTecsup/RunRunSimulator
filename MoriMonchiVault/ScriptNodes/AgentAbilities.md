---
tags: [script, world, ai, expedition, internal]
---

# AgentAbilities.cs

**Ruta:** `World/AI/AgentAbilities.cs` (clase interna)

**Responsabilidad:** Gestor de 3 slots de habilidades (Damage Basic/Super, Mobility, Passive) por agente. Sincroniza asignación por DNA/parte (Horn/Wings/Back), cooldowns, disparo automático (Damage por RivalInReach), tick de buffs de movilidad (Fleeing/Chasing), carga de Super abilities (Hit/Mined/Secured). Integra estadísticas pasivas en ExpeditionStats via `RefreshStats()`. **S118:** agregadas super abilities con carga acumulable, pedido manual del jugador (Request), auto-fire con delay configurables, fachadas HasRequestedSuper, Charge01 para UI.

**Estructura:**
- 3 slots internos (struct Slot: AbilitySO, ReadyAt, FiredAt, Charge, Requested, ReadySince)
- Flags globales: ManualSupers (true = pedido obligatorio), AutoFireDelay (5s default)

**Métodos Internos:**

- `AgentAbilities(MoriMochiAgent owner, AgentContext ctx)` — constructor; inicializa slots con FiredAt = -1, ReadySince = -1

- `Bind(AbilitySO[] set)` — asigna array de 3 habilidades (resuelto por AbilityDatabaseSO.Resolve() en ArenaSandbox):
  - set[0] → Horn, set[1] → Wings, set[2] → Back
  - Resetea ReadyAt = 0, FiredAt = -1, Charge = 0, Requested = false, ReadySince = -1
  - Llama RefreshStats() para resolver ExpeditionStats (Damage/Mobility/Passive)

- `RefreshStats()` — resuelve ctx.Stats:
  - Llama `ExpeditionStats.Resolve(ExpeditionRulesSO.Current, ctx.Occupation, [slots[0].Ability, ...])`
  - Integra stats pasivas (CarryCapacity, LoadedSpeedFactor, KeepCarryOnKnock, GuardRadius, VisibleFrom)
  - Llamado en Bind() y ResetForReuse()

- `int Count { get; }` — retorna 3 (constante)

- `AbilitySO Ability(int i) → AbilitySO` — getter slot i o null si out of bounds

- `bool HasRequestedSuper { get; }` — **S118 NUEVO:** retorna true si algún Super está Requested AND Charge >= 1.0

- `float Charge01(int i) → float` — **S118 ACTUALIZADO:** carga normalizada [0,1]:
  - Retorna 1f si ability == null, i inválido, o (Kind != Damage O Role != Super)
  - Si ReadyAt ya pasó (ability.Kind != Damage O Role != Super), retorna 1f
  - Sino Super: 1 - (ReadyAt - now) / Cooldown
  - Sino Super: clamp01(Charge), mostrando acumulación

- `float FiredAt(int i) → float` — timestamp último disparo (o -1 si nunca)

- `bool IsReady(int i)` — **S118 ACTUALIZADO:**
  - Si Super: Charge >= 1f
  - Si Basic/Mobility: Time.time >= ReadyAt

- `bool IsRequested(int i)` — **S118 NUEVO:** retorna slots[i].Requested

- `void Request(int i)` — **S118 NUEVO:** solicita disparo Super:
  - Filtra: i válido, ability != null, Kind == Damage, Role == Super, IsReady(i)
  - Si todo ok: slots[i].Requested = true

- `void AddCharge(AbilityChargeSource source)` — **S118 NUEVO:** acumula carga a Super abilities:
  - Itera slots buscando Kind == Damage, Role == Super
  - Suma ability.ChargeFor(source) a Charge (clamped 0-1)
  - Si Charge >= 1 por primera vez: ReadySince = Time.time (para AutoFireDelay)

- `ClashMoveSO TryFireDamage(float dist, int rivalsNearby, bool allowBack) → ClashMoveSO` — **S118 ACTUALIZADO:** disparo automático Damage en combate:
  - **Fase 1 - Super**: itera slots buscando Role == Super
    - Filtra: Trigger RivalInReach, IsReady(), dist en rango, rivalsNearby >= minRivalsNearby, allowBack override
    - Prioridad: MinRivalsNearby > 0 > MinDistance > 0 > cualquier otro
    - Chequea canAutoFire = !ManualSupers O Requested O (ReadySince >= 0 AND time - ReadySince >= AutoFireDelay)
    - Si encuentra: resetea Charge = 0, Requested = false, ReadySince = -1, FiredAt = now, retorna Move
  - **Fase 2 - Basic**: itera slots buscando Role == Basic
    - Filtra: Trigger RivalInReach, Time.time >= ReadyAt, dist en rango, rivalsNearby >= minRivalsNearby, allowBack override
    - Prioridad: MinRivalsNearby > 0 > MinDistance > 0 > cualquier otro
    - Si encuentra: sets FiredAt = now, ReadyAt = now + Cooldown, retorna Move
  - Sino, retorna null

- `TickMobility()` — activa buffs de movilidad (Mobility solo, no Passive):
  - Detecta Fleeing/Retreating o Chasing/Hunting
  - Recorre slots buscando AbilityKind.Mobility con Trigger relevante
  - Si listo y debe disparar: ctx.SpeedMultiplier = ability.SpeedMultiplier, ctx.SpeedBoostUntil = now + BoostSeconds
  - Sets FiredAt y ReadyAt (cooldown)
  - Pasivas se resuelven en RefreshStats(), no en TickMobility

- `void ResetForReuse()` — limpia para pooling:
  - ReadyAt = 0, FiredAt = -1, Charge = 0, Requested = false, ReadySince = -1
  - Llama RefreshStats() para resetear stats pasivas

**Estado Interno:**
- `owner` (MoriMochiAgent) — referencia al agente
- `ctx` (AgentContext) — contexto de navegación (SpeedMultiplier, SpeedBoostUntil, Stats)
- `slots` (Slot[3]) — array de habilidades
- `ManualSupers` (bool) — si true, Super solo dispara si solicitado O tras AutoFireDelay
- `AutoFireDelay` (float) — segundos de espera tras carga para auto-disparar Super

**Integración:**

- Instanciado en MoriMochiAgent.Awake()
- Bind() llamado desde ArenaSandbox.SpawnAgent() tras Initialize(), antes de expedición
- RefreshStats() se invoca en Bind() y ResetForReuse() para resolver ExpeditionStats con habilidades pasivas
- TryFireDamage() llamado desde AgentClash.TryEngage()
- AddCharge() llamado desde ClashStrike.Impact() con AbilityChargeSource.Hit
- Request() llamado desde UI (RadialSlot click) cuando jugador toca power en ArenaHudCard
- TickMobility() llamado cada frame desde MoriMochiAgent si en Expedition o Idle/Roaming
- ResetForReuse() llamado por ControllerPool al devolver a pool

**S118 Cambios:**

- TryFireDamage(): fase 1 busca Super listos, aplica ManualSupers + AutoFireDelay logic, descarta Requested/Charge tras disparo
- AddCharge(): nueva responsabilidad, acumula desde Hit/Mined/Secured
- Request(): nueva fachada para UI (jugador solicita Super)
- HasRequestedSuper: fachada para HUD (saber si hay Super activo solicitado)
- Charge01: ahora retorna carga real para Supers, no normaliza ReadyAt
- IsReady: diferencia Super (Charge >= 1) de Basic (ReadyAt)

**Invariantes:**

- 3 slots = 3 partes (Horn, Wings, Back), siempre
- Abilities pueden ser null (si no hay habilidad para slot)
- Pasivas se resuelven en Bind/RefreshStats, no hay "cooldown" de pasiva
- Cooldowns de Basic/Mobility son independientes (cada uno mantiene ReadyAt)
- Super Charge [0,1], Requested solo true si IsReady, AutoFireDelay solo si ReadySince >= 0
- Stats pasivas inmutables hasta siguiente Bind/SetOrders (no cambian mid-expedición)
- ManualSupers = true desactiva auto-fire de Super salvo si Requested O tras AutoFireDelay

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], [[Index/22 - Bajada Nocturna y Linaje]], S118

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[AgentClash]], [[ClashStrike]], [[AbilitySO]], [[AbilityDatabaseSO]], [[ArenaSandbox]], [[ExpeditionStats]], [[ArenaHudCard]], [[RadialSlot]]
