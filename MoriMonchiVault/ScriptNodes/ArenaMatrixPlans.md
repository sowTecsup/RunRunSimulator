---
tags: [script, data, expedition, plans]
---

# ArenaMatrixPlans.cs

**Ruta:** `World/Expedition/ArenaMatrixPlans.cs`

**Responsabilidad:** Catalogo de planes (órdenes + personalidades) para simulaciones arena. Struct `ArenaMatrixTeam` (Name, Orders[], Boldness[], Sociability[]). Expone conjuntos: 10 arquetipos, 7 mejores, 3 perfiles puros. **S124:** Eliminados GuaV/CazV/RecV/SenV (Small variations).

## Struct ArenaMatrixTeam

- `string Name` — ej "Muralla"
- `ArenaOrders[] Orders` — 3 órdenes
- `float[] Boldness` — 3 valores [0,1]
- `float[] Sociability` — 3 valores [0,1]

## Conjuntos Predefinidos (S124)

**Plans10:** 10 equipos arquetipos (Guardián, Cazador, Recolector, Señuelo con loot Big)
- Muralla, Hormiguero, Jauria, Emboscada, Codicia, etc.

**Subset7:** Top 7 testing rápido
- Muralla, Fortin, Jauria, Hormiguero, Hormiguero Señuelo, Señuelos, Engaño

**Personalities3:** 3 perfiles puros extremos
- Osados (0.90), Tímidos (0.15), Balanceados (0.5)

## Cambios S124

- Eliminadas variantes Small (GuaV, CazV, RecV, SenV)
- 10+7+3 estructura simplificada
- Focus en arquetipos Big (centro)

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Team(name, orders)` | Factory con personalidades default |
| `Team(name, orders, bold, sociability)` | Factory custom |
| `Find(set, name)` | Busca por nombre |

## Invariantes

- Cada equipo: 3 órdenes + 3 boldness + 3 sociability
- Sincronizados por índice criatura

## Vinculado a

[[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ArenaMatrixDev]], [[ArenaOrders]]
