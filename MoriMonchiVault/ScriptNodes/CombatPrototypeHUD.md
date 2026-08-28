---
tags: [script, combat-prototype, ui]
---

# CombatPrototypeHUD.cs

**Ruta:** `CombatPrototype/CombatPrototypeHUD.cs`

**Responsabilidad:** UI UITK orquestadora de 8 fases. **S86 cambio:** Picking real UITK con `IsPointerOver(screenPosition)` en `RuntimePanelUtils.ScreenToPanel()` para consultar si puntero está sobre panel; input chequea `overUi` antes de raycast. **S88 cambios UI**: Banners por fase con franja Planning expandida: base Planning + "\n⚔ ATAQUE ENEMIGO en N turnos" (calcula `manager.TurnsUntilEnemyAttack`), y si N==1 fondo rojo oscuro "⚠ ÚLTIMO TURNO". Reacting banner nuevo: "LOS GOLPEADOS SE REACOMODAN...". Tarjetas de jugadores: ancho 282px, color gris + "SIN PODERES" si no `HasAvailableAbility(unitId)`, mostrar poderes gastados con "✗" gris (lee `manager.IsAbilitySpent(unitId, abilityIndex)` HashSet). Botón EXECUTE/PASAR: si no hay acciones y `canPass` (no hay poderes), texto = "PASAR", sino "EXECUTE". Minigrids de habilidades con `AbilityCardVisuals.BuildAbilityMiniGrid` (auto-encuadre ±4, celdas 5px si >6 cols) + tag tipo. **Guarda anti-huérfano S88**: método `IsUiStale()` verifica si `bannerLabel == null` o `bannerLabel.panel != document.rootVisualElement.panel` (detecta panel reconstruido); `Update()` llama `Rebuild()` si stale (auto-curación). `Refresh()` también chequea stale primero. Suscripción a `targeting.SelectionChanged` para updates.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatPrototypeManager]], [[TargetingController]], [[CombatSimState]], [[Choreography]], [[AbilityCardVisuals]], [[CombatInputController]]
