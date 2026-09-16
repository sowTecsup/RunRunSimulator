---
tags: [script, data, expedition, catalog]
---

# ArenaOrderCatalog.cs

**Ruta:** `Data/Expedition/ArenaOrderCatalog.cs`

**Responsabilidad:** Catálogo estático de textos, etiquetas y descripciones para órdenes, arquetipos y estado de arena. Traduce enums a strings legibles, genera nombres de arquetipos, describe estrategias de equipo, infiere lectura de sala, sugiere contras. **S122:** `PersonalityName` devuelve `ArenaBases.RoleName(dna.Role)` y `RivalRead` devuelve `ArenaBases.RivalRead(dna.Role)` (p. ej. "Protector · Territorio o Rebusque"); `UnlockRead` y `LockReason` borrados (los diales ya no bloquean).

**Métodos públicos:**
- `string[] PillarLabels { get; }` — ["BOTÍN", "ENCUENTRO", "EQUIPO"]
- `string ChoiceLabel(OrderPillar pillar, int choice)` — etiqueta legible
- `string ArchetypeShort(ArenaOrders o)` — "Guardián", "Cazador", "Recolector", "Señuelo"
- `string ArchetypeName(ArenaOrders o)` — nombre largo
- `string ArchetypeDescription(ArenaOrders o)` — descripción estratégica
- `string CounterHint(ArenaOrders o)` — cómo counters actúan
- `string TeamPlanName(IReadOnlyList<ArenaOrders> orders)` — nombre de estrategia de equipo
- `string RivalRead(CreatureDNA dna, ExpeditionRulesSO rules)` — qué puede hacer un rival
- `string RoomText(ArenaRoomRead read)` — descripción de sala

**S105:** Textos de Cazador + Señuelo con mecánica de anzuelo.

**S122:** Nombre de personalidad y lectura del rival delegan en [[ArenaBases]]; arquetipos, `CounterHint` y `TeamPlanName` siguen iguales.

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]], [[Index/22 - Bajada Nocturna y Linaje]]

**Conexiones:** [[ArenaBases]], [[ArenaOrders]], [[ArenaPlanPanel]]
