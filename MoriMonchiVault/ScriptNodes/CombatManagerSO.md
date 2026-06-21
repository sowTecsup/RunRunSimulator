---
tags: [script, combat]
---

# CombatManagerSO.cs

**Ruta:** `Data/Combat/CombatManagerSO.cs`

**Responsabilidad:** Configuración global de combate (EvolutionChance, DeathChance, CritChance, CritMultiplier, MaxRounds, MaxFightCount, EnergyCostToQueue). SerializedScriptableObject sin `static Current`; lo posee y expone CombatController vía su getter `Config`.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatService]], [[AsyncCombatService]]
