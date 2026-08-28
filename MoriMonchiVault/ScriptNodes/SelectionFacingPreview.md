---
tags: [script, combat-prototype, ui]
---

# SelectionFacingPreview.cs

**Ruta:** `CombatPrototype/SelectionFacingPreview.cs`

**Responsabilidad:** Controlador de vista previa en tiempo real del facing de la unidad seleccionada durante Planning. Suscribe a `TargetingController.SelectionChanged`. Cuando cambia la selección de dirección (via `CurrentDirection`), rotación de arrastrador (drag), o giro con Q/E, este componente aplica inmediatamente `CombatUnitView.SetFacingInstant()` al visual del dragón seleccionado para que el jugador vea cómo quedará orientado el golpe antes de confirmar. Caché de último facing aplicado (`_lastApplied`, `_lastUnitId`) para evitar rotaciones redundantes. Solo actúa en fase Planning.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[TargetingController]], [[CombatPrototypeManager]], [[CombatUnitView]]
