---
tags: [script, world, ai, expedition, internal]
---

# AgentAbilities.cs

**Ruta:** `World/AI/AgentAbilities.cs` (clase interna)

**Responsabilidad:** Gestor de 3 slots de habilidades (Damage, Mobility) por agente. Sincroniza asignación por DNA/parte (Horn/Wings/Back), cooldowns, disparo automático (Damage por RivalInReach), tick de buffs de movilidad (Fleeing/Chasing).

**Estructura:**
- 3 slots internos (struct Slot: AbilitySO, ReadyAt, FiredAt)

**Métodos Internos:**

- `AgentAbilities(MoriMochiAgent owner, AgentContext ctx)` — constructor; inicializa slots con FiredAt = -1

- `Bind(AbilitySO[] set)` — asigna array de 3 habilidades (resuelto por AbilityDatabaseSO.Resolve() en ArenaSandbox):
  - set[0] → Horn, set[1] → Wings, set[2] → Back
  - Resetea ReadyAt = 0, FiredAt = -1

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

- `TickMobility()` — activa buffs de movilidad:
  - Detecta Fleeing/Retreating o Chasing/Hunting
  - Recorre slots buscando AbilityKind.Mobility con Trigger relevante
  - Si listo y debe disparar: ctx.SpeedMultiplier = ability.SpeedMultiplier, ctx.SpeedBoostUntil = now + BoostSeconds
  - Sets FiredAt y ReadyAt (cooldown)

- `ResetForReuse()` — limpia para pooling: ReadyAt = 0, FiredAt = -1

**Estado Interno:**
- `owner` (MoriMochiAgent) — referencia al agente
- `ctx` (AgentContext) — contexto de navegación (SpeedMultiplier, SpeedBoostUntil)
- `slots` (Slot[3]) — array de habilidades

**Integración:**
- Instanciado en MoriMochiAgent.Awake()
- Bind() llamado desde ArenaSandbox.SpawnAgent() tras Initialize()
- TryFireDamage() llamado desde AgentClash.TryEngage()
- TickMobility() llamado cada frame desde MoriMochiAgent si en Expedition
- ResetForReuse() llamado por ControllerPool al devolver a pool

**S107 (NUEVO):**
- Extrae lógica de habilidades del monolito de MoriMochiAgent
- Maneja cooldowns, disparo automático y buffs en un solo lugar
- Facilita testing y extensión futura de sistema de abilities

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[AgentClash]], [[AbilitySO]], [[AbilityDatabaseSO]], [[ArenaSandbox]]
