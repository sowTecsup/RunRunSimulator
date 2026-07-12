---
tags: [script, ui, partial]
---

# CombatPanelUITK.Tabs

**Ruta:** `UI/CombatPanelUITK.Tabs.cs`

**Responsabilidad:** Contenido de las 3 pestañas principales del panel de combate: Batalla Online (Tab 0), Resultados (Tab 1), Historial (Tab 2). **S34:** Tab Historial incluye boton replay "▶ Ver replay" lazy-creado. **S37/S38:** Tab "Combate Local" (antigua tab 2) fue **retirada completamente**; lineup UI 3v3 movida a sibling `CombatLineupUITK`. Tabs reindexadas y métodos legacy de 1v1 (RebuildFighterLists, SelectFighterA/B, RefreshSlots, SetSlot, DoLocalFight) **eliminados**.

## Tabs Resumen (Post-S37)

| Tab | Nombre | Contenido |
|-----|--------|----------|
| 0 | Batalla Online | Lista criaturas, selecciona 1, muestra stats+partes, envía a async (Instant/Timer) |
| 1 | Resultados | Criaturas en cola (`QueuedForCombat`), countdown a próximo server tick |
| 2 | Historial | Todos los combates históricos, filtrable por criatura, **boton replay por combate (S34)** |
| (3) | Equipo 3v3 | **NO EN ESTE ARCHIVO** — manejado por CombatLineupUITK (sibling) |

## Cambios S37/S38 (Eliminaciones)

**Métodos Legacy de Tab "Combate Local" — REMOVIDOS:**
- `RebuildFighterLists()` — ya no existe
- `SelectFighterA()` / `SelectFighterB()` — ya no existen
- `RefreshSlots()` — ya no existe
- `SetSlot()` — ya no existe
- `DoLocalFight()` — ya no existe (logic movida a CombatLineupUITK.OnFightClick)

**Campos Legacy — REMOVIDOS:**
- `fighterAList`, `fighterBList` — refs UI de tab 1v1
- `slotA`, `slotB` — visuales de slots
- `btnFightLocal` — boton "Luchar" de tab 1v1
- `t2Cards`, `t2Index` — indices de tab 1v1

**Navegación — SIMPLIFICADA:**
- `Region` enum ya no tiene T2* (T2List, T2Actions) → solo TabBar, T1List, T1Actions, T3List, T4List
- Índices de navegación ajustados

**Flujo post-enqueue — ACTUALIZADO:**
- `EnqueueOnline()` ahora salta post-enqueue a Tab 1 (Resultados), no Tab 2 (era Historial, ahora es Resultados)

## Métodos Principales (Tab 0 — Batalla Online)

- `RebuildOnlineList()`: lista de criaturas elegibles vía `MakeCandidate()`
- `MakeCandidate(dna, bucket, onClick)`: fila nombre + 6 stats + ratio peleas/límite
- `SetCenter()`: detalles del candidato online seleccionado (imagen, nombre, stats, partes)
- `EnqueueOnline(instant)`: envía a async combate (Instant o Scheduled) → salta a Tab 1

## Métodos Principales (Tab 1 — Resultados)

- `RebuildResults()`: Tab 1 con criaturas en cola
- `DoRefresh()`: poll async vía `AsyncCombatService.PollResultsAsync()`
- `UpdateClock()`: countdown a próximo tick servidor (hh:mm)

## Métodos Principales (Tab 2 — Historial + Replay S34)

- `RebuildHistory()`: historial de combates (flattened, newest first)
- `RebuildHistoryFilter()`: dropdown "Todos" + creatures con historia
- `RebuildHistoryList()`: muestra combates filtrados (outcome, oponente, fecha)
- `ShowHistory(it)`: detalles de un combate + **boton replay lazy-creado (S34)**

## ShowHistory (S34 + S37 cambios)

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

    // S37: CanReplay retorna false para 3v3, desactiva el boton
    histReplayBtn?.SetEnabled(CombatReplayRequest.CanReplay(it.Self, it.Rec, registry));
}
```

**S34 cambios:**
1. **Lazy creation:** Boton se crea **la primera vez** que se abre un combate en ShowHistory
2. **Clase CSS:** `"cbt-replay-btn"` para styling
3. **Callback:** Ejecuta `CombatReplayRequest.Request()` si `CanReplay()`

**S37 cambios:**
1. **Bloqueo de 3v3:** `CanReplay()` retorna false para records con SelfTeam != null, boton queda disabled
2. **Boton tooltip:** "Replay no soportado — equipo 3v3" (Fase 4)

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `histReplayBtn` | `Button` | **S34** Boton "▶ Ver replay" creado lazy en ShowHistory |
| `histCurrent` | `HistItem` | **S34** Struct (Self, Rec) del combate mostrado actualmente |

## Struct HistItem

```csharp
private struct HistItem { public CreatureDNA Self; public CombatRecord Rec; }
```

Par (criatura, record) para manejo efficient del historial.

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CombatPanelUITK]] — componente padre
- [[CombatLineupUITK]] — sibling (maneja tab 3)
- [[CombatController]] — `SimulateLocal()`, `EnqueueForAsyncCombat()`
- [[AsyncCombatService]] — `EnqueueInstantAsync()`, `EnqueueScheduledAsync()`, `PollResultsAsync()`
- [[CombatReplayRequest]] — `CanReplay()`, `Request()` (S34+S37)
- [[CreatureDNA]] — `Role`, `Stats`, `CombatHistory`

## Conexiones

**Entrada:**
- Interacción de usuario en tabs (botones, selección)
- `GameEvents.CombatCompleted()` — listener que actualiza Resultados/Historial
- `CreatureRegistrySO` — lista de criaturas

**Salida:**
- `AsyncCombatService` (enqueue, poll)
- `CombatReplayRequest.Request()` — replay request (Tab 2, S34+S37)

## Notas (S34 + S37 + S39)

- **Partial class:** Deuda activa (Fase 8, refactor a componentes pequeños).
- **S37 impacto:** Tab Combate Local completamente removida. Métodos 1v1 eliminados. Tabs reindexadas.
- **S34 Replay:** Boton lazy para evitar crear UI innecesaria hasta que se abra Tab Historial.
- **S37 Transicional:** Tab 3 Equipo 3v3 es manejada por sibling CombatLineupUITK.
- **S37 Bloqueo 3v3 Replay:** Records 3v3 no pueden ser replayados en visualizador 1v1 (Fase 4).
- **S39:** Sin cambios. Sistema elemental integrado en CombatPanelUITK.Tabs (stats display OK).
