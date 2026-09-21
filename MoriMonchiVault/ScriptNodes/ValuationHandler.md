---
tags: [script, system, customer]
---

# ValuationHandler.cs

**Ruta:** `Systems/Customers/ValuationHandler.cs`

**Responsabilidad:** Clase pura que calcula el precio de un MoriMochi. API: `Estimate(CreatureDNA, CustomerArchetypeSO, CustomerPricingSO)`. Suma de: base (por tier de partes), rarity de partes, breed count. Aplica multiplicadores del arquetipo y presupuesto final. **S75:** Sin term de combat winrate (demolición del combate). **S129:** Sin term de stats (eliminados de DNA).

## Factores de Precio

- **Base por tier:** Body/Horn/Back/Wing (4 partes con tiers)
- **Breed count:** Progresión por número de veces criada
- **Rarity:** Bonus por Tier2/3
- **Arquetipo weights:** Multiplicadores por Customer Archetype (si aplica)

## Cambios Históricos

- **S75:** Eliminado término de combat winrate (demolición del combate)
- **S129:** Eliminado término de stats (Constitution, Attack, Speed, Defense, Luck, Evasion borrados de DNA)

## Mantiene

- Bases por tier, breed count, rarity
- Multiplicadores de arquetipo

## Vinculado a

[[Index/06 - Customer System]]
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[CustomerPricingSO]], [[CustomerArchetypeSO]], [[CreatureDNA]]
