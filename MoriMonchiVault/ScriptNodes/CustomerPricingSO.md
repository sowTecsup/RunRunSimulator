---
tags: [script, data, customer]
---

# CustomerPricingSO.cs

**Ruta:** `Data/Customers/CustomerPricingSO.cs`

**Responsabilidad:** SO que centraliza parámetros de valuación de MoriMonchis. `BasePricePerTier` (dict Tier→int), multiplicadores (`BreedCountMultiplier`, `TierMultiplier`), `RenegotiationStep`. **S75:** Sin `CombatWinrateMultiplier` (demolición del combate). **S129:** Sin `StatsMultiplier`.

## Cambios

- **S75:** Eliminado `CombatWinrateMultiplier`
- **S129:** Eliminado `StatsMultiplier`
- **MANTIENE:** BasePricePerTier, BreedCountMultiplier, TierMultiplier, RenegotiationStep

## Vinculado a

- [[Index/06 - Customer System]]

**Conexiones:** [[ValuationHandler]], [[CustomerArchetypeSO]]
