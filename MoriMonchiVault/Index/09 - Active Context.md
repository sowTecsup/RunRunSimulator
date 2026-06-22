---
tags: [index, core]
---

# 09 - Active Context

**Session:** 2026-06-21 (Session 18 — Combat Visualizer: replay desde `CombatRecord`, hooks Feel, UI Pokémon-style) — **CÓDIGO HECHO, SIN testear en Play (pendiente de wiring Unity)**
**Focus:** Sistema standalone para visualizar peleas en escena propia. Recibe 2 `CreatureDNA` + un `CombatRecord` (turn-by-turn ya persistido en `CreatureDNA.CombatHistory`) y reproduce la pelea: arma los dos modelos vía `MoriMonchiVisualizer.Assemble`, ejecuta los `CombatTurn` en coroutine (windup → hit → HP tween → between-turns), barras de HP world-space por lado y log inferior cartas-por-turno. Hooks Feel (`UnityEvent`) listos para arrastrar `MMF_Player` desde inspector.

**Decisiones de arquitectura (Juan):**
- `CombatVisualizerService.Instance` apex: dueño de slots, prefab visualizer-only, refs serializadas a `CreatureDatabaseSO` + `PartVisualBankSO` + `FurTypeDatabaseSO` (cascada estricta, NO va a `GameManager.Instance`).
- Bus dedicado `CombatVisualEvents` (estático, archivo aparte) — NO ensucia `GameEvents` con eventos visuales efímeros. Justificado por el patrón "eventos visuales viven en su propio bus" (como UIManager).
- HP máx por lado = `CombatService.GetEffectiveStats(dna, db).HP` (el mismo `BaseHP × 5` + bonos de partes que usa el sim). HP tras cada turno = `CombatTurn.DefenderHpAfter`.
- Standalone (NO panel del UIManager): la idea futura es escena aparte. Si después se mete al stack, se migra.
- Reuso total de `MoriMonchiVisualizer.Assemble/RefreshFur` — el prefab es una variante MM **sin** `MoriMochiAgent` (zero AI/NavMesh).
- Hooks Feel: 1 componente con enum `HookKind { Global, SideA, SideB }` → Juan instancia 3 GameObjects (1 global + 1 por side) y arrastra MMFs. Por-side filtra Attack/Hit/Crit/Dead/HpChanged; Global cubre CombatStart/End + TurnStart/End + Log.

**Eventos del bus (`CombatVisualEvents`):**
`OnVisualCombatStart(ctx)` · `OnVisualCombatEnd(winnerSide, isDraw)` · `OnTurnStart/End(turn)` · `OnAttack(side)` · `OnHit(hit)` · `OnCrit(hit)` · `OnHpChanged(side, current, max)` · `OnDead(side)` · `OnLog(line)`.

**Files Created (input para ScriptNodes — agente haiku al cierre):**
- `Systems/CombatVisualizer/CombatVisualEvents.cs` (bus + DTOs `CombatVisualContext`, `CombatVisualHit`, enum `CombatVisualSide`).
- `Systems/CombatVisualizer/CombatVisualizerService.cs` (apex `.Instance`, `Play(dnaA, dnaB, record)`, coroutine de replay, spawn/despawn de visualizers).
- `Systems/CombatVisualizer/CombatVisualHooks.cs` (`HookKind` enum + UnityEvents filtrados por side).
- `UI/CombatVisualizerPanelUITK.cs` (header de turno + log inferior, suscribe el bus).
- `UI/CombatHpBarUITK.cs` (world-space por side, lerp del fill por % HP).
- `UI Toolkit/CombatVisualizerPanel.uxml/.uss` (cv-root + cv-header + cv-log-frame).
- `UI Toolkit/CombatHpBar.uxml/.uss` (hp-root + hp-track + hp-fill).

**Files Touched:** ninguno (sistema 100% nuevo).

**PASO MANUAL PENDIENTE (Juan, Unity) — bloqueante para Play:**
1. Variante de prefab del MM **sin** `MoriMochiAgent` (solo `MoriMonchiVisualizer` con sockets) → asignar como `visualizerPrefab` del Service.
2. Crear GameObject "CombatVisualizer" con `CombatVisualizerService` + 2 child empty `SlotA` / `SlotB`. Asignar refs (`database` = el `CreatureDatabaseSO` del proyecto, `partVisualBank` = el `PartVisualBankSO`, `furDatabase` = el `FurTypeDatabaseSO`, slots, prefab).
3. Como child de cada slot: GameObject con `UIDocument` world-space apuntando a `CombatHpBar.uxml` + `CombatHpBarUITK` con `side` A o B según el slot.
4. GameObject standalone "CombatVisualizerPanel" con `UIDocument` (screen-space) apuntando a `CombatVisualizerPanel.uxml` + `CombatVisualizerPanelUITK`.
5. GameObjects "FeelHooks_Global" / "FeelHooks_A" / "FeelHooks_B" con `CombatVisualHooks` (`HookKind` respectivo). Arrastrar `MMF_Player` en cada `UnityEvent` que se quiera usar.
6. Para testear: desde un dev tool / botón Odin, llamar `CombatVisualizerService.Instance.Play(dnaA, dnaB, record)` con un `CombatRecord` cualquiera del `CreatureDNA.CombatHistory` de algún MM existente.

---

## TESTING REQUIRED — Sesión 19 (Juan, en Play)

Sesión 18 cerró sin Play (sistema nuevo, sin escena armada). Checklist de validación, ordenada por flujo. Si algún paso falla, anotar el síntoma y NO seguir hasta entender la causa.

**Pre-flight (wiring Unity):**
- [ ] Reabrir Unity → recompila sin errores. Carpeta nueva `Systems/CombatVisualizer/` debe generar su `.meta`.
- [ ] Crear variante de prefab MM **sin** `MoriMochiAgent` (mantiene solo `MoriMonchiVisualizer` con sockets configurados vía botón Setup).
- [ ] GameObject "CombatVisualizer" en escena con `CombatVisualizerService` + 2 child empty `SlotA` / `SlotB` separados ~3-4m en X.
- [ ] Refs asignadas en el Service: `database` (el `CreatureDatabaseSO` del proyecto), `partVisualBank`, `furDatabase`, `visualizerPrefab` (la variante anterior), `slotA`, `slotB`.
- [ ] Como child de cada slot: GameObject con `UIDocument` (Render Mode = World Space, PanelSettings world-space) apuntando a `CombatHpBar.uxml` + `CombatHpBarUITK` con `side` A o B según slot. Escalar el UIDocument a ~0.005-0.01 y posicionarlo encima del modelo.
- [ ] GameObject standalone "CombatVisualizerPanel" con `UIDocument` (screen-space overlay) apuntando a `CombatVisualizerPanel.uxml` + `CombatVisualizerPanelUITK`.
- [ ] GameObjects de hooks (mínimo para Test 5): `FeelHooks_Global` (HookKind=Global), `FeelHooks_A` (HookKind=SideA), `FeelHooks_B` (HookKind=SideB).
- [ ] Cámara apuntando a la zona entre los slots (puede ser una cámara dedicada o un transform al que mover la principal).

**Disparo de prueba (botón Odin o dev tool temporal):**
- [ ] Asegurar que hay al menos 2 MM en el registry con `CombatHistory` poblado. Si no, simular una pelea local primero vía el `CombatDevConsole` para llenar el historial.
- [ ] Desde un dev tool (botón Odin nuevo o reutilizar `CombatDevConsole`), invocar:
  ```csharp
  var dnaA   = GameManager.Instance.Registry.All[0];
  var dnaB   = GameManager.Instance.Registry.All[1];
  var record = dnaA.CombatHistory[^1];
  CombatVisualizerService.Instance.Play(dnaA, dnaB, record);
  ```

**Test 1 — Spawn visual:**
- [ ] Al llamar `Play`, aparecen 2 modelos 3D ensamblados (cuerpo + brazos + ojos + boca) en SlotA y SlotB.
- [ ] Color del fur coincide con `BaseColor` de cada DNA (regresión: invariante color↔identidad de S15).
- [ ] Si los modelos salen desensamblados o sin partes, revisar que `visualizerPrefab` tenga sockets seteados (botón Setup en `MoriMonchiVisualizer`).

**Test 2 — Barras HP world-space:**
- [ ] Aparecen 2 HP bars con el `CustomName` de cada MM y la barra al 100%.
- [ ] Si no se ven o salen gigantes, revisar PanelSettings (mode = World) + escala del UIDocument transform.

**Test 3 — Log inferior + header turno:**
- [ ] Header arriba muestra "Turno 0 / N" al inicio (N = `record.Turns.Count`).
- [ ] El log va llenando líneas: "VS: A vs B", "Turno 1 · A → B", "Daño: X" o "¡Crítico! X de daño", "X cae derrotado.", "Ganador: …" / "Empate.".
- [ ] El header se actualiza a "Turno K / N" en cada `OnTurnStart`.
- [ ] El log respeta `maxLogLines` (default 6) — las viejas se recortan por arriba.

**Test 4 — HP tween:**
- [ ] La barra del defensor baja con lerp suave hasta el % correspondiente (`DefenderHpAfter / hpMax`).
- [ ] Al final del último turno, el % de la barra coincide visualmente con el último `DefenderHpAfter` del record (no debería divergir).
- [ ] Cambiar `fillLerpSeconds` en el inspector de la HP bar afecta la velocidad del tween.

**Test 5 — Hooks Feel (sin MMF aún, solo verificar disparo):**
- [ ] En cada slot de `FeelHooks_Global`/`A`/`B`, colgar temporalmente un `UnityEvent → Debug.Log("…")`:
  - **Global**: ver logs de Start, TurnStart, TurnEnd, End, LogLine (con string).
  - **SideA**: Attack, HitTaken, HitDealt, CritTaken, CritDealt, Dead, HpChanged (con float, float).
  - **SideB**: idem.
- [ ] Confirmar que SideA solo dispara cuando el side del evento es A (atacante o defensor según el hook), y SideB cuando es B. Ningún cruce.

**Test 6 — Crítico / muerte / empate:**
- [ ] Reproducir un record que termine en KO → `OnDead(loser)` se dispara exactamente UNA vez al cruce de HP=0; el log muestra "X cae derrotado."
- [ ] Reproducir un record que haya llegado a `MaxRounds` sin KO → `OnVisualCombatEnd(winner=A, isDraw=true)` y el header muestra "Empate" tras el último turno.
- [ ] Reproducir un record con al menos un `WasCrit=true` → `OnCrit` se dispara, el log muestra "¡Crítico!" y los hooks per-side `OnCritTaken`/`OnCritDealt` se invocan.

**Test 7 — Replay consecutivo:**
- [ ] Llamar `Play(...)` mientras hay otro combate corriendo → `Stop()` interno limpia los visualizers viejos y arranca el nuevo SIN GameObjects huérfanos en los slots.
- [ ] Al terminar (post `endHoldSeconds`, default 1.5s), los 2 visualizers se destruyen y los slots quedan vacíos.
- [ ] `IsPlaying` vuelve a `false` y `playRoutine` es null.

**Test 8 — Feel real (cuando Juan integre MMF_Players):**
- [ ] Reemplazar los `Debug.Log` por `MMF_Player.PlayFeedbacks()` en cada hook (windup en `OnAttack`, impacto en `OnHitTaken`/`OnCritTaken`, shake/zoom en `OnCritDealt`, ragdoll/disolve en `OnDead`).
- [ ] Confirmar que el ritmo se siente sincronizado con `windupSeconds` (0.35) e `impactSeconds` (0.35). Ajustar timings del Service si la animación de Feel necesita otro tempo.

**Bugs latentes posibles (revisar si aparecen en Play):**
- 🟡 Si `record.Turns` está vacío (combat draw sin turns? — improbable, el sim siempre agrega al menos 1), el visualizer dispara Start → End sin pasar por turnos. Síntoma: aparecen los modelos un instante y se van. No es bug, es comportamiento esperado con record vacío — pero conviene verificar que `endHoldSeconds` mantenga visible el "Empate"/"Final" un momento.
- 🟡 Si el `CombatRecord` viene de async (server-side), `OpponentName` puede no matchear ningún DNA del registry → llamada `Play` necesita un dnaB válido a mano (no reconstruible del record solo). Documentado como fuera de scope; bloqueante real solo cuando se integre el botón "Replay" en el panel de Resultados.

---

### Sesión 17 (histórico) — 2026-06-21

**Session:** 2026-06-21 (Session 17 — Sistema de NPC compradores: arquitectura + implementación Fase 1) — **CÓDIGO HECHO, SIN testear en Play (sesión de pura arquitectura por decisión de Juan)**
**Focus:** Diseñar e implementar el sistema completo de NPCs compradores. 3 pilares: NPC humanoide con FSM + estantes (`StoreContainer` ya furniture) + caja registradora (`CashRegister`, nueva furniture comprable). NPC pasea, inspecciona N displays según arquetipo, elige un MoriMonchi, se forma en cola tipo árbol ternario (Back/Left/Right por eslabón), espera atención en la caja con timeout, negocia 1 contraoferta. UITK barra superior con estado mínimo + panel de transacción en la caja (3 botones Aceptar/Pedir más/Rechazar) + price tag world-space en cada `StoreContainer`. Venta = `BusyReason.Sold` (no Remove, no IsDead).

**Decisiones de arquitectura (Juan):**
- NPC humanoide, archivo único (NO partial — clase chica, no aplica regla 11).
- Cascada de responsabilidad: `CustomerService.Instance` apex (dueño de SOs); `NpcController.Instance` apex de spawn; `CashRegister.Instance` apex de cola.
- Cola visible tipo árbol ternario por eslabón (BFS para el primer slot libre, `slotSpacing` único derivado a 3 offsets locales). Al avanzar la cola se recolectan ocupantes BFS y se re-asignan slots desde la raíz.
- Caja registradora = FURNITURE comprable en el ShopCatalog (consistente con la regla: todos los objetos son furniture). Validación de unicidad en el `BuildModeController` queda pendiente del lado Unity.
- ValuationHandler puro + NegotiationFlow puro (sin estado, instanciados por `CustomerService`). Fórmula simple multiplicadores configurables vía `CustomerPricingSO`.
- StoreContainer emite `OnDisplayContentsChanged(IReadOnlyList<MoriMochiAgent>)` por polling barato del count (sin tocar `MoriMochiContainer`).
- 5 eventos nuevos en `GameEvents`: Spawned/Decided/ArrivedAtRegister/Sold/Left.
- `BusyReason.Sold = 3` y `UIPanelType.Transaction = 7` agregados.
- "Pedir más" mínimo: 1 botón, sube `pricing.RenegotiationStep` (def 20%), 1 sola contra, NPC acepta según `archetype.RenegotiationTolerance` (random vs umbral).

**Bug evitado (mismatch de tipo, fix aplicado):**
- `StoreContainer.Occupants` (heredado de `MoriMochiContainer`) es `IReadOnlyList<MoriMochiAgent>`, NO `MoriMonchiController`. El sub-agente UI había escrito `StoreContainerPriceTagUITK.HandleChanged/Rebuild` con `MoriMonchiController` → arreglado a `MoriMochiAgent` (`.DNA` se accede igual: `MoriMochiAgent.Brain.cs:15 public CreatureDNA DNA => dna;`). `NpcAgent.BestPickFromContainer` usa `var` en el foreach → compila por inferencia.

**Files Touched (input para ScriptNodes — agente haiku al cierre):**
- `Core/Enums.cs`: `+ BusyReason.Sold`, `+ UIPanelType.Transaction`.
- `Core/GameEvents.cs`: + 5 eventos de customers (Spawned/Decided/ArrivedAtRegister/Sold/Left).
- `World/Containers/StoreContainer.cs`: + evento `OnDisplayContentsChanged` por polling de `Occupants.Count`.

**Files Created:**
- `Data/Customers/CustomerPricingSO.cs` (base por tier + multiplicadores + step renegociación).
- `Data/Customers/CustomerArchetypeSO.cs` (preferencia + presupuesto + tolerancia + browsing + timeout).
- `Data/Customers/CustomerArchetypeDatabaseSO.cs` (pool + RandomArchetype).
- `Systems/Customers/ValuationHandler.cs` (cálculo puro de oferta inicial).
- `Systems/Customers/NegotiationFlow.cs` (ComputeCounter + EvaluateCounter).
- `Systems/Customers/CustomerService.cs` (apex `.Instance`, dueño de SOs).
- `World/Npc/NpcController.cs` (apex spawn, cadencia, despawn).
- `World/Npc/NpcAgent.cs` (FSM humanoide, archivo único, 8 estados).
- `World/Containers/CashRegister.cs` (árbol ternario de cola + IInteractable via PanelTrigger del prefab).
- `UI/TransactionPanelUITK.cs` (panel del UIManager: 3 botones, suscribe `OnCurrentCustomerChanged`).
- `UI/NpcStatusBarUITK.cs` (overlay standalone estilo `InfoOverlayUITK`, estados textuales en ES).
- `UI/StoreContainerPriceTagUITK.cs` (world-space, usa `CustomerService.EstimateAverage`).
- `UI Toolkit/TransactionPanel.uxml/.uss`, `NpcStatusBar.uxml/.uss`, `StoreContainerPriceTag.uxml/.uss`.

**PASO MANUAL PENDIENTE (Juan, Unity) — bloqueante para Play:**
1. Crear los SO assets: `CustomerPricing.asset`, N `CustomerArchetype.asset`, `CustomerArchetypeDatabase.asset` (con archetypes adentro).
2. Asignar `pricing` + `archetypes` en el componente `CustomerService` (mismo GameObject que GameManager, sugerido).
3. Crear prefab humanoide placeholder con `NavMeshAgent` + `NpcAgent`.
4. Crear prefab `CashRegister`: collider, `PlacedFurnitureMarker`, `PanelTrigger(panel=Transaction)`, `CashRegister`, child empty "QueueAnchor" asignado a `queueRoot`.
5. Crear `FurnitureDefinitionSO` para la caja + entrada en el `ShopCatalog` (categoría Functional, stock=1).
6. Validación de unicidad de caja en `BuildModeController` (no permitir colocar 2).
7. En el GameObject del NpcController: asignar `spawnPoint`, `exitPoint`, `displays` (lista de StoreContainers), `register`, `defaultAgentPrefab`.
8. En UIManager dict Odin: asignar slot `Transaction → [GameObject del TransactionPanel]` (panel con UIDocument apuntando a `TransactionPanel.uxml` + `TransactionPanelUITK`).
9. Crear GameObject standalone "NpcStatusBar" con UIDocument + `NpcStatusBarUITK`.
10. En el prefab del StoreContainer: agregar child con UIDocument world-space (apunta a `StoreContainerPriceTag.uxml`) + componente `StoreContainerPriceTagUITK`.

---

## TESTING REQUIRED — Sesión 18 (Juan, en Play)

Juan no testeó esta sesión (decisión: pura arquitectura). Esto es la checklist completa de validación en Play, ordenada por flujo. Si cualquier paso falla, anotar el síntoma y NO seguir con los siguientes hasta entender la causa.

**Pre-flight (compilación + wiring):**
- [ ] Reabrir Unity → recompila sin errores (en particular, sin "type or namespace 'X' could not be found"). Las 13 carpetas nuevas (`Data/Customers`, `Systems/Customers`, `World/Npc`) deben generar sus `.meta`.
- [ ] Crear los 3 SO assets vía menú: `RunRunSimulator/Customers/Customer Pricing`, `…/Customer Archetype` (al menos 2-3 arquetipos distintos), `…/Customer Archetype Database` (con la lista poblada).
- [ ] En `CustomerPricing.asset` pulsar "Seed Defaults" → confirmar `BasePricePerTier` se rellena (T1=20, T2=50, T3=120).
- [ ] Asignar `pricing` + `archetypes` en el componente `CustomerService` del GameObject que lo aloja.
- [ ] Crear prefab humanoide NPC con `NavMeshAgent` + `NpcAgent` (puede ser un cubito alto stand-in).
- [ ] Crear prefab `CashRegister.prefab`: collider sólido, `PlacedFurnitureMarker`, `PanelTrigger(panel=Transaction)`, `CashRegister`, child empty "QueueAnchor" asignado a `queueRoot`.
- [ ] Crear `Furniture3x2_CashRegister.asset` (FurnitureDefinitionSO) + agregar al `ShopCatalog` con stock=1.
- [ ] Validación de unicidad en `BuildModeController` (no permitir colocar 2 cajas) — pendiente de implementar.
- [ ] En el GameObject del `NpcController`: asignar `spawnPoint`, `exitPoint`, `displays` (arrastra los StoreContainers de escena), `register`, `defaultAgentPrefab`.
- [ ] En `UIManager` (dict Odin): asignar `Transaction → [GameObject del TransactionPanel]`. El panel necesita `UIDocument` (apuntando a `TransactionPanel.uxml`) + `TransactionPanelUITK`.
- [ ] Crear GameObject standalone "NpcStatusBar" con `UIDocument` (apuntando a `NpcStatusBar.uxml`) + `NpcStatusBarUITK`.
- [ ] En el prefab del `StoreContainer`: agregar child world-space con `UIDocument` (apuntando a `StoreContainerPriceTag.uxml`) + `StoreContainerPriceTagUITK`.

**Test 1 — Price tag del StoreContainer (mínimo viable):**
- [ ] Tirar un MoriMonchi a un StoreContainer → aparece el price tag con `{nombre} · {N} D` encima del estante.
- [ ] Sacarlo → el tag desaparece (`DisplayStyle.None`).
- [ ] Tirar 2 MoriMonchis → aparecen 2 filas.

**Test 2 — Spawn + wandering + inspección:**
- [ ] Spawnea un NPC al pasar `minSpawnInterval` (default 20s). Aparece en `spawnPoint`.
- [ ] El NPC camina hacia un StoreContainer con ocupantes (NavMesh path visible si Gizmos on).
- [ ] Aparece en la barra superior con su arquetipo y estado "Pensando…" o "Mirando…".
- [ ] Tras `InspectionDuration` (default 3s) parado frente al display, transiciona: o "Yendo a la caja" (eligió uno) o vuelve a "Mirando…" (otro display).

**Test 3 — Cola en árbol:**
- [ ] 1er NPC llega a la caja → ocupa el slot raíz (frente).
- [ ] 2do NPC llega → ocupa uno de Back/Left/Right del 1er NPC.
- [ ] 3er NPC llega → ocupa otro hueco entre los 3 disponibles.
- [ ] Si los slots se ven solapados en Play, ajustar `slotSpacing` en el inspector del `CashRegister`.

**Test 4 — Negociación:**
- [ ] Acercarse a la caja y tocar E → abre el `TransactionPanel`.
- [ ] El panel muestra: arquetipo del cliente, nombre del MM target, oferta inicial.
- [ ] "Aceptar" → suma Dabloons al inventario, MM marcado `BusyState=Sold`, NPC se va al `exitPoint`, despawn al llegar.
- [ ] "Pedir más" → la oferta sube `RenegotiationStep` (def +20%). Botón se deshabilita (1 sola contra).
- [ ] Si el NPC tiene `RenegotiationTolerance=1` siempre acepta; si =0 cierra y el NPC se va decepcionado.
- [ ] "Rechazar" → NPC se va al exitPoint, sin venta.

**Test 5 — Timeout:**
- [ ] Dejar a un NPC esperando en la caja sin abrir el panel → tras `WaitTimeoutSeconds` (def 60s) se va solo.

**Test 6 — Cola dinámica (BUG SOSPECHADO, ver review abajo):**
- [ ] 2 NPCs en cola. Vender al 1ro → el 2do debe avanzar al frente y caminar hasta el slot raíz.
- [ ] **Si el 2do se queda parado en su slot viejo en lugar de moverse adelante**, confirmamos el bug latente que detallo en el review.

**Test 7 — Filtros de venta:**
- [ ] Un MM en breeding/QueuedForCombat NO debe ser ofertable (`dna.IsBusy` true → `BestPickFromContainer` lo saltea).
- [ ] Un MM `Sold` desaparece del CreatureGrid UI / no se respawna en mundo (el filtro del spawner ya descarta `IsBusy` históricamente — confirmar).

---

## Review de arquitectura: ¿overengineering? Veredicto

Pasada crítica sobre los 12 archivos nuevos contra la regla 8 ("Sin complejidad innecesaria. Tres líneas similares > abstracción prematura"). **Veredicto general: el sistema NO está sobre-ingenierizado** — cada pieza responde a una decisión explícita de Juan (cola en árbol, archetype bundleado, SO pricing aparte, panel separado de overlay). Detectados 1 bug latente + 2 grasas cosméticas, ninguno bloqueante para testing.

**🟡 Bug latente (probable, revisar en Test 6):**
- `CashRegister.CurrentSlotOf(NpcAgent)` está expuesto pero **nadie lo consume**. `NpcAgent.TickQueueing` usa `reservedQueueSlot` (cache local de la primera reserva) y nunca repollea su slot real. Consecuencia: tras `AdvanceQueue` (cuando el front sale), los NPCs que quedaron en la cola reciben slot nuevo en el árbol, pero su `navAgent.destination` sigue apuntando al lugar viejo. **El 2do NPC se quedaría parado en lugar de avanzar al frente**.
- Fix futuro (S18, después de confirmar el síntoma): en `TickQueueing`, repollear `register.CurrentSlotOf(this)`; si difiere de `reservedQueueSlot`, actualizar `reservedQueueSlot` y llamar `SetDestination`. O — más limpio — un evento `OnSlotReassigned(NpcAgent, Vector3)` que `CashRegister` dispare desde `AdvanceQueue`. Elegir el más simple según se vea en Play.

**🟢 Grasa cosmética 1 — `NpcState.Spawned`:**
- Valor inicial del enum antes de `Initialize`. La FSM no lo ticka (no hay case en el switch). Podría eliminarse y arrancar directamente en `Wandering` por default (el primer `TransitionTo(Wandering)` ya pasa por `Initialize`). Cosmético, no bloqueante. Borrarlo es 1 línea menos.

**🟢 Grasa cosmética 2 — `inspectStandoff` no usado en `TickInspecting`:**
- Se usa solo al fijar el destino inicial en `TickWandering` (línea 89), pero el nombre sugiere que también afecta la distancia de chequeo de llegada. No es bug — el chequeo usa `arriveDistance`. Renombrar a `inspectDestinationOffset` haría el campo auto-documentado. Cosmético.

**Lo que NO es overengineering aunque parezca:**
- Árbol ternario en `CashRegister`: Juan pidió textualmente "atrás / izquierda / derecha del eslabón". El árbol implementa exactamente eso. Una List<Transform> serializada hubiera sido más simple pero NO cumple el requisito.
- 3 SOs separados (Pricing / Archetype / Database): consistente con el patrón existente (FurnitureDatabaseSO, CreatureDatabaseSO, etc.). No introducir un nuevo patrón ahorra carga cognitiva.
- `ValuationHandler` y `NegotiationFlow` como clases plain C# `[Serializable]` en archivos separados: cada una resuelve UNA responsabilidad pura. Fusionarlas en CustomerService crearía un God Object. Mantener separadas es la opción de menor resistencia futura.
- `BusyReason.Sold` en lugar de `bool IsSold` paralelo: reutiliza el campo de verdad (`BusyState`) y la propiedad derivada (`IsBusy`). El spawner / breeding / combat ya descartan los `IsBusy` → cero plumbing nuevo.

**Camino de menor resistencia confirmado en estas decisiones:**
- Caja registradora como furniture comprable (no como GO de escena hardcodeado) → reutiliza ShopCatalog, BuildModeController, PlacedFurnitureMarker.
- TransactionPanel como panel del UIManager (no como overlay custom) → reutiliza el stack LIFO + input maps + PanelTrigger.
- StoreContainerPriceTag como world-space UIDocument hermano (no como Canvas world-space) → consistente con la migración UITK del proyecto.
- Polling barato (`lastOccupantCount`) en `StoreContainer.Update` en lugar de hookear `Admit`/`Release` con virtual override → cero tocar `MoriMochiContainer` (sistema sensible — corrales, breeding, F1/F2 históricos).
- Reutilización del `PanelTrigger` existente en lugar de implementar `IInteractable` en `CashRegister` → 0 líneas de código de interacción.

**NEXT SESSION (18):**
1. Ejecutar Tests 1-7 arriba en orden. Reportar fallas.
2. Si Test 6 confirma el bug latente: aplicar el fix más simple (probablemente añadir polling en `TickQueueing` a `register.CurrentSlotOf(this)`).
3. Si todo OK → eliminar `NpcState.Spawned` (cosmético) y marcar etapa ✅ — primer mecanismo de monetización real del juego.
4. Volver al bug pendiente de Sesión 16 (1ª carga en frío del breeding cortejo — abierto en [[Index/11 - Technical Debt]]).

---

## Cierre Sesión 17 (formal)

- **Tipo de sesión:** 100% arquitectura + implementación, sin Play. Decisión explícita de Juan (no estaba físicamente para testear).
- **Volumen entregado:** 3 archivos `.cs` modificados + 12 archivos `.cs` nuevos + 6 archivos UXML/USS = **21 archivos** tocados/creados en una pasada.
- **Pipeline de ejecución usado:**
  - **Opus** (esta sesión): diseño + validación de decisiones + plan de archivos + revisión crítica final.
  - **5 sub-agentes Sonnet en paralelo** (1 wave): Foundations / Services / World-Npc / Furnitures / UI. Cada uno con contratos públicos de los vecinos para evitar mismatches → 1 mismatch real detectado y arreglado in-line (`MoriMonchiController` → `MoriMochiAgent`).
  - **1 sub-agente Haiku** para ScriptNodes del vault: 3 actualizados + 12 creados (per memoria de Juan: haiku en lugar de opencode/Deepseek que se cuelga).
- **Estado vault:** ScriptNodes al día, Active Context al día. `CLAUDE.md` no requiere update (no se introdujo regla nueva ni cambio de stack — el patrón "todo objeto físico es furniture comprable" ya estaba codificado).
- **Pendiente único antes de marcar etapa ✅:** validación en Play (Tests 1-7), realizable cuando Juan esté frente a Unity (S18).
- **Riesgo conocido:** 1 bug latente probable (cola dinámica tras `AdvanceQueue`), Test 6 lo detecta. Fix documentado.
- **Sin deuda nueva en [[Index/11 - Technical Debt]]:** no se introdujeron partial classes, ni `static SO Current`, ni saltos de capa. El árbol ternario en CashRegister es complejidad esencial (requisito de diseño), no deuda.

---

### Sesión 16 (histórico) — 2026-06-21

**Session:** 2026-06-21 (Session 16 — Breeding container: cortejo orientado al AGENTE (orbit/tend) + 3 comportamientos + bug de carga en frío) — **EN PROGRESO, 1ª carga SIN confirmar**
**Focus:** Mejorar el behavior dentro de los corrales: cortejo vivo (no congelado) con sub-estado por sexo, cría sale disparada al nacer, solo adultos crían, padres vuelven a merodear al nacer. **PARCIAL:** el feel + reload (2ª carga) funcionan y gustan; la 1ª carga en frío sigue fallando (pareja congelada + cría no sale del corral) → la raíz es el ORDEN DE CARGA, ver [[Index/11 - Technical Debt]] (🔴 ABIERTO — orden de carga). NO parchear más sin trazar primero.

**Modelo nuevo (contexto vs comportamiento):**
- El **corral** solo provee contexto social: quién está dentro + quién emparejado con quién. Empareja y dispara `EnterCourtship(partner, anchor)` UNA vez por agente; ya NO posa por frame.
- El **agente** es dueño del cortejo: estado `Courting` + sub-estado `CourtRole` por sexo. **Hembra (`Tend`):** darts cortos/rápidos alrededor del slot, mirando al macho. **Macho (`Orbit`):** orbita la posición VIVA de la hembra, mirándola. Velocidad boosteada (`courtSpeedMultiplier`). Visualmente distinto del penned no-emparejado (merodeo calmo).

**CONFIRMADO por Juan (funciona):**
- Feel orbit/tend ("me gusta cómo ha quedado", "está funcionando bien").
- Reload → retoma cortejo (2ª carga). **Fix persistencia:** `HomePenKey`/`HomePenSlot` se seteaban DESPUÉS del `RegistryChanged` de `StartBreedingAsync` → no persistían; ahora `TryRollPair` dispara `RegistryChanged` tras estamparlos. **+ `ManageCourtship` robusto:** `ResolveCourtAnchor` (slot si válido, si no centro del corral) → corteja aunque el slot no sobreviva.

**FALLA (fix + testear Sesión 17):**
- **1ª carga en frío:** pareja queda CONGELADA (no orbita) + cría NO sale del corral. Causa aguas abajo del orden de carga (los breeders no quedan bien como co-ocupantes del corral). Plan A-E en [[Index/11 - Technical Debt]]. **Trazar PRIMERO con SpawnDiagnostics.**
- Watchdog `RecoverIfStuckOffMesh` agregado como red de seguridad — **NO resolvió la 1ª carga** (el breeder roto no cumple kinematic+off-mesh). Revisar si mantener o reemplazar.

**Sin confirmar (dependían de testear hoy):**
- **Cría sale disparada:** `BirthLanding()` (afuera del corral, `birthEjectDistance`) + `RegisterBirthLaunch(child, muzzle, landing)` 3-args. Hoy depende de `FindOccupant` en `OnBreedingCompleted` → si los padres no son ocupantes (bug 1ª carga), no se registra el lanzamiento. **Fix correcto: dueño por `HomePenKey` (item E del debt).**
- **Solo adultos crían:** `AvailableOf` filtra `IsAdult(d)` (`LifeStageTable.GetStage(AgeDays) >= Adult`, incluye Elder); diagnóstico muestra "no adulto". Depende de que el `Life Stage Table` esté asignado en `BreedingController` (si no, no bloquea).
- **Padres a merodear al nacer:** `OnBreedingCompleted` → `ExitCourtship` + safety de `TickCourting` (la pareja deja de estar Breeding → auto-exit). Mismo downstream del bug 1ª carga.

**Files Touched (esta sesión — input para ScriptNodes, PENDIENTE correr agente haiku cuando se confirme en Play):**
- `World/AI/MoriMochiAgent.cs`: `baseSpeed`, enum `CourtRole`, campos de cortejo, `offMeshGrace`, llamada `RecoverIfStuckOffMesh` en Update.
- `World/AI/MoriMochiAgent.Confinement.cs`: `EnterCourtship(partner, anchor)` + `ExitCourtship` reescritos; normalización de rotación en `EnterConfinement`.
- `World/AI/MoriMochiAgent.Brain.cs`: `TickCourting`/`TickOrbit`/`TickTend`/`FacePartner`; `IsRecovering`/`IsBreeding`; restaura `agent.speed` en `EnterRoaming`.
- `World/AI/MoriMochiAgent.Physics.cs`: knock-proof por `IsBreeding`; `RecoverIfStuckOffMesh`.
- `World/AI/MoriMochiAgent.Tuning.cs`: tunables de cortejo (`courtSpeedMultiplier`/`courtOrbitRadius`/`courtAngularSpeed`/`courtLookahead`/`courtRepath`/`courtTendRadius`/`courtTendInterval`) + `offMeshRecoverDelay` + readout `CourtInfo`.
- `World/Containers/BreedingContainer.cs`: `ManageCourtship` solo-contexto + `ResolveCourtAnchor`; regla adultos (`IsAdult`) + diagnóstico; `BirthLanding`/`birthEjectDistance`; `RegistryChanged` tras estampar slot/key; reclaim espera `!IsRecovering`.
- `World/Spawning/MoriMochiSpawner.cs`: `DeferBreeder`/`breederPlaceTimeout`/`breederPlaceDeadline`; `RegisterBirthLaunch` 3-args + `birthLandingPoints`.

**Files Created:** ninguno.

**NEXT SESSION (17):**
1. **Trazar el orden de carga en frío** (SpawnDiagnostics + dev toggles) → confirmar dónde se rompe la colocación de breeders y qué pre-requisito falta. Ver [[Index/11 - Technical Debt]].
2. Implementar la solución correcta (items A-E) y testear: 1ª carga sana (pareja orbita), cría sale del corral, padres merodean, solo adultos crían.
3. Si todo OK → correr agente haiku para ScriptNodes (lista Files Touched arriba) + marcar etapa ✅.

---

### Sesión 15 (histórico) — 2026-06-21

**Session:** 2026-06-21 (Session 15 — Bugs post-refactor: desync color/genética + migración shader Toon + reset completo)
**Focus:** Tres bugs tras el refactor. (1) Breeding "Mother not found" + (2b) UITK en blanco = **misma causa raíz**. (2a) Migración al shader nuevo (Unity Toon). (3) "Reset All Progress" no borraba todo el estado MM server-side. **Confirmado funcionando en Play por Juan.**

**Causa raíz (1 + 2b):** `UniqueID` = `ToStringID()-Timestamp`, y `ToStringID()` **embebe el `BaseColor`** como RRGGBB. Saves viejos (era `PrimaryColor`, pre-S10) se cargaron sin migrar → `BaseColor` cayó a su default y se re-guardó: **color real en la KEY, blanco/negro en el value**. Cada load → `BaseColor` default → `UniqueID` recalculado ≠ key → `registry.TryGet` falla (breeding) y la UITK pinta el default. Verificado en disco (key `...5918EA...` vs value `FFFFFF`). El color real sobrevive en la genetic string de la KEY → **recuperable**.

**Fix 1 (self-heal):** `CreatureRegistrySO.LoadFrom` → `ReconcileColors` (+ `TryColorFromKey`): parsea el color de la KEY; si difiere del value restaura `BaseColor` y regenera `SecondaryColor`. Único embudo de carga → cubre **local + nube**. Tras el primer load+save queda reparado; queda como blindaje anti-desync permanente.

**Fix 2a (shader):** material nuevo `BaseFurNewTest` = Unity Toon Shader (props `_BaseColor`, `_1st_ShadeColor`, `_2nd_ShadeColor`, `_RimLightColor`). **Nuevo modelo de color**: `BaseColor` (genético, en la key, fuente de verdad) + `SecondaryColor` (slot derivado determinista del base). `ColorGenetics.BuildFurPalette(base, secondary)` orquesta los 4 colores del shader. Eliminados `ShadowsColor`/`OutlineColor` (todo deriva del base → reconstruible). El outline UTS es pass aparte (no lee MPB) → no se maneja por genética; el rim lo reemplaza. **Acople a nombres de shader sigue SOLO en MoriMonchiVisualizer** (ahora 4 PropertyIDs).

**Fix 3 (reset completo):** `CloudSyncService` Reset ahora borra TODO el estado MM: huevos (`cancel-all-breeding`), entries de combate (`dequeue-combat` por criatura `QueuedForCombat`), `combat_results`, además de las 4 keys de siempre. Usa **endpoints DIRECTOS** (`CloudEndpoint.CallAsync`) en vez de los wrappers de cliente para evitar el race `RegistryChanged → Persist → PushToCloud` (que re-subiría lo borrado). No requiere Cloud Code nuevo.

**Files Touched:**
- `Data/Genetics/CreatureRegistrySO.cs`: `ReconcileColors` + `TryColorFromKey` dentro de `LoadFrom`.
- `Data/Genetics/CreatureDNA.cs`: nuevo `SecondaryColor`; eliminados `ShadowsColor`/`OutlineColor`.
- `Core/ColorGenetics.cs`: `DeriveSecondary`, `BuildFurPalette`, `Shade`, struct `FurPalette`; eliminados `DeriveShadow`/`DeriveOutline`.
- `Core/CreatureGenerator.cs`: mint setea `SecondaryColor`.
- `Systems/Breeding/BreedingService.cs`: la cría setea `SecondaryColor` desde el base del hijo.
- `World/Creatures/MoriMonchiVisualizer.cs`: 4 PropertyIDs del Toon + `ApplyFur` usa `BuildFurPalette`.
- `Systems/Cloud/CloudSyncService.cs`: consts `COMBAT_RESULTS_KEY` / `CANCEL_ALL_BREEDING` / `DEQUEUE_COMBAT`.
- `Systems/Cloud/CloudSyncService.Sync.cs`: `ResetProgressAsync` borra huevos + cola de combate + `combat_results`.

**Files Created:** ninguno.

**Unity (hecho por Juan):** `FurTypeDatabase.asset` → materiales con el shader nuevo (`BaseFurNewTest`); recompiló; probó en Play (mint con color, breeding sano, UITK con color, Reset deja todo limpio local+nube).

**NEXT SESSION:** verificar la cadencia del spawner al hatchear varios huevos ("spawnea de golpe", `spawnInterval`/`spawnPerTick`) si reaparece. Seguir con features de gameplay.

---

### Sesión 14 (histórico) — 2026-06-21

**Session:** 2026-06-21 (Session 14 — Spawning: diagnóstico + toolkit de debug + fixes + gate data-ready)
**Focus:** Dos bugs del spawning: (1) al nacer un MM se "re-instanciaban TODOS", (2) al reactivarse el NavMeshAgent las criaturas se pineaban de golpe al piso. Diagnóstico instrumentado + fix raíz + blindaje estructural. Confirmado funcionando en Play por Juan.

**Diagnóstico (cerrado con evidencia del `SpawnDiagnostics`):**
- El snap al piso = `agent.enabled = true` teletransporta el transform al punto más cercano del NavMesh. Pasa en `RestoreNavMeshControl` (lo llama `Initialize`) y en los handoffs de rebake/recovery.
- El "re-instancia todos" = `MoriMochiSpawner.OnRegistryReloaded` llamaba `controller.Initialize()` sobre CADA criatura spawneada → snap masivo. **El gatillo NO es el nacimiento** (Register solo agrega; HatchLocally solo emite `RegistryChanged` → `Sync` quirúrgico). Los únicos que disparan `RegistryReloaded` en runtime son `CloudSyncService.OnSignedInComplete` (reload local) y `PullAsync` (reload nube) — éste último **llega tarde** (8.6s de latencia observada) y al caer después de spawnear, despawnea+re-dispara cuando local≠nube. Confirmado: en el run con local==nube y pull rápido → `Reload reconcile → rebound=0 despawned=0` (cero churn).

**Fixes (raíz + blindaje):**
- **Rebind (fix raíz):** `OnRegistryReloaded` ahora usa un `Rebind` liviano (re-vincula DNA + `RefreshFur`, SIN tocar nav/estado/posición) en vez de `Initialize`. Cascada nueva: `spawner.OnRegistryReloaded → controller.Rebind → agent.Rebind + visualizer.RefreshFur`. Solo churna el delta real (stale despawn, nuevos enqueue); las coincidencias hacen Rebind sin snap.
- **Anti-snap defensivo:** `RestoreNavMeshControl` hace `Warp` a la posición actual tras habilitar el agente (consistente con `RejoinNavMesh`/`TickRecovering`).
- **Gate data-ready (estructural):** el spawner ya NO puebla desde el `.asset` antes de la data autoritativa. Campo `dataReady` (gate en `EnsurePump`), se setea en el primer `OnRegistryReloaded`; coroutine `DataReadyFallback` (timeout `dataReadyTimeout`, def 6s) cubre offline/sin nube. Puebla UNA sola vez del roster correcto; reloads posteriores reconcilian vía Rebind.

**Toolkit de debug (nuevo):**
- `World/Spawning/SpawnDiagnostics.cs` (componente standalone, solo bus público → patrón F3): loguea cada `RegistryChanged`/`Reloaded`/`BreedingCompleted`/rebake con frame+tiempo + contadores; warning ruidoso en Reloaded.
- `MoriMochiAgent` pestaña Dev (en `.Tuning.cs`, excepción de tooling): readouts `CurrentState`/`NavStatus`, `forceRagdoll` (nunca rejoina), `logStateTransitions` (+ detector de snap por salto de `y`).
- `MoriMochiSpawner.Debug.cs`: botón "Dump Spawn State". Núcleo: log "Reload reconcile → rebound/despawned/enqueued".

**Files Touched:**
- `World/Spawning/MoriMochiSpawner.cs`: `Rebind` en OnRegistryReloaded + log reconcile; gate `dataReady` (campo, `EnsurePump`, `DataReadyFallback`, `dataReadyTimeout`, readout).
- `World/AI/MoriMochiAgent.cs`: método `Rebind` (público); hooks dev en Update (`forceRagdoll` + `DevTrackState`).
- `World/AI/MoriMochiAgent.Tuning.cs`: pestaña Dev (readouts + `forceRagdoll`/`logStateTransitions`/`snapWarnThreshold` + `DevTrackState`).
- `World/AI/MoriMochiAgent.Physics.cs`: guard `if (forceRagdoll) return;` en `TickThrown`.
- `World/AI/MoriMochiAgent.Confinement.cs`: anti-snap Warp en `RestoreNavMeshControl`.
- `World/Creatures/MoriMonchiController.cs`: passthrough `Rebind`.
- `World/Creatures/MoriMonchiVisualizer.cs`: `RefreshFur` público (wrap de `ApplyFur`).
- `World/Spawning/MoriMochiSpawner.Debug.cs`: botón "Dump Spawn State".

**Files Created:**
- `World/Spawning/SpawnDiagnostics.cs`.

**Nota dev:** los `Debug.Log` de "Reload reconcile" y `SpawnDiagnostics` quedan como instrumentación viva (útil mientras se sigue debugeando el spawning). Quitar/gatear cuando se cierre el tema.

**NEXT SESSION:** spawning robusto (sin churn/snap, pobla del autoritativo). Pasar a la sesión de debugs general que Juan mencionó.

---

### Sesión 13 (histórico) — 2026-06-20

**Session:** 2026-06-20 (Session 13 — F0 Higiene: namespacing + reorg de carpetas)
**Focus:** Cerrar Fase 0. Namespace raíz único en los 116 scripts + reorganización de `Scripts/World` y `Scripts/Data` en subcarpetas por dominio. Decidido por Juan pese a recomendación de saltar el namespacing (payoff cosmético con assembly único); ejecutado con auditoría de serialización previa para descartar pérdida de datos.

**Decisiones:**
- **Namespace:** raíz único `MoriMonchiSimulator`, **block-scoped** (`namespace X { }`). File-scoped (`namespace X;`) es C# 10 y Unity 6.3 compila C# 9 → no disponible.
- **NO re-indentado:** se insertan solo 3 líneas por archivo (`namespace` / `{` / `}`); el cuerpo queda en columna 0 dentro del namespace (legal, compila idéntico). Motivo: diff mínimo y revisable + cero riesgo a strings. Indentado bonito = paso opcional posterior con Reformat del IDE (Rider `Ctrl+Alt+L`) como commit cosmético aparte.
- **`#region`:** la auditoría de `Index/11` lo listaba; medido = **0 ocurrencias** en todo el código → ítem moot, tachado.

**Reorg (47 `.cs` movidos con su `.cs.meta` → GUID intacto, cero refs rotas):**
- `World/` (24) → `AI/` (MoriMochiAgent×5), `Spawning/` (MoriMochiSpawner×2, SpawnBallistics, ControllerPool), `Creatures/` (MoriMonchiController, MoriMonchiVisualizer, FurRenderer, BodyPartJoint, NameTag), `Containers/` (MoriMochi/Breeding/Store Container), `Needs/` (NeedStation, NeedStationRegistry, Feeder, PlayZone, RestZone), `Props/` (WorldPropInstance, HotbarController).
- `Data/` (24 sueltos) → por dominio: `Databases/` (+Creature/Furniture/Item/FurType/PartVisualBank), `Genetics/` (CreatureDNA, RarityOdds, PersonalityProfile, CreatureRegistry, Creature/PartNameBank), `Breeding/` (BreedingAffinity, InheritanceOdds, CreatureLifeStage), `Combat/` (CombatManager, CombatLogEntry/Record/Result), `Furniture/` (FurnitureDefinition/Registry, PlacedFurniture), `Items/` (ItemDefinition), `Player/` (PlayerInventory). `NeedsState` queda en raíz.
- **`CreateAssetMenu` reorganizado:** los 25 `menuName` que estaban planos bajo `RunRunSimulator/` ahora cuelgan de subcarpetas que espejan los dominios (`Databases/`, `Parts/`, `Genetics/`, `Breeding/`, `Furniture/`, `Combat/`, `Items/`, `Player/`, `Store/`). Solo cambia el menú Assets→Create; assets existentes intactos (tipo por GUID).

**Namespacing (116 `.cs`):** script PowerShell determinista (no subagentes — transform uniforme, más predecible). Inserta tras el último `using` (o antes del primer tipo si no hay using; 4 casos: Enums, Feeder, PlayZone, RestZone), `}` al EOF. Preserva CRLF + estado de BOM exactos. Guardas: salta si ya tiene namespace, `break` antes del cuerpo de clase (no confunde `using(...)` de método). **Validado: los 116 con exactamente 1 namespace, llaves balanceadas, cierre en `}`.**

**Auditoría de serialización (clave — descarta pérdida de datos):** 0 `SerializeReference` / `TypeNameHandling` / `$type` / binders custom. Newtonsoft serializa por nombre de propiedad (enums por `StringEnumConverter`), deserializa en tipos genéricos explícitos → tipo resuelto en compilación, no del JSON ⇒ saves locales/cloud/DNA string namespace-safe. Odin: Parts son `SerializedScriptableObject` (refs por GUID); dicts autorales con claves enum / tipos concretos (resueltos por firma de campo, no polimórficos) ⇒ namespace-safe. Registry `.asset` es espejo del JSON (re-Sync).

**PASO MANUAL PENDIENTE (Juan, Unity):** reabrir Unity → reimporta (genera `.meta` de las 13 carpetas nuevas) y recompila. **Confirmar 0 errores de `using`/namespace.** De paso, ojear que los assets Odin autorales (FurTypeDatabase, BreedingAffinityTable, InheritanceOddsTable, PersonalityProfileTable, CreatureLifeStageTable, Part DBs) muestren sus entradas — deberían; si alguno saliera vacío, restaurar ese `.asset` de git (no debía cambiar). Opcional: Reformat del IDE para indentar.

**Cierre de vault (mantenimiento, 2026-06-20):**
- **Tags limpiados:** de 54 tags (40+ usados 1 vez) a **10 canónicos**. Cada nota = `[tipo, dominio]` con tipo ∈ {script, index, archive} y dominio ∈ {core, genetics, combat, ui, world, cloud, furniture}. Eliminado `memory-bank` (estaba en 116/120 → sin valor de filtro) y toda la cola. Aplicado por script determinista (vocabulario controlado).
- **ScriptNodes (haiku):** refrescada la línea `**Ruta:**` de los 40 nodos cuyos scripts se movieron (los 5 partials no tienen nodo propio). Creado nodo faltante `FurRenderer.md` y poblado el stub vacío `RarityOddsTableSO.md`. Vault completo: todo script con nodo, rutas al día.
- No se tocó responsabilidad/conexiones del resto (no cambió contrato — solo ubicación + namespace + menú).

**NEXT SESSION:** F0 cerrada + vault sincronizado a la fecha (queda solo el reindentado cosmético opcional del namespace). Volver a features de gameplay / probar en Play el cableado de F3/F4.

---

### Sesión 12 (histórico) — 2026-06-20
**Focus:** F3 (slim Core/Systems) resuelto por **componentes independientes, NO partial** (corrige el enfoque de F1/F2). Codificación de la **regla 11** (disciplina de partial class) en `CLAUDE.md`. Auditoría y corrección de los partials previos: convertir lo puro, documentar el resto como excepción justificada.

**Decisión de arquitectura (Juan):** partial class solo por ventaja física de archivo (Git / código generado). Si la clase creció, el remedio es dividir en clases/componentes independientes. Tooling dev y código puro nunca en partial. Núcleo con estado mutable irreducible: partial SOLO documentado como deuda en `Index/11`. Sin abusar de static: las consolas dev usan **referencias serializadas explícitas** (no `.Instance`, no SO `.Current`) → trazabilidad de quién depende de qué.

**Files Touched:**
- `CLAUDE.md`: principio 3 refinado + regla 11 nueva.
- `Core/GameManager.cs` (318→177): solo persistencia/assets/Mint.
- `Systems/Breeding/BreedingController.cs` (281→108): solo dominio; `BreedCreatures` devuelve childID; header actualizado.
- `Systems/Combat/CombatController.cs` (281→77): solo dominio + API (`Config`, `SimulateLocal`, wrappers async, `EnqueueForAsyncCombat`→`Task`).
- `World/MoriMochiSpawner.cs`: call-sites puros → `SpawnBallistics`; `RandomLandingPoint`/`ResolveActivationPoint` movidos al núcleo.
- `World/MoriMochiSpawner.Debug.cs`: call-sites → `SpawnBallistics`.

**Files Created:**
- `Core/GeneticsLabPreview.cs`, `Core/DevToolsConsole.cs`
- `Systems/Breeding/BreedingDevConsole.cs`
- `Systems/Combat/CombatDevConsole.cs`
- `World/SpawnBallistics.cs` (static, matemática de lanzamiento)
- `World/ControllerPool.cs` (pool independiente con su propia Queue; el spawner pide Get/Return)

**Files Deleted:**
- `World/MoriMochiSpawner.Ballistics.cs` (+ `.meta`)
- `World/MoriMochiSpawner.Pool.cs` (+ `.meta`) — su lógica de pooling salió a `ControllerPool`; `Despawn`/`ClearAll` (spawn-tracking) movidos al núcleo de `MoriMochiSpawner.cs`

**Auditoría de partials:** solo `Ballistics` era convertible (puro). Resto = excepción documentada en `Index/11`: `MoriMochiAgent` (FSM irreducible), paneles UITK (árbol VisualElement), `CloudSyncService` (sesión de red), `MoriMochiSpawner` (Pool/Debug: estado privado + gizmos).

**Wiring Unity ✅ HECHO (Juan):** los 4 dev consoles agregados al GameObject con sus refs `[Required]` (`GeneticsLabPreview`, `DevToolsConsole`, `BreedingDevConsole`, `CombatDevConsole`). Confirmado compilando y funcionando en Play.

**ScriptNodes:** el agente Deepseek/opencode se colgó; se rehízo con un **subagente Claude (haiku)** que SÍ funcionó → 4 nodos actualizados (GameManager, BreedingController, CombatController, MoriMochiSpawner) + 6 creados (GeneticsLabPreview, DevToolsConsole, BreedingDevConsole, CombatDevConsole, SpawnBallistics, ControllerPool). Nodos al día. **Nota de proceso:** para futuros cierres, usar subagente haiku en vez de opencode/Deepseek (este último colgó sin output).

**MoriMochiAgent (auditoría 2026-06-20):** se evaluó decomponer el FSM y se DESCARTÓ con evidencia dura (ver `Index/11`): los 3 concerns comparten `state` + ~30 campos mutables + ~10 timers y se llaman entre sí; un blackboard relocalizaría el acoplamiento a un contexto público (peor). Queda como excepción documentada de la regla 11.

**F4 + F5 HECHAS (2026-06-20, modelo cascada de Juan):** cada dominio = pirámide con apex dueño de sus refs; hijos piden al apex por `.Instance` (servicio runtime, permitido); SO `static Current` eliminados (perdían trazabilidad de dueño).
- F4 — 5 `Current` fuera: CombatManagerSO→CombatController (config serializado + nuevo `Instance`); InheritanceOddsTableSO→BreedingController (serializado, getter `InheritanceOdds`, sacado de GameManager); BreedingAffinityTableSO→BreedingController (ya serializado, quitado fallback); FurTypeDatabaseSO→GameManager (raíz) ruteado spawner→`controller.Initialize(furDb)`→`visualizer.SetFurDatabase`; PersonalityProfileSO→queda en GameManager, ya llegaba por Initialize.
- F5 — `CloudEndpoint` (static, `CallAsync`/`CallAsync<T>`) deduplica call+deserialize en AsyncCombat/AsyncBreeding (la reconciliación queda per-servicio).
- Files: GameManager, CombatController, AsyncCombatService, CombatPanelUITK, CombatManagerSO, BreedingController, AsyncBreedingService, BreedingPanelUITK.Content, InheritanceOddsTableSO, BreedingAffinityTableSO, MoriMonchiController, MoriMonchiVisualizer, MoriMochiAgent, FurTypeDatabaseSO, PersonalityProfileSO (mod) + CloudEndpoint (nuevo). Verificado: 0 `.Current` residuales, llaves OK.
- Wiring Unity ✅ HECHO (Juan): `config` en CombatController, `inheritanceOddsTable` en BreedingController, `furTypeDatabase` en GameManager asignados. Confirmado funcionando en Play (cascada SO operativa, sin nulls).

**NEXT SESSION:** todas las F del roadmap de saneamiento cerradas (F0 namespacing saltado por decisión). Volver a features de gameplay / probar en Play el nuevo cableado (cascada SO + dev consoles de F3). Backlog opcional vivo en Index/11: tabs UITK→sub-presenters; FSM MoriMochiAgent vía blackboard (descartado con evidencia).

---

### Sesión 11 (histórico) — 2026-06-19
**Session:** 2026-06-19 (Session 11 — Mantenimiento / Refactor)
**Focus:** Auditoría de arquitectura + saneamiento. Mapa por capas, hoja de ruta viva en `Index/11 - Technical Debt`, regla de arquitectura general codificada en `CLAUDE.md`. F0 (higiene): sin código muerto (CombatManagerSO vivo, CloudCodeTester es dev tool). F1 (partir `MoriMochiAgent`): hecho vía `partial class` (no componentes — el FSM comparte núcleo mutable; separarlo empeoraría el acoplamiento).

**Files Touched:**
- `CLAUDE.md`: nueva sección "Regla de arquitectura general" antes de las 10 reglas (4 principios: capas sin saltos de 2 niveles, comunicación solo por bus/servicio, límite ~400 líneas/1 dominio, singleton=servicio / SO=data).
- `World/MoriMochiAgent.cs`: reducido a núcleo (~243 líneas) — campos, lifecycle, dispatch, helpers NavMesh compartidos, gizmos. Clase ahora `partial`. Cero cambio de comportamiento ni de API pública (verificado: diff de contenido exacto contra el original de 1189 líneas).

**Files Created:**
- `World/MoriMochiAgent.Tuning.cs` (~201): `[SerializeField]` Odin + readouts + dev buttons.
- `World/MoriMochiAgent.Brain.cs` (~358): estados + needs + reacciones + intent + queries.
- `World/MoriMochiAgent.Physics.cs` (~274): colisión/knock/throw/ragdoll/recovery/handoff.
- `World/MoriMochiAgent.Confinement.cs` (~136): pen + courtship + rebake survival + pooling.

F1 compilado OK por Juan. **F2 (partir paneles UI gigantes) HECHO** con el mismo patrón partial-class (los paneles comparten estado UI mutable → componentes empeorarían el acoplamiento):
- `UI/CombatPanelUITK.cs` (849→241 núcleo) + creados `UI/CombatPanelUITK.Tabs.cs` (390) + `UI/CombatPanelUITK.Navigation.cs` (234). Verificado exacto (563 líneas), sin cambio de comportamiento.
- `UI/BreedingPanelUITK.cs` (637→170 núcleo) + creados `UI/BreedingPanelUITK.Content.cs` (273) + `UI/BreedingPanelUITK.Navigation.cs` (210). Verificado exacto (418 líneas).
- Patrón UITK establecido: **Core (lifecycle+wiring+data) / Content|Tabs (build+bind) / Navigation (IUINavigable+foco)**.
- `UI/MorimonchiDetailInfoUITK.cs` (478→289 núcleo+Info+Combat) + creado `UI/MorimonchiDetailInfoUITK.Trees.cs` (196, tabs Linaje/Descendencia). Verificado exacto (327 líneas). **Todos los paneles UI >400 líneas partidos.**
- `World/MoriMochiSpawner.cs` (622→398 motor) + creados `.Pool.cs` (62), `.Ballistics.cs` (66), `.Debug.cs` (123, dev+gizmos). Verificado exacto (404 líneas).
- `Systems/Cloud/CloudSyncService.cs` (568→133 núcleo+meta) + creados `.Auth.cs` (246), `.Sync.cs` (218, validate+reset+push+pull). Verificado exacto (353 líneas).

**RESUMEN SESIÓN 11:** 6 monstruos partidos vía partial-class (Agent 1189, CombatPanel 849, BreedingPanel 637, DetailInfo 478, Spawner 622, CloudSync 568). **14 partials nuevos**, 6 núcleos reducidos. Todos verificados byte-exactos + llaves balanceadas + GUIDs de prefab intactos. Cero cambio de comportamiento/API. **Compilado OK por Juan** (Unity generó los 14 `.meta`).

**CIERRE SESIÓN 11 (2026-06-20):** agente externo Deepseek ejecutado OK → 6 ScriptNodes actualizados con sección "Organización (partial class)" (MoriMochiAgent, CombatPanelUITK, BreedingPanelUITK, MorimonchiDetailInfoUITK, MoriMochiSpawner, CloudSyncService), sin tocar responsabilidad/conexiones. Hoja de ruta viva en `Index/11`.

**NEXT SESSION (arranque):** F3 — adelgazar `GameManager` + separar debug/dominio en `BreedingController`/`CombatController` (refactor de LÓGICA, no split). Splits menores pendientes: `BreedingContainer` (468), `AsyncCombatService` (462), `BuildModeController` (418). F0-namespacing aún pendiente (decisión de Juan: vale la churn o no).

**PASO MANUAL PENDIENTE (Juan, Unity):** reimportar para que Unity genere los `.meta` de los partials nuevos (MoriMochiAgent ×4, CombatPanelUITK ×2, BreedingPanelUITK ×2) y recompile. Las referencias de componentes en prefabs/escena NO cambian (mismos `.cs.meta`/GUID de los archivos núcleo). Confirmar compilación sin errores de `using`.

**Next Session Goal:** F3 (adelgazar GameManager + separar debug/UI de dominio en Controllers) o F2-extra (`MorimonchiDetailInfoUITK` 478 con el mismo patrón). Hoja de ruta completa en `Index/11`.

---

### Sesión 10 (histórico) — 2026-06-19
**Session:** 2026-06-19 (Session 10)
**Focus:** FurType + sistema de 3 colores genéticos (BaseColor / ShadowsColor / OutlineColor). Cada MoriMochi tiene un FurType (enum) que mapea a un Material CartoonShader vía FurTypeDatabaseSO; las partes del cuerpo comparten ese material y los 3 colores se aplican per-criatura por MaterialPropertyBlock. Herencia: los 3 colores se cruzan de los padres con variación HSV; el FurType se hereda 50/50.

**Files Touched:**
- `Core/Enums.cs`: nuevo enum `FurType { Smooth, Fluffy, Spiky, Shaggy, Scaly }` (metadata, NO parte del DNA string; mapea 1:1 a un material en FurTypeDatabaseSO).
- `Data/CreatureDNA.cs`: `PrimaryColor` → renombrado a `BaseColor`; nuevos `ShadowsColor`, `OutlineColor` (Color, `[ColorUsage(false)]`) y `FurType FurType`. El genetic string NO cambia de formato (BaseColor sigue ocupando la posición RRGGBB en `ToStringID()`/`FromID()`; los otros 3 son metadata JSON como Gender/Personality → no rompen UniqueIDs ni lineage refs).
- `Core/CreatureGenerator.cs`: mint genera `BaseColor` aleatorio (`ColorGenetics.RandomBase`) + `ShadowsColor`/`OutlineColor` derivados, y FurType aleatorio (mismo patrón Enum.GetValues que RandomPersonality).
- `Systems/Breeding/BreedingService.cs`: el hijo hereda los 3 colores vía `ColorGenetics.Inherit(mother.X, father.X)` (blend + jitter HSV) y FurType 50/50 vía `ColorGenetics.Inherit(furM, furF)`. Reemplaza el color full-random anterior.
- `World/MoriMonchiVisualizer.cs`: PropertyIDs cacheados ahora son los del CartoonShader (`_Base_Color`, `_Shadows_Color`, `_Outline_Color`). Eliminados `ApplyColor`, el campo `bodyRenderer` y `ApplyPersonalityTint`. Nuevo `ApplyFur(dna)`: resuelve material vía `FurTypeDatabaseSO.Current.GetMaterial(dna.FurType)`, recolecta TODOS los renderers bajo modelRoot, asigna ese material a cada uno (si existe) y aplica UN MaterialPropertyBlock con los 3 colores. El color ya no es solo del body: todas las partes lo heredan.
- `World/MoriMonchiController.cs`: en `Initialize` eliminadas las líneas del personality tint (resolución del profile + `ApplyPersonalityTint`). El `Tint` de PersonalityProfileSO queda intacto, reservado para colorear los OJOS a futuro (fuera de alcance hoy).
- UI (rename `PrimaryColor`→`BaseColor`, sin cambio de lógica): `UI/MorimonchiDetailInfoUITK.cs`, `UI/CreatureVisualUI.cs`, `UI/CreatureGridView.cs`, `UI/CreatureGridUITK.cs`, `UI/CombatPanelUITK.cs`, `UI/BreedingPanelUITK.cs`.

**Files Created:**
- `Core/ColorGenetics.cs`: helper estático centralizado de genética de color. `RandomBase()`, `DeriveShadow(base)` (V×0.55), `DeriveOutline(base)` (V×0.25), `Inherit(Color,Color)` (lerp aleatorio + jitter HSV ±0.04 H / ±0.05 S,V), `Inherit(FurType,FurType)` (50/50). Lo usan CreatureGenerator y BreedingService.
- `Data/FurTypeDatabaseSO.cs`: `SerializedScriptableObject` con singleton `Current` (auto-registro en OnEnable, patrón BreedingAffinityTableSO). Dict Odin `FurType→Material` + `GetMaterial(type)` (fallback null + warning) + botón "Populate from Enum".

**PASO MANUAL PENDIENTE (Juan, en Unity):**
- Crear el asset `FurTypeDatabase` (menú RunRunSimulator/Databases/Fur Type Database), pulsar "Populate from Enum", crear un Material CartoonShader por FurType (variando solo el look, colores en neutro) y asignarlos en el dict. Sin materiales, las criaturas igual reciben sus 3 colores por MPB sobre el material que tengan los prefabs.

**Nota de shader (Juan avisó cambio inminente):** el shader es intercambiable. El ÚNICO acople a nombres de propiedad del shader vive en `MoriMonchiVisualizer.cs` (3 `Shader.PropertyToID`: `_Base_Color`, `_Shadows_Color`, `_Outline_Color`). Al cambiar de shader, actualizar SOLO esos 3 IDs para que coincidan con las propiedades del nuevo shader; el resto del pipeline (DNA, herencia, FurTypeDatabaseSO) no se toca.

**Next Session Goal:** crear el asset FurTypeDatabase + materiales, probar en Play que cría y mint pintan base/sombra/contorno y que las crías heredan colores+furtype. Posible swap de shader.

---

### Sesión 9 (histórico) — 2026-06-18
**Focus:** BreedingContainer — edad/etapa de vida en NameTag, corral de origen (HomePenKey), orden de carga (ocupados directos + libres por cañón), recién nacido lanzado desde su corral, contador de crías 0/MaxBreedCount.

**Files Touched:**
- `Core/Enums.cs`: nuevo enum `LifeStage { Newborn, Child, Teen, Adult, Elder }` (display-only, no parte del DNA string).
- `Data/CreatureDNA.cs`: `HomePenKey` (string, corral donde cría; persiste); `AgeDays` (int, días vividos desde BirthDate UTC).
- `World/MoriMonchiController.cs`: expone `public MoriMochiAgent Agent` (lo usa el spawner para colocación directa).
- `World/NameTag.cs`: línea `stage-label` = "{etapa} · {días}d" (ambos layouts, consulta `CreatureLifeStageTableSO.Current`); `breed-label` = "{BreedCount}/{MaxBreedCount}" (solo layout corral). Helper `StageText`.
- `UI Toolkit/NameTagUITK.uxml` + `NameTagUITKStyle.uss`: labels `stage-label` (.tag__stage) y `breed-label` (.tag__breed).
- `World/BreedingContainer.cs`: `penKey` resuelto en Start vía `PlacedFurnitureMarker.AnchorCell` ("x_y"); registro estático `byKey` + `TryGet`; stamp `HomePenKey` en ambos padres al aceptar el server; `ReclaimBreedingOccupants` filtra por `HomePenKey == penKey`; `ReclaimDirect(agent)` (expone Claim); `LaunchPoint` = Center + up*launchHeight; `ClearBreed` limpia HomePenKey. Dueño del nacimiento: suscribe `OnBreedingCompleted` y si la pareja es ocupante de ESTE corral → `MoriMochiSpawner.Instance.RegisterBirthLaunch(child, LaunchPoint)` + `ExitCourtship()` en ambos padres (vuelven a deambular; antes quedaban posados mirándose tras nacer la cría).
- `World/MoriMochiSpawner.cs`: singleton `Instance`; `breederQueue` (prioridad) drena antes que `spawnQueue`; `SpawnOne` coloca breeders directo en su corral (`TryPlaceInPen` → ReclaimDirect, fallback a cañón si rechaza); helper `Acquire` (unifica prewarm/cold). El lanzamiento del recién nacido lo decide el corral: `RegisterBirthLaunch(childId, worldPoint)` rellena `birthLaunchPoints`; en `SpawnOne` el muzzle del recién nacido = ese punto. (Ya NO suscribe `OnBreedingCompleted` ni depende de `HomePenKey` para el lanzamiento.)

**Files Created:**
- `Data/CreatureLifeStageTableSO.cs`: SerializedScriptableObject (SIN singleton — un SO nunca es singleton), dict Odin `LifeStage→umbral días`, `GetStage(ageDays)`, `Label(stage)` (español), botón Seed Defaults (0/1/3/7/20). La referencia vive en `BreedingController.lifeStageTable` (getter `LifeStageTable`); el NameTag la lee vía `BreedingController.Instance`.

**PASO MANUAL PENDIENTE (Juan, en Unity):**
- Crear el asset `CreatureLifeStageTable` (menú RunRunSimulator/Life Stage Table), pulsar "Seed Defaults", y **asignarlo en el campo `Life Stage Table` del componente BreedingController** (mismo GameObject que GameManager). Sin asignarlo, el NameTag muestra solo "{días}d" sin etiqueta de etapa.
- Verificar que los corrales (BreedingFurnitureNew / ContainerBase) entren por el sistema de muebles (tienen PlacedFurnitureMarker); si hay corrales puestos a mano en escena, el penKey caerá al `name` (warning en consola).

**Quirk técnico:** identidad estable del corral = CellKey del mueble (`"x_y"`), no el GameObject. El `PlacedFurnitureMarker` lo estampa FurnitureSpawner DESPUÉS del Instantiate, por eso el corral resuelve su key en `Start()`, no en Awake/OnEnable.

**Fixes de feedback (mismo bloque de sesión):**
- `World/MoriMochiAgent.cs`: (a) `ReactIfPlayerNear` — estando penned se restringe SOLO el acercarse al jugador (Approach/Follow se omiten dentro del corral); el resto de estados (flee/retreat, roaming, idle, needs) se mantienen. (Primero se gateó toda reacción, pero quedaban estáticos/encimados; corregido a restricción quirúrgica.) (b) `Knock` preserva `thrownTimer` si ya estaba `Thrown` → un cúmulo de criaturas golpeándose en vuelo ya no se resetea el safety timeout mutuamente (antes quedaban "por los aires" indefinidamente; ahora recuperan a más tardar en `maxThrownTime`).
- `World/NameTag.cs`: layout de corral más alto + compacto. Campos `penRaise` (def 0.6) y `penScale` (def 0.8); cachea localPosition/localScale base en Awake y los aplica en LateUpdate solo cuando `agent.IsPenned` (antes el tag de cría clipeaba el piso).

**Breed points fijos + datos de pareja persistidos (sub-feature):**
- `Data/CreatureDNA.cs`: `HomePenSlot` (int, −1 = sin asignar) = slot de cría preferido; persiste con el DNA (local+cloud) junto a `HomePenKey`+`BreedPartnerID` → el dato de pareja (qué MM↔qué MM, corral, slot) queda completo y reconstruible del registro.
- `World/BreedingContainer.cs`: reemplazado el spacing dinámico (`courtGap`/`BodyRadius`) por **slots fijos configurables**: `BreedingSlot[] breedingSlots` (cada slot = 2 anclas hijas spotA/spotB, mirándose). `FindFreeSlot()` asigna el primer slot libre al emparejar (escribe `HomePenSlot` en ambos padres). `ManageCourtship` reescrito multi-pareja: posa cada pareja breeding en su slot (hembra→spotA, macho→spotB). `ClearBreed` limpia `HomePenSlot`. Manager-awareness: `static All` (registro de corrales), `PenKey`, `ActivePairs()`.
- `World/MoriMochiAgent.cs`: quitado `BodyRadius` (sin uso con slots fijos). `EnterCourtship(pos, lookAt)` ahora recibe los Transforms del slot.
- `Systems/Breeding/BreedingController.cs`: readout Odin (BoxGroup "Corrales") `ActivePenCount` + `ActivePairsInfo` leyendo `BreedingContainer.All`/`ActivePairs()` — el manager conoce los corrales activos y sus parejas sin refs serializadas.

**PASO MANUAL PENDIENTE (Juan, Unity):** en el prefab del corral, crear los empties spotA/spotB por slot (1 par para 1 pareja, 2 pares en esquinas para 2) y asignarlos al array `Breeding Slots`. Sin slots, las parejas no se posan (merodean).

**Next Session Goal:** testear en Play. Confirmar: pose en slots fijos (sin variar al recargar), 2 parejas en 2 slots, readout de parejas en BreedingController, padres vuelven a merodear al nacer cría.

---

### Sesión 8 (histórico) — 2026-06-13
**Focus:** BreedingContainer feedback + visual cortejo. BreedingController como única fuente de servicios. Cancel async server-side. NameTag corral. Recuperación de huevos huérfanos.

**Files Touched:**
- `World/BreedingContainer.cs`: implementa `IInteractable` (tap E → hatch); restore pasivo de needs; `ForceRoll` (botón Odin) que ignora cooldown; `TryRollPair(verbose, ignoreCooldown)` ahora es `async` y reporta éxito SOLO si el server inició la incubación (no miente: si el server rechaza con `already_breeding` lo dice y no quema cooldown); `ManageCourtship` posa la pareja incubando enfrentada en slots fijos (`courtGap`); `CancelBreeding` al retirar del corral (limpia local + llama server); `ReclaimBreedingOccupants` reengancha al corral los breeders mid-incubación tras recargar escena; `PairDiagnostics` + `lastRollInfo` para debug en inspector
- `World/MoriMochiContainer.cs`: `OccupantInfo` ahora {Name, Gender, Personality} (sin dump de DNA); `Claim()` compartido (Admit + reclaim); `Release()` public virtual; warning de NavMesh si confinamiento falla
- `World/MoriMochiAgent.cs`: estado `Courting` + `EnterCourtship`/`ExitCourtship`/`IsCourting`/`IsPenned`
- `World/NameTag.cs`: layout de corral (glifo género ♂/♀ + nombre + personalidad; ❤ + countdown si incubando) vs layout libre (status/intent/petHint)
- `Systems/Breeding/BreedingController.cs`: singleton `Instance`; dueño de `AsyncBreedingService` + `BreedingAffinityTableSO`; wrappers `GetAffinity`/`StartBreedingAsync`/`HatchAsync`/`CancelBreedingAsync`/`CancelAllBreedingAsync`; botón "Cancel All Eggs" en BoxGroup Breed Timer
- `Systems/Breeding/AsyncBreedingService.cs`: `CancelBreedingAsync(mother,father)` (endpoint cancel-breeding) y `CancelAllBreedingAsync()` (endpoint cancel-all-breeding, vacía la cola entera + `ClearAllLocalBreeding`)

**Cloud Code (nuevos, desplegar):**
- `CloudCode/cancel-breeding.js`: borra un huevo puntual (motherId+fatherId)
- `CloudCode/cancel-all-breeding.js`: vacía la key `breeding_eggs_<playerId>` entera (recupera huérfanos que el cliente no rastrea)

**Quirk técnico resuelto:** el bug de "¡Emparejados! pero no pasa nada" era desync con el server — un cancel cuyo endpoint no estaba desplegado dejaba el huevo vivo en el server; al reusar esos IDs, `start-breeding` devolvía `already_breeding` y el `BusyState` local nunca se seteaba. Fix doble: (1) `TryRollPair` async ahora verifica `BusyState` real antes de cantar éxito; (2) botón Cancel All Eggs + endpoint para limpiar huérfanos.

**Next Session Goal (diseño + testing):**
- **Corral de origen**: las parejas deben recargar en el MISMO corral donde breedearon. Crear data que empareje `furnitureId` ↔ MoriMonchis (hoy `ReclaimBreedingOccupants` reengancha a cualquier corral disponible, no al de origen).
- **Bug en testing (no reproducido)**: error "unknown" raro, posiblemente al pulsar "Cancel All Eggs" repetidas veces (sospecha: `async void` sin guard re-entrante → llamadas solapadas a CallEndpointAsync). Intentar reproducir; si confirma, agregar guard (estilo `isHatching`).
- Verificar visualmente la pose de cortejo y el reclaim tras recarga de escena.
