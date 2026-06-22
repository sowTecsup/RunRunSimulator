---
tags: [script, ui, uitk]
---

# TransactionPanelUITK.cs

**Ruta:** `UI/TransactionPanelUITK.cs`

**Responsabilidad:** Panel UITK que muestra negociación en directo con cliente en register. Ref `UIDocument`. Queries UITK: labels (`customer-name`, `archetype`, `target-name`, `offer`), botones (`accept`, `counter`, `reject`). OnEnable: suscribe a `CashRegister.OnCurrentCustomerChanged`, binds root, refreshes. OnDisable: desuscribe y cleans up. HandleCustomerChanged: actualiza currentCustomer y llama Refresh. Refresh: muestra datos del cliente actual (archetype display name, target MM name, current offer), habilita/deshabilita botones (counter deshabilitado si HasCounteredOnce). OnAccept: llama `AcceptCurrentOffer()`, cierra panel. OnCounter: llama `TryCounterOffer()`, si falla cierra, sino refreshes. OnReject: llama `RejectByPlayer()`, cierra panel.

**Vinculado a:** [[Index/08 - UI System]]

**Conexiones:** [[CashRegister]], [[NpcAgent]], [[UIManager]]
