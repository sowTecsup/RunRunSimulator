---
tags: [memory-bank, active, session]
---

# 09 - Active Context

**Session:** 2026-06-11 (Session 7)
**Focus:** Fix stutter ragdoll agent. StoreContainer. BreedingContainer + BreedingAffinityTableSO.

**Files Touched:**
- `World/MoriMochiAgent.cs`: fix stutter get-up (lerp pos+rot, Warp diferido)
- `World/MoriMochiContainer.cs`: Awake protected virtual para herencia
- `World/StoreContainer.cs` (new): vitrina restaura needs a restoreRate/s
- `World/BreedingContainer.cs` (new): corral timer dado + breed hibrido async/local
- `Data/BreedingAffinityTableSO.cs` (new): matriz 6x6 afinidad + Seed Defaults

**Next Session Goal:**
- BreedingController como singleton para BreedingContainer resuelva AsyncBreedingService + BreedingAffinityTableSO sin doble asignacion
- Cartelito visual pareja durante apareamiento y hatch
