---
tags: [script, ui, uitk]
---

# TransactionPanelUITK.cs

**Ruta:** `UI/TransactionPanelUITK.cs`

**Responsabilidad:** Panel UITK (3 columnas) para negociar venta de MoriMochi con cliente. Muestra: cliente (nombre del archetype) | swatch BaseColor + nombre MM + género/edad | oferta en Dablones. Botones: Cancelar / Aceptar / Pedir más. Detecta abrir/cerrar por el estado real de `display` (Update poll de `IsShown()`) → emite `EnterNegotiating`/`ExitNegotiating` en NpcAgent.

**Propiedades y métodos:**
- `UIDocument document` → source asset con tree UITK.
- `IsShown()` — chequea si el root tiene `displayStyle != None`.
- `OnShown()` → llama `EnsureBound()` + `Refresh()` + `currentCustomer.EnterNegotiating()`.
- `OnHidden()` → si está en state `Negotiating`, sale con `ExitNegotiating()`.
- `Refresh()` — lee `CashRegister.Instance.CurrentCustomer` y actualiza labels: archetype, color swatch, nombre MM, género/edad, oferta actual. Habilita botones según `HasCounteredOnce`.
- Botones: `OnAccept` → `AcceptCurrentOffer()` + cierra. `OnCounter` → `TryCounterOffer()` (si es false, cierra). `OnReject` → `RejectByPlayer()` + cierra.

**Labels UITK esperados:**
- `customer-name`, `archetype`, `mm-swatch` (VisualElement color), `target-name`, `target-info`, `offer`, `accept` (Button), `counter` (Button), `reject` (Button).

**Vinculado a:** [[Index/08 - UI System]]

**Conexiones:** [[CashRegister]], [[NpcAgent]], [[UIManager]], [[CreatureDNA]], [[CustomerArchetypeSO]]
