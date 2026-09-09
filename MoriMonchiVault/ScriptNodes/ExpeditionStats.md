---
tags: [script, data, struct, expedition]
---

# ExpeditionStats.cs

**Ruta:** `Data/Expedition/ExpeditionStats.cs`

**Responsabilidad:** Struct puro que define estadísticas operacionales de un agente en expedición. Contiene capacidad de carga, velocidad cargado, resistencia a golpes, radio de guarda y visibilidad. Se resuelve dinámicamente mediante `Resolve()` leyendo ocupación y habilidades del agente, con reglas de prioridad para overrides. Dueño del dato: `AgentContext.Stats`.

**Estructura:**

**Campos Públicos:**
- `CarryCapacity` (int) — máximo de unidades a recolectar (3 para Gather/Explore, 2 para Break/Decoy)
- `LoadedSpeedFactor` (float) — multiplicador de velocidad cuando cargado (≥ 1f, ej. 0.85 = -15%)
- `KeepCarryOnKnock` (bool) — si true, no suelta carga al ser golpeado
- `GuardRadius` (float) — radio de custodio (ej. 4m desde ExpeditionRulesSO.GuardRadius)
- `VisibleFrom` (float) — distancia de visibilidad del agente para rivalidades (ej. 8m)

**Métodos Estáticos:**

- `static ExpeditionStats Default { get; }` — valores por defecto:
  - CarryCapacity = 3, LoadedSpeedFactor = 1f, KeepCarryOnKnock = false
  - GuardRadius = 4f, VisibleFrom = 0f

- `static ExpeditionStats Resolve(ExpeditionRulesSO rules, Occupation occupation, AbilitySO[] abilities)` — resuelve estadísticas finales:
  1. Comienza con `Default`
  2. Si `rules != null`:
     - CarryCapacity = (occupation == Gather || Explore) ? rules.CarryCapacity : rules.SupportCarryCapacity
     - GuardRadius = rules.GuardRadius
  3. Itera `abilities` (Horn, Wings, Back):
     - Si ability.CarryCapacity > 0 y (sin override previo OR < bestCarryCapacity anterior) → guarda como override (gana el menor)
     - LoadedSpeedFactor *= ability.LoadedSpeedFactor (si > 0)
     - KeepCarryOnKnock |= ability.KeepCarryOnKnock (OR lógico)
     - GuardRadius = ability.GuardRadius (si > 0, último gana)
     - VisibleFrom = max(VisibleFrom, ability.VisibleFrom)
  4. Si hay override de CarryCapacity por habilidad, aplica el menor valor

**Integración:**

- Creado en: `AgentAbilities.RefreshStats()` (Bind, ResetForReuse) y `MoriMochiAgent.SetOrders()`
- Almacenado en: `AgentContext.Stats`
- Leído por:
  - `AgentGatherer.TryEngage()` — cierra si ctx.Stats.CarryCapacity alcanzado
  - `AgentGatherer.OnKnocked()` — suelta carga solo si !ctx.Stats.KeepCarryOnKnock
  - `ExpeditionNav.GuardPoint/HoldAtPost` — usa ctx.Stats.GuardRadius para radio
  - `AgentContext.ApplyGaitSpeed()` — multiplica por LoadedSpeedFactor si cargado
  - `Perceivable.NoticeRadius` — retorna ctx.Stats.VisibleFrom
  - `PerceivableRegistry.QueryInRadius()` — acepta agente si SqrDistance ≤ max(radius², NoticeRadius²)

**Invariantes S109:**

- **Determinismo:** Resolve() es puro; misma entrada (rules, occupation, abilities) → misma salida
- **Prioridad de carry:** habilidades pueden reducir capacidad (break reduce a 2); entre dos overrides, gana el menor (costo estricto)
- **Acumulativos:** LoadedSpeedFactor y VisibleFrom se combinan (producto y max respectivamente)
- **Ocupación define base:** Gather/Explore ≠ Break/Decoy en capacidad base (2 vs 3)
- **Stats immutable en expedición:** resuelto al Bind() y SetOrders(); no cambia hasta siguiente reset

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[AgentContext]], [[AgentAbilities]], [[AbilitySO]], [[ExpeditionRulesSO]], [[AgentGatherer]], [[ExpeditionNav]], [[Perceivable]], [[PerceivableRegistry]]
