---
tags: [script, combat-prototype, ui]
---

# TargetingController.cs

**Ruta:** `Systems/CombatPrototype/TargetingController.cs`

**Responsabilidad:** Controla la interfaz de targeting del jugador: selección de unidad, habilidad, dirección (cardinales rotables), cursor. Maneja el flujo de slam (dos pasos). **Novedades S82:** plantilla de aterrizaje (Landing) se muestra como highlight diferente; validación usa GetLandingCell + IsLandingFree. Suscribe a BoardHighlighter para visualizar celdas afectadas y zonas de aterrizaje. **Cambios S83:** evento C# público `SelectionChanged` (Action sin parámetros, disparado en SelectUnit/SelectAbility/ClearSelection y cambios de slam pendiente). Propiedad `AwaitingSlamCell` (bool, true si pendingSlamTarget != null). RefreshHighlights pinta `Selection` en celda de unidad seleccionada si viva.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatSimState]], [[CombatAbilitySO]], [[PlannedAction]], [[AbilityTargeting]], [[BoardHighlighter]], [[CombatInputController]], [[CombatPrototypeHUD]]
