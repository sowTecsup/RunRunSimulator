---
tags: [script, data, customer]
---

# CustomerPricingSO.cs

**Ruta:** `Data/Customers/CustomerPricingSO.cs`

**Responsabilidad:** SO que centraliza los parámetros de valuación de MoriMonchis. Posee: `BasePricePerTier` (dict Tier→int), multiplicadores (`StatsMultiplier`, `BreedCountMultiplier`, `CombatWinrateMultiplier`, `TierMultiplier`), `RenegotiationStep` (0-1). Método botón `SeedDefaults()` para cargar valores base (Tier1=20, Tier2=50, Tier3=120).

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[ValuationHandler]], [[NegotiationFlow]], [[CustomerService]]
