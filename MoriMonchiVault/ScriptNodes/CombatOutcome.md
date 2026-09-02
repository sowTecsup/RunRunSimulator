---
tags: [script, combate, data, struct]
---

# CombatOutcome.cs

**Ruta:** `Systems/Combat/CombatOutcome.cs`

**Responsabilidad:** Struct inmutable de resultado de combate. Campos: `Won` (victoria bool), `HitsPlayer`/`HitsRival` (golpes recibidos), `Rounds` (rondas jugadas), `MaterialGained` (material si victoria), `CooldownUntilTicks` (long ticks del cooldown si derrota).

**Vinculado a:** [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsService]], [[CombatResultPresenter]]
