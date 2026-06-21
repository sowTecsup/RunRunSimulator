---
tags: [script, genetics]
---

# BreedingAffinityTableSO.cs

**Ruta:** `Data/Breeding/BreedingAffinityTableSO.cs`

**Responsabilidad:** Matriz simétrica (Personality, Personality) → float de afinidad (0..1). SerializedScriptableObject con OdinSerialize Dictionary. Sin `static Current`; lo posee BreedingController, accedible vía `BreedingController.Instance.GetAffinity()`. Devuelve 0.5 por defecto si falta par. Botón SeedDefaults llena todos los 21 pares.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[BreedingController]], [[BreedingService]]
