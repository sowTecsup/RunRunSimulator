---
tags: [script, combat-prototype, ui]
---

# EnemyBriefPanel.cs

**Ruta:** `Systems/CombatPrototype/EnemyBriefPanel.cs`

**Responsabilidad:** Panel UITK que muestra info enemigo al hover right-click: nombre, GuardTicks/FinisherTicks, líneas brief del EnemyDefinitionSO (BriefLines). Posición tooltip sigue mouse, con boundary check para no salir de pantalla. **Show(enemy, screenPosition)** verifica stale y reconstruye si es necesario (S88: `panel == null || panel.panel == null || (document.rootVisualElement != null && panel.panel != document.rootVisualElement.panel)` = guarda de reconstrucción ampliada).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[EnemyUnit]], [[EnemyDefinitionSO]], [[CombatPrototypeManager]], [[CombatInputController]]
