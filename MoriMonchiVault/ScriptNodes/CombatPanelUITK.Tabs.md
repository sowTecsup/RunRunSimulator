---
tags: [script, ui, partial]
---

# CombatPanelUITK.Tabs

**Ruta:** `UI/CombatPanelUITK.Tabs.cs`

**Responsabilidad:** Contenido de las 4 pestañas del panel de combate: Batalla Online (Tab 0), Combate Local (Tab 1), Resultados (Tab 2), Historial (Tab 3). **S34:** Tab Historial ahora incluye boton replay "▶ Ver replay" lazy-creado.

## Tabs Resumen

| Tab | Nombre | Contenido |
|-----|--------|----------|
| 0 | Batalla Online | Lista criaturas, selecciona 1, muestra stats+partes, envía a async (Instant/Timer) |
| 1 | Combate Local | Dos listas (A/B), selecciona dos criaturas diferentes, lucha local, log inline |
| 2 | Resultados | Criaturas en cola (`QueuedForCombat`), countdown a próximo server tick |
| 3 | Historial | Todos los combates históricos, filtrable por criatura, **boton replay por combate (S34)** |

## Métodos Principales (Tab 0+1)

- `RebuildOnlineList()`: lista de criaturas elegibles (Tab 0) vía `MakeCandidate()`
- `RebuildFighterLists()`: dos listas izquierda/derecha para seleccionar combatientes (Tab 1)
- `MakeCandidate(dna, bucket, onClick)`: fila nombre + 6 stats + ratio peleas/límite
- `SetCenter()`: detalles del candidato online seleccionado (imagen, nombre, stats, partes)
- `EnqueueOnline(instant)`: envía a async combate (Instant o Scheduled)
- `SelectFighterA()`, `SelectFighterB()`: selecciona combatientes locales
- `RefreshSlots()`: actualiza slots A/B con nombres e imágenes
- `DoLocalFight()`: ejecuta combate local, dispara `GameEvents.CombatCompleted()`

## Métodos Principales (Tab 2 — Resultados)

- `RebuildResults()`: Tab 2 con criaturas en cola
- `DoRefresh()`: poll async vía `AsyncCombatService.PollResultsAsync()`
- `UpdateClock()`: countdown a próximo tick servidor (hh:mm)

## Métodos Principales (Tab 3 — Historial + Replay S34)

- `RebuildHistory()`: historial de combates (flattened, newest first)
- `RebuildHistoryFilter()`: dropdown "Todos" + creatures con historia
- `RebuildHistoryList()`: muestra combates filtrados (outcome, oponente, fecha)
- `ShowHistory(it)`: detalles de un combate + **boton replay lazy-creado (S34)**

## ShowHistory (S34 cambio principal)

```csharp
private void ShowHistory(HistItem it)
{
    if (histLines == null) return;
    histLines.Clear();
    histCurrent = it;

    // ... header, opponent, date, turnos ...

    if (histOutcome != null)
    {
        // ... outcome text ...

        if (histReplayBtn == null)  // Lazy creation
        {
            histReplayBtn = new Button { text = "▶ Ver replay" };
            histReplayBtn.AddToClassList("cbt-replay-btn");
            histReplayBtn.clicked += () =>
            {
                if (histCurrent.Self != null && CombatReplayRequest.CanReplay(histCurrent.Self, histCurrent.Rec, registry))
                    CombatReplayRequest.Request(histCurrent.Self, histCurrent.Rec);
            };
            int idx = histOutcome.parent.IndexOf(histOutcome);
            histOutcome.parent.Insert(idx + 1, histReplayBtn);
        }
    }

    histReplayBtn?.SetEnabled(CombatReplayRequest.CanReplay(it.Self, it.Rec, registry));
}
```

**S34 cambios:**
1. **Lazy creation:** Boton se crea **la primera vez** que se abre un combate en ShowHistory
2. **Clase CSS:** `"cbt-replay-btn"` para styling
3. **Callback:** Ejecuta `CombatReplayRequest.Request()` si `CanReplay()`
4. **Posicionamiento:** Se inserta tras `histOutcome` (abajo-derecha típicamente)
5. **Habilitación dinámica:** Re-habilitado/deshabilitado en cada ShowHistory para captar cambios de registry

## Stats Mostrados

- `MakeCandidate`: "CON X ATK Y SPD Z DEF A LCK B EVA C"
- `SetCenter`: "CON X   ATK Y   SPD Z   DEF A   LCK B   EVA C"
- AddPartRow: "nombre · Set · Tier{int}"

## OutcomeShort vs OutcomeLong (S34)

```csharp
private static string OutcomeShort(CombatOutcome o, bool died) => o switch
{
    CombatOutcome.Won  => "Ganó",
    CombatOutcome.Lost => died ? "Murió" : "Perdió",
    _                  => "Empate",
};

private static string OutcomeLong(CombatOutcome o) => o switch
{
    CombatOutcome.Won  => "¡Victoria!",
    CombatOutcome.Lost => "Derrota",
    _                  => "Empate",
};
```

- `OutcomeShort`: para lista (compacto)
- `OutcomeLong`: para detail pane (enfático)

## Vinculado a

- [[CombatPanelUITK]] — clase principal
- [[Index/05 - UI System]]
- [[CombatReplayRequest]] — S34 replay request
- [[CombatService]] — simula combate local
- [[AsyncCombatService]] — enqueue + poll async

## Conexiones

**Entrada:**
- `GameEvents.OnRegistry Changed/Reloaded` — rebuild listas
- `GameEvents.OnCombatLogged` — nuevo combate en historial

**Salida:**
- `CombatService.Simulate()` — combate local (Tab 1)
- `AsyncCombatService.EnqueueInstantAsync/EnqueueScheduledAsync()` — async enqueue (Tab 0)
- `CombatReplayRequest.Request()` — replay request (Tab 3, S34)

## Notas

- **Tab 2 (Resultados):** Muestra SÓLO criaturas en cola; finalizadas se mueven a Tab 3
- **Historial filtrable:** Dropdown por criatura; cada selección rebuildea lista
- **Newest first:** Combates ordenados por fecha descendente
- **Tab 3 layout:** Left = lista combates (nombre/outcome/fecha filtrados), right = detail pane (log turno-a-turno + outcome + **boton replay S34**)
- **Replay button S34:** Validado en cada ShowHistory para captar cambios (rival vendido, eliminado, etc.)
