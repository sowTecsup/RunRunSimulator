---
tags: [script, data, expedition, rules]
---

# ArenaOrderRules.cs

**Ruta:** `Data/Expedition/ArenaOrderRules.cs`

**Responsabilidad:** Utilidades estáticas para convertir entre órdenes (`ArenaOrders`), ocupaciones (`Occupation` / `ArenaSite`) y bases (`ArenaBase`). Valida bloqueos de órdenes por personalidad (Boldness/Sociability), aplica clamping de valores fuera de rango. **S122:** Agrega `Clamp(role, base)` que mapea base a órdenes concretas (sin diales).

**Métodos públicos:**

| Método | Descripción |
|--------|-------------|
| `Occupation ToOccupation(ArenaOrders o)` | Retorna Gather/Decoy/Guard/Break según Contact+Posture |
| `ArenaSite ToSite(ArenaOrders o)` | Retorna Center/NearVein/FarVein según Loot y Posture |
| `ArenaOrders FromOccupation(Occupation occ, ArenaSite site)` | Reconstruye órdenes desde ocupación y sitio |
| `int Choice(ArenaOrders o, OrderPillar pillar)` | Extrae int del pilar (0-1) |
| `ArenaOrders With(ArenaOrders o, OrderPillar pillar, int choice)` | Retorna copia con pilar reemplazado |
| `ArenaOrders Clamp(CreatureDNA dna, ExpeditionRulesSO rules, ArenaOrders o)` | Aplica bloqueos por diales (S104-S121) |
| `ArenaOrders Clamp(Role role, ArenaBase base)` | **(S122)** Mapea rol+base a órdenes concretas (sin diales) |

## Mapeo Rol+Base → Órdenes (S122)

Delegado a [[ArenaBases]].ToOrders(role, base):

| Role | Territory | Forage | Opportunism |
|------|-----------|--------|-------------|
| Protector | Big/Fight/Protect | Small/Flee/Protect | — |
| Agresivo | Big/Fight/Aggressive | — | Small/Flee/Aggressive |
| Empático | — | Big/Flee/Protect | Small/Fight/Aggressive |

## Determinismo (S122+S124)

- `Clamp(dna, rules, o)` retorna `o` si es variante de base abierta del Role
- `Clamp(role, base)` es determinístico: mismo rol+base = mismas órdenes
- Diales seguir existiendo pero no bloquean ocupación (solo afinar ejecución en S122+)

## Invariantes S104+S111+S122

- Órdenes: 3 pilares; clampeadas por DNA si personalidad extrema (S104)
- Ocupación derivada: autoridad única de ToOccupation()
- Bloqueos por diales (S104-S121): aplicados via Clamp(dna, rules, o)
- Base y ocupación derivada son autoridad en S122+

## Vinculado a

[[Index/24 - Puente Tienda-Arena]], [[Index/22 - Bajada Nocturna y Linaje]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ArenaBases]], [[ArenaOrders]], [[ArenaOrderCatalog]], [[CreatureDNA]], [[WorldEnums]], [[ArenaRun]] (S124)
