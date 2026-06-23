---
tags: [script, world, npc]
---

# NpcAgent.cs

**Ruta:** `World/Npc/NpcAgent.cs`

**Responsabilidad:** Cliente NPC en escena. Enum `NpcState` (Spawned, Wandering, InspectingDisplay, ApproachingRegister, Queueing, WaitingAtRegister, Negotiating, Leaving). Propiedades públicas: `Archetype`, `DisplayName` (nombre random asignado en Initialize vía `NpcNameBank`), `QueueWasFull` (bool, true si `TryReserveSlot` devolvió null), `TargetMM` (DNA elegida), `InitialOffer`/`CurrentOffer`, `HasCounteredOnce`, `State`, `CurrentDisplay` (StoreContainer donde inspecciona). Ciclo: wander entre vitrinas vía `TryReserveUsePoint` → inspect → decide compra → cola register (autorellena si hay cambios) → negocia → venta/rechazo → salida. Calcula mejor MM según valuation del archetype.

**Métodos públicos:**
- `Initialize(archetype, shopDisplays: IReadOnlyList<StoreContainer>, register, owner: NpcController)` — inicializa el agente.
- `CurrentDisplay` (propiedad) — referencia a `StoreContainer` donde está inspeccionando/reservó use point.
- `AcceptCurrentOffer()` — marca MM como Sold, estampa `TargetMM.SaleDate = DateTime.UtcNow`, suma dabloons al inventory, dispara eventos `CustomerSold`, `RegistryChanged`, `InventoryChanged`.
- `TryCounterOffer()` — calcula/evalúa contraoferta; true si aceptada, false si rechazada (← leaving).
- `RejectByPlayer()` → Leaving.
- `EnterNegotiating()` → state Negotiating.
- `ExitNegotiating()` → vuelve a WaitingAtRegister si estaba en Negotiating.

**Cambios principales:**
- Propiedades nuevas: `DisplayName` (string) y `QueueWasFull` (bool).
- `Initialize()` ahora asigna `DisplayName = NpcNameBank.GetRandomName()`.
- `AcceptCurrentOffer()` estampa `TargetMM.SaleDate = DateTime.UtcNow` (antes no lo hacía).
- **TickQueueing (rediseño):** ahora repolla `register.CurrentSlotOf(this)` cada frame. Si la posición ha cambiado (> 0.04 distancia cuadrada), actualiza `reservedQueueSlot` y destino del NavMeshAgent. Esto permite que clientes atrás avancen automáticamente cuando la fila se reorganiza (frente avanza, gente atrás sube).
- `TickApproachingRegister()`: si `TryReserveSlot()` devuelve null, estampa `QueueWasFull = true` antes de Leaving.

**Eventos:**
- `GameEvents.CustomerDecided(this, pick)` → tras decidir MM.
- `GameEvents.CustomerArrivedAtRegister(this)` → llega a front slot.
- `GameEvents.CustomerSold(this, mm, offer)` → aceptó.
- `GameEvents.CustomerLeft(this, wasSold)` → salió de escena.

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[NpcController]], [[NpcNameBank]], [[StoreContainer]], [[StoreDisplayRegistry]], [[CashRegister]], [[CustomerArchetypeSO]], [[CustomerService]], [[CreatureDNA]], [[GameEvents]], [[GameManager]]
