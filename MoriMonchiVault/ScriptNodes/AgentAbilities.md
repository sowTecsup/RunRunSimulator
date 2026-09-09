---
tags: [script, world, ai, expedition, internal]
---

# AgentAbilities.cs

**Ruta:** `World/AI/AgentAbilities.cs` (clase interna)

**Responsabilidad:** Gestor de 3 slots de habilidades (Damage, Mobility, Passive) por agente. Sincroniza asignación por DNA/parte (Horn/Wings/Back), cooldowns, disparo automático (Damage por RivalInReach), tick de buffs de movilidad (Fleeing/Chasing). Integra estadísticas pasivas en ExpeditionStats via `RefreshStats()`.

**Estructura:**
- 3 slots internos (struct Slot: AbilitySO, ReadyAt, FiredAt)

**Métodos Internos:**

- `AgentAbilities(MoriMochiAgent owner, AgentContext ctx)` — constructor; inicializa slots con FiredAt = -1

- `Bind(AbilitySO[] set)` (S109 ACTUALIZADO) — asigna array de 3 habilidades (resuelto por AbilityDatabaseSO.Resolve() en ArenaSandbox):
  - set[0] → Horn, set[1] → Wings, set[2] → Back
  - Resetea ReadyAt = 0, FiredAt = -1
  - Llama RefreshStats() para resolver ExpeditionStats (Damage/Mobility/Passive)

- `RefreshStats()` (S109 NUEVO) — resuelve ctx.Stats:
  - Llama `ExpeditionStats.Resolve(ExpeditionRulesSO.Current, ctx.Occupation, [slots[0].Ability, ...])`
  - Integra stats pasivas (CarryCapacity, LoadedSpeedFactor, KeepCarryOnKnock, GuardRadius, VisibleFrom)
  - Llamado en Bind() y ResetForReuse()

- `int Count { get; }` — retorna 3 (constante)

- `AbilitySO Ability(int i) → AbilitySO` — getter slot i o null si out of bounds

- `float Charge01(int i) → float` — carga normalizada [0,1]:
  - Retorna 1f si ability == null, ReadyAt ya pasó, o i inválido
  - Sino, 1 - (ReadyAt - now) / Cooldown

- `float FiredAt(int i) → float` — timestamp último disparo (o -1 si nunca)

- `ClashMoveSO TryFireDamage(float dist, int rivalsNearby, bool allowBack) → ClashMoveSO` — disparo automático Damage en combate:
  - Itera slots buscando AbilityKind.Damage con Move != null
  - Filtra: Trigger debe incluir RivalInReach, ReadyAt ya pasó, dist en rango, rivalsNearby >= minRivalsNearby, allowBack override
  - Prioridad: MinRivalsNearby > 0 > MinDistance > 0 > cualquier otro
  - Si encuentra, sets ReadyAt = now + Cooldown, FiredAt = now, retorna Move
  - Sino, retorna null

- `TickMobility()` (S109 ACTUALIZADO) — activa buffs de movilidad (Damage + Mobility solo, no Passive):
  - Detecta Fleeing/Retreating o Chasing/Hunting
  - Recorre slots buscando AbilityKind.Mobility con Trigger relevante
  - Si listo y debe disparar: ctx.SpeedMultiplier = ability.SpeedMultiplier, ctx.SpeedBoostUntil = now + BoostSeconds
  - Sets FiredAt y ReadyAt (cooldown)
  - Pasivas se resuelven en RefreshStats(), no en TickMobility

- `ResetForReuse()` (S109 ACTUALIZADO) — limpia para pooling:
  - ReadyAt = 0, FiredAt = -1
  - Llama RefreshStats() para resetear stats pasivas

**Estado Interno:**
- `owner` (MoriMochiAgent) — referencia al agente
- `ctx` (AgentContext) — contexto de navegación (SpeedMultiplier, SpeedBoostUntil, Stats)
- `slots` (Slot[3]) — array de habilidades

**Integración:**

- Instanciado en MoriMochiAgent.Awake()
- Bind() llamado desde ArenaSandbox.SpawnAgent() tras Initialize(), antes de expedición
- RefreshStats() se invoca en Bind() y ResetForReuse() para resolver ExpeditionStats con habilidades pasivas
- TryFireDamage() llamado desde AgentClash.TryEngage()
- TickMobility() llamado cada frame desde MoriMochiAgent si en Expedition o Idle/Roaming
- ResetForReuse() llamado por ControllerPool al devolver a pool

**S109 Cambios:**

- Bind() + RefreshStats(): integra pasivas (Passive kind) en ExpeditionStats.Resolve()
- TickMobility() afecta solo Mobility (no Passive); pasivas son estáticas via ctx.Stats
- CarryCapacity, velocidad cargado, resistencia a golpes, visibilidad se resuelven dinámicamente
- ResetForReuse() resetea stats pasivas al recliclar agente
- MoriMochiAgent.SetOrders() también invoca RefreshStats() para re-resolver stats si cambió ocupación

**Invariantes:**

- 3 slots = 3 partes (Horn, Wings, Back), siempre
- Abilities pueden ser null (si no hay habilidad para slot)
- Pasivas se resuelven en Bind/RefreshStats, no hay "cooldown" de pasiva
- Cooldowns de Damage/Mobility son independientes (cada uno mantiene ReadyAt)
- Stats pasivas inmutables hasta siguiente Bind/SetOrders (no cambian mid-expedición)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[AgentClash]], [[AbilitySO]], [[AbilityDatabaseSO]], [[ArenaSandbox]], [[ExpeditionStats]]
