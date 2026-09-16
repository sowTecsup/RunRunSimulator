---
tags: [script, data, expedition, bases]
---

# ArenaBases.cs

**Ruta:** `Data/Expedition/ArenaBases.cs`

**Responsabilidad (S122):** Utilidad estática que mapea `Role` (personalidad genética) a `ArenaBase` (estrategia de competencia). Cada rol abre 2 de 3 bases; la variante sale del rol y se mapea a `ArenaOrders` concretas. Provee métodos de lectura, nombre/descripción, validación y mapeo inverso.

**Enum ArenaBase:**
- `Territory = 0` — centro: domina recurso central
- `Forage = 1` — rebusque: explota veta cercana
- `Opportunism = 2` — oportunismo: vive de lo que sobra

**Métodos estáticos:**
- `string Name(ArenaBase b)` — "Territorio", "Rebusque", "Oportunismo"
- `string RoleName(Role r)` — "Protector", "Agresivo", "Empático"
- `ArenaBase Closed(Role r)` — la base cerrada para el rol (Protector → Opportunism, Agresivo → Forage, Empático → Territory)
- `bool Opens(Role r, ArenaBase b)` — true si la base está abierta para el rol
- `string ClosedReason(Role r)` — razón legible por qué la base está cerrada
- `ArenaBase OpenAt(Role r, int index)` — la base abierta en índice 0 o 1 del rol
- `ArenaBase Default(Role r)` — base por defecto si no se especifica (Agresivo → Territory, otros → Forage)
- `ArenaOrders ToOrders(Role r, ArenaBase b)` — mapea rol + base a órdenes concretas (clampeado a default si base cerrada)
- `bool TryBaseOf(Role r, ArenaOrders o, out ArenaBase b)` — mapeo inverso: órdenes → base del rol
- `bool RoleFor(ArenaOrders o, out Role r)` — mapeo inverso: órdenes → rol
- `string VariantName(Role r, ArenaBase b)` — nombre de la variante (ej: Protector + Territory = "Dominante")
- `string VariantDescription(Role r, ArenaBase b)` — descripción estratégica de la variante
- `string RivalRead(Role r)` — lectura de rivales: "Protector · Territorio o Rebusque"

**Mapeo Rol → Bases → Variantes → Órdenes:**

| Role | Base | Variante | LootChoice | ContactChoice | PostureChoice |
|---|---|---|---|---|---|
| Protector | Territory | Dominante | Big | Fight | Protect |
| Protector | Forage | Custodio | Small | Flee | Protect |
| Agresivo | Territory | Invasor | Big | Fight | Aggressive |
| Agresivo | Opportunism | Hiena | Small | Flee | Aggressive |
| Empático | Forage | Compañera | Big | Flee | Protect |
| Empático | Opportunism | Gaviota | Small | Fight | Aggressive |

**S122-S122:** Implementación nueva. Diseño decidido S118 (`Index/22` Parte 3), implementado S122. Bases por personalidad permiten que cada rol tenga una estrategia única pero elegible. Las órdenes clampeadas ya no contienen diales bloqueados; las bases se leen en el panel de plan como píldoras y en rivales como "Protector · Territorio o Rebusque".

**Invariantes:**
- Cada rol abre exactamente 2 de 3 bases (una cerrada)
- `ToOrders()` clampea a default si la base está cerrada
- `TryBaseOf()` e `RoleFor()` implementan mapeo inverso confiable
- Nombres y descripciones en español neutro

**Vinculado a:** [[Index/22 - Bajada Nocturna y Linaje]] (§3, Parte 8), [[Index/24 - Puente Tienda-Arena]] (§6c)

**Conexiones:** [[ArenaBase]] (enum), [[Role]] (enum), [[ArenaOrders]], [[ArenaOrderRules]], [[ArenaCastPlanner]], [[ArenaPlanPanel]]
