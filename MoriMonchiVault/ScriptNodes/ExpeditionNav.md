---
tags: [script, world, ai, expedition, util]
---

# ExpeditionNav.cs

**Ruta:** `World/AI/ExpeditionNav.cs`

**Responsabilidad:** Librería estática de búsqueda y navegación para tareas de expedición. Localiza recursos (material, presas, aliados, tauntadores), computa puntos de aproximación/custodia/huida evitando aliados, y valida metas y rivales. Eje de cálculos geométricos para todas las ocupaciones.

**Métodos públicos:**
- `bool Usable(MaterialPickup m)` — valida que no esté tomado ni desactivado
- `bool IsThreat(MoriMochiAgent rival)` — chequea si rival está activo y en modo ofensivo (Hunting, Guarding, Clashing, Taunting, Fighting)
- `MaterialPickup InjectedPost(AgentContext ctx)` — retorna GuardPost si es usable
- `MaterialPickup FindPost(AgentContext ctx, bool excludeLode = false)` — busca mejor post en percepts (más material restante, o más cercano)
- `bool IsThiefIntent(CreatureIntent intent)` — valida intenciones de carga (Taking, Carrying, Securing, Collecting)
- `MoriMochiAgent FindPrey(AgentContext ctx, MoriMochiAgent owner)` — busca recolector sin guardián custodio
- `MoriMochiAgent FindDecoyTarget(AgentContext ctx, MoriMochiAgent owner)` — busca rival con prioridad a peleadores
- `MoriMochiAgent NearestRival(AgentContext ctx, MoriMochiAgent owner, out float sqrDist)` — rival más cercano
- `MoriMochiAgent FighterAllyNear(AgentContext ctx, MoriMochiAgent owner, float maxDistance)` — aliado que pelea dentro de radio
- `MaterialPickup NearestDrop(AgentContext ctx, float maxDistance)` — material caído más cercano
- `MoriMochiAgent NearestTaunter(AgentContext ctx, MoriMochiAgent owner, float maxDistance)` — rival en intención Taunting
- `MoriMochiAgent NearestAlly(AgentContext ctx, MoriMochiAgent owner, float maxDistance, out float sqrDist)` — aliado más cercano
- `Vector3 ApproachPoint(AgentContext ctx, MoriMochiAgent owner, MaterialPickup target, ExpeditionRulesSO rules)` — punto a nivel rim, separado de otros recolectores (evita overlap)
- `Vector3 GuardPoint(AgentContext ctx, MaterialPickup post, ExpeditionRulesSO rules)` — punto de custodia entre post y salida
- `void FaceToward(AgentContext ctx, Vector3 point, float dt)` — rota smoothly hacia punto
- `bool HoldAtPost(AgentContext ctx, MaterialPickup post, ExpeditionRulesSO rules, ref float repathTimer, float dt)` — mantiene guardián en radio del post (retorna true si llegó)
- `Vector3 FleePoint(AgentContext ctx, Vector3 threat, Vector3 pull, bool hasPull, float distance)` — punto de huida (away from threat, pulled toward home/ally)

**Internals:**
- Cálculos de ángulos, distancias planares, muestreo de NavMesh
- Separación angular para evitar que múltiples recolectores ocupen el mismo rim

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[AgentGatherer]], [[AgentGuard]], [[AgentHunter]], [[AgentDecoy]], [[AgentScout]], [[AgentContext]], [[TeamBlackboard]]
