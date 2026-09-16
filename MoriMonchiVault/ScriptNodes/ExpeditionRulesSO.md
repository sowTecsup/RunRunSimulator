---
tags: [script, data, scriptableobject, expedition]
---

# ExpeditionRulesSO.cs

**Ruta:** `Data/Expedition/ExpeditionRulesSO.cs`

**Responsabilidad:** Singleton por escena centraliza tuning de expedición. Lista polimórfica de reglas `ExpeditionRuleBase`, knobs navegación/beats/ocupaciones/visión. `Activate()/Deactivate()` estáticos. **S124:** Eliminados 4 campos *Lock (BoldFightLock, ShyFleeLock, SocialProtectLock, LonerAggressiveLock) — diales ya no bloquean órdenes (base es autoridad).

## Métodos Estáticos

| Método | Descripción |
|--------|-------------|
| `Activate(ExpeditionRulesSO rules)` | Current = rules |
| `Deactivate(ExpeditionRulesSO rules)` | Si Current == rules, Current = null |

## Secciones de Tuning (Activas S124)

**Navegación:** ArriveDistance, RepathInterval, GiveUpSeconds, ApproachMargin
**Beats:** NoticeSeconds, LoseSeconds
**Ocupaciones:** MiningSecondsPerUnit, LodeMiningSecondsPerUnit, CarryCapacity, DepositSeconds, DropPrefab, DropScale, GuardRadius, HuntRepathInterval, DecoyRange, TauntSeconds, DecoyFleeDistance, DecoyFleeSeconds, DecoyCooldown
**Explorar (S103):** ScoutArriveDistance, ReportSeconds, ReportRepeatSeconds, ScoutRestSeconds
**Huida:** FleeTriggerDistance, FleeDistance, FleeSeconds, FleeCooldown, AllyPullRadius, GuardTrustRadius
**Contras:** HunterRetreatSeconds, GuardChaseRadius, GuardChaseSeconds, IdleMineSeconds, SupportCarryCapacity, DropPickupRadius, HunterBaitSeconds, BaitImmunitySeconds
**Visión:** VisionRadius, VisionDegrees, NearSenseRadius, BoldnessVisionSkew

## Cambios S124

**Eliminados (diales NO bloquean):**
- ~~BoldFightLock~~ 
- ~~ShyFleeLock~~
- ~~SocialProtectLock~~
- ~~LonerAggressiveLock~~

Base (ArenaBase) es la autoridad; diales afinen ejecución solo.

## Invariantes

- Singleton por escena
- Compartido por AgentExpedition, AgentSenses, AgentScout, etc.
- Huida triggerada por Gatherer sin custodio
- Contras balancean ocupaciones rivales

## Vinculado a

[[Index/23 - Arena Sandbox & Expedicion]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[VisionProfile]], [[AgentSenses]], [[MoriMochiAgent]], [[AgentExpedition]], [[ArenaSandbox]]
