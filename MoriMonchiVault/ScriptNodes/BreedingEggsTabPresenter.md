---
tags: [script, ui, presenter]
---

# BreedingEggsTabPresenter.cs

**Ruta:** `UI/BreedingEggsTabPresenter.cs`

**Responsabilidad (S54):** Presenter de Tab 1 "Incubando" (mostrar huevos en progreso como filas: madre 💗 padre, timer, botón Hatch). Implementa `ITabPresenter`. Almacena lista de `EggView` (por madre en progreso: ReadyAt, Row, Time label, Hatch button). Método público `Tick()` adicional (no en interfaz) — cuenta atrás con throttle 1s (core llama solo si tab visible en Update).

**Datos UI:**
- `eggListView` (ScrollView con filas de huevos)
- Cada fila: "Madre 💗 Padre" + label tiempo + botón Hatch (oculto hasta ReadyAt)

**Timer:**
- `lastTickSecond` — evita recalcular timers cada frame (throttle 1s via `DateTime.UtcNow.Second`)
- `Tick()` — recorre eggs, actualiza labels con tiempo restante (hh:mm:ss o mm:ss), muestra botón cuando ReadyAt <= now

**Navegación:**
- v up/down navega lista, v-up sale del tab (retorna false)
- Submit sobre egg ready → dispara `DoHatch()` async si ReadyAt <= now

**Métodos de interfaz:**
- `Enter()` — resetea foco, ScrollTo primer huevo
- `Navigate(h,v):bool` — mueve índice en lista, retorna false si exit
- `Submit()` — hatch el egg seleccionado si ready
- `Cancel():bool` — retorna false (cierra tab)
- `ClearFocus()` — limpia clases visuales
- `Rebuild()` — rebuildEggs (escanea registry por criaturas en Breeding)
- `Teardown()` — sin callbacks de botones (se recrean cada rebuild)

**Métodos privados:**
- `RebuildEggs()` — escanea registry, crea fila por madre en `BusyReason.Breeding` (muestra padre por BreedPartnerID)
- `RefreshEggTimers()` — calcula `ReadyAt - now`, formatea label (mm:ss o hh:mm:ss), muestra botón si ready
- `DoHatch(motherId, btn)` — await `asyncBreedingService.HatchAsync()`, grisea botón durante, restaura si no_ready (btn aún attached) o lo deja orphaned si éxito

**Conexiones:** [[ITabPresenter]], [[BreedingPanelUITK]], [[AsyncBreedingService]], [[CreatureRegistrySO]]
