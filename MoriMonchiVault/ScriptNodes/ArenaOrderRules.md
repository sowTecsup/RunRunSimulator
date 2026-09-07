---
tags: [script, data, expedition, rules]
---

# ArenaOrderRules.cs

**Ruta:** `Data/Expedition/ArenaOrderRules.cs`

**Responsabilidad:** Utilidades estáticas para convertir entre órdenes (`ArenaOrders`) y ocupaciones (`Occupation` / `ArenaSite`). Valida bloqueos de órdenes por personalidad (Boldness y Sociability fuerzan Contact y Posture), aplica ciampar de valores fuera de rango, y expone getters/setters por pilar (Loot, Contact, Posture).

**Métodos públicos:**
- `Occupation ToOccupation(ArenaOrders o)` — retorna Gather/Decoy si no hay Contact.Fight, Guard/Break si hay Fight (según Posture)
- `ArenaSite ToSite(ArenaOrders o)` — retorna Center si Loot=Big, else NearVein/FarVein (según Posture)
- `ArenaOrders FromOccupation(Occupation occupation, ArenaSite site)` — reconstruye órdenes a partir de ocupación y sitio
- `int Choice(ArenaOrders o, OrderPillar pillar)` — extrae el valor int del pilar (0-1)
- `ArenaOrders With(ArenaOrders o, OrderPillar pillar, int choice)` — retorna copia con pilar reemplazado
- `bool IsLocked(CreatureDNA dna, ExpeditionRulesSO rules, OrderPillar pillar, out int forced)` — chequea si DNA fuerza una elección; retorna true y asigna `forced` si hay bloqueo
- `ArenaOrders Clamp(CreatureDNA dna, ExpeditionRulesSO rules, ArenaOrders o)` — aplica bloqueos de Contact y Posture, retorna órdenes ajustadas

**Bloqueadores:**
- Contact: `Boldness >= BoldFightLock` fuerza Fight; `Boldness <= ShyFleeLock` fuerza Flee
- Posture: `Sociability >= SocialProtectLock` fuerza Protect; `Sociability <= LonerAggressiveLock` fuerza Aggressive

**Vinculado a:** [[Index/22 - Arena (S103-S104)]]

**Conexiones:** [[ArenaOrders]], [[ArenaOrderCatalog]], [[ExpeditionRulesSO]], [[CreatureDNA]], [[WorldEnums]]
