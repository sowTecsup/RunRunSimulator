---
tags: [script, ui, breeding]
---

# BreedingPanelUITK.cs

**Ruta:** `UI/BreedingPanelUITK.cs`

**Responsabilidad:** Panel modal de crianza (2 tabs: Criar/Incubando). Orquesta `BreedingBreedTabPresenter` (Tab 0: seleccionar padre+madre, preview, breed) y `BreedingEggsTabPresenter` (Tab 1: huevos + timers + hatch). Implementa `IUINavigable`. S131: Recibe `IncubationService` (antes `asyncBreedingService`). Borrados campos `breedBusy` y `SetBreedBusy()` (cría ahora síncrona). Duración se muestra en horas de juego.

## Estructura (S54 Composición)

- `BreedingPanelUITK.cs` — núcleo MonoBehaviour: lifecycle, tab navigation
- `ITabPresenter.cs` — contrato polimórfico (Enter/Navigate/Submit/Cancel/ClearFocus/Rebuild/Teardown)
- `BreedingBreedTabPresenter.cs` — Tab 0: selección, preview, breed
- `BreedingEggsTabPresenter.cs` — Tab 1: lista, timers, hatch

## Cambios S131

**Renombrado:**
```csharp
[FormerlySerializedAs("asyncBreedingService")]
[SerializeField] private IncubationService incubationService;
```

**Borrados:**
- `bool breedBusy` — cría síncrona no necesita flag async
- `void SetBreedBusy(bool)` — sin cambios de estado global
- Callbacks await async → ahora síncrono

**Propósito:** IncubationService.StartBreeding() es blocking local; no hay estado "en vuelo".

## Duración Display

```csharp
int breedDurationMinutes = BreedingController.Instance?.InheritanceOdds?.BreedDurationMinutes ?? 360;
int hours = breedDurationMinutes / 60;
label.text = $"{hours}h";  // Ej: 360 min = 6 horas
```

## Update (Tick EggsTab)

```csharp
void Update()
{
    if (currentTabIndex != 1) return;  // Solo si tab 1 visible
    eggsPresenter.Tick();
}
```

## Métodos Públicos (IUINavigable)

| Método | Descripción |
|--------|-------------|
| `Enter()` | Entra panel, inicia tab 0 |
| `Navigate(h, v)` | Navega entre tabs o content |
| `Submit()` | Delega a presenter actual |
| `Cancel()` | Cierra panel |

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/05 - UI System]]
- [[Index/09 - Active Context]]

## Conexiones

**Presenters:**
- [[BreedingBreedTabPresenter]] — Tab 0
- [[BreedingEggsTabPresenter]] — Tab 1

**Sistemas:**
- [[IncubationService]] (S131, antes AsyncBreedingService)
- [[BreedingController]]
- [[GameEvents]]
- [[UIManager]]

## Notas (S131 HC-4)

- **Sin breedBusy:** IncubationService.StartBreeding() es síncrono, retorna bool inmediatamente.
- **Duración horas:** BreedDurationMinutes (360 default) / 60 = 6 horas.
- **Presenters desacoplados:** No conocen UI global; solo reciben callback para rebuild.
