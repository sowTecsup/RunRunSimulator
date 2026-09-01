---
tags: [script, combate, dragon-rps, state]
---

# DragonRpsSide.cs

**Ruta:** `DragonRps/DragonRpsSide.cs`

**Responsabilidad:** Estado en-juego de un lado del combate: deck shuffleado, mano de 3, descarte público, contador de golpes. Métodos core: `Play(action)` mueve carta a descarte; `Draw()` repone desde deck; `RemainingByType()` calcula el conteo público (cuántas de cada tipo quedan) restando descarte del reparto original — **este es el motor de habilidad** del sistema, debe ser visible en UI. `CanAct` señala si hay mano o no.

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsRules]], [[DragonRpsDragon]], [[DragonRpsMatch]], [[DragonRpsSession]]
