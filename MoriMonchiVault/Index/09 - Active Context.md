---
tags: [index, core]
---

# 09 - Active Context

**Session:** 2026-07-14/15 (Session 47 — **TEST VISUAL S46 APROBADO + ESCUDO POR RONDA + BARRA MINIMAL + COREOGRAFÍA DE PASIVAS + FRASES SELF + FIX PARTÍCULAS + RAGDOLL DEL MODELO NUEVO — ✅ CERRADA: todo verificado en editor por MCP (0 errores), sim verificado por log sintético + Determinism OK, ragdoll probado en Play con impulso**)
**Focus:** Juan corrió el hard reset y aprobó el modelo energía→marcas de S46 ("por fin se entiende el panel superior... lo hemos logrado, está visualmente aceptable esta etapa de simulación"). La sesión encadenó varias rondas de feedback:

1. **Barra de orden**: columna de marcas 4→2 slots de altura fija (`CombatVisualizerPanel.uss`, min-height 76→40px) — en la práctica nunca hay más de 2 marcas por canal.
2. **ESCUDO POR RONDA (cambio de sim, decisión de diseño)**: el escudo deja de ser HP temporal persistente — se acumula solo dentro de la ronda y **se disipa al cierre de cada ronda** (`CombatService.ExpireShields`, orden fijo A→B, log `[escudo] X pierde su escudo (-N)`, sin consumo de rng). Rompe paridad de gameplay a propósito. Verificado por log sintético (10 expiraciones) + Determinism OK 2×.
3. **Barra world-space MINIMAL (decisión de Juan: "solo barra de vida")**: `MoriMonchiCombatVisualizerUITK` reescrito — quedan solo HP (track+fill+hp-value con "+N") y el escudo como **trocito azul montado dentro de la propia barra** (a continuación del fill, left=hpPct, width=min(shieldPct, 1-hpPct)); nombre/ATK/VEL del UXML ocultos por display:none. **Marcos**: dorado = turno activo, rojo = objetivo del ataque (rojo gana). API nueva: `Bind()` (sin args), `SetHp`, `SetShield`, `SetActiveTurn(bool)`, `SetTargeted(bool)`. ELIMINADOS: `SetStatus`/`SetElementState`/`FlashReaction`/`Bind(6 args)`, struct `ElementChipData` (CombatVisualEvents), `PushStatusAll`/`PushElements` del service, param `elements` de `CombatVisualUnits.Spawn`.
4. **COREOGRAFÍA DE PASIVAS (pedido de Juan: "que sea visual")**: orden visual del turno = declaro intención (marco rojo al objetivo desde `TurnStart`) → **viajo físicamente al aliado de mi pasiva** (mismo lunge, `lungeFraction`) + globo con su nombre → vuelvo → viajo al enemigo con **"¡TOMA, {0}!"** (campo `attackLine` nuevo) + golpe → vuelvo → afinidad. Mecanismo: campo **`PassivePhase`** en `CombatProcEvent` (aditivo, patrón `BeforeStrike`) estampado por `CombatResolver`; `TakeTurn` envuelve `ApplyPassives`+`HealAfterStrike` con el flag (afinidad y lifesteal fuera). `ForwardRoutine` agrupa los procs de pasiva consecutivos por target y coreografía el viaje; pasiva sobre SÍ MISMO = sin viaje + **frase self** (`protectorSelfLine` "¡Me escudaré!" · `empaticoSelfLine` "¡Qué alivio!" · `agresivoSelfLine` "¡Me toca a mí!" — la del Agresivo exige `PassivePhase` para no dispararse con la marca propia por afinidad). `protectorLine` → "¡Protégete, {0}!" (actualizado en escena). Verificado sintético: 81 procs etiquetados en 39 turnos, 62 con viaje.
5. **Fix partículas del FeelDirector**: los 18 `MMF_ParticlesInstantiation` estaban en PositionMode **WorldPosition** (Vector3 fijo → todo nacía en 0,0,0); pasados a **Script** (consume la posición de `PlayFeedbacks(Vector3)`). + 3 **mutes por sección** (`muteSoporte`/`muteMarcas`/`muteEstados`, gates independientes por rama). `hideForDebug` → false en prefab (barra) y escena (globos) — era el motivo de "no veo las barras nuevas".
6. **RAGDOLL DEL MODELO NUEVO (arranque de la siguiente etapa, escena `MorimonchiNewModel`)**: modelo arácnido de primitivas de Juan (Head/Torso/LowerBody/ArmR/ArmL/4 Legs). Configurados los `ConfigurableJoint` estilo Gang Beasts: **Torso = raíz** (rb sin joint); Head/Arms/LowerBody→Torso, 4 patas→LowerBody; lineal Locked (no estira), angular Limited (±20-35° según parte), **Slerp drive con resorte** (500-1500, damper 40) que mantiene la pose, anclas clampeadas hacia el cuerpo conectado, proyección Position+Rotation (0.1/30°), masas 3/2.5/1/0.5/0.4, solverIterations 12, maxDepenetrationVelocity 2. **PROBADO EN PLAY**: impulso (3,4,2) m/s al torso → vuela ~2m, aterriza, y todas las distancias al torso quedan idénticas a las iniciales (mantiene la forma) — 0 NaN, 0 errores. Juan feliz → siguiente: más articulaciones en piernas/brazos.

> ### 📋 Notas S47
> 1. **La coreografía de pasivas y el escudo nuevo solo se ven en peleas NUEVAS**: `PassivePhase` y los valores de escudo viven en el record al momento de simular. Records pre-S47 replayean sin fase de viaje y con escudo persistente viejo (los globos salen igual, post-golpe). NO hace falta hard reset — solo simular pelea nueva.
> 2. **Quirk Empático**: su cura es % del daño del golpe y ahora se muestra ANTES del ataque → el número de cura aparece antes del golpe que lo genera. Si molesta en pantalla, invertir solo para ese rol (golpe → cura) manteniendo el viaje.
> 3. **Pasiva sobre sí mismo = quieto por diseño** (Protector/Agresivo pueden auto-elegirse): frase self, sin viaje. En la corrida sintética ~25% de las pasivas fueron self.
> 4. **Play mode por MCP sin foco del editor NO avanza la física** (Run In Background off): fix runtime `Application.runInBackground = true` vía execute_code antes de medir.
> 5. Los feedbacks los llenó Juan (`Hovl Studio`); quedan tweakeables el `offset` del director y las 4+3 frases del service en el inspector.
> 6. Log de escudo (`[escudo] ... pierde su escudo`) va solo al log de texto del sim — el visualizer no lo narra (el trocito azul desaparece por resync de snapshot al turno siguiente). Si Juan quiere narrarlo, haría falta un proc/evento propio.

**Files Touched (.cs — input ScriptNodes):**
- `Data/Combat/CombatProcEvent.cs` (MODIFICADO): + campo `PassivePhase` (aditivo, records viejos → false).
- `Systems/Combat/CombatResolver.cs` (MODIFICADO): + flag `PassivePhase` estampado en `Record`/`RecordElement` (patrón `BeforeStrike`).
- `Systems/Combat/CombatService.cs` (MODIFICADO): `ExpireShields` al cierre de cada ronda (escudo dura 1 ronda); envoltura `r.PassivePhase` alrededor de las pasivas de rol en `TakeTurn`; header actualizado.
- `Systems/CombatVisualizer/CombatVisualizerService.cs` (MODIFICADO): coreografía de pasivas en `ForwardRoutine` (viaje al aliado, grupos por target); marco rojo desde TurnStart; `attackLine` + 3 frases self + formateo con nombre; `SetActiveFrames`/`ClearTargetedFrames`; fuera `PushStatusAll`/`PushElements`/`FlashReaction`/`SetStatus`.
- `Systems/CombatVisualizer/CombatVisualUnits.cs` (MODIFICADO): `Bind()` sin args; `Spawn` sin param `elements`.
- `Systems/CombatVisualizer/CombatVisualEvents.cs` (MODIFICADO): struct `ElementChipData` eliminado.
- `Systems/CombatVisualizer/CombatFeelDirector.cs` (MODIFICADO): + `muteSoporte`/`muteMarcas`/`muteEstados` (gates por sección).
- `UI/MoriMonchiCombatVisualizerUITK.cs` (MODIFICADO): rework total a barra minimal — solo HP + escudo embebido + marcos dorado/rojo; API `Bind()`/`SetActiveTurn`/`SetTargeted`.

**Files Touched (no-ScriptNode):** `UI Toolkit/CombatVisualizerPanel.uss` (marcas 2 slots), `Resources/Prefabs/MoriMonchiVisualizer.prefab` (hideForDebug=false), `Resources/Scenes/CombatVisualizerMM.unity` (particles→Script, mutes, hideForDebug globos=false, protectorLine nueva), `Resources/Scenes/MorimonchiNewModel.unity` (ragdoll completo: joints/rbs configurados), registry asset (guardado por SaveSystem en Play, borrable), `Resources/Particles/EffectCircle_Purple.prefab` (de Juan, sin commitear).

**Next session (S48 — RAGDOLL/MODELO NUEVO, palabra de Juan):**
1. Iterar el ragdoll del arácnido: más articulaciones en piernas y brazos (multi-segmento), tuning de springs para el wobble Gang Beasts (hoy mantiene la forma casi rígida — bajar springs si se quiere más flopa), y control/locomoción.
2. Arrastran: volcar el MODELO NUEVO de S46 a `Index/13` (§3 sigue describiendo la energía que ya no existe), quirk Empático (nota 2) si molesta, F5 async 3v3, economía F7, ítems con estados, tuning de knobs elementales.

**ScriptNodes (cierre S47):** ACTUALIZAR `CombatProcEvent.md`, `CombatResolver.md`, `CombatService.md`, `CombatVisualizerService.md`, `CombatVisualUnits.md`, `CombatVisualEvents.md`, `CombatFeelDirector.md`, `MoriMonchiCombatVisualizerUITK.md`.

---

**Session:** 2026-07-14 (Session 46 — **REDISEÑO DEL MODELO DE ENERGÍA→MARCAS + ORDEN DEL TURNO + DIRECTOR DE FEEDBACKS (Feel) — 🟡 CERRADA SIN TEST VISUAL: sim verificado por log + Determinism OK + 0 errores de compilación y de Play, pero el REPLAY NO se testeó (Juan tuvo que irse; requiere su hard reset)**)
**Focus:** la sesión arrancó apuntando al "feel de movimiento" (plan S45) y Juan la **redirigió en el primer mensaje**: el lunge le gusta y queda como está. El problema real era que **el ciclo de energía no se entendía** ("veo llenarse los 2 circulitos, se vacían, y no aparece ninguna marca de su elemento"). Diagnóstico: NO era un contador roto (su hipótesis de "¿contamos 3 acciones?"), eran 3 causas sumadas — (a) `GainAffinity` era el paso 16 (último) del turno y los traits que gastaban energía corrían ANTES (pasos 2/7/14), así que la energía generada al final de la acción 2 recién se gastaba en la acción 3; (b) la marca aliada nunca iba al actor sino al objetivo del trait; (c) el Agresivo solo proqueaba si acertaba su roll del 50%, y como el ⚡ se había eliminado en S44, la energía acumulada era INVISIBLE.

> ### 🔑 MODELO NUEVO (decisión de Juan, cerrada en esta sesión — reemplaza la §3 "Afinidad → energía → proc" de Index/13)
> **La energía como recurso contable DEJA DE EXISTIR.** Quedan solo la **afinidad** (los 2 circulitos) y las **marcas**. Hay exactamente **dos vías** de generar marcas aliadas:
> 1. **Afinidad → marca PROPIA**: cada 2 acciones el MM se aplica la marca de su propio elemento **sobre sí mismo**, **en ese mismo turno**. Es su única función; no alimenta nada más. Ningún rol se salva ni se beneficia.
> 2. **Pasiva de rol → marca a OTRO**: la pasiva aplica el elemento del actor al objetivo sobre el que actúa, **sin gate de recurso**, todos los turnos. Protector → el aliado escudado · Empático → el aliado curado · **Agresivo → un aliado al azar, acierte o no la backline** (su roll del 50% queda como PURO TARGETING).
>
> **Orden del turno (pedido explícito de Juan, opción "todas las pasivas después del daño")**: los 3 roles leen igual → **intención de golpe → daño (+marca enemiga) → AFINIDAD (+marca propia) → pasiva de rol (+marca al aliado)**. El escudo del Protector pasa a aplicarse DESPUÉS de su golpe (antes era pre-golpe).
>
> **Decisiones auxiliares de Juan**: marca duplicada = se sobreescribe (no-op, ya era el comportamiento); el volumen alto de marcas es **intencional en esta etapa** ("mientras más mejor" — lo que importa es que todo se vea y se entienda, el balance de cuántas acciones pide cada pasiva es problema futuro).

**Cambios:**
1. **Sim** (`Combatant`/`CombatService`/`RolePassiveBase`/`RoleActiveBase`/`CombatRoleHooks`/`CombatRecord`/`ReactionEffectBase`): campo `Energy` ELIMINADO del combatiente y del snapshot del record; `GainAffinity(actor, config, result, r, rng)` (firma nueva) al llegar a 2 resetea y llama `CombatElements.AddMark(actor, actor.Element, ally, actor, ...)` + re-emite `AffinityGained(0)` (beat: 2 llenos → marca propia → se vacían); gates `if (actor.Energy > 0)` fuera de las 3 pasivas; leaf NUEVO `MarkRandomAllyPassive` (Agresivo); `BacklineHunterActive` reducido a targeting (perdió las 2 ramas de energía, incluido el fallback "comparte energía"); `GrantEnergyEffect` BORRADO (verificado antes: no estaba en ningún asset → no rompe deserialización Odin); hook `OnTurnStart`→**`OnAfterStrike`** y `GrantShield`→**`ApplyPassives`** (renombrados porque ya no corren en turn start); `ElementEventKind.EnergyGained/EnergySpent` quedan **inertes** en el enum (append-only, precedente Synergy S39).
2. **Visualizer**: `OnUnitAffinity` perdió el param `energy` (era **parámetro muerto** — la barra no lo leía desde que se quitó el ⚡ en S44 → **cero cambio visual**); el globo del Agresivo colgaba de `EnergyGained` (que ya no se emite) → re-enganchado a su `MarkApplied` ally-sourced vía helper nuevo `SnapRole(side,index)`; `PosOf(side,index)` **público** nuevo (para el feel director); `CombatElementEventData` + campo `ReactionName`.
3. **Estados instantáneos con carta** (pedido de Juan): PisoTierra/OverGrow/Cleanse/Leech no quedan armados → nunca se dibujaban. `CombatOrderBarUITK` ahora parsea `ReactionName`→`ElementalState` en el caso `Reaction` y lo agrega a `States`; el resync de `ApplyState` a fin de turno los borra solo. **Quirk**: si Juan renombra el `Name` de una reacción en el ElementTable, la carta (y su partícula) dejan de salir en silencio — degrada suave, no rompe.
4. **Marcas sin deformar la carta** (pedido de Juan): como hay **4 elementos**, una columna nunca supera 4 marcas → `.cv-ob-marks-col` pasó de row+wrap a **column con 4 slots de altura fija** (chip `height:16px`, split `min-height:76px`, ellipsis). Recuadro dorado del turno activo 2px→**4px**.
5. **`CombatFeelDirector` NUEVO** (`SerializedMonoBehaviour`, en la escena, wireado al GO CombatVisualizer): dueño único de los MMFeedbacks del replay. **Juan rechazó los hooks por prefab** ("no sé qué tan correcto sea repetir los mismos 12 sistemas de partículas en el prefab") → se REVIRTIÓ el intento inicial de agregar UnityEvents a `MoriMonchiCombatVisualizer` (quedó idéntico al original, no figura en git status). Suscribe `OnPopup` (Shield/Heal, que ya trae `Position`) + `OnUnitElement` (MarkApplied/Reaction) y reproduce con `MMFeedbacks.PlayFeedbacks(Vector3)` en la posición del MM (vía `PosOf`, mismo patrón que `CombatCameraDirector`→`VCamOf`). Granularidad final pedida por Juan: **1 feedback por cada uno de los 12 estados** (`Dictionary<ElementalState, MMFeedbacks>` Odin) + **1 por cada uno de los 4 elementos** para marcas + escudo + cura = **18**. Botón `[Button]` "Crear objetos de feedback y wirear" → crea los 18 GameObjects hijos con `MMF_Player` y los auto-wirea (idempotente: re-ejecutar completa lo que falte, ej. si se agrega un estado al enum). **Ejecutado y verificado**: las 18 referencias apuntan a su hijo, ninguna null.

> ### 📋 Notas S46
> 1. **`MissingReferenceException: UIDocument` — CERRADO, NO ES NUESTRO (probado, no inferido)**. S45 lo dio por bug del Editor por el stack; Juan lo reportó de nuevo ("no hice nada, solo Play y volví") así que se hizo **experimento controlado**: sin nada seleccionado → Play/Stop → **0 errores**; con el GO del UIDocument seleccionado → **el error aparece (x2)**, stack 100% dentro de `UIDocumentInspector.UpdateValues`←`InspectorWindow.RedrawFromNative`, ni una línea nuestra. Causa de que Juan lo viera "sin hacer nada": **la selección del Inspector persiste entre sesiones de Unity** (el CombatVisualizer quedó seleccionado desde S45). Se dejó la selección limpia. No hay nada que arreglar.
> 2. **`MMF_Player` SÍ existe** en el proyecto (`Feel/MMFeedbacks/MMFeedbacks/Core/MMF_Player/MMF_Player.cs`) — en un chequeo previo se dijo que no y era falso (glob en la ruta equivocada). Clave: **`MMF_Player : MMFeedbacks`**, por eso los campos tipados `MMFeedbacks` aceptan MMF_Player.
> 3. **Por qué NO se usó UnityEvent para los feedbacks** (Juan lo propuso y preguntó): `PlayFeedbacks()` sin args reproduce en `this.transform.position` (posición FIJA del objeto del feedback, no la del MM), y `UnityEvent<Vector3>` tampoco sirve porque la firma real es `PlayFeedbacks(Vector3, float=1f, bool=false)` — **3 params** y Unity solo bindea dinámicamente métodos de exactamente 1 param del tipo (los defaults no cuentan). Se necesita la referencia sí o sí para posicionar.
> 4. **ROMPE PARIDAD A PROPÓSITO**: el consumo de rng cambió (gates fuera + orden nuevo). Los records viejos **siguen siendo replayables** (el visualizer los lee, no los re-simula) pero NO muestran el comportamiento nuevo. **Verify Determinism corrido 2 veces (mismo seed → log idéntico): OK.**
> 5. Verificación del sim por log (`SimulateCore` directo con 6 DNAs sintéticos de rol/elemento controlado — **no se tocó el registry ni se consumieron peleas**): en 50 turnos → **16 auto-marcas por afinidad, todas en el mismo turno que llenó los circulitos**, 43 procs de rol, 33 reacciones. Orden confirmado en el log: `dmg → [marca enemiga] → [afinidad] (2/2) → [marca propia] → [Empático] cura → [marca al aliado]`.
> 6. Clonar `CreatureDNA` por Newtonsoft **NO funciona**: `UnityEngine.Color` tiene auto-referencia (`.linear.linear...`) → StackOverflow incluso con `ReferenceLoopHandling.Ignore`. Para tests de sim, construir DNAs a mano copiando campos escalares de un proto del registry.
> 7. `hideForDebug` **sigue en `true`** (escena + prefab, heredado de S45): globos de habla y cuadros world-space OCULTOS. Decidir si se reactivan antes del test visual.
> 8. Juan ya trajo assets de partículas al proyecto (`Assets/Hovl Studio/`, `Assets/RunRunSimulator/Resources/Particles/`) — sin commitear, son suyos.

**Files Created (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatFeelDirector.cs` (NUEVO): dueño único de los MMFeedbacks del replay; 12 estados + 4 elementos + escudo + cura; reproduce en la posición del MM vía `PosOf`; botón que crea y auto-wirea los 18 hijos con `MMF_Player`.

**Files Touched (.cs — input ScriptNodes):**
- `Systems/Combat/Combatant.cs` (MODIFICADO): campo `Energy` eliminado.
- `Systems/Combat/CombatService.cs` (MODIFICADO): `GainAffinity` firma nueva + auto-marca al llegar a 2; orden del turno (afinidad y pasivas después del daño); `ApplyPassives`; snapshot sin `Energy`; header reescrito.
- `Systems/Combat/CombatRoleHooks.cs` (MODIFICADO): `GrantShield`→`ApplyPassives`, llama `OnAfterStrike`.
- `Data/Combat/RolePassiveBase.cs` (MODIFICADO): gates de energía fuera; `OnTurnStart`→`OnAfterStrike`; leaf NUEVO `MarkRandomAllyPassive` (Agresivo).
- `Data/Combat/RoleActiveBase.cs` (MODIFICADO): `BacklineHunterActive` reducido a targeting puro.
- `Data/Combat/ReactionEffectBase.cs` (MODIFICADO): `GrantEnergyEffect` borrado.
- `Data/Combat/CombatRecord.cs` (MODIFICADO): `CombatUnitState.Energy` fuera.
- `Data/Combat/RoleTableSO.cs` (MODIFICADO): "Poblar v2" da `MarkRandomAllyPassive` al Agresivo.
- `Systems/CombatVisualizer/CombatVisualEvents.cs` (MODIFICADO): `OnUnitAffinity` sin param `energy`; `CombatElementEventData` + `ReactionName`.
- `Systems/CombatVisualizer/CombatVisualizerService.cs` (MODIFICADO): `PosOf` público; `SnapRole`; globo del Agresivo re-enganchado a MarkApplied; emisiones de energía fuera; `ReactionName` en el evento.
- `UI/CombatOrderBarUITK.cs` (MODIFICADO): `HandleAffinity` sin `energy`; cartas de estados instantáneos por parseo de `ReactionName`.

**Files Touched (no-ScriptNode):** `ScriptableObjects/Abilitys/RoleTable.asset` (pasiva del Agresivo, agregada quirúrgicamente sin re-poblar para no pisar tuneos), `Resources/Scenes/CombatVisualizerMM.unity` (`CombatFeelDirector` + 18 hijos con `MMF_Player`), `UI Toolkit/CombatVisualizerPanel.uss` (marcas en columna con 4 slots fijos, borde activo 4px), assets de registry/inventory/furniture (guardados por SaveSystem en Play, borrables).

**Next session (S47 — TEST VISUAL DEL MODELO NUEVO, pendiente de S46):**
1. **Juan hace el hard reset de MMs y corre peleas nuevas** (los records viejos no tienen la data nueva). Después verificar en Play por screenshot: (a) la marca propia apareciendo en la carta **en el mismo turno** que se llenan los circulitos y **antes** que la de la pasiva; (b) cartas de PisoTierra/OverGrow dibujadas hasta el cierre del turno; (c) cartas que ya no cambian de alto; (d) recuadro dorado 4px.
2. **Decidir `hideForDebug`**: reactivar globos y cuadros world-space (nota 7) antes o después del test.
3. Juan llena los 18 `MMF_Player` con sus partículas (`Hovl Studio`) y se verifica que cada una salga en la posición del MM correcto.
4. Arrastran: escudo-1-ronda (cambio de sim), F5 async 3v3, economía F7, ítems con estados, tuning de knobs elementales. **Nuevo pendiente**: volcar el MODELO NUEVO (recuadro de arriba) a `Index/13 - Combat Design Direction`, cuya §3 quedó DESACTUALIZADA (describe el modelo de energía que ya no existe).

**ScriptNodes (cierre S46):** CREAR `CombatFeelDirector.md`; ACTUALIZAR `CombatService.md`, `Combatant.md`, `CombatRoleHooks.md`, `RolePassiveBase.md`, `RoleActiveBase.md`, `ReactionEffectBase.md`, `CombatRecord.md`, `RoleTableSO.md`, `CombatVisualEvents.md`, `CombatVisualizerService.md`, `CombatOrderBarUITK.md`.

---

**Session:** 2026-07-14 (Session 45 — **ITERACIÓN PROFUNDA DE LA BARRA DE ORDEN DEL REPLAY (varias rondas de feedback) + LUNGE DEL ATACANTE + TOGGLES DE DEBUG — ✅ CERRADA, verificada en Play (0 errores de nuestro código, screenshot por cada cambio)**)
**Focus:** rondas de feedback de Juan sobre la legibilidad del sistema elemental en la barra superior + arranque del "feel" de movimiento. Cambios:
1. **Orden por ronda** (`CombatVisualizerService.PublishOrder` + `CombatOrderBarUITK`): la barra dejó de ser equipoA|gap|equipoB estática; ahora es UN contenedor y las cartas se ordenan por la secuencia de acción de la ronda (mapa `roundOrders` precomputado en BuildStates, key = TurnNumber), ESTABLE dentro de la ronda (se reordena solo al cambiar de ronda), recuadro dorado móvil en el activo. Ordinal "1ero/2do" ELIMINADO.
2. **Diferenciación de equipo**: primero fondo pastel azul/rojo, luego (feedback) revertido a **fondo gris + banner delgado de 5px arriba** (azul aliado / rojo enemigo, `cv-ob-team-banner`).
3. **Marcas elementales en TIEMPO REAL** (lo central; el ⚡ de energía fue descartado por Juan — "nadie entiende un rayo"): nuevo evento `CombatVisualEvents.OnUnitElement` (struct `CombatElementEventData`) que el service emite por-proc en `PlayProc` (MarkApplied/MarkRemoved/Reaction/StateArmed/StateConsumed/StateRemoved); la barra mantiene listas runtime `Marks`/`States` por carta y las reconstruye incrementalmente, con resync al snapshot a fin de turno. Con los beats (MarkApplied/EnergyGained/Afinidad-a-2 sumados a Reaction/State) se LEE la combinación: aparece un elemento → aparece el otro → beat → aparece el estado. Verificado: Planta+Fuego juntas en la carta antes de reaccionar → Charcoal.
4. **Chips de estado más grandes** (fs 10→15, azul positivo / rojo negativo).
5. **Lunge del atacante** (`ForwardRoutine` + helper `MoveOverTime` + campo `lungeFraction`=0.6): el MM se desplaza hacia la posición del objetivo durante el windup y vuelve tras el impacto. Verificado (desplazamiento ~3 al pico). **El "feel" NO está conseguido — Juan quiere empezar frescos la próxima sesión.**
6. **Toggles de debug**: `hideForDebug` en `CombatSpeechBubbles` (globos) y `MoriMonchiCombatVisualizerUITK` (cuadros world-space), ACTIVADOS (true) en la escena y el prefab para ver el movimiento sin oclusión.

> ### 📋 Notas S45
> 1. **Crash `MissingReferenceException: UIDocument destroyed`**: confirmado 3 veces que es un **bug del Editor de Unity** (`UIDocumentInspector.UpdateValues` ← `InspectorWindow.RedrawFromNative`), solo al SALIR de Play y solo si hay un GO con UIDocument SELECCIONADO en el Inspector. 0 errores durante el replay. NO es nuestro código. Workaround: deseleccionar antes de parar.
> 2. `hideForDebug=true` quedó en escena (CombatSpeechBubbles del GO CombatVisualizer) y prefab (MoriMonchiVisualizer → MoriMonchiCombatVisualizerUITK). Para reactivar globos/cuadros, destildar.
> 3. `lungeFraction` (0.6) tuneable — 0.6 del gap de ~5u = ~3u (cruza medio tablero); bajar si se siente exagerado.
> 4. Record de test vigente: primer MM con pelea del registry (6 records espejo, 31 turnos). AI del visualizer = turno_index+1 (head=0).
> 5. Mecanismo elemental confirmado por el stream de eventos: 2 acciones → energía → el trait aplica el elemento del MM a un aliado AL AZAR (a veces él mismo); la reacción ocurre en el que RECIBE 2 marcas de distinto elemento (mismo canal). La energía ya NO se muestra como icono; se ve como el elemento apareciendo en la sección aliada.

**Files Touched (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatVisualEvents.cs` (MODIFICADO): + struct `CombatElementEventData`; + evento/helper `OnUnitElement`/`UnitElement`.
- `Systems/CombatVisualizer/CombatVisualizerService.cs` (MODIFICADO): `roundOrders` + `PublishOrder` por ronda; emit `OnUnitElement` por-proc en `PlayProc`; beats extra (MarkApplied/EnergyGained/Afinidad-2); lunge en `ForwardRoutine` + helper `MoveOverTime` + campo serializado `lungeFraction`.
- `UI/CombatOrderBarUITK.cs` (MODIFICADO): contenedor único + reorden por ronda; sin ordinal; banner por equipo; display incremental de marcas/estados (listas runtime `Marks`/`States`, `HandleUnitElement`/`RemoveMark`/`RebuildElements`), resync en `ApplyState`; ⚡/`EnergyLabel`/`SetEnergy` ELIMINADOS.
- `Systems/CombatVisualizer/CombatSpeechBubbles.cs` (MODIFICADO): + `hideForDebug`.
- `UI/MoriMonchiCombatVisualizerUITK.cs` (MODIFICADO): + `hideForDebug` (oculta el root del cuadro world-space).

**Files Touched (no-ScriptNode):** `UI Toolkit/CombatVisualizerPanel.uss` (banner, fondo gris, chips de estado grandes, sin `.cv-ob-energy`/`.cv-ob-order-num`/`.cv-ob-team*`), `Resources/Scenes/CombatVisualizerMM.unity` (CombatSpeechBubbles.hideForDebug=true), `Resources/Prefabs/MoriMonchiVisualizer.prefab` (card.hideForDebug=true), assets de registry/inventory (guardados por SaveSystem en Play, borrables).

**Next session (S46 — FEEL DE MOVIMIENTO/COMBATE desde cero, palabra de Juan):** el lunge existe pero el feel no está logrado — rediseñar movimiento/timing del ataque (anticipación, impacto, retorno; lungeFraction; animación de golpe; re-encuadre de cámara). Reactivar globos/cuadros (`hideForDebug=false`) cuando se retome. Arrastran: escudo-1-ronda (cambio de sim), F5 async 3v3, economía F7, ítems con estados, tuning de knobs elementales.

**ScriptNodes (cierre S45):** ACTUALIZAR `CombatVisualEvents.md`, `CombatVisualizerService.md`, `CombatOrderBarUITK.md`, `CombatSpeechBubbles.md`, `MoriMonchiCombatVisualizerUITK.md`.

---

**Session:** 2026-07-13 (Session 44 — **ITERACIÓN BARRA DE ORDEN v2/v3 + CÁMARA FIJA — ✅ CERRADA, verificada en Play (replay completo, 0 errores)**)
**Focus:** dos rondas de feedback de Juan sobre la barra superior del replay, mismo día que el cierre de S43. **Ronda 1**: carta 116px cuadrada, marcas divididas en columnas IZQ=aliadas/DER=enemigas, ⚡energía ELIMINADA de la UI (decisión de diseño: visualmente el ciclo es acción→circulito→2 llenos→aparece la marca aliada; la energía queda solo en el sim), log de marca con canal ("recibe marca Fuego **enemiga/aliada**" — antes el caso enemigo no decía nada), cámara FIJA (`CombatCameraDirector.enabled=false` en escena — componente queda para el futuro; con VCam_Scene priority 10 y las vcams de unidad a 0, la general manda siempre). **Ronda 2 (v3, lección del recetario UITK S38 aplicada: espacios flexibles, NO px fijos)**: `order-bar` ahora es teamA(flex-grow)+gap+teamB con **slots** por unidad (`.cv-ob-slot` columna flex-grow, max 240px) que usan TODO el ancho; carta = número de orden de acción **"1ero/2do/3ero/4to…"** en dorado arriba (espacio SIEMPRE presente → la carta no se deforma; helper `Ordinal(int)`, posición en la lista de `OnActionOrder` contando solo vivas, muertas sin número), nombre completo a **2 líneas** (white-space normal) **coloreado por su elemento** (swatch, mini-chip de elemento, borde izquierdo de equipo y flecha ▼ ELIMINADOS; turno activo = solo recuadro dorado sin cambio de grosor ni scale), marcas como **cuadritos con el NOMBRE del elemento en blanco** sobre fondo del color, cartitas de estado **FUERA de la carta** (debajo, dentro del slot). `SnapshotColor` borrado (dead code tras quitar el swatch). Implementado por 3 sub-agentes `morimonchi-coder`; review de orquestador sin hallazgos.

> ### 📋 Notas S44
> 1. La escena quedó con `CombatCameraDirector` DESACTIVADO (no borrado): para retomar cortes de cámara, re-activarlo y resolver antes el hallazgo del globo fuera de pantalla (S43 quirk 2).
> 2. Las clases `.cv-order-card--self/--opp` quedaron vacías en el USS (el lado se lee por posición); `.cv-ob-energy`, `.cv-ob-turn-marker`, `.cv-ob-swatch`, `.cv-ob-mini-chip`, `.cv-ob-mark-row` borradas.
> 3. Detalle visto en verificación (cosmético, autocorrige): la carta de una unidad que muere ese mismo turno puede mostrar su ordinal un beat hasta el siguiente `PublishOrder`.
> 4. Limpieza de vault: el vault-documenter del cierre S43 había creado por error `ScriptNodes/CombatCameraDirector.cs` (nodo .md con extensión equivocada, duplicado del .md correcto) — borrado en S44.
> 5. **SIGUIENTE PASO dicho por Juan**: "una vez arreglemos la barra pasaremos a las interacciones ingame".

**Files Touched (.cs — input ScriptNodes):**
- `UI/CombatOrderBarUITK.cs` (MODIFICADO): rework completo de la carta (v2: split marcas aliadas/enemigas + sin energía; v3: slots flexibles, ordinales de acción, nombre 2 líneas color elemento, marcas con nombre, estados fuera de la carta, sin marker ▼/swatch/mini-chip).
- `Systems/CombatVisualizer/CombatVisualizerService.cs` (MODIFICADO — cosmético, sin cambio de contrato): una línea del log (`ElementText`/MarkApplied ahora dice "aliada"/"enemiga" siempre).

**Files Touched (no-ScriptNode):** `UI Toolkit/CombatVisualizerPanel.uss` (layout v3 de la barra), `Resources/Scenes/CombatVisualizerMM.unity` (director desactivado), `Assets/Screenshots/s44_*.png` (verificación, borrables).

**Next session (S45 — INTERACCIONES INGAME del replay, palabra de Juan):** diseño a definir con Juan al abrir. Arrastran: hallazgo globo fuera de pantalla si se reactivan los cortes de cámara, escudo-1-ronda (cambio de sim, rompe paridad, sesión aparte), F5 async 3v3, economía F7, ítems con estados, tuning de knobs.

**ScriptNodes (cierre S44):** ACTUALIZAR `CombatOrderBarUITK.md` (el service fue cambio cosmético — no requiere nodo).

---

**Session:** 2026-07-13 (Session 43 — **TWEAKS VISUALES DEL REPLAY — ✅ CERRADA: código + wiring de escena + verificación end-to-end en Play vía MCP**) — **🟢 compilación 0 errores · 🟢 wiring completo (CombatSpeechBubbles + CombatCameraDirector en el GO CombatVisualizer, VCam_Scene nueva priority 10 wireada a `sceneCamera`, CinemachineBrain en Main Camera, escena guardada) · 🟢 replay completo 27 turnos en Play sin un solo error de consola · 🟢 verificado por screenshot: globo "¡Toma, protégete!" + narrador "¡Quedé Debilidad!", escudo "+N" en hp-value + tira azul renderizando (track Flex, fill 4-6px), estados con nombre en cuadros rojos en barras y cartas, popup de reacción con nombre+color, indicador de turno moviéndose, cortes de vcam por unidad activa y retorno a VCam_Scene al final ("Ganador: tu equipo")**
**Focus:** los 7 tweaks de la lista S43 implementados por 6 sub-agentes `morimonchi-coder` + review de orquestador sin hallazgos: (1) **Cartas comprimidas** (`CombatOrderBarUITK` + `CombatVisualizerPanel.uss`): carta 118→86px, fila horizontal swatch(12px)+nombre(10px)+mini-chip de elemento (color, tooltip)+chip inicial de rol (P/A/E, tooltip); clases viejas `cv-ob-body/role/elem` borradas, nuevas `cv-ob-body-row/mini-chip/role-chip`. (2) **Escudo azul** (`MoriMonchiCombatVisualizerUITK.SetShield` + service): tira azul (4px) sobre el hp-track (escala escudo/MaxHp, oculta si <0.5) + sufijo "+N" en hp-value; service la empuja en Restore/ForwardRoutine (`CombatUnitState.Shield`) y mid-golpe (`DefenderShieldAfter`); proc `ModifierEffectKind.Shield` ganó log "recibe escudo (+N)" + popup "Escudo" (label nuevo en `CombatDamageNumbers`). (3) **Estados legibles** (barra world-space): estados armados con NOMBRE completo dentro de cuadro ROJO (negativos) / VERDE (positivos) — `ElementChipData.Negative` nuevo (service lo llena con `CombatElements.IsNegative`); `BuildArmedChip`→`BuildStateLabel`. (4) **Globos de habla** (`CombatSpeechBubbles` NUEVO + `CombatSpeechData`/`OnSpeech` en el bus): globo cómic sobre el hablante + flecha ▼ sobre el aliado objetivo, reposicionado por frame vía `RuntimePanelUtils.CameraTransformWorldToPanel`; el service emite en `PlayProc` (firma nueva `PlayProc(pe, node)`): Protector=proc Shield pre-golpe, Empático=proc Heal post-golpe (formato con nombre del curado), Agresivo=EnergyGained sobre unidad ≠ actor, narrador "¡Quedé {estado}!" en StateArmed; frases en 3 campos serializados del service (`protectorLine/empaticoLine/agresivoLine`) + `speechSeconds`. (5) **Reacción en la MISMA carta**: `FlashReaction(text,color)` en la barra (label transient `reactionFlashSeconds`=2s, color del elemento), llamado desde el branch Reaction de PlayProc. (6) **Timing**: `stateBeatSeconds` (0.5) — beat extra en Reaction/StateArmed/StateConsumed/procs de rol. (7) **vcams** (`CombatCameraDirector` NUEVO): sube prioridad de la vcam de la unidad activa vía `CombatVisualizerService.VCamOf` nuevo (OnActiveUnit), resetea la anterior, `sceneCamera` general al mando en start/end; `CombatVisualUnit.VCam` guarda la ref al spawn. Fix de compilación aplicado: `Color32`→`(Color)` cast en 3 styles de CombatSpeechBubbles (CS0029).

> ### 📋 Quirks / hallazgos S43 (verificación)
> 1. **Escena post-wiring**: GO CombatVisualizer ahora tiene 8 componentes (+ `CombatSpeechBubbles` con `document` al UIDocument del panel, + `CombatCameraDirector` con defaults scene=10/active=20); raíz nueva `VCam_Scene` (CinemachineCamera, encuadre copiado de Main Camera 0,5.2,-8.2 · rot 30,0,0, Priority.Enabled=true Value=10); Main Camera + `CinemachineBrain`. Guardado con OK previo de Juan.
> 2. ⚠️ **HALLAZGO para Juan (decisión de diseño)**: con las vcams cercanas por unidad (offset 0,1.5,-2.6), el globo de habla suele quedar FUERA de pantalla (se verificó bubble renderizando en panel y=-271 con panel de 675 de alto — la cabeza del hablante proyecta arriba del borde). En plano general se ve perfecto. Opciones: clamp del globo a los bordes del panel (cambio chico en CombatSpeechBubbles) o re-encuadrar las vcams (más lejos/más abajo). Decidir en S44.
> 3. El quirk heredado S41 de `SetAuto(true)` durante `busy` afecta también al testeo por MCP: hicieron falta 3 llamadas (las 2 primeras cayeron mid-turno con busy=true y AutoRoutine nunca arrancó pese a isAuto=true). Sin cambio de código — mismo workaround del "segundo click en ▶".
> 4. El **record seed 4242 de S42 ya no existe** (limpieza del registry): la pelea elemental vigente es seed `-463063901` (6 records espejo, 27 turnos, 83 eventos elementales, 3v3 OK) — usarla para futuros tests de replay.
> 5. Los popups en tiempo real sí se capturan con `EditorApplication.Step()` en loop (a diferencia de lo notado en S42): quedaron en screenshot el popup "Debilidad" con color y "Cura +2". Screenshots de verificación en `Assets/Screenshots/s43_*.png` (borrables).
> 6. Roslyn volvió a funcionar en `execute_code` (compiler=roslyn en toda la sesión) — el fallback C#6 de codedom ya no fue necesario; actualizar hábito, no la regla (re-chequear por sesión).

**Files Created (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatSpeechBubbles.cs` (NUEVO): globos de habla + flecha al objetivo, suscriptor de OnSpeech/Start/End, posicionamiento world→panel por frame.
- `Systems/CombatVisualizer/CombatCameraDirector.cs` (NUEVO): director de prioridades de vcams por OnActiveUnit/Start/End, pide vcam por `VCamOf` al singleton del service.

**Files Touched (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatVisualEvents.cs`: + `ElementChipData.Negative`; + struct `CombatSpeechData`; + evento/helper `OnSpeech`/`Speech`.
- `Systems/CombatVisualizer/CombatDamageNumbers.cs`: + label "Escudo" para `CombatPopupKind.Shield`.
- `Systems/CombatVisualizer/CombatVisualUnits.cs`: + `CombatVisualUnit.VCam` (ref asignada al spawn).
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: + `stateBeatSeconds`/frases speech serializadas; + `VCamOf`; + `PushShield`/`PushShieldAll` (Restore, post-turno, mid-golpe); + `Negative` en chips; + caso Shield en `ProcText`/`RaiseProcPopup`; `PlayProc(pe, node)` + emisiones de speech + FlashReaction + beat.
- `UI/CombatOrderBarUITK.cs`: body en fila `cv-ob-body-row` con mini-chips de elemento/rol + `RoleInitial`; labels de rol/elemento fuera.
- `UI/MoriMonchiCombatVisualizerUITK.cs`: + `SetShield`/`FlashReaction`/`reactionFlashSeconds`; shield-track/fill dinámicos; cajas `states-negative`/`states-positive`; `BuildArmedChip` borrado → `BuildStateLabel`.

**Files Touched (no-ScriptNode):** `UI Toolkit/CombatVisualizerPanel.uss` (cartas comprimidas), `Resources/Scenes/CombatVisualizerMM.unity` (wiring S43: componentes nuevos + VCam_Scene + CinemachineBrain), `Assets/Screenshots/s43_*.png` (verificación, borrables).

**Next session (S44, candidatos — Juan decide):**
1. Resolver el hallazgo del globo fuera de pantalla (clamp en `CombatSpeechBubbles` o re-encuadre de vcams por unidad) + tuning fino de encuadres/timing que pida Juan tras jugar el replay.
2. ⚠️ **Cambio de sim pendiente de S42 (sesión aparte, rompe paridad a propósito)**: escudo dura SOLO UNA RONDA (CombatService + Determinism OK + nota en Index/13 + oráculo nuevo).
3. Después: F5 async 3v3. Pendientes que arrastran: economía F7 (PriceModifier), ítems con estados, tuning de knobs.

**ScriptNodes (cierre S43):** CREAR `CombatSpeechBubbles.md`, `CombatCameraDirector.md`; ACTUALIZAR `CombatVisualEvents.md`, `CombatDamageNumbers.md`, `CombatVisualUnits.md`, `CombatVisualizerService.md`, `CombatOrderBarUITK.md`, `MoriMonchiCombatVisualizerUITK.md`.

---

**Session:** 2026-07-12 (Session 42 — **F4 CAPA VISUAL DEL REPLAY 3V3 — IMPLEMENTADA, ITERADA CON JUAN (2 rondas) Y VERIFICADA EN PLAY**) — **🟢 compilación 0 errores · 🟢 replay end-to-end en Play vía MCP (record fresco con 57 eventos elementales, ida/Back con revive/fin) · 🟢 los 4 criterios de éxito de Juan cumplidos y vistos en screenshot (rol+elemento legibles · afinidad legible · marcas aliadas/enemigas legibles · efecto de cada estado legible vía tooltip con Description del ElementTable) · 🟢 escena mutada y guardada con OK de Juan · 🟢 Juan aprobó el funcionamiento ("me encanta como funciona") y dejó lista de tweaks visuales para S43**
**Focus:** (1) **Barra superior de orden de acción** (`CombatOrderBarUITK` NUEVO, Mono hermano patrón F3 con refs propias `UIDocument`+`ElementTableSO`): cartas por unidad (swatch ColorHex, nombre, rol, elemento coloreado por `GetIdentity`, marcas ALIADAS chips borde superior / ENEMIGAS borde inferior, estados armados con borde verde/rojo, 2 circulitos de afinidad + label "⚡n" de energía, muertos en gris) + **tooltip hover** por chip con la `Description` exacta del ElementTable; tras feedback de Juan la barra pasó a ser **ESTÁTICA** (equipo A izq · gap · equipo B der, cartas nunca se reordenan) con **indicador de turno móvil** (▼ + borde amarillo + scale, clase `cv-order-card--active`) driven por evento nuevo `OnActiveUnit`. (2) **Bus visual aditivo** (`CombatVisualEvents`): structs `ElementChipData`/`CombatOrderEntry`, eventos `OnActionOrder` (estado por unidad tras cada nodo; ya no reordena, solo actualiza) / `OnUnitAffinity(side,index,affinity,energy; -1=sin cambio)` (beat mid-turno: los circulitos llegan a 2 llenos un instante antes de vaciarse con EnergyGained) / `OnActiveUnit(side,index)`; `CombatVisualContext` + `SnapsA/SnapsB`; `CombatVisualPopup` + `Text`/`OverrideColor`/`HasOverrideColor`. (3) **Service**: ref serializada `elementTable` (wireada en escena), `PublishOrder()` (orden de próxima acción por primera-aparición, muertos al final), `PushElements()` (chips de marcas/estados a las barras world-space vía `CombatElements.IsNegative`), popup de reacción propio (`CombatPopupKind.Reaction` append-only + texto = `ReactionName` + color del elemento; `CombatDamageNumbers` renderiza texto/color custom), narración del log con DisplayName del ElementTable (`ElemName`/`StateName`), **fix contador** (`CombatNode.ActionIndex`: el header ya no muestra la ronda del sim — "Turno acción/total" correcto hasta el final). (4) **Barra world-space** (`MoriMonchiCombatVisualizerUITK`): `Bind` 6 args (+ rol, nombre y color de elemento — fila "role-element" bajo el nombre), `SetElementState(marks, armed)` → filas dinámicas marks-ally (arriba) / armed-row (★) / marks-enemy (abajo). (5) **vcams**: `CombatVisualUnits.Spawn` crea `VCam_{side}{i}` hija de cada MM (CinemachineCamera priority 0 + RotationComposer + LookAt; paquete 3.1.6) — foco/prioridad quedan del lado de Juan. (6) **Layout v2** (ronda 2 de feedback): log ABAJO full-width con header "Registro" + botón minimizar ▼/▲ (minimizado = tira fina sin hueco), controles ◀ ▶ ▶▶ + velocidad compactos en columna al borde IZQUIERDO (son de testeo), "Turno X/Y" en caja al borde DERECHO, botón Volver (CombatTopBar) abajo-izquierda con gutter propio (log-frame left:96px).

> ### 📋 Quirks / notas S42
> 1. **Records pre-S42 NO son replayables con capa elemental**: los 24 records del registry eran pre-paso-0 → sin ElementMarks/Affinity/Energy (las cartas muestran circulitos vacíos y cero chips; fue el "bug" de afinidad que reportó Juan — no era bug). Solo peleas simuladas desde hoy traen la data completa. 2. Se corrió UNA pelea de test para generar record elemental (`CombatService.Simulate` directo, seed 4242, primeros 6 elegibles por UniqueID + `GameEvents.RegistryChanged(reg)`) — consumió 1 fight de 6 MMs con consecuencias normales, persistida local+nube. 3. **vcams a priority 0**: si Juan agrega `CinemachineBrain` a la Main Camera SIN una vcam de escena de mayor prioridad, las vcams de los MMs toman el control. 4. El popup de reacción anima en tiempo real — no se puede capturar con `EditorApplication.Step()` (frames congelados); verificar a ojo (el pipeline es el mismo del popup de ¡Crítico! que sí se ve). 5. El glifo "⚡" renderiza bien en el PanelSettings actual. 6. El lector de afinidad en cartas: `CombatUnitState.Affinity` post-turno solo vale 0/1 (convierte a energía en 2) — el beat de "2 llenos→vacía" existe solo mid-turno vía `OnUnitAffinity` con Amount del proc. 7. Convención layout: `turn-box`/`controls` absolutos por borde; el log define su alto por contenido con max-height 34% (sin height rígida). 8. Restos sin commitear de S40/S41 siguen en git (Juan maneja git).

**Files Created (.cs — input ScriptNodes):**
- `UI/CombatOrderBarUITK.cs` (NUEVO): barra superior de orden de acción — cartas estáticas por unidad + indicador de turno + tooltips con Description del ElementTable; vive solo del bus (regla 2).

**Files Touched (.cs — input ScriptNodes):**
- `Core/Enums.cs`: + `CombatPopupKind.Reaction` (append-only).
- `Systems/CombatVisualizer/CombatVisualEvents.cs`: + `ElementChipData`/`CombatOrderEntry`; + `OnActionOrder`/`OnUnitAffinity`/`OnActiveUnit`; ctx + `SnapsA/SnapsB`; popup + `Text`/`OverrideColor`/`HasOverrideColor`.
- `Systems/CombatVisualizer/CombatDamageNumbers.cs`: topText usa `p.Text` si viene; color usa `OverrideColor` si viene; label "¡Reacción!".
- `Systems/CombatVisualizer/CombatVisualUnits.cs`: `Spawn` + param `ElementTableSO`; Bind 6 args con identidad de elemento; vcam hija por unidad.
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: + `elementTable`; `ActionIndex` (fix contador); `PublishOrder`/`PushElements`; popup Reaction con texto/color; `ElemName`/`StateName` en narración; emisiones `UnitAffinity` (AffinityGained/EnergyGained/EnergySpent) y `ActiveUnit` (ForwardRoutine + Restore).
- `UI/MoriMonchiCombatVisualizerUITK.cs`: `Bind(name, atk, spd, role, elementName, elementColor)`; + `SetElementState(marks, armed)`; filas dinámicas role-element / marks-ally / armed-row / marks-enemy.
- `UI/CombatVisualizerPanelUITK.cs`: botón `btn-log-toggle` (minimizar/expandir log).

**Files Touched (no-ScriptNode):** `UI Toolkit/CombatVisualizerPanel.uxml` (order-bar + turn-box + controls absolutos + log-header) / `.uss` (reescrito: layout v2 + clases `cv-ob-*`, `cv-order-card--active`, gutter log), `UI Toolkit/CombatTopBar.uss` (Volver abajo-izquierda), `Resources/Scenes/CombatVisualizerMM.unity` (wiring: `elementTable` en el service + componente `CombatOrderBarUITK` en el GameObject CombatVisualizer con document+elementTable), `Assets/Screenshots/s42_*.png` (verificación, borrables), registry asset (pelea de test).

**Next session (S43 — TWEAKS VISUALES del replay, lista textual de Juan al cierre; "aún no se entiende muy bien los efectos pero vamos por buen camino"):**
1. **Cartas superiores más comprimidas**: nombre más chico; a la DERECHA del nombre, ícono de su elemento y su rol.
2. **Escudo en la barra de vida**: que se acumule visualmente sobre la barra de HP en color AZUL (el dato ya viaja: `CombatUnitState.Shield` + `DefenderShieldAfter`).
3. ⚠️ **NOTA DE DISEÑO para el sim (sesión aparte, rompe paridad a propósito)**: el escudo debe durar SOLO UNA RONDA, para no quitarle el rol a la curación (decisión de Juan; requiere cambio en CombatService + Determinism OK + nota en Index/13).
4. **Popups como globos de habla** (los MMs "dicen" sus acciones): Protector "¡Toma, protégete!" + INDICADOR estilo flecha apuntando al aliado protegido; Empático dice a quién cura + indicador; Agresivo al compartir energía: "te comparto mi espíritu de pelea".
5. **Barras world-space**: estados más centrados y CON NOMBRE (no solo inicial — "no se entiende mucho"); contenedor cuadrado ROJO para negativos y a la derecha cuadrado VERDE para positivos; si hay reacción elemental, indicarla en la MISMA carta; usar el mismo globo de texto como narrador de estados ("qué pasó").
6. **Más timing**: pausas/beats cuando suceden estados, para marcar las acciones.
7. **vcams Cinemachine simples**: al turno de un MM, subir la PRIORIDAD de su vcam (las vcams ya están spawneadas priority 0 + RotationComposer; falta CinemachineBrain en Main Camera + vcam general de escena + la lógica de prioridad — el evento `OnActiveUnit` ya da el (side,index) del turno). **Documentar el sistema al implementarlo.**
Después: F5 async 3v3. Pendientes que arrastran: economía F7 (PriceModifier), ítems con estados, tuning de knobs.

**ScriptNodes (cierre S42):** CREAR `CombatOrderBarUITK.md`; ACTUALIZAR `Enums.md`, `CombatVisualEvents.md`, `CombatDamageNumbers.md`, `CombatVisualUnits.md`, `CombatVisualizerService.md`, `MoriMonchiCombatVisualizerUITK.md`, `CombatVisualizerPanelUITK.md`.

---

**Session:** 2026-07-12 (Session 41 — **F4 PASO 0 (EVENTOS ELEMENTALES AL RECORD) + F4 PASO 1 (REPLAY 3V3 EN CombatVisualizerMM) — AMBOS VERIFICADOS**) — **🟢 compilación 0 errores · 🟢 paso 0 con PARIDAD EXACTA al oráculo S40 (SHA256 `7a1eab96…`, 217.452 chars — el runner se recuperó del transcript S40 porque el domain reload borra el historial de execute_code) · 🟢 DETERMINISM OK (log + JSON de Turns idénticos en 2 corridas) · 🟢 record verificado (125 eventos elementales en 31 turnos, cross-check evento-por-evento vs log) · 🟢 replay 3v3 probado en Play vía MCP end-to-end (spawn 6 unidades, auto/Next/Back con revive, muerte, ganador, consola limpia) · 🟢 escena mutada y guardada con OK de Juan**
**Focus:** (1) **Paso 0**: los eventos elementales dejaron de ser log-only — se graban como `CombatProcEvent` ADITIVO en el mismo stream `Turn.Procs` (preserva orden interleaved con procs clásicos): enum nuevo `ElementEventKind` (13 valores: MarkApplied/MarkRemoved/Reaction/StateArmed/StateConsumed/StateRemoved/Heal/Damage/ShieldDoubled/AffinityGained/EnergyGained/EnergySpent), campos nuevos en el proc (`ElementEvent/Element/ElementB/AllySource/State/ReactionName` — significativos SOLO si `ElementEvent != None`; `TargetStatusAfter` null en elementales), `CombatUnitState` + `ElementMarks` (DTO nuevo `CombatElementMark`)/`ArmedStates`/`Affinity`/`Energy`, `CombatResolver.RecordElement()`, y plumbing del resolver a los emisores (`CombatElements.AddMark`, `ReactionEffectBase.Apply` y 8 leaves, `CombatStrike.Execute`, `RoleActiveBase/CombatRoleHooks.ResolveTarget` ganan param `r`; `GainAffinity(actor, result, r)`; `r.BeforeStrike=false` se movió a ANTES del strike — cero impacto en records viejos). (2) **Paso 1**: `CombatVisualizerService` REESCRITO a equipos (1..3 por lado; el modo 1v1 se RETIRÓ — post-hard-reset no existe ningún record 1v1): `CombatNode` lee `TeamStateA/B` del record (Back/Restore casi gratis), turnos animan por `AttackerIndex/DefenderIndex`, barras usan stats EXACTOS del snapshot (sin recomputar — listo para async), log del replay narra marcas/reacciones/estados (afinidad/energía fuera a propósito), reacciones con popup "¡Sinergia!" violeta placeholder; `CombatVisualUnits` NUEVO (regla 11: spawn/lookup por lado+índice sobre anchors por fila); `CombatVisualEvents` aditivo (`UnitHpChanged`/`UnitDead`, índices en `CombatVisualHit`, teams en Context); `CanReplay` ahora EXIGE record 3v3 con los 6 IDs resolubles; `CombatSceneManager` delega a `Play(self, record)` (el service resuelve equipos).

> ### 🎬 Escena CombatVisualizerMM (mutada por MCP, guardada)
> `BOARDS/BoardA` (0,0,-2.5) y `BoardB` (0,0,2.5, yaw 180) — 7 anchors c/u por nombre **`Front0/Front1/Mid0/Mid1/Mid2/Back0/Back1`** (hex 2-3-2: front/back x=±0.75, mid x=0/±1.5, y=0.5) con hijo `Platform` (cilindro placeholder gris). AnchorA/AnchorB/2 Podiums viejos DESACTIVADOS (no borrados). `CombatVisualizerService.boardA/boardB` wireados. Main Camera reencuadrada (0,5.2,-8.2 · rot 30,0,0) — Juan la re-estiliza a gusto; los anchors son el contrato de spawn.

> ### 📋 Quirks / notas S41
> 1. **Convención de Amount en eventos elementales**: afinidad/energía → TOTAL resultante; Heal/Damage → magnitud; ShieldDoubled → escudo resultante; Boiling consumido → daño amplificado; Charcoal consumido → reflect (+ evento Damage aparte sobre el atacante). 2. Los campos elementales del proc quedan en default en eventos None (p.ej. `State` serializa "Energizado" por ser valor 0) — **el lector SIEMPRE gatea por `ElementEvent`**, nunca por campo suelto. 3. Header del panel dice "Turno 4/19" al final: `TurnNumber` = RONDA del sim, `totalTurns` = acciones — cosmético, pendiente. 4. `SetAuto(true)` durante `busy` no arranca la corrutina (edge heredado del 1v1, puede pedir segundo click en ▶). 5. Para testear por MCP sin foco del editor el player loop se congela — usar `EditorApplication.Step()` en loop (runInBackground=false; Application.runInBackground en runtime no alcanzó). 6. El oráculo de paridad vigente sigue siendo `7a1eab96…` (runner recuperable del transcript S40 o re-extraíble: fixtures sobre primeros 6 vivos por UniqueID, semillas 101-606). 7. Records 1v1 legacy ya NO son replayables (`CanReplay` exige SelfTeam) — decisión consciente, no existe ninguno.

**Files Created (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatVisualUnits.cs` (NUEVO): CombatVisualUnit + CombatVisualUnits — spawn/lookup/lifecycle de las unidades del replay (anchors por fila, Bind de barras desde snapshot).

**Files Touched (.cs — input ScriptNodes):**
- `Core/Enums.cs`: + enum `ElementEventKind` (append-only).
- `Data/Combat/CombatProcEvent.cs`: + 6 campos elementales aditivos.
- `Data/Combat/CombatRecord.cs`: `CombatUnitState` + ElementMarks/ArmedStates/Affinity/Energy; + DTO `CombatElementMark`.
- `Systems/Combat/CombatResolver.cs`: + `RecordElement(...)`.
- `Systems/Combat/CombatElements.cs`: `AddMark` + param `r`; graba MarkApplied/Reaction; pasa `r` a los effects.
- `Data/Combat/ReactionEffectBase.cs`: `Apply` + param `r` (base + 8 leaves); cada leaf graba su evento.
- `Systems/Combat/CombatStrike.cs`: `Execute` + param `r`; graba 5 consumos de estado + Damage del reflejo Charcoal.
- `Systems/Combat/CombatRoleHooks.cs`: `ResolveTarget` + param `r` (passthrough).
- `Data/Combat/RolePassiveBase.cs`: EnergySpent al gastar energía (2 leaves); AddMark con `r`.
- `Data/Combat/RoleActiveBase.cs`: `ResolveTarget` + param `r`; EnergySpent/EnergyGained en BacklineHunter.
- `Systems/Combat/CombatService.cs`: StateConsumed (Energizado/Confuso/Mareado) + Damage de Mareado; `GainAffinity` graba AffinityGained/EnergyGained; BeforeStrike flip pre-strike; helper `UnitState(u)` puebla los campos nuevos de `CombatUnitState` en EmitTurn.
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: REWRITE a equipos (ver Focus).
- `Systems/CombatVisualizer/CombatVisualEvents.cs`: aditivo — UnitHpChanged/UnitDead, índices en Hit, teams en Context.
- `Systems/CombatVisualizer/CombatReplayRequest.cs`: `CanReplay` 3v3 (exige SelfTeam + 6 IDs en registry); `ResolveOpponent` BORRADO.
- `Systems/CombatVisualizer/CombatSceneManager.cs`: `ConsumeReplayRequest` ya no resuelve rival — `Play(self, record)`.

**Files Touched (no-ScriptNode):** `Resources/Scenes/CombatVisualizerMM.unity` (boards/anchors/plataformas + wiring + cámara), `Assets/Screenshots/` (capturas de verificación, borrables). NOTA: los flags git de `CombatManagerSO.cs`/`RoleTableSO.cs`/`ElementTableSO.cs`/`CombatItems.cs` son restos SIN COMMITEAR de S40 — hoy no se tocaron.

**Next session (F4 — CAPA VISUAL del replay, diseño iterativo con Juan):**
1. **Barra superior de orden de acción** (visión de Juan, Index/13): cartas por unidad — marcas ALIADAS borde superior, ENEMIGAS borde inferior, 2 circulitos de afinidad que se llenan por acción (los datos ya viajan: eventos AffinityGained/EnergyGained + CombatUnitState.Affinity/Energy).
2. Chips de marca/estado elemental en las barras world-space (identidad visual vía `ElementTableSO.GetIdentity` — DisplayName + UiColor existen); popup de reacción propio (hoy placeholder "¡Sinergia!" violeta).
3. Fix cosmético contador de turnos (ronda vs acción) + tuning del beat que pida Juan tras testear.
4. Setup de vcams Cinemachine dentro de los MMs spawneados (Juan maneja foco/prioridad).
Después: Fase 5 async 3v3. Pendientes que arrastran: economía F7 (PriceModifier), ítems con estados, tuning de knobs.

**ScriptNodes (cierre S41):** CREAR `CombatVisualUnits.md`; ACTUALIZAR `Enums.md`, `CombatProcEvent.md`, `CombatRecord.md`, `CombatResolver.md`, `CombatElements.md`, `ReactionEffectBase.md`, `CombatStrike.md`, `CombatRoleHooks.md`, `RolePassiveBase.md`, `RoleActiveBase.md`, `CombatService.md`, `CombatVisualizerService.md`, `CombatVisualEvents.md`, `CombatReplayRequest.md`, `CombatSceneManager.md`.

---

**Session:** 2026-07-12 (Session 40 — **REFACTOR DE EXTENSIÓN COMPLETO (plan S40 de Index/13 ejecutado entero): descomposición de CombatService + RoleTableSO v2 polimórfico + ElementTableSO nuevo — VERIFICADO POR PARIDAD AL HASH**) — **🟢 compilación 0 errores · 🟢 PARIDAD EXACTA tras cada hito (oráculo: 3 fixtures × 6 semillas sobre clones, 193.676 chars de log, SHA256 `7bde5212…` idéntico antes/después de los 3 hitos) · 🟢 DETERMINISM OK (2 corridas misma semilla → fingerprint idéntico) · 🟢 wiring de assets por MCP con OK de Juan · 🟢 Play smoke test limpio**
**Focus:** cero cambio de gameplay — todo el refactor es habilitador de extensión (pedido de Juan, patrón `EquipmentSO.Effects` en todo): (1) **Hito 1**: `TakeTurn` descompuesto en colaboradores estáticos `CombatRoleHooks` (pasivas/activas de rol) / `CombatItems` (paso N usos + CollectUses) / `CombatStrike` (evasión/crit/daño/escudo/estados de golpe/marca enemiga) — CombatService 747→607 líneas, núcleo delgado (validación, orden de ronda, secuencia, consecuencias); (2) **Hito 2**: `RoleProfile` v2 = stats fijos (`ConMod/AtkMod/SpdMod/PriceModifier`) + listas polimórficas `Passives` (`RolePassiveBase`: hooks virtuales OnTurnStart/OnDamageDealt — leaves `ShieldAllyPassive{AmountPerTurn}`, `HealLowestAllyOnHitPassive{PercentOfDamage}`) y `Actives` (`RoleActiveBase.ResolveTarget` — leaf `BacklineHunterActive{Chance}` con fallback comparte-energía); `CombatRoleHooks` pasa a iterar las listas (mismas 3 firmas públicas, CombatService no se tocó en este hito); botón "Poblar v2" reproduce el contenido v1 exacto; (3) **Hito 3**: `ElementTableSO` NUEVO (3 secciones: Identities `Element→{DisplayName,UiColor}` · States `ElementalState→{DisplayName,Description,Percent,Amount}` — **los 8 knobs elementales se MUDARON acá desde CombatManagerSO** (Vaporizado/GolpePreciso/Boiling/Charcoal en Percent; Mareado Percent+Amount; Cleanse/Leech viven en sus leaves) · Reactions `List<ElementReaction>{Name,A,B,AllySource,Effects}`); leaves `ReactionEffectBase` (`ArmStateEffect`/`CleanseEffect`/`DoubleShieldEffect`/`LeechEffect`/`RemoveRandomMarkEffect` + genéricos del plan `HealEffect`/`DamageEffect`/`GrantEnergyEffect`, aún sin uso en recetas v1); `CombatElements` pasó de dueño de tablas hardcodeadas a EJECUTOR del SO (conserva `ElementMark`, bookkeeping de marcas en `AddMark` e `IsNegative`; `ReactionFor`/`ApplyState` BORRADOS — sus cuerpos viven en los leaves); cableado `CombatManagerSO.Elements` (patrón Roles, null = sin reacciones ni bonus).

> ### 🔬 Método de verificación S40 (reutilizable)
> Oráculo de paridad ANTES de tocar código: `SimulateCore` sobre CLONES (patrón Verify Determinism: `SaveSystem.DeserializeCreature(Serialize(d))`, sin mutar registry, en EDIT MODE vía `execute_code` C#6) — fixture 1: primeros 6 vivos por UniqueID con EQ0/EQ1 forzados; fixture 2: elementos/roles forzados para cubrir los 12 estados; fixture 3: solo Agua/Fuego aliados (Vaporizado). 6 semillas fijas c/u, log concatenado → SHA256. Tras cada hito: mismo runner → mismo hash exacto. Logs en el scratchpad de la sesión (`baseline_s40.log`, `parity_hito1/2/3.log`).

> ### ➕ Addendum post-cierre S40 (mismo día, pedido de Juan)
> 1. **Descripciones del ElementTable**: cada `StateDefinition.Description` ahora dice el efecto EXACTO del estado, cuándo se consume y qué representa su knob; `Percent` pasó a slider `[PropertyRange(0f,1f)]` con tooltip, `Amount` con tooltip; asset repoblado (knobs sin cambio, paridad verificada con el hash viejo).
> 2. **Reorden narrativo del log** (base para F4): en `CombatStrike.Execute` la línea del golpe se emite ANTES de Charcoal/marca enemiga (antes las marcas/reacciones salían primero) — la línea del golpe ahora muestra el HP post-golpe PRE-reacción; `StrikeOutcome.DefenderHpAfter` sigue capturándose post-reacción (record intacto). `GainAffinity` loguea `[afinidad] {name} +1 afinidad (n/2)` en CADA acción (antes solo se logueaba la conversión a energía). Secuencia resultante: golpe → [marca] → [reacción]/[estado] → [Empático] → [afinidad] → [energía]. RNG intacto, Determinism OK. **ORACLE NUEVO** (el log cambió a propósito): SHA256 `7a1eab96…` (217.452 chars, `oracle_s40_v2_logorder.log` en scratchpad) — usar ESTE para futuros refactors.
> 3. **Visión de Juan para F4 registrada en [[Index/13 - Combat Design Direction]]** (sección "Visión de Juan para F4"): vcam Cinemachine simple dentro de cada MM (nosotros seteamos, Juan enfoca por prioridad), barra superior de orden de acción con cartas (marcas aliadas arriba / enemigas abajo / 2 circulitos de afinidad que se llenan por acción), plataformas 2-3-2 abajo.

> ### 📋 Quirks / notas S40
> 1. Firma nueva de hooks: `RolePassiveBase.OnTurnStart/OnDamageDealt(actor, allies, config, result, r, rng)` (virtuales no-op) y `RoleActiveBase.ResolveTarget(actor, allies, enemies, config, result, rng)` (null = no decide targeting; la primera activa que devuelve target gana, fallback `PickFrontTarget`). `ReactionEffectBase.Apply(bearer, reactor, reactionName, result, rng)` — el `reactionName` se interpola en los logs (paridad con los strings viejos). 2. Knobs de CONSUMO de estados armados viven en `States` del ElementTable (`StatePercent/StateAmount`); knobs de efectos INSTANTÁNEOS viven en su leaf (CleanseEffect.HealPercent=0.20, LeechEffect.Amount=4). 3. `RoleTable.asset` re-poblado v2 y `ElementTable.asset` creado/poblado/wireado por MCP (reflection sobre los botones privados + SetDirty + SaveAssets); `CombatManager.asset` ya no tiene los 8 floats. 4. Los logs de combate quedaron byte-a-byte idénticos a S39 — visualizer/record no notan nada. 5. Los 3 sub-agentes `morimonchi-coder` trabajaron con specs verbatim; review del orquestador sin hallazgos. 6. `CombatService` quedó en 607 líneas (>400): tolerado como núcleo de secuencia — si crece más, el siguiente corte natural es mover BuildCombatant/Snapshot/TickStatuses a un colaborador de setup.

**Files Created (.cs — input ScriptNodes):**
- `Systems/Combat/CombatRoleHooks.cs` (NUEVO): ejecutor de Passives/Actives del RoleProfile (ResolveTarget/GrantShield/HealAfterStrike).
- `Systems/Combat/CombatItems.cs` (NUEVO): CollectUses + UseItems (paso de ítems N usos).
- `Systems/Combat/CombatStrike.cs` (NUEVO): StrikeOutcome + Execute (evasión/crit/daño/escudo/Vaporizado/GolpePreciso/Debilidad/Boiling/Charcoal/marca enemiga + log del golpe).
- `Data/Combat/RolePassiveBase.cs` (NUEVO): base + ShieldAllyPassive + HealLowestAllyOnHitPassive.
- `Data/Combat/RoleActiveBase.cs` (NUEVO): base + BacklineHunterActive.
- `Data/Combat/ReactionEffectBase.cs` (NUEVO): base + 8 leaves de reacción.
- `Data/Combat/ElementTableSO.cs` (NUEVO): ElementIdentity/StateDefinition/ElementReaction + SO con Poblar v1.

**Files Touched (.cs — input ScriptNodes):**
- `Systems/Combat/CombatService.cs`: TakeTurn delegado a los 3 colaboradores; Mareado lee `config.Elements` (StatePercent/StateAmount); CollectUses removido; header actualizado.
- `Data/Combat/RoleTableSO.cs`: RoleProfile v2 (fuera ShieldPerTurn/BacklineHitChance/HealPercentOfDamage; + Passives/Actives); PopulateV1→PopulateV2.
- `Data/Combat/CombatManagerSO.cs`: − 8 knobs elementales; + `Elements` (ElementTableSO).
- `Systems/Combat/CombatElements.cs`: ejecutor del SO (AddMark → FindReaction → Effects.Apply); − ReactionFor/ApplyState.

**Files Touched (no-ScriptNode):** `ScriptableObjects/Abilitys/ElementTable.asset` (NUEVO, poblado v1, wireado), `ScriptableObjects/Abilitys/RoleTable.asset` (re-poblado v2), `ScriptableObjects/Abilitys/CombatManager.asset` (− knobs, + Elements), `MoriMonchiVault/Index/13` (PLAN S40 marcado ejecutado).

**Next session (Fase 4 — VISUALIZER 3V3, orden de Juan):**
1. **Paso 0: enriquecer el record** (los eventos elementales siguen log-only): eventos de marca/reacción/estado/energía en `CombatProcEvent` (campos aditivos) o lista nueva por turno; `CombatUnitState` + marcas/estados armados/energía. Backward-compatible. Los puntos de emisión ahora están limpios: `CombatElements.AddMark` + leaves de `ReactionEffectBase` (hoy loguean; ahí mismo se graba al record).
2. Replay 3v3: 6 unidades + grilla en `CombatVisualizerMM`, quitar guard `CanReplay`, tarjetas historial 3v3.
3. Capa visual elemental: chips de marca por canal, popup de reacción, iconos de 12 estados, pips de energía (la identidad visual puede leer `ElementTableSO.GetIdentity` — DisplayName + UiColor ya existen).
4. Después: Fase 5 async 3v3. Pendientes: tuning de knobs con data real (ahora en ElementTable/RoleTable, sin recompilar), ítems con estados, economía F7 (PriceModifier sigue sin consumidor).

**ScriptNodes (cierre S40):** CREAR `CombatRoleHooks.md`, `CombatItems.md`, `CombatStrike.md`, `RolePassiveBase.md`, `RoleActiveBase.md`, `ReactionEffectBase.md`, `ElementTableSO.md`; ACTUALIZAR `CombatService.md`, `RoleTableSO.md`, `CombatManagerSO.md`, `CombatElements.md`.

---

**Session:** 2026-07-11/12 (Session 39 — **FASE 3 LIMPIEZA LEGACY + FASE 7 PERSONALITY→ROLE + FASE 6 CAPA ELEMENTAL CORE — TODO IMPLEMENTADO Y VERIFICADO EN PLAY VÍA MCP**) — **🟢 compilación 0 errores · 🟢 los 12 estados elementales observados funcionando por log · 🟢 DETERMINISM OK (misma semilla → log idéntico al hash) · 🟢 wiring de assets/escena por MCP · ⚠️ eventos elementales son LOG-ONLY (no van al record — prerequisito de Fase 4)**
**Focus:** (1) Fase 3: retirar tab 1v1 + overload `SimulateLocal(a,b)` + TODOS los procs de ítems (`CombatProcEffect.cs` BORRADO) + recetas de SynergyTable archivadas + `EvolutionChance`/`TriggerType` fuera; (2) nuevo sistema de ítems `ItemUseEffect` (N usos + regla Always/SelfHpBelow, leaves `HealUseEffect`/`DamageUseEffect`, disparo determinista SIN rolls, usos runtime en `Combatant.Uses`); (3) Fase 7 adelantada: enum `Personality` y `PersonalityProfileSO` ELIMINADOS — `RoleWorldProfileSO` (3 perfiles de mundo), `BreedingAffinityTableSO` re-keyeada 3×3 por Role, NameTag/detail/containers por rol; (4) Fase 6 core adelantada: `Element {Agua,Fuego,Electricidad,Planta}` en DNA (campo plano, mint random, herencia 50/50 + `ElementMutationChance` 0.10 en InheritanceOddsTable), afinidad/energía (2 acciones→1 energía), marcas por fuente (enemiga cada golpe / aliada por trait con energía; máx 1 por elemento+fuente), 12 reacciones/estados de un uso en `CombatElements` (NUEVO, estática — DIVERGENCIA: no se usó el motor de SynergyTable) + integración en `CombatService`; (5) UI: chips de elemento reales en tab Equipo 3v3, sección "Rol y Elemento" en detail, tab Equipo 3v3 entró a navegación por teclado (índice 3).

> ### 🔑 Decisiones S39 (Juan)
> Hard reset EJECUTADO por Juan (local+nube) antes de la sesión · Element = campo plano FUERA del genetic string (como Role) · herencia 50/50 + mutación · marcas 1-por-elemento+fuente persistentes, mismo elemento no reacciona · orden del roadmap: visualizer y async AL FINAL. Decisiones del orquestador (revisables): Cleanse evaluado al aplicarse (purga 1 negativo o cura 20%) · Confuso falla la acción completa · Mareado 50%/3 fijo · Confuso/Mareado generan afinidad, stun no. **Tabla completa de estados implementada (efecto/consumo/knob): [[Index/13 - Combat Design Direction]] §6 (v3).**

> ### 📋 Quirks / notas S39
> 1. Los 8 knobs elementales viven en `CombatManager` → bloque "Elemental"; defaults v1 sin tuning real. 2. **Motor de sinergias RETIRADO COMPLETO** (decisión de Juan al cierre): `SynergyTableSO.cs`/`SynergyRule.cs`/`SynergyEffectBase.cs` BORRADOS, `CombatResolver` sin CheckSynergies/bearer-helpers, `CombatManagerSO` sin campo Synergies, `SynergyTable.asset` borrado; kinds `Synergy` del enum y mapeos del visualizer quedan (append-only, inertes); smoke test 3v3 post-retiro OK. 3. EQ0 tiene `HealUseEffect` (3 usos, HP<70%, cura 4) y EQ1 `DamageUseEffect` (2 usos, always, 5) como ítems de referencia; los demás ítems S35 quedaron solo-stats o vacíos. 4. Registry quedó con ~16 MMs de test (roles/elementos forzados en varios, fights consumidos). 5. **Quirk carga en frío post-wipe**: mints hechos apenas arranca Play se borraron cuando el pull de CloudSync (nube vacía, sin sesión) aterrizó después — los posteriores persisten. 6. Lifesteal/EffDefense/EffEvasion/EffSpeed por stacks viejos quedaron en `Combatant` (inofensivos, nada los aplica ya). 7. Names UXML renombrados: `personality`→`role-element` (detail), `personality-label`→`role-label` + `.tag__personality`→`.tag__role` (NameTag).

**Files Created (.cs — input ScriptNodes):**
- `Data/Equipment/ItemUseEffect.cs` (NUEVO): efectos activos de ítem — N usos + regla de disparo (UseRule), leaves HealUseEffect/DamageUseEffect vía ICombatContext.
- `Systems/Combat/CombatElements.cs` (NUEVO): estática de marcas/reacciones elementales — ElementMark, tablas por fuente, ApplyState (instantáneos vs armados), IsNegative.
- `Data/Genetics/RoleWorldProfileSO.cs` (NUEVO): perfiles de comportamiento de mundo por Role (reemplaza PersonalityProfileSO).

**Files DELETED (.cs — remover ScriptNodes si existen):** `Data/Equipment/CombatProcEffect.cs`, `Data/Genetics/PersonalityProfileSO.cs`, `Data/Combat/SynergyTableSO.cs`, `Data/Combat/SynergyRule.cs`, `Data/Combat/SynergyEffectBase.cs`.

**Files Touched (.cs — input ScriptNodes):**
- `Core/Enums.cs`: − Personality, − TriggerType; + Element, + ElementalState (12).
- `Data/Genetics/CreatureDNA.cs`: − campo Personality; + campo Element.
- `Core/CreatureGenerator.cs`: − RandomPersonality; + RandomElement.
- `Data/Genetics/CreatureRegistrySO.cs`: botón reroll ahora rerollea Role + Element.
- `Core/GameManager.cs`: campo roleWorldProfiles (RoleWorldProfileSO); mint asigna Element random.
- `Systems/Breeding/BreedingService.cs`: herencia Element 50/50 + mutación; − Personality random.
- `Data/Breeding/BreedingAffinityTableSO.cs`: matriz re-keyeada (Role,Role) 3×3.
- `Data/Breeding/InheritanceOddsTableSO.cs`: + ElementMutationChance (0.10).
- `Systems/Breeding/BreedingController.cs`: GetAffinity por Role.
- `World/Containers/BreedingContainer.cs`: afinidad/logs por Role.
- `World/Containers/MoriMochiContainer.cs`: OccupantInfo.Role.
- `World/AI/MoriMochiAgent.cs` + `.Tuning.cs`: perfil por Role (RoleWorldProfile).
- `World/Spawning/MoriMochiSpawner.cs`: RoleWorldProfiles.
- `World/Creatures/MoriMonchiController.cs`: params RoleWorldProfileSO.
- `World/Creatures/NameTag.cs`: RoleText + query role-label.
- `Systems/Combat/CombatService.cs`: − FireProcs/RollProc/procs; + paso ítems N usos + capa elemental completa (Energizado en orden, Confuso/Mareado, procs de trait con energía, Vaporizado/GolpePreciso/Boiling/Debilidad/Charcoal, marca enemiga on-connect, GainAffinity); header reescrito con el orden determinista nuevo.
- `Systems/Combat/Combatant.cs`: Procs→Uses (ItemUseState); + Element/Affinity/Energy/Marks/States + HasState/ConsumeState.
- `Systems/Combat/CombatController.cs`: − overload 1v1.
- `Data/Combat/CombatManagerSO.cs`: − EvolutionChance; + bloque Elemental (8 knobs).
- `Systems/Combat/CombatResolver.cs`: − Synergies/CheckSynergies/FirstSatisfiedRule/ConsumeStacks/StunBearer/DamageBearer/HealBearer/AddStatusTo (el motor de sinergias completo).
- `UI/CombatPanelUITK.cs` + `.Tabs.cs` + `.Navigation.cs`: tab Combate Local RETIRADA; reindex (Resultados=1, Historial=2, Equipo 3v3=3 y DENTRO de la navegación); EnterContent para t=3 queda en TabBar (contenido mouse por CombatLineupUITK).
- `UI/CombatLineupUITK.cs`: chips de elemento reales (ElementText).
- `UI/MorimonchiDetailInfoUITK.cs`: RoleName/RoleDesc/ElementName ("Rol · Elemento"); ModifiersText itera ItemUseEffect; campo roleElementLabel.

**Files Touched (no-ScriptNode):** `UI Toolkit/CombatPanelUITK.uxml/.uss` (− tab-local, − clases exclusivas), `UI Toolkit/MorimonchiDetailInfoUITK.uxml` (sección "Rol y Elemento", name role-element), `UI Toolkit/NameTagUITK.uxml` + `NameTagUITKStyle.uss` (role-label/.tag__role), assets: `RoleWorldProfileTable.asset` (NUEVO, poblado), `PersonalityProfileTable.asset` (BORRADO), `BreedingAffinityTable.asset` (reseed 3×3), `CombatManager.asset` (knobs), equipment assets (procs removidos; EQ0/EQ1 con use-effects de referencia), `SynergyTable.asset` (rules vaciadas), `GameScene.unity` (GameManager rewireado), `MoriMonchiVault/Index/13` (v3: tabla de estados implementada + divergencia + roadmap).

**Next session (S40 — REFACTOR DE EXTENSIÓN, plan completo YA HECHO en [[Index/13 - Combat Design Direction]] sección "PLAN S40"):**
0. **S40 entera = el refactor de extensión** (pedido de Juan, patrón EquipmentSO.Effects en todo): (a) `RoleTableSO` v2 — stats + listas polimórficas Passives/Actives por rol (leaves: ShieldAlly, HealLowestAllyOnHit, BacklineHunter); (b) `ElementTableSO` NUEVO — identidad de elementos, definiciones de estados (los 8 knobs se MUDAN acá), tabla de reacciones con efectos polimórficos (`ArmStateEffect` y compañía); `CombatElements` pasa a ejecutor del SO; (c) descomponer `CombatService`/`TakeTurn` en `CombatRoleHooks`/`CombatItems`/`CombatStrike` (regla 11, jamás partial). **Verificación = paridad**: log de pelea de referencia con semilla fija antes/después IDÉNTICO + Verify Determinism.
Después (Fase 4 — VISUALIZER 3V3):
1. **Paso 0 del visualizer: enriquecer el record** (los eventos elementales hoy son log-only): eventos de marca/reacción/estado/energía en `CombatProcEvent` (campos opcionales aditivos) o lista nueva por turno; `CombatUnitState` + marcas elementales/estados armados/energía. Todo backward-compatible.
2. Replay 3v3: 6 unidades + grilla en `CombatVisualizerMM`, quitar guard `CanReplay`, tarjetas historial 3v3.
3. Capa visual elemental: chips de marca por canal, popup de reacción, iconos de 12 estados, pips de energía; storytelling (ghost bar/banner).
4. Después: Fase 5 async 3v3 (al final). Pendientes: tuning knobs, decisión motor sinergias, ítems con estados, economía F7.

**ScriptNodes (cierre S39):** CREAR `ItemUseEffect.md`, `CombatElements.md`, `RoleWorldProfileSO.md`; BORRAR `CombatProcEffect.md`, `PersonalityProfileSO.md`; ACTUALIZAR `Enums.md`, `CreatureDNA.md`, `CreatureGenerator.md`, `CreatureRegistrySO.md`, `GameManager.md`, `BreedingService.md`, `BreedingAffinityTableSO.md`, `InheritanceOddsTableSO.md`, `BreedingController.md`, `BreedingContainer.md`, `MoriMochiContainer.md`, `MoriMochiAgent.md`, `MoriMochiSpawner.md`, `MoriMonchiController.md`, `NameTag.md`, `CombatService.md`, `Combatant.md`, `CombatController.md`, `CombatManagerSO.md`, `SynergyTableSO.md`, `CombatPanelUITK.md`, `CombatPanelUITK.Tabs.md`, `CombatPanelUITK.Navigation.md`, `CombatLineupUITK.md`, `MorimonchiDetailInfoUITK.md`.

---

**Session:** 2026-07-11 (Session 38 — **FASE 2 CORE COMPLETA: tab "Equipo 3v3" con grilla 2-3-2, drag & drop, y COMBATE LOCAL 3V3 REAL DESDE UI** — diseño iterado en vivo contra 5 mockups/screenshots de Juan, resultado final aprobado: "me encanta") — **🟢 compilación 0 errores · 🟢 flujo completo verificado en Play vía MCP (lineup custom → SimulateLocal → record con rows correctas) · 🟢 wiring de escena por MCP con OK de Juan · 🟢 UI aprobada como referencia de calidad**
**Focus:** (1) UI de posicionamiento pre-pelea (el beat de agencia de Fase 2): tab nueva "Equipo 3v3" en el Combat Panel — carrusel de selección abajo, dos grillas hex 2-3-2 enfrentadas al centro, rosters laterales por equipo; (2) drag & drop runtime por punteros con slot exacto; (3) conexión del motor: botón "¡Pelear!" → `CombatController.SimulateLocal(idsA, idsB, rowsA, rowsB)` con el lineup REAL (adiós default {Front,Front,Mid} en el flujo de UI); (4) documentación del recetario UITK en [[Index/05 - UI System]] (pedido explícito de Juan).

> ### ✅ Componentes nuevos (composición, regla 11 — cero cambios al partial del panel)
> `CombatLineupBoard` (clase plana, NO Mono): renderiza UNA grilla 2-3-2 (7 slots = 2 Front/3 Mid/2 Back en 3 columnas centradas → offset hex), dueña de asignaciones DNA↔slot exacto (máx 3), espejable (`mirrored`: A front a la derecha, B a la izquierda → fronts cara a cara), API `Place/RemoveAt/CanPlace/SlotAtPosition/SetDropHighlight/SetSlotSize/Units/PlacedIds/RowOf`. `CombatLineupUITK` (Mono hermano de CombatPanelUITK, mismo GameObject, ref propia a `UIDocument` + `CreatureDatabaseSO` — patrón F3/DevConsoles): dueño de la tab — pool con exclusión (colocado = sale del carrusel), drag & drop (umbral 8px, ghost `PickingMode.Ignore`, highlight verde/rojo, drop fuera = vuelve a pool), click DERECHO = hoja de personaje (carrusel + rosters + slots ocupados; izquierdo solo drag), Auto-armar/Vaciar, rosters auto-ordenados por SPD efectiva desc, contador de peleas RESTANTES "n/x" en cards (no en slots), overlay de detalle (stats Base→Final con rol+equipo — pipeline EXACTO del sim: `GetEffectiveStats(dna,db,roles)` → `EquipmentStats.Apply`, mismo orden que CombatService — + ítems equipados), overlay de resultado (ganador A/B coloreado + evolucionó/murió + log completo), **sizing responsive** (GeometryChangedEvent → slot clamp [90,190] por alto/ancho disponible → inline style pisa USS; ghost matchea).

> ### ✅ Combate real desde la UI (hito Fase 2)
> `OnFight()`: recolecta ids+rows de ambos boards (`RowOf(slot)` → int) en el MISMO recorrido → `SimulateLocal(idsA, idsB, rowsA, rowsB)` → overlay de resultado. `RegistryChanged` sincrónico del controller ya prunea muertos/agotados de los boards y rebuildea pool ANTES del overlay. **Verificado en Play**: pelea con lineup custom (Mid/Back/Back) → el record persistido trae `SelfTeam` con esas rows exactas y el log del sim las imprime ("[A0] ... (Protector, Mid)"); FightCount consume, evolución/muerte aplican. La tab es índice 4 del TabView → fuera de la navegación por teclado (clamp 0..3 de Navigation) a propósito: WIP mouse-only, no toca `CombatPanelUITK.Navigation`.

> ### ✅ Recetario UITK documentado (pedido de Juan)
> Sección nueva en [[Index/05 - UI System]]: triple `flex-grow` del TabView (incluido `.unity-tab`, el que faltaba y causaba la banda muerta), `margin-top: auto` para anclar al fondo, el **feedback loop de auto-medición** (contenedor centrado sin width → `width: 100%`), sizing dinámico por GeometryChangedEvent (nunca px fijos pensando en una resolución), patrón completo de drag & drop runtime (**Unregister ANTES de ReleasePointer** o el PointerCaptureOut cancela el drop), click derecho, scrollbars custom, rueda→scroll horizontal, quirk de hot-reload USS en Play (huerfana el árbol, reiniciar Play), y la técnica de iterar diseño con screenshots del Game view vía MCP.

> ### 🟢 Wiring + verificación por MCP (OK de Juan)
> Componente `CombatLineupUITK` agregado al GameObject `UIManager` con `document` (UIDocument del CombatPanel) y `database` (CreatureDatabase.asset) — escena guardada. Verificación en Play por `execute_code` (abrir panel por reflection + OnAuto/OnFight) + screenshots de `manage_camera` en cada iteración de diseño (5 rondas contra mockups de Juan).

> ### 📋 Quirks / notas
> 1. Los 3 assets dev de equipo (EQ0/EQ1/EQ2 → Weapon1/Equipment2/Equipment3) se llaman TODOS "Sword" en su data — la UI resuelve bien; renombrar en inspector si molesta. 2. NREs de PlayerInputs/UIInputs/BuildingInputs en OnEnable tras recompilar con editor parado = artefacto de domain reload fuera de Play, NO reproduce en Play (vigilar). 3. El registry asset quedó tocado por las peleas de test en Play (FightCounts consumidos, una evolución, persistido local+nube). 4. `Assets/Screenshots/` acumuló capturas de verificación MCP (borrables). 5. El chip de Elemento en cards/detalle es placeholder "—" hasta la capa elemental (Fase 6).

**Files Created (.cs — input ScriptNodes):**
- `UI/CombatLineupBoard.cs` (NUEVO): widget grilla 2-3-2 (clase plana, estado+render, espejable, slot exacto, SetSlotSize).
- `UI/CombatLineupUITK.cs` (NUEVO): tab Equipo 3v3 — carrusel/rosters/drag&drop/detalle/resultado + lanza `SimulateLocal` con lineup real.

**Files Touched (.cs):** ninguno (todo el C# de la sesión vive en los 2 archivos nuevos).

**Files Touched (no-ScriptNode):** `UI Toolkit/CombatPanelUITK.uxml` (tab-lineup: carrusel + rosters + overlays detalle/resultado + botón ¡Pelear!), `UI Toolkit/CombatPanelUITKStyle.uss` (bloque `cbt-lu-*` completo + fix `.unity-tab` flex-grow + scrollbar custom), `Resources/Scenes/GameScene.unity` (componente CombatLineupUITK en UIManager, wireado), registry asset (peleas de test), `MoriMonchiVault/Index/05 - UI System.md` (Recetario UITK S38 + 2 filas en Panel Controllers).

**Next session (Fase 3 del roadmap — LIMPIEZA DE LEGACY, o lo que Juan priorice):**
1. Retirar la tab "Combate Local" 1v1 vieja + la sobrecarga transicional `SimulateLocal(a,b)` (la tab Equipo 3v3 ya cubre el flujo local completo). Revisar Navigation al retirar (índices de tabs).
2. Hard reset / migración de MMs antiguos (recomendación registrada: UN solo wipe coordinado con re-autoría de ítems a N usos; idealmente cerca del backfill de elemento de Fase 6).
3. Ítems: retirar procs de elementos S35 → sistema de N usos + regla de disparo; archivar recetas viejas de SynergyTable.
4. Pendientes menores S38: decidir si la tab Equipo 3v3 entra a la navegación por teclado cuando deje de ser WIP; tuning de textos/colores que pida Juan.
Después: Fase 4 visualizer 3v3 (reusar CombatVisualizerMM, quitar guard CanReplay) → Fase 5 async de equipos.

**ScriptNodes (cierre S38):** CREAR `CombatLineupBoard.md`, `CombatLineupUITK.md`.

---

**Session:** 2026-07-10 (Session 37 — **TESIS DE DISEÑO ROLES/ELEMENTOS (Juan) DOCUMENTADA + SIM 3V3 CORE IMPLEMENTADO, WIREADO Y VERIFICADO EN PLAY VÍA MCP**) — **🟢 compilación 0 errores · 🟢 pelea 3v3 real corrida · 🟢 DETERMINISM OK · 🟢 wiring de assets hecho por MCP con OK de Juan**
**Focus:** (1) Juan trajo la tesis completa que baja a tierra el pivot autobattler de S36: 3 roles heredables que reemplazan Personality, 4 elementos innatos con reacciones por fuente (aliada/enemiga), grilla hex 2-3-2, estados de un uso, ítems con N usos — TODO documentado en [[Index/13 - Combat Design Direction]] v2 con 15 decisiones cerradas en sesión (energía=proc, marcas ofensivas=cada golpe recibido deja la marca del atacante, escudo=HP temporal persistente, Empático cura ALIADO adicional, mods de rol sobre hoja 1-10, 1 evolución/5% muerte, 1v1 muere, etc.); (2) implementación de la Fase 1 (sim 3v3 core, sin elementos) en 4 olas de sub-agentes; (3) wiring + testeo en editor vía Unity MCP.

> ### ✅ Gen Rol (aditivo, SIN wipe de saves)
> Enum `Role {Protector, Agresivo, Empatico}` + `CombatRow {Front, Mid, Back}` + `ModifierEffectKind.Shield=12` + `CombatPopupKind.Shield` (append-only). `CreatureDNA.Role` campo nuevo (metadata como Personality — NO en genetic string; el async viaja por JSON). Mint = `CreatureGenerator.RandomRole()`; cría = 50/50 padre/madre en `BreedingService.Breed`. **Personality queda INTACTA en esta fase** (la usan breeding affinity/mundo/UI) — la migración total es Fase 7 del roadmap. Saves viejos deserializan con Role=Protector; el registry actual (15 MMs) fue rerolleado en sesión: 5 Protector / 3 Agresivo / 7 Empático (persistido local+nube).

> ### ✅ RoleTableSO + capa de rol en stats
> `RoleTableSO` (SerializedScriptableObject, dict Odin `Role→RoleProfile {ConMod, AtkMod, SpdMod, ShieldPerTurn, BacklineHitChance, HealPercentOfDamage, PriceModifier}`, botón "Poblar v1": Protector +4/−2/−2 escudo 1 · Agresivo −3/+2/+1 backline 50% precio −10% · Empatico +1/−3/+2 cura 50% precio +10% — net-zero los tres). Cableado en `CombatManagerSO.Roles` (patrón Synergies). `CombatStats.GetEffectiveStats` gana sobrecarga con roles: mods planos sobre el trío nato clampeados [1,10] ANTES de partes/equipo (sobrecarga vieja delega con null — la UI de hoja de stats aún no muestra el rol, Fase 2/3). `DeathChance` default 0.15→0.05 (código + asset).

> ### ✅ SimulateCore por EQUIPOS (1..3 por lado; gameplay = siempre 3v3)
> Firmas nuevas: `Simulate(idsA, idsB, rowsA, rowsB, …)` (valida duplicados/tamaños/capacidad de grilla 2-3-2; lineup default {Front,Front,Mid}; fan-out de records a las 6 DNAs) · `SimulateCore(teamA, teamB, rowsA, rowsB, …)` · `BuildRecord(result, selfTeam, oppTeam, self, selfWasA, …)` POR UNIDAD. Las firmas 1v1 se ELIMINARON. **Orden de rolls determinista documentado en el header**: por ronda 1 tiebreak `NextFloat` POR unidad viva (consumo constante) → sort (EffSpeed desc, tiebreak, lado, índice — orden TOTAL, sin empates posibles) → por turno: tick → targeting (Agresivo: roll 50% → backline si existe / fallback "comparte energía" no-op → frontal) → procs pasivos → stun → escudo del Protector (`PickAlly` incluye self) → armado ofensivo → evasión → crit → daño con ESCUDO absorbiendo primero (solo daño de ataque; ticks/procs lo bypasean, v1) → cura del Empático (50% del daño al aliado vivo con menos HP, sin roll, incluye self) → lifesteal → procs armados + defensivos. Fin: wipe de equipo (wipe mutuo → gana A, sin evolución) → FightCount++ a los 6, WinCount++ al ganador → **1 unidad viva al azar del ganador evoluciona** → **1 unidad al azar del perdedor rollea 5% de muerte permanente**. `CombatTargeting` NUEVO (estática: FrontRow/PickFrontTarget/PickBacklineTarget/PickAlly/LowestHpAlly — picks siempre consumen rng si hay candidatos, incluso con 1). `Combatant` + Role/Row/Index/Shield/IsAlive. `CombatResolver.Record` + TargetIndex. Sinergias/procs de equipo viejos SIGUEN funcionando sobre el motor nuevo (verificado: Explosión tóxica + Robo de vida detonaron en la pelea de test).

> ### ✅ DTOs (aditivos en lo persistido, reshape en lo transiente)
> `CombatRecord` (persistido, backward-compatible): + `SelfTeam/OpponentTeam` (List<CombatFighterSnapshot>; null = record 1v1 viejo) + `SelfTeamIds/OpponentTeamIds`; en records 3v3 los legacy `SelfStats/OpponentStats` van null → la tarjeta actual muestra el placeholder "sin stats" (rediseño = Fase 4). `CombatTurn` + AttackerIndex/DefenderIndex/DefenderShieldAfter + `TeamStateA/TeamStateB` (List<CombatUnitState> {Hp, Shield, Marks} por unidad por turno; StatusA/StatusB legacy quedan vacíos en records nuevos). `CombatFighterSnapshot` + Name/Role/Row. `CombatProcEvent` + TargetIndex. `CombatResult` (transiente, reshape): fuera Winner*/Loser*/StatsA/B → `TeamAWon/EvolvedUnitId/EvolvedUnitName/DiedUnitId/DiedUnitName/TeamA/TeamB`.

> ### ✅ Integración
> `CombatController.SimulateLocal(teams, rows)` + **sobrecarga transicional** `SimulateLocal(a,b)` (equipos de 1 — la usa la tab local del Combat Panel hasta la UI 3v3; retirar en Fase 3). `AsyncCombatService.ApplyResult` = 1v1 transicional como equipos de 1 (el JS matchmaker de equipos es Fase 5). `CombatDevConsole`: box Combat → `teamAIds/teamBIds`, "Fill Random Teams (3v3)" (exige ≥6 elegibles), Verify Determinism por equipos (fingerprint {TeamAWon, IsDraw, EvolvedUnitId, EvolvedSlot, DiedUnitId, Turns}). `CombatReplayRequest.CanReplay` devuelve false si `SelfTeam != null` (**records 3v3 NO replayables hasta el visualizer 3v3, Fase 4**; los viejos siguen).

> ### 🟢 Wiring + testeo EN EDITOR (vía Unity MCP, autorizado por Juan)
> `RoleTable.asset` creado en `ScriptableObjects/Abilitys/` y poblado por `execute_code` (quirk Odin documentado en [[Index/12 - Unity MCP]]: dict via API C# + SetDirty + SaveAssets), asignado a `CombatManager.Roles`, DeathChance del asset → 0.05. En Play: reroll de roles del registry (persistido), **pelea 3v3 real** (11 turnos, ganó Team B, "Dizzy Plop" evolucionó Eye — consumió 1 fight de las 6 involucradas), **DETERMINISM OK** (2 corridas idénticas, fingerprint 15.740 chars), y 8 seeds sobre CLONES ejercitando todas las ramas: caza backline ×8, fallback sin-backline ×2, escudo absorbió ×26 (cuentas exactas en log), curas Empático ×100, muertes 5% ×0 (esperado).

> ### 📋 Quirks / notas
> 1. Los mods de rol se aplican al trío nato PRE-partes con clamp [1,10] — un Empático con ATK nato bajo queda clampeado a 1 (visto en test: ATK final 1). Tuning pendiente con data real. 2. Escudo v1 absorbe SOLO daño de ataque directo. 3. Wipe mutuo simultáneo → gana Team A por convención, sin evolución (guard sin crash). 4. Los 6 records de una pelea comparten la referencia a `Turns` (se serializa por copia — mismo patrón que el 1v1 con 2 records); los records 3v3 crecen ~3× por TeamState (optimización futura ya flaggeada). 5. `CombatService.Clip()` eliminado (sin uso). 6. Review del orquestador sobre las 4 olas: sin bugs; un detalle de spec (DefaultLineup duplicado como constante local en DevConsole porque el del service es private — aceptado).

**Decisiones de diseño S37 (las 15) + ROADMAP de Fases 1-7: TODO en [[Index/13 - Combat Design Direction]]** (fuente canónica; no duplicar acá).

**Files Created (.cs — input ScriptNodes):**
- `Data/Combat/RoleTableSO.cs` (NUEVO): SO Odin de tuning de roles (RoleProfile por Role + GetProfile + Poblar v1).
- `Systems/Combat/CombatTargeting.cs` (NUEVO): estática de targeting determinista 3v3 (fila frontal viva, backline, aliado azar, menor HP).

**Files Touched (.cs — input ScriptNodes):**
- `Core/Enums.cs`: + Role, + CombatRow, + ModifierEffectKind.Shield=12, + CombatPopupKind.Shield.
- `Data/Genetics/CreatureDNA.cs`: + campo `Role` (metadata, no genetic string).
- `Core/CreatureGenerator.cs`: + `RandomRole()`.
- `Core/GameManager.cs`: mint asigna Role random.
- `Systems/Breeding/BreedingService.cs`: hijo hereda Role 50/50.
- `Data/Combat/CombatManagerSO.cs`: + `Roles` (RoleTableSO); DeathChance default 0.05.
- `Systems/Combat/Combatant.cs`: + Role/Row/Index/Shield/IsAlive.
- `Systems/Combat/CombatStats.cs`: sobrecarga GetEffectiveStats con capa de rol clampeada.
- `Data/Combat/CombatRecord.cs`: + SelfTeam/OpponentTeam/SelfTeamIds/OpponentTeamIds; CombatTurn + índices/DefenderShieldAfter/TeamStateA-B; + CombatUnitState; snapshot + Name/Role/Row.
- `Data/Combat/CombatResult.cs`: reshape a equipos (TeamAWon/Evolved*/Died*/TeamA/TeamB).
- `Data/Combat/CombatProcEvent.cs`: + TargetIndex.
- `Systems/Combat/CombatService.cs`: REFACTOR COMPLETO a equipos (ver bloque de arriba).
- `Systems/Combat/CombatResolver.cs`: Record captura TargetIndex.
- `Systems/Combat/CombatController.cs`: SimulateLocal por equipos + sobrecarga transicional 1v1.
- `Systems/Combat/AsyncCombatService.cs`: ApplyResult como equipos de 1 (transicional).
- `Systems/Combat/CombatDevConsole.cs`: box Combat 3v3 + Verify Determinism por equipos.
- `UI/CombatPanelUITK.Tabs.cs`: línea de resultado local adaptada al result de equipos.
- `Systems/CombatVisualizer/CombatReplayRequest.cs`: guard CanReplay para records 3v3.

**Files Touched (no-ScriptNode):** `ScriptableObjects/Abilitys/RoleTable.asset` (NUEVO, poblado v1), `ScriptableObjects/Abilitys/CombatManager.asset` (Roles + DeathChance 0.05), `MoriMonchiVault/Index/13 - Combat Design Direction.md` (v2: tesis completa + 15 decisiones + roadmap Fases 1-7).

**Next session (Fase 2 del roadmap — prioridad de Juan): UI DE POSICIONAMIENTO + FLUJO LOCAL COMPLETO.** Grilla hex 2-3-2 pre-pelea (elegir 3 MMs + colocarlos), lineup real a SimulateLocal, rediseño de la tab local del Combat Panel — todo el flujo local 3v3 desde UI sin dev console. Después: Fase 3 LIMPIEZA DE LEGACY (hard reset/migración de MMs antiguos, ítems que ya no proquean estados → sistema de N usos + regla de disparo, archivar recetas viejas de SynergyTable, retirar overload transicional). Roadmap completo de Fases 1-7 en [[Index/13 - Combat Design Direction]].

**ScriptNodes (cierre S37):** CREAR `RoleTableSO.md`, `CombatTargeting.md`; ACTUALIZAR `Enums.md`, `CreatureDNA.md`, `CreatureGenerator.md`, `GameManager.md`, `BreedingService.md`, `CombatManagerSO.md`, `Combatant.md`, `CombatStats.md`, `CombatRecord.md`, `CombatResult.md`, `CombatProcEvent.md`, `CombatService.md`, `CombatResolver.md`, `CombatController.md`, `AsyncCombatService.md`, `CombatDevConsole.md`, `CombatPanelUITK.Tabs.md`, `CombatReplayRequest.md`.

---

**Session:** 2026-07-10 (Session 36 — **ONBOARDING DE UNITY MCP + HIGIENE DE GAMESCENE + PlayerInputs 100% action-driven**. Sesión de tooling/infra, NO la de storytelling de combate que estaba planeada como "S36" — esa queda en cola.) — **🟢 todo verificado en editor/Play vía MCP**
**Focus:** Juan instaló un MCP de Unity (editor en vivo). La sesión exploró la capacidad y la aplicó: (1) validar qué puede hacer el MCP en ESTE proyecto (lectura de escena, escritura a diccionarios Odin, edición de jerarquía, ProBuilder, Play); (2) auditar y reorganizar GameScene; (3) documentar el MCP para futuras sesiones; (4) arreglar un detalle de arquitectura de inputs.

> ### ✅ Unity MCP validado + documentado
> Dos instancias conectadas (`RunRunSimulator` + `FasterTheBetter`) → **fijar `unity_instance` siempre**. **Quirk Odin probado con smoke test**: `manage_scriptable_object` NO ve los blobs `[OdinSerialize]` (diccionarios/listas polimórficas de las databases) — para escribirlos hay que usar `execute_code` con la API C# real (`db.Equipment[id]=so` + SetDirty + SaveAssets) o los botones Populate/Sync del SO; la entrada sobrevive un ForceReimport (Odin re-deserializa del blob). `execute_code` cae a compilador **codedom (C# 6)** porque el assembly Roslyn está roto (warning pre-existente) → usar solo C# 6 ahí. Todo en [[Index/12 - Unity MCP]] (NOTA NUEVA). Nuevo **paso 8 del protocolo** en CLAUDE.md: verificar en editor antes de declarar hecho.

> ### ✅ GameScene reorganizada (27 → 14 raíces)
> Grupos nuevos: `WORLD` (Props/Markers/Furniture), `TEMPLATES` (MorimonchiAgent+NpcAgent inactivos), `POOLS` (DefaultDamageNumberPo). 2 nav surfaces sueltos consolidados en `NAVMESH_SURFACES` (4→6). Renames: `ChasRegister`→`CashRegister` (typo), `PanelUI (3)`→`StoragePanel`. Borradas 2 `Sphere` de test. Reparent con `Undo.SetTransformParent` (posición mundial preservada, subárboles intactos). **Seguro porque el código no usa `GameObject.Find`-por-nombre** (única búsqueda textual: `FindGameObjectWithTag("Player")` en SpawnBallistics). Escena guardada.

> ### ✅ PlayerInputs 100% action-driven (fix de arquitectura)
> Estaban las 2 últimas lecturas directas de device en `Update()` (rueda→`HotbarScrolled`, Q→`DropPressed`) salteando el Input Actions asset. Se agregaron al mapa Player del asset `InputSystem_Actions` dos acciones: `Drop` (Button, `<Keyboard>/q`) y `HotbarStep` (PassThrough/Axis, `<Mouse>/scroll/y`); wrapper C# regenerado por reimport. `PlayerInputs.cs`: suscribe `Drop.performed`/`HotbarStep.performed`, **borra `Update()` y el campo `playerActive`** (el gating por foco ahora es automático: las acciones viven en el mapa que se deshabilita en menús). Comportamiento idéntico (wheel up=−1, umbral `scrollThreshold`). **Verificado en Play**: Drop→q, HotbarStep→scroll/y, mapa habilitado, 0 errores. Los otros 5 inputs (Move/Jump/Interact/Attack/Build) YA estaban bien. (Acciones vestigiales `Previous`/`Next` del template quedaron sin tocar, fuera de scope.)

> ### 📌 Corrección: anchors S21 ya estaban cerrados
> La memoria/creencia "IAnchorPlace sin probar en Play (test S22)" era stale — el Active Context confirma **CERRADO y probado en Play en S23** (línea ~760). El plan original de "verificar anchors" era redundante; se pivoteó a la higiene de escena. Memoria corregida.

**Files Created (.cs):** ninguno.

**Files Touched (.cs — input ScriptNodes):**
- `Player/PlayerInputs.cs`: rutea Drop/HotbarStep por el asset; borra `Update()` + campo `playerActive`; handlers `OnDrop`/`OnHotbarStep`; comentario de cabecera del input-map actualizado.

**Files Touched (no-ScriptNode):**
- `Assets/InputSystem_Actions.inputactions` (+ acciones `Drop`/`HotbarStep` + 2 bindings al mapa Player) → `Assets/InputSystem_Actions.cs` (wrapper) regenerado.
- `Assets/RunRunSimulator/Resources/Scenes/GameScene.unity` (reorg jerarquía, guardada).
- `CLAUDE.md` (fila MCP + paso 8 + "12 notas"), `MoriMonchiVault/00 - Index.md` (fila routing), `MoriMonchiVault/Index/12 - Unity MCP.md` (NUEVO).

> ### 🎨 DECISIONES DE DISEÑO (cierre S36, con Juan) — ver [[Index/13 - Combat Design Direction]]
> 1. **Combate = payoff autobattler estilo Pokémon Quest (3v3) + legibilidad Super Auto Pets.** Async determinista (S32) se queda. Cada MoriMonchi = UN rol legible; el equipo AMPLIFICA una característica para expresar ese rol (palanca del momento eureka). Sinergias se SIMPLIFICAN: de recetas opacas de stacks de elementos → sinergias de ROLES telegrafiadas. El motor (seed sim, leaves de efectos, pipeline de equipo, visualizer) SOBREVIVE y se retargetea; el contenido de elementos S35 queda EN REVISIÓN (repurpose/archivo). Lift grande = 1v1→3v3 (sim/records/matchmaking/visualizer). Pasada de diseño (set de roles + targeting) ANTES de tocar el sim.
> 2. **Furniture = colocación FIJA + expansiones compradas por un objeto `PC` (UI).** Retira el free-build (BuildModeController/PlacementGrid/BuildingInputs/BuildBrowserUITK); se apoya en `AnchorRegistry` (S21) para los anchors fijos de corrales. Falta decidir: ¿retira TODO el free-build o se conserva para decoración libre?
> 3. **Genética/equipo/sinergias confirmado como capa de agencia central** — no se toca, se le construye el payoff.
> Herramienta validada: ProBuilder autora el mobiliario fijo desde una sola geometría (silueta 2D + extrude). Grupo `PROBUILDER_EVAL` de muestra dejado en escena SIN guardar para evaluación de Juan.

**Decisiones abiertas (esperan a Juan):** (1) Furniture — ¿el placement fijo retira TODO el free-build o se conserva para decoración libre? (2) Próximo foco — ¿pasada de diseño de roles 3v3 o sistema de furniture fijo + PC? (3) `PROBUILDER_EVAL` — ¿conservar (guardar/prefabs) o descartar? (4) ¿Volcar diseño a Notion (autorización puntual)?

**Next session:** (diseño primero) cerrar la pasada de roles/targeting 3v3 de [[Index/13 - Combat Design Direction]] antes de tocar el sim; y/o el sistema de furniture fijo + PC. El plan viejo de "storytelling del combat visualizer" (ghost bar, banner, popups) sigue vigente pero ahora al servicio de la legibilidad SAP (un beat por vez). Diferido: volcar el diseño a Notion (autorización puntual de Juan). Tooling opcional: renombrar los 2 nav surfaces ALL-CAPS; revisar el tag `Player`.

**ScriptNodes (cierre S36):** ACTUALIZAR `PlayerInputs.md` (el nodo está desactualizado — lista eventos viejos como GrabPressed/HotbarSlotSelected/MouseDelta que ya no existen; los reales son MoveChanged/Jumped/InteractPressed/InteractReleased/ThrowPressed/BuildToggled/HotbarScrolled/DropPressed).

---

**Session:** 2026-07-03 (Session 35 — **TESTING S34 EN PLAY (todo 🟢) + SISTEMA DE ELEMENTOS Y ESTADOS EMERGENTES** (decisión de diseño grande con Juan): 6 elementos **Poison/Burn/Static/Pulse/Steel/Mist** aplicados por ítems como stacks; **Regen/Stun/Lifesteal dejan de ser efectos base → EMERGENTES** vía recetas de sinergia; stats dinámicos por stack (Steel +DEF / Mist +EVA / Static −SPD); record granular por proc + secuencia "stacks ×3 visibles → delay → ¡Sinergia! → chips caen"; chips rediseñados estilo Pokémon (código 3 letras + ×N debajo)) — **🟢 S34 probado por Juan (mochila/tarjetas/popups/replay OK) · 🟢 SIM DE ELEMENTOS PROBADO EN PLAY (post-cierre, mismo día: wiring + ítems + combate local — log verificado) · 🟡 queda: receta Regeneración (combo 1+2+3), replay visual (chips granulares + delay), Verify Determinism con recetas nuevas**
**Focus:** (1) pasada de Play de lo acumulado S33+S34 — todo verde; los dos "bugs" reportados de la barra de efectos resultaron no-bugs: el V×3 nunca visible era la Explosión tóxica quemando los stacks al instante (confirmado: popup "¡Sinergia!" aparecía), y el "desfase" era granularidad por turno (chips solo refrescaban al cierre); (2) diseño con Juan del sistema de elementos: nombres en inglés (elementos) / español (emergentes y sistema), elementos de ataque (Poison/Burn/Static-debuffer) vs soporte (Pulse cura / Steel +DEF / Mist +EVA), **el ítem decide el target** (leaf con campo Target); v1 con 3 recetas: Regeneración (PUL×3+STE×1), Cortocircuito (STA×2+MIS×1 sobre el rival), Robo de vida (PUL×2+MIS×1, % del daño vuelve como cura); (3) implementación en 3 fases (A sim/data, B contenido, C visualizer) + corrección a pedido de Juan: leaves por elemento en vez del genérico.

> ### ✅ Fase A — Sim/Data (determinismo intacto: cero rolls nuevos)
> `ModifierEffectKind` + `Static=7, Pulse=8, Steel=9, Mist=10, Lifesteal=11` (append-only, records viejos OK); `CombatPopupKind` espejado. `Combatant` gana **stats dinámicos**: `EffDefense/EffEvasion/EffSpeed` (base ± suma de `Magnitude` de stacks Steel/Mist/Static) + `LifestealPercent` (suma de Magnitude de stacks Lifesteal /100, cap 1) — `SimulateCore` usa `EffSpeed` en el orden de ronda y `TakeTurn` usa `EffEvasion`/`EffDefense` en las fórmulas. **Hook Lifesteal** post-strike (tras `BeforeStrike = false`, antes de armed procs): cura `daño × %` al atacante + `Record(Lifesteal)`. `TickStatuses`: Pulse comparte el case de cura con Regen (log `[{a.Kind}]`); Static/Steel/Mist/Lifesteal solo expiran (decremento común). **Record granular**: `CombatProcEvent` + `TargetStatusAfter` (`List<CombatStatusMark>`, null = record viejo) — `CombatResolver.Record` lo captura SIEMPRE; `StatusMarks(Combatant)` se movió de CombatService a **`CombatResolver` público estático**. En `CheckSynergies` el `Record(Synergy)` se movió a DESPUÉS de `ConsumeStacks` → el evento de detonación viaja con marks post-quema (el evento del stack que completa la receta viaja con los marks llenos, p.ej. Poison×3).

> ### ✅ Fase B — Contenido
> `CombatPopupPaletteSO` "Seteo base" + 5 colores (Static celeste eléctrico / Pulse rosa / Steel gris / Mist celeste pálido / Lifesteal carmesí). `CombatDamageNumbers.Label` + 5 (elementos en inglés: "Static"/"Pulse"/"Steel"/"Mist"; "Robo de vida" en español; **Poison/Burn siguen "Veneno"/"Quemadura" — decisión abierta**). `CombatVisualizerService.ProcPopupKind` + 5 mapeos. `SynergyTableSO` gana botón **"Recetas v1 (elementos)"**: Regeneración (Regen 4/turno ×3t), Cortocircuito (`SynergyStunEffect` 1t — respeta anti-permastun), Robo de vida (`SynergyStatusEffect` Lifesteal Magnitude=30 → 30%). Los leaves de sinergia existentes alcanzaron — cero engine nuevo.

> ### ✅ Fase C — Visualizer (la secuencia pedida por Juan)
> `CombatVisualizerService`: knob `synergyPopupDelay` (0.6s, `/Speed`) — `PlayProc` espera ese delay ANTES del popup si `Kind == Synergy`; tras popup+PushHp, si `pe.TargetStatusAfter != null` → `PushStatusSide(side, marks)` (helper nuevo) → **chips actualizados POR PROC** (se ve el ×3 formarse; al popup de sinergia los chips caen porque el evento trae marks post-quema). El `PushStatus(node)` por turno en ForwardRoutine/Restore quedó intacto (corrige drift + cubre records viejos con `TargetStatusAfter` null). `MoriMonchiCombatVisualizerUITK`: chip = mini-badge 2 filas (código 3 letras arriba coloreado por paleta, "×N" debajo si Stacks>1); `StatusText`/`StatusInitial` reemplazados por `StatusCode` (POI/BUR/STA/PUL/STE/MIS · REG/ATU/ESP/CUR/ROB); `MapKind` + 5 kinds.

> ### ✅ Leaves por elemento (corrección de Juan a mitad de sesión)
> El primer intento fue un leaf genérico `ElementStackEffect` (dropdown de elemento + target) — Juan pidió conformar al patrón del archivo (un leaf nombrado por efecto). Se ELIMINÓ el genérico (mismo día, cero assets lo referenciaban) y `CombatProcEffect.cs` ganó enum `ProcTarget {Opponent, Self}` a nivel de archivo + 4 leaves planos con Target default en su lado natural: `StaticEffect` (Opponent, −SPD), `PulseEffect` (Self, HP/turno), `SteelEffect` (Self, +DEF), `MistEffect` (Self, +EVA). Para Cortocircuito: ítem MistEffect con Target=Opponent. Los leaves viejos (Stun/Regen/etc.) quedan intactos — **el stun base muere por contenido** (re-autoría de ítems), no por código.

> ### 🟢 Testeo post-cierre (mismo día — Juan hizo el wiring completo + ítems + combate local)
> Log verificado por el orquestador, todo correcto con cuentas al decimal: **Robo de vida** detona (PUL×2+MIS×1) y el lifesteal cura 30% del daño (y **60% con 2 stacks vivos** — stacking de % OK, expiración OK); **Cortocircuito** detona 3 veces (Spray con Magnitude 0 = combustible puro, "Mist (0/turn)") con ciclo stun→skip→inmunidad→re-stun sin permastun; **EVA dinámica** visible (10%→20% con stacks de Mist, DODGE incluido); **DEF dinámica** correcta incluso en expiración (daño 9,0→8,3 = exactamente 1 stack de Steel vivo); **Pulse HoT** tickea; quema FIFO exacta (sobrantes quedan, Mist llegó a ×4); KO/evolución/record intactos. NO ejercitado aún: receta **Regeneración** (nadie juntó PUL×3+STE×1 en el mismo portador), el **replay visual** (chips granulares + delay ×3→¡Sinergia!) y re-correr **Verify Determinism** con las recetas asignadas.

> ### ⚠️ WIRING S36 (lo que queda)
> 1. ~~Recompilar~~ ✅ 2. **"Verify Determinism (seed)"** con las recetas nuevas asignadas. 3. ~~Paleta "Seteo base"~~ ✅ 4. ~~"Recetas v1 (elementos)"~~ ✅ 5. Knob `synergyPopupDelay` (ajustar a gusto viendo el replay). 6. ~~Autorar ítems~~ ✅ — falta la pelea de **Regeneración** (combo 1+2+3 en un MM) y mirar el **replay** de un combate con sinergias.

> ### 🧪 Plan de testeo — 6 equipos (2 por slot, todos Proc 100%)
> | # | Ítem | Slot | Effect | Trigger | Target | Turns | Mag |
> |---|------|------|--------|---------|--------|-------|-----|
> | 1 | Latido de Peluche | Arma | Pulse | Passive | Self | 3 | 2 |
> | 2 | Amuleto de Pulso | Amuleto | Pulse | Defensive | Self | 3 | 2 |
> | 3 | Caparazón de Lata | Armadura | Steel | Defensive | Self | 3 | 1 |
> | 4 | Cable Pelado | Arma | Static | Offensive | Opponent | 3 | 1 |
> | 5 | Spray Empañador | Amuleto | Mist | Offensive | **Opponent** | 3 | **0** (combustible puro; con 1 le sube EVA al rival = tradeoff) |
> | 6 | Chaleco de Pelusa | Armadura | Mist | Defensive | Self | 3 | 1 |
>
> Peleas: **Regeneración** = 1+2+3 · **Cortocircuito** = 4+5+6 (mirar flip de orden de turnos por −SPD en el log "first:") · **Robo de vida** = 1+2+6 (la barra propia sube al pegar). Si un MM junta ingredientes de 2 recetas gana la primera en la lista de Rules.

> ### 📋 Decisiones abiertas / quirks
> 1. Labels de popup Poison/Burn siguen "Veneno"/"Quemadura" mientras chips dicen POI/BUR — ¿unificar a inglés? 2. Magnitude del Spray (0 vs 1, ver tabla). 3. Static debuffea SPD (alternativa −EVA como espejo de Mist — tuning). 4. Records ahora cargan `TargetStatusAfter` por proc → crecen (junto al CombatHistory en snapshots, optimización futura si pega el límite de Cloud Save). 5. Diseño de sinergias sigue ABIERTO por decisión de Juan: v1 solo Regen/Stun/Lifesteal (Zarza/Vapor y recetas de Burn quedaron en el tintero). 6. Review del orquestador: sin bugs de sub-agentes esta vez; solo se alineó un `MinValue(0f)` faltante. 7. Cerrado como no-bug: V×3 invisible = sinergia quemando stacks; "desfase" de chips = granularidad por turno (resuelto con chips por proc).

**Files Created (.cs):** ninguno.

**Files Touched (.cs — input ScriptNodes):**
- `Core/Enums.cs`: `ModifierEffectKind` +5 (Static/Pulse/Steel/Mist/Lifesteal, 7-11); `CombatPopupKind` +5.
- `Data/Combat/CombatProcEvent.cs`: + `TargetStatusAfter` (marks del objetivo al momento del proc; null = viejo).
- `Systems/Combat/Combatant.cs`: + `EffDefense/EffEvasion/EffSpeed/LifestealPercent` + `StackSum` (stats dinámicos por stack).
- `Systems/Combat/CombatResolver.cs`: `StatusMarks` movido acá (público estático); `Record` captura `TargetStatusAfter`; `Record(Synergy)` post-`ConsumeStacks`.
- `Systems/Combat/CombatService.cs`: EffSpeed en orden de ronda; EffEvasion/EffDefense en fórmulas; hook Lifesteal post-strike; tick de Pulse (case compartido con Regen); `StatusMarks` privado eliminado.
- `Data/Equipment/CombatProcEffect.cs`: + enum `ProcTarget` + leaves `StaticEffect`/`PulseEffect`/`SteelEffect`/`MistEffect` (campo Target, default lado natural); `ElementStackEffect` creado y eliminado en la misma sesión.
- `Data/Combat/CombatPopupPaletteSO.cs`: +5 colores en "Seteo base".
- `Data/Combat/SynergyTableSO.cs`: + botón "Recetas v1 (elementos)" (Regeneración/Cortocircuito/Robo de vida).
- `Systems/CombatVisualizer/CombatDamageNumbers.cs`: +5 labels.
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: +5 mapeos `ProcPopupKind`; knob `synergyPopupDelay`; `PlayProc` con delay pre-popup de sinergia + push granular de marks; helper `PushStatusSide`.
- `UI/MoriMonchiCombatVisualizerUITK.cs`: chip 2 filas (código+contador); `StatusCode` reemplaza `StatusText`/`StatusInitial`; `MapKind` +5.

**Files Touched (no-ScriptNode):** assets de equipment (Juan: `Equipment1.asset` borrado, carpeta `MainWeapon/` nueva).

> ### 🎯 FOCO S36 decidido al cierre — STORYTELLING DEL COMBATE (feedback de Juan con screenshot del replay)
> Diagnóstico de Juan: el replay funciona pero "no transmite lo que está sucediendo" — el objetivo es que **alguien que no conoce el juego entienda la escena** y que cada combate sea memorable. Frentes concretos:
> 1. **Barra de vida estilo juego de pelea (ghost bar)**: al recibir daño, la barra baja al instante pero deja un "trozo fantasma" marcado que se drena con delay (patrón Street Fighter). El trozo se TIÑE por la fuente del daño: golpe = rojo, Poison = morado, Burn = naranja; curas al revés (el trozo que se rellena en verde/rosa de Pulse). Implementación: segundo fill detrás del principal en `MoriMonchiCombatVisualizerUITK` + `SetHp` gana la fuente (`CombatPopupKind`) — el service ya sabe el kind en cada `PushHp`/`PlayProc`.
> 2. **Popups más duraderos y expresivos**: aparecen en la posición correcta pero desaparecen rápido — más duración, más color, más texto. Vía `prefabOverrides` existente (prefabs DNP por familia con lifetime/fade mayores) + tuning de `impactSeconds`/`betweenTurnsSeconds` para dar aire a cada beat.
> 3. **Paleta**: Poison pasa a MORADO (pedido explícito; ojo con diferenciarlo del violeta de Synergy — decidir tonos en el asset).
> 4. **Banner de eventos clave** (candidato): anuncio grande en pantalla para los beats — "¡Sinergia: Robo de vida!", "¡CRÍTICO!", "K.O.", stun — driven por el bus visual existente (`CombatVisualEvents`); el log de cartas queda como registro, el banner como drama. Log más narrativo ("Clammy se cura 2 con Pulse" en vez de "— Pulse").
> 5. **Fixes de layout vistos en el screenshot**: nombre del peleador cortado en el header (overflow del UXML) y chip de efectos (STE×2) flotando despegado del panel izquierdo — la fila `effects` programática cae fuera del contenedor en `CombatHpBar.uxml` (agregar la fila al UXML o anclarla al contenedor correcto).

**Next session (S36) — storytelling del combate + cierre de testeo de elementos:**
1. **Storytelling** (bloque de arriba): ghost bar teñida por fuente → popups duraderos → paleta Poison morado → banner de eventos → fixes de layout (nombre cortado + chip flotante).
2. Cierre de testeo de elementos: receta **Regeneración** (combo 1+2+3), replay visual de un combate con sinergias (chips granulares + delay ×3→¡Sinergia!, calibrar `synergyPopupDelay`), re-correr **Verify Determinism** con recetas asignadas.
3. Re-autoría de contenido: convertir ítems viejos de Stun/Regen a elementos (el stun base desaparece por contenido).
4. Decisiones abiertas: labels Poison/Burn, Magnitude del Spray, Static −SPD vs −EVA. Tuning de magnitudes/duraciones/recetas con data real. UI de recetas para el jugador.
5. Diferidos: test online async end-to-end, refactor composición Fases 6-9, retirar `EvolutionChance`.

**ScriptNodes (cierre S35):** ACTUALIZAR `Enums.md`, `CombatProcEvent.md`, `Combatant.md`, `CombatResolver.md`, `CombatService.md`, `CombatProcEffect.md`, `CombatPopupPaletteSO.md`, `SynergyTableSO.md`, `CombatDamageNumbers.md`, `CombatVisualizerService.md`, `MoriMonchiCombatVisualizerUITK.md`.

---

**Session:** 2026-07-02 (Session 34 — **PERFILADO DEL COMBAT VISUALIZER**: juice de popups DNP (follow-transform + presets por tipo + "+" en curas + escala en críticos + posición baja) + **efectos activos en la barra HP** (iniciales V/Q/R/A/E×N desde snapshot por turno del sim) + **REPLAY CROSS-ESCENA** (`CombatReplayRequest` NUEVO: botón ▶ en tarjeta del detail + "▶ Ver replay" en historial del Combat Panel → flush → carga `CombatVisualizerMM` → Play → Volver ya existía) + **tarjeta de combate rediseñada** (swatch color genético + tiers + dueño + comentario) + fix duplicado `devNoConsume` mochila) — **🔴 TODO SIN PROBAR EN PLAY · WIRING DIFERIDO A S35 (emergencia de Juan, cierre abrupto)**
**Focus:** Plan S34 decidido al cierre de S33 + feedback de Juan al abrir: (1) popups salían muy arriba y sin variedad → follow al MM, offset bajo, presets por tipo, "+" en curas, escala en críticos; (2) barra de vida extendida con los efectos activos del momento (iniciales); (3) conexión de escenas para reproducir un combate desde el record (tarjeta e historial) con vuelta; (4) tarjeta de combate más simple según imagen de referencia (mini-imagen color, stats comprimidas, partes mejoradas, dueño, comentario, ▶); (5) fix del duplicado de equipo en modo dev.

> ### ✅ Snapshots aditivos del sim (cero RNG, backward-compatible)
> `CombatTurn` gana `StatusA/StatusB` (`List<CombatStatusMark> {Kind, Stacks}` — estado de efectos de ambos lados SIM al cierre de cada turno; `EmitTurn` los puebla vía `StatusMarks(Combatant)`: conteo de `Active` por Kind en orden de enum + mark `Stun` con `StunTurns` al final). `CombatFighterSnapshot` gana `BodyTier/ArmTier/EyeTier/MouthTier` (int; 0 = snapshot pre-S34, `Tier1 = 1`) y `ColorHex` (RRGGBB del `BaseColor`, "" = viejo) — `Snapshot(Combatant)` los setea (pre-combate, correcto). Orden de rolls intacto; puro output.

> ### ✅ Popups — `CombatDamageNumbers` (DamageNumbersPro)
> `CombatVisualPopup` gana `Transform Follow` (el service lo setea en los 4 raises vía `FighterTransform(side)`) → `SetFollowedTarget`. `prefabOverrides` (`List<KindPrefabOverride>` serializada) para presets DNP distintos por `CombatPopupKind`. "+" en Heal/Regen (**quirk DNP: exige `enableLeftText = true` — los overloads numéricos de `Spawn` no lo activan** — + `UpdateText()` idempotente por el reuso de pool). `critScale` (knob, default 1.35) vía `SetScale`. Default de `spawnOffset` bajado a (0, 0.6, 0) — **el valor serializado en escena lo pisa: ajustar a mano en el wiring**.

> ### ✅ Barra HP con efectos activos — `MoriMonchiCombatVisualizerUITK`
> Nueva API `SetStatus(List<CombatStatusMark>)` (driven por el service como `SetHp`). Fila `effects` creada programáticamente si el UXML no la trae (sin tocar `CombatHpBar.uxml`); chips con iniciales V(eneno)/Q(uemadura)/R(egen)/A(turdido)/E(spinas) + "×N", color por `palette` opcional (`CombatPopupPaletteSO`, fallback blanco). Patrón dirty + re-render al rebuild del árbol (mismo seam que el fix de árbol huérfano). El service: `CombatNode` gana `StatusA/StatusB` (lado VISUAL, mapeados con `SelfWasA`, null-tolerantes) y `PushStatus(node)` corre en `Restore` y en `ForwardRoutine` tras `current = target`.

> ### ✅ Replay cross-escena — `CombatReplayRequest` (NUEVO) + `CombatSceneManager` + `GameManager`
> Estática: `Request(self, rec)` guarda `SelfId`/`FightIndex`, llama `GameManager.FlushForSceneChange()` (**método público nuevo** = patrón quit: `CollectLooseWorldProps` + `FlushToCloud`) y `LoadScene("CombatVisualizerMM")`. `CanReplay`/`ResolveOpponent` (por `OpponentDnaId` → fallback `CustomName`, patrón del DEV harness). `CombatSceneManager.Start` consume el request con corrutina tolerante (**GameManager NO persiste entre escenas — la escena de combate tiene el suyo que carga de disco**; espera Instance/Registry + TryGet con timeout 3s) → `CombatVisualizerService.Play`. La vuelta (`btn-home`) ya existía. **Rivales async fuera del registry → botón deshabilitado (v1)**; guardar el DNA string del rival en el record queda como decisión futura.

> ### ✅ Tarjeta de combate rediseñada (detail) + botón en historial (Combat Panel)
> `BuildCombatCard(dna, rec)` (ya no static): header igual (badge+fecha); "vs {rival} · de {jugador}|local"; cuerpo 2 columnas Tú/Rival con **swatch 28×28** (`ColorHex` del snapshot → fallback `dna.BaseColor`/gris), stats en 2 líneas compactas (HP·ATK·SPD / DEF·LCK·EVA), **chips de tiers >1** ("Brazo T2"); línea de comentario ("Se mejoró {parte}" / "Victoria — sin mejora" / "Regresó derrotado" / "Murió en combate" / "Empate — sin consecuencias"); **▶ abajo-derecha** (`SetEnabled(CanReplay)`). Newest first. `BuildStatsColumn`/`AddCombatStatLabel` eliminados (verificado sin refs); USS `combat-card__*` reescrito (chips viejos eliminados, + swatch/body/colstats/comment/footer/play). Combat Panel tab 4: `ShowHistory` gana botón lazy "▶ Ver replay" (`histReplayBtn`/`histCurrent` en el partial core; `.cbt-replay-btn` en su USS; NO participa de la navegación por teclado).

> ### ✅ Fix mochila (modo dev, decidido con Juan: SIMÉTRICO)
> Bug: `devNoConsume` guardaba el consumo al equipar pero el desequipar (celda None) devolvía el item incondicional → duplicado. Fix: la devolución + `InventoryChanged` del desequipar quedaron bajo `if (!devNoConsume)`; `RegistryChanged` + `Rebuild` siempre corren. Swap al equipar ya estaba correcto; moves internos de grilla no tocan DNA. (El botón "vaciar inventario" pedido ya existía: "Clear Equipment" del box Equipment (DEV) — Juan no lo había visto.)

> ### ⚠️ WIRING S35 (Juan — NADA de S34 probado en Play)
> 1. **Build Settings: agregar la escena `CombatVisualizerMM`** (y confirmar `GameScene`) — sin esto `LoadScene` falla. 2. Recompilar (`CombatReplayRequest.cs` nuevo sin .meta). 3. `CombatDamageNumbers` (escena de combate): **bajar `spawnOffset` serializado a (0, 0.6, 0)**; opcional `prefabOverrides` por tipo + `critScale`. 4. Prefab del peleador → `MoriMonchiCombatVisualizerUITK`: asignar `palette` (CombatPopupPalette.asset) para iniciales coloreadas.

> ### 📋 Quirks / notas de la sesión
> 1. Records pre-S34: barra sin chips (turnos sin Status), swatch fallback y sin chips de tier; los de S33 tienen stats pero tiers=0/ColorHex="" → la UI los trata como desconocidos. 2. `FlushForSceneChange` es best-effort en el push (igual que quit). 3. Review del orquestador: sin bugs cross-archivo esta vez; solo se removió un comentario explicativo (regla 3). 4. Pendiente de S31 sigue abierto: decidir "Aturdido" en 1 o 2 momentos (se decide viéndolo en Play).

**Files Created (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatReplayRequest.cs` (NUEVO): servicio estático de replay cross-escena (Request/CanReplay/ResolveOpponent/Clear).

**Files Touched (.cs — input ScriptNodes; el working tree arrastra ADEMÁS los de S33 sin commitear — DevToolsConsole, PlayerInventorySO, CreatureGridUITK y parte de EquipmentBackpackUITK/MorimonchiDetailInfoUITK):**
- `Data/Combat/CombatRecord.cs`: + `CombatStatusMark`; `CombatTurn` + `StatusA/StatusB`; `CombatFighterSnapshot` + 4 tiers + `ColorHex`.
- `Systems/Combat/CombatService.cs`: `Snapshot` extendido (tiers+color); + `StatusMarks(Combatant)`; `EmitTurn` puebla StatusA/B. Cero RNG.
- `Systems/CombatVisualizer/CombatVisualEvents.cs`: `CombatVisualPopup` + `Transform Follow`.
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: + `FighterTransform`; Follow en los 4 popups; `CombatNode` + StatusA/B (mapeo SelfWasA); + `PushStatus` (Restore + ForwardRoutine).
- `Systems/CombatVisualizer/CombatDamageNumbers.cs`: + `prefabOverrides`/`ResolvePrefab`; + follow; + "+" curas (`enableLeftText`+`UpdateText`); + `critScale`; offset default 0.6.
- `Systems/CombatVisualizer/CombatSceneManager.cs`: + corrutina `ConsumeReplayRequest` (espera GameManager/registry, resuelve y Play).
- `Core/GameManager.cs`: + `FlushForSceneChange()` público.
- `UI/MoriMonchiCombatVisualizerUITK.cs`: + `SetStatus` + fila `effects` programática + `palette` opcional + helpers iniciales/mapeo.
- `UI/MorimonchiDetailInfoUITK.cs`: tab Combate rediseñada (`BuildCombatCard(dna, rec)`, `BuildCombatColumn`, `SnapshotColor`, `AddTierChips`, `CommentText`, `PartEs`, botón ▶; `BuildStatsColumn`/`AddCombatStatLabel` eliminados).
- `UI/CombatPanelUITK.cs`: + campos `histReplayBtn`/`histCurrent`.
- `UI/CombatPanelUITK.Tabs.cs`: `ShowHistory` + botón lazy "▶ Ver replay".
- `UI/EquipmentBackpackUITK.cs`: fix devNoConsume simétrico en el desequipar.

**Files Touched (no-ScriptNode):** `UI Toolkit/CombatPanelUITKStyle.uss` (+`.cbt-replay-btn`); `UI Toolkit/MorimonchiDetailInfoUITKStyle.uss` (bloque `combat-card__*` reescrito).

**Next session (S35) — wiring + UNA pasada de Play con todo lo acumulado:**
1. Wiring de arriba (Build Settings es bloqueante) y probar: mochila rework S33 (None / drop en huecos / swap / página en drag / sin duplicados dev) + tarjetas nuevas (pide combate nuevo para swatch/tiers) + popups (posición/follow/"+"/escala/presets) + barra de efectos + replay ida-vuelta desde tarjeta e historial.
2. Decisión S31 pendiente: "Aturdido" en 1 o 2 momentos; validar popup "¡Sinergia!" en replay.
3. Juice restante: hooks Feel/MMF reales (`MMF_Players` vía `CombatVisualHooks`).
4. Diferidos: test online async end-to-end, tuning/recetas multi-elemento + UI de recetas, refactor composición Fases 6-9, retirar `EvolutionChance`, ¿DNA string del rival en el record para replay async completo?

**ScriptNodes (cierre S34):** CREAR `CombatReplayRequest.md`; ACTUALIZAR `CombatRecord.md`, `CombatService.md`, `CombatVisualEvents.md`, `CombatVisualizerService.md`, `CombatDamageNumbers.md`, `CombatSceneManager.md`, `GameManager.md`, `MoriMonchiCombatVisualizerUITK.md`, `MorimonchiDetailInfoUITK.md`, `CombatPanelUITK.md`, `CombatPanelUITK.Tabs.md`, `EquipmentBackpackUITK.md`.

---

**Session:** 2026-07-02 (Session 33 — **Sinergias PROBADAS en Play + deploy JS hecho** (cierre de la 2ª mitad de S32) + **TARJETAS de historial de combate** (snapshot de stats en el record) + **MOCHILA DE EQUIPO** (`EquipmentBackpackUITK` + inventario de equipo free-placement en `PlayerInventorySO`)) — **🟢 sinergias / determinismo-con-tabla / deploy OK (Juan) · 🟢 mochila v1 probada por Juan → rework por feedback (huecos+None) 🟡 a probar · 🟡 tarjetas a probar (pide combate nuevo) · test online DIFERIDO por decisión de Juan**
**Focus:** (1) cerrar la 2ª mitad de S32: wiring de sinergias, test en Play, re-verificación de determinismo con tabla, deploy de los 2 JS (dashboard); (2) dos features de accesibilidad pedidos por Juan: historial de combate legible (tarjetas con las stats de AMBOS peleadores al momento de la pelea) y equipar sin drag-drop de editor (mochila UI desde la tab Equipo del detail panel); (3) decisión de UX tras probar la v1: el inventario de equipo pasa de lista compacta a **grilla por slot con huecos (free placement)**.

> ### ✅ PROBADO por Juan (1ª mitad — cierre S32)
> Explosión tóxica detona en Play (`[synergy] ¡Explosión tóxica!`, quema FIFO de 3 stacks, daño aplicado), fix de barras/stats del visualizer con equipo OK, **"Verify Determinism (seed)" → OK con la tabla de sinergias asignada**, y **deploy de `run-combat.js` + `process-matchmaking.js` hecho vía dashboard**. El test online async end-to-end queda DIFERIDO (decisión Juan: primero completar el flujo local).

> ### ✅ Tarjetas de historial de combate (data + UI)
> **Data (aditivo, backward-compatible):** nueva clase serializable `CombatFighterSnapshot` (MaxHp/Attack/Speed/Defense/Luck/Evasion, vive en CombatRecord.cs junto a CombatTurn). `CombatResult` gana `StatsA`/`StatsB` — `SimulateCore` los setea desde los `Combatant` recién construidos (stats efectivos CON equipo; puro output, **cero consumo de RNG** → determinismo intacto). `CombatRecord` gana `SelfStats`/`OpponentStats` (`BuildRecord` copia por POV; **null = record viejo**, la UI lo tolera). **UI:** tab Combate de `MorimonchiDetailInfoUITK` reescrita — los foldouts con turnos se reemplazan por tarjetas (badge Victoria/Derrota/Empate coloreado, fecha, "vs rival" (+jugador si async), chips "evolucionó X"/"murió", dos columnas Tú/Rival con las 6 stats; records viejos → "Combate antiguo — sin stats registradas"). El detalle turno-a-turno se retiró de la tab (el replay vive en el Combat Visualizer). USS: bloque `combat-fold/meta/turn` eliminado (verificado sin otros usos), set `combat-card__*` nuevo.

> ### ✅ Inventario de equipo del jugador (PlayerInventorySO, 3er namespace)
> **NO se creó SO nuevo** (decisión: `PlayerInventorySO` ya es el inventario único con furniture "F#" + world props "I#" y su pipeline `OnInventoryChanged` → GameManager → `SaveSystem.SaveInventory` local). Nuevo namespace `equipmentGrids`: `Dictionary<EquipmentSlot, List<string>>` — **grilla por slot con huecos** (entrada null/empty = celda vacía, índice de lista = índice de celda; free placement pedido por Juan tras probar la v1 compacta donde soltar en celda vacía "no hacía nada"). API: `AddEquipment(slot, id)` (primer hueco o append), `RemoveEquipmentAt(slot, i)` (deja hueco, no desplaza, trim de nulls finales), `MoveEquipment(slot, from, to)` (a hueco = mueve, a ocupada = SWAP, pad con nulls si `to` excede), `GetEquipment(slot)` (puede contener nulls), `ClearEquipmentOwned()`. Persistencia: `InventoryData.EquipmentGrids` (campo JSON **nuevo a propósito** — los `inventory.json` de la v1 del mismo día se ignoran sin romper; mochila arranca vacía, se repuebla con dev). El SO NO dispara eventos: el caller dispara `GameEvents.InventoryChanged`. **DevToolsConsole** gana box "Equipment (DEV)": Add Equipment Item / Add Full Equipment Catalog / Clear Equipment (refs: `devEquipmentItem` + `equipmentDatabase`).

> ### ✅ Mochila de equipo (`EquipmentBackpackUITK` NUEVO + su .uss propio)
> Popup estilo tooltip **anclado a la equip-card clickeada en la tab Equipo del detail panel** (corrección de Juan a mitad de sesión: NO en las cartas del grid — ahí los 3 íconos de slots quedaron **display-only**, cerrando de paso el pendiente S27 de ver el equipo en las cartas; click en ícono del grid abre el detail normal). API: `Open(dna, slot, anchor, registry)` / `Close()` — construye su VisualElement en el root del panel del anchor (funciona en cualquier UIDocument), stylesheet serializada attacheada al popup Y al ghost. Header solo con el nombre del slot (sin DEV/Quitar/✕ — feedback Juan). Grilla 3×3 por página, pestañas si hace falta (siempre ≥1 celda libre al final: pageCount con +2). **Celda 0 = "None"**: click desequipa (el item vuelve al primer hueco). Click en item = equipar: `dna.Equipped[slot] = id` + consumo `RemoveEquipmentAt` + swap-back del anterior con `AddEquipment` (cae exactamente en la celda recién vaciada) → `InventoryChanged` + `RegistryChanged` + Rebuild (queda abierto). **Drag & drop free placement**: drop en hueco = el item queda en ESA celda; drop sobre ocupada = intercambio; drop en None/fuera = cancela; arrastrar sobre una pestaña cambia de página (**hit-test manual en PointerMove — con pointer capture los PointerEnter de las pestañas NO disparan**, bug cazado en review). Cierra con click afuera (PointerDown TrickleDown en el root). `devNoConsume` serializado (inspector, sección Dev): equipar no consume el item — modo testing n-copias pedido por Juan. **MorimonchiDetailInfoUITK** ahora también escucha `GameEvents.OnRegistryChanged` → re-`Populate(current)` (al equipar se refrescan la card del slot y las stats Base→Final sin cerrar el panel; suscripción simétrica OnEnable/OnDisable) y su X cierra el popup.

> ### 📌 Quirks / decisiones de la sesión
> 1. `EffectiveStats` (readonly struct con ctor posicional) NO sirve para (de)serializar en el record con Newtonsoft → DTO propio `CombatFighterSnapshot` (clase → null gratis como marcador de record viejo). 2. Dos bugs cazados en review del orquestador sobre el trabajo de sub-agentes: ghost del drag sin stylesheet (los `styleSheets` attacheados al popup no aplican a un hijo de root) y cambio de página por PointerEnter muerto bajo captura de pointer. 3. Las cartas del grid ganaron ~24px de fila de equipo → `cardSize` (knob inspector) puede necesitar ~120×175. 4. Juan commiteó a mitad de sesión (`5d922c3 Update`, incluye la v1); el rework (huecos+None+pulido) quedó en working tree al cierre.

> ### ⚠️ WIRING (Juan — ya hecho en Play salvo recompilar el rework)
> Componente `EquipmentBackpackUITK` en el objeto UIManager: `inventory` (PlayerInventory.asset) + `equipmentDatabase` + `equipmentPalette` + `styleSheet` (`EquipmentBackpackUITK.uss`). Ref `backpack` en `MorimonchiDetailInfoUITK` (campo Data). `equipmentDatabase` (+`devEquipmentItem` opcional) en `DevToolsConsole`. Tras el rework la mochila arranca vacía (campo JSON renombrado) → "Add Full Equipment Catalog (DEV)".

**Files Created (.cs — input ScriptNodes):**
- `UI/EquipmentBackpackUITK.cs` (NUEVO): popup mochila — grilla 3×3 free-placement por slot, celda None, drag&drop, equipar/swap, devNoConsume.

**Files Touched (.cs — input ScriptNodes):**
- `Data/Combat/CombatRecord.cs`: + clase `CombatFighterSnapshot` + campos `SelfStats`/`OpponentStats` (null = record viejo).
- `Data/Combat/CombatResult.cs`: + `StatsA`/`StatsB` (CombatFighterSnapshot).
- `Systems/Combat/CombatService.cs`: `SimulateCore` setea StatsA/B (helper `Snapshot(Combatant)`); `BuildRecord` copia por POV. Cero cambio en orden de rolls.
- `UI/MorimonchiDetailInfoUITK.cs`: tab Combate → tarjetas (`BuildCombatCard`/`BuildStatsColumn`/`BadgeText`; `OutcomeShort` eliminado); + ref `backpack`, campo `current`, suscripción `OnRegistryChanged` → re-Populate, click en equip-card abre mochila, X cierra popup.
- `Data/Player/PlayerInventorySO.cs`: 3er namespace `equipmentGrids` (dict por slot con huecos) + API nueva + `InventoryData.EquipmentGrids` (deep copy en GetData/LoadFrom).
- `Core/DevToolsConsole.cs`: box "Equipment (DEV)" — add item / add full catalog / clear (API slot-aware).
- `UI/CreatureGridUITK.cs`: cartas con fila de 3 íconos de equipo display-only (`BindEquipSlot`; refs `equipmentDatabase`/`equipmentPalette`; sin ref a backpack).

**Files Touched (no-ScriptNode):** `UI Toolkit/EquipmentBackpackUITK.uss` (NUEVO); `UI Toolkit/CreatureCardUITK.uxml` (+equip-row); `UI Toolkit/CreatureGridUITKStyle.uss` (+card__equip-*); `UI Toolkit/MorimonchiDetailInfoUITKStyle.uss` (combat-fold→combat-card); escena/assets (wiring Juan: componente backpack, refs, CombatManager movido a Abilitys/, equipment assets).

**Next session (S34) — decidido con Juan: PERFILAR EL COMBAT VISUALIZER MM (el test online espera al flujo local completo):**
1. Probar en Play lo pendiente de hoy: rework mochila (None / drop en huecos / swap / cambio de página en drag) + tarjetas de historial (pide un combate nuevo para ver stats; records viejos → placeholder).
2. **Foco visualizer:** probar Etapa 3b pendiente de S31 (floaters con etiquetas + stun; decidir si "Aturdido" queda en 1 o 2 momentos) + validar el popup violeta "¡Sinergia!" en el replay.
3. Juice: "+" en curas, escala en críticos, follow-transform del popup, hooks Feel/MMF reales (MMF_Players).
4. Candidato natural: botón "Replay" por tarjeta en la tab Combate (`CombatVisualizerService.Play`; `Seed`/`OpponentDnaId` ya viven en el record desde S32).
5. Diferidos: test online async end-to-end (necesita 2º jugador en pool), tuning/recetas multi-elemento de sinergias + UI de recetas, refactor composición Fase 6/7, retirar `EvolutionChance`.

**ScriptNodes (cierre S33):** CREAR `EquipmentBackpackUITK.md`; ACTUALIZAR `CombatRecord.md`, `CombatResult.md`, `CombatService.md`, `MorimonchiDetailInfoUITK.md`, `PlayerInventorySO.md`, `DevToolsConsole.md`, `CreatureGridUITK.md`.

---

**Session:** 2026-07-02 (Session 32 — **Seed determinista local+online** (retiro del combate JS) + **balance: anti-permastun + stacking** + **refactor de composición** (mini-managers) + **SISTEMA DE SINERGIAS** (`SynergyTableSO`, recetas de stacks que detonan y queman) + fix hpMax del visualizer) — **🟢 Determinismo PROBADO por Juan ("DETERMINISM OK", 2 corridas idénticas) · 🟡 sinergias + fix recién agregados (a probar) · deploy JS pendiente**
**Focus:** Tres frentes decididos con Juan: (1) el sim de combate pasa a ser determinista por semilla y es EL MISMO camino en local y online — el server deja de simular (JS reducido a matchmaker que emite seed + snapshots; cada cliente corre el sim C# y deriva el MISMO record); (2) fase sinergias/balance: anti-permastun (no re-aplicar + inmunidad post-stun configurable) + stacking real de estados (instancias acumulables, stun binario); (3) la dirección de arquitectura cambia de partial-class a COMPOSICIÓN (mini-managers) — aplicada hoy a Systems/Combat, hoja de ruta del resto en [[11 - Technical Debt]] (Fases 6-9).

> ### ✅ Sim determinista por semilla (local + async comparten el camino)
> `CombatRng` nuevo (xorshift32 puro, inyectado por parámetro — nunca System.Random ni UnityEngine.Random). `CombatService.Simulate(...)` gana `int seed` (wrapper local: validaciones registry + records); nuevo núcleo público **`SimulateCore(dnaA, dnaB, db, config, equipDb, rng)`** SIN registry/validaciones/records — muta los DNA que recibe (vivos en local, snapshots efímeros en async). TODOS los rolls salen del rng en orden fijo documentado en el header (tie-break SPD, passives, eva, crit, procs, ticks, evolución, muerte). `CollectProcs` itera `Equipped` por `Enum.GetValues` (orden determinista de slots; antes `Dictionary.Values`, divergente entre clientes). Nuevo **`BuildRecord(result, self, opponent, selfWasA, ...)`** arma el CombatRecord por POV (reemplaza RecordHistory). `CombatRecord` gana `Seed` + `OpponentDnaId` (UniqueID del rival → habilita replay async sin registro, pendiente viejo de S25) + `OpponentPlayerId` (aditivo, backward-compatible). `SimulateLocal` genera la seed (`Guid.NewGuid().GetHashCode()`); `DoLocalFight` ahora pasa por `CombatController.Instance.SimulateLocal` (eliminada la llamada directa + eventos duplicados).

> ### ✅ Online sin combate JS (server = matchmaker + oráculo de seed)
> `run-combat.js` y `process-matchmaking.js` REESCRITOS: se borró TODA la simulación (fórmulas/strike/evolveRandom — estaban desactualizados vs C# desde S26; esa deuda de deploy muere sola). Al matchear emiten a `combat_results` de ambos jugadores un **match blob** `{CreatureId, Seed, SelfWasA, CreatureJsonA, CreatureJsonB, OpponentName, OpponentPlayerId, OpponentPlayerName, Date}` (los snapshots ya viajaban en el enqueue — pool intacto; enqueue/dequeue/get-queue-status sin cambios). `AsyncCombatService`: `CloudCombatResult` → `CloudMatchBlob`; `ApplyResult` deserializa ambos snapshots (**`SaveSystem.DeserializeCreature`** nuevo), corre `SimulateCore(new CombatRng(blob.Seed))` y aplica consecuencias al DNA VIVO (FightCount/WinCount/`CombatEvolution.AdvanceTier`/IsDead/record); el DRAW ahora existe en async (el JS viejo no lo contemplaba). Anti-cheat por consenso sigue DIFERIDO (decisión S29). Blobs de formato viejo pendientes en la nube → skip con warning (transición).

> ### ✅ Balance: anti-permastun + stacking (ya sin espejo JS que mantener)
> **Anti-permastun** (hito S31; decisión Juan: "no re-aplicar + inmunidad"): `CombatResolver.StunOpponent` no re-aplica sobre un objetivo ya aturdido NI sobre uno inmune (y NO graba proc event en esos casos — sin popup falso en el visualizer); al despertar (StunTurns llega a 0 en el stun-skip) el combatiente gana `StunImmunityTurns` (**nuevo en `CombatManagerSO`**, default 1, tuneable) que consume al actuar → siempre hay ventana de golpes; el deadlock de S30 (Stun passive 100%/2t mutuo) es imposible. **Stacking** (decisión Juan: DoT/HoT stackean, stun binario): `AddStatus` ya no mergea por Kind — cada aplicación es una instancia `ActiveEffect` independiente que tickea sola (log con `— xN stacks`). Los multiplicadores de tuning quedan para después; el "N stacks → efecto extra" se construyó en esta MISMA sesión (bloque siguiente) a pedido de Juan.

> ### ✅ Sistema de SINERGIAS — recetas de stacks que detonan y queman (foco del juego, pedido Juan)
> **Modelo**: `SynergyTableSO` (UN asset Odin autorable, patrón tabla única tipo `BreedingAffinityTableSO`) con `List<SynergyRule>`: cada regla = `Name` + `Requirements` (variedades: lista de `{Kind, Stacks}`, p.ej. Poison×2+Burn×2) + `Effects` (**polimórficos inline**, patrón `EquipmentSO.Effects`): `SynergyDamageEffect`/`SynergyHealEffect`/`SynergyStatusEffect`/`SynergyStunEffect` — agregar un efecto nuevo = una clase que hereda `SynergyEffectBase.Apply(CombatResolver, bearer)`. Botón "Receta ejemplo" (Explosión tóxica: Poison×3 → 10 daño) + readout de resúmenes. **Detonación**: en `CombatResolver.AddStatus` — al ganar el stack que completa una receta, detona sobre el PORTADOR: quema exactamente los stacks requeridos (FIFO, los más viejos primero, determinista) ANTES de aplicar efectos (los stacks que apliquen los efectos no se queman a sí mismos); loop con guard (cap 8) permite cadenas sin re-entrada recursiva (flag `resolvingSynergies`). **Sin roll**: umbral = detona siempre (determinista con la seed). El stun de sinergia respeta el anti-permastun (comparte `StunTarget`). **Cableado**: `CombatManagerSO.Synergies` (ref al SO) → viaja gratis a local y async sin cambiar firmas; sin tabla = sin sinergias. **Visualizer**: kind nuevo `ModifierEffectKind.Synergy`(=6) + `CombatPopupKind.Synergy` → popup violeta "¡Sinergia!" (textual en la detonación, con número en daño/cura); color en "Seteo base" de la paleta.

> ### ✅ Fix — barras/stats del visualizer ahora aplican el EQUIPO
> `CombatVisualizerService.BuildStates` pasaba `CombatStats.GetEffectiveStats` directo (sin equipo) a `statsA/statsB/hpMax` → barras y ATK/VEL desfasados vs el sim (que pelea con equipo). Ahora aplica `EquipmentStats.Apply` (propiedad `EquipDb` espejo de `Db`) — mismo pipeline que `BuildCombatant`.

> ### ✅ Refactor de composición — Systems/Combat como patrón canónico (decisión de dirección)
> Juan fijó la dirección: scripts grandes se dividen en **partes pequeñas que componen el todo (mini-managers)**, NO en partial classes. Aplicado hoy al sistema que estábamos tocando: `CombatService` (513→366) quedó orquestador delgado; piezas extraídas: `CombatRng` (RNG), `Combatant`+`ActiveEffect` (modelo, +StunImmunityTurns), `CombatResolver` (ICombatContext + anti-stun + stacking), `CombatStats` (stats base+partes; `EffectiveStats` ahora struct top-level en Data/Combat), `CombatEvolution` (tiers — dedup del switch duplicado en AsyncCombatService). **Regla 11 de CLAUDE.md REESCRITA** (composición sobre partial; la "excepción pragmática" pasa a deuda activa); hoja de ruta de los partials restantes en [[11 - Technical Debt]] Fases 6-9 (CloudSync → paneles UITK → MoriMochiAgent, una sesión dedicada por monstruo).

> ### 🧪 Verificación de determinismo (gate antes de confiar el online)
> `CombatDevConsole` gana botón **"Verify Determinism (seed)"**: clona A/B vía el MISMO pipeline de snapshots del online (`Serialize`→`DeserializeCreature`), corre `SimulateCore` 2× con la misma seed y compara huellas JSON (WinnerID/LoserID/IsDraw/LoserDied/EvolvedSlot/Turns) → OK o divergencia al log.

> ### ✅ PROBADO por Juan (mitad de sesión)
> Pelea local + **"Verify Determinism (seed)" → DETERMINISM OK** (2 corridas idénticas, 4 turns). El sim seedeado funciona; todo compila y corre.

> ### ⚠️ WIRING/DEPLOY (Juan) — lo que queda para probar la 2ª mitad
> 1. Unity recompila (sinergias: 3 .cs nuevos sin .meta aún). 2. **Crear el asset** Create → RunRunSimulator/Combat/**Synergy Table**, apretar "Receta ejemplo" (o autorar recetas propias) y **asignarlo al campo `Synergies` del asset CombatManager**. 3. **Re-apretar "Seteo base"** en `CombatPopupPalette` (toma el color violeta de `Synergy`). 4. Play: equipar procs de Poison a un MM, pelear hasta acumular 3 stacks → ver `[synergy] ¡Explosión tóxica!` en el log + popup "¡Sinergia!" en el visualizer + verificar que las barras ahora arrancan con el HP con equipo. 5. Re-correr **"Verify Determinism"** con la tabla asignada (valida que las sinergias son deterministas). 6. **Deploy de los 2 .js a UGS** (`run-combat` + `process-matchmaking`) + async instant end-to-end. 7. (Juan avisó: los resultados viejos en la nube no importan — demos; puede resetear desde el dev console.)

> ### 📋 Observaciones / decisiones abiertas
> 1. **`EvolutionChance` (config, 0.30) NUNCA se usa** — el ganador siempre intenta evolucionar. Juan (S32): "probablemente lo retiremos eventualmente" → NO construir el roll; candidato a borrar el campo en una limpieza futura. 2. El `CombatHistory` dentro de los snapshots infla el match blob (ya pasaba en el enqueue) — recortar el snapshot es optimización futura si pega el límite de Cloud Save. 3. Sinergias v1 sin cap de stacks ni ProcChance (detonación garantizada al umbral) — tuning de la fase de sinergias completa.

**Files Created (.cs — input ScriptNodes):**
- `Systems/Combat/CombatRng.cs` (NUEVO): RNG xorshift32 determinista inyectable del sim.
- `Systems/Combat/Combatant.cs` (NUEVO): `Combatant` (+`StunImmunityTurns`) + `ActiveEffect` (extraídos de CombatService).
- `Systems/Combat/CombatResolver.cs` (NUEVO): ICombatContext extraído + anti-permastun + stacking por instancias.
- `Systems/Combat/CombatStats.cs` (NUEVO): stats base+partes + `BaseHpCombatMultiplier` (extraídos de CombatService).
- `Systems/Combat/CombatEvolution.cs` (NUEVO): TryEvolveRandomSlot(rng)/AdvanceTier/GetSlotTier unificados.
- `Data/Combat/EffectiveStats.cs` (NUEVO): struct top-level (antes anidado en CombatService).
- `Data/Combat/SynergyEffectBase.cs` (NUEVO): base polimórfica + leaves Damage/Heal/Status/Stun (Apply(CombatResolver, bearer)).
- `Data/Combat/SynergyRule.cs` (NUEVO): SynergyRule (Name/Requirements/Effects/Summary) + SynergyStackRequirement {Kind, Stacks}.
- `Data/Combat/SynergyTableSO.cs` (NUEVO): tabla única autorable de recetas (Odin), botón "Receta ejemplo" + readout.

**Files Touched (.cs — input ScriptNodes):**
- `Core/Enums.cs`: + `ModifierEffectKind.Synergy = 6` + `CombatPopupKind.Synergy`.
- `Data/Combat/CombatPopupPaletteSO.cs`: + color violeta `Synergy` en "Seteo base".
- `Systems/CombatVisualizer/CombatDamageNumbers.cs`: label "¡Sinergia!"; `enableNumber` ahora también exige `Amount >= 0.5` (popups textuales sin número).
- `Systems/Combat/CombatService.cs`: REESTRUCTURADO (513→366) — `Simulate(+seed)` wrapper, `SimulateCore` núcleo puro, `BuildRecord` por POV, rolls por CombatRng, CollectProcs determinista, inmunidad post-stun en TakeTurn; clases internas/stats/evolución extraídas; pasa `config.Synergies` al resolver.
- `Systems/Combat/CombatResolver.cs` (además de su creación): motor de sinergias — `CheckSynergies` en AddStatus (detección + quema FIFO + efectos, guard anti-reentrada), métodos bearer `DamageBearer`/`HealBearer`/`StunBearer`/`AddStatusTo`, refactor `StunTarget` compartido.
- `Data/Combat/CombatRecord.cs`: + `Seed`/`OpponentDnaId`/`OpponentPlayerId` (aditivo).
- `Data/Combat/CombatManagerSO.cs`: + `StunImmunityTurns` + ref `Synergies` (SynergyTableSO) en Title "Status / Balance".
- `Core/SaveSystem.cs`: + `DeserializeCreature(json)` (inverso de `Serialize(dna)`, mismo pipeline).
- `Systems/Combat/CombatController.cs`: `SimulateLocal` genera la seed y la pasa.
- `Systems/Combat/AsyncCombatService.cs`: `CloudMatchBlob` + `ApplyResult` simula client-side (SimulateCore + BuildRecord + CombatEvolution.AdvanceTier; el AdvanceTier local se borró).
- `Systems/Combat/CombatDevConsole.cs`: + botón "Verify Determinism (seed)".
- `UI/CombatPanelUITK.Tabs.cs`: `DoLocalFight` → `CombatController.Instance.SimulateLocal` (sin eventos duplicados).
- `Systems/Combat/EquipmentStats.cs`, `UI/CombatPanelUITK.cs`, `UI/BreedingPanelUITK.Content.cs`, `UI/MorimonchiDetailInfoUITK.cs`, `World/AI/MoriMochiAgent.Tuning.cs`: rename mecánico `CombatService.EffectiveStats`→`EffectiveStats`, `GetEffectiveStats`/`BaseHpCombatMultiplier`→`CombatStats.*` (sin cambio de lógica).
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: rename CombatStats + FIX stats/hpMax con equipo (`EquipDb` + `EquipmentStats.Apply` en BuildStates) + popup Synergy (caso textual en `RaiseProcPopup` + mapeo en `ProcPopupKind`).

**Files Touched (no-ScriptNode):** `CloudCode/run-combat.js` + `CloudCode/process-matchmaking.js` (matchmaker sin sim → match blob); `CLAUDE.md` (regla 11 reescrita); `Index/11 - Technical Debt` (decisión S32 + Fases 6-9).

**Next session (S33):**
1. Testear sinergias en Play (wiring de arriba) + deploy JS + async end-to-end.
2. Tuning de sinergias con data real: autorar recetas multi-elemento, decidir cap de stacks, balance de magnitudes. UI: mostrar las recetas al jugador (¿tab/panel de sinergias?).
3. Refactor de composición — siguiente monstruo por riesgo: `CloudSyncService` (Fase 6) o piloto de sub-presenters en `CombatPanelUITK` (Fase 7).
4. Hook de consenso anti-cheat (server compara records subidos) cuando toque seguridad (Etapa 2.3). Limpieza: retirar `EvolutionChance` (decisión Juan pendiente de confirmar).
5. Pendiente S31 si no se probó: floaters con etiquetas + stun (Etapa 3b) en Play.

**ScriptNodes (cierre S32 — 2 tandas):** Tanda 1 (seed+refactor) ✅ YA CORRIDA por el vault-documenter a mitad de sesión (6 creados + 14 actualizados). Tanda 2 (sinergias+fix, corrida al cierre): CREAR `SynergyEffectBase.md`, `SynergyRule.md`, `SynergyTableSO.md`; ACTUALIZAR `CombatResolver.md` (motor de sinergias + bearer methods), `CombatManagerSO.md` (+Synergies), `CombatService.md` (pasa Synergies, menor), `Enums.md` (+Synergy ×2), `CombatPopupPaletteSO.md` (+color), `CombatVisualizerService.md` (fix equipo + popup Synergy), `CombatDamageNumbers.md` (label + enableNumber por Amount).

---

**Session:** 2026-07-01 (Session 31 — Enganche de los **procs de combate con el Combat Visualizer**: `CombatTurn` transporta proc events + replay honesto (ticks/thorns/heal/regen/stun) + **números flotantes DamageNumbersPro** coloreados y etiquetados por fuente) — **🟢 Etapa 1+2 PROBADO en Play (Juan) · Etapa 3 floaters funcionando; labels+stun recién agregados (a probar)**
**Focus:** Que el visualizer dramatice los procs (hoy solo capturaba el golpe directo) y que aparezcan popups de daño/elemento. Tres etapas: (1) data — `CombatTurn` gana proc events + `NoAttack`; (2) replay — el visualizer consume los procs (barras HP exactas, turnos sin golpe, muerte por proc); (3) juice — DamageNumbersPro coloreado + texto por fuente vía SO de paleta.

> ### ✅ Etapa 1 — `CombatTurn` transporta los procs (data)
> Nuevo `CombatProcEvent` (`Kind`/`TargetIsA`/`Amount`/`TargetHpAfter`/`BeforeStrike`) + `CombatTurn` gana `List<CombatProcEvent> Procs` + `bool NoAttack` (aditivo, backward-compatible: records viejos/async deserializan con lista vacía). El `Resolver` (seam de `ICombatContext`) y `TickStatuses` graban un event en cada mutación (thorns/heal/stun/apply-status/tick DoT-HoT) vía `Resolver.Record`. `TakeTurn` reestructurado: emite **siempre** un turno (con `NoAttack` en stun-skip / muerte-por-aflicción / muerte-por-passive) vía helper `EmitTurn`.

> ### ✅ Etapa 2 — el visualizer replica los procs (PROBADO en Play)
> `BuildStates`/`ForwardRoutine`/`CombatNode` consumen `t.Procs`: barras HP exactas por `TargetHpAfter` (mapeo sim→visual `TargetIsA == SelfWasA`), turnos `NoAttack` sin lunge fantasma, muerte por proc en **cualquier** lado (`ADiedHere`/`BDiedHere`, no solo el defensor), líneas de log de proc (`+ CombatVisualLogKind.Proc`; texto por delta: apply/tick/heal). **Fix de review (Opus):** muerte derivada del **HP final del turno** + golpe gateado solo por `NoAttack` (confía en el sim; evita divergencias veneno-revive-regen y lifesteal-remata). Juan confirmó en Play: "funciona, veo que baja".

> ### 🟢 Etapa 3 — DamageNumbersPro (floaters coloreados por fuente)
> Bus visual `+ CombatVisualPopup` DTO + evento `OnPopup`. El Service raisea un popup en cada golpe (Hit/Crit) y cada proc que **mueve HP** (usa el delta real contra `shownHp`; apply-status no popea). Presenter nuevo `CombatDamageNumbers` (suscribe OnEnable/OnDisable) hace `Spawn` del `DamageNumber` en la pos del peleador + `SetColor` por `CombatPopupPaletteSO` (Odin, dict `CombatPopupKind→Color`, botón "Seteo base"). **Nunca popea desde `Restore`/`Back`.** `DamageNumbersPro.asmdef` es `autoReferenced` → Assembly-CSharp lo ve sin refs.

> ### ✅ Etapa 3b — texto descriptivo + stun visible (a pedido de Juan)
> Cada popup escribe la fuente en `topText`: Golpe / ¡Crítico! / Veneno / Quemadura / Espinas / Cura / Regeneración / Aturdido. El **stun** ahora se muestra: `+ CombatPopupKind.Stun`, el turno de stun-skip graba un proc event `Stun` (`CombatService`), y `RaiseProcPopup` lo popea como texto (sin exigir baja de HP; `enableNumber=false`). El "Aturdido" aparece al aplicar el stun y en el turno saltado (decisión abierta: dejarlo en uno solo si es ruidoso).

> ### ⚠️ WIRING UNITY (Juan)
> Paleta `CombatPopupPalette.asset` creada; **re-apretar "Seteo base"** para tomar la entrada `Stun` nueva (si se creó antes de S31b sale blanca). Prefab `DefaultDamageNumberPo` + componente `CombatDamageNumbers` en la escena `CombatVisualizerMM` (asignar paleta + prefab + `spawnOffset`). Para 3D: prefab con `enable3DGame`/`faceCameraView`.

**Files Created (.cs — input ScriptNodes):**
- `Data/Combat/CombatProcEvent.cs` (NUEVO): DTO serializable de un proc dentro de un turno.
- `Data/Combat/CombatPopupPaletteSO.cs` (NUEVO): SO Odin `CombatPopupKind→Color` + botón "Seteo base".
- `Systems/CombatVisualizer/CombatDamageNumbers.cs` (NUEVO): presenter que suscribe `OnPopup` y escupe floaters (color + topText por fuente; stun sin número).

**Files Touched (.cs — input ScriptNodes):**
- `Data/Combat/CombatRecord.cs`: `CombatTurn` + `NoAttack` + `List<CombatProcEvent> Procs`.
- `Systems/Combat/CombatService.cs`: `Resolver` graba proc events (`Record` +2 sobrecargas, `TurnProcs`/`BeforeStrike`); `TickStatuses(+r)` graba ticks; `TakeTurn` emite siempre turno (`EmitTurn`, `NoAttack`); stun-skip graba event `Stun`.
- `Systems/CombatVisualizer/CombatVisualEvents.cs`: `+ CombatVisualLogKind.Proc`; `+ CombatVisualPopup` DTO; `+ OnPopup`/`Popup`.
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: `BuildStates`/`ForwardRoutine`/`CombatNode` consumen procs; `shownHp`; helpers `PlayProc`/`RaiseProcPopup`/`ProcPopupKind`/`FighterPos`/`SimToVisual`; muerte por HP final.
- `Core/Enums.cs`: `+ CombatPopupKind {Hit,Crit,Poison,Burn,Thorns,Heal,Regen,Stun}`.

**Files Touched (no-ScriptNode):** import package `DamageNumbersPro/`; escenas `CombatVisualizerMM.unity`/`GameScene.unity` (wiring); assets `CombatPopupPalette`/`DefaultDamageNumberPo.prefab` (Juan); `Examples/TestDamageNumberPro.cs` (test suelto de Juan, sin ScriptNode); registries de prueba + `EmojiOne.asset` (side effects del package).

**Next session (S32):**
1. Probar en Play los floaters con etiquetas + stun (Etapa 3b). Ajustar labels / campo de texto (top vs bottom/left) y decidir si el "Aturdido" queda en uno o dos momentos.
2. (Opcional) refinar juice: "+" en curas, escala en críticos, follow-transform del popup.
3. **Seed determinista + online** (retiro del JS de combate) — foco macro pendiente.
4. **Fase de sinergias / balance** — hitos a tratar:
   - Stacking real de estados (instancias acumulables, N stacks → efectos extra; hoy se mergean por Kind).
   - Tuning de procs (multiplicadores de sinergia sobre los `ActiveEffect`).
   - **HITO nuevo (pedido Juan, S31): resolver el permastun y evitar softbloqueos.** Un passive/proc de stun mutuo o recurrente (visto en S30: ambos MMs con Stun passive 100%/2t) aturde en loop → deadlock → DRAW a MaxRounds. Diseñar salvaguarda para que **ningún combate quede sin progreso**: p.ej. diminishing returns del stun, inmunidad/resistencia temporal post-stun, cap de turnos aturdido consecutivos, o no re-aplicar stun mientras dura. Es problema de diseño/tuning, no bug. Ver memoria [[project_combat_synergies_balance]].

**ScriptNodes (cierre S31 — agente Haiku):** CREAR `CombatProcEvent.md`, `CombatPopupPaletteSO.md`, `CombatDamageNumbers.md`; ACTUALIZAR `CombatRecord.md` (CombatTurn +NoAttack/+Procs), `CombatService.md` (graba proc events + NoAttack + stun record), `CombatVisualEvents.md` (+Proc kind, +CombatVisualPopup, +OnPopup), `CombatVisualizerService.md` (consume procs + popups + muerte por proc), `Enums.md` (+CombatPopupKind).

---

**Session:** 2026-06-30 (Session 30 — Testeo en Play del combate local con efectos (S29) + enriquecimiento del **log de combate** para legibilidad/observabilidad) — **✅ CERRADA (probado en Play por Juan)**
**Focus:** Probar que se pueden seleccionar dos MMs desde la pantalla UITK del combate (Tab local) y que escupa el **log detallado**. El código de S29 (combate local con procs polimórficos inline) corrió SIN tocar nada — pasó la pasada de cordura previa (cero refs colgadas a lo borrado en S29, contratos externos `CombatManagerSO`/`EquipmentStats`/enums OK). Sobre eso, se enriqueció el log a pedido de Juan.

> ### ✅ S29 validado en Play — el combate local con efectos FUNCIONA
> Juan equipó procs (Poison/Burn/Stun/ReturnDamage con Trigger + Proc%) y peleó local. Confirmado: rolls de los 3 triggers, on-hit, ticks de DoT, stun, thorns, KO, evolución y muerte. El combate por `CombatProcEffect` polimórficos inline en `EquipmentSO.Effects` anda.

> ### ✅ Log de combate enriquecido
> (1) **Rolls visibles**: cada proc rolado (ofensivo/pasivo/defensivo) loguea `[roll] {MM} {Kind} ({tipo}, {chance}%) → {dado}: PROC/no proc` — **incluidos los que fallan**. Helpers nuevos `RollProc` + `TriggerLabel`. (2) **Headers por turno** `» Turno de {MM}` + cuerpo indentado a 4 espacios (separa los dos turnos de cada ronda). (3) **Periódicos claros**: `takes N {Kind} damage → hp (Xt left)` / `regenerates N HP`; la línea `gains …` marca el momento del proc. (4) **Stun resultante** (`stunned for N`, no el valor intentado). (5) **Dados de crit/eva** en la línea del golpe: `(eva 81 vs 20% · crit 8 vs 13%)`. (6) **Print a consola** en `DoLocalFight` (`Debug.Log("[Combat]\n"+...)`) → no más imágenes.

> ### ✅ Cambio de comportamiento — no proquear sobre muerto
> `TakeTurn`: los procs on-connect (ofensivos armados + defensivos del defensor) ahora corren solo si `!dodged && def.Hp > 0f`. Antes se aplicaba veneno/burn/thorns sobre un MM ya en 0 (cosmético, ensuciaba el log).

> ### ✅ AddStatus loguea estado resultante
> `Resolver.AddStatus` ahora loguea magnitud/turnos **reales tras refrescar** (guarda ref `existing`), no los parámetros crudos (mismo patrón que el fix de stun). La lógica de merge por Kind NO cambió.

> ### ⚠️ OBSERVACIÓN de balance (NO es bug) — stun-lock pasivo
> En el último test ambos MMs llevaban **Stun passive 100% / 2 turnos**: cada uno re-aturde al otro todos los turnos antes de poder pegar → deadlock → DRAW a 10 rounds. El código simula correctamente esas reglas; el problema es **tuning** (un passive stun 100%/2t mutuo es degenerado). Va a la **fase de sinergias/balance**. NO construir restricción ahora.

> ### 🧠 Decisión confirmada — stacking de estados = futuro (fase de sinergias)
> Dos procs del mismo Kind (ej. dos Burns) HOY se **mergean** (refresh por Kind: último gana magnitud, máximo de turnos). Juan quiere que **se stackeen** (instancias acumulables) y que cada N stacks dispare efectos extra → eso es la fase de sinergias, no se construye ahora. El log honesto (AddStatus resultante) refleja el merge interino.

**Files Touched (.cs — input ScriptNodes):**
- `Systems/Combat/CombatService.cs` (MODIFICADO): log enriquecido (helpers nuevos `RollProc`/`TriggerLabel`, headers de turno + indent, dados crit/eva, ticks/stun/AddStatus más claros); guard `def.Hp > 0f` en procs on-connect; `AddStatus` loguea estado resultante. **Contrato público (`Simulate`/`EffectiveStats`/`GetEffectiveStats`) sin cambios.**
- `UI/CombatPanelUITK.Tabs.cs` (MODIFICADO): `DoLocalFight` imprime el log completo a consola (`Debug.Log("[Combat]\n"+...)`). Cambio menor.

**Files Touched (no-script — wiring de testeo de Juan):** assets `Equipment1`/`Equipment2`, `New Creature Registry SO`, `New Furniture Registry SO` — datos de prueba, no tocan código.

**Next session (S31):**
1. Enganche de los procs con el **Combat Visualizer**: extender `CombatTurn` con eventos de proc (hoy solo captura el golpe directo) para dramatizarlos.
2. **Seed determinista + online** (decisión macro S29): seed + snapshots de DNA, sim C# puro en ambos clientes, retiro del JS de combate.
3. **Fase de sinergias / balance**: stacking real de estados (instancias acumulables, N stacks → efectos extra) + tuning de procs (resolver el stun-lock pasivo).

**ScriptNodes (cierre S30 — agente Haiku):** ACTUALIZAR `CombatService.md` (log enriquecido + guard no-proc-sobre-muerto + `AddStatus` resultante; helpers `RollProc`/`TriggerLabel`); `CombatPanelUITK.md` (`DoLocalFight` imprime el log a consola).

---

**Session:** 2026-06-30 (Session 29 — Equipamiento: rediseño del sistema de **procs de combate** a efectos polimórficos INLINE + **levantado del combate local con efectos** (resolver, solo log)) — **🟡 CÓDIGO HECHO, SIN PROBAR EN PLAY (cerrada por tiempo)**
**Focus:** Tras varias iteraciones de diseño con Juan, se descartó el catálogo `(Kind,Tier)` de S28 y se volvió al plan original de S26: los combat procs son **subclases de `EquipmentEffectBase` inline en `EquipmentSO.Effects`**. Luego se levantó el combate LOCAL con esos efectos funcionando (resolver con triggers), solo log de texto. Seed/online + visualizer = próxima sesión.

> ### 🧭 Decisión macro — combate por SEMILLA determinista (fijada, implementación futura)
> El server pasará a emitir una **seed + snapshots de ambos DNA**; ambos clientes corren el MISMO sim C# puro → mismo record. **JS de combate se retira.** Mata la paridad C#↔JS (dolor recurrente) y libera el behavior a C# polimórfico (deroga la regla S28 "catálogo cerrado para que el JS lo espeje"). Por eso pudimos volver a efectos polimórficos con lógica. Detalle en memoria [[project-combat-seed-architecture]]. Implementación = próxima sesión (esta usa el `UnityEngine.Random` actual, sin seedear).

> ### ✅ Rediseño modifiers → procs polimórficos inline
> Se BORRÓ el catálogo de S28/S29 (`EquipmentModifier.cs`, `ModifierEffectSO.cs`, `EquipmentModifierDatabaseSO.cs`, enums `ModifierTier`/`StatusEffect`, campo+accessor en GameManager, bloque `Modifiers` en EquipmentSO). Reemplazo: `CombatProcEffect : EquipmentEffectBase` (abstracto: `Trigger` + `ProcChance` + `Kind` tag + `Apply(ICombatContext)`) con leaves `ReturnDamageEffect`/`HealEffect`/`StunEffect` y base `PeriodicProcEffect` → `PoisonEffect`/`BurnEffect`/`RegenEffect`. **Valores FLAT.** Viven en `EquipmentSO.Effects` (misma lista que `StatModifierEffect`). `ModifierEffectKind` queda como **tag de runtime** (`{ReturnDamage,Heal,Poison,Burn,Stun,Regen}`; `ApplyStatus` eliminado, statuses promovidos a kinds para que el arma elija cuál).

> ### ✅ Trigger configurable por efecto (ofensivo/defensivo/pasivo)
> Enum `TriggerType {Offensive, Defensive, Passive}`, campo en `CombatProcEffect`, mostrado en el `Summary` (`[on hit]`/`[when hit]`/`[passive]`). Un mismo efecto (ej. Regen) puede ser pasivo, ofensivo (lifesteal) o defensivo. **Los 3 triggers rolean `ProcChance`** (incl. Passive: se rolea solo al inicio del turno, sin depender de un golpe, pero puede fallar). Condición "defensivo solo si <X vida" = futuro.

> ### ✅ Combat resolver LOCAL (solo log) — el "nuevo combate local"
> `CombatService.Simulate` reescrito: param nuevo `EquipmentDatabaseSO`; `Combatant` (mutable: hp/maxhp/stats CON EQUIPO vía `EquipmentStats.Apply` + `List<CombatProcEffect> Procs` + `List<ActiveEffect> Active` + `StunTurns`). Flujo de turno: tick DoT/HoT → fire pasivos → si stun saltea → **roll ofensivo al inicio del turno** → ataque (evasión→crit→DEF) → **on connect: ofensivos armados + defensivos del defensor** (rolled al ser golpeado). Los efectos NO mutan estado: emiten acciones vía `ICombatContext` (`Resolver` nested) — **seam para el stack Yu-Gi-Oh** futuro. `ActiveEffect` tickea Poison/Burn (daño) y Regen (cura). Todo va al log; el `CombatTurn` estructurado sigue capturando solo el golpe directo (procs en el visualizer = próxima sesión).

> ### ✅ Visual — procs en la columna derecha de la card
> Procs (ámbar `◆`) movidos a `.equip-card__procs` (40%, derecha) sobre la cuña diagonal (reservada desde S27); stat mods (verde `•`) a la izquierda. `EffectsText`/`ModifiersText` filtran `item.Effects` por tipo.

**Files Created (.cs — input ScriptNodes):**
- `Data/Equipment/CombatProcEffect.cs` (NUEVO): base + leaves ReturnDamage/Heal/Stun + `PeriodicProcEffect`→Poison/Burn/Regen.
- `Data/Combat/ICombatContext.cs` (NUEVO): interfaz del resolver (DamageOpponent/HealSelf/ApplyStatusTo*/StunOpponent).

**Files Touched (.cs — input ScriptNodes):**
- `Core/Enums.cs`: `ModifierEffectKind` split; + `TriggerType`; borrados `ModifierTier` y `StatusEffect`.
- `Systems/Combat/CombatService.cs`: REESCRITO (Combatant/ActiveEffect/Resolver, equipDb, stats con equipo + procs, flujo con triggers, tick).
- `Data/Equipment/EquipmentSO.cs`: borrado bloque `Modifiers`/`ModifiersSummary`.
- `UI/MorimonchiDetailInfoUITK.cs`: borrado `modifierDatabase`; `EffectsText`/`ModifiersText` por tipo; procs a la derecha.
- `Core/GameManager.cs`: borrado campo+accessor `EquipmentModifierDatabase`.
- `Systems/Combat/CombatController.cs`, `UI/CombatPanelUITK.Tabs.cs`: `Simulate(...)` +param equipDb.

**Files Deleted (.cs — borrar su ScriptNode):**
- `Data/Equipment/EquipmentModifier.cs`, `Data/Equipment/ModifierEffectSO.cs`, `Data/Databases/EquipmentModifierDatabaseSO.cs`.

**Files Touched (no-ScriptNode):**
- `UI Toolkit/MorimonchiDetailInfoUITKStyle.uss`: + `.equip-card__procs` + `.equip-card__mods` a la derecha.

**WIRING UNITY (Juan):**
1. ✅ Assets huérfanos borrados (Juan, 2026-06-30): `EquipmentModifierDatabase.asset` + `Effect_*.asset`.
1b. 🔴 **PENDIENTE S30 (primer foco): TESTEAR el combate local con efectos en Play (log)** — no se llegó a probar esta sesión.
2. NO hace falta Wipe Registry (procs viven en assets de equipo, no en saves).
3. Agregar procs a `EquipmentSO.Effects` (Trigger + Proc %).
4. Play → pelea local → revisar el **log**: rolls, on-hit, ticks, stun, thorns.

**Next session (S30):**
1. Testear el combate local con efectos en Play (log).
2. Enganche con el Combat Visualizer (extender `CombatTurn` con eventos de proc).
3. Seed determinista + online (retiro del JS de combate).
4. Synergias (multiplicadores x1.5 / x0.5 sobre los `ActiveEffect`).

**ScriptNodes (cierre S29 — agente Haiku):** CREAR `CombatProcEffect.md`, `ICombatContext.md`; ACTUALIZAR `CombatService.md`, `Enums.md`, `EquipmentSO.md`, `MorimonchiDetailInfoUITK.md`, `GameManager.md`, `CombatController.md`, `CombatPanelUITK.md`; BORRAR `EquipmentModifier.md`, `ModifierEffectSO.md`, `EquipmentModifierDatabaseSO.md`.

---

**Session:** 2026-06-26 (Session 28 — Equipamiento: sistema de **Modificadores** (combat procs) como referencia liviana `(enum Kind + enum Tier)` resuelta contra una DB local de catálogo. Etapa inicial = data + display, SIN combate) — **🟡 CÓDIGO HECHO, TESTEO PENDIENTE (sin tiempo; se prueba próxima sesión)**
**Focus:** Extender `EquipmentSO` para que, aparte de `Effects` (mods de stat inline), contenga `Modifiers` que son combat procs (regresar daño / curar / aplicar estado N turnos). Etapa inicial: crear, conservar info y mostrarlo en la card del detail panel. Integración con combate = etapa siguiente.

> ### 🟡 Diseño (conversado con Juan) — DB local + referencia liviana `(Kind, Tier)`
> Se descartaron SO polimórficas y SO declarativo único → se adoptó el **mismo patrón que `EquipmentDatabaseSO`/ID en el DNA**: el equipo guarda solo una referencia liviana y la data concreta vive en una DB. Estructura final: `Tier` es **enum** `ModifierTier {I..V}`; el catálogo es **`Dictionary<ModifierEffectKind, Dictionary<ModifierTier, ModifierTierDef>>`** (efecto → tier → struct de tuning).

> ### 🧭 Análisis de arquitectura (decisión clave, escrita para Et.2)
> El backbone es **correcto para este juego** precisamente porque el combate es **async/server-authoritative/replayable**: el JS debe espejar el efecto de forma determinista → un **catálogo cerrado de efectos (enum) + tuning por tier** es lo idiomático (SO polimórficos con lógica libre morderían acá: el server no corre C#). Tres ejes separados: **Identidad/behavior = `ModifierEffectKind`**, **escala = `ModifierTier`**, **números = `ModifierTierDef` (data en la DB)**; lo que viaja (save/cloud/JS) = **`(Kind, Tier)`**, contrato estable. **Regla para Et.2 (no romper): `Kind` es el ÚNICO punto de dispatch del behavior** (un handler por Kind), el struct solo lleva tuning numérico. Techo futuro: NO ensanchar el struct con columnas que casi nadie usa → cuando un Kind pida params estructurados, darle un **payload tipado por Kind**. **Stacking por cantidad (estilo RoR) = decisión abierta**, no construir aún (hoy el poder sale del Tier autorado).

> ### ✅ Persistencia gratis
> `Modifiers` vive en el asset `EquipmentSO` (la **plantilla**), NO en `CreatureDNA`. La criatura ya guarda el ID del equipo → al resolverlo aparecen sus modifiers. No se tocó save/cloud ni `GameEvents`.

**Files Created (.cs — input ScriptNodes):**
- `Data/Equipment/EquipmentModifier.cs` (NUEVO): structs `ModifierTierDef` (tuning por tier: Label/Magnitude/DurationTurns/Status) + `EquipmentModifierRef` (referencia liviana `{Kind, Tier}`).
- `Data/Databases/EquipmentModifierDatabaseSO.cs` (NUEVO): catálogo dict anidado (Odin `[OdinSerialize]`); `TryResolve(ref/kind+tier)`, `Summary(ref)`, `KindLabel` estático, `KindCount`, `Editor` estático (resuelve sin GameManager vivo, como `EquipmentDatabaseSO.Editor`).

**Files Touched (.cs — input ScriptNodes):**
- `Core/Enums.cs`: + `ModifierEffectKind {ReturnDamage, Heal, ApplyStatus}`, + `StatusEffect {None, Poison, Burn, Stun, Regen}`, + `ModifierTier {I, II, III, IV, V}`.
- `Data/Equipment/EquipmentSO.cs`: + `List<EquipmentModifierRef> Modifiers` (add por enum+tier) + "Resumen mods" editor (resuelve vía `EquipmentModifierDatabaseSO.Editor`).
- `Core/GameManager.cs`: + campo `equipmentModifierDatabase` + accessor `EquipmentModifierDatabase`.
- `UI/MorimonchiDetailInfoUITK.cs`: + ref serializada `modifierDatabase` (fallback a GameManager) + `ModifiersText(item)` + label `equip-card__mods` en la card (debajo de los efectos).

**Files Touched (no-ScriptNode):**
- `UI Toolkit/MorimonchiDetailInfoUITKStyle.uss`: + `.equip-card__mods` (tinte ámbar, distinto del verde de `equip-card__effects`).

**WIRING UNITY (Juan — PENDIENTE, bloqueante para Play):**
1. Crear asset: Create → RunRunSimulator/Databases/Equipment Modifier Database.
2. Poblar el catálogo: por cada `ModifierEffectKind`, sus tiers (I–V) con valores (ej. `ReturnDamage` I `{Magnitude:0.15}`; `ApplyStatus` I `{Status:Poison, DurationTurns:3, Magnitude:2}`).
3. Asignarlo en `GameManager` (campo *Equipment Modifier Database*) y en `MorimonchiDetailInfoUITK` (campo *Modifier Database*).
4. Agregar `Modifiers` (Kind+Tier) a algún `EquipmentSO`; el "Resumen mods" del inspector confirma la resolución.
5. Play: abrir la card de un MM con ese equipo → ver las líneas ámbar `◆ …`.

**ScriptNodes a crear/actualizar (cierre S28 — agente Haiku):** CREAR `EquipmentModifier.md`, `EquipmentModifierDatabaseSO.md`; ACTUALIZAR `EquipmentSO.md` (+`Modifiers`), `GameManager.md` (+`EquipmentModifierDatabase`), `MorimonchiDetailInfoUITK.md` (+modifiers en la card vía DB), `Enums.md` (si existe: +ModifierEffectKind/StatusEffect/ModifierTier).

**Next session (S29):**
1. **Testear** la etapa inicial de modifiers en Play (wiring de arriba).
2. Si OK → **Etapa 2 = behavior**: handler por `Kind` (`IModifierBehavior` resuelto por Kind), aplicar procs en el pipeline de combate + `StatSheet`/hooks, **paridad JS** (espejo del catálogo). Llenar la mitad diagonal de la card con los efectos. Respetar la regla "Kind = dispatch".

---

**Session:** 2026-06-26 (Session 27 — Equipamiento Etapa 1 cerrada: DISPLAY del equipo (StatSheet de display, tab Stats en MoriMochiAgent, tab Equipo minimalista con cards/diagonal/paleta) + pipeline de arte Gemini (pausado)) — **✅ CERRADA (probado en Play por Juan)**
**Focus:** Cerrar los pendientes de Et.1 (S26): que el equipo se VEA. (1) Capa que aplica los `StatModifier` del equipo a los stats (el "StatSheet", solo display). (2) Vista rápida de stats en el inspector del agente. (3) Tab Equipo del detail panel. Se exploró un look cyberpunk con sprites (+ se montó un pipeline de generación de imágenes con Gemini) pero se descartó por no pegar con el vibe cozy/low-poly → se volvió a UI minimalista.

> ### ✅ Bloque A — `EquipmentStats.Apply` (el StatSheet, solo display)
> Nuevo `Systems/Combat/EquipmentStats.cs` (clase estática pura). `Apply(EffectiveStats base, dna, EquipmentDatabaseSO)` junta los `StatModifier` de los ítems equipados y aplica por stat **Flat → PercentAdd (Σ%) → PercentMult (compuesto)**, piso 0. **Solo conectado al DISPLAY** (no al pipeline de combate — eso es Fase 2). Reutilizable por combate + espejo JS cuando llegue.

> ### ✅ Bloque B — Tab "Stats" Odin en `MoriMochiAgent`
> En `MoriMochiAgent.Tuning.cs`, `[TabGroup("Tuning","Stats")]` (mismo patrón live-readout que la tab Needs): por stat muestra `Base (con partes) → Final (con equipo)` con el delta. Solo en Play (cuando `dna` está inyectado). Resuelve DBs vía `GameManager.Instance.Database`/`EquipmentDatabase`.

> ### ✅ Bloque C — Tab Equipo del detail panel (MINIMALISTA, tras descartar cyberpunk)
> Reescrita en `MorimonchiDetailInfoUITK` (+uxml/uss). 2 columnas: **Izq** = cards (iteran el enum `EquipmentSlot` → escalan a 6 slots solas, dentro de un `ScrollView`): ícono + nombre (color por rareza) + `slot · rareza` + `Description` + efectos; **acento de borde-izq por slot** + **cuña diagonal a la MITAD pintada con `Painter2D` en el color de la rareza** (alpha 0.5, espacio reservado para los efectos de Fase 2); slot vacío = card atenuada sin diagonal. **Der** = imagen grande del MM (swatch `BaseColor`) + desglose de stats `Base → Final` (delta verde/rojo). La tab **Info** también aplica `EquipmentStats.Apply` ahora (stats reflejan el equipo). Fix recurrente: flechitas ◄► del `TabView` ocultadas en USS (`.unity-repeat-button`/`.unity-button` dentro del header-container).

> ### ✅ Bloque D — `EquipmentPaletteSO` + campos de `EquipmentSO`
> Nuevo `EquipmentPaletteSO` (Odin): `rareza→color (pastel)` + `slot→color`, con botón **"Seteo base (pastel)"** que precarga ambos (fallbacks: rareza→`BodyPart.RarityColor`, slot→defaults). `EquipmentSO` ganó **`Description`** (multilínea, se muestra en la card) + **`IconColor`** (color del ícono cuando no hay sprite). El panel resuelve colores vía la paleta (con fallback si no está asignada).

> ### 🎨 Bloque E — Pipeline de arte Gemini (workflow nuevo, generación PAUSADA)
> Montado y probado: `Tools/gen-image.ps1` lee prompts `.md` de `Resources/Sprites/UI/Ideas/` (con `_style.md` compartido = dirección de arte), llama a Gemini (`gemini-3.1-flash-image-preview`/Nano Banana), guarda el PNG. `Tools/key-transparency.ps1` hace chroma **verde→alfa** (Nano Banana NO entrega alfa real — confirmado, devuelve 24bpp opaco). Key en `Tools/gemini.key` (gitignored vía `Tools/*.key`) o env `GEMINI_API_KEY`. **Pausado** porque se volvió a UI minimalista; los PNG generados (`equip_*.png`) quedan sin uso por la UI.

**WIRING UNITY (Juan):** crear el asset `EquipmentPalette` (Create → RunRunSimulator/Equipment/Equipment Palette) + botón "Seteo base"; asignarlo al campo `Equipment Palette` del `MorimonchiDetailInfoUITK` (y confirmar `Equipment Database` ya asignado). Opcional: `Description`/`IconColor` por `EquipmentSO`.

**Files Created (.cs — input ScriptNodes):**
- `Systems/Combat/EquipmentStats.cs` (NUEVO): resolver puro de modificadores (Flat→PercentAdd→PercentMult), el "StatSheet" de display.
- `Data/Equipment/EquipmentPaletteSO.cs` (NUEVO): rareza→color + slot→color (Odin), botón seteo base pastel.

**Files Touched (.cs — input ScriptNodes):**
- `World/AI/MoriMochiAgent.Tuning.cs`: + tab Odin "Stats" (Base→Final con equipo, live-readout play-mode).
- `UI/MorimonchiDetailInfoUITK.cs`: stats de Info con equipo; tab Equipo reescrita (cards por enum + ScrollView + acento slot + diagonal Painter2D + portrait + stats Base→Final); helpers `RarityColor`/`SlotColor`/`PaintDiagonal`; campos `equipmentDatabase`/`equipmentPalette`.
- `Data/Equipment/EquipmentSO.cs`: + `Description` (multilínea) + `IconColor`.

**Files Touched (no-ScriptNode):**
- `UI Toolkit/MorimonchiDetailInfoUITK.uxml`/`.uss`: tab Equipo layout 2 columnas + estilos minimalistas + fix flechitas TabView.
- `Tools/gen-image.ps1`, `Tools/key-transparency.ps1` (NUEVOS), `.gitignore` (+`Tools/*.key`), `Resources/Sprites/UI/Ideas/*.md` (prompts + `_style.md` + `_README.md`), PNGs generados.

**ScriptNodes a actualizar/crear (cierre S27 — agente Haiku):** CREAR `EquipmentStats.md`, `EquipmentPaletteSO.md`; ACTUALIZAR `MoriMochiAgent.md` (tab Stats), `MorimonchiDetailInfoUITK.md` (tab Equipo minimalista + Info con equipo), `EquipmentSO.md` (+Description/+IconColor).

**Next session (S28) — FASE 2: Sistema de modificadores en COMBATE:**
1. Conectar `EquipmentStats.Apply` (o un `StatSheet`) al pipeline real: `CombatService.ComputeStats`/`Strike` deben usar los stats con equipo.
2. Hooks polimórficos en `EquipmentEffectBase` (OnHitDealt/OnHitReceived/OnTurnEnd) + procs concretos (lifesteal/thorns/DoT) sobre un `CombatContext` efímero.
3. Llenar la **mitad diagonal** de la card con esos efectos.
4. **Paridad JS** (`run-combat.js`/`process-matchmaking.js`). Et.3 = Combat Visualizer.

---

**Session:** 2026-06-26 (Session 26 — Justicia de stats base (point-buy) + 3 stats nuevos DEF/LCK/EVA con rename HP→CON + Sistema de Equipamiento Etapa 1 (data/persistencia/drag-drop)) — **🟡 CÓDIGO HECHO, TESTEO PARCIAL (sigue mañana)**
**Focus:** (1) Auditar `MintRandomCreature` y volver justos los stats base. (2) Diseñar e implementar 3 stats adicionales derivados del equipo (DEF/LCK/EVA) + rename HP→CON. (3) Conversar y arrancar el gran Sistema de Equipamiento (SO + efectos polimórficos Odin) — Etapa 1 de 3.

> ### ✅ Bloque A — Stats base justos (point-buy)
> `MintRandomCreature` tiraba 3 stats independientes 1–10 (suma 3–30, lotería). Decisión de Juan: **presupuesto compartido**. `CreatureGenerator.RandomBaseStats()` reparte `StatBudget=18` entre CON/ATK/SPD (min 1, max 10) → mismo poder total, perfil variado. Además el breeding (`InheritStat`) no tenía techo (solo piso 1) → power-creep infinito; ahora `Mathf.Clamp(.., StatMin, StatMax)` con `CreatureGenerator.StatMax=10` (fuente única del tope, mint y breed simétricos). `BaseHpCombatMultiplier ×5` queda igual (decisión de Juan).

> ### ✅ Bloque B — 3 stats nuevos DEF/LCK/EVA + rename HP→CON
> Stats con 3 letras: **CON** (antes HP), ATK, SPD, **DEF**, **LCK**, **EVA**. `BaseHP`→`BaseConstitution`; nuevos `BaseDefense/BaseLuck/BaseEvasion` nacen en **0** (los llenará el equipo). Fórmulas (en `CombatManagerSO`, tuneables): HP pool=`CON×5`; crit=`CritChance(0.10)+LCK×LuckCritPerPoint(0.03)`; daño=`ATK×(crit?×3)×(1-DEF×DefenseReductionPerPoint(0.08))` (máx 80%); evasión=`EVA×EvasionPerPoint(0.10)` → esquiva (daño 0). Orden en `Strike`: evasión→crit→daño→reducción DEF. `EffectiveStats` pasó de 3 a **6 campos** (toca toda la UI). `CombatService.BaseHpCombatMultiplier` ahora público (lo usa el visualizer). **JS server sincronizado** (`run-combat.js`/`process-matchmaking.js`) con las mismas fórmulas (constantes hardcodeadas, deuda vieja: el JS no suma stats de partes). **RESET requerido** por el rename → botón nuevo **"Wipe Registry (DEV)"** en `CreatureRegistrySO`. **Pendiente Juan: desplegar los .js a UGS.**

> ### 🟡 Bloque C — Sistema de Equipamiento, ETAPA 1 (data + persistencia + drag-drop)
> Conversado largo con Juan. Decisiones: **slots tipados** (Weapon/Armor/Amulet), **plantilla por ID** (DNA guarda IDs, resuelve vs DB), **hooks polimórficos** (no eventbus) para los procs de Etapa 2, **diseñar para paridad async** → efectos = **catálogo cerrado y declarativo** (StatModifier, y en Et.2 Lifesteal/Thorns/ApplyStatus parametrizados) para que el JS los espeje. Implementado: enums `StatType/ModifierType/EquipmentSlot`; `StatModifier` (struct); `EquipmentEffectBase`(abstracta Odin)+`StatModifierEffect`; `EquipmentSO` (slot + `[OdinSerialize] List<EquipmentEffectBase>`); `EquipmentDatabaseSO` (espejo `PartDatabaseSO`, prefijo "EQ"); `CreatureDNA.Equipped` (`Dictionary<EquipmentSlot,string>`, fuera del genetic string) con **drag-drop Odin editor por slot** (resuelve/estampa ID vía `EquipmentDatabaseSO.Editor`); `GameManager.EquipmentDatabase`. Persistencia **gratis** (IDs en JSON via `StringEnumConverter`, local+cloud; saves viejos cargan vacío). NO se tocó combate ni `GameEvents` (por pedido).

> ### ⚠️ PENDIENTES de Et.1 detectados en testeo de Juan (PRIMER FOCO MAÑANA)
> 1. **Targeting del grid:** puse la columna Equip + 6 stats en `CreatureGridView` (TableList **dev** de Odin, componente suelto en GameScene — por eso Juan "no halló referencia"). El grid que ve el JUGADOR es **`CreatureGridUITK`** (cartas, `CreatureCardUITK.uxml`): su `BindCard` solo pinta nombre/color/estado, **nunca** stats ni equipo. Mover el display de equipo (y stats) ahí.
> 2. **Stats modificadas no se ven:** esperado — `StatModifier` NO está conectado al cálculo de stats todavía (falta un `StatSheet`/capa en `GetEffectiveStats`). Es Et.2 (o adelantarlo si Juan quiere verlo en display).
> 3. **Tab de equipo:** no existe. Crear (en `MorimonchiDetailInfoUITK` tabs o panel propio).

**Files Created (.cs — input ScriptNodes):**
- `Data/Equipment/StatModifier.cs` (NUEVO): struct átomo `{StatType, ModifierType, float}`.
- `Data/Equipment/EquipmentEffectBase.cs` (NUEVO): base abstracta Odin polimórfica + `StatModifierEffect` (lista de StatModifier). Hooks de combate llegan en Et.2.
- `Data/Equipment/EquipmentSO.cs` (NUEVO): objeto equipable (slot + lista de efectos + resumen).
- `Data/Databases/EquipmentDatabaseSO.cs` (NUEVO): dict por ID, prefijo "EQ", Populate/Sync/GetByID/GetBySlot, `Editor` static para el drag-drop del DNA.

**Files Touched (.cs — input ScriptNodes):**
- `Core/Enums.cs`: + `StatType`, `ModifierType`, `EquipmentSlot`.
- `Core/CreatureGenerator.cs`: consts StatBudget/StatMin/StatMax + `RandomBaseStats()` (point-buy).
- `Core/GameManager.cs`: `RandomBaseStats()` en Mint; ref `EquipmentDatabase`.
- `Data/Genetics/CreatureDNA.cs`: rename `BaseHP`→`BaseConstitution`; + `BaseDefense/Luck/Evasion`; + `Equipped` dict + drag-drop editor por slot.
- `Data/Genetics/CreatureRegistrySO.cs`: botón "Wipe Registry (DEV)".
- `Data/Combat/CombatManagerSO.cs`: CritChance 0.20→0.10 + `LuckCritPerPoint`/`DefenseReductionPerPoint`/`EvasionPerPoint`.
- `Systems/Combat/CombatService.cs`: `EffectiveStats` 6 campos; `ComputeStats`/`AccumulatePart` (con); pool=CON×5; `Strike` evasión→crit(LCK)→daño→reducción(DEF); mult público.
- `Systems/Breeding/BreedingService.cs`: `InheritStat` clamp [StatMin,StatMax]; `BaseConstitution`.
- `Systems/Customers/ValuationHandler.cs`: suma los 6 stats.
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: `hpMax = Constitution × mult`.
- `UI/MorimonchiDetailInfoUITK.cs`, `UI/CombatPanelUITK.cs`, `UI/CombatPanelUITK.Tabs.cs`, `UI/BreedingPanelUITK.Content.cs`: `EffectiveStats` 6 args, labels CON/ATK/SPD/DEF/LCK/EVA (ya pasados por Haiku a mitad de sesión).
- `UI/CreatureGridView.cs`: 6 columnas de stats + columna Equip (TableList dev — ver pendiente #1).

**Files Touched (no-ScriptNode):**
- `CloudCode/run-combat.js`, `CloudCode/process-matchmaking.js`: fórmulas DEF/LCK/EVA + crit nuevo (constantes hardcodeadas).
- `UI Toolkit/MorimonchiDetailInfoUITK.uxml`/`.uss`: stat-con/def/lck/eva + colores.

**ScriptNodes a actualizar/crear (cierre S26 — agente Haiku):** CREAR `StatModifier.md`, `EquipmentEffectBase.md`, `EquipmentSO.md`, `EquipmentDatabaseSO.md`; ACTUALIZAR `CreatureGenerator.md`, `GameManager.md`, `CreatureDNA.md`, `CreatureRegistrySO.md`, `CombatManagerSO.md`, `CombatService.md`, `BreedingService.md`, `ValuationHandler.md`, `CombatVisualizerService.md`, `CreatureGridView.md`.

**Next session (S27):**
1. **PENDIENTES Et.1** (arriba): equipo en `CreatureGridUITK` (cartas) + tab de equipo + (opcional) `StatSheet` para ver stats modificadas en display.
2. **Etapa 2 — Equipamiento en combate (logs):** `CombatContext` efímero, `Combatant`, hooks polimórficos (OnHitDealt/OnHitReceived/OnTurnEnd), procs concretos (Lifesteal/Thorns/ApplyStatus DoT), `StatSheet` aplicando StatModifier al pipeline. Solo logs.
3. **Etapa 3 — Equipamiento en el Combat Visualizer** (estructurar procs en `CombatRecord`/`Turns`, reproducir feedbacks).
4. Wiring + deploy de los .js a UGS (Bloque B). Reset de progreso (Wipe Registry).

---

**Session:** 2026-06-25 (Session 25 — Cierre del bug S24 (StackOverflow), fixes del Combat Visualizer (rewind/muerte, doble animación), stats en la barra de HP, botón Volver a GameScene, color de género en el NameTag) — **✅ CERRADA (probado en Play por Juan)**
**Focus:** Resolver los pendientes de S24 y pulir el Combat Visualizer con UI y un fix cosmético del NameTag.

> ### ✅ Bug S24 StackOverflow — RESUELTO (S25)
> **Causa:** el `UnityEvent OnAttack` de `MoriMonchiCombatVisualizer` estaba cableado a su **propio** `PlayAttack` (misma instancia) → recursión infinita. Era el único binding mal apuntado.
> **Fix:** se renombraron los 6 wrappers de `MoriMonchiProceduralAnimator` `Play*` → `Anim*` (`AnimIdle/AnimWalk/AnimAttack/AnimHit/AnimDeath/AnimVictory`) para matar la colisión de nombres con el combat visualizer, y se reescribió el cableado YAML del `MoriMonchiVisualizer.prefab` completo (8 bindings al animator + corrección del target de `OnAttack`). `PlayMMAnimation(enum)` (API core + botón Odin `Test ▶`) queda igual.

> ### ✅ Fix — atacante hacía "las dos" (lance + retroceso) (S25)
> `OnAttack` (windup, `FireWindup`) y `OnHitDealt` (impacto, `FireImpact`) son momentos distintos del mismo turno. `OnHitDealt`/`OnCritDealt` estaban cableados a animaciones de cuerpo del atacante (`AnimHit`/`AnimAttack`), duplicando el lance del `OnAttack`. **Fix:** se vaciaron los bindings de animación de `OnHitDealt` y `OnCritDealt` en el prefab (quedan libres para SFX/shake MMF). Reacción de cuerpo del atacante = solo `AnimAttack` vía `OnAttack`; del defensor = `AnimHit` vía `OnHitTaken`/`OnCritTaken`.

> ### ✅ Fix — rewind se atoraba en la animación de muerte (S25)
> Al retroceder, `Restore` reactivaba el GameObject del muerto pero el `MoriMonchiProceduralAnimator` seguía con `dead=true` → quedaba volcado. **Fix (idea de Juan: el estado de animación vive en la lista enlazada):** el Service cachea `animA`/`animB` (`GetComponent` en spawn) y `Restore` repone la pose desde el flag de muerte **del nodo** (`node.ADead`/`BDead`) vía `RestoreAnim`: vivo → `AnimIdle` (revive), muerto → `AnimDeath`. No dispara juice (respeta S23).

> ### ✅ Stats en la barra de HP del Combat Visualizer (S25)
> `MoriMonchiCombatVisualizerUITK`: nueva API `Bind(nombre, ataque, velocidad)` + `SetHp(actual, max)` (antes recibía un pct). La barra muestra `actual / max` **dentro** del track (animado con el lerp, estilo Pokémon), y una fila debajo con **ATK** (izq, naranja) y **VEL** (der, celeste). El Service computa `statsA`/`statsB` (`CombatService.GetEffectiveStats`) una vez en `BuildStates` y los pasa al `Bind`; `PushHp` manda `(hp, max)` reales.

> ### ✅ Botón "Volver a GameScene" + scene manager aparte (S25)
> Nuevo `CombatSceneManager` (responsabilidad única: navegación de escena) con `ReturnToGameScene()` + `gameSceneName` serializado (default `GameScene`); hace `Stop()` del Service antes de `SceneManager.LoadScene`. Botón "◀ Volver" arriba-izquierda en su **propio** UIDocument screen-space (`CombatTopBar.uxml`/`.uss`), independiente del panel de replay (que se oculta sin combate) → siempre visible. **Build Settings:** `GameScene` debe estar en Scenes In Build.

> ### ✅ Color de género en el NameTag (S25)
> El nombre del MoriMochi se colorea por género (azul claro macho / rosa hembra) reutilizando el helper existente `NameTag.GenderColor` (single source of truth, mismo color que el glyph ♂/♀). Se aplica en `Bind` junto al texto.

**Diferencia async vs local (respondido a Juan, sin código):** el harness "🎲 MM al azar" a veces no simula por dos causas distintas (ver Consola): (1) **"El rival no está en el registro"** — el visualizer es local y reconstruye el modelo del rival buscándolo por `CustomName` en el registro; peleas **async contra otro jugador** (o rivales muertos/vendidos) no están → no se pueden visualizar; (2) **"Ningún MM tiene peleas con turnos"** — records de **formato viejo** con `Turns` vacío (filtrados por `HasTurns`). Fix real del caso async (FUTURO, no hecho): guardar el DNA del rival (`ToStringID`) en `CombatRecord` para reconstruir sin depender del registro (toca serialización + ambos motores + JS del server).

**DEUDA aún abierta (de S24, NO tocada):** `MoriMonchiProceduralAnimator.autoLoopFromMovement` lee `NavMeshAgent.velocity` en `LateUpdate` (viola Regla 1/2). Forma limpia: que `MoriMochiAgent` (dueño del movimiento) empuje `AnimWalk()`/`AnimIdle()` desde su `IsMoving`. Pendiente para próxima sesión.

**Files Created (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatSceneManager.cs` (NUEVO): navegación de escena; cablea el botón `btn-home` de su UIDocument → `ReturnToGameScene` (`Stop()` + `LoadScene(gameSceneName)`).

**Files Touched (.cs — input ScriptNodes):**
- `World/Creatures/MoriMonchiProceduralAnimator.cs`: wrappers `Play*` → `Anim*` (sin colisión con el combat visualizer).
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: cachea `animA`/`animB` + `RestoreAnim` (rewind revive desde el nodo); `statsA`/`statsB` (`EffectiveStats`); `Bind(nombre,atk,vel)` y `PushHp` → `SetHp(hp,max)`.
- `UI/MoriMonchiCombatVisualizerUITK.cs`: API `Bind(nombre,atk,vel)` + `SetHp(actual,max)`; labels `hp-value`/`atk`/`spd`; número animado con el lerp.
- `World/Creatures/NameTag.cs`: nombre coloreado por género en `Bind` (reusa `GenderColor`).

**Files Touched (no-ScriptNode — UXML/USS/prefab):**
- `Resources/Prefabs/MoriMonchiVisualizer.prefab`: recableado completo de los UnityEvents del `MoriMonchiCombatVisualizer` (8 bindings al animator `Anim*`, `OnAttack` deja de auto-apuntarse, `OnHitDealt`/`OnCritDealt` vaciados).
- `UI Toolkit/CombatHpBar.uxml`/`.uss`: `hp-value` dentro del track + fila `stats` (ATK izq / VEL der).
- `UI Toolkit/CombatTopBar.uxml`/`.uss` (NUEVOS): overlay del botón Volver.

**ScriptNodes a actualizar (cierre S25 — agente Haiku):**
- `CombatSceneManager.md` — CREAR.
- `MoriMonchiProceduralAnimator.md` — actualizar (wrappers `Anim*`).
- `CombatVisualizerService.md` — actualizar (`animA`/`animB`+`RestoreAnim`, `statsA`/`statsB`, `Bind`/`SetHp`).
- `MoriMonchiCombatVisualizerUITK.md` — actualizar (`Bind(nombre,atk,vel)`, `SetHp(actual,max)`, labels atk/spd/hp-value).
- `NameTag.md` — actualizar (nombre coloreado por género).

**Next session:**
1. DEUDA: desacoplar `autoLoopFromMovement` (mover el disparo Walk/Idle a `MoriMochiAgent`).
2. (Opcional/futuro) `OpponentDnaId` en `CombatRecord` para replays async.

---

**Session:** 2026-06-25 (Session 24 — Animación procedural del MoriMonchi: `MoriMonchiProceduralAnimator` idle/walk + reacciones) — **✅ RESUELTA EN S25 (era 🟡 bug de cableado abierto)**
**Focus:** Teorizar e implementar animación procedural para que el MoriMonchi se mueva (idle/walk + reacciones de combate), reutilizable en GameScene y escena de combate. Las animaciones se ven bien en Play; falta destrabar el cableado a los UnityEvents.

> ### ✅ Animación procedural — FUNCIONA en idle/walk (S24). 🟡 reacciones pendientes de cablear (bug abajo).
> Juan revisó idle/walk en Play y le gustan. Las reacciones (Attack/Hit/Death/Victory) están implementadas pero el cableado a los UnityEvents tira StackOverflow (ver bug).

**Decisiones de diseño (planeado con Opus + Juan):**
- **Viabilidad ALTA:** el modelo es una jerarquía de Transforms rígidos colgados de 6 sockets (NO skinned mesh / NO bones). Las genéticas combinatorias hacen inviable la animación autorizada (clips por combinación) → procedural es el approach natural. Los Transforms ya estaban expuestos en `MoriMonchiVisualizer` "for future procedural animation".
- **Componente nuevo independiente** `MoriMonchiProceduralAnimator` (una responsabilidad: pose procedural). NO toca `MoriMonchiVisualizer` (ensamblaje/fur) ni `MoriMonchiCombatVisualizer` (hooks Feel). Solo LEE los Transforms públicos del visualizer.
- **Movimiento de cuerpo entero sobre `ModelRoot`** (los 6 sockets son hermanos, no hijos del body → bobear solo el body dejaría los brazos flotando). Se expuso `ModelRoot` en `MoriMonchiVisualizer`. Respiración → escala del body; swing/sway → rotación de brazos; parpadeo → escala Y de ojos.
- **Enum `MMAnimationType { Idle, Walk, Attack, Hit, Death, Victory }` en `Core/Enums.cs`** (regla: TODOS los enums centralizados ahí, no en archivos sueltos). Idle/Walk = loops persistentes; Attack/Hit/Victory = one-shots superpuestos que vuelven al loop; Death = topplea + encoge y se queda (un Idle/Walk lo revive).
- **Todo en `LateUpdate`** (corre tras NavMeshAgent y Assemble), sin coroutines ni eventos que desuscribir. Captura la **rest pose lazy** tras el Assemble y la recaptura si reensambla (detecta cambio de `BodyTransform`).
- **Brazos espejados:** la parte R tiene `scale.x` negativo. Para idle **sincronizado** + walk **opuesto** la solución es que el brazo R use SIEMPRE el signo opuesto al L en ambos modos (se eliminó el toggle `mirroredArms`).
- **API:** `PlayMMAnimation(MMAnimationType)` (código + botón `Test ▶` Odin) + **6 wrappers sin parámetros** (`PlayIdle/PlayWalk/PlayAttack/PlayHit/PlayDeath/PlayVictory`). Razón: los UnityEvent NO serializan argumentos `enum` (solo int/float/string/bool/Object) → solo se cablean métodos sin args. **Odin NO es solución** (no reemplaza el cableado de refs de escena de UnityEvent).
- **Tooltips** en todos los stats (General/Idle/Walk/Reacciones): distancias en m, ángulos en °, tiempos en s, amplitudes como fracción.

**🐞 BUG ABIERTO (próxima sesión) — StackOverflow al disparar PlayAttack:**
- `MoriMonchiCombatVisualizer.PlayAttack()` (`MoriMonchiCombatVisualizer.cs:27`) hace `OnAttack?.Invoke()`. El `UnityEvent OnAttack` quedó cableado a **`MoriMonchiCombatVisualizer.PlayAttack` (a sí mismo)** en vez de a `MoriMonchiProceduralAnimator.PlayAttack` → recursión infinita.
- **Raíz:** colisión de nombres. Ambos componentes viven en el MISMO GameObject del peleador y los dos exponen `PlayAttack()`/`PlayVictory()` → fácil elegir el equivocado en el dropdown del UnityEvent.
- **Fix probable:** renombrar los wrappers del `MoriMonchiProceduralAnimator` (ej. `AnimAttack()`/`PlayAnim*()`) para eliminar la colisión, y recablear los UnityEvents al componente correcto.

**🧹 DEUDA marcada por Juan (código `MoriMonchiProceduralAnimator.cs` L131):** el `autoLoopFromMovement` lee `NavMeshAgent.velocity` para cambiar idle↔walk — acopla la representación con otro sistema (viola Regla 1/2). Se metió como atajo para probar sin cablear. **Forma limpia futura:** el dueño del movimiento (`MoriMochiAgent`) llama `PlayWalk()`/`PlayIdle()` en sus transiciones; el animator solo recibe llamadas (igual que las de combate). Dejar `autoLoopFromMovement` apagado o eliminarlo.

**Files Created (.cs — input ScriptNodes):**
- `World/Creatures/MoriMonchiProceduralAnimator.cs` (NUEVO): pose procedural idle/walk + one-shots, TabGroups Odin, tooltips, `PlayMMAnimation` + wrappers, auto idle/walk por velocidad (deuda), readout `Status` + botón `Test ▶`.

**Files Touched (.cs — input ScriptNodes):**
- `World/Creatures/MoriMonchiVisualizer.cs`: + `public Transform ModelRoot => modelRoot;` (expuesto para el animator).
- `Core/Enums.cs`: + enum `MMAnimationType`.

**ScriptNodes a actualizar (cierre S24 — agente externo):**
- `MoriMonchiProceduralAnimator.md` — CREAR.
- `MoriMonchiVisualizer.md` — actualizar (ahora expone `ModelRoot`).

**Next session:**
1. Resolver el StackOverflow (renombrar wrappers del animator + recablear los UnityEvents al componente correcto).
2. Enganchar los one-shots (`Attack`/`Hit`/`Death`/`Victory`) a los UnityEvents del `MoriMonchiCombatVisualizer`.
3. Desacoplar Walk/Idle del `NavMeshAgent` (deuda L131): mover el disparo al `MoriMochiAgent`.

---

**Session:** 2026-06-25 (Session 23 — Combat Visualizer: hooks Feel reales (MMF) + fix carrera de Awake del combate local) — **✅ CERRADA**
**Focus:** Cerrar el último pendiente del Combat Visualizer (enganchar los feedbacks Feel/MMF) y un bug de combate local en la escena de juego. Se confirmaron cerrados los testeos/limpiezas pendientes de S20-S21.

> ### ✅ Hooks Feel del Combat Visualizer — HECHO (S23, 2026-06-25)
> Decisión de Juan: en vez del bridge `CombatVisualHooks` por-side, una **clase derivada `MoriMonchiCombatVisualizer : MoriMonchiVisualizer`** que vive en el prefab del peleador y expone los `UnityEvent`s (Juan arrastra los MMF; un evento admite varios). Los **`CombatNode`** de la lista enlazada son los que disparan los `Play*` sobre la instancia correcta (atacante/defensor) por fase. Inspector compactado con TabGroups de Odin (Ataque / Recibe / Estado).

> ### ✅ Fix — combate local "No CombatManager config" (S23)
> Era una **carrera de orden de Awake**: `CombatPanelUITK.Awake` cacheaba `config = CombatController.Instance.Config`, pero el `Awake` del controller (que setea `Instance`) podía correr después → `config` null permanente, aunque el SO estuviera asignado. Fix: `config` pasó de campo cacheado a **propiedad lazy** `Config => CombatController.Instance?.Config`. No había cableado cruzado entre escenas.

**Detalle del diseño de hooks (lo vigente):**
- `MoriMonchiCombatVisualizer : MoriMonchiVisualizer` (en `World/Creatures/`). UnityEvents públicos: `OnAttack`/`OnHitDealt`/`OnCritDealt` (Ataque), `OnHitTaken`/`OnCritTaken`/`OnHpChanged` (Recibe), `OnCombatStart`/`OnDead`/`OnVictory` (Estado). `OnHpChanged` usa subclase concreta `HpChangedEvent : UnityEvent<float,float>` para ser cableable en inspector. Métodos `Play*()` invocan cada uno.
- El `CombatNode` guarda `Attacker`/`Defender`/`Crit` y dispara: `FireWindup` (atacante `PlayAttack`), `FireImpact` (hit/crit dealt+taken + `PlayHpChanged`), `FireDeath` (defensor `PlayDead`). El Service resuelve `hooksA`/`hooksB` (instancias derivadas vía `as`) al spawnear, dispara `PlayCombatStart` al instanciar y `PlayVictory` del ganador al final. El rewind (`Restore`) NO dispara juice.
- Bus `CombatVisualEvents` y `CombatVisualHooks` quedan **intactos** (panel + hooks globales/escena opcionales). `CombatVisualHooks` SideA/SideB queda redundante para feedbacks del peleador (ya no hace falta `FeelHooks_A`/`_B` en escena); el `kind=Global` sigue válido para cámara/SFX.

**WIRING UNITY (Juan):** en el prefab del peleador del visualizer, reemplazar el componente `MoriMonchiVisualizer` por `MoriMonchiCombatVisualizer` (re-correr Setup + reasignar `modelRoot` tras el swap), luego arrastrar los MMF a las pestañas. El `visualizerPrefab` del Service NO se toca (la derivada es-un base).

**Files Created (.cs — input ScriptNodes):**
- `World/Creatures/MoriMonchiCombatVisualizer.cs` (NUEVO): derivada con UnityEvents Feel + métodos `Play*`, TabGroups Odin.

**Files Touched (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: `CombatNode` gana `Attacker`/`Defender`/`Crit` + `FireWindup`/`FireImpact`/`FireDeath`; resuelve `hooksA`/`hooksB`; dispara `PlayCombatStart`/`PlayVictory`.
- `UI/CombatPanelUITK.cs`: `config` campo cacheado → propiedad lazy `Config`.
- `UI/CombatPanelUITK.Tabs.cs`: usa `Config` (propiedad) en `DoLocalFight`.

**Cierres confirmados por Juan (S23):**
- ✅ **S21 Generalización de containers**: probado en Play, funciona (store/corral persisten, clear-on-grab, ancla muerta). CERRADO.
- ✅ Combat Visualizer: correcto para etapa de prototipo.
- ✅ Limpieza Unity (componente `CombatHpBarUITK` huérfano + campos viejos del Service): hecha.
- ✅ Cosmético: `NpcState.Spawned` borrado.
- ⏳ Diferido (no bloqueante): botón "Replay" desde el panel de resultados async.

**ScriptNodes a actualizar (cierre S23 — agente Haiku):**
- `MoriMonchiCombatVisualizer.md` — CREAR.
- `CombatVisualizerService.md` — actualizar (nodos disparan feedbacks Feel vía `hooksA`/`hooksB`, `FireWindup`/`Impact`/`Death`, `PlayCombatStart`/`Victory`).
- `CombatPanelUITK.md` — actualizar (`config` lazy vía `CombatController.Instance`, fix carrera de Awake).

---

**Session:** 2026-06-24/25 (Session 22 — Combat Visualizer: cierre completo, probado en Play e iterado con Juan) — **✅ FUNCIONANDO DECENTEMENTE**
**Focus:** Retomar el Combat Visualizer de S18 (replay de un `CombatRecord` en escena, hooks Feel, UI Pokémon-style) que nunca se probó ni se documentó. Se auditó, se reescribió el motor de reproducción y se iteró en varias rondas de Play con Juan hasta dejarlo funcional.

> ### ✅ Combat Visualizer — FUNCIONA (S22, 2026-06-25)
> Juan probó en Play e iteró hasta dejarlo "funcionando decentemente". Doc del vault aprobada y actualizada. Pendiente cosmético/futuro: hooks Feel reales (MMF_Players) + botón "Replay" desde el panel de resultados async.

**RESUMEN FINAL del subsistema (lo vigente):**
- **Motor por lista doblemente enlazada** (`CombatNode` con Prev/Next; sugerencia de Juan): cada nodo = un estado del combate. `current` navega; `head` = inicio. Reemplazó al modelo de índices.
- **Control de reproducción** (panel UITK + DEV harness): arranca en pausa; `TogglePlay` (auto), `Next`/`Back` (manual; Back revive al derrotado), `SetSpeed` 0.25–4x.
- **Slots fijos A=tu MM / B=oponente**; orientación de turnos por `attackerIsSelf = (AttackerIsA == record.SelfWasA)`. Nombre del oponente desde `record.OpponentName`.
- **Barras por referencia directa** (`barA`/`barB`, sin `side`), billboard a cámara (como NameTag), binding resiliente con fix de árbol huérfano del UIDocument al reactivar.
- **Muerte estilo Pokémon**: el derrotado `SetActive(false)`; queda el ganador. Back lo revive.
- **Log en cartas** (ScrollView, caja de tamaño fijo): una carta por turno con color por tipo; nombres azul (local) / rojo (oponente) y **daño en rojo** vía rich-text.
- **DBs por `GameManager.Instance`**; DEV harness sin Rival B (autoresuelve por nombre).

**Historial de bugs resueltos en S22 (Play):**
1. Barra de HP oculta al instanciar → refs lazy + 2 frames antes del Start.
2. Replay espejado → mapeo `SelfWasA` (no swap).
3. Nombre/HP del oponente cruzados → barras driven por referencia directa + nombre del record.
4. Nombre/HP rotos al re-Simular → binding resiliente (reaplica en Update).
5. Oponente de espaldas / barra rota al rotar ancla → billboard de la barra.
6. Derrotado "desaparece y pierde referencia" al retroceder → fix de árbol huérfano del UIDocument (detecta swap en EnsureRefs) + revive vía nodo.

**LIMPIEZA EN UNITY (heredada, si no se hizo):** borrar el componente `CombatHpBarUITK` huérfano (missing script) en el GameObject del `CombatVisualizerPanel` de las escenas; el `MoriMonchiCombatVisualizerUITK` debe ir en un HIJO del prefab (no la raíz) para el billboard; reimportar `CombatVisualizerPanel.uxml`/`.uss`.

---

> ### ⏳ (histórico) Checklist de testeo S22 — ya cubierto en Play

**Estado real del código (evolucionó desde el doc de S18 — esto es lo vigente):**
- **DBs por `GameManager.Instance`** (`Database`/`PartVisualBank`/`FurTypeDatabase`), NO refs serializadas. Las únicas refs del inspector del Service: `visualizerPrefab`, `slotA`, `slotB`, timings.
- **Barra de HP = `MoriMonchiCombatVisualizerUITK`** (componente HIJO del prefab del peleador, recibe el side vía `SetSide` al instanciar). Reemplaza al `CombatHpBarUITK` de S18 (script borrado; el UXML `CombatHpBar.uxml` sigue siendo la barra). YA NO hay que crear GameObjects de HP bar por slot a mano.
- **DEV Test Harness integrado en el Service** (Odin): dropdowns (Combatiente A / pelea / rival B opcional) + "🎲 MM al azar con pelea" + "▶ Simular". NO hace falta dev tool externo ni `CombatDevConsole`.

**Bug encontrado y arreglado (orientación por `SelfWasA`):**
- Cada pelea se persiste en el `CombatHistory` de AMBAS criaturas, cada una desde su POV, con `CombatRecord.SelfWasA` (¿fui yo el combatiente A de la sim?). Los `Turns` son simétricos y su `AttackerIsA` apunta al combatiente A de la simulación.
- `Play` asumía ciegamente `dnaA = combatiente A`. El harness pasa `dnaA = la criatura cuyo historial leés` (= "self"), que solo es A cuando `SelfWasA == true`. → En ~la mitad de los replays (cuando self fue B) la pelea salía **espejada**: animaba al lado equivocado y las barras de HP cruzaban su `HpMax` (podían leer >100%).
- **Fix:** `Play(self, opponent, record)` ahora orienta con `var dnaA = record.SelfWasA ? self : opponent; var dnaB = record.SelfWasA ? opponent : self;`. El `PlayRoutine` queda intacto (sigue asumiendo dnaA = sim-A, que ahora es correcto). Blinda también el futuro botón "Replay" del panel de resultados (tendrá self+opponent+record naturalmente).

**Files Touched (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: `Play(dnaA, dnaB, record)` → `Play(self, opponent, record)` + orientación por `record.SelfWasA`.

**ScriptNodes actualizados (en esta sesión, a mano):**
- `CombatVisualizerService.md` — actualizado (DBs por GameManager, `Play(self,opponent)`+SelfWasA, DEV harness, conexión a MoriMonchiCombatVisualizerUITK).
- `MoriMonchiCombatVisualizerUITK.md` — CREADO (reemplaza al borrado).
- `CombatVisualEvents.md` — conexión `CombatHpBarUITK` → `MoriMonchiCombatVisualizerUITK`.
- `CombatHpBarUITK.md` — BORRADO (script ya no existe).
- `Index/03 - Combat.md` — agregada sección/tabla "Combat Visualizer" + flujo.

**WIRING UNITY (corregido respecto a S18 — bloqueante para Play):**
1. Reabrir Unity → recompila sin errores.
2. Variante de prefab del MM **sin** `MoriMochiAgent` (solo `MoriMonchiVisualizer` con sockets seteados vía botón Setup) → como child, GameObject con `UIDocument` world-space → `CombatHpBar.uxml` + `MoriMonchiCombatVisualizerUITK` (con su `document` asignado). El side lo fija el Service al instanciar; no hay que tocarlo en el prefab.
3. GameObject "CombatVisualizer" con `CombatVisualizerService` + 2 child empty `SlotA`/`SlotB` (~3-4m en X). Asignar `visualizerPrefab`, `slotA`, `slotB`. **NO** hay refs de DB (salen de GameManager).
4. GameObject "CombatVisualizerPanel" con `UIDocument` (screen-space) → `CombatVisualizerPanel.uxml` + `CombatVisualizerPanelUITK`.
5. (Opcional, Test 9) `FeelHooks_Global`/`_A`/`_B` con `CombatVisualHooks` (HookKind respectivo).
6. Cámara apuntando entre los slots.

**TESTEO PENDIENTE S22 (Juan, en Play) — disparo desde el DEV harness del Service:**
1. **Poblar historial:** que haya ≥2 MM con `CombatHistory` con turnos (si no, simular una pelea local con `CombatDevConsole`).
2. **Disparo:** en el inspector del `CombatVisualizerService` (en Play) → FoldoutGroup "DEV — Test Harness" → "🎲 MM al azar con pelea" → "▶ Simular".
3. **Spawn:** aparecen 2 modelos ensamblados en A/B; el fur coincide con `BaseColor` (regresión invariante color↔identidad S15).
4. **HP bars:** 2 barras con el `CustomName` correcto al 100%; bajan con lerp suave.
5. **Orientación (EL FIX):** elegí a propósito una pelea donde la criatura A **perdió/fue B** → confirmar que ataca el lado correcto y la barra que baja es la del que recibió (NO espejado). Repetir con varias peleas.
6. **Log/header:** header "Turno K / N"; log "VS…", "Turno k · X→Y", "Daño/¡Crítico!", "X cae derrotado.", "Ganador/Empate"; respeta `maxLogLines` (6).
7. **KO/empate/crítico:** KO → `OnDead` una vez; MaxRounds sin KO → "Empate"; crítico → "¡Crítico!".
8. **Replay consecutivo:** "▶ Simular" con otro corriendo → `Stop()` limpia sin huérfanos; al terminar (`endHoldSeconds` 1.5s) se destruyen los 2.
9. **Hooks (si se cablearon):** Global dispara Start/Turn/End/Log; SideA/B solo su lado, sin cruce.
10. Si todo OK → marcar ✅ el Combat Visualizer (1er sistema de S18 cerrado).

---

### S22 — CONTINUACIÓN (post-Play): fixes + control de reproducción manual/auto + muerte estilo Pokémon

Juan probó en Play: **andaba a grandes rasgos**. Pidió fixes y features. Se rediseñó el motor de reproducción a **por pasos con snapshots** (única forma de soportar avanzar Y retroceder consistente).

**Cambios de diseño (todos pedidos de Juan):**
1. **Bug barra de HP oculta al instanciar — RESUELTO.** Causa: el `Start()` de `MoriMonchiCombatVisualizerUITK` corría DESPUÉS de que el Service disparaba `OnVisualCombatStart` (Instantiate no ejecuta Start en el acto) → handler con `root==null`, y luego `Start()` la ocultaba para siempre. Fix: refs **lazy** (`EnsureRefs()` en los handlers) + 2 `yield return null` en `BeginRoutine` antes de disparar Start + se eliminó toda la lógica de ocultar/mostrar (la barra es visible mientras su GO esté activo).
2. **Slots fijos A = tu MM (`self`), B = oponente** (se REVIRTIÓ el swap de la primera parte de S22). La orientación correcta de turnos se mapea con `attackerIsSelf = (turn.AttackerIsA == record.SelfWasA)` al construir los frames. `Play(self, opponent, record)` ya NO hace swap.
3. **El `side` dejó de ser campo de inspector** en `MoriMonchiCombatVisualizerUITK` (lo asigna el Service vía `SetSide`).
4. **Reproducción controlable (no auto-pasa):** arranca **en pausa** en step 0. Controles: `TogglePlay/SetAuto` (auto con `playbackSpeed`), `Next`/`Back` (paso manual; `Back` revive al derrotado), `SetSpeed` (0.25x–4x divide los timings).
5. **Muerte estilo Pokémon:** al llegar a 0 el defensor, tras `deathPauseSeconds` se hace `SetActive(false)` (desaparece); al final queda el ganador. Rewind lo revive.
6. **Controles en el panel UITK** (`CombatVisualizerPanel.uxml`): ◀ / ▶❚❚ / ▶▶ + slider "Velocidad". El panel se reconstruye entero desde `OnPanelState` (snapshot). Los botones llaman a `CombatVisualizerService.Instance` (servicio explícito, permitido).

**Motor (Service):** `Frame` por turno (HpA/HpB, ADead/BDead, nº turno, log acumulado). `ForwardRoutine` = transición con juice (windup/hit/crit/hp tween/muerte); `ApplyFrame`/`RestoreTo` = estado puro (rewind, sin juice). Nuevo evento `CombatVisualEvents.OnPanelState` + DTO `CombatVisualPanelState`.

**Files Touched (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatVisualEvents.cs`: + `CombatVisualPanelState` + evento `OnPanelState`.
- `Systems/CombatVisualizer/CombatVisualizerService.cs`: reescrito — motor por pasos, control API, A=self/B=opp + mapeo SelfWasA, muerte-desaparece, DEV harness con botones de control.
- `UI/MoriMonchiCombatVisualizerUITK.cs`: refs lazy (fix barra), quitado `side` serializado + lógica de visibilidad.
- `UI/CombatVisualizerPanelUITK.cs`: render desde `OnPanelState`, controles cableados al servicio.
- (no-ScriptNode) `CombatVisualizerPanel.uxml`/`.uss`: fila de controles + slider + estilos.

**LIMPIEZA EN UNITY PENDIENTE (Juan):**
- En las escenas `CombatVisualizerMM.unity` y `TestScene.unity`, el GameObject del `CombatVisualizerPanel` tiene un componente **`CombatHpBarUITK` huérfano (missing script, `document:0`)** — borrarlo (no hacía nada; las barras reales están en el `visualizerPrefab`).
- El Service en escena tiene `endHoldSeconds` serializado (campo eliminado, Unity lo ignora). Revisar en el inspector los nuevos: `deathPauseSeconds` (0.6), `playbackSpeed` (1).
- En el prefab del peleador, confirmar que la HP bar es `MoriMonchiCombatVisualizerUITK` con su `UIDocument` asignado (el side lo pone el Service solo).

**TESTEO PENDIENTE S22-cont (Juan, Play):** disparar con "▶ Simular"; (a) la barra de HP ahora SÍ se ve al instanciar; (b) arranca pausado → ▶ reproduce, ❚❚ pausa; (c) ◀/▶▶ pasan turnos a mano (con un combate pausado); (d) el slider cambia la velocidad del texto; (e) el derrotado desaparece y queda el ganador; (f) ◀ desde el final revive al derrotado y rebobina HP+log; (g) probar una pelea donde tu MM fue B → no espejado.

---

### Sesión 21 (histórico) — 2026-06-24

**Session:** 2026-06-24 (Session 21 — Generalización de containers: ancla de ubicación persistida + spawn por colocación-primero) — **CÓDIGO HECHO, TESTEO PENDIENTE (se prueba la próxima sesión)**
**Focus:** Cambio arquitectónico estilo Palworld: que los MoriMonchis retomen lo que estaban haciendo al cargar la partida. Se generalizó el patrón de reclaim que SOLO tenía el breeding a TODOS los containers (breeding / store / corral) vía un contrato `IAnchorPlace` + `AnchorRegistry`, y se invirtió el spawner a "colocación primero" (el cañón queda solo para criaturas libres). Resuelve la persistencia faltante del store y ataca la raíz de la familia de bugs de carga en frío (la carrera cañón-vs-reclaim deja de existir).

> ### ✅ CERRADO — Generalización de containers (S21, probado en Play en S23, 2026-06-25)
> Juan probó en Play: funciona. Store/corral persisten tras reload, clear-on-grab no re-shelvea, ancla muerta cae al cañón sin defer infinito, regresión de breeding frío intacta. Checklist de abajo cubierto.

**Concepto (decisión de diseño, planeada con Opus + Juan):**
- **Costo de nube ≈ cero:** el registro se sube como UN blob de una sola key (`CloudSyncService.Sync.cs`). El `CreatureDNA` ya viaja entero (Needs, CombatHistory, BusyState). Sumar el ancla = decenas de bytes por criatura, sin operaciones extra. El techo real a vigilar es `CombatHistory` ilimitado, no el ancla.
- **Local vs nube: ya es ambos** automáticamente (todo lo que vive en `CreatureDNA` ride el JSON local + Cloud Save). NO se separa. Lo de alta frecuencia (posición de merodeador, sub-FSM) NO se persiste ni dispara push — el ancla se estampa solo en transición (entrar/salir).
- **El costo real estaba en el World layer, no en la nube:** el cañón dispersaba a todos y cada container tenía que recuperarse con su propia coroutine de poll (la causa de los bugs S14-S16). Invertir a colocación-primero **borra** esas carreras.

**Contrato nuevo (las 4 semánticas de ocupación):**
- **Entrar** (tirado adentro) → `Admit`: estampa `LocationKey/LocationSlot` + `RegistryChanged`. ✅ persiste.
- **Salir** (lo levanta el jugador) → `Release`: limpia el ancla + `RegistryChanged`. ✅ persiste.
- **Reclaim en carga** → `TryReclaim`/`AnchorPosition`: coloca directo, ❌ NO persiste (ya estaba estampado).
- **Lifecycle** (pool/re-init) → `DetachOccupant`: saca del censo, ❌ silencioso (no persiste, no cancela cría).

**Decisiones puntuales:**
- `HomePenKey/HomePenSlot` → renombrados a `LocationKey/LocationSlot` (genéricos). **Caveat de migración aceptado por Juan:** saves viejos con breeders a mitad de incubación pierden el ancla en la 1ª carga post-update (se re-dispersan una vez; hay botón Reset).
- El ancla (DÓNDE) es genérica en la base; la ACTIVIDAD (cortejo/incubación) sigue siendo propia del `BreedingContainer` (`BreedPartnerID`/`BreedReadyAt` intactos).
- `StoreContainer` gana persistencia **gratis por herencia** (cero lógica nueva) — resuelve el gap del store.
- Ancla huérfana (furniture removida mientras no estabas): el spawner cae al cañón y **limpia** `LocationKey` (sin defer infinito).
- Re-ancla tras pull de nube en `OnRegistryReloaded` (generaliza la deuda D de S16).
- **Sin wiring nuevo en Unity:** la clave sale del `PlacedFurnitureMarker` existente; `AnchorRegistry` se auto-registra. Única precondición (ya existía): piso del container pintado con el área de confinamiento + bakeado.

**Fuera de scope (Fase 2, infra ya lista):** camas persistentes + energía offline. La cama es un `NeedStation`, no un container; persistirla = sumarle `IAnchorPlace` a `NeedStation`, y el "siguieron descansando mientras no estabas" es matemática de `sync_meta` en la carga (casi gratis). No se metió ahora para no expandir el blast radius dentro del FSM de needs.

**Files Created:**
- `World/Containers/AnchorRegistry.cs` (interface `IAnchorPlace` + registro estático espejo de `NeedStationRegistry`).

**Files Touched (.cs — input ScriptNodes):**
- `Data/Genetics/CreatureDNA.cs`: `HomePenKey/HomePenSlot` → `LocationKey/LocationSlot` (significado genérico).
- `World/Containers/MoriMochiContainer.cs`: implementa `IAnchorPlace`; deriva `AnchorKey` del marker en `Start` + auto-registro/desregistro; `Admit` estampa+persiste; `Release` limpia+persiste (solo grab del jugador); nuevo `DetachOccupant` silencioso; `TryReclaim`/`AnchorPosition`.
- `World/Containers/BreedingContainer.cs`: borrado `penKey`/`byKey`/`TryGet`/`ReclaimDirect`/`ReclaimBreedingOccupants` (+ campo `reclaimTimeout`); `Start`/`OnDestroy` → `protected override` que llaman `base`; `All` respaldado por lista propia; usa `AnchorKey`; renombres DNA; `ClearBreed` conserva el clear de `LocationKey/Slot` (cubre a la pareja no-agarrada).
- `World/Containers/StoreContainer.cs`: hereda persistencia de ancla sin lógica nueva; rename interno `occupants`(NpcAgent[]) → `usePointOccupants` (evita choque con la base).
- `World/AI/MoriMochiAgent.Confinement.cs`: `RestoreNavMeshControl` + `PrepareForPool` → `DetachOccupant` (silencioso) en vez de `Release` (evita push fantasma + cancelación de huevo al reciclar). `OnGrab` (Physics) intacto con `Release`.
- `World/Spawning/MoriMochiSpawner.cs` (+`.Debug.cs`): `breederQueue`→`anchoredQueue`, ruteo por `LocationKey`, `TryPlaceAtAnchor`/`DeferAnchored` genéricos vía `AnchorRegistry`, clear de ancla huérfana, re-ancla en reload. Cañón/balística/recién nacidos intactos.
- (colateral) `Systems/Breeding/BreedingDevConsole.cs`: `pen.PenKey` → `pen.AnchorKey`.

**TESTEO PENDIENTE S22 (Juan, en Play):**
1. **Compila** sin errores (carpeta/clase nueva genera su `.meta`).
2. **Regresión breeding frío:** pareja incubando carga → cortejo orbita → eclosiona (no romper el fix S16).
3. **Store persiste (NUEVO):** MM al estante → quit → reload → vuelve al estante con su precio.
4. **Corral persiste (NUEVO):** MM al corral → reload → sigue adentro.
5. **Clear-on-grab:** sacar un MM del estante → reload → NO se re-shelvea.
6. **Ancla muerta:** borrar la furniture con un MM anclado → reload → sale por cañón y queda libre (sin defer infinito).
7. Si todo OK → marcar ✅ y pasar a Fase 2 (camas persistentes + energía offline) o al Combat Visualizer (S18, aún sin Play).

**ScriptNodes a actualizar (cierre S21 — agente haiku):** `AnchorRegistry.md` (CREAR), `MoriMochiContainer.md`, `BreedingContainer.md`, `StoreContainer.md`, `MoriMochiAgent.md` (Confinement), `MoriMochiSpawner.md`, `CreatureDNA.md`.

---

### Sesión 20 (histórico) — 2026-06-23

**Session:** 2026-06-23 (Session 20 — Sold + diálogo NPC + rediseño cola + nombres + outbid + variación + banco de diálogos + cerco de áreas) — **PROBADO EN PLAY ✅**
**Focus:** (1) Completar el estado Sold como Dead (timestamp, filtros, `IsSold`). (2) Diálogos del `NpcThoughtTag` (feliz al vender, "muy lleno" al no caber). (3) **Rediseño total de la cola de la caja** (varias iteraciones con Juan): de árbol ternario BFS → cadena lineal ortogonal que tiende a la salida. (4) Nombres random de NPC.

> ### ✅ CIERRE — Sistema NPC compradores CERRADO (2026-06-23)
> Juan probó en Play: **funciona suficientemente bien**, se cierra el tema (primer mecanismo de monetización live). Entregado en S17-S20: use points anti-overlap, precio por NameTag, cola lineal ortogonal hacia la salida con avance, estado Sold=Dead, reacción "¡Me ganaron!" (outbid), variación NavMesh + ReactionDelay + banco de diálogos, cerco de áreas caminables. **Bug de carga en frío del breeding (S16): también RESUELTO** (ver [[Index/11 - Technical Debt]], causa = refresh de data al reclamar al corral).
> **Siguiente foco:** Combat Visualizer (S18) — único sistema codificado SIN testear en Play (checklist Test 1-8 más abajo). Cosmético suelto: borrar `NpcState.Spawned`.

**Decisiones de arquitectura — Cola (S20, diseño FINAL tras iterar con Juan):**
- **Causa original:** árbol ternario BFS (`QueueSlotNode` root→Back/Left/Right por nivel) → repartía en abanico, no en fila. + bug latente S17: `TickQueueing` cacheaba el slot una sola vez y nunca repolleaba → al avanzar la cola los de atrás no se movían.
- **Modelo final (cadena lineal por responsabilidades, guía explícita de Juan):** `CashRegister` es **dueño del orden** (`List<Link>{Agent,Pos}`, frente = índice 0). Por cada cliente pide candidatos a dos handlers puros y entrega el `Vector3` al `NpcAgent` (que solo camina).
  - **`QueueDirectionHandler` (puro `[Serializable]`):** `Candidates(anchorPos, backAxis, spacing, outBuf)` → 3 candidatos **estrictamente ortogonales (90°)** en orden **Atrás → Izquierda → Derecha**, todos relativos a un **eje fijo** (no al diagonal del anterior). Atrás siempre preferido.
  - **`QueueAvailabilityHandler` (puro `[Serializable]`):** `IsAvailable(from, candidate, areaMask, sampleRadius, maxSnap, occupied, minSeparation, out snapped)` → cae en NavMesh (`SamplePosition`) + **no se desvió más de `maxSnap` al pegarse** (si el mesh válido queda lejos → estaba sobre un obstáculo → se descarta) + camino libre desde el anterior (`NavMesh.Raycast`) + no se solapa (`minSeparation`).
  - **Dirección de la cola = `BackDirection()`:** apunta del frente hacia la **salida** (`queueTowards` serializado → fallback `NpcController.Instance.ExitPoint` → fallback alejándose de la caja), luego **snappeada a la dirección ortogonal más cercana** (`SnapToOrthogonal`, entre ±forward/±right de la caja). Así la fila se forma hacia afuera como en la vida real PERO con pasos 90° puros (sin diagonales). **Regla 1 de Juan: tender siempre a la salida.**
  - **Regla 2 de Juan (sin espacio → se va):** `TryComputeLink` devuelve `bool`; si ningún candidato pasa la validación NO inventa posición (saqué el fallback que forzaba un lugar malo) → `TryReserveSlot` devuelve `null` → `NpcAgent.QueueWasFull = true` → se va y el thought tag dice **"¡Está muy lleno!"**. En `Recompute` los ya-en-fila conservan su lugar si no se recalcula (no se evicta a nadie a mitad).
  - **Avance arreglado:** `TickQueueing` repollea `register.CurrentSlotOf(this)` cada frame; si difiere >0.2m actualiza destino. `ReleaseSlot` → `Recompute()` rearma la cadena frente-a-atrás.
  - **Detección por NavMesh** (no físicas), consistente con los use points.
  - Tunables `CashRegister`: `queueRoot`, `queueTowards`, `slotSpacing`, `sampleRadius`, `maxSnap` (0.5), `minSeparation`, `maxQueueDepth` (largo máx). Gizmos: flecha azul larga (dirección a la salida) + cadena (frente verde, resto ámbar).
  - API pública INTACTA → `TransactionPanelUITK`/`NpcController` no se tocan.
- **Nombres random NPC:** `NpcNameBank` (estático, espejo de `CreatureNameBank`): nombre+apellido humanos ES. `NpcAgent.DisplayName` asignado en `Initialize`; el `NpcThoughtTag` muestra el nombre del cliente (fallback arquetipo/"Cliente").

**Decisiones de arquitectura (S20):**
- **`IsSold` como propiedad derivada** (no bool separado): `public bool IsSold => BusyState == BusyReason.Sold;` en `CreatureDNA`. El dato ya existe en `BusyState`; no duplicar. Diferencia con `IsDead` (que es bool separado): `IsDead` se setea independiente del BusyState (un MM puede morir sin pasar por un estado Busy explícito), pero `Sold` SIEMPRE pasa por `BusyReason.Sold`. Derivar evita desync.
- **`SaleDate`**: `public DateTime SaleDate;` en `CreatureDNA`, paralelo a `QueuedAt`. Se estampa en `NpcAgent.AcceptCurrentOffer()` con `DateTime.UtcNow`.
- **Diálogo Leaving post-venta**: `NpcThoughtTag.ThoughtText` distingue `Leaving` → si `target.IsSold` → `"¡{targetName} se viene conmigo!"`, sino `"Será en otra ocasión…"`. Sin evento extra, sin estado adicional: el dato ya está en el DNA del `TargetMM`.
- **Spawner**: todos los checks `d.IsDead` extendidos a `d.IsDead || d.IsSold` (6 lugares: 2× `.Where`, 3× `if continue`, 1× guard en pump).

**Files Created:**
- `World/Containers/QueueDirectionHandler.cs` (handler puro: 3 candidatos ortogonales Atrás/Izq/Der relativos a un eje fijo).
- `World/Containers/QueueAvailabilityHandler.cs` (handler puro: NavMesh + maxSnap + raycast camino libre + separación).
- `World/Npc/NpcNameBank.cs` (estático: nombre+apellido humanos ES, espejo de `CreatureNameBank`).
- `World/Npc/NpcDialogueBank.cs` (estático, S20-cont: 5-6 frases por situación con `{0}`=nombre del MM; `Pick(state, reason, name)`).

**Files Touched (.cs — input ScriptNodes):**
- `Data/Genetics/CreatureDNA.cs`: + `SaleDate` (DateTime) + `IsSold` (propiedad derivada de BusyState).
- `World/Npc/NpcAgent.cs`: + `using System;`; stamp `TargetMM.SaleDate` en `AcceptCurrentOffer`; `TickQueueing` repollea `CurrentSlotOf` (fix bug S17); + `DisplayName` (random en `Initialize`). **S20-cont:** `QueueWasFull` → enum `LeaveReason {None,Purchased,Outbid,QueueFull}` + propiedad `Reason` (dueño del "porqué me voy"); se suscribe a `GameEvents.OnCustomerSold` (`OnEnable`/`OnDisable`) → si el MM vendido por OTRO == su `TargetMM` ⇒ `Reason=Outbid` + `Leaving`; `AcceptCurrentOffer` setea `Reason=Purchased`; `ApplyInstanceVariation` (en `Initialize`) sortea speed/angularSpeed/acceleration (`moveVariation`), `avoidancePriority` y `ReactionDelay`.
- `World/Npc/NpcThoughtTag.cs`: nameLabel = `agent.DisplayName`. **S20-cont:** se eliminó `ThoughtText` fijo → `UpdateThought` lee `NpcDialogueBank.Pick(State, Reason, name)`, detecta cambio de situación, cachea la frase y la muestra recién tras `agent.ReactionDelay` (mantiene la frase previa durante el delay; sin blanco). Distingue comprador (Purchased "se viene conmigo") de perdedor (Outbid "¡Me ganaron a X!").
- `World/Containers/CashRegister.cs`: árbol BFS → cadena lineal ortogonal hacia la salida (dueño del orden, 2 handlers, `BackDirection`+`SnapToOrthogonal`, `queueTowards`, `TryComputeLink` bool, `Recompute`, `maxSnap`, gizmos). API pública intacta.
- `World/Spawning/MoriMochiSpawner.cs`: 6 checks `IsDead` → `IsDead || IsSold`.
- `UI/CreatureGridUITK.cs` / `CreatureVisualUI.cs` / `CreatureGridView.cs` / `MorimonchiDetailInfoUITK.cs`: `StateOf` + `"SOLD"` antes de `"DEAD"`.

**PASOS MANUALES PENDIENTES (Juan, Unity — heredados S19):**
1. Prefab NPC: en el child que tenía `NpcStatusBarUITK` (script faltante ahora), poner `NpcThoughtTag` + UIDocument Source = `NpcThought.uxml` (mantener `WorldUIPanelSettings`), posicionar sobre la cabeza.
2. Cada `StoreContainer`: agregar `usePoints` (child empties sobre el NavMesh; gizmos amarillos guían) + componente `StoreContainerDebug`. **Piso pintado con el área de confinamiento + bakeado** (si no, el MM no es admitido → sin precio).
3. `NpcController`: ya no tiene `displays` (auto-registro). Confirmar `register`/`spawnPoint`/`exitPoint`/`defaultAgentPrefab`.
4. Panel transacción: reimportar UXML/USS; confirmar slot `Transaction → GameObject` en UIManager + `PanelTrigger(Transaction)` + collider/layer (en `grabMask`) en la caja.
5. **Cola hacia la salida:** la grilla de la cola se alinea a la orientación de la `CashRegister` (rotar la caja rota los 4 ejes posibles). La dirección la elige sola hacia la salida: si el `NpcController` tiene `exitPoint`, no hace falta cablear nada; si querés apuntar a la puerta (≠ punto de despawn), creá un empty y arrastralo a `queueTowards`. La **flecha azul larga** del gizmo confirma hacia dónde crece la fila. NavMesh bakeado en esa zona. Tunables si hace falta: `slotSpacing`, `maxSnap` (sube si esquiva de más, baja si atraviesa muebles), `minSeparation`, `maxQueueDepth`.

**ScriptNodes Actualizados (fin de sesión 20 — documentación mecánica):**
- `CashRegister.md` — actualizado: cadena lineal ortogonal, `BackDirection`, `SnapToOrthogonal`, `maxSnap`, gizmos azul+cadena.
- `QueueDirectionHandler.md` — actualizado: 3 candidatos ortogonales estrictamente 90° (Atrás/Izq/Der).
- `QueueAvailabilityHandler.md` — actualizado: `maxSnap` como validación de desviación, raycast camino libre.
- `NpcAgent.md` — actualizado: `DisplayName`, `QueueWasFull`, `TickQueueing` repollea slot, `AcceptCurrentOffer` estampa `SaleDate`, conexión a `NpcNameBank`.
- `NpcThoughtTag.md` — actualizado: nameLabel = `agent.DisplayName`, `ThoughtText` toma `queueWasFull`, diálogo "¡Está muy lleno!", conexión a `NpcNameBank`.
- `NpcNameBank.md` — CREADO: clase estática, 40 nombres + 40 apellidos ES, `GetRandomName()`.

**NEXT SESSION (21) — PENDIENTE DE TESTEO EN PLAY (todo el código S19+S20 está hecho, nada testeado salvo el panel de transacción de la S19):**
1. **S19:** use points (no overlap al inspeccionar), fix del atasco "Mmm déjame ver…", precio en NameTag de los MM en venta, thought tag por NPC.
2. **S20 — Sold:** al aceptar oferta el MM queda `Sold` con `SaleDate`; desaparece del mundo (no respawnea) y muestra "SOLD" en el grid/detail; el vendedor sale feliz ("¡X se viene conmigo!").
3. **S20 — Cola:** (a) 2-3 clientes forman fila recta **hacia la salida** (flecha azul); (b) pasos **90° puros** (atrás/izq/der, sin diagonales) al rodear un obstáculo; (c) al vender/irse el frente los de atrás **AVANZAN** un lugar (bug S17); (d) **Regla 2:** si no hay espacio válido el cliente se va diciendo "¡Está muy lleno!" (probar saturando la zona de cola o tapándola).
4. **S20 — Nombres:** cada NPC muestra un nombre random (nombre+apellido) en su thought tag.
5. Si todo OK → marcar ✅ el sistema NPC compradores.
6. Volver al bug pendiente de S16 (1ª carga en frío del breeding cortejo — abierto en [[Index/11 - Technical Debt]]).

**S20 — CONTINUACIÓN (mismo día 2026-06-23): reacción "me ganaron" + variación por NPC + banco de diálogos**

Pedido de Juan tras la auditoría: escenario "10 clientes quieren el mismo MM" + dar vida a los NPCs.

- **Reacción en cadena al vender (bug + feature):** la lógica vieja decidía el diálogo de salida por `target.IsSold`, pero TODOS los que querían ese MM lo tienen en `true` → todos decían "se viene conmigo". Fix: el "porqué me voy" pasa a ser **un dato dueño del agente** (`enum LeaveReason`, propiedad `Reason`). Cada `NpcAgent` se suscribe al `Action` existente `GameEvents.OnCustomerSold` (reuso, Regla 10); si `buyer != this` y `mm == TargetMM` (igualdad por referencia, `CreatureDNA` no sobreescribe `==`) ⇒ `Reason=Outbid` + `Leaving`. Comprador ⇒ `Reason=Purchased`. Guardas: ignora si es el comprador o ya está en `Leaving`. Suscripción en `OnEnable`/`OnDisable` (Regla 9, NPCs se `Destroy`ean al despawn).
- **Variación del NavMeshAgent al instanciar** (`ApplyInstanceVariation` en `Initialize`): sortea por separado `speed`/`angularSpeed`/`acceleration` (tunable `moveVariation`, ±15% def, piso 0.1) + `avoidancePriority` (rango `avoidancePriorityRange`, def 30-70). Comportamiento → vive en el agente.
- **`ReactionDelay` random por NPC** (`reactionDelayRange`, def 0.2-1.2s) + **`NpcDialogueBank`**: el `NpcThoughtTag` elige una frase random del banco por situación `(State, Reason)`, la cachea (no re-sortea cada frame) y la muestra recién tras el delay, manteniendo la frase previa mientras "piensa" (sin parpadeo en blanco). Delay aplicado a la **respuesta verbal**, no al comportamiento. Presentación → vive en el thought tag.
- **Auditoría previa (sin cambios de código):** se verificó que el path "Sold mientras está en estante" está limpio (`RegistryChanged`→`Sync`→`Despawn`→`pool.Return`→`PrepareForPool`→`currentContainer.Release` → sale de `Occupants`, NameTag se va con el GameObject) y que el fix de avance de cola S17 es correcto. Cosmético pendiente: `NpcState.Spawned` sigue en el enum (inofensivo).

**Nuevos ítems de test S21 (sumar):**
- **Outbid:** varios clientes en cola apuntando al MISMO MM (un solo MM en venta, precio atractivo) → vender al frente ⇒ el resto dice "¡Me ganaron a X!" y se va. Si toda la cola quería ese MM, la fila se vacía y el panel se cierra (esperado).
- **Variación:** los clientes se mueven a velocidades/giros distintos y se esquivan con prioridades distintas.
- **Delay + banco:** las frases varían por cliente y por situación, aparecen tras un beat random, sin respuestas vacías. **Caveat de wiring:** `NavMeshAgent.stoppingDistance` del prefab NPC en ~0 (si ≥0.5 el cliente del frente nunca llega a `WaitingAtRegister`).

**POST-TEST (mismo día 2026-06-23) — Juan probó en Play: el sistema NPC funciona correctamente.** Único ajuste pedido: **reforzar las áreas caminables**. Los NPCs a veces ruteaban por el breeding room para acortar camino.
- **Cerco de áreas (`NpcAgent`):** nuevo campo serializado `walkableAreaNames` (`List<string>` con `[ValueDropdown]` de los nombres reales de Navigation, default `ShopFrontDesk` + `Outside`; helper `EditorNavMeshAreaNames` espejo del de `MoriMochiAgent`). `ApplyWalkableAreas` (en `Initialize`) lo convierte en `navAgent.areaMask` → el pathfinding nunca sale de esas áreas. Fallback a `AllAreas` si la lista está vacía o los nombres no existen (no congela al NPC). `GetAreaFromName` es case-sensitive.
- **Cola hereda la máscara:** `NpcAgent.AreaMask` (propiedad pública) → `CashRegister.TryComputeLink` muestrea los slots con `agent.AreaMask` en vez de `NavMesh.AllAreas` (single source of truth, sin config duplicada).
- **Paso manual Juan:** en el prefab NPC → `NpcAgent` → "Walkable areas", confirmar las 2 áreas exactas desde el dropdown; ambas pintadas/bakeadas/conectadas.

**ScriptNodes PENDIENTES de re-doc (S20-cont):** `NpcAgent.md` (re-update: `LeaveReason`/`Reason`, suscripción `OnCustomerSold`, `ApplyInstanceVariation`, `ReactionDelay`, `walkableAreaNames`/`ApplyWalkableAreas`/`AreaMask`), `NpcThoughtTag.md` (re-update: `UpdateThought` + banco + delay), `NpcDialogueBank.md` (CREAR), `CashRegister.md` (re-update: cola muestrea con `agent.AreaMask`).

---

### Sesión 19 (histórico) — 2026-06-22

**Session:** 2026-06-22 (Session 19 — Sistema NPC compradores: wiring en Play + precio por MoriMonchi + thought tag por NPC + UX panel transacción + use points anti-overlap + auto-registro de estantes) — **CÓDIGO HECHO, panel transacción confirmado visualmente en Play; resto pendiente de testear**
**Focus:** Retomar el wiring/testeo en Play del sistema de NPCs de la S17. Se resolvieron bugs de wiring (panel no abría, price tag no salía, status bar invisible) y se aplicaron mejoras de arquitectura pedidas por Juan (use points, auto-registro, thought tag por NPC).

**Decisiones de arquitectura (Juan):**
- **Precio = opción B (por MoriMonchi, NO por estante):** el `StoreContainer` vuelve a ser SOLO contenedor/interactuable; el precio sale en el `NameTag` propio de cada MM cuando está en venta. Eliminado el `StoreContainerPriceTag` (cartel-lista del estante) + su evento `OnDisplayContentsChanged`/polling en `StoreContainer`.
- **Estado de venta por NameTag:** nuevo `MoriMochiAgent.IsForSale => currentContainer is StoreContainer`; `NameTag` agrega layout "tienda" (`RefreshStore`: nombre + precio via `CustomerService.EstimateAverage`), branch ANTES del layout de cría (que también es `IsPenned`).
- **Thought tag por NPC (no barra standalone):** reemplazado `NpcStatusBar` (overlay agregado tipo HUD) por `NpcThoughtTag` (world-space por NPC, patrón `NameTag`: billboard + distance-gate, lee `NpcAgent.State` vivo, diálogos ES por estado, se auto-bindea con `GetComponentInParent<NpcAgent>`). Razón de Juan: consistencia con el patrón establecido + inmersión + cada NPC tiene su pensamiento.
- **Panel transacción:** layout 3 columnas (cliente | swatch `BaseColor` + nombre + género/edad | `+oferta` price-tag verde) + 3 botones (Cancelar/Aceptar/Pedir más). **Fix de ciclo de vida:** el panel se oculta por `display` (no `SetActive`), así que `OnEnable` corría 1 sola vez → ahora detecta abrir/cerrar por el estado real de `display` en `Update` → `EnterNegotiating`/`ExitNegotiating` robusto (incluido ESC). **Fix visual:** faltaba `<Style src="TransactionPanel.uss">` en el UXML (se veía sin estilos) + backdrop `position:absolute inset:0` (patrón `CombatPanel`) para centrar apaisado (antes `flex-grow` lo clampeaba a lo alto).
- **Auto-registro de estantes (`StoreDisplayRegistry`):** espejo de `NeedStationRegistry`. Los `StoreContainer` se auto-registran en `OnEnable`/`OnDisable`; `NpcController` ya NO tiene `displays` serializada, le pasa `StoreDisplayRegistry.All` (lista viva) a los NPCs en `Initialize`. Resuelve orden de spawn + furniture colocada en runtime.
- **Use points anti-overlap (igual que los feeders/`NeedStation`):** `StoreContainer.usePoints` + `TryReserveUsePoint`/`ReleaseUsePoint` (1 NPC por punto, snap a NavMesh via `NavMesh.SamplePosition`). El NPC reserva el punto libre más cercano y alcanzable; si ninguno en ningún estante → se va. Reemplaza el standoff geométrico (causa raíz del atasco "Mmm, déjame ver…" eterno: el punto caía off-mesh bajo el estante). Release en transiciones `ApproachingRegister`/`Leaving`, re-wander y `OnDisable`.
- **Fix llegada inspección:** `TickInspecting` usa `remainingDistance` (path-based) en vez de `Vector3.Distance` al destino, consistente con las demás fases.
- **Debug del StoreContainer (`StoreContainerDebug`, patrón F3 solo API pública):** listas en vivo de MMs adentro (nombre/género/busy/precio) + NPCs interactuando (arquetipo/estado/target, filtra por `NpcAgent.CurrentDisplay == container`) + botón "Spawn Test Customer" (`NpcController.ForceSpawn`).

**Confirmado en Play (Juan):** el panel de transacción se ve correcto (header dorado, 3 columnas con divisores, swatch enmarcado, price-tag verde, botones coloreados, centrado apaisado).

**Files Created:**
- `World/Npc/NpcThoughtTag.cs`
- `World/Containers/StoreDisplayRegistry.cs`
- `World/Containers/StoreContainerDebug.cs`
- `UI Toolkit/NpcThought.uxml`, `UI Toolkit/NpcThoughtStyle.uss`

**Files Touched (.cs — input ScriptNodes):**
- `World/AI/MoriMochiAgent.Brain.cs`: + `IsForSale`.
- `World/Creatures/NameTag.cs`: layout tienda (precio) + `RefreshStore` + `price-label` (oculto en los otros layouts).
- `World/Containers/StoreContainer.cs`: solo contenedor + `usePoints` + registro en `StoreDisplayRegistry` + `TryReserveUsePoint`/`ReleaseUsePoint`/`HasFreeUsePoint` + gizmos (eliminado evento/polling).
- `UI/TransactionPanelUITK.cs`: swatch/info/oferta `+Valor` + fix lifecycle (poll de `display` en Update).
- `World/Npc/NpcAgent.cs`: `CurrentDisplay`, fix `TickInspecting` (remainingDistance), reserva use point en `TickWandering`, `ReleaseDisplaySlot`, `displays` → `IReadOnlyList`, fuera `inspectStandoff`.
- `World/Npc/NpcController.cs`: `ForceSpawn`, `TrySpawnOne` devuelve `NpcAgent`, sin `displays` serializada (usa `StoreDisplayRegistry.All`).
- UXML/USS (no van a ScriptNodes): `NameTagUITK.uxml`/`NameTagUITKStyle.uss` (price), `TransactionPanel.uxml`/`.uss` (layout + estilo + backdrop).

**Files Deleted:**
- `UI/StoreContainerPriceTagUITK.cs` + `StoreContainerPriceTag.uxml`/`.uss` (+ metas) → borrar `ScriptNodes/StoreContainerPriceTagUITK.md`.
- `UI/NpcStatusBarUITK.cs` + `NpcStatusBar.uxml`/`.uss` (+ metas) → borrar `ScriptNodes/NpcStatusBarUITK.md`.

**PASOS MANUALES PENDIENTES (Juan, Unity):**
1. Prefab NPC: en el child que tenía `NpcStatusBarUITK` (script faltante ahora), poner `NpcThoughtTag` + UIDocument Source = `NpcThought.uxml` (mantener `WorldUIPanelSettings`), posicionar sobre la cabeza.
2. Cada `StoreContainer`: agregar `usePoints` (child empties sobre el NavMesh; gizmos amarillos guían) + componente `StoreContainerDebug`. **Piso pintado con el área de confinamiento + bakeado** (si no, el MM no es admitido → sin precio).
3. `NpcController`: ya no tiene `displays` (auto-registro). Confirmar `register`/`spawnPoint`/`exitPoint`/`defaultAgentPrefab`.
4. Panel transacción: reimportar UXML/USS; confirmar slot `Transaction → GameObject` en UIManager + `PanelTrigger(Transaction)` + collider/layer (en `grabMask`) en la caja.

**NEXT SESSION (20) — pendientes pedidos por Juan:**
1. **Estado "Sold" completo (debe funcionar EXACTAMENTE como "Dead"):** al aceptar oferta el MM ya pasa a `BusyReason.Sold` (`NpcAgent.AcceptCurrentOffer`), PERO falta: (a) **timestamp de venta** (paralelo a la fecha de muerte de `IsDead` — buscar dónde se guarda la death date en `CreatureDNA` y espejarlo), (b) tratamiento completo igual que `IsDead` (filtros en grid UI / spawn / persistencia), (c) helper/propiedad tipo `IsSold`.
2. **Texto del vendedor feliz al vender:** el `NpcThoughtTag` debe mostrar un diálogo feliz al concretarse la venta (suscribir `GameEvents.OnCustomerSold` o un estado post-venta — hoy el NPC pasa directo a `Leaving` con "Será en otra ocasión…", que no aplica a una venta exitosa).
3. Testear en Play lo de la S19: use points (no overlap), fix del atasco, precio en NameTag, thought tag por NPC.

---

### Sesión 18 (histórico) — 2026-06-21

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
