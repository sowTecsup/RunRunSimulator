---
tags: [script, data, scriptableobject, expedition]
---

# ExpeditionRulesSO.cs

**Ruta:** `Data/Expedition/ExpeditionRulesSO.cs`

**Responsabilidad:** Singleton por escena (`Current` static) que centraliza tuning de expedición. Contiene lista polimórfica de reglas `ExpeditionRuleBase`, knobs de navegación/beats/ocupaciones/visión/exploración (S103), **órdenes/huida/contras (S104 NUEVO)**. `Activate()/Deactivate()` estáticos (ArenaSandbox.OnEnable/OnDisable). En tienda, `Current == null` → expedición desactiva. En Arena, `Current` apunta a `ExpeditionRules.asset`.

**Métodos Estáticos:**
- `Activate(ExpeditionRulesSO rules)` — Current = rules
- `Deactivate(ExpeditionRulesSO rules)` — si Current == rules, Current = null

**Secciones de Tuning:**

**Navegación:**
- `ArriveDistance` [Min(0.1)] = 0.9
- `RepathInterval` [Min(0.05)] = 0.5
- `GiveUpSeconds` [Min(1)] = 12
- `ApproachMargin` [Min(0.05)] = 0.15

**Beats (interacción):**
- `NoticeSeconds` [Min(0)] = 0.5 — tiempo antes de moverse a sitio
- `TakeSeconds` — obsoleto (no usado)
- `LoseSeconds` [Min(0)] = 1 — tiempo sin poder encontrar sitio

**Ocupaciones:**
- `MiningSecondsPerUnit` [Min(0.5)] = 4 — tiempo por unidad normal
- `LodeMiningSecondsPerUnit` [Min(0.5)] = 2 — tiempo por unidad lode (S104 NUEVO)
- `CarryCapacity` [Min(1)] = 3 — máximo normal
- `SupportCarryCapacity` [Min(1)] = 2 — máximo para Guard/Break (S104)
- `DepositSeconds` [Min(0)] = 0.8
- `DropPrefab`, `DropScale` — material caído por golpeo
- `GuardRadius` [Min(1)] = 4
- `HuntRepathInterval` [Min(0.1)] = 0.4
- `DecoyRange` [Min(1)] = 4.5
- `TauntSeconds` [Min(0)] = 0.8
- `DecoyFleeDistance` [Min(1)] = 8
- `DecoyFleeSeconds` [Min(0.5)] = 5
- `DecoyCooldown` [Min(0)] = 4

**Explorar (S103):**
- `ScoutArriveDistance` [Min(0)] = 1.2
- `ReportSeconds` [Min(0)] = 0.9
- `ReportRepeatSeconds` [Min(0)] = 4
- `ScoutRestSeconds` [Min(0)] = 12

**Órdenes (S104 NUEVO):**
- `BoldFightLock` [Range(0,1)] = 0.65 — Boldness que fuerza Contact.Fight
- `ShyFleeLock` [Range(0,1)] = 0.35 — Boldness que fuerza Contact.Flee
- `SocialProtectLock` [Range(0,1)] = 0.65 — Sociability que fuerza Posture.Protect
- `LonerAggressiveLock` [Range(0,1)] = 0.35 — Sociability que fuerza Posture.Aggressive

**Huida (S104 NUEVO):**
- `FleeTriggerDistance` [Min(1)] = 6.5 — distancia a rival que activa huida
- `FleeDistance` [Min(1)] = 9 — distancia de huida
- `FleeSeconds` [Min(0.5)] = 3.5 — duración de huida
- `FleeCooldown` [Min(0)] = 2 — espera post-huida antes de huir de nuevo
- `AllyPullRadius` [Min(0)] = 15 — radio de aliados que tiran hacia ellos
- `GuardTrustRadius` [Min(0)] = 6 — radio donde Gatherer confía en Guardian

**Contras (S104 NUEVO):**
- `HunterRetreatSeconds` [Min(0)] = 10 — tiempo que Hunter se retira post-golpeo
- `GuardChaseRadius` [Min(0)] = 10 — radio de persecución de provocadores
- `GuardChaseSeconds` [Min(0)] = 8 — duración persecución
- `IdleMineSeconds` [Min(0)] = 8 — tiempo ocioso antes de que ocupación no-gather cambie a Gather
- `DropPickupRadius` [Min(0)] = 6 — radio para detectar drops caídos

**Visión (S102):**
- `VisionRadius` [Min(1)] = 11
- `VisionDegrees` [Range(30,360)] = 150
- `NearSenseRadius` [Min(0)] = 3
- `BoldnessVisionSkew` [Range(0,0.5)] = 0.25 — multiplicador por osadía

**Métodos Públicos:**
- `PopulateDefaults()` [Button] — inicializa rules con SeekMaterialRule

**Invariantes:**
- Singleton por escena
- Compartido por AgentExpedition, AgentSenses, AgentScout (S103), AgentGatherer, AgentGuard, AgentHunter, AgentDecoy (S104)
- Órdenes inmutables dentro de expedición (clampeadas al inicio)
- Huida triggerada por Gatherer si rival amenaza sin custodio
- Contras balancean ocupaciones rivales (Hunter retreat, Guard chase, idle → Gather)

**S104 Cambios:**
- Secciones "Órdenes", "Huida", "Contras" agregadas
- LodeMiningSecondsPerUnit agregado (lode más rápido que veta)
- SupportCarryCapacity agregado (Guard/Break llevan menos)
- ArenaOrderRules.IsLocked() consulta secciones de órdenes
- AgentGatherer consulta Huida + AllyPullRadius
- AgentGuard consulta GuardChase*
- AgentHunter consulta HunterRetreatSeconds
- AgentExpedition consulta IdleMineSeconds

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[VisionProfile]], [[AgentSenses]], [[MoriMochiAgent]], [[AgentExpedition]], [[AgentGatherer]], [[AgentGuard]], [[AgentHunter]], [[AgentDecoy]], [[AgentScout]], [[TeamBlackboard]], [[ArenaSandbox]], [[ArenaOrderRules]]
