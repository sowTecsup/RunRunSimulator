---
tags: [script, world, npc]
---

# CashRegister.cs

**Ruta:** `World/Containers/CashRegister.cs`

**Responsabilidad:** Singleton que gestiona la cola de clientes (sistema BFS 3-ario: root → Back/Left/Right). Posee ref `queueRoot` (transform raíz de la cola), parámetros `slotSpacing`, `maxQueueDepth`. Clase interna `QueueSlotNode` almacena posición local, ocupante NpcAgent, hijos (Back/Left/Right), depth. API: `TryReserveSlot(NpcAgent)` → Vector3 (posición mundial del slot) o null si no hay espacio. `ReleaseSlot(NpcAgent)` → limpia y avanza cola (reordena ocupantes del árbol). `IsFrontSlot(NpcAgent)` → bool. `CurrentSlotOf(NpcAgent)` → Vector3?. Getters: `CurrentCustomer` (root.Occupant), `QueueRootPos`. Evento: `OnCurrentCustomerChanged(NpcAgent)` disparado cuando cambia el cliente en root.

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[NpcAgent]], [[TransactionPanelUITK]], [[NpcController]]
