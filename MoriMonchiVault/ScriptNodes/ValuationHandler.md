---
tags: [script, system, customer]
---

# ValuationHandler.cs

**Ruta:** `Systems/Customers/ValuationHandler.cs`

**Responsabilidad:** Clase pura (serializable pero sin MonoBehaviour) que calcula el precio de un MoriMochi. API: `Estimate(CreatureDNA, CustomerArchetypeSO, CustomerPricingSO)`. Suma base (4 tiers) + bonos derivados: stats (suma de los 6 base: Constitution + Attack + Speed + Defense + Luck + Evasion), breed count (historial), combat winrate, tier bonus. Aplica pesos del arquetipo si existe, multiplicadores de pricing, y presupuesto final. Devuelve int >= 0.

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[CustomerPricingSO]], [[CustomerArchetypeSO]], [[CreatureDNA]], [[CustomerService]], [[NpcAgent]], [[Enums]]
