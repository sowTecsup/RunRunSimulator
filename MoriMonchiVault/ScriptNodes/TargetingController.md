---
tags: [script, combat-prototype, ui]
---

# TargetingController.cs

**Ruta:** `CombatPrototype/TargetingController.cs`

**Responsabilidad:** Controla la interfaz de targeting del jugador: selección de unidad, habilidad, dirección (cardinales). **S84 NUEVO:** `SetDirection(cardinal)` para el drag (oriente durante arrastra). Flujo de slam (dos pasos). Plantilla de aterrizaje (Landing) se muestra como highlight diferente; validación usa GetLandingCell + IsLandingFree. Suscribe a BoardHighlighter para visualizar celdas afectadas y zonas de aterrizaje. Evento C# público `SelectionChanged` (Action sin parámetros, disparado en SelectUnit/SelectAbility/ClearSelection y cambios de slam pendiente). Propiedad `AwaitingSlamCell` (bool, true si pendingSlamTarget != null). RefreshHighlights pinta `Selection` en celda de unidad seleccionada si viva.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatSimState]], [[CombatAbilitySO]], [[PlannedAction]], [[AbilityTargeting]], [[BoardHighlighter]], [[CombatInputController]], [[CombatPrototypeHUD]]
