---
tags: [script, world, registry]
---

# StoreDisplayRegistry.cs

**Ruta:** `World/Containers/StoreDisplayRegistry.cs`

**Responsabilidad:** Registro estático singleton (espejo de `NeedStationRegistry`) de todos los `StoreContainer` activos en escena. Los containers se auto-registran en OnEnable/OnDisable.

**API estática:**
- `IReadOnlyList<StoreContainer> All` (propiedad) — lista de vitrinas activas.
- `Register(StoreContainer d)` — agrega si no está (no duplicados).
- `Unregister(StoreContainer d)` → remueve.

**Ciclo:**
- Cada `StoreContainer.OnEnable()` → `Register(this)`.
- Cada `StoreContainer.OnDisable()` → `Unregister(this)`.
- `NpcController.TrySpawnOne()` pasa `StoreDisplayRegistry.All` a `NpcAgent.Initialize()`.
- `NpcAgent.TickWandering()` itera `displays` (que apunta a `All`) para buscar displays con ocupantes.

**Invariantes:**
- Sin duplicados.
- Refleja estado vivo de la escena (no tiene caché manual; Add/Remove directo).
- Nunca es null (retorna lista vacía si no hay displays).

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[StoreContainer]], [[NpcAgent]], [[NpcController]]
