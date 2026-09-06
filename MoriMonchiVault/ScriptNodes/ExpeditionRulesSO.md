---
tags: [script, data, scriptableobject, expedition]
---

# ExpeditionRulesSO.cs

**Ruta:** `Data/Expedition/ExpeditionRulesSO.cs`

**Responsabilidad:** Singleton por escena (`Current` static) que centraliza tuning de expedición. Contiene lista polimórfica de reglas `ExpeditionRuleBase`, knobs de navegación/beats, **visión S102**, **exploración S103**. `Activate()/Deactivate()` estáticos (ArenaSandbox.OnEnable/OnDisable). En tienda, `Current == null` → expedición desactiva. En Arena, `Current` apunta a `ExpeditionRules.asset`.

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
- `NoticeSeconds`, `TakeSeconds`, `LoseSeconds` [Min(0)]

**Ocupación Gather:**
- `MiningSecondsPerUnit`, `CarryCapacity`, `DepositSeconds`, `DropPrefab`, `DropScale`

**Ocupación Guard:**
- `GuardRadius` [Min(1)] = 4

**Ocupación Break:**
- `HuntRepathInterval` [Min(0.1)] = 0.4

**Ocupación Decoy:**
- `DecoyRange`, `TauntSeconds`, `DecoyFleeDistance`, `DecoyFleeSeconds`, `DecoyCooldown`

**Visión (S102):**
- `VisionRadius`, `VisionDegrees`, `NearSenseRadius`
- `BoldnessVisionSkew` — multiplicador por osadía

**Exploración S103 NUEVA:**
- `ScoutArriveDistance` [Min(0)] = 1.2 — distancia de arribo al sitio scout
- `ReportSeconds` [Min(0)] = 0.9 — duración de reporte (stand still)
- `ReportRepeatSeconds` [Min(0)] = 4 — cooldown entre reportes de misma veta
- `ScoutRestSeconds` [Min(0)] = 12 — cooldown tras completar ciclo de visita

**Métodos Públicos:**
- `PopulateDefaults()` [Button] — inicializa rules con SeekMaterialRule

**Invariantes:**
- Singleton por escena
- Compartido por AgentExpedition, AgentSenses, AgentScout (S103)
- Visión condicional: null en tienda, poblado en Arena
- **S103:** Exploración knobs consultados por AgentScout.TryEngage/Tick

**S103 Cambios:**
- Sección "Explorar" agregada con 4 knobs (ScoutArriveDistance, ReportSeconds, ReportRepeatSeconds, ScoutRestSeconds)
- AgentScout consulta estos valores
- TeamBlackboard.ReportVein() usa ReportRepeatSeconds

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[VisionProfile]], [[AgentSenses]], [[MoriMochiAgent]], [[AgentExpedition]], [[AgentScout]], [[TeamBlackboard]], [[ArenaSandbox]], [[ArenaRoomCueOverlay]]
