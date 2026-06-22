---
tags: [script, system, customer]
---

# NegotiationFlow.cs

**Ruta:** `Systems/Customers/NegotiationFlow.cs`

**Responsabilidad:** Clase pura (serializable, sin MonoBehaviour) que gestiona el flujo de negociación (cliente vs. jugador). Enum interno `NegotiationResult` (Accept/Reject). API: `ComputeCounter(int, CustomerPricingSO)` calcula contraoferta aumentando el precio en `RenegotiationStep`. `EvaluateCounter(CustomerArchetypeSO)` devuelve Accept/Reject basado en `RenegotiationTolerance` del arquetipo (probabilidad).

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[CustomerPricingSO]], [[CustomerArchetypeSO]], [[CustomerService]], [[NpcAgent]]
