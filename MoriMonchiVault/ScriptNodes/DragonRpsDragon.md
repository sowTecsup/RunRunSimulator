---
tags: [script, combate, dragon-rps, data]
---

# DragonRpsDragon.cs

**Ruta:** `DragonRps/DragonRpsDragon.cs`

**Responsabilidad:** Modelo de dragón con reparto de cartas por tipo (Counts: horns/wings/back) y potencia por tipo (Power: 1-3 entero). Factorías: `Standard(name, power)` genera el 2/2/2 canónico con potencia uniforme; `FromSpread(h,w,b)` crea dragones con repartos asimétrcos; `AllSpreads()` itera los 10 repartos posibles (para simulación). Contrato público: `BuildDeck()` materializa las 6 cartas.

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsRules]], [[DragonRpsSide]], [[DragonRpsMatch]], [[DragonRpsSession]], [[DragonRpsHarness]]
