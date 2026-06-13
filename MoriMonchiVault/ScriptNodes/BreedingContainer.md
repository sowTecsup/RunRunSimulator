---
tags: [memory-bank, script, genetics]
---

# BreedingContainer.cs

**Ruta:** `World/BreedingContainer.cs`

**Responsabilidad:** Corral de cría extendido de `MoriMochiContainer`. Implementa `IInteractable`. Auto-pair timer con dice roll (`affinity × diceChance`), restaura pasivamente necesidades de ocupantes, gestiona courtship visual (posa pareja frente a frente). Tap E para eclosionar huevo listo. Al retirar un Morimonchi cancela el emparejamiento. Tras recarga de escena, recupera criaturas en incubación vía corrutina `ReclaimBreedingOccupants`.

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiContainer]], [[MoriMochiAgent]], [[BreedingController]], [[BreedingAffinityTableSO]]
