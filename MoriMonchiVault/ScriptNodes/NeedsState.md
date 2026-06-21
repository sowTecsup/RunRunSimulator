---
tags: [script, genetics]
---

# NeedsState.cs

**Ruta:** `Data/NeedsState.cs`

**Responsabilidad:** Struct runtime Health/Energy/Affect incrustado en `CreatureDNA`. NO debe disparar `GameEvents.RegistryChanged`. Flushes solo en quit/pause.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[MoriMochiAgent]], [[NeedStation]], [[BreedingService]], [[CombatService]]
