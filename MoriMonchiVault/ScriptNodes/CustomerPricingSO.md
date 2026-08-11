---
tags: [script, data, customer]
---

# CustomerPricingSO.cs

**Ruta:** `Data/Customers/CustomerPricingSO.cs`

**Responsabilidad:** SO que centraliza parámetros de valuación de MoriMonchis. `BasePricePerTier` (dict Tier→int), multiplicadores (`StatsMultiplier`, `BreedCountMultiplier`, `TierMultiplier`), `RenegotiationStep`. **S75:** Sin `CombatWinrateMultiplier` (demolición del combate).

## Cambios en S75

- **ELIMINADO:** `CombatWinrateMultiplier`
- **MANTIENE:** BasePricePerTier, StatsMultiplier, BreedCountMultiplier, TierMultiplier

## Vinculado a

- [[Index/06 - Customer System]]

**Conexiones:** [[ValuationHandler]], [[CustomerArchetypeSO]]
