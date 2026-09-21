---
tags: [script, world, ai, agent, facade, expedition]
---

# MoriMochiAgent.cs

**Ruta:** `World/AI/MoriMochiAgent.cs`

**Responsabilidad:** Núcleo delgado que orquesta vida en mundo. Compone 9 colaboradores. **S122:** Expone feedbacks onDiveLaunch/onDiveSlam para VFX (despegue y impacto de picada). **S129:** Sin inspector de stats (removido bloque de campos públicos serializados para visualización de stats base/finales).

**Propiedades Públicas (Fachada):**
- `CreatureDNA DNA { get; }`
- `CreatureIntent Intent { get; }`
- `ExpeditionTeam Team { get; }`
- `Occupation Occupation { get; }`
- `ArenaOrders Orders { get; }`
- `void SetOrders(ArenaOrders orders)`

**Eventos (S118, S122):**
- `onClashHit` (UnityEvent) — golpe conecta
- `onDiveLaunch` — **(S122)** despegue de picada (Wings)
- `onDiveSlam` — **(S122)** impacto de picada en suelo

**S122 Cambios:**
- Dos eventos nuevos: `onDiveLaunch` + `onDiveSlam`
- ClashStrike dispara en Launch + Land (sin que AgentClash lo sepa)
- Prefab MorimonchiAgent: hijo Feedbacks/ con MMF_Players wired a onDiveLaunch/onDiveSlam

**S129 Cambios:**
- Removido bloque de inspector `[Title("Stats")]` con campos `StatCon`, `StatAtk`, ..., `StatEva` (readonly visualización)
- Removidos campos `StatsBase`, `StatsFinal` (info de debug en inspector)
- Composición sigue igual: 9 colabs sin partial class

**Composición (S55):**
- Sin partial class; todo en colabs delegados

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]], [[Index/22 - Bajada Nocturna y Linaje]] (S122)

**Conexiones:** [[AgentClash]], [[ClashStrike]], [[MoriMonchiAgent]], [[MMFeedbacks]]
