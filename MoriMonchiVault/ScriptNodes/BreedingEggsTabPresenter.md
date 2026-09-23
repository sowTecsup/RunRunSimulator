---
tags: [script, ui, presenter]
---

# BreedingEggsTabPresenter.cs

**Ruta:** `UI/BreedingEggsTabPresenter.cs`

**Responsabilidad:** Presenter de Tab "Incubando" (mostrar huevos en progreso como filas: madre 💗 padre, timer, botón Hatch). Implementa `ITabPresenter`. Almacena lista de `EggView` (por madre en progreso: ReadyAt, Row, Time label, Hatch button). Método público `Tick()` para cuenta atrás con throttle 1s. S131: Llama `IncubationService.TryHatch()` que retorna `HatchResult` enum.

## Datos UI

- `eggListView` (ScrollView) — filas de huevos
- Cada fila: "Madre 💗 Padre" + label tiempo + botón Hatch
- Botón mostrado solo cuando ReadyAt <= now

## Timer

- `lastTickSecond` — throttle 1s (evita recalcular cada frame)
- `Tick()` — recorre eggs, actualiza labels con tiempo restante (mm:ss o hh:mm:ss)

## Métodos de Interfaz ITabPresenter

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Enter()` | `void` | Resetea foco, ScrollTo primer huevo |
| `Navigate(h, v)` | `bool` | Mueve índice en lista; retorna false si exit |
| `Submit()` | `void` | Hatch el egg seleccionado si ready |
| `Cancel()` | `bool` | Retorna false (cierra tab) |
| `ClearFocus()` | `void` | Limpia clases visuales |
| `Rebuild()` | `void` | RebuildEggs (escanea registry por BusyReason.Breeding) |
| `Teardown()` | `void` | Limpia callbacks |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `RebuildEggs()` | Escanea registry, crea fila por madre en Breeding (muestra padre por BreedPartnerID) |
| `RefreshEggTimers()` | Calcula ReadyAt - now, formatea label, muestra botón si ready |
| `DoHatch(motherID, fatherID, btn)` | **(S131)** Llama `IncubationService.TryHatch()`, procesa HatchResult |

## DoHatch Flow (S131)

```csharp
private async void DoHatch(string motherID, string fatherID, Button btn)
{
    btn.SetEnabled(false);
    
    HatchResult result = incubationService.TryHatch(motherID, fatherID);
    
    switch (result)
    {
        case HatchResult.Hatched:
            // Animar eclosión, eliminar fila, toast éxito
            row.RemoveFromHierarchy();
            UIManager.Toast("¡Ha nacido!");
            break;
            
        case HatchResult.NotReady:
            // Toast: "No está listo"
            UIManager.Toast($"Aún falta tiempo");
            btn.SetEnabled(true);
            break;
            
        case HatchResult.InsufficientMinerita:
            // Toast: "Insuficiente Minerita"
            UIManager.Toast("Insuficiente Minerita");
            btn.SetEnabled(true);
            break;
            
        case HatchResult.Invalid:
            // Toast: "Error: estado inválido"
            UIManager.Toast("Error al eclosar");
            btn.SetEnabled(true);
            break;
    }
}
```

## Cambios S131

**Renombrado:**
- Campo: `asyncBreedingService` → `incubationService` (tipo `IncubationService`)

**Método DoHatch ahora:**
- Llama `IncubationService.TryHatch()` que retorna `HatchResult`
- Procesa result switch para animar/toastear según caso
- No es async (TryHatch es síncrono en S131)

**HatchResult valores:**
- `Hatched` = éxito, elimina fila
- `NotReady` = timer no vencido, re-habilita botón
- `InsufficientMinerita` = sin fondos, re-habilita botón
- `Invalid` = error de estado, re-habilita botón

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/09 - Active Context]]

## Conexiones

**UI:**
- [[ITabPresenter]] — interfaz
- [[BreedingPanelUITK]] — host del presenter

**Sistemas:**
- [[IncubationService]] — orquestador de cría (S131, antes AsyncBreedingService)
- [[CreatureRegistrySO]] — escanea para huevos
- [[GameClock]] — lee TotalMinutes para ReadyAt comparison
- [[Wallet]] — deducción de Minerita (en IncubationService)

## Notas (S131 HC-4)

- **HatchResult enum:** Nuevo en S131, permite UI distinguir entre fallos sin exceptions.
- **Síncrono:** TryHatch es now síncrono (no await), resultado inmediato.
- **Fila removal:** Si Hatched, elimina fila de lista (registry cambió via RegistryChanged event).
- **Toast messaging:** Sugere Loc.Tr para i18n de mensajes.
