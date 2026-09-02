---
tags: [script, ui, helper]
---

# CreatureDisplay.cs

**Ruta:** `UI/CreatureDisplay.cs`

**Responsabilidad:** Helper estático con métodos de presentación compartidos para criaturas. Centraliza `StateOf()` localizado (sold/dead/breeding/cooldown/free), colores de rareza, visuales de iconos y bordes de rareza.

**S95:** Agregado caso `status.cooldown` en StateOf() para mostrar hora HH:mm cuando criatura está en cooldown post-combate.

**Vinculado a:** [[Index/05 - UI System]], [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[CreatureGridUITK]], [[DetailInfoTabPresenter]], [[CreatureGridView]], [[CreatureVisualUI]], [[DetailEquipTabPresenter]], [[CombatPickPresenter]]

