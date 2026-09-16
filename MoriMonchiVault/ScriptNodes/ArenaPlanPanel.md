---
tags: [script, world, ui, uitk, expedition]
---

# ArenaPlanPanel.cs

**Ruta:** `World/Expedition/ArenaPlanPanel.cs`

**Responsabilidad:** Panel UITK de planificación pre-ronda y resultado post-ronda. Permite elegir órdenes (S104) por criatura, muestra sala. **S119:** ReturnToStore() → ExpeditionHandoff. **S120:** Botón retorno visible solo si CameFromStore. **S122:** Panel muestra dos píldoras de base abiertas + razón de base cerrada (no diales bloqueados).

**Métodos públicos:**
- `void Update()` — gestiona visible/oculto, delay resultado
- `void ReturnToStore()` — **(S119)** ExpeditionHandoff.ReturnToStore(lastResult)

**UI Structure (S104-S107-S111-S122):**
- `plan-root`
  - `plan-header` — "Forma · sala NNNN · entrada · paleta"
  - `cast-list` (ScrollView) — tarjetas criaturas player
    - Cada `cast-card`:
      - Nombre + stats
      - **S122:** Fila BASE: tres píldoras (12 px); dos abiertas (clickeables), una cerrada deshabilitada con clase `pill--closed` y label `plan-row__lock` con `ClosedReason` debajo
      - Lectura: "Rol · Base1 o Base2"
      - Contra sugerido
  - `plan-rival` — naturaleza + arquetipos rivales
  - Botones: play, **return** (S119, visible solo si **CameFromStore** S120)

**Campos Privados (S119):**
- `lastResult` (ExpeditionResult?) — resultado de última ronda

**Métodos Privados (S122):**
- `void BuildCard(...)` → crea tarjeta. **S122:** Fila BASE con píldoras (role abre 2 bases, mapea a órdenes)
- `void ChooseBase(Card, ArenaBase)` → **(S122)** `ArenaBases.ToOrders(role, base)` → sandbox.SetPlayerOrders()
- `void RefreshRivalLine()` → **(S122)** Muestra rival como "Rol · Base1 o Base2" (lee vía ArenaBases.RivalRead())

**Flujo Pilares → Bases (S122):**
1. BuildCard: role = dna.Role (desde Roster Entry)
2. Dos bases abiertas: ArenaBases.OpenAt(role, 0/1)
3. Una base cerrada: ArenaBases.Closed(role), gris con tooltip ClosedReason()
4. Clic pilar → ChooseBase() → ArenaBases.ToOrders(role, base) → SetPlayerOrders()
5. Rival leído como: ArenaBases.RivalRead(role) = "Protector · Territorio o Rebusque"

**S120-S122:**
- **S120:** Botón "Volver" visible solo si `ExpeditionHandoff.CameFromStore`. Ya en S119 pero confirmado S120.
- **S121:** Sin cambios en panel (energía gastada en retorno).
- **S122:** Pilares → Bases: `ArenaBase` enum nuevoenum nuevo, ArenaBases.ToOrders/RivalRead. Dos píldoras abiertas, una gris cerrada con razón. Diales no bloquean.

**Invariantes:**
- Botón retorno solo visible desde tienda (S120)
- Base abierta → órdenes concretas (S122)
- Lectura rival por rol+bases (S122)

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]] (S120-S122), [[Index/22 - Bajada Nocturna y Linaje]] (S122)

**Conexiones:** [[ArenaSandbox]], [[ArenaRound]], [[ArenaBases]], [[ArenaOrderRules]], [[ExpeditionHandoff]], [[ArenaPaletteApplier]]
