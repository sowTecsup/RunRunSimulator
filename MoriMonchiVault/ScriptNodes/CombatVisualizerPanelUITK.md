---
tags: [script, ui, combat, visualizer]
---

# CombatVisualizerPanelUITK.cs

**Ruta:** `UI/CombatVisualizerPanelUITK.cs`

**Responsabilidad:** Panel UITK de replay 3v3. Header turno, log en cartas, controles play/next/back/velocidad. **S61:** Nuevo handler `HandleLogAppend()` recibe líneas incrementales vía evento `OnLogAppend`, llama `AddCard()` (extraído de RebuildLog) + `ScrollLogToEnd()`. **S58:** Log filtrado a "Eventos" — solo muestra líneas con HasUnit (reacciones/muertes) o Kind Result. Cada línea con HasUnit renderiza mini-headshot de la unidad afectada.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `OnEnable/OnDisable()` | **S61** Suscribe/desuscribe OnVisualCombatStart, OnPanelState, OnLogAppend |
| `Start()` | Localiza UI elements, conecta callbacks |

## Cambios S61

**OnEnable/OnDisable () actualizado (línea 25-37):**
```csharp
private void OnEnable()
{
    CombatVisualEvents.OnVisualCombatStart += HandleStart;
    CombatVisualEvents.OnPanelState        += HandleState;
    CombatVisualEvents.OnLogAppend         += HandleLogAppend;  // S61 NEW
}

private void OnDisable()
{
    CombatVisualEvents.OnVisualCombatStart -= HandleStart;
    CombatVisualEvents.OnPanelState        -= HandleState;
    CombatVisualEvents.OnLogAppend         -= HandleLogAppend;  // S61 NEW
}
```

**Handler HandleLogAppend() nuevo (línea 102-107):**
```csharp
private void HandleLogAppend(CombatVisualLogLine line)
{
    if (logContainer == null) return;
    AddCard(line);
    ScrollLogToEnd();
}
```

**Propósito:**
- Recibe líneas incrementales del evento OnLogAppend (una por beat de proc elemental)
- Agrega directamente a la UI sin rebuild (O(1) vs O(n))
- Auto-scrollea al final para seguir la acción

**Método AddCard() extraído (línea 109-129):**
```csharp
private void AddCard(CombatVisualLogLine line)
{
    if (!line.HasUnit && line.Kind != CombatVisualLogKind.Result) return;

    var card = new VisualElement();
    card.AddToClassList("log-card");
    card.AddToClassList(KindClass(line.Kind));

    if (line.HasUnit)
    {
        var headshot = new VisualElement();
        headshot.AddToClassList("log-headshot");
        MonchiPortraitUI.ApplyHeadshot(headshot, ResolveDna(line.UnitSide, line.UnitIndex));
        card.Add(headshot);
    }

    var label = new Label(line.Text);
    label.AddToClassList("log-text");
    card.Add(label);
    logContainer.Add(card);
}
```

**Cambios vs S58:**
- Antes: parte integral de RebuildLog()
- Ahora: **extraído como método privado** reutilizable
- Usado por: RebuildLog (loop), HandleLogAppend (singular)

**Helper ScrollLogToEnd() nuevo (línea 131-135):**
```csharp
private void ScrollLogToEnd()
{
    if (logScroll != null)
        logScroll.schedule.Execute(() => logScroll.scrollOffset = new Vector2(0f, float.MaxValue)).ExecuteLater(1);
}
```

**Propósito:**
- Centralizador de scroll-to-end logic (1ms delay permite que el layout calculate antes)
- Reutilizable por RebuildLog y HandleLogAppend

**RebuildLog() refactorizado (línea 90-100):**
```csharp
private void RebuildLog(CombatVisualLogLine[] lines)
{
    if (logContainer == null) return;
    logContainer.Clear();
    if (lines == null) return;

    foreach (var line in lines)
        AddCard(line);  // CAMBIO: usa AddCard en lugar de inline

    ScrollLogToEnd();
}
```

**Impacto S61:**
- RebuildLog ahora delegaba AddCard (DRY principle)
- Permite append incremental sin duplicar lógica de card creation/filtering
- ScrollLogToEnd centralizado

## Cambios S58

**RebuildLog() filtrado:**
- Omite líneas sin HasUnit EXCEPTO Result
- Muestra: Proc/Death (HasUnit=true) + Result final
- Oculta: Versus inicial, Hit/Crit normales

**Headshot mini:**
- ResolveDna(side, index) desde contexto.TeamA/TeamB
- ApplyHeadshot renderiza lateral 512×192
- Headshot+texto = identidad visual del unit afectado

**Label "Eventos" vs "Log":**
- Panel internamente "log" (CSS, variables)
- UI muestra "Eventos" (solo reacciones/muertes, no log bruto)

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `RebuildLog()` | **S61** Refactorizado: Clear → loop AddCard() → ScrollLogToEnd() |
| `AddCard()` | **S61 NEW** Extraído de RebuildLog. Crea VisualElement card con headshot + label, filtra por HasUnit |
| `HandleLogAppend()` | **S61 NEW** Handler OnLogAppend: AddCard(line) + ScrollLogToEnd() |
| `ScrollLogToEnd()` | **S61 NEW** Helper: scrollOffset = MaxValue con 1ms delay |
| `ResolveDna()` | Busca DNA en contexto |
| `HandleState()` | Actualiza turno, botones, llamaa RebuildLog |
| `KindClass()` | CSS por Kind |
| `ToggleLog()` | Minimiza/expande scroll |

## Flujo S61

**Rebuild total (OnPanelState → HandleState):**
1. RebuildLog(log[]) — Clear container
2. Loop: foreach line → AddCard(line)
3. ScrollLogToEnd()

**Append incremental (OnLogAppend → HandleLogAppend):**
1. AddCard(line) — agrega single element
2. ScrollLogToEnd()

**Diferencia:**
- Rebuild: O(n) cartas, usado al pasar entre turnos
- Append: O(1) carta, usado en tiempo real en beat de proc

## Flujo S58 (Visual)

**Log "Eventos" (visible a user):**
- Reacción → "**{Estado}** — {Description}" + headshot
- Muerte → "X cae derrotado" + headshot
- Resultado → "Ganador: equipo"
- Omitido: Versus, Hit normal

**Layout:**
- Header turno (superior)
- Log expandible (central, scrollable)
- Botones play/back/next, velocidad (inferior)

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[CombatVisualEvents]] — **S61** nuevo OnLogAppend; OnPanelState
- [[CombatVisualizerService]] — emite OnLogAppend (S61), OnPanelState
- [[MonchiPortraitUI]] — ApplyHeadshot
- [[MonchiPortraitService]] — GetHeadshot

## Conexiones

**Entrada:**
- OnVisualCombatStart → HandleStart (muestra panel)
- OnPanelState → HandleState (turno, log rebuild, botones)
- **S61** OnLogAppend → HandleLogAppend (append incremental)

**Salida:**
- Panel UI visible
- Back/TogglePlay/Next/SetSpeed → CombatVisualizerService

## Notas S61

- AddCard es método reusable: filtra, crea card, agrega headshot si HasUnit
- ScrollLogToEnd es helper centralizado (usado 2+ veces)
- Refactorización: sin cambio de comportamiento, mejor mantenibilidad
- Append incremental: eficiente (O(1) vs O(n) rebuild)

## Notas S58

- Log se llama "Eventos" (UI friendlier)
- Filtrado: solo reacciones/muertes (HasUnit marker)
- Headshot 512×192 miniaturizado
- Cada línea con unit = mini-avatar para identidad visual
