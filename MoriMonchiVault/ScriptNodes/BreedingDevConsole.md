---
tags: [script, genetics]
---

# BreedingDevConsole.cs

**Ruta:** `Systems/Breeding/BreedingDevConsole.cs`

**Responsabilidad:** Componente dev (MonoBehaviour) para testing de cría local y async: Fill Random Breeders, Breed (síncrono, captura childID), Breed Timer (async StartBreedingAsync), Show Eggs / Hatch Egg / Cancel All Eggs (async: HatchAsync, CancelAllBreedingAsync). Despliega info de parejas activas, huevos incubando con timer. Refs serializadas [SerializeField] a GameManager + BreedingController. Solo para desarrollo.

**S129:** Criterio de disponibilidad (madre/padre vivos, no vendidos, no ocupados, bajo límite de crianzas) delegado a `CreatureAvailability.IsFree()`.

## Filtrado de Breeders (S129)

- Mother/Father: `CreatureAvailability.IsFree(dna) && dna.Gender == ... && dna.BreedCount < MaxBreedCount`
- IsFree verifica: viva, no vendida, no ocupada (BusyState == Free)

**Vinculado a:** [[Index/02 - Breeding]], [[Index/09 - Dev Tools]]

**Conexiones:** [[GameManager]], [[BreedingController]], [[BreedingContainer]], [[CreatureRegistrySO]], [[BreedingService]], [[AsyncBreedingService]], [[CreatureAvailability]]

**Uso en escena:** Adjuntar a un GameObject con acceso a GameManager + BreedingController. Inspect, configura refs y usa botones para test cría.
