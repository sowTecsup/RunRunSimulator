---
tags: [script, world, ai, expedition, util]
---

# ExpeditionNav.cs

**Ruta:** `World/AI/ExpeditionNav.cs`

**Responsabilidad:** Librería estática de búsqueda y navegación para tareas de expedición. Localiza recursos (material, presas, aliados, tauntadores), computa puntos de aproximación/custodia/huida evitando aliados, y valida metas y rivales. Eje de cálculos geométricos para todas las ocupaciones. **S107:** Añade `IsRevealing()` para detectar intenciones que revelan rivales a HUD. **S109:** GuardPoint/HoldAtPost usan `ctx.Stats.GuardRadius` (dinámico por habilidades).

**Métodos públicos:**
- `bool Usable(MaterialPickup m)` — valida que no esté tomado ni desactivado
- `bool IsThreat(MoriMochiAgent rival)` — chequea si rival está activo y en modo ofensivo (Hunting, Guarding, Clashing, Taunting, Fighting)
- `MaterialPickup InjectedPost(AgentContext ctx)` — retorna GuardPost si es usable
- `MaterialPickup FindPost(AgentContext ctx, bool excludeLode = false)` — busca mejor post en percepts (más material restante, o más cercano)
- `bool IsThiefIntent(CreatureIntent intent)` — valida intenciones de carga (Taking, Carrying, Securing, Collecting)
- `bool IsRevealing(CreatureIntent intent)` — **S107 NUEVO** detecta si intención debe revelar rival: Clashing, Fighting, Dazed, Taking, Carrying, Securing, Losing. Usado por ArenaCueOverlay.LateUpdate() para control de reveal state.
- `MoriMochiAgent FindPrey(AgentContext ctx, MoriMochiAgent owner)` — busca recolector sin guardián custodio
- `MoriMochiAgent FindDecoyTarget(AgentContext ctx, MoriMochiAgent owner)` — busca rival con prioridad a peleadores
- `MoriMochiAgent NearestRival(AgentContext ctx, MoriMochiAgent owner, out float sqrDist)` — rival más cercano
- `MoriMochiAgent FighterAllyNear(AgentContext ctx, MoriMochiAgent owner, float maxDistance)` — aliado que pelea dentro de radio
- `MaterialPickup NearestDrop(AgentContext ctx, float maxDistance)` — material caído más cercano
- `MoriMochiAgent NearestTaunter(AgentContext ctx, MoriMochiAgent owner, float maxDistance)` — rival en intención Taunting
- `MoriMochiAgent NearestAlly(AgentContext ctx, MoriMochiAgent owner, float maxDistance, out float sqrDist)` — aliado más cercano
- `Vector3 ApproachPoint(AgentContext ctx, MoriMochiAgent owner, MaterialPickup target, ExpeditionRulesSO rules)` — punto a nivel rim, separado de otros recolectores (evita overlap)
- **`Vector3 GuardPoint(AgentContext ctx, MaterialPickup post, ExpeditionRulesSO rules)`** (S109 ACTUALIZADO) — punto de custodia entre post y salida; usa `ctx.Stats.GuardRadius` para radio (en lugar de ExpeditionRulesSO.GuardRadius fijo)
- `void FaceToward(AgentContext ctx, Vector3 point, float dt)` — rota smoothly hacia punto
- **`bool HoldAtPost(AgentContext ctx, MaterialPickup post, ExpeditionRulesSO rules, ref float repathTimer, float dt)`** (S109 ACTUALIZADO) — mantiene guardián en radio del post usando `ctx.Stats.GuardRadius`; retorna true si llegó
- `Vector3 FleePoint(AgentContext ctx, Vector3 threat, Vector3 pull, bool hasPull, float distance)` — punto de huida (away from threat, pulled toward home/ally)

**Internals:**
- Cálculos de ángulos, distancias planares, muestreo de NavMesh
- Separación angular para evitar que múltiples recolectores ocupen el mismo rim

**S107 Cambios:**
- `IsRevealing()` método público nuevo
- Usado por ArenaCueOverlay para determinar si rival debe ser revelado en HUD
- Intenciones que "ponen en evidencia" el agente: activamente robando, luchando, o perdiendo

**S109 Cambios:**

- `GuardPoint()` (S109): ahora lee `ctx.Stats.GuardRadius` en lugar de valor fijo de rules
  - Permite que habilidades pasivas aumenten/disminuyan radio de custodia
  - Ejemplo: habilidad "Vigilancia Extendida" suma +2m al radio
- `HoldAtPost()` (S109): internamente usa `ctx.Stats.GuardRadius` para manter guardián dentro de radio
  - Si KeepCarryOnKnock está activo, guardián no pierde posición incluso tras golpe
- Ambos métodos ahora integran estadísticas dinámicas resueltas en ExpeditionStats.Resolve()

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[AgentGatherer]], [[AgentGuard]], [[AgentHunter]], [[AgentDecoy]], [[AgentScout]], [[AgentContext]], [[TeamBlackboard]], [[ArenaCueOverlay]], [[ExpeditionStats]]
