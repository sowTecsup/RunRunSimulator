---
tags: [script, combate, dragon-rps, testing]
---

# DragonRpsHarness.cs

**Ruta:** `DragonRps/DragonRpsHarness.cs`

**Responsabilidad:** Entry points de simulación y testing. `PlayVerbose(seed, powerPlayer, powerFoe)` corre un combate 1v1 con `Counting` vs `Counting`, log detallado, retorna string. `RunBalance(matches, powerA, powerB)` corre batallas (N iteraciones, Counting vs Random) y calcula winrate, frecuencia de empates y duración media — **así se validó que 82,5% de skill diferencia es binaria y "espejo parejo = golpe mutuo" mata empates** (ver Index/21 §2.3-2.4). **Cero dependencias de Unity.**

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsRules]], [[DragonRpsDragon]], [[DragonRpsMatch]]
