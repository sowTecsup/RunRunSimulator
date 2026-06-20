---
tags: [memory-bank, script, genetics]
---

# CreatureDNA.cs

**Ruta:** `Data/CreatureDNA.cs`

**Responsabilidad:** Modelo central: string genético (`ToStringID()`/`FromID()`: `"BODYSHAPE-ARM-EYE-MOUTH-RRGGBB"`), identidad (`UniqueID` con timestamp), linaje (`MotherID`/`FatherID`/`ChildrenIDs`), género, personalidad, stats base (`BaseHP`/`BaseAttack`/`BaseSpeed`), combat history, needs, busy state, timers de cría (`BreedReadyAt`/`BreedPartnerID`/`HomePenKey`/`HomePenSlot`), `FurType` (metadata). `FromID()` parsea solo la parte genética; la deserialización JSON maneja el estado completo.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureStats]], [[NeedsState]], [[CombatRecord]], [[MoriMochiAgent]], [[BreedingService]], [[PartDatabaseSO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[FurTypeDatabaseSO]]
