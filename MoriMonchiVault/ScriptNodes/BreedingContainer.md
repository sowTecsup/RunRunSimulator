---
tags: [script, genetics]
---

# BreedingContainer.cs

**Ruta:** `World/Containers/BreedingContainer.cs`

**Responsabilidad:** Corral de cría extendido de `MoriMochiContainer`. Implementa `IInteractable`. Auto-pair timer con dice roll (`affinity × diceChance`), restaura pasivamente necesidades de ocupantes, gestiona courtship visual (posa pareja frente a frente con gap dinámico = `courtGap` o `BodyRadius` combinado, el que sea mayor). Tap E para eclosionar huevo listo. Al retirar un Morimonchi cancela el emparejamiento. Tras recarga de escena, recupera criaturas en incubación vía corrutina `ReclaimBreedingOccupants`. Clave `penKey` derivada de `PlacedFurnitureMarker.AnchorCell` (x_y). Registro estático `TryGet(key)` para lookup por `HomePenKey`. `ReclaimDirect(agent)` para que `MoriMochiSpawner` coloque criaturas directamente. `LaunchPoint` (centro + `launchHeight`) desde donde se lanzan las crías recién nacidas. Al emparejar, estampa `HomePenKey` en ambos padres.

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiContainer]], [[MoriMochiAgent]], [[BreedingController]], [[BreedingAffinityTableSO]]
