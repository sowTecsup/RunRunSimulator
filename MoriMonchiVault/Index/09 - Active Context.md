---
tags: [index, core]
---

# 09 - Active Context

**Session:** 2026-07-21 (Session 56 — **PIVOT DE MODELO: Suriyun Dragons_SD APROBADO COMO MORIMOCHI FINAL — análisis de viabilidad + simulador de validación + Fase C de assets COMPLETA (33 FurPatterns + 25 caras + 5 gemas + 4 cuerpos + 23 anims copiados a nuestra estructura) — ✅ CERRADA: 0 errores, todo verificado en Play por MCP. Un solo .cs nuevo: `SuriyunSimDriver` (paleta de diseño autocontenida)**)
**Focus:** Juan descargó el asset Suriyun `Dragons_SD` (dragones SD chibi) y pidió análisis de viabilidad para pivotar el modelo del MoriMochi. Veredicto: encaje excepcional. Se validó en simulador vivo, Juan aprobó el look ("con esto ya lo tenemos") y se materializó la base de assets. El plan "animaciones de la pelotita" (S52-S55) queda RETIRADO — lo reemplaza este modelo con animaciones reales.

1. **Análisis del paquete** (`Assets/Suriyun/`, queda INTACTO como referencia): 4 cuerpos FBX (`DragonSD_A-D`) con **rig Generic COMPARTIDO** y 6 SkinnedMeshRenderers separables cada uno (`Dragon_body`, `Face` plano, `Teech`, `Horn` propio por FBX, `Back` A/B, `Wing_A`); 23 clips de animación en FBX aparte sobre el mismo rig (Idle/Walk/Run/Jump/Damage/Die/**Eat/Rest/Sick**/Roar/Fire/Yes/No/Fly×6/giros); 33 materiales de cuerpo **con el MISMO Unity Toon Shader que nuestros FurTypes** (mismo GUID → `ApplyFur`/MPB funciona tal cual); textura `T_Dragon_00` casi neutra (tinteable), las otras 32 con color+patrón horneados; 25 caras = textura transparente en material Unlit aparte (swap de cara = swap de material).
2. **Fase A — simulador de validación** (`Assets/Scenes/SuriyunSimTest.unity`, quedó en el proyecto): 16 dragones (4 por cuerpo) con tinte genético real (`ColorGenetics.BuildFurPalette` + 4 PropertyIDs por MPB), caras random, Animator con el controller demo, driver de ciclado (anims cada 2-5s) vía `EditorApplication.update` keyed a GO `__SimDriver` (patrón lap-marker S49; registrarlo DESPUÉS de entrar a Play por el domain reload). 16/16 animando, perf trivial.
3. **Descubrimiento clave — "el amago" de los materiales de fábrica**: su riqueza son los **gradientes/patrones pintados en la textura**, no el color. Desaturando las 32 texturas (Python PIL: autocontrast + gamma 0.6, conservando luminancia y alpha) el patrón sobrevive y se tiñe con el pipeline genético → **FurType = patrón de textura** (gen ya existente cobra significado visual real).
4. **Sistema de armonías de color** (aprobado por Juan tras iteración de suavidad): TODO deriva del color principal; cuerpo=base, alas=armonía1, cuerno+cresta=armonía2, dientes=marfil apenas teñido; esquemas ponderados **análogo 40% / mono 30% / triádico 20% / complementario 10%**; saturación natural (0.28-0.56); `Rim` = Lerp(color, blanco, 0.65) — fix de las "alas radiactivas" (el rim saturado dominaba en membranas finas de canto).
5. **Fase C — assets permanentes creados** (copiados/generados, Suriyun intacta): `Resources/Models/MoriMochi/MonchiBody_A-D.fbx` · `Resources/Animations/MoriMochi/MonchiAnim_*.fbx` (23) · `Resources/Textures/MoriMochi/Patterns/MonchiPattern_00-32.png` (33 desaturadas) + `Faces/MonchiFace_01-25.png` + `MonchiGemMatcap.png` (matcap procedural) · `Resources/Materials/MoriMochi/FurPatterns/MonchiFur_00-32.mat` (UTS+patrón, tinteables) + `Faces/MonchiFace_01-25.mat` + `Gems/MonchiGem_{Gold,Ruby,Emerald,Sapphire,Amethyst}.mat` (**MatCap glossy** + `_Is_SpecularToHighColor`, verificado en Play: resaltan clarísimo de los mate).

> ### 📌 DECISIONES DE DISEÑO DE JUAN (S56 — el nuevo contrato del MoriMochi visual)
> 1. **Genes del MoriMochi nuevo**: (a) **Tipo de cuerno** = gen primario que selecciona el FBX COMPLETO (cuerno+cuerpo+cresta vienen juntos → sin retarget cruzado, riesgo eliminado); (b) **Rol** (ya existe); (c) **Color principal** único — todos los demás derivan con armonía.
> 2. **Caras = MOOD, no gen**: las 25 caras ciclan según el estado emocional del agente (enganche con needs/reacciones — `Sick` hasta tiene animación propia). Sirve al store mode para que se sientan vivos.
> 3. **FurType = patrón de textura; AMPLIAR el enum** (hoy 5 slots vs 33 patrones) — **reset de saves aceptado** por Juan.
> 4. **Determinismo del esquema de armonía**: la solución de menor resistencia = derivar del hash del `BaseColor` (sin gen nuevo, sin cambio de DNA).
> 5. **Shiny gem**: gen rarísimo **0.5%, únicamente estético**, usa los materiales `MonchiGem_*`.
> 6. **Cartas de MoriMonchis** (teorización pendiente post-integración): idea de Juan = cámara por MM aislado a RenderTexture con otro skybox; propuesta alternativa presentada = **híbrido fotomatón** (snapshot cacheado por DNA+mood para grillas, cámara live solo para la carta enfocada). SIN DECIDIR.
> 7. El AnimatorController demo auto-retorna a Idle al terminar cada clip (transiciones con exit time) — conducta ideal para moods; el controller propio debe replicarla con nombres limpios.
> 8. Los 33 prefabs demo, escenas, scripts, Floor y UI de Suriyun NO se copian (solo referencia).

6. **`SuriyunSimDriver` (pedido final de Juan): la escena quedó AUTOCONTENIDA como paleta de diseño guía** — MonoBehaviour en GO `__SimSetup` de `SuriyunSimTest` que en cada Play regenera todo solo: carga los bancos por `Resources.Load` (33 fur / 25 caras / 5 gemas), toma los `Dragon_*` de la escena, aplica armonía ponderada 40/30/20/10 + tinte MPB + shinies (2, gema random) y cicla anims/caras en `Update`. Knobs serializados: `seed` (4242 = paleta reproducible; `randomizeEachPlay` para variar), `shinyCount`, `cycleSeconds`, rangos de saturación/valor. Reemplaza el driver efímero por `EditorApplication.update`. Verificado en Play sin inyección: 0 errores.

**Files Touched (.cs — input ScriptNodes):**
- `Prototype/SuriyunSimDriver.cs` (NUEVO): driver autocontenido de la escena de paleta `SuriyunSimTest` (regenera looks + cicla anims/caras cada Play vía Resources.Load).

**Files Touched (no-ScriptNode):** NUEVOS: `Assets/Scenes/SuriyunSimTest.unity` (escena simulador, 16 dragones + Animators wireados); toda la estructura `Resources/{Models,Animations,Textures,Materials}/MoriMochi/` (4 FBX + 23 anim FBX + 59 texturas + 63 materiales); `Assets/Screenshots/s56_*.png` (10, borrables). La carpeta `Assets/Suriyun/` (paquete importado por Juan) queda sin trackear como referencia.

**Next session (S57, candidatos):**
1. **INTEGRACIÓN DE CÓDIGO DEL MODELO NUEVO (prioridad)**: AnimatorController propio (nombres limpios + auto-idle), prefab `MorimonchiAgent` sobre `MonchiBody_*`, `DragonAnimationDriver : MonchiAnimationDriver` (Animator-based), visualizer nuevo/adaptado (selección FBX por gen cuerno + patrón FurType + armonía por hash + cara por mood), enum `FurType` ampliado + wipe de saves, gen shiny 0.5%. Plan formal con sub-agentes `morimonchi-coder`; cuidado con el invariante color↔identidad (BaseColor embebido en UniqueID).
2. Después: integración replay 3v3 con el driver nuevo + barra con nombre + interacciones ingame (arrastra de S45); teorizar cartas (nota 6).
3. Arrastran: probar confinamiento/cortejo con corrales, F5 async 3v3, economía F7, nota escudo por RONDA.

**ScriptNodes (cierre S56):** CREAR `SuriyunSimDriver.md`.

---

**Session:** 2026-07-20/21 (Session 55 — **DEUDA TÉCNICA TOTAL: FASE 8 (`MoriMochiAgent` → AgentContext + 3 mini-managers) + FASE 9 (`SpawnerDevConsole`) + ELIMINACIÓN COMPLETA DEL SPIDER + ARCHIVADO DEL ACTIVE CONTEXT — ✅ CERRADA: 0 errores, todo verificado en Play por MCP. NO QUEDA NINGÚN PARTIAL EN EL JUEGO**)
**Focus:** Juan pidió "acabar con todas las deudas pendientes". Cayeron los 4 frentes en una sesión: el experimento spider borrado entero, la Fase 9 menor, el jefe final (Fase 8) y la higiene del vault.

1. **SPIDER ELIMINADO (S52 nota 2 ejecutada)**: carpetas `Scripts/Prototype/` (13 scripts) y `Prototype/` (SpiderTuning, SpiderConeMesh, 8 materiales `Spider_*`) borradas enteras; `MoriMochiSpider.prefab` y escena `MorimonchiNewModel.unity` borrados (decisión de Juan: borrar, no archivar); campo `spiderVisual` + sus dos bloques quitados de `MoriMonchiController` (Initialize/Rebind quedan lineales); 13 ScriptNodes `Spider*.md` borrados del vault. Sobreviven `MonchiAnimationDriver` (contrato permanente) y la regla 60/30/10. Build settings ya no referenciaban la escena.
2. **FASE 9 — `MoriMochiSpawner` ya no es partial** (`.Debug.cs` eliminada): botones dev → **`SpawnerDevConsole`** nuevo (patrón F3: ref serializada, estado `debugShots` propio, 4 botones); el spawner ganó accesores `internal` mínimos (`CreaturePrefab`/`MuzzlePosition`/`LaunchAngleRange`/`SpawnedEntries` + `Sync`/`ClearAll`/`RandomLandingPoint`/counts promovidos); gizmos quedaron en el núcleo (auditoría 2026-06-20). Componente agregado al GO `MoriMonchiSpawner` en GameScene y wireado (escena guardada — ojo: la escena estaba dirty de antes, se persistió también lo pendiente de Juan).
3. **FASE 8 — `MoriMochiAgent` ya no es partial** (los 4 `.Brain/.Physics/.Confinement/.Tuning` eliminados): **`AgentContext`** (blackboard plano, dueño único del estado compartido + helpers) + **`AgentBrain`** (conducta/needs/reacciones/intent/pet) + **`AgentPhysics`** (throw/knock/ragdoll/recovery/handoff, incluye el viejo FixedUpdate como `FixedTick`) + **`AgentConfinement`** (pen + cortejo COMPLETO — los ticks se movieron de Brain — + rebake). Núcleo: tuning absorbido con campos `private`→`internal` (mismos nombres → prefab intacto), dispatch con orden exacto del Update viejo, fachada pública idéntica, switchboard `Request*` (managers nunca se llaman entre sí). Cero cambios de escena/prefab (managers = clases planas).
4. **Verificación Fase 8 (Play, GameScene, 0 errores)**: pipeline cañón 9/9 (Thrown→Recovering→Roaming), `DevForceRagdoll`→recovery, `OnGrab`/`OnThrow` (Carried→Thrown + penalidad Affect), needs (drenaje→`Condition=Sick`, fallback sin estación correcto — la escena no tiene NeedStations), rebake simulado por `GameEvents` (9/9 a ragdoll con flag → 9/9 recuperadas), `RespawnAll` con reuso de pool (9/9 on-mesh). Verificación Fase 9: 4 botones del console + wiring por MCP.
5. **Archivado del vault**: `09 - Active Context` pasó de 346KB a ~68KB — S45 y anteriores viven ahora en **`Index/09b - Session Archive.md`** (35 sesiones, backlink en ambas notas). Index/11 actualizado: Fases 8 y 9 ✅ + nota de cierre de la deuda de partials.

> ### 📌 Notas S55
> 1. **NO ejercitado en vivo** (Fase 8): confinamiento/cortejo (GameScene actual sin corrales colocados — probar cuando haya furniture) y pet (requiere jugador posicionado). Cubiertos por paridad de código movido verbatim.
> 2. Durante los tests en Play hubo que setear `Application.runInBackground = true` por código (el editor sin foco congela el player loop) — es runtime-only, no quedó tocado nada.
> 3. La implementación se delegó a 4 sub-agentes `morimonchi-coder` (3 managers en paralelo + núcleo) contra un contrato rígido; compiló limpio al primer intento.

**Files Touched (.cs — input ScriptNodes):**
- `World/AI/AgentContext.cs` (NUEVO): blackboard del agente (estado compartido + helpers + enum `AgentState` top-level).
- `World/AI/AgentBrain.cs` (NUEVO): mini-manager de conducta (estados NavMesh, needs, reacciones, intent, pet).
- `World/AI/AgentPhysics.cs` (NUEVO): mini-manager físico (grab/throw/knock/ragdoll/recovery/rejoin).
- `World/AI/AgentConfinement.cs` (NUEVO): mini-manager de corral + cortejo completo + rebake.
- `World/AI/MoriMochiAgent.cs` (MODIFICADO): reescrito como núcleo delgado; ya no partial; tuning absorbido (`internal`); fachada + switchboard.
- `World/Spawning/SpawnerDevConsole.cs` (NUEVO): consola dev del spawner (patrón F3).
- `World/Spawning/MoriMochiSpawner.cs` (MODIFICADO): ya no partial; gizmos absorbidos; accesores `internal` para el console; sin `debugShots`.
- `World/Creatures/MoriMonchiController.cs` (MODIFICADO): eliminado el camino `spiderVisual`.
- ELIMINADOS: `MoriMochiAgent.Brain/.Physics/.Confinement/.Tuning.cs`, `MoriMochiSpawner.Debug.cs`, `Scripts/Prototype/Spider/` (13 scripts), con sus `.meta`.

**Files Touched (no-ScriptNode):** GameScene (componente `SpawnerDevConsole` wireado, guardada), borrados `MoriMochiSpider.prefab` / `MorimonchiNewModel.unity` / `Prototype/` (assets+materiales); vault: `Index/09b - Session Archive.md` (NUEVO), `Index/11` actualizado, 13 `ScriptNodes/Spider*.md` borrados.

**Next session (S56, candidatos):**
1. **Prioridad de gameplay de Juan (arrastra desde S53)**: animaciones de la pelotita (`MonchiBallAnimationDriver` sobre `MoriMonchiProceduralAnimator`) + integración replay 3v3 + barra con nombre + INTERACCIONES INGAME del replay (palabra de Juan, S45).
2. Probar confinamiento/cortejo en Play cuando haya corrales colocados (gap de verificación S55, nota 1).
3. Arrastran: F5 async 3v3, economía F7, nota de diseño del escudo por RONDA (rompe paridad).

**ScriptNodes (cierre S55):** CREAR `AgentContext.md`, `AgentBrain.md`, `AgentPhysics.md`, `AgentConfinement.md`, `SpawnerDevConsole.md`; ACTUALIZAR `MoriMochiAgent.md` (ya no partial, composición), `MoriMochiSpawner.md` (ya no partial, console aparte), `MoriMonchiController.md` (sin spiderVisual); los `Spider*.md` ya fueron borrados.

---

**Session:** 2026-07-20 (Session 54 — **DEUDA TÉCNICA: FASE 7 COMPLETA (`BreedingPanelUITK` + `MorimonchiDetailInfoUITK` → presenters) + contrato generalizado `ITabPresenter` — ✅ CERRADA: 0 errores, ambos paneles verificados en Play por MCP**)
**Focus:** Continuación directa del piloto S53. Cayeron los dos paneles restantes de la Fase 7 con el patrón de composición. Los paneles UITK quedan SIN partials; de deuda solo restan Fase 8 (`MoriMochiAgent`) y Fase 9 (`MoriMochiSpawner.Debug`).

1. **Contrato generalizado**: `ICombatTabPresenter` → **`ITabPresenter`** (renombre de archivo `.cs`+`.meta` juntos, GUID preservado; actualizados los 3 presenters de combate + núcleo `CombatPanelUITK` — mecánico, sin cambio de comportamiento).
2. **`BreedingPanelUITK` ya no es partial** (`.Content.cs`/`.Navigation.cs` eliminados): **`BreedingBreedTabPresenter`** (tab Criar: candidatos padre/madre, selección, preview, `TryBreed` con callback `onBred` al núcleo — equivalente al `onEnqueued` del piloto; expone `Busy` FUERA de la interfaz y el núcleo congela TODO el input mientras un breed está en vuelo, paridad con el viejo `breedBusy` que también congelaba la TabBar) + **`BreedingEggsTabPresenter`** (tab Incubando: huevos, `DoHatch`, `Tick()` fuera de la interfaz con throttle interno de 1s — el núcleo lo llama solo con la tab visible, patrón `CombatResultsTabPresenter`) + núcleo (216 líneas) espejo de `CombatPanelUITK`: `IUINavigable` TabBar⇄Content, campos serializados intactos → cero cambios de escena.
3. **`MorimonchiDetailInfoUITK` ya no es partial** (`.Trees.cs` eliminado) — **decisión de arquitectura**: este panel NO tiene navegación interna (A/D cambia tabs, Submit vacío, Cancel cierra), así que sus presenters **NO implementan `ITabPresenter`** (habrían sido 5 métodos muertos por clase — regla 8): son colaboradores planos `ctor(root, deps)` + `Rebuild(dna)`. Cuatro: `DetailInfoTabPresenter` (stats con desglose, identidad, rol/elemento, partes, progresión), `DetailCombatTabPresenter` (tarjetas historial + replay), `DetailTreesPresenter` (tabs Linaje **y** Descendencia — UN dominio, comparten `MakeChip`/`ParseGenetics`), `DetailEquipTabPresenter` (cards por slot, popup mochila, stats Base→Final). Núcleo 173 líneas (era 665+199): lifecycle, `Show`/`Populate` (título+retrato+delegar 4 `Rebuild`), `IUINavigable` intacto.
4. **Regla del patrón consolidada (para futuros paneles)**: `ITabPresenter` es para tabs con foco jerárquico real (regiones/listas navegables); un panel de solo-contenido usa colaboradores planos con `Rebuild(data)`. Siempre: `Q<>` propios sobre el root, registry por `Func<>` (no cachear), estado UI propio, `Teardown` solo desuscribe botones persistentes.
5. **Verificación Breeding (Play, GameScene)**: panel por código — Criar con 3 padres/6 madres; navegación completa por `IUINavigable` (entrar → lista padres → seleccionar 2º → slot madre → lista madres → seleccionar → preview con colores/stats/partes/"≈ 30 min", foco en el slot correcto); cancelación jerárquica con paridad exacta (listas→Slots→TabBar consumen; en TabBar retorna `false`); tab Incubando con 0 huevos (solo rebuild/nav vacía); un pull de nube disparó `OnRegistryReloaded` en medio y el rebuild vía presenters pasó sin errores. NO ejercitado: breed/hatch reales (efecto servidor).
6. **Verificación DetailInfo (Play, GameScene)**: "Gloomy Sprout", 5 tabs recorridas por `IUINavigable` con clamp en extremos: Info completa (stats 8/4/6 con desglose, Agresivo · Electricidad, 4 partes), Combate con récord viejo (fallback "sin stats registradas" + botón replay — camino backward-compat), Equipo (3 slots vacíos con acento por slot, retrato, stats), Linaje (chip "Tú" + vacío "linaje silvestre"). Límites: el registry no tiene criaturas con ancestros ni ítems equipados → árboles poblados y cards con ítem no vistos en pantalla (mismo código compartido sí corrió); replay no cliqueado (carga escena).

> ### 📌 Notas S54
> 1. Screenshots `Assets/Screenshots/s54_*.png` (6; `s54_detailpanel_info.png` salió vacío por timing de frame, su `-1` es el bueno) — borrables.
> 2. Durante la verificación DetailInfo la consola mostró "PlayerLoop internal function has been called recursively" + "Missing Profiler.EndSample" — ruido interno del editor (ScreenCapture + execute_code por MCP), sin ningún stack de código del juego.
> 3. **FASE 7 COMPLETA** (piloto Combat S53 + Breeding y DetailInfo S54). Deuda de partials restante: Fase 8 (`MoriMochiAgent`, blackboard + mini-managers, sesión dedicada) y Fase 9 (`MoriMochiSpawner.Debug`, menor).
> 4. Index/11 actualizado: Fase 7 ✅ completa.

**Files Touched (.cs — input ScriptNodes):**
- `UI/ITabPresenter.cs` (NUEVO — renombre de `ICombatTabPresenter.cs` con GUID preservado): contrato generalizado de tab presenters con foco.
- `UI/BreedingBreedTabPresenter.cs` (NUEVO): presenter tab Criar (+ `Busy` fuera de la interfaz).
- `UI/BreedingEggsTabPresenter.cs` (NUEVO): presenter tab Incubando (+ `Tick()` fuera de la interfaz).
- `UI/BreedingPanelUITK.cs` (MODIFICADO): reescrito como núcleo delgado; ya no partial.
- `UI/DetailInfoTabPresenter.cs` (NUEVO): tab Info del detalle (colaborador plano, sin interfaz).
- `UI/DetailCombatTabPresenter.cs` (NUEVO): tab Combate del detalle (colaborador plano).
- `UI/DetailTreesPresenter.cs` (NUEVO): tabs Linaje+Descendencia del detalle (colaborador plano).
- `UI/DetailEquipTabPresenter.cs` (NUEVO): tab Equipo del detalle (colaborador plano).
- `UI/MorimonchiDetailInfoUITK.cs` (MODIFICADO): reescrito como núcleo delgado; ya no partial.
- `UI/CombatPanelUITK.cs`, `UI/CombatOnlineTabPresenter.cs`, `UI/CombatResultsTabPresenter.cs`, `UI/CombatHistoryTabPresenter.cs` (MODIFICADOS: solo renombre `ICombatTabPresenter`→`ITabPresenter`).
- ELIMINADOS: `BreedingPanelUITK.Content.cs`, `BreedingPanelUITK.Navigation.cs`, `MorimonchiDetailInfoUITK.Trees.cs`, `ICombatTabPresenter.cs` (con sus `.meta`).

**Files Touched (no-ScriptNode):** `Assets/Screenshots/s54_*.png` (6, borrables).

**Next session (S55, candidatos):**
1. **Prioridad de gameplay de Juan (S53 original)**: animaciones de la pelotita (`MonchiBallAnimationDriver` sobre `MoriMonchiProceduralAnimator`) + integración replay 3v3 + barra con nombre.
2. **Deuda**: Fase 8 `MoriMochiAgent` (el jefe final, sesión dedicada con testing exhaustivo en Play) o Fase 9 menor; eliminación del experimento spider (S52 nota 2, sesión mecánica).
3. Arrastran: F5 async 3v3, economía F7, archivar sesiones viejas de esta nota (>315KB).

**ScriptNodes (cierre S54):** CREAR `BreedingBreedTabPresenter.md`, `BreedingEggsTabPresenter.md`, `DetailInfoTabPresenter.md`, `DetailCombatTabPresenter.md`, `DetailTreesPresenter.md`, `DetailEquipTabPresenter.md`, `ITabPresenter.md` (sucesor de `ICombatTabPresenter.md`); ACTUALIZAR `BreedingPanelUITK.md`, `MorimonchiDetailInfoUITK.md`, `CombatPanelUITK.md`, `CombatOnlineTabPresenter.md`, `CombatResultsTabPresenter.md`, `CombatHistoryTabPresenter.md` (referencia a `ITabPresenter`); MARCAR RETIRADOS `BreedingPanelUITK.Content.md`, `ICombatTabPresenter.md`.

---

**Session:** 2026-07-20 (Session 53 — **DEUDA TÉCNICA: FASE 6 COMPLETA (CloudSyncService → composición) + FASE 7 PILOTO COMPLETO (CombatPanelUITK → presenters por pestaña) — ✅ CERRADA: 0 errores, ambos verificados en Play por MCP**)
**Focus:** Juan eligió el frente de deuda técnica (Index/11, decisión S32: composición sobre partial). Cayeron DOS monstruos con el patrón canónico (mini-managers con estado propio + núcleo delgado coordinador): el partial de red y el panel UITK más grande.

1. **FASE 6 — `CloudSyncService` ya no es partial** (`.Auth.cs`/`.Sync.cs` eliminados): **`CloudAuth`** (clase plana, dueña única de identidad: `IsSignedIn`/`PlayerID`/`PlayerName`/`AuthMethod`/`ServerOffset`; init UGS, sign-in anónimo/Unity/resume, sign-out, update de nombre, server time; reporta status por `Action<string>` y el sign-in completo por `Func<string, Task>`) + **`CloudSyncOps`** (clase plana, dueña de sync: keys, `SyncMeta` anti-cheat, `MetaPath` por `auth.PlayerID`, validate/push/pull/reset/notify; lee la identidad, nunca la muta) + **`CloudSyncService`** núcleo MonoBehaviour (148 líneas): compone ambos en `Start`, orquesta la secuencia post-sign-in (SetUserScope → loads locales → server time → pull → notify), fachada pública INTACTA (`PushAsync`/`ServerOffset`/etc. — `GameManager` no cambió) + botones/displays Odin delegando. Único `[SerializeField]` (`newNameInput`) se quedó en el núcleo → cero riesgo de serialización.
2. **FASE 7 PILOTO — `CombatPanelUITK` ya no es partial** (`.Tabs.cs`/`.Navigation.cs` eliminados): interfaz **`ICombatTabPresenter`** (`Enter/Navigate/Submit/Cancel/ClearFocus/Rebuild/Teardown`) + **un presenter por pestaña**: `CombatOnlineTabPresenter` (elegibles, selección, stats/partes, enqueue con callback `onEnqueued` al core, foco interno lista⇄acciones), `CombatResultsTabPresenter` (cola + reloj cron vía `Tick()` que el core llama solo con la tab visible), `CombatHistoryTabPresenter` (items aplanados, filtro, detalle, replay). Núcleo (237 líneas): lifecycle, eventos, wiring, composición en `Wire()` y `IUINavigable` reducido a `TabBar ⇄ Content` que DELEGA el foco interno al presenter activo. Cada presenter hace sus `Q<>` sobre el root y recibe el registry por `Func<>` (no cachea). Campos serializados intactos → cero cambios de escena. Tab 3 (Equipo 3v3) sigue siendo del sibling `CombatLineupUITK`, sin tocar.
3. **Decisión de arquitectura (patrón para el resto de F7)**: la navegación NO se queda entera en el núcleo — el núcleo solo conserva la región TabBar⇄Contenido; los sub-estados de foco (p.ej. lista vs botones de acción) viven en su presenter. El presenter retorna `false` en `Navigate`/`Cancel` para "salir a la tab bar".
4. **Verificación Fase 6 (Play, GameScene)**: sign-in resume OK (SowMain#6345), server offset +0,3s, pull automático 9 criaturas + meta local, push por fachada con `ValidateBeforePush` → Security OK, flush de `OnApplicationQuit` por el camino nuevo, y `AsyncCombatService.FetchQueuedIdsAsync` contra servidor OK (el riesgo auth→async que marcaba Index/11).
5. **Verificación Fase 7 (Play, GameScene)**: panel abierto por código — Online 9 cards, Historial 6 items; navegación ejercitada por `IUINavigable` (entrar lista → seleccionar → cancelar ×2 → tab Historial → detalle "Gloomy Sprout") con paridad al Navigation viejo; screenshots `s53_combatpanel_*.png`. NO se ejercitó el enqueue real (efecto en servidor) ni `ResetProgressAsync` (destructivo, solo compila).

> ### 📌 Notas S53
> 1. La escena activa quedó **GameScene** (antes estaba `MorimonchiNewModel`); sin cambios de escena guardados.
> 2. `Assets/Screenshots/s53_combatpanel_*.png` (2) son borrables.
> 3. **Fase 7 pendiente** (mecánico con el patrón del piloto): `BreedingPanelUITK` (.Content) y `MorimonchiDetailInfoUITK` (.Trees) — una sesión por panel con testing en Play.
> 4. Quedan de deuda: Fase 8 (`MoriMochiAgent`, el jefe final) y Fase 9 (`MoriMochiSpawner.Debug`).
> 5. Index/11 actualizado: Fase 6 ✅, Fase 7 piloto ✅.

**Files Touched (.cs — input ScriptNodes):**
- `Systems/Cloud/CloudAuth.cs` (NUEVO): mini-manager de identidad UGS (clase plana, callbacks de status/sign-in).
- `Systems/Cloud/CloudSyncOps.cs` (NUEVO): mini-manager de sync/meta (clase plana; push/pull/reset/validate/notify).
- `Systems/Cloud/CloudSyncService.cs` (MODIFICADO): reescrito como núcleo delgado que compone `CloudAuth`+`CloudSyncOps`; ya no partial; fachada pública intacta.
- `UI/ICombatTabPresenter.cs` (NUEVO): contrato núcleo↔presenter de pestaña.
- `UI/CombatOnlineTabPresenter.cs` (NUEVO): presenter tab Batalla Online.
- `UI/CombatResultsTabPresenter.cs` (NUEVO): presenter tab Resultados (+`Tick()` fuera de la interfaz).
- `UI/CombatHistoryTabPresenter.cs` (NUEVO): presenter tab Historial.
- `UI/CombatPanelUITK.cs` (MODIFICADO): reescrito como núcleo delgado; ya no partial; `IUINavigable` TabBar⇄Content delegado.
- ELIMINADOS: `CloudSyncService.Auth.cs`, `CloudSyncService.Sync.cs`, `CombatPanelUITK.Tabs.cs`, `CombatPanelUITK.Navigation.cs` (con sus .meta).

**Files Touched (no-ScriptNode):** `Assets/Screenshots/s53_combatpanel_*.png` (2, borrables).

**Next session (S54, candidatos):**
1. **Fase 7 continuación**: `BreedingPanelUITK` → presenters de .Content (patrón del piloto), luego `MorimonchiDetailInfoUITK`.
2. O retomar la **prioridad de gameplay de Juan (S53 original)**: animaciones de la pelotita (`MonchiBallAnimationDriver` sobre `MoriMonchiProceduralAnimator`) + integración replay 3v3 + barra con nombre.
3. Arrastran: eliminación del experimento spider (S52 nota 2), Fases 8-9 de deuda, F5 async 3v3, economía F7, archivar sesiones viejas de esta nota (>315KB).

**ScriptNodes (cierre S53):** CREAR `CloudAuth.md`, `CloudSyncOps.md`, `ICombatTabPresenter.md`, `CombatOnlineTabPresenter.md`, `CombatResultsTabPresenter.md`, `CombatHistoryTabPresenter.md`; ACTUALIZAR `CloudSyncService.md`, `CombatPanelUITK.md`; ELIMINAR (o marcar retirados) `CombatPanelUITK.Tabs.md`, `CombatPanelUITK.Navigation.md` si existen.

---

**Session:** 2026-07-20 (Session 52 — **CONTRATO DE ANIMACIÓN DE COMBATE + SET COMPLETO EN EL SPIDER + 60/30/10 + EXPERIMENTO "SPIDER COMO MODELO DEL JUEGO" EJECUTADO Y REVERTIDO — ✅ CERRADA: 0 errores, rollback verificado, spider DESCARTADO como modelo (eliminación pendiente)**)
**Focus:** Juan reencuadró el rig procedural como stand-in temporal y pidió el set mínimo de animaciones de combate + roadmap de integración. Se diseñó **contract-first**: la API semántica sobrevive a cualquier modelo. Al final de la sesión Juan probó el spider como modelo real en GameScene y lo **descartó** ("ver tantas arañas moverse con sus patitas da repelús") — rollback completo, el experimento spider (S48-S52) queda terminado.

1. **Contrato permanente `MonchiAnimationDriver`** (`Scripts/World/`, abstract MonoBehaviour): `IsBusy` / `MoveTo(dest, onArrived)` / `PlayAttack(targetPos, onImpact, onFinished)` / `PlayHit(intensity)` / `PlayBuff(onFinished)` / `PlayDefeat()` / `PlayVictory()` / `PlayIdle()`. Es LO QUE SOBREVIVE de la sesión: el CombatVisualizer consumirá esta API y cada modelo la implementa con su driver.
2. **Set completo implementado en el spider** (driver procedural de referencia): ataque con aproximación (caminar o saltitos 50/50 random) + encarado + inclinación adelante (~28°) + punch elástico + manotazo del brazo físico; golpe recibido = squash + inclinación atrás; buff = viaje al aliado + mini baile (brazos arriba + vaivén + saltito); derrota = ragdoll + lanzamiento hacia arriba; victoria = brazos arriba + botes. Inclinaciones vía `SpiderBodyMotion.AddPitchImpulse` (resorte subamortiguado, dueño único de la rotación del pivote).
3. **REGLA DE COLOR 60/30/10 (decisión de Juan, sobrevive al spider)**: cuerpo=`BaseColor` (60), cara=`SecondaryColor` (30), detalles de cara=acento `DeriveSecondary(secondary)` (10). Implementada en `SpiderPaletteApplier` (MaterialPropertyBlock `_BaseColor`+`_Color`, sin mutar materiales compartidos) + `ApplyMaterial` para que el material base lo dicte la DB (`FurTypeDatabaseSO.GetMaterial(dna.FurType)`).
4. **Experimento GameScene (ejecutado y REVERTIDO)**: `MoriMochiSpider.prefab` creado (limpio, dev-tools movidos a `__SpiderDevTools` en la escena playground); `SpiderBodyController` ganó modo `externalMotion` (patas siguen al NavMeshAgent sin pelear el movimiento); `MoriMonchiController` ganó camino opcional `spiderVisual` (si != null: material DB + ApplyFromDna, sin `Assemble`; hoy quedó **null** = camino viejo activo). El prefab `MorimonchiAgent.prefab` volvió al ensamblado por partes.
5. **Perf medida en GameScene (9 criaturas)**: main 7,6ms / render thread 8,5ms / GPU 2,4ms / física ~2,5ms — los ~30fps que vio Juan son mayormente overhead del editor (Scene+Game view+profiler). Se removieron igualmente los rbs/joints de patas del variant (solo servían al ragdoll apagado) antes del rollback.

> ### 📌 Notas S52
> 1. **SPIDER DESCARTADO COMO MODELO** — decisión de Juan tras verlo en densidad real (guardada en memoria del agente). Lección: validar estética SIEMPRE con la densidad real de gameplay, no en solitario.
> 2. **PENDIENTE NUEVO — ELIMINACIÓN DEL EXPERIMENTO SPIDER**: carpeta `Scripts/Prototype/Spider/` (13 scripts), `MoriMochiSpider.prefab`, campo `spiderVisual` de `MoriMonchiController`, escena `MorimonchiNewModel` (limpiar o archivar), `SpiderTuning.asset`, `SpiderConeMesh.asset`, materiales `Spider_*.mat`, ScriptNodes `Spider*.md`. Sesión mecánica dedicada.
> 3. El contrato `MonchiAnimationDriver` y la regla 60/30/10 NO se eliminan — son la base de las animaciones de la pelotita.
> 4. La UI de alineaciones 2-3-2 (pregunta de Juan) YA EXISTE: `CombatLineupUITK` + `CombatLineupBoard`, tab "Equipo 3v3" (~S38, ajustada S39).
> 5. `BaseFurNewTest.mat`/`FurBaseTest.mat` figuran modificados en git (re-serialización/toques de Juan) — los tintes van por MPB, ningún script muta materiales.
> 6. La barra de vida del combate debe RECUPERAR EL NOMBRE del MoriMochi (pedido de Juan, quedó oculta desde la barra minimal S47) — va con la fase del visualizador.

**Files Touched (.cs — input ScriptNodes):**
- `World/MonchiAnimationDriver.cs` (NUEVO, PERMANENTE): contrato abstracto de animación de combate (8 miembros).
- `World/Creatures/MoriMonchiController.cs` (MODIFICADO): camino opcional `spiderVisual` en Initialize/Rebind (material DB + colores 60/30/10, salta Assemble); hoy con ref null = inerte.
- `Prototype/Spider/SpiderAnimationDriver.cs` (NUEVO): driver procedural que implementa el contrato completo componiendo controller/jump/elastic/ragdoll/arms/motion.
- `Prototype/Spider/SpiderArmDriver.cs` (NUEVO): poses de brazos vía `targetRotation` de los ConfigurableJoints (rest/arriba/swipe con callback de impacto).
- `Prototype/Spider/SpiderPaletteApplier.cs` (NUEVO): 60/30/10 por MPB sobre 3 grupos de renderers + `ApplyMaterial` (material dictado por DB) + `ApplyFromDna`.
- `Prototype/Spider/SpiderBodyController.cs` (MODIFICADO): `SetExternalDrive/ClearExternalDrive` (conducción por código) + modo `externalMotion` (gait reactivo a movimiento impuesto, turning por yaw observado).
- `Prototype/Spider/SpiderBodyMotion.cs` (MODIFICADO): `AddPitchImpulse(degrees)` — inclinación dramática con resorte subamortiguado hacia 0, knobs `actionFrequency`/`actionDamping`.
- `Prototype/Spider/SpiderElasticBody.cs` (MODIFICADO): `AddImpulse(velocity)` — inyección al resorte de escala (squash/punch).
- `Prototype/Spider/SpiderDevPanel.cs` (MODIFICADO): scroll view general; sección "Acciones (driver)" (Atacar/Buff/Recibir golpe/Victoria/Derrota/Idle/Ir al spawn) + "Colores random (60/30/10)"; refs driver/palette/attackTarget.

**Files Touched (no-ScriptNode):** `Resources/Prefabs/MoriMochiSpider.prefab` (NUEVO — pendiente de borrar con el experimento), `Resources/Prefabs/MorimonchiAgent.prefab` (cirugía spider ejecutada y REVERTIDA — quedó como antes + ref `spiderVisual` null), `Resources/Scenes/MorimonchiNewModel.unity` (componentes nuevos wireados, dev-tools a `__SpiderDevTools`, 15 renderers con `BaseFurNewTest`), `Prototype/SpiderTuning.asset`, materiales FurTypes re-serializados, registry asset (guardados de Play).

**Next session (S53):**
1. **ANIMACIONES DE LA PELOTITA (prioridad de Juan)**: implementar `MonchiAnimationDriver` sobre el modelo ensamblado real — `MonchiBallAnimationDriver` apoyado en `MoriMonchiProceduralAnimator` (squash/stretch de ataque y golpe, baile de buff con los bracitos, desplome de derrota, botes de victoria). Trasladar lo aprendido: resortes subamortiguados, impulsos de pitch, callbacks de impacto.
2. Luego integración en el replay 3v3 (`CombatVisualizerMM`): `CombatVisualUnit` gana driver, `ForwardRoutine` reemplaza `MoveOverTime` por la API del contrato, barra recupera el nombre (nota 6).
3. **DEUDA TÉCNICA (frente declarado por Juan)**: descomposición de partials según Index/11 Fases 6-9 (`MoriMochiAgent`, paneles UITK, `CloudSyncService`, `MoriMochiSpawner.Debug`) — una sesión por monstruo. + Eliminación del experimento spider (nota 2).
4. Arrastran: F5 async 3v3, economía F7, ítems con estados, tuning de knobs elementales, archivar sesiones viejas de esta nota (>315KB).

**ScriptNodes (cierre S52):** CREAR `MonchiAnimationDriver.md`, `SpiderAnimationDriver.md`, `SpiderArmDriver.md`, `SpiderPaletteApplier.md`; ACTUALIZAR `MoriMonchiController.md`, `SpiderBodyController.md`, `SpiderBodyMotion.md`, `SpiderElasticBody.md`, `SpiderDevPanel.md`.

---

**Session:** 2026-07-20 (Session 51 — **CONSOLIDACIÓN NOTION ✅ (autorizada por Juan): el wiki de diseño quedó sincronizado con el estado del código y el vault** — ✅ CERRADA, sin scripts .cs tocados)
**Focus:** Sesión de documentación pura. Juan autorizó explícitamente el `notion-documenter`; el orquestador digirió vault + Active Context con 2 sub-agentes y ejecutó 4 corridas de Notion con alcances disjuntos. 17 páginas del wiki actualizadas.

1. **Notion — Combate (4 páginas)**: "Combate, Venganza y Bidding" (tesis v3 autobattler 3v3; Venganza/Bidding preservados con callout "etapa futura, no revisado en v3"), "Combate Local — Implementación" (reescrita 1v1→3v3 completa), "Evolución y Ciclo de Vida" (trigger 3v3: una unidad al azar del ganador evoluciona; muerte 5%; Tiers/aging/límites 4-5 intactos), "Combate Async + UGS" (nota: 1v1 async operativo como base, 3v3 async al final del roadmap).
2. **Notion — Criaturas (7 páginas)**: Personalidades(6)→Roles(3) en "Vida y Comportamiento" (tabla vieja en subsección ⚠️ Legacy pendiente de revisión de Juan) y "Agentes en Escena"; metadata del DNA (Género/Rol/Elemento fuera del genetic string) en "Sistema Genético" y "Genética — Implementación"; herencia Rol 50/50, Elemento 50/50+10% mutación, matriz 3×3 y límites duros en "Breeding" (diseño + implementación); subsección "Dirección de Arte y Animación 2026-07" (trípode arácnido, 5 paletas, híbrido gait kinematic + física Gang Beasts) en "Concepto y Pilares".
3. **Notion — Transversal (5 páginas)**: "Decisiones de Diseño" +14 decisiones cerradas S39-S50; "Preguntas Abiertas" con Ronda 3 (11 nuevas) y varias marcadas resueltas; modificadores de precio por Rol + delivery físico en "Tienda, Economía y Onboarding"; pipeline único de persistencia en "Identidad y Persistencia"; regla UI escalable + 4 tabs de combate en "Arquitectura General".
4. **Notion — Raíz del wiki**: roadmap actualizado a julio 2026 (overhaul combate v3 ✅, modelo nuevo MoriMochi 🔶 en progreso).
5. **Vault — `Index/13 - Combat Design Direction` actualizado a v4**: nueva sección "MODELO VIGENTE S46-S47" (energía eliminada, dos vías de marcas, sobreescritura, orden de turno unificado, escudo por ronda); secciones históricas con energía marcadas como pre-S46. Saldada la deuda documental de S46.

> ### 📝 Notas S51
> 1. Incidente resuelto: la primera autenticación de Notion MCP entró al workspace institucional (tecsup) → 404; se reconectó con la cuenta SowtankDev (gmail) y se verificó con fetch antes de escribir. Si Notion vuelve a dar 404, revisar la cuenta del conector.
> 2. Para revisión de Juan en Notion: (a) subsección Legacy de Personalidades en "Vida y Comportamiento"; (b) el agente marcó resueltas algunas preguntas preexistentes no indicadas (Evolución Tier2/Tier3, Sets, presupuesto de Venganza) — verificar; (c) especificación pendiente marcada en Notion: cómo evoluciona exactamente la unidad ganadora en 3v3.
> 3. Venganza y Bidding siguen sin contraparte en código/vault — confirmado como diseño de etapa futura, no descartado.
> 4. Sigue pendiente de S50: archivar sesiones viejas de este archivo (>315KB) y decidir sobre `Assets/_Recovery/`. Y el primer paso de gameplay sigue siendo el test visual del salto + cuerpo elástico.

**Files Touched (.cs — input ScriptNodes):** ninguno (sesión de documentación; vault-documenter no aplica).

---

**Session:** 2026-07-16/17 (Session 50 — **FEEL DEL AVANCE APROBADO POR JUAN ✅ + ESTÉTICA DE LA HOJA + EXPERIMENTO GANG BEASTS (brazos físicos + salto + cuerpo elástico emergente) + REGLA UI ESCALABLE — ✅ CERRADA: 0 errores de consola, escena guardada; el salto/elástico quedó SIN test visual de Juan (cerró sesión antes)**)
**Focus:** Juan aprobó visualmente el fix del avance de S49 ("lo conseguimos, luce bien") — pendiente #1 cerrado. Dirección nueva de Juan para las animaciones de combate del MoriMochi: caminar/atacar/saltar/rotar, squash & stretch súper elástico, pose de derrota, loop de victoria, brazos que jueguen — y que sea **orgánico/emergente estilo Gang Beasts, no estrictamente codeado**. Approach acordado: HÍBRIDO — gait aprobado se queda kinematic; la vida y las acciones salen de física con resortes.

1. **Estética de la hoja (S48 nota 10) ✅**: patas cono (mesh procedural `SpiderConeMesh.asset` reutilizable, base en la articulación y punta dentro de la garra — reemplazó los 6 cilindros), 2 colmillos marfil bajo la cara (`Spider_Fang.mat` nuevo), marca de patita pad+3 dedos sobre el lomo (`Spider_PawMark.mat` nuevo). Todo bajo `BodyVisual`, grupo Undo "S50 Spider estetica". Screenshots `s50_estetica_*.png`.
2. **Brazos físicos (Gang Beasts v1, SIN código)**: `Arm_L`/`Arm_R` reparentados de `BodyVisual` al root (un rb dinámico no puede vivir bajo un transform animado), ahora Rigidbody dinámico (masa 0.15, sin collider) + `ConfigurableJoint` al rb del root: lineal Locked, angular Limited ±60°, Slerp drive spring 50 / damper 4, target = pose de reposo. Bamboleo 100% emergente. CLAVE: quedan FUERA del array `bodies` de `SpiderRagdollMode` → siempre dinámicos en ambos modos, cero cambios a ese script.
3. **Salto + cuerpo elástico (Iteración A del set de acciones)**: `SpiderJump` NUEVO (Space o botón del panel; impulso + gravedad integrada con `gravityScale`, expone `HeightOffset` que el controller SUMA al ride height del raycast) y `SpiderElasticBody` NUEVO (dueño EXCLUSIVO de `localScale` de `BodyVisual` — pos/rot siguen siendo de `SpiderBodyMotion`, un dueño por dato; resorte subamortiguado que persigue la velocidad vertical REAL con conservación de volumen xz=1/√y; el squash de aterrizaje EMERGE del resorte al cortarse la velocidad; guarda anti-teleport |vy|>12). Knobs nuevos en `SpiderTuningSO`: `elasticAmount`, `jumpImpulse` + sliders Elasticidad/Salto y botón "Saltar! (Space)" en el DevPanel. Wireado en escena por MCP (grupo Undo "S50 wiring salto+elastico").
4. **REGLA NUEVA DE JUAN (permanente, guardada en memoria)**: toda UI debe escalar con el screen size — nunca píxeles absolutos. Aplicado a `SpiderDevPanel` y `SpiderGaitMonitor`: `GUI.matrix = Scale(Screen.height/1080f)` (mín 1) + rects en coordenadas virtuales. Para UITK futura: PanelSettings ScaleWithScreenSize; uGUI: CanvasScaler.
5. **REGLA NUEVA DE JUAN (workflow, guardada en memoria)**: no abusar del Play mode por MCP (tiempo/tokens) — verificación puntual y solo cuando él apruebe; el feel/visual lo prueba él con su teclado.

> ### 📝 Notas S50
> 1. **⚠️ Salto + elástico SIN aprobación visual de Juan** — primer paso de S51: Play, Space para saltar, sentir el squash de aterrizaje y el jelly al caminar, tunear sliders Elasticidad/Salto.
> 2. Durante el salto las patas intentan quedarse plantadas (el IK se estira hacia sus homes en el piso) — en saltos bajos se lee como anticipación cartoon. Si Juan quiere que recoja las patitas en el aire, es la primera mejora de la Iteración B.
> 3. Posible desfase visual en hombros: brazos anclados al root pero el caparazón bobea aparte (`BodyVisual`) — con amplitudes actuales debería ser imperceptible; vigilar.
> 4. Knob de brazos si se sienten flácidos/tiesos: spring/damper del slerpDrive de los 2 joints (hoy 50/4, en escena, no en el SO).
> 5. `execute_code` ahora compila con **Roslyn** (C# moderno OK) — el quirk "solo C# 6" de Index/12 ya no aplica en este editor (verificado varias veces esta sesión). OJO: `Object` sigue ambiguo — calificar `UnityEngine.Object`.
> 6. El refresh de scripts nuevos necesita `refresh_unity` scope **all** (scope scripts no importa archivos nuevos → CS0246 fantasma).
> 7. `Assets/_Recovery/` (del cuelgue de Unity en S49) sigue sin trackear — decisión de Juan borrarlo o no. El `09 - Active Context.md` ya pesa >315KB — archivar sesiones viejas sigue pendiente.

**Files Touched (.cs — input ScriptNodes):**
- `Prototype/Spider/SpiderJump.cs` (NUEVO): integración vertical del salto (impulso+gravedad propia), lee Space de `Keyboard.current`, respeta ragdoll. Contrato: `HeightOffset`/`IsAirborne`/`Jump()`.
- `Prototype/Spider/SpiderElasticBody.cs` (NUEVO): dueño exclusivo de `localScale` del pivote visual; resorte subamortiguado sobre velocidad vertical real, conserva volumen; reset en ragdoll.
- `Prototype/Spider/SpiderBodyController.cs` (MODIFICADO): + ref `SpiderJump jump`; la altura del raycast suma `jump.HeightOffset`.
- `Prototype/Spider/SpiderTuningSO.cs` (MODIFICADO): + Header "Elastico" con `elasticAmount` (0-1) y `jumpImpulse` (1-6).
- `Prototype/Spider/SpiderDevPanel.cs` (MODIFICADO): escala con pantalla (GUI.matrix ref 1080p); + ref jump, sliders Elasticidad/Salto, botón "Saltar! (Space)".
- `Prototype/Spider/SpiderGaitMonitor.cs` (MODIFICADO): escala con pantalla (GUI.matrix, anclaje derecho en coordenadas virtuales).

**Files Touched (no-ScriptNode):** `Resources/Scenes/MorimonchiNewModel.unity` (conos/colmillos/patita, brazos reparentados+rb+joints, componentes nuevos wireados), `Prototype/SpiderConeMesh.asset` (NUEVO), `Prototype/Materials/Spider_Fang.mat` + `Spider_PawMark.mat` (NUEVOS), `Assets/Screenshots/s50_estetica_*.png` (2, borrables), `Prototype/SpiderTuning.asset` (campos nuevos serializados).

**Next session (S51):**
1. **Aprobación de Juan del salto + cuerpo elástico** (nota 1) y tuning de sliders; ajustar spring de brazos si hace falta (nota 4).
2. **Iteración B del set de acciones** (física primero): atacar (lunge + manotazo vía `targetRotation` del resorte del brazo), recoger patas en el aire si Juan lo pide (nota 2), gesticulación de brazos al caminar (Perlin sobre `targetRotation`).
3. **Iteración C**: derrota (soltar ragdoll, quizá con drives debilitados para desinfle) + loop de victoria (botes por resorte + brazos arriba).
4. Arrastran: terreno irregular (prueba de fuego del gait), decisión de fondo procedural vs alternativas, volcar MODELO NUEVO S46 a `Index/13`, quirk Empático, F5 async 3v3, economía F7, ítems con estados, tuning de knobs elementales, archivar sesiones viejas de esta nota (>315KB).

**ScriptNodes (cierre S50):** CREAR `SpiderJump.md`, `SpiderElasticBody.md`; ACTUALIZAR `SpiderBodyController.md`, `SpiderTuningSO.md`, `SpiderDevPanel.md`, `SpiderGaitMonitor.md`.

---

**Session:** 2026-07-16 (Session 49 — **FEEL DEL AVANCE: DIAGNÓSTICO CERRADO + FIX DEL TRIGGER DE TORSIÓN — ✅ verificado por telemetría MCP en Play (0 errores de consola), ⚠️ FALTA LA APROBACIÓN VISUAL DE JUAN (Unity se le colgó en reload al cierre, reinicia)**)
**Focus:** el pendiente #1 de S48 ("el giro luce bien y moverme adelante/atrás se ve raro"). Diagnóstico por A/B con `SpiderGaitMonitor` + muestreo de `Twist` vivo: **H1 confirmada** — el disparador de torsión de S48 disparaba constantemente en marcha recta, amplificado por la urgencia.

1. **Causa raíz**: `Twist` se calcula contra el home PREDICHO (`lastHome` incluye `hipVelocity × anticipation`). A `moveSpeed=2.36` con `anticipation=0.22` la predicción corre el home 0.52u adelante — comparable al offset de pata (~0.6u). Para la trasera (offset −0.45 en z) el home quedaba casi sobre la cadera apuntando ADELANTE → **`Twist=180°` permanente caminando recto** (medido). La guarda `sqrMagnitude < 0.0004` no cubre este caso. Las delanteras también pasaban de 22° en recta (58.8° medido). La urgencia (ratio 180/22 = 8× → clamp 0.5) hacía a la trasera ametralladora: **6.74 pasos/s vs 3.68 de las delanteras** (A/B: con `maxTwist=60` apenas bajaba a 6.0 — el ángulo degenerado supera cualquier umbral). `turnSpeed=60` descartado como causa. La traslación del root está sana (velocidad medida = exactamente `moveSpeed`).
2. **Fix (vía `morimonchi-coder`, 2 archivos)**: el twist (trigger + aporte a urgencia) se gatea con **`turningNow || !moving`** — activo girando y en reposo, apagado SOLO en marcha recta. `SpiderBodyController` computa `bool turning = |turn| > 0.01` (input A/D + AutoTurn) y lo pasa por la firma nueva **`Tick(bool mayStep, bool turning)`**; `SpiderLegStepper` guarda `turningNow`. El `|| !moving` importa: en reposo no hay anticipación que degenere la métrica, y sin él una pata quedaba clavada tras frenar con drag 0.54 (justo bajo el umbral de reposo 0.545) y twist 52° — regresión detectada en el primer retest y corregida.
3. **Verificación (todo por MCP en Play, laps automáticos vía `EditorApplication.update` porque el plano 40×40 queda corto)**: recta 56s → FL/FR 186/186 alternando + trasera 231 (4.1 pasos/s, la diferencia restante = no paga turno, por diseño); giro en el lugar 43s (~7 vueltas) → 104/104/116 parejo, comportamiento aprobado del giro INTACTO; quieto 45s → 0 pasos; freno → exactamente 1 paso de asentamiento por pata, pose final drag≈0/twist≈0.

> ### 📝 Notas S49
> 1. **⚠️ Juan NO vio el resultado todavía**: Unity se colgó en reload al cierre y reinicia. Primer paso de S50: Play en `MorimonchiNewModel`, probar W/S + giro + freno, y que Juan apruebe (o no) el feel.
> 2. La trasera sigue leyendo `Twist=180°` en recta (la métrica sigue degenerada por la anticipación) — hoy es INOFENSIVO porque está gateada, pero si algún día el twist se necesita en marcha recta, calcularlo contra el home SIN anticipación.
> 3. `SpiderTuning.asset` quedó con sus valores originales (`maxTwist=22`; durante el test se subió a 60 y se restauró). El diff de git es solo line-endings.
> 4. Truco de test: para corridas largas de autowalk, registrar un callback en `EditorApplication.update` que teletransporta al spider a z=−18 cuando z>12, controlado por un GameObject marcador `__LapMarker` (destruirlo desregistra). La guarda anti-teleport del stepper lo absorbe. `maxDrag` del monitor queda contaminado por los teleports (~30) — ignorarlo; métricas robustas: conteo de pasos y esperas.
> 5. La escena activa quedó `MorimonchiNewModel` (se cambió desde `CombatVisualizerMM`, que estaba limpia).

**Files Touched (.cs — input ScriptNodes):**
- `Prototype/Spider/SpiderBodyController.cs` (MODIFICADO): computa `bool turning` del input de giro y lo pasa a las patas vía la firma nueva `Tick(mayStep, turning)`.
- `Prototype/Spider/SpiderLegStepper.cs` (MODIFICADO): contrato `Tick(bool)` → `Tick(bool mayStep, bool turning)`; campo `turningNow`; trigger de torsión y su componente de urgencia gateados con `turningNow || !moving`.

**Files Touched (no-ScriptNode):** `Prototype/SpiderTuning.asset` (solo line-endings, valores intactos).

**Next session (S50):**
1. **Aprobación visual de Juan del feel del avance** (nota 1) — la telemetría dice que la recta volvió al régimen pre-fix-del-giro, pero el ojo decide.
2. Si el feel cierra: estética pendiente de la hoja (colmillos, marca patita, conos ProBuilder) y/o terreno irregular (la prueba de fuego: piedras/rampas).
3. Decisión de fondo pendiente (arrastra de S48): si el approach procedural no convence → squash-and-stretch cartoon, asset comprado, o clips a mano + Animation Rigging.
4. Arrastran: volcar el MODELO NUEVO de S46 a `Index/13`, quirk Empático, F5 async 3v3, economía F7, ítems con estados, tuning de knobs elementales. El propio `09 - Active Context.md` ya pesa 310KB — considerar archivar sesiones viejas.

**ScriptNodes (cierre S49):** ACTUALIZAR `SpiderBodyController.md`, `SpiderLegStepper.md`.

---

**Session:** 2026-07-15 (Session 48 — **POC ARÁCNIDO PROCEDURAL: HUMANOIDE DESCARTADO → TRÍPODE SEGÚN HOJA DE ARTE + IK ANALÍTICO + GAIT PREDICTIVO + RAGDOLL TOGGLE + PLAYGROUND — ✅ CERRADA: 0 errores en consola, todo verificado en Play por MCP con telemetría; el giro aprobado por Juan, el avance quedó raro al final (pendiente #1 de S49)**)
**Focus:** la sesión iba a iterar el ragdoll de S47 y Juan la redirigió dos veces: primero a "controlador simple + movimiento básico como POC, sin tanto detalle" (spider walk procedural, la opción más barata), y a mitad de sesión trajo una **hoja de concepto de arte** (versión arácnida: bola + cara enorme + 3 patitas + 2 brazitos, paletas NOCTURNO/ARENA/SELVA/GLACIAL/ALBINO) que **descartó el rig humanoide** construido en la primera mitad. TODO es prototipo explícito (carpeta `Scripts/Prototype/Spider/`): a futuro entra modelo real + Animator + RigBuilder (Animation Rigging ya descargado por Juan; `TwoBoneIKConstraint` mapea 1:1 con nuestro IK — cadera/rodilla/pie + pole).

1. **Rig humanoide v1 (DESCARTADO, desactivado en escena, no borrado)**: 4 patas 3-segmentos FABRIK + brazos, caminó verificado (13-19u por inyección de teclado). Juan al ver el resultado: "ya no se ven como mascotas" → hoja de arte.
2. **Rig trípode v2 (`MoriMonchiSpider`)**: caparazón esfera + cara + 6 ojos (2 grandes con brillo + 4 chicos) + 2 brazitos con garra + 3 patas (2 adelante, 1 atrás) de **2 huesos con IK analítico** (ley de cosenos + pole vector — FABRIK borrado, era iterativo y la rodilla derivaba). Huesos = empties SIN escala que rotan; mallas hijas con su escala (estructura lista para Animator/RigBuilder). Patas salen de la BASE del caparazón (caderas y=-0.34, rodilla ARRIBA de la cadera = codo de araña). Materiales paleta NOCTURNO en `Prototype/Materials/`.
3. **Gait (3 rondas de feedback de Juan, validadas con telemetría)**: (a) reactivo→**PREDICTIVO**: home adelantado por `velocidad de la propia cadera × anticipation` — la rotación sale gratis (velocidad tangencial); (b) **bípedo + seguidora**: FL/FR alternan estricto (gaitGroup 0/1), trasera `gaitGroup=-1` = INDEPENDIENTE (pisa sin bloquear ni ser bloqueada; convención nueva: grupo negativo = fuera del sistema de turnos); (c) anti-jank: smoothstep en el paso, refractario 0.09s, parado umbral ×1.6 + paso de asentamiento lento sin overshoot (mata el shuffle hacia atrás al frenar — verificado: 30s quieto = 0 pasos, freno = exactamente 1 paso de asentamiento); (d) giro: **disparador de torsión** (`Twist > maxTwist`, ángulo pie-vs-home respecto a la cadera) + **pasos por urgencia** (paso más rápido y descanso más corto si muy atrasada) — torsión máxima girando 111°→69°, turnSpeed default 90→60 (pies no deben barrer más rápido que moveSpeed).
4. **Capa de vida (`SpiderBodyMotion`)**: respiración + vaivén Perlin + bob al caminar + lean/banking, sobre pivote visual `BodyVisual` (hijo del root con caparazón/cara/ojos/brazos) — física y pies no se enteran; en ragdoll se resetea y apaga.
5. **Modo ragdoll con booleano (`SpiderRagdollMode`, pedido de Juan)**: walk=rbs kinematic+interp None / ragdoll=dinámicos+Interpolate, sincroniza `rb.position` ANTES de soltar (sin salto). ConfigurableJoints por hueso (cadera 3 ejes ±40°, rodilla 1 eje -70..0) configurados sobre la pose de reposo calculada con la MISMA matemática del IK. Verificado ida y vuelta walk→ragdoll→walk y caída desde 1.2 sin explotar.
6. **Playground (`SpiderDevPanel` IMGUI izquierda + `SpiderTuningSO` asset)**: sliders Velocidad/Giro/Altura/Largo/Alto/Duración/Apertura/Anticipación(overshoot)/Predicción/Torsión max/Idle/Bob/Inclinación/Impulso + botones Activar ragdoll / **Lanzar!** / Reset + toggle Auto-caminar. El SO es el único dueño del dato (panel edita, componentes leen, se persiste con SetDirty). **`SpiderGaitMonitor`** (IMGUI derecha): por pata pasos/drag/esperas + Report() para verificación por MCP.

> ### 📝 Notas S48
> 1. **PENDIENTE #1 S49 (palabra de Juan al cierre)**: tras el fix del giro, "el giro luce bien y moverme adelante/atrás se ve raro". Sospechosos: el disparador de torsión o los pasos por urgencia metiendo ruido en marcha recta (antes del fix el recto estaba aprobado), o el turnSpeed 60 cambiando la percepción. Diagnóstico: comparar en el monitor recto puro con `maxTwist` alto (60 = trigger casi apagado) vs 22.
> 2. **Rigidbody kinematic + `interpolation=Interpolate` NO sigue al padre** que se mueve por transform (acumula desfasaje, medido +43u; `autoSyncTransforms=true` NO lo arregla — duplica movimiento). Por eso el toggle maneja interpolación: None en walk, Interpolate en ragdoll.
> 3. **Inyección de teclado por MCP no confiable**: el Input System resetea el estado sin foco del editor; tocar `InputSystem.settings` lo empeoró (revertido, sin dirty en disco). Para verificar movimiento: `AutoWalk`/`AutoTurn` (propiedades públicas del controller) + `SpiderGaitMonitor.Report()`.
> 4. El proyecto es **Input System nuevo only** (`activeInputHandler: 1`): `Input.GetAxis` tira excepción. El prototipo lee `Keyboard.current` directo A PROPÓSITO (escena aislada, sin action maps).
> 5. Escala no uniforme deforma hijos: por eso huesos sin escala + mallas hijas, y los ojos cuelgan de `Eyes` (root) con posición calculada sobre el elipsoide de la cara, NO como hijos de `Face`.
> 6. Los ConfigurableJoints capturan su pose de reposo al crearse: mover huesos después = joints tirando a pose vieja. Las patas se REHACEN, no se mueven (pasó al bajar las caderas a la base).
> 7. `maxDrag` del monitor quedó no comparable entre configs con predicción distinta (se mide contra el home predicho). Métricas robustas: esperas y conteo de pasos.
> 8. Unity 6: `rb.linearVelocity` (no `.velocity`). `execute_code` sigue C# 6 (sin local functions).
> 9. La cámara de la escena quedó mirando la CARA (+Z); `Body` del rig viejo quedó rotado 180° (miraba -Z y caminaba de culo). El plano es 40×40 — los tests largos de autowalk se salen (teleport de vuelta, el stepper tiene guarda anti-teleport >5u/s).
> 10. Pendiente estética de la hoja: colmillos, marca de patita en caparazón, patas cono (ProBuilder, no hay primitiva). Los brazitos/ojos son mallas estáticas (en ragdoll viajan con el caparazón, no cuelgan).
> 11. `Assets/Samples/Animation Rigging/` (samples importados por Juan) y `Packages/manifest.json` tocados por la instalación — suyos, sin commitear por mí.

**Files Created (.cs — input ScriptNodes, TODOS en `Assets/RunRunSimulator/Scripts/Prototype/Spider/`):**
- `SpiderTuningSO.cs` (NUEVO): ScriptableObject dueño de TODOS los knobs del prototipo (cuerpo/patas/cuerpo vivo/ragdoll). SO plano sin Odin (solo floats).
- `SpiderBodyController.cs` (NUEVO): input WASD (`Keyboard.current`) + AutoWalk/AutoTurn, altura por raycast, orquesta el gait: selección "most-overdue" con grupos (negativo = independiente), tickea steppers y resuelve IKs en dos loops.
- `SpiderLegStepper.cs` (NUEVO): decide cuándo/dónde pisa UNA pata. Home predictivo (velocidad de cadera × anticipation, guarda anti-teleport), disparadores por distancia Y torsión, pasos smoothstep con arco, urgencia (duración/descanso adaptativos), modo reposo (umbral ×1.6, asentamiento lento). Sin Update: lo tickea el controller. Contrato: `IsStepping/FootPosition/Home/Drag/Twist/WantsStep/Tick(bool)`.
- `SpiderLegIK.cs` (NUEVO): IK analítico 2 huesos (ley de cosenos + pole). Aplica SOLO rotaciones a la jerarquía de huesos. Contrato: `SolveTo(Vector3)/KneePosition/PolePosition`.
- `SpiderRagdollMode.cs` (NUEVO): interruptor walk↔ragdoll (booleano en inspector + `SetRagdoll(bool)`). Maneja kinematic, interpolación, sync de pose pre-suelta, enabled de controller/IKs.
- `SpiderBodyMotion.cs` (NUEVO): capa de vida del pivote visual (respiración, Perlin sway, bob, lean/banking). Se apaga en ragdoll.
- `SpiderDevPanel.cs` (NUEVO): playground IMGUI izquierda — sliders del SO + botones ragdoll/lanzar/reset + auto-caminar.
- `SpiderGaitMonitor.cs` (NUEVO): telemetría IMGUI derecha — por pata pasos/drag/esperas, `ResetStats()/Report()`.

**Files Touched (no-ScriptNode):** `Resources/Scenes/MorimonchiNewModel.unity` (rig humanoide v1 y Head/Torso/LowerBody con física DESACTIVADOS no borrados — reactivarlos restaura el ragdoll S47; `MoriMonchiSpider` nuevo completo con BodyVisual/patas/joints/componentes wireados; plano 40×40; cámara a la cara), `Assets/RunRunSimulator/Prototype/SpiderTuning.asset` (NUEVO, valores tuneados), `Assets/RunRunSimulator/Prototype/Materials/Spider_*.mat` (6 NUEVOS, paleta NOCTURNO), `Assets/Screenshots/s48_*.png` (verificación, borrables), `Packages/manifest.json` + `Assets/Samples/Animation Rigging/` (instalación de Juan).

**Next session (S49 — pulir el feel del avance, palabra de Juan):**
1. **El avance adelante/atrás quedó raro tras el fix del giro** (nota 1): diagnosticar con el monitor si el trigger de torsión o la urgencia ensucian la marcha recta; A/B con maxTwist 60 vs 22.
2. Si el feel cierra: estética pendiente de la hoja (colmillos, marca patita, conos ProBuilder) y/o terreno irregular (la prueba de fuego del spider walk: piedras/rampas).
3. Decisión de fondo pendiente: si el approach procedural no convence tras el pulido → alternativas evaluadas: squash-and-stretch cartoon (menos sim, más Tamagotchi), asset comprado de locomoción (precedente fur shaders), o clips a mano + Animation Rigging (ya descargado).
4. Arrastran de S47: volcar el MODELO NUEVO de S46 a `Index/13` (§3 sigue describiendo la energía que ya no existe), quirk Empático, F5 async 3v3, economía F7, ítems con estados, tuning de knobs elementales.

**ScriptNodes (cierre S48):** CREAR `SpiderTuningSO.md`, `SpiderBodyController.md`, `SpiderLegStepper.md`, `SpiderLegIK.md`, `SpiderRagdollMode.md`, `SpiderBodyMotion.md`, `SpiderDevPanel.md`, `SpiderGaitMonitor.md`.

---

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


> Sesiones S45 y anteriores: [[09b - Session Archive]]
