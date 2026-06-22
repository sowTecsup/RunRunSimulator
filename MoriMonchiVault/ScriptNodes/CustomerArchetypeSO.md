---
tags: [script, data, customer]
---

# CustomerArchetypeSO.cs

**Ruta:** `Data/Customers/CustomerArchetypeSO.cs`

**Responsabilidad:** Defines un arquetipo NPC único (persona en la tienda). Campos: `DisplayName`, `Icon`, `AgentPrefab`. Pesos de preferencia: `WeightBreed`, `WeightCombat`, `WeightStats`, `WeightTier` (afectan valuación). `BudgetMultiplier` (presupuesto máximo del cliente). `RenegotiationTolerance` (0-1: probabilidad de aceptar contraoferta). Browsing: `MinInspections`, `MaxInspections`, `InspectionDuration` (segundos mirando cada display). `WaitTimeoutSeconds` (timeout antes de irse si espera demasiado).

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[ValuationHandler]], [[NegotiationFlow]], [[CustomerArchetypeDatabaseSO]], [[NpcController]], [[NpcAgent]]
