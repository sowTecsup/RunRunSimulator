---
tags: [script, ui, partial]
---

# CombatPanelUITK.Tabs

**Ruta:** `UI/CombatPanelUITK.Tabs.cs`

**Responsabilidad:** Contenido de las 4 pestañas del panel de combate: Batalla Online (Tab 0), Combate Local (Tab 1), Resultados (Tab 2), Historial (Tab 3). **S34:** Tab Historial ahora incluye boton replay "▶ Ver replay" lazy-creado. **S37:** Tabs siguen siendo 1v1 en UI (transicional), pero subyacentemente Simulate usa equipos de tamaño 1 (3v3 con 1 criatura por lado).

## Tabs Resumen

| Tab | Nombre | Contenido |
|-----|--------|----------|
| 0 | Batalla Online | Lista criaturas, selecciona 1, muestra stats+partes, envía a async (Instant/Timer) |
| 1 | Combate Local | Dos listas (A/B), selecciona dos criaturas diferentes, lucha local, log inline |
| 2 | Resultados | Criaturas en cola (`QueuedForCombat`), countdown a próximo server tick |
| 3 | Historial | Todos los combates históricos, filtrable por criatura, **boton replay por combate (S34)**, **bloqueo replay 3v3 (S37)** |

## Métodos Principales (Tab 0+1)

- `RebuildOnlineList()`: lista de criaturas elegibles (Tab 0) vía `MakeCandidate()`
- `RebuildFighterLists()`: dos listas izquierda/derecha para seleccionar combatientes (Tab 1)
- `MakeCandidate(dna, bucket, onClick)`: fila nombre + 6 stats + ratio peleas/límite
- `SetCenter()`: detalles del candidato online seleccionado (imagen, nombre, stats, partes, **rol vía chip S37**)
- `EnqueueOnline(instant)`: envía a async combate (Instant o Scheduled)
- `SelectFighterA()`, `SelectFighterB()`: selecciona combatientes locales
- `RefreshSlots()`: actualiza slots A/B con nombres e imágenes, **muestra rol (S37)**
- `DoLocalFight()`: **S37** ejecuta combate local vía `CombatController.SimulateLocal(idsA=[A], idsB=[B], null, null)`, dispara `GameEvents.CombatCompleted()`

## Métodos Principales (Tab 2 — Resultados)

- `RebuildResults()`: Tab 2 con criaturas en cola
- `DoRefresh()`: poll async vía `AsyncCombatService.PollResultsAsync()`
- `UpdateClock()`: countdown a próximo tick servidor (hh:mm)

## Métodos Principales (Tab 3 — Historial + Replay S34 + S37)

- `RebuildHistory()`: historial de combates (flattened, newest first)
- `RebuildHistoryFilter()`: dropdown "Todos" + creatures con historia
- `RebuildHistoryList()`: muestra combates filtrados (outcome, oponente, fecha)
- `ShowHistory(it)`: detalles de un combate + **boton replay lazy-creado (S34)**, **disabled si 3v3 (S37)**

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
1. **Bloqueo de 3v3:** `CanReplay()` retorna false para records con SelfTeam != null, boton queda disabled ("Replay no soportado — equipo 3v3")
2. **Display de rol:** MakeCandidate y RefreshSlots ahora incluyen chip de rol (S37) vía icono/color
3. **DoLocalFight():** Llama `CombatController.SimulateLocal(idsA, idsB, null, null)` con rows=null (default 2-3-2), pero UI sigue siendo 1v1

## Cambios S37

**Display de rol:**
```csharp
// En SetCenter() o MakeCandidate():
var roleProfile = config.RoleProfiles.GetProfile(dna.Role);
var roleChip = new Label { text = RoleDisplay(dna.Role) };  // "Protector" / "Agresivo" / "Empático"
roleChip.AddToClassList($"role-{dna.Role.ToString().ToLower()}");
// Agrega color CSS (Protector=azul, Agresivo=rojo, Empático=verde, ejemplo)
```

**Bloqueo de replay 3v3:**
```csharp
// En ShowHistory():
histReplayBtn?.SetEnabled(CombatReplayRequest.CanReplay(it.Self, it.Rec, registry));
// Si CanReplay() retorna false (3v3), boton desabilitado con tooltip
if (!CombatReplayRequest.CanReplay(it.Self, it.Rec, registry))
    histReplayBtn.tooltip = "Replay de equipos 3v3 pendiente en Fase 4";
```

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CombatController]] — `SimulateLocal(idsA, idsB, rowsA, rowsB)`
- [[AsyncCombatService]] — `EnqueueInstantAsync()`, `EnqueueScheduledAsync()`, `PollResultsAsync()`
- [[CombatReplayRequest]] — `CanReplay()`, `Request()` (S34+S37)
- [[CreatureDNA]] — `Role`, `Stats`, `CombatHistory`
- [[RoleTableSO]] — perfiles de rol (S37)

## Conexiones

**Entrada:**
- Interacción de usuario en tabs (botones, selección)
- `GameEvents.CombatCompleted()` — listener que actualiza Resultados/Historial
- `CreatureRegistrySO` — lista de criaturas

**Salida:**
- `CombatController.SimulateLocal()` — local combat (Tab 1)
- `CombatController.EnqueueForAsyncCombat()` — async combat (Tab 0)
- `AsyncCombatService.PollResultsAsync()` — poll (Tab 2)
- `CombatReplayRequest.Request()` — replay request (Tab 3, S34+S37)

## Notas (S34 + S37)

- **Partial class:** Deuda activa (Fase 8, refactor a componentes pequeños)
- **S34 Replay:** Boton lazy para evitar crear UI innecesaria hasta que se abra Tab Historial
- **S37 Transicional:** UI sigue siendo 1v1 (2 slots), pero subyacentemente es 3v3 con equipos de tamaño 1. Futuro (Fase 6+): redesignar UI para soporte visual de equipos 3v3.
- **S37 Bloqueo 3v3 Replay:** Records 3v3 se persisten normalmente, pero no pueden ser replayados en visualizador 1v1. Esperando visualizador 3v3 (Fase 4).
- **Rol display:** Chip visual de rol mostrado en candidatos/slots (S37) — ayuda al jugador a entender composición del equipo.
