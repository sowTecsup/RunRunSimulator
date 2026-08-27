---
tags: [script, combat, data]
---

# SeedUnit.cs

**Ruta:** `CombatPrototype/SeedUnit.cs`

**Responsabilidad:** Unidad-objetivo inmóvil heredada de `CombatUnit`. Inmune a push/launch (rechazado en `CombatEffects.ApplyPush()`). Los ticks de la semilla representan vida del objetivo; al llegar a 0 = derrota. Germinación en el turno configurado (`CombatPrototypeManager.germinationTurn`) activa `ActionResolver.ResolveGermination()` y mata enemigos vivos.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatUnit]], [[CombatEffects]], [[ActionResolver]], [[CombatPrototypeManager]]
