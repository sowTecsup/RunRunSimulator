---
tags: [script, ui, combat, visualizer]
---

# CombatVisualizerPanelUITK.cs

**Ruta:** `UI/CombatVisualizerPanelUITK.cs`

**Responsabilidad:** Panel UITK de replay 3v3. Header turno, log en cartas, controles play/next/back/velocidad. **S58:** Log filtrado a "Eventos" — solo muestra líneas con HasUnit (reacciones/muertes) o Kind Result. Cada línea con HasUnit renderiza mini-headshot de la unidad afectada.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `OnEnable/OnDisable()` | Suscribe/desuscribe OnVisualCombatStart, OnPanelState |
| `Start()` | Localiza UI elements, conecta callbacks |

## Cambios S58

**RebuildLog() filtrado (línea 88-118):**

```csharp
foreach (var line in lines)
{
    if (!line.HasUnit && line.Kind != CombatVisualLogKind.Result) continue;  // S58 FILTER

    var card = new VisualElement();
    card.AddToClassList("log-card");
    card.AddToClassList(KindClass(line.Kind));

    if (line.HasUnit)  // S58 HEADSHOT
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

**Filtro (línea 96):**
- Omite líneas sin HasUnit EXCEPTO Result
- Muestra: Proc/Death (HasUnit=true) + Result final
- Oculta: Versus inicial, Hit/Crit normales

**Headshot mini (línea 102-107):**
- ResolveDna(side, index) desde contexto.TeamA/TeamB
- ApplyHeadshot renderiza lateral 512×192
- Headshot+texto = identidad visual del unit afectado

**Label "Eventos" vs "Log":**
- Panel internamente "log" (CSS, variables)
- UI muestra "Eventos" (solo reacciones/muertes, no log bruto)

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `RebuildLog()` | **S58** Filtra HasUnit, headshots por unidad |
| `ResolveDna()` | Busca DNA en contexto |
| `HandleState()` | Actualiza turno, botones, llamaa RebuildLog |
| `KindClass()` | CSS por Kind |
| `ToggleLog()` | Minimiza/expande scroll (S42) |

## Flujo S58

**Log "Eventos" (visible a user):**
- Reacción → "X activó Y al combinar Z" + headshot X
- Muerte → "X cae derrotado" + headshot X
- Resultado → "Ganador: equipo" (sin headshot)
- Omitido: Versus, Hit normal

**Layout:**
- Header turno (superior)
- Log expandible (central, scrollable)
- Botones play/back/next, velocidad (inferior)

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[CombatVisualEvents]] — OnPanelState
- [[CombatVisualizerService]] — emite PanelState
- [[MonchiPortraitUI]] — ApplyHeadshot
- [[MonchiPortraitService]] — GetHeadshot

## Conexiones

**Entrada:**
- OnVisualCombatStart → muestra panel
- OnPanelState → HandleState (turno, log, botones)

**Salida:**
- Panel UI visible
- Back/TogglePlay/Next/SetSpeed → CombatVisualizerService

## Notas S58

- Log se llama "Eventos" (UI friendlier que "Log")
- Filtrado: solo reacciones/muertes (HasUnit marker)
- Headshot 512×192 miniaturizado (Cover crop en tarjeta)
- Cada línea con unit = mini-avatar para identidad visual
