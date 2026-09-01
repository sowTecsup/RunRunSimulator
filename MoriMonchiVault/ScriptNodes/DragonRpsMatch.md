---
tags: [script, combate, dragon-rps, orchestration]
---

# DragonRpsMatch.cs

**Ruta:** `DragonRps/DragonRpsMatch.cs`

**Responsabilidad:** Orquestación de un combate completo. La clase `DragonRpsResult` captura resultado (golpes A/B, rondas, ganador). Método estático `Play(dragonA, dragonB, policyA, policyB, seed, log)` itera rondas hasta IsOver(). En cada ronda: ambos eligen simultáneamente, se resuelve con `ResolveRound()` (RPS + espejo + potencia + golpe mutuo si empatan), se loguea. `Winner()` desempata a mano. **Invariante:** las reglas de puntuación son inmutables (3 golpes gana, espejo parejo = golpe mutuo).

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsRules]], [[DragonRpsDragon]], [[DragonRpsSide]], [[DragonRpsBrain]], [[DragonRpsSession]], [[DragonRpsHarness]]
