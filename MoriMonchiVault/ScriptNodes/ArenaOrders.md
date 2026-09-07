---
tags: [script, data, expedition, struct]
---

# ArenaOrders.cs

**Ruta:** `Data/Expedition/ArenaOrders.cs`

**Responsabilidad:** Struct serializable que encapsula las tres órdenes que define el equipo de jugador o rivales en arena: `Loot` (qué recolectar: grande/pequeño), `Contact` (cómo actuar con rivales: huir/enfrentar), `Posture` (rol dentro de la acción: proteger/agresivo). Constructor, igualdad y constante `Default` (grande, huir, proteger).

**Campos públicos:**
- `LootChoice Loot` — objetivo de recolección (Big o Small)
- `ContactChoice Contact` — disposición ante contacto (Flee o Fight)
- `PostureChoice Posture` — rol actitudinal (Protect o Aggressive)

**Constructores y métodos:**
- `ArenaOrders(LootChoice loot, ContactChoice contact, PostureChoice posture)` — inicializa los tres campos
- `static ArenaOrders Default { get; }` — retorna (Big, Flee, Protect)
- `bool Equals(ArenaOrders other)` — compara los tres campos

**Vinculado a:** [[Index/22 - Arena (S103-S104)]]

**Conexiones:** [[ArenaOrderRules]], [[ArenaOrderCatalog]], [[MoriMochiAgent]], [[AgentContext]]
