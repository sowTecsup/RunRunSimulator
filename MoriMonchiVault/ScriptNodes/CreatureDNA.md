---
tags: [memory-bank, script, genetics]
---

# CreatureDNA.cs

**Ruta:** `Data/CreatureDNA.cs`

**Responsabilidad:** Modelo central: string genético (part IDs + color), identidad (UniqueID), linaje, género, personalidad, stats base, combat history, needs, busy state, `HomePenKey` (corral de cría asignado), `AgeDays` (días vividos desde BirthDate, deriva stage del NameTag). `ToStringID()`/`FromID()` son el contrato de red.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureStats]], [[NeedsState]], [[CombatRecord]], [[MoriMochiAgent]], [[BreedingService]], [[PartDatabaseSO]]
