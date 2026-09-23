---
tags: [script, genetics, dev-tools]
---

# BreedingDevConsole.cs

**Ruta:** `Systems/Breeding/BreedingDevConsole.cs`

**Responsabilidad:** Dev component para testing cría. S131: API síncrona: `StartBreeding`, `TryHatch`, `CancelBreeding`. Duración mostrada en minutos de juego. Refs a GameManager + BreedingController. Solo para desarrollo.

## Buttons (S131 síncrono)

| Button | Acción |
|--------|--------|
| `Fill Random Breeders (DEV)` | Genera 2 criaturas adultas aleatorias para test |
| `Start Breeding (DEV)` | Llama `BreedingController.StartBreeding()` — retorna bool |
| `Try Hatch (DEV)` | Llama `BreedingController.TryHatch()` — retorna HatchResult |
| `Cancel Breeding (DEV)` | Llama `BreedingController.CancelBreeding()` |
| `Show Eggs (DEV)` | Lista huevos activos |

## Cambios S131

**API síncrona:**
```csharp
// Antes (async):
await BreedingController.Instance.StartBreedingAsync(motherID, fatherID);

// Ahora:
bool success = BreedingController.Instance.StartBreeding(motherID, fatherID);
Debug.Log($"Breeding started: {success}");
```

**Duración display:**
```csharp
// Muestra en minutos de juego
int minutes = BreedingController.Instance?.InheritanceOdds?.BreedDurationMinutes ?? 360;
Debug.Log($"Egg ready in {minutes} game minutes (~{minutes/60}h)");
```

**HatchResult handling:**
```csharp
HatchResult result = BreedingController.Instance.TryHatch(motherID, fatherID);
switch (result)
{
    case HatchResult.Hatched:
        Debug.Log("Hatched successfully!");
        break;
    case HatchResult.NotReady:
        Debug.Log("Egg not ready yet");
        break;
    // ...
}
```

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/09 - Dev Tools]]
- [[Index/09 - Active Context]]

## Conexiones

- [[BreedingController]] — API síncrona (S131)
- [[IncubationService]] (vía BreedingController)
- [[GameManager]], [[CreatureRegistrySO]]
- [[CreatureAvailability]]

## Notas (S131 HC-4)

- **Síncrono:** Sin async/await; resultados inmediatos.
- **Duración:** Minutos de juego (360 default = 6 horas si tiempo real 1:1).
- **HatchResult enum:** Hatched/NotReady/InsufficientMinerita/Invalid.
