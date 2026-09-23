---
tags: [script, ui, presenter]
---

# BreedingBreedTabPresenter.cs

**Ruta:** `UI/BreedingBreedTabPresenter.cs`

**Responsabilidad:** Presenter de Tab 0 "Criar" (seleccionar padre+madre, preview, iniciar breed). Implementa `ITabPresenter`. S131: Cría síncrona; borrados campos `Busy` y `SetBreedBusy()`. Método TryBreed ahora retorna bool (éxito) sin async.

## Cambios S131

**Borrados:**
- `public bool Busy` — cría ahora síncrona (no hay estado "en vuelo")
- `private void SetBreedBusy(bool)` — sin manejo de estado async

**TryBreed signature:**
```csharp
// Antes (async):
private async Task TryBreedAsync()
{
    Busy = true;
    await BreedingController.Instance.StartBreedingAsync(motherID, fatherID);
    Busy = false;
}

// Ahora (síncrono):
private void TryBreed()
{
    bool success = BreedingController.Instance.StartBreeding(motherID, fatherID);
    if (success)
    {
        UIManager.Toast("¡Crianza iniciada!");
        SaltarATabEggs();
    }
    else
    {
        UIManager.Toast("No se pudo iniciar la cría");
    }
}
```

**Flujo:**
1. Submit en breedButton → TryBreed()
2. TryBreed() → BreedingController.StartBreeding() (blocking)
3. Si ok: toast + saltar a Tab 1 (Eggs)
4. Si fallo: toast error, permanece en Tab 0

## Duración Display

```csharp
// Tab 0 preview muestra:
int hours = BreedingController.Instance?.InheritanceOdds?.BreedDurationMinutes / 60 ?? 6;
durationLabel.text = $"Incubación: {hours}h";
```

## Navegación S131

- SubFocus.Slots / SubFocus.FatherList / SubFocus.MotherList (sin cambios)
- Sin bloqueo de input (no hay async en vuelo)

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/09 - Active Context]]

## Conexiones

- [[ITabPresenter]]
- [[BreedingPanelUITK]] — anfitrión
- [[BreedingController]] — StartBreeding() síncrono
- [[IncubationService]] (vía BreedingController)

## Notas (S131 HC-4)

- **Sin Busy:** TryBreed() retorna inmediatamente (no async/await).
- **Duración horas:** Minutos / 60 para display (ej: 360 min = 6h).
- **Éxito/error:** Toast + salto de tab vs toast error + permanencia.
