---
tags: [script, genetics, world, tuning]
---

# RoleWorldProfileSO.cs

**Ruta:** `Data/Genetics/RoleWorldProfileSO.cs`

**Responsabilidad:** SerializedScriptableObject con diccionario `Dictionary<Role, RoleWorldProfile>`. Data-driven tuning centralizado de cómo cada rol se comporta en world: movimiento (MoveSpeed, IdleChance, RoamRadius), reacción al jugador (ProximityReaction, FollowDistance), confinamiento (PreferredArea, AreaPreference), recuperación (RecoverySpeed), y NUEVO en S64: lista polimórfica de ReactionRuleBase para percepción social. `GetProfile(Role)` devuelve fallback neutro si falta entrada. Singleton (Current) establecido en OnEnable. Botón "Populate Defaults" precarga los tres roles estándar con ejemplos de reglas de reacción.

**RoleWorldProfile (bloque serializable):**
- `MoveSpeed` — velocidad base del NavMeshAgent (default 2.5)
- `IdleChance` — probabilidad de pausar en waypoint (default 0.3)
- `IdleMin/Max` — rango de duración de pausa (default 0.5–1.5s)
- `RoamRadius` — distancia para samplear próximo punto de roam (default 4m)
- `ProximityRadius` — radio de detección del jugador (default 6m)
- `Reaction` — ProximityReaction al jugador cercano (Ignore/Flee/Approach/Follow/Retreat)
- `FollowDistance` — distancia de parada en reacciones amigables (default 2m)
- `PreferredArea` — WorldArea que el agente prefiere (default ShopBackroom)
- `AreaPreference` — [0,1] odds de roam hacia PreferredArea (default 0.5)
- `RecoverySpeed` — multiplicador de velocidad al levantarse post-throw (default 1.0)
- `Tint` — color debug/visible del cuerpo por rol (default white)
- `Reactions` — **S64 NUEVO** `List<ReactionRuleBase>` polimórfica (Odin serializable). Dropdown "+" permite mezclar ApproachFriendRule, AvoidDislikedRule, PlayChaseRule libremente. AgentSocial lo consulta en TryEngage y AdjustRoamForAvoidance.

**Método Populate Defaults:**
Precarga los tres roles con perfiles iniciales S64:
- **Protector:** MoveSpeed 1.8, Roaming tímido (IdleChance 0.55, preferencia Storage 0.75), reacción Ignore al jugador; + reglas: Approach amigos (0.3), Avoid enemigos (−0.35)
- **Agresivo:** MoveSpeed 2.6, hiperactivo (IdleChance 0.25), preferencia frontDesk (0.6), reacción Approach; + reglas: PlayChase (0.25, cooldown 20s), Avoid (−0.5)
- **Empatico:** MoveSpeed 2.8, activísimo (IdleChance 0.25), preferencia frontDesk (0.5), reacción Follow; + reglas: Approach (0.15), PlayChase (0.35), Avoid (−0.6)

**Notas:**
- Cada rol tiene DIFERENTE lista de reacciones: no hay rules global, cada perfil define su propia sociabilidad
- La lista Reactions es local al perfil, no compartida entre roles
- ReactionRuleBase es abstracta; dropdown de Odin permite seleccionar concretas (ApproachFriendRule, AvoidDislikedRule, PlayChaseRule)
- Cada regla tiene `Cooldown` propio (no usado en S64, futuro: throttling por regla)

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[MoriMonchiVault/Index/14 - Social V1]]

**Conexiones:** [[MoriMochiAgent]], [[AgentSocial]], [[GeneticsEnums]], [[ReactionRuleBase]], [[SocialTuningSO]]
