---
tags: [script, system, customer]
---

# CustomerService.cs

**Ruta:** `Systems/Customers/CustomerService.cs`

**Responsabilidad:** Singleton apex del sistema de clientes. Posee refs serializados a `CustomerPricingSO` y `CustomerArchetypeDatabaseSO`. Instancia interna `ValuationHandler` y `NegotiationFlow`. Getters públicos para ambas SOs y handlers. API pública: `EstimateAverage(CreatureDNA)` (valuación sin arquitect específico, usa pricing base). Lifecycle: Awake setta Instance (destruye duplicados), OnDestroy limpia.

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[CustomerPricingSO]], [[CustomerArchetypeDatabaseSO]], [[ValuationHandler]], [[NegotiationFlow]], [[NpcController]], [[NpcAgent]]
