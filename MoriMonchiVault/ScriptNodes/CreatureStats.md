---
tags: [script, stats]
---

# CreatureStats.cs

**Ruta:** `Systems/Stats/CreatureStats.cs`

**Responsabilidad:** Clase estática para calcular estadísticas efectivas de una criatura. `GetEffectiveStats(dna, db)` acumula stats base de DNA + bonificadores por tier de cada parte (BodyShape, Horn, Back, Wing). Devuelve `EffectiveStats` (Con, Atk, Spd, Def, Lck, Eva). `BaseHpCombatMultiplier = 5f`.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[EffectiveStats]], [[CreatureDatabaseSO]], [[BodyPart]]
