---
tags: [script, combat]
---

# CombatElements.cs

**Ruta:** `Systems/Combat/CombatElements.cs`

**Responsabilidad:** Motorizado de marcas elementales + reacciones 3v3. `ElementMark` (Element + AllySource bool). `AddMark()` impide duplicados del mismo (Element, AllySource); dos elementos distintos en la misma fuente → reacción vía `ReactionFor()` (8 reacciones aliadas + 8 ofensivas). Reacciones instantáneas (Cleanse/OverGrow/Leech/PisoTierra) se resuelven inmediato; armadas (Energizado/Vaporizado/etc) se añaden a `Combatant.States` (single-use, sin duplicados). Determinista: rolls vía `CombatRng` inyectado. Todos los checks de marcas filtran por fuente.

**Vinculado a:** [[Index/03 - Combat System]]

**Conexiones:** [[Combatant]], [[CombatManagerSO]], [[CombatRng]], [[Enums]]
