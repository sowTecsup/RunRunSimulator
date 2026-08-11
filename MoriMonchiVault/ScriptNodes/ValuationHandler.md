---
tags: [script, system, customer]
---

# ValuationHandler.cs

**Ruta:** `Systems/Customers/ValuationHandler.cs`

**Responsabilidad:** Clase pura que calcula el precio de un MoriMochi. API: `Estimate(CreatureDNA, CustomerArchetypeSO, CustomerPricingSO)`. Suma de: base (por tier de partes), stats base (Constitution + Attack + Speed), rarity de partes, breed count. Aplica multiplicadores del arquetipo y presupuesto final. **S75:** Sin term de combat winrate (demolición del combate).

## Factores de Precio

- **Base por tier:** Body/Horn/Back/Wing (4 partes con tiers)
- **Stats:** Suma de Constitution + Attack + Speed + Defense + Luck + Evasion
- **Breed count:** Progresión por número de veces criada
- **Arqueipo weights:** Multiplicadores por Customer Archetype (si aplica)

## Cambios en S75

- **ELIMINADO:** Término de combat winrate
- **MANTIENE:** Bases, stats, breed count, rarity, archetype weights

## Vinculado a

- [[Index/06 - Customer System]]

**Conexiones:** [[CustomerPricingSO]], [[CustomerArchetypeSO]], [[CreatureDNA]]
