---
tags: [script, data, customer]
---

# CustomerArchetypeSO.cs

**Ruta:** `Data/Customers/CustomerArchetypeSO.cs`

**Responsabilidad:** Define un arquetipo NPC único (persona en la tienda). `DisplayName`, `Icon`, `AgentPrefab`. Pesos de preferencia: `WeightBreed`, `WeightStats`, `WeightTier` (afectan valuación). `BudgetMultiplier`, `RenegotiationTolerance`. Comportamiento: `MinInspections`, `MaxInspections`, `InspectionDuration`, `WaitTimeoutSeconds`.

## Pesos (Reemplazados en S75)

- **WeightBreed** — Preferencia por criados más veces
- **WeightStats** — Preferencia por stats altos
- **WeightTier** — Preferencia por tiers altos
- **SIN:** WeightCombat (demolición del combate)

## Cambios en S75

- **ELIMINADO:** `WeightCombat`

## Vinculado a

- [[Index/06 - Customer System]]

**Conexiones:** [[ValuationHandler]], [[CustomerPricingSO]]
