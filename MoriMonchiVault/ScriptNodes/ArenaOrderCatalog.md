---
tags: [script, data, expedition, catalog]
---

# ArenaOrderCatalog.cs

**Ruta:** `Data/Expedition/ArenaOrderCatalog.cs`

**Responsabilidad:** Catálogo estático de textos y etiquetas para órdenes, arquetipos y equipos. Traduce enums a strings legibles, genera nombres de arquetipos, describe estrategias. **S122:** Delega rol/lectura a ArenaBases. **S124:** `PersonalityName(dna)` y `RivalRead(dna)` sin parámetro rules (simplificación).

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `string ChoiceLabel(OrderPillar p, int choice)` | Etiqueta legible |
| `string ArchetypeShort(ArenaOrders o)` | "Guardián", "Cazador", "Recolector", "Señuelo" |
| `string ArchetypeName(ArenaOrders o)` | Nombre + ubicación |
| `string ArchetypeDescription(ArenaOrders o)` | Descripción estratégica |
| `string CounterHint(ArenaOrders o)` | Cómo counters actúan |
| `string TeamPlanName(IReadOnlyList<ArenaOrders> orders)` | Estrategia equipo |
| `string PersonalityName(CreatureDNA dna)` | **(S124)** ArenaBases.RoleName; sin rules |
| `string RivalRead(CreatureDNA dna)` | **(S124)** ArenaBases.RivalRead; sin rules |
| `string RoomText(ArenaRoomRead read)` | Descripción sala |

## Cambios S124

- `PersonalityName(dna)` — eliminado parámetro rules
- `RivalRead(dna)` — eliminado parámetro rules

## Vinculado a

[[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ArenaBases]], [[ArenaOrders]], [[ArenaPlanPanel]]
