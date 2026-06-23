---
tags: [script, world, npc]
---

# NpcAgent.cs

**Ruta:** `World/Npc/NpcAgent.cs`

**Responsabilidad:** Cliente NPC en escena. Enum `NpcState` (Spawned, Wandering, InspectingDisplay, ApproachingRegister, Queueing, WaitingAtRegister, Negotiating, Leaving). Propiedades públicas: `Archetype`, `TargetMM` (DNA elegida), `InitialOffer`/`CurrentOffer`, `HasCounteredOnce`, `State`, `CurrentDisplay` (StoreContainer donde inspecciona). Ciclo: wander entre vitrinas vía `TryReserveUsePoint` → inspect → decide compra → cola register → negocia → venta/rechazo → salida. Calcula mejor MM según valuation del archetype.

**Métodos públicos:**
- `Initialize(archetype, shopDisplays: IReadOnlyList<StoreContainer>, register, owner: NpcController)` — inicializa el agente.
- `CurrentDisplay` (propiedad) — referencia a `StoreContainer` donde está inspeccionando/reservó use point.
- `AcceptCurrentOffer()` — marca MM como Sold, suma dabloons al inventory, dispara eventos.
- `TryCounterOffer()` — calcula/evalúa contraoferta; true si aceptada, false si rechazada (← leaving).
- `RejectByPlayer()` → Leaving.
- `EnterNegotiating()` → state Negotiating.
- `ExitNegotiating()` → vuelve a WaitingAtRegister si estaba en Negotiating.

**Cambios principales:**
- Recibe `displays` (IReadOnlyList<StoreContainer>) en `Initialize()` (no serializado, pasa `StoreDisplayRegistry.All`).
- `TickWandering()` usa `TryReserveUsePoint()` para reservar slot (no solo ocupantes).
- `TickInspecting()` usa `navAgent.remainingDistance` (no `Vector3.Distance`).
- `ReleaseDisplaySlot()` libera el use point reservado en transiciones (Wandering → re-wander, ApproachingRegister, Leaving, OnDisable).
- `reservedDisplay` + `currentDisplay` separados (reservado ≠ visitando).

**Eventos:**
- `GameEvents.CustomerDecided(this, pick)` → tras decidir MM.
- `GameEvents.CustomerArrivedAtRegister(this)` → llega a front slot.
- `GameEvents.CustomerSold(this, mm, offer)` → aceptó.
- `GameEvents.CustomerLeft(this, wasSold)` → salió de escena.

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[NpcController]], [[StoreContainer]], [[StoreDisplayRegistry]], [[CashRegister]], [[CustomerArchetypeSO]], [[CustomerService]], [[CreatureDNA]], [[GameEvents]], [[GameManager]]
