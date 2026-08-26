---
tags: [script, combat-prototype, ui]
---

# CombatPrototypeHUD.cs

**Ruta:** `Systems/CombatPrototype/CombatPrototypeHUD.cs`

**Responsabilidad:** UI UITK que muestra tablero de combate: banner (instrucciones por fase), strip de beats, contador "Acciones N/2", tarjetas de jugadores (ticks, habilidades), botón EXECUTE. **Novedades S82:** UpdateActionBudget muestra "Acciones N/2" (N = TotalActions, 2 = MaxActions); marca naranja si usado >= presupuesto. **Cambios S83:** suscribe a targeting.SelectionChanged (OnEnable/OnDisable) para refrescar UI al cambiar selección. Nuevo `selectionLabel` bajo banner (texto-guía por estado: sin unidad → "Elegí un dragón"; con unidad, sin habilidad → "dragón — elegí plantilla"; con habilidad → "plantilla • esperando target" o "esperando slam"; si AwaitingSlamCell → "clic donde caer"). Tintado con Tint del dragón. Tarjetas de jugadores con backgroundColor (no solo borde) en selectedUnitId. Habilidades ahora pill (nombre, amarilla si seleccionada, gris con sufijo "— usada" si ya ejecutada en ese beat). Banner de Planning con 2da línea de controles de cámara (←/→ girar, rueda zoom).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatPrototypeManager]], [[TargetingController]], [[CombatSimState]], [[Choreography]]
