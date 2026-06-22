---
tags: [script, world, npc]
---

# NpcAgent.cs

**Ruta:** `World/Npc/NpcAgent.cs`

**Responsabilidad:** Componente que representa 1 cliente NPC en escena. Enum interno `NpcState` (Spawned, Wandering, InspectingDisplay, ApproachingRegister, Queueing, WaitingAtRegister, Negotiating, Leaving). Campos públicos (getters): `Archetype`, `TargetMM` (DNA elegida), `InitialOffer`/`CurrentOffer`, `HasCounteredOnce`, `State`. Requerimiento: NavMeshAgent. Update() tickea estados (TickWandering busca display con occupants, TickInspecting cronometra inspección, TickApproachingRegister pide slot al register, TickQueueing avanza hacia slot, TickWaiting timeout, TickLeaving navega exit). Transiciones disparan eventos: CustomerDecided, CustomerArrivedAtRegister, CustomerSold, CustomerLeft. Public API: `Initialize()`, `AcceptCurrentOffer()` (marca Sold, suma dabloons, dispara eventos), `TryCounterOffer()` (calcula/evalúa, devuelve bool), `RejectByPlayer()`, `EnterNegotiating()`, `ExitNegotiating()`.

**Vinculado a:** [[Index/04 - Customer System]]

**Conexiones:** [[NpcController]], [[StoreContainer]], [[CashRegister]], [[CustomerArchetypeSO]], [[CustomerService]], [[ValuationHandler]], [[NegotiationFlow]], [[CreatureDNA]], [[GameEvents]], [[GameManager]]
