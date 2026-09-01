---
tags: [script, combate, dragon-rps, ai]
---

# DragonRpsBrain.cs

**Ruta:** `DragonRps/DragonRpsBrain.cs`

**Responsabilidad:** Políticas de IA (enum `DragonRpsPolicy`): `Random` elige carta al azar; `Counting` calcula valor esperado contra el `RemainingByType()` del rival, descontando riesgo (cuán pesadas son las cartas que le quedan). Método público `Choose(policy, myState, foeState, rng)` es el entry point; la lógica de scoring interno valida cada candidato contra el descarte público del rival. **No toma decisiones sobre perks** (fase v2).

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsRules]], [[DragonRpsSide]], [[DragonRpsMatch]], [[DragonRpsSession]], [[DragonRpsHarness]]
