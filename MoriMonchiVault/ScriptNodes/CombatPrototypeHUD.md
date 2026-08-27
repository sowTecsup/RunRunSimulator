---
tags: [script, combat-prototype, ui]
---

# CombatPrototypeHUD.cs

**Ruta:** `CombatPrototype/CombatPrototypeHUD.cs`

**Responsabilidad:** UI UITK orquestadora. Banners por fase (Setup "DESPLIEGUE NOCTURNO", Spawning "REFUERZOS NOCTURNOS", Planning, Executing, EnemyTurn, Victory, Defeat) con instrucciones contextuales. Línea SEMILLA en Planning con countdown de ticks hasta germinación. Strip de beats (choreography). Contador de acciones "N/2" leyendo `Choreography.MaxActions` (S85). Tarjetas de jugadores con minigrids de `AbilityCardVisuals` (minigrid 5×5 + tag de tipo). Botón EXECUTE. Suscripción a `targeting.SelectionChanged` para refresh; `selectionLabel` con guía por estado (dragón + plantilla + target). Tintado con `Tint` de dragón activo. Habilidades como pills (nombre, amarilla si seleccionada, gris con "— usada" si ejecutada en beat).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatPrototypeManager]], [[TargetingController]], [[CombatSimState]], [[Choreography]], [[AbilityCardVisuals]]
