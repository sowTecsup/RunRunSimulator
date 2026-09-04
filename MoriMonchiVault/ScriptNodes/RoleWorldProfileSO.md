---
tags: [script, genetics, world, tuning]
---

# RoleWorldProfileSO.cs

**Ruta:** `Data/Genetics/RoleWorldProfileSO.cs`

**Responsabilidad:** SerializedScriptableObject con diccionario `Dictionary<Role, RoleWorldProfile>`. Data-driven tuning centralizado de cómo cada rol se comporta en world: movimiento (MoveSpeed, IdleChance, RoamRadius), reacción al jugador (ProximityReaction, FollowDistance), confinamiento (PreferredArea, AreaPreference), recuperación (RecoverySpeed), **S98 NUEVO:** `RoamSpeedFactor` (multiplicador de velocidad durante Roaming), y S64: lista polimórfica de ReactionRuleBase para percepción social. `GetProfile(Role)` devuelve fallback neutro si falta entrada. Botón "Populate Defaults" precarga los tres roles con tuning base y reglas de reacción. **S98 Hallazgo:** `MoveSpeed` no lo lee nadie; la velocidad base real viene del prefab del NavMeshAgent (cacheada en `AgentContext.BaseSpeed`), y `RoamSpeedFactor` se aplica como multiplicador vía `ApplyGaitSpeed()`.

## RoleWorldProfile (Bloque Serializable)

- `MoveSpeed` (float, default 2.5) — **S98 HALLAZGO:** campo aquí pero NO lo lee nadie. La velocidad base real es el `speed` del NavMeshAgent en el prefab (cacheada en `AgentContext.BaseSpeed`). Mantenerlo como histórico/referencia.
- **S98 NUEVO:**
  - `RoamSpeedFactor` (float, range 0.2–1, default 1) — multiplicador de velocidad durante Roaming. Default 1 en código; en `RoleWorldProfileTable.asset`: 0.35 para los 3 roles. Aplicado por `AgentContext.ApplyGaitSpeed()` (Roaming → BaseSpeed × RoamSpeedFactor).
- `IdleChance` (float, default 0.3) — probabilidad de pausar en waypoint
- `IdleMin/Max` (float, default 0.5–1.5s) — rango de duración de pausa
- `RoamRadius` (float, default 4m) — distancia para samplear próximo punto de roam
- `ProximityRadius` (float, default 6m) — radio de detección del jugador
- `Reaction` (ProximityReaction) — tipo de reacción al jugador cercano (Ignore/Flee/Approach/Follow/Retreat)
- `FollowDistance` (float, default 2m) — distancia de parada en reacciones amigables
- `PreferredArea` (WorldArea, default ShopBackroom) — área que el agente prefiere
- `AreaPreference` (float, range 0–1, default 0.5) — odds de roam hacia PreferredArea
- `RecoverySpeed` (float, default 1.0) — multiplicador de velocidad al levantarse post-throw
- `Tint` (Color, default white) — color debug del cuerpo por rol
- `Reactions` (List<ReactionRuleBase>) — S64 lista polimórfica de reglas de reacción social

## Método GetProfile(Role)

Retorna `RoleWorldProfile` asociado, o fallback neutral si falta entrada o es null.

## Método Populate Defaults

Precarga los tres roles Protector/Agresivo/Empatico con perfiles iniciales e igual aplica `RoamSpeedFactor = 0.35f` a todos (líneas 31-33). Agrega reglas de reacción por rol:

- **Protector:** MoveSpeed 1.8, Roaming tímido (IdleChance 0.55), preferencia Storage 0.75, reacción Ignore
  - Reglas: Approach amigos (0.3), Avoid enemigos (−0.35)
  - RoamSpeedFactor 0.35 (línea 31)

- **Agresivo:** MoveSpeed 2.6, hiperactivo (IdleChance 0.25), preferencia frontDesk 0.6, reacción Approach
  - Reglas: PlayChase (0.25, cooldown 20s), Avoid (−0.5)
  - RoamSpeedFactor 0.35 (línea 32)

- **Empatico:** MoveSpeed 2.8, activísimo (IdleChance 0.25), preferencia frontDesk 0.5, reacción Follow
  - Reglas: Approach (0.15), PlayChase (0.35), Avoid (−0.6)
  - RoamSpeedFactor 0.35 (línea 33)

## Cambios S98

**RoamSpeedFactor centralizado:**
- Campo nuevo en RoleWorldProfile (línea 81)
- Default en código: 1f (sin reducción)
- Default en asset `RoleWorldProfileTable.asset`: 0.35f para los 3 roles (líneas 31-33)
- Leído por `AgentContext.ApplyGaitSpeed()` cada frame: `factor = (State == Roaming && Profile != null) ? Profile.RoamSpeedFactor : 1f`
- Aplicado: `Agent.speed = BaseSpeed × factor`
- Efecto: Roaming es ~65% más lento que otros estados (0.35 = 35% velocidad)

**Hallazgo S98:**
- `MoveSpeed` no lo lee nadie en código. La velocidad base real viene del NavMeshAgent en el prefab (`Agent.speed` en setup inicial).
- `AgentContext.BaseSpeed` cachea ese valor inicial
- `RoamSpeedFactor` modula dinámicamente según estado

## Notas S64 + S98

- Cada rol tiene DIFERENTE lista de reacciones: no hay rules global
- La lista Reactions es local al perfil, no compartida entre roles
- ReactionRuleBase es abstracta; dropdown de Odin permite seleccionar concretas (ApproachFriendRule, AvoidDislikedRule, PlayChaseRule)
- `RoamSpeedFactor` permite tuning fino: roles tímidos (Protector) se mueven lento en Roaming para explorar; roles activos (Agresivo/Empatico) similar velocidad en todos lados.

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/14 - Social V1]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S98: velocidades en arena, S99: equipos)

## Conexiones

- [[MoriMochiAgent]] — obtiene Profile vía Role
- [[AgentContext]] — S98 lee Profile.RoamSpeedFactor en ApplyGaitSpeed()
- [[AgentSocial]] — consulta Reactions del Profile
- [[GeneticsEnums]] — Role enum
- [[ReactionRuleBase]] — lista de reglas en Reactions
- [[SocialTuningSO]]
