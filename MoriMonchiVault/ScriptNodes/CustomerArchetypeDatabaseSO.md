---
tags: [script, data, customer]
---

# CustomerArchetypeDatabaseSO.cs

**Ruta:** `Data/Customers/CustomerArchetypeDatabaseSO.cs`

**Responsabilidad:** Registro centralizado de arquetipos NPC. Posee lista `Archetypes`. API pública: `RandomArchetype()` (selecciona aleatorio, devuelve null si lista vacía con warning).

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[CustomerArchetypeSO]], [[CustomerService]], [[NpcController]]
