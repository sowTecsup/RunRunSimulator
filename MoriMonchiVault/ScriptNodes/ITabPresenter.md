---
tags: [script, ui, interface]
---

# ITabPresenter

**Ruta:** `UI/ITabPresenter.cs`

**Responsabilidad:** Contrato polimórfico para presenters de tabs. Métodos: `Enter()` (reset foco), `Navigate(h,v):bool` (retorna false = exit a tab bar), `Submit()`, `Cancel():bool` (retorna false = exit a tab bar), `ClearFocus()`, `Rebuild()` (sync data+UI), `Teardown()` (cleanup callbacks).

## Implementadores Vigentes

- `BreedingBreedTabPresenter` (Tab 0 Criar) — implementa `ITabPresenter` + campo `Busy` (async breed)
- `BreedingEggsTabPresenter` (Tab 1 Incubando) — implementa `ITabPresenter` + método `Tick()` (cuenta atrás)

## Métodos del Contrato

| Método | Descripción |
|--------|-------------|
| `Enter()` | Reset foco interior al abrir tab |
| `Navigate(h,v):bool` | Navega dentro tab; false = exit a tab bar |
| `Submit()` | Acción principal |
| `Cancel():bool` | Cancelar; false = exit a tab bar |
| `ClearFocus()` | Limpia foco |
| `Rebuild()` | Sync data con UI |
| `Teardown()` | Cleanup callbacks (suscripciones) |

## Patrón

Los presenters son clases planas sin estado excepto UI; reciben `Func<CreatureRegistrySO>` para evitar cacheo.

## Vinculado a

[[Index/05 - UI System]]

**Conexiones:** [[BreedingBreedTabPresenter]], [[BreedingEggsTabPresenter]], [[BreedingPanelUITK]]

