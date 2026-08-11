---
tags: [script, ui]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatLineupBoard

**Ruta:** `UI/CombatLineupBoard.cs`

**Responsabilidad:** Renderiza una grilla 2-3-2 (7 slots: 0,1=Front · 2,3,4=Mid · 5,6=Back) para equipos de 3 MoriMonchis max. Dueña de las asignaciones CreatureDNA↔slot. Expone API de estado (Count, CanPlace, IsFull), posicionamiento (Place/RemoveAt/Clear), hit-test para drag (SlotAtPosition), highlight visual para feedback (SetDropHighlight/ClearHighlight), y sizing responsive (SetSlotSize). Sin lógica de drag propiamente dicha — CombatLineupUITK maneja PointerEvents y llama a Place/RemoveAt. **S57b:** Unidades colocadas en grilla usan retrato fotomatón vía [[MonchiPortraitUI]].Apply().

**Vinculado a:** [[Index/05 - UI System]], [[Index/13 - Combat Design Direction]]

**Conexiones:** [[CombatLineupUITK]], [[CreatureDNA]], [[Role]], [[CombatRow]], [[MonchiPortraitUI]]
