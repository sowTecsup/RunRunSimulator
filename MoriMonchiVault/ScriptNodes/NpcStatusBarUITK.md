---
tags: [script, ui, uitk]
---

# NpcStatusBarUITK.cs

**Ruta:** `UI/NpcStatusBarUITK.cs`

**Responsabilidad:** Panel UITK que muestra lista en tiempo real de clientes activos + estado. Ref `UIDocument`, query container `npc-list`. OnEnable: suscribe a GameEvents (OnCustomerSpawned, OnCustomerLeft, OnCustomerArrivedAtRegister) con handlers que llaman Rebuild. Start: inicializa container, llama Rebuild. Update: acumula tiempo, refreshea cada 0.25s. Rebuild: limpia container, itera NpcController.Active, crea visual rows (name label + status label con enum switch que traduce NpcState a strings españoles: Wandering→"Mirando…", InspectingDisplay→"Pensando…", ApproachingRegister→"Yendo a la caja", Queueing→"Haciendo fila", WaitingAtRegister→"Esperando atención", Negotiating→"Negociando", Leaving→"Saliendo").

**Vinculado a:** [[Index/08 - UI System]]

**Conexiones:** [[NpcController]], [[NpcAgent]], [[GameEvents]]
