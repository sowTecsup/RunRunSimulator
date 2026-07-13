---
tags: [script, ui, combat, visualizer]
---

# CombatVisualizerPanelUITK.cs

**Ruta:** `UI/CombatVisualizerPanelUITK.cs`

**Responsabilidad:** Panel UITK (screen-space) del visualizer: header de turno (actual/total), log de combate en cartas, controles de reproducción, y botón nuevo para minimizar/expandir log. Se reconstruye entero desde `CombatVisualEvents.OnPanelState` (single source of truth → soporta rewind). `OnVisualCombatStart` solo lo hace visible. **S42:** Botón `btn-log-toggle` (▼/▲) para minimizar/expandir ScrollView de log, layout v2 con log abajo y controles compactos a la izquierda.

**Log en cartas con scroll:** `RebuildLog` vacía `log-container` (dentro de un `ScrollView` `log-scroll`) y crea una **carta por entrada** (`CombatVisualLogLine`), con clase USS por `Kind` (`log-versus`/`log-hit`/`log-crit`/`log-death`/`log-result`) → color de fondo + borde izquierdo. Los nombres/daño vienen ya con rich-text de color (azul local / rojo oponente / rojo daño). La caja de log tiene **tamaño fijo** (`height` en USS) y el ScrollView navega adentro, auto-scrolleando al último turno.

**Controles (en el UXML, llaman al servicio singleton):** 
- `btn-back` → `Back()` (retrocede turno)
- `btn-play` → `TogglePlay()` (texto ▶/❚❚ según `IsAuto`)
- `btn-next` → `Next()` (avanza turno)
- `speed-slider` (0.25–4) → `SetSpeed()`
- **S42 NEW:** `btn-log-toggle` → `ToggleLog()` (texto ▼/▲, toggle `log-scroll.style.display`)

`btn-back`/`btn-next` se habilitan según `CanBack`/`CanForward`. Llama a `CombatVisualizerService.Instance` (servicio explícito — permitido, no es Find).

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | Ref al UIDocument que contiene el panel |

## Campos Internos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `root` | `VisualElement` | Raíz del documento |
| `turnLabel` | `Label` | "Turno X / Y" o "Final" o "Empate" |
| `logScroll` | `ScrollView` | ScrollView del log (S42: ocultable) |
| `logContainer` | `VisualElement` | Contenedor de cartas de log |
| `backButton` | `Button` | Botón retroceso |
| `playButton` | `Button` | Botón play/pausa |
| `nextButton` | `Button` | Botón avance |
| `speedSlider` | `Slider` | Slider velocidad (0.25–4) |
| `speedLabel` | `Label` | Texto "Velocidad x1.0" |
| `logToggleButton` | `Button` | **S42 NEW** Botón ▼/▲ para toggle log |
| `logExpanded` | `bool` | **S42 NEW** Estado del log (true = visible) |

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe a `OnVisualCombatStart`, `OnPanelState` |
| `OnDisable()` | Desuscribe |
| `Start()` | Inicializa referencias y wiring de botones/slider, `ApplyLogExpanded()` |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `HandleStart(CombatVisualContext ctx)` | `OnVisualCombatStart` handler — `SetVisible(true)` |
| `HandleState(CombatVisualPanelState st)` | `OnPanelState` handler — actualiza turno, log, botones, speed |
| `RebuildLog(CombatVisualLogLine[] lines)` | Limpia `logContainer`, crea cartas por entrada con clase USS |
| `ToggleLog()` | **S42 NEW** Toggle `logExpanded`, llamar `ApplyLogExpanded()` |
| `ApplyLogExpanded()` | **S42 NEW** Aplica `logScroll.style.display = logExpanded ? Flex : None`, actualiza texto botón ▼/▲ |
| `KindClass(CombatVisualLogKind kind)` | Retorna clase USS por kind (log-versus, log-hit, etc.) |
| `SetVisible(bool v)` | Muestra/oculta el panel root |

## Flujo de HandleState (S42 ACTUALIZADO)

```csharp
private void HandleState(CombatVisualPanelState st)
{
    SetVisible(true);

    if (turnLabel != null)
        turnLabel.text = st.Ended
            ? (st.IsDraw ? "Empate" : "Final")
            : (st.TotalTurns > 0 ? $"Turno {st.TurnNumber} / {st.TotalTurns}" : $"Turno {st.TurnNumber}");

    RebuildLog(st.Log);

    backButton?.SetEnabled(st.CanBack);
    nextButton?.SetEnabled(st.CanForward);
    if (playButton != null) playButton.text = st.IsAuto ? "❚❚" : "▶";
    if (speedLabel != null) speedLabel.text = $"Velocidad x{st.Speed:0.0}";
    if (speedSlider != null && !Mathf.Approximately(speedSlider.value, st.Speed))
        speedSlider.SetValueWithoutNotify(st.Speed);
}
```

**S42:** TurnNumber ahora es ActionIndex (contador de acciones, 0 en head, incrementa por turno jugable).

## RebuildLog

```csharp
private void RebuildLog(CombatVisualLogLine[] lines)
{
    if (logContainer == null) return;
    logContainer.Clear();
    if (lines == null) return;

    foreach (var line in lines)
    {
        var card = new VisualElement();
        card.AddToClassList("log-card");
        card.AddToClassList(KindClass(line.Kind));
        var label = new Label(line.Text);
        label.AddToClassList("log-text");
        card.Add(label);
        logContainer.Add(card);
    }

    if (logScroll != null)
        logScroll.schedule.Execute(() => logScroll.scrollOffset = new Vector2(0f, float.MaxValue)).ExecuteLater(1);
}
```

**Flujo:**
1. Limpia `logContainer`
2. Por cada línea del log, crea:
   - VisualElement "log-card" con clases "log-card" + clase por kind
   - Label hijo con clase "log-text"
   - Agrega al container
3. Auto-scroll al fondo (1 frame delay para que el layout se recalcule)

## ToggleLog (S42 NEW)

```csharp
private void ToggleLog()
{
    logExpanded = !logExpanded;
    ApplyLogExpanded();
}

private void ApplyLogExpanded()
{
    if (logScroll != null)
        logScroll.style.display = logExpanded ? DisplayStyle.Flex : DisplayStyle.None;
    if (logToggleButton != null)
        logToggleButton.text = logExpanded ? "▼" : "▲";
}
```

**Lógica S42:**
- Toggle booleano `logExpanded`
- Si expandido: `logScroll` visible (Flex), botón muestra ▼
- Si colapsado: `logScroll` oculto (None), botón muestra ▲
- Permite minimizar el log para ver más del campo de juego

## KindClass

```csharp
private static string KindClass(CombatVisualLogKind kind) => kind switch
{
    CombatVisualLogKind.Versus => "log-versus",
    CombatVisualLogKind.Hit    => "log-hit",
    CombatVisualLogKind.Crit   => "log-crit",
    CombatVisualLogKind.Death  => "log-death",
    CombatVisualLogKind.Result => "log-result",
    _                          => "log-hit",
};
```

Mapea Kind enum a clase USS (cada clase define color de fondo, borde, etc.). Fallback a "log-hit" si kind desconocido.

## Wiring en Start (S42 ACTUALIZADO)

```csharp
if (logToggleButton != null) logToggleButton.clicked += ToggleLog;
```

**S42:** Nuevo wiring para el botón de toggle.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatVisualEvents]] — `OnVisualCombatStart`, `OnPanelState` eventos
- [[CombatVisualizerService]] — singleton llamado (Back, Next, TogglePlay, SetSpeed)

## Conexiones

**Entrada:**
- `OnVisualCombatStart(ctx)` — muestra panel
- `OnPanelState(st)` — reconstruye turno, log, habilita botones

**Salida:**
- `CombatVisualizerService.Back/Next/TogglePlay/SetSpeed()` — control de reproducción
- UIElements visuales (cartas de log, turno, botones, slider)

## Cambios S42

**Aditivos (append-only):**
- **Campo:** `logToggleButton` (serializado opcional, auto-resuelto del UXML)
- **Campo:** `logExpanded` (bool, default true)
- **Método nuevo:** `ToggleLog()` — toggle booleano
- **Método nuevo:** `ApplyLogExpanded()` — aplica display style + actualiza texto botón
- **Wiring:** `if (logToggleButton != null) logToggleButton.clicked += ToggleLog;` en Start

**Layout v2 (visual, no código):**
- Log abajo, colapsable (▼ expandido, ▲ colapsado)
- Controles compactos (back, play, next, speed) a la izquierda
- Turno a la derecha
- Permite maximizar vista del tablero durante replay

**Invariante:** Todos los métodos viejos intactos (HandleStart, HandleState, RebuildLog, KindClass, SetVisible).

## Notas

- **UXML:** Requiere elementos con names exactos (turn-label, log-scroll, log-container, btn-back, btn-play, btn-next, speed-slider, speed-label, **btn-log-toggle** S42)
- **ScrollView auto-scroll:** Ejecutado 1 frame después de agregar cartas (tiempo para layout)
- **Rich-text en log:** Los colores vienen de `CombatVisualizerService` (líneas con `<color=#...>` tags)
- **S42 Toggle:** Mejora UX al permitir ocultar log largos y ver más del tablero de juego
- **Clases USS:** log-versus (vs screen), log-hit (golpe normal), log-crit (crítico), log-death (muerte), log-result (final)
