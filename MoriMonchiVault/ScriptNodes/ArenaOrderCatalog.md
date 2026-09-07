---
tags: [script, data, expedition, catalog]
---

# ArenaOrderCatalog.cs

**Ruta:** `Data/Expedition/ArenaOrderCatalog.cs`

**Responsabilidad:** Catálogo estático de textos, etiquetas y descripciones para órdenes, arquetipos y estado de arena. Traduce enums a strings legibles ("Grande", "Escapar", "Proteger"), genera nombres de arquetipos ("Guardián", "Cazador", "Recolector", "Señuelo"), describe estrategias de equipo ("Jauría del centro", "Muralla de las vetas"), infiere lectura de sala (terreno, vetas, loot), y sugiere contras/desbloqueos. Multilingüe (es).

**Métodos públicos:**
- `string[] PillarLabels { get; }` — ["BOTÍN", "ENCUENTRO", "EQUIPO"]
- `string ChoiceLabel(OrderPillar pillar, int choice)` — etiqueta legible por pilar y valor
- `string ArchetypeShort(ArenaOrders o)` — "Guardián", "Cazador", "Recolector" o "Señuelo"
- `string LootPlace(ArenaOrders o)` — "del centro" o "de las vetas"
- `string ArchetypeName(ArenaOrders o)` — `ArchetypeShort() + " " + LootPlace()`
- `string ArchetypeDescription(ArenaOrders o)` — descripción en prosa de mecánica (1-2 oraciones)
- `string PastVerb(ArenaOrders o)` — verbo pasado por rol ("vigiló", "cazó", "recolectó", "distrajo")
- `string PersonalityName(CreatureDNA dna, ExpeditionRulesSO rules)` — tipo de personalidad ("Osado solitario", "Tímido sociable", etc.)
- `string CounterHint(ArenaOrders o)` — cómo counters actúan contra este arquetipo
- `string TeamPlanName(IReadOnlyList<ArenaOrders> orders)` — nombre de estrategia por composición de equipo
- `string RivalRead(CreatureDNA dna, ExpeditionRulesSO rules)` — qué puede hacer un rival dado su DNA ("guardián o cazador", "puede hacer cualquiera", etc.)
- `string UnlockRead(CreatureDNA dna, ExpeditionRulesSO rules)` — qué desbloquea al subirle stats
- `string RoomText(ArenaRoomRead read)` — descripción de sala: terreno, vetas, distancias
- `string LockReason(OrderPillar pillar, int forced)` — por qué está forzada ("nunca huye", "nunca pelea", etc.)

**Internals:**
- Diccionarios privados de etiquetas por enum
- Lógica de scoring para nombres de equipos (conteo de roles y loot distribution)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]]

**Conexiones:** [[ArenaOrders]], [[ArenaOrderRules]], [[ArenaRoomRead]], [[CreatureDNA]], [[ExpeditionRulesSO]]
