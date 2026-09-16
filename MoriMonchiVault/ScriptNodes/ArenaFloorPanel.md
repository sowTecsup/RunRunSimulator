---
tags: [script, ui, presentation, panel]
---

# ArenaFloorPanel.cs

**Ruta:** `World/Expedition/ArenaFloorPanel.cs`

**Responsabilidad:** Presentador UITK del panel de transición entre pisos. Muestra estado de vida del equipo, número de piso, tipo (Enemies/Buff), material asegurado y decisiones (Continuar → próximo piso, Retirarse → tienda con botín, Dar por Vencido si Lost). Lógica puramente de representación: lee `ArenaRunDirector.Run` y `FloorRecorded` para determinar qué UI mostrar. Hilos de control: Continuar (invoca director.Continue + callback onContinued), Retirarse/Dar por Vencido (director.Retreat/GiveUp). No es un MonoBehaviour; se instancia desde ExpeditionPanelUITK.

**S124:** Creado. Constructor toma VisualElement root, ArenaRunDirector director, Action onContinued. Query buttons por ID ("plan-floor", "btn-continue", etc). Refresh() re-renderiza según estado.

## Campos Privados (Serialización UITK)

| Campo | ID Query | Descripción |
|-------|----------|-------------|
| `floorLabel` | `plan-floor` | Label con piso, tipo, vida del equipo |
| `continueButton` | `btn-continue` | Avanza al próximo piso |
| `retreatButton` | `btn-retreat` | Retorno a tienda asegurando botín |
| `giveUpButton` | `btn-giveup` | Retorno a tienda tras derrota |
| `playButton` | `btn-play` | Inicia combate piso (controlado fuera de panel) |
| `roomButton`, `paletteButton`, `castButton`, etc | (otros) | Dev tools; ocultos en Refresh() |

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Dispose()` | Desuscribe buttons; llamar al destruir panel |
| `Refresh()` | Re-renderiza según Active/Lost/FloorRecorded; actualiza textos y visibilidad |

## Lógica de Rendering (Refresh)

### Estado Inicial: Antes de combate (`!FloorRecorded`)

```
floorLabel.text = "Piso {Floor} · {KindText}
{TeamHealthLine}"
playButton visible
retreatButton visible si Floor > 1
continueButton/giveUpButton hidden
```

### Estado Post-Combate: Después del combate (`FloorRecorded && !Lost`)

```
floorLabel.text = "Piso {Floor} terminado/superado · llevás {Material} material
{TeamHealthLine}"
continueButton visible: "Seguir → Piso {NextFloor}: {NextKind}" + advertencia si hay caídas
retreatButton visible: "Retirarse (asegura {Material})"
playButton hidden
```

### Estado Derrota: Rival ganó piso Enemies (`Lost == true`)

```
floorLabel.text = "Perdiste en el piso {Floor} · botín perdido
{TeamHealthLine}"
floorLabel CSS: add class "plan-floor--lost"
giveUpButton visible: "Volver a la tienda"
Otros buttons hidden
```

### Inactiva: Sin bajada activa

Todos los elementos ocultos (DisplayStyle.None).

## Helpers

| Método | Descripción |
|--------|-------------|
| `TeamHealthLine(run)` | Concatena nombres + vida de cada criatura; añade "(caída)" si IsDown |
| `AnyDown(run)` | True si alguna criatura tiene health <= 0 |
| `KindText(kind)` | "Enemigos" o "Buffo: +{BuffHealth} vida..." |

## Callbacks de Botones

| Botón | Handler |
|-------|---------|
| Continuar | `OnContinueClicked()` → director.Continue() → onContinued?.Invoke() |
| Retirarse | `OnRetreatClicked()` → director.Retreat() |
| Dar por Vencido | `OnGiveUpClicked()` → director.GiveUp() |

## Invariantes S124

- Refresh() es idempotente: llamar sin cambios = mismo resultado visual
- Vida mostrada es health actual (no delta): usuario ve puntos vivos
- "Riesgo: hay caídas" solo en post-combate si alguno down
- Lost anula todas las opciones excepto GiveUp
- Dev buttons siempre ocultos en Refresh()

## Vinculado a

[[Index/26 - Plan H0 - Bajada por pisos]] (S124)

## Conexiones

- [[ArenaRunDirector]] — lee Run/Active/FloorRecorded
- [[ExpeditionPanelUITK]] — instanciador y consumidor de Refresh()
- [[ArenaRun]] — source de health, Floor, Material, Lost
