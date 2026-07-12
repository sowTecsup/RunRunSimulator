---
tags: [script, equipment]
---

# ItemUseEffect.cs

**Ruta:** `Data/Equipment/ItemUseEffect.cs`

**Responsabilidad:** Base abstracta para efectos de item con N usos consumibles en combate. Define `UseRule` (Always, SelfHpBelow) y umbral de HP. Subclases `HealUseEffect` y `DamageUseEffect` aplican acciones sobre contexto sin mutar estado directo. El contador de usos restantes vive en `Combatant` al runtime, no aquí (template puro).

**Vinculado a:** [[Index/03 - Combat System]]

**Conexiones:** [[EquipmentSO]], [[Combatant]], [[CombatService]], [[ICombatContext]]
