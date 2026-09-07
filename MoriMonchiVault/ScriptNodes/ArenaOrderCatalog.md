---
tags: [script, data, expedition, catalog]
---

# ArenaOrderCatalog.cs

**Ruta:** `Data/Expedition/ArenaOrderCatalog.cs`

**Responsabilidad:** Catálogo estático de textos, etiquetas y descripciones para órdenes, arquetipos y estado de arena. Traduce enums a strings legibles ("Grande", "Escapar", "Proteger"), genera nombres de arquetipos ("Guardián", "Cazador", "Recolector", "Señuelo"), describe estrategias de equipo ("Jauría del centro", "Muralla de las vetas"), infiere lectura de sala (terreno, vetas, loot), y sugiere contras/desbloqueos. **S105: textos de Cazador y Señuelo actualizados con anzuelo**. Multilingüe (es).

**Métodos públicos:**
- `string[] PillarLabels { get; }` — ["BOTÍN", "ENCUENTRO", "EQUIPO"]
- `string ChoiceLabel(OrderPillar pillar, int choice)` — etiqueta legible por pilar y valor
- `string ArchetypeShort(ArenaOrders o)` — "Guardián", "Cazador", "Recolector" o "Señuelo"
- `string LootPlace(ArenaOrders o)` — "del centro" o "de las vetas"
- `string ArchetypeName(ArenaOrders o)` — `ArchetypeShort() + " " + LootPlace()`
- `string ArchetypeDescription(ArenaOrders o)` — descripción en prosa de mecánica (1-2 oraciones)
  - **S105:** Cazador: "Ronda ... cazando rivales cargados ... Si no hay a quién cazar, mina un poco."
  - **S105:** Señuelo: "Se acerca a los rivales... los provoca y huye; se lleva lo que cae. No pelea ni mina."
- `string PastVerb(ArenaOrders o)` — verbo pasado por rol ("vigiló", "cazó", "recolectó", "distrajo")
- `string PersonalityName(CreatureDNA dna, ExpeditionRulesSO rules)` — tipo de personalidad ("Osado solitario", "Tímido sociable", etc.)
- `string CounterHint(ArenaOrders o)` — cómo counters actúan contra este arquetipo
  - **S105:** Cazador: "Vacía recolectores sin guardián; si lo tumban se retira. No toca a quien está custodiado y **muerde el anzuelo de un señuelo**."
  - **S105:** Señuelo: "Arrastra guardianes y cazadores lejos de su puesto y espanta recolectores. Sin quien aproveche el hueco, no rinde."
- `string TeamPlanName(IReadOnlyList<ArenaOrders> orders)` — nombre de estrategia por composición de equipo
- `string RivalRead(CreatureDNA dna, ExpeditionRulesSO rules)` — qué puede hacer un rival ("guardián o cazador", "puede hacer cualquiera", etc.)
- `string UnlockRead(CreatureDNA dna, ExpeditionRulesSO rules)` — qué desbloquea al subirle stats
- `string RoomText(ArenaRoomRead read)` — descripción de sala: terreno, vetas, distancias
- `string LockReason(OrderPillar pillar, int forced)` — por qué está forzada ("nunca huye", "nunca pelea", etc.)

**Arquetipos (S104-S105):**
- **Guardián** (Fight + Protect): "Se planta en [lugar] y embiste a quien se acerque. No recolecta ni persigue."
  - Contra: "Frena cazadores y cubre a quien mina a su lado. Lo saca del puesto un señuelo."
- **Cazador** (Fight + Aggressive): "Ronda [lugar] cazando rivales cargados para que suelten. Si no hay a quién cazar, mina un poco."
  - Contra: "Vacía recolectores sin guardián; si lo tumban se retira. No toca a quien está custodiado y muerde el anzuelo de un señuelo."
  - **S105:** Mención explícita de anzuelo (HunterBaitSeconds)
- **Recolector** (Flee + Protect): "Mina [lugar] y huye con la carga apenas ve un rival. Nunca pelea."
  - Contra: "Es quien puntúa. Con un guardián a menos de 6 m no huye. Lo caza un cazador."
- **Señuelo** (Flee + Aggressive): "Se acerca a los rivales de [lugar], los provoca y huye; se lleva lo que cae. No pelea ni mina."
  - Contra: "Arrastra guardianes y cazadores lejos de su puesto y espanta recolectores. Sin quien aproveche el hueco, no rinde."

**Internals:**
- LootLabels = {"Grande", "Pequeño"}
- ContactLabels = {"Escapar", "Enfrentar"}
- PostureLabels = {"Proteger", "Agresivo"}
- Lógica scoring en TeamPlanName: conteo de roles (guards, hunters, miners, decoys, big)

**Integración:**
- Usado por UI (BreedingEggsTabPresenter, ArenaPlanPanel, CreatureGridUI)
- Consulta ExpeditionRulesSO para locks de personalidad
- Genera textos dinámicos según órdenes/DNA

**Invariantes:**
- Descripciones invariantes respecto órdenes (ArchetypeDescription determinista)
- CounterHint refiere a mecánicas en código (Guard chase, Hunter baiting, Decoy taunt)
- PersonalityName + RivalRead + UnlockRead son deterministas por DNA + rules locks

**S105 Cambios:**
- ArchetypeDescription (Cazador): menciona "si no hay a quién cazar, mina un poco" → TryHunt + fallback
- CounterHint (Cazador): agrega "muerde el anzuelo de un señuelo" → HunterBaitSeconds mecánica

**Vinculado a:** [[Index/23 - Arena Sandbox y Expedicion]]

**Conexiones:** [[ArenaOrders]], [[ArenaOrderRules]], [[ArenaRoomRead]], [[CreatureDNA]], [[ExpeditionRulesSO]], [[ArenaMatrixPlans]]
