---
tags: [script, combat-prototype, ui]
---

# CombatPrototypeHUD.cs

**Ruta:** `CombatPrototype/CombatPrototypeHUD.cs`

**Responsabilidad:** UI UITK orquestadora dividida en franjas de flujo. **S86 cambio:** Picking real UITK con `IsPointerOver(screenPosition)` en `RuntimePanelUtils.ScreenToPanel()` para consultar si el puntero está sobre el panel; input en `CombatInputController` chequea `overUi` antes de raycast. Banners por fase (Setup "DESPLIEGUE NOCTURNO", Spawning "REFUERZOS NOCTURNOS", Planning, Executing, EnemyTurn, Victory, Defeat) con instrucciones contextuales. Línea SEMILLA en Planning con countdown de ticks hasta germinación. Strip de beats (choreography). Contador de acciones "N/2" leyendo `Choreography.MaxActions`. Tarjetas de jugadores con minigrids de `AbilityCardVisuals` (minigrid 5×5 + tag de tipo). Botón EXECUTE. Suscripción a `targeting.SelectionChanged` para refresh en cada evento de selector; `selectionLabel` con guía por estado (dragón + plantilla + instrucciones de target). Tintado con `Tint` de dragón activo. Habilidades como pills (nombre, amarilla si seleccionada, gris con "✓" si ejecutada en beat actual).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatPrototypeManager]], [[TargetingController]], [[CombatSimState]], [[Choreography]], [[AbilityCardVisuals]], [[CombatInputController]]
