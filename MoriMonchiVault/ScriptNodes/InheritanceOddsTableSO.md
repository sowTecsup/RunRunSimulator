---
tags: [script, genetics]
---

# InheritanceOddsTableSO.cs

**Ruta:** `Data/Breeding/InheritanceOddsTableSO.cs`

**Responsabilidad:** 5 slots (Parent, Grandparent, GreatGrandparent, Mutation, Base) con pesos para herencia genética. SerializedScriptableObject sin `static Current`; lo posee BreedingController, accedible vía `BreedingController.Instance.InheritanceOdds`. Método `Roll()` devuelve un Slot según los pesos normalizados. BreedDurationMinutes es solo display (real está hardcodeado en server).

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[BreedingController]], [[BreedingService]]
