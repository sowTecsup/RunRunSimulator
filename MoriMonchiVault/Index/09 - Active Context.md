---
tags: [index, core]
---

# 09 - Active Context

**Session:** 2026-07-22 (Session 63 — **SESIÓN DE DISEÑO: "MoriMonchis vivos" — diagnóstico del comportamiento en GameScene + roadmap V1-V6 de vida/socialización — ✅ CERRADA: solo diseño, 0 código tocado**)
**Focus:** Juan con poco tiempo pidió sesión de diseño y planeación: diagnóstico del MoriMochi en GameScene y teorizar cómo lograr que se sientan vivos (interacción entre ellos, personalidades, petting real, curiosidad, entorno). Prioridades declaradas del juego: (1) criar/emparejar/cuidar, (2) combate, (3) manejar la tienda. Se planea REMOVER el sistema de building actual.

1. **Diagnóstico (leído del vault, sin abrir .cs)**: la base es sólida — FSM de 9 estados compuesta (S55: Brain/Physics/Confinement/Context), necesidades H/E/A con estaciones y reserva, `RoleWorldProfileSO` por rol, `MonchiMoodDriver` (caras por Condition/Intent con stagger), física grab/throw/knock robusta, cortejo orbit/tend (código de comportamiento en pareja YA existe). Agujeros de "vida": (a) los monchis NO se perciben entre sí (toda la percepción apunta al jugador; entre ellos solo Knock físico accidental); (b) personalidad = tuning numérico, no conducta legible; (c) petting = E instantáneo sin animación; (d) entorno = solo estaciones de necesidad, cero curiosidad; (e) nada es iniciado por la criatura (agencia siempre del jugador).
2. **Personalidades — propuesta acordada como dirección**: 3 arquetipos marcados ligados a rol (Protector = *guardián*: patrulla, se interpone, duerme cerca del triste; Agresivo = *gremlin*: territorial, roba Feeder, tira cosas, inicia peleas de juego; Empático = *social*: saluda, inicia juegos, te busca, se deprime en soledad) + 2 diales genéticos heredables que modulan sin romper el arquetipo (**Sociabilidad** y **Osadía**, herencia 50/30/20) — así el temperamento SE CRÍA (core loop #1). Los diales pueden diferirse si se quiere V1 minimalista.
3. **Sistema social propuesto**: colaborador nuevo `AgentSocial` (encaja en la composición S55 sin tocar Brain/Physics) + percepción staggered cada 2-4s (patrón MoodDriver) + servicio runtime `SocialGraph` (score de afinidad por par de IDs; semillas: química elemental de ElementTable, rol, parentesco genético; muta con historia: jugar suma, pelear resta; persistencia regla NeedsState — NUNCA RegistryChanged, flush en quit/pause). Interacciones de a pares: jugar/perseguirse (variante del orbit de cortejo), pelea de gremlins (Knock existente + moods, sin daño real), dormir juntos (afinidad alta + Energy baja), evitarse (filtro en `NextRoamDestination`, casi gratis). Legibilidad: emote bubbles world-space (reciclar patrón NameTag/CombatSpeechBubbles).
4. **Jugador y entorno propuestos**: petting hold-E con acercamiento + animación (boost escala con duración); dar de comer de la mano (Hotbar en mano → el monchi viene, los tímidos dudan); agencia de la criatura (el Empático te saluda al entrar, Affect crítico te sigue y llora — es Reacting iniciado por necesidad/personalidad); `PointOfInterest` en props (`WorldPropInstance` ya etiqueta) + estado `Investigating` (olfatear muebles, perseguir ThrowableObjects); hideouts (variante NeedStation sin refill) y escaparse si la puerta queda abierta (anécdota "¿dónde está Michi?").
5. **Volar — recomendación: NO vuelo libre** (todo el stack es NavMesh+ragdoll; vuelo 3D = reescribir locomoción/contención/corrales). En cambio: saltitos/planeos/aleteos con la física Thrown existente + OffMeshLinks para superficies — 90% de la sensación a 10% del costo. Vuelo real queda como pregunta abierta post-remoción del building.

> ### 📋 Roadmap "vivos" propuesto (V1-V6, ~1 sesión c/u, orden por vida-por-esfuerzo)
> - **V1 — Fundamento social**: AgentSocial + percepción staggered + emote bubbles + acercarse/evitarse por afinidad estática (elemento/rol, sin historia). Sin dependencias.
> - **V2 — Interacciones de a pares**: jugar/perseguirse, pelea de gremlins, dormir juntos; SocialGraph con historia y persistencia ligera. Depende V1.
> - **V3 — Arquetipos legibles**: conductas firma por rol + diales genéticos Sociabilidad/Osadía en DNA y breeding. Depende V1 (mejor tras V2).
> - **V4 — Momento de interacción**: petting hold-E animado, comer de la mano, la criatura te saluda/reclama. Independiente — adelantable si se quiere impacto visible rápido.
> - **V5 — Entorno y travesura**: PointOfInterest, Investigating, perseguir objetos, hideouts, escaparse. Depende V1; diseñar POST-remoción del building (define el espacio/NavMesh).
> - **V6 — Verticalidad**: saltitos/planeos con física Thrown + OffMeshLinks. Depende V5.
> - **Primera sesión de implementación recomendada**: V1 + versión mínima de jugar/pelear de V2 — dos monchis persiguiéndose con emotes es el momento "están vivos"; el código base (orbit, knock, mood, stagger) ya existe.

> ### 📋 Preguntas de diseño abiertas (S63, sin respuesta de Juan aún)
> 1. ¿Diales genéticos de personalidad entran en V3 o arquetipos puros por ahora?
> 2. ¿La pelea de gremlins puede bajar Health o solo Affect?
> 3. ¿Un monchi escapado puede perderse DE VERDAD (fuera del registry visible hasta encontrarlo) o siempre recuperable a la vista?
> 4. Remoción del building: decisión declarada pero sin sesión asignada — condiciona V5/V6 (NavMesh/áreas).

**Files Touched (.cs):** NINGUNO — sesión de diseño puro (vault-documenter omitido).

**Files Touched (no-ScriptNode):** solo esta nota.

**Next session (S64+):** Juan decide entre: (a) arrancar el roadmap "vivos" por V1(+V2 mínima) o V4; (b) retomar async 3v3 (F5) del roadmap general; (c) sesión de remoción del building. Arrastres previos siguen: beats visuales muerte súbita, posicionamiento 2-3-2, limpieza VCamOf, bodyOverrides del bank, capa morada del escudo, sizing collider 0.7, borrar spider experiment, localization-ready.

---

**Session:** 2026-07-22 (Session 62 — **BALANCE DE COMBATE DATA-DRIVEN: ~10,000 simulaciones Monte Carlo vía MCP, rework pasiva Protector + escudos con TTL + PisoTierra/OverGrow, muerte súbita con desempate por vida, fix crash NRE — ✅ CERRADA: 0 errores, meta comprimido de 60pp a ~23pp de spread, 0% draws**)
**Focus:** Análisis de balance pedido por Juan (S62 declarada): sus dos hipótesis (pasivas de Protector y Empático pobres) contrastadas con data. Metodología: 1 pelea real (solo había una en el registry) + Monte Carlo con `SimulateCore` vía `execute_code` — clones del roster (800 peleas), matriz de composiciones sintéticas (unidades 6/6/6 idénticas, 10 comps de rol vs referencia PAE × 250 seeds), ablaciones de knobs en copias en memoria de las tablas (`Object.Instantiate`, cero mutación de assets), y bucketing elemental. Todo determinista y verificado por log seeded antes de cada batería.

1. **Diagnóstico (estado pre-cambios)**: pasiva Protector absorbía 0.56% del daño del juego (77% del escudo expiraba sin uso al cierre de ronda; ablación 0 vs ×4 = sin efecto medible → problema mecánico, no numérico). Cura Empático sí ganaba peleas (apagarla = −10-14pp winrate) pero beat invisible (~1.5 HP/proc). OverGrow +0.12 escudo/proc (duplicaba escudos de 0-1). PisoTierra 7 marcas removidas en 975 reacciones (consumía el par y no quedaba tercera marca). **Debilidad = no-op total** (ignora DEF y todo el mundo tiene DEF 0 hasta que existan ítems). Matriz: PPP 87% vs AAA 27% — el valor de los roles lo dictaban los stat mods (CON×5 HP domina), no las pasivas. Mono-elemento pierde 16/84 vs trío.
2. **BUG CAZADO Y FIXEADO — crash NRE de producción**: `ShieldAllyPassive.OnAfterStrike` sin guard de null — si el Protector era el último vivo y moría por reflejo Charcoal durante su strike, la fase pasiva corría igual, `PickAlly` devolvía null → NRE (~0.1-0.6% de peleas; habría crasheado clientes en async F5). Las otras dos pasivas ya tenían guard.
3. **Rework aprobado por Juan e implementado** (vía `morimonchi-coder`, 2 tandas): (a) **escudos con TTL**: `Combatant.ShieldExpiresAfterRound` + `CombatResolver.Round`; grant en ronda R expira al cierre de R+1 (antes: todo escudo moría al cierre de la ronda del grant); (b) **pasiva Protector targetea al aliado con menor % de vida** (`CombatTargeting.LowestHpPercentAlly`, sin rng; todos al 100% → `PickAlly` random con rng) + guard null; (c) **PisoTierra remueve solo marcas ALIADAS** (debuff real; sin candidatas = no-op sin consumir rng); (d) **OverGrow dual**: duplica escudo o, si no hay, otorga `GrantAmount` (2). Resultado: escudo absorbido 14.6%→61.7% del pool, PisoTierra 65.3% efectiva, OverGrow 2.21 escudo/proc.
4. **Tuning de Juan + mío en assets**: RoleTable — Protector CON +3 (era +4), ATK −2, SPD −2; Agresivo CON −2, **ATK +3** (era +2), SPD +1; Empático CON 0, ATK −2, SPD +2, heal 60%. ElementTable — Vaporizado 40%, Boiling 40%, GolpePreciso 35% (Descriptions actualizadas con los números); escudo pasiva 2.0. La regla net-zero de S37 queda formalmente rota (decisión consciente: CON vale mucho más que ATK punto a punto).
5. **MUERTE SÚBITA (decisión de diseño de Juan, estilo Clash Royale)**: multiplicador de daño de strikes por ronda — knobs `CombatManagerSO.SuddenDeathStartRound` (=6) + `SuddenDeathMultipliers` {1.4, 1.8, 2.2, 2.6, 3.0} (R5 ×1.0, R6 ×1.4 … R10 ×3.0, clamp al último). Aplica en `CombatStrike` post-DEF pre-Boiling (`MSx{n}` en el log); daños fijos (Mareado/Leech) quedan planos. Al llegar a MaxRounds con ambos en pie: **desempate por % de vida total de equipo** (`TeamHpPercent`), gana el mayor, consecuencias completas (evolución + roll de muerte); empate exacto = draw (rarísimo). Resultado: **0.0% draws en 1,500 peleas** (antes 5.7%), 99.7% cierra por wipe natural, tiebreak 0.9% — y de yapa le afeitó a PPP las victorias por atrincheramiento.
6. **Meta final validado** (matriz vs referencia PAE): PPP 66.4 · PPE 62.0 · EEE 60.8 · AAA 49.6 · PAE espejo 47.2 · AAE 43.6 — spread 23pp (arrancó en 60pp). Criterio de Juan: **ideal todos entre 40-60%**; solo PPP excede. Palancas descartadas con data: Protector ATK −1 (PPP cae a 37.6, demasiado gruesa) y escudo 1.5 (PPP −4pp pero EEE +2.4, reacomoda sin comprimir). **Decisión: knobs congelados hasta tener data real del async F5** (la matriz sintética es el mejor caso de PPP; en juego real cuesta breeding+elementos+permadeath).
7. **Capa elemental medida**: cada elemento distinto extra ≈ +10pp winrate (3v2 = 57.4%, 2v3 = 37.5%; mono vs trío 18/82). Ranking de tríos: dejar fuera Agua es lo peor (43.2% vs ~55-60 los demás) — **los 3 buffs de hoy son reacciones de Agua → Agua quedó premium** (±8pp). Roster de Juan: 1 solo Agua → recomendado criar Agua (más valioso que cualquier sim para SU juego).

> ### 📌 Notas S62
> 1. QUIRK Odin: campo nuevo con initializer (`GrantAmount = 2f`) en clase ya serializada en un asset deserializa en 0, NO en el default — hubo que setearlo a mano en `ElementTable.asset`. Chequear siempre tras agregar campos a leaves serializados.
> 2. QUIRK MCP: `execute_code` compila el código como cuerpo de método — sin `using`, tipos totalmente calificados (`MoriMonchiSimulator.X`). Respuestas >~30s pueden perderse en el transporte (la ejecución corre igual — `get_history` la recupera); batches largos → escribir resultados a archivo.
> 3. Harness de balance reusable: clones de DNA a mano (nunca los del registry — `SimulateCore` muta), ablaciones con `Object.Instantiate(config/tabla)` + mutación de la copia (RoleProfile es clase → mutación directa), `DestroyImmediate` al final. Resultados de todas las baterías en el scratchpad de la sesión (`mc_*.txt`).
> 4. El multiplicador de muerte súbita NO se emite como evento visual propio — el replay muestra los números amplificados del record y el log lleva `MSx{n}`. Para el beat visual (ver Next) probablemente convenga un evento dedicado en vez de parsear el log.
> 5. Los textos de `ElementTable.asset` (Short/Description de OverGrow, PisoTierra, Vaporizado, Boiling, GolpePreciso) fueron reescritos por IA en el estilo de Juan, con su autorización explícita de esta sesión.

**Files Touched (.cs — input ScriptNodes):**
- `Systems/Combat/Combatant.cs` (MODIFICADO): campo `ShieldExpiresAfterRound`.
- `Systems/Combat/CombatResolver.cs` (MODIFICADO): campo de contexto `Round`.
- `Systems/Combat/CombatService.cs` (MODIFICADO): `resolver.Round`, `ExpireShields(team, result, round)` con TTL, helper `TeamHpPercent`, cierre reestructurado wipe/tiebreak/draw (consecuencias compartidas).
- `Systems/Combat/CombatStrike.cs` (MODIFICADO): multiplicador de muerte súbita (`config.SuddenDeathMultiplier(r.Round)`) post-DEF pre-Boiling + `MSx` en log.
- `Systems/Combat/CombatTargeting.cs` (MODIFICADO): helper `LowestHpPercentAlly` (menor Hp/MaxHp, empate por Index, sin rng).
- `Data/Combat/RolePassiveBase.cs` (MODIFICADO): `ShieldAllyPassive` targetea menor % de vida + guard null (fix crash) + setea TTL del escudo.
- `Data/Combat/ReactionEffectBase.cs` (MODIFICADO): `RemoveRandomMarkEffect` solo marcas aliadas; `DoubleShieldEffect` dual con campo `GrantAmount`.
- `Data/Combat/CombatManagerSO.cs` (MODIFICADO): knobs `SuddenDeathStartRound`/`SuddenDeathMultipliers` + método `SuddenDeathMultiplier(round)`.

**Files Touched (no-ScriptNode):** `RoleTable.asset` (stat mods de Juan + míos, escudo 2), `ElementTable.asset` (GrantAmount 2, percents Vaporizado/Boiling/GolpePreciso, 5 pares de textos), `CombatManager.asset` (SuddenDeathStartRound 6 — la lista usa el default de código). Registry/QualitySettings dirty por arrastre de sesiones previas, no de esta.

**Next session (S63+):** con el balance congelado, retomar el roadmap: **async 3v3 (F5)** — y el próximo pase de balance se hace con data real de jugadores. Backlog de balance anotado: (1) beats visuales de muerte súbita (multiplicador `x1.0` bajo el número de ronda + pantalla "¡DESEMPATE!" antes de la resolución por vida — pedido explícito de Juan para sesión de tweaking visual, considerar evento dedicado en el record); (2) posicionamiento 2-3-2 SIN explorar (todas las sims usaron lineup default — eje completo pendiente); (3) mono-elemento como decisión de diseño abierta; (4) Debilidad dormida hasta ítems con DEF (F7) — "en el peor de los casos tenemos un slot para otra habilidad" (Juan); (5) vigilar premium de Agua en data real; (6) criterio de balance de Juan: todos los comps entre 40-60%. Arrastres previos siguen: limpieza VCamOf, bodyOverrides del bank, capa morada del escudo, sizing collider 0.7, borrar spider experiment, localization-ready.

---

**Session:** 2026-07-22 (Session 61b — **PACING DEL COMBATE: +20% espaciado, pausa entre etapas, cámaras de fase por tablero (Cinemachine 3), animaciones UI con overshoot, procs sincronizados al impacto, tooltip de stats, ShortDescription en estados — ✅ CERRADA: 0 errores, validado por Juan en vivo ("me gusta mucho más")**)
**Focus:** Sesión larga de feel dirigida por Juan sobre el replay 3v3: descansos entre acciones/turnos, dos etapas legibles (pasivas → pausa → ataque), cámaras `VCam_Ally`/`VCam_Enemy` que él colocó en escena, animaciones UI para los cambios encadenados (afinidad/chips), acción-reacción sincronizada al impacto.

1. **ShortDescription** (pedido previo a la tanda de pacing): `StateDefinition` ganó `[TextArea] ShortDescription`; `ReactionLine` la usa sin truncar con fallback `Truncate(Description, 40)` mientras esté vacía. Juan la llena en `ElementTable.asset` (ya empezó — asset dirty).
2. **Espaciado +20%** (knobs escena CombatVisualizerMM): betweenTurns 0.95→1.14, stateBeat 0.70→0.84, impact 0.50→0.60, deathPause 0.80→0.96.
3. **Fases de turno**: enum `CombatTurnPhase { Rest, Passives, Attack }` + evento **`CombatVisualEvents.OnPhase(fase, actorSide)`** emitido por el service (Passives antes del loop de pasivas, Attack al entrar al bloque de golpe, Rest antes de TurnEnd y en Restore). Knob nuevo **`phasePauseSeconds` (0.9)**: pausa de lectura entre pasivas y ataque (solo si hubo pasivas y hay ataque).
4. **Cámaras de fase — decisión de diseño de Juan (2 iteraciones)**: 3 vistas ESTÁTICAS transicionadas por Priority (blend del Brain). `CombatCameraDirector` se SIMPLIFICÓ: eliminado el seguimiento por unidad (`OnActiveUnit`/`VCamOf`/`lastActive`/`activePriority` fuera — `VCamOf` del service quedó SIN consumidores, candidato a limpieza). Mapeo final POR LADO: **Passives → tablero del actor; Attack → tablero del objetivo** (VCam_Ally=tablero A, VCam_Enemy=tablero B, phasePriority 30 vs escena 10; Rest → ambas a 0). **BUG CAZADO**: el director estaba `enabled=False` en la escena (herencia del experimento de seguimiento) — por eso "no veo el cambio de cámara"; habilitado + blend del Brain 2s→0.6s, escena guardada.
5. **Animaciones UI barra de orden** (`PopIn(el, fromScale, ms)` easeOutBack + opacity, cierre Scale.None()): punto de afinidad nace 2.6× transparente y se asienta con acento (420ms — spec de Juan, exagerado a pedido); chips de marcas/estados nuevos 1.9×/320ms (solo el chip NUEVO — flags reactedNew/armedNew evitan falsos positivos); carta activa pop 1→1.10→1 (240ms).
6. **Procs sincronizados al impacto**: el loop de procs post-golpe se movió ADENTRO del bloque de ataque, tras el popup de daño y antes de esperar `attackDone` — golpe→afinidad→marca se lee causa-efecto mientras el atacante regresa volando. Caso NoAttack conserva camino propio (else, sin duplicar).
7. **Tooltip de stats actuales**: hover sobre el headshot de la carta → Nombre/Elemento/Rol, HP vivo/max, ATK·DEF·SPD, Suerte·Evasión. HP fresco vía `OnUnitHpChanged` + sync en `ApplyState` (`CombatUnitState.Hp` cubre Back/saltos); texto armado AL MOMENTO del hover.

> ### 📝 Notas S61b
> 1. `CombatVisualizerService.VCamOf(side, index)` quedó sin consumidores tras simplificar el director — limpieza pendiente (¿y las vcams por unidad si existen en escena? el prefab MonchiCombatUnit NO tiene CinemachineCamera).
> 2. El blend de cámaras es el DefaultBlend del CinemachineBrain de Main Camera en CombatVisualizerMM (EaseInOut 0.6s) — knob si Juan quiere corte más seco.
> 3. Las 3 VCams del asset quedaron con prio 10 de autor; irrelevante en runtime (el director impone 10/0/0 al arrancar y 30 por fase).
> 4. QUIRK confirmado: componente deshabilitado en escena = OnEnable jamás suscribe → sistema "fantasma" que compila y no hace nada. Chequear `enabled` antes de diagnosticar código.
> 5. El replay SIEMPRE corre en la escena CombatVisualizerMM (`CombatReplayRequest.CombatSceneName` + LoadScene single) — GameScene no tiene visualizador propio.

**Files Touched (.cs — input ScriptNodes):**
- `Data/Combat/ElementTableSO.cs` (MODIFICADO): StateDefinition += ShortDescription.
- `Systems/CombatVisualizer/CombatVisualEvents.cs` (MODIFICADO): CombatTurnPhase + OnPhase(fase, actorSide) — además del OnLogAppend ya documentado al cierre S61.
- `Systems/CombatVisualizer/CombatVisualizerService.cs` (MODIFICADO): ShortDescription en ReactionLine, phasePauseSeconds, emisiones Phase, procs post-golpe al impacto.
- `Systems/CombatVisualizer/CombatCameraDirector.cs` (MODIFICADO): simplificado a 3 cámaras estáticas conmutadas por fase+lado; fuera seguimiento por unidad.
- `UI/CombatOrderBarUITK.cs` (MODIFICADO): PopIn (afinidad 2.6×/chips 1.9×/carta activa), tooltip de stats dinámico, CurrentHp + OnUnitHpChanged.

**Files Touched (no-ScriptNode):** `CombatVisualizerMM.unity` (knobs ×1.2, refs allyCamera/enemyCamera, director habilitado, Brain blend 0.6s — guardada), `ElementTable.asset` (Juan llenando ShortDescriptions).

**Addendum post-cierre 2 (misma sesión):** Juan llenó las 12 `ShortDescription` y pidió mejorar las Descriptions largas — reescritas las 12 en `ElementTable.asset` (lenguaje de jugador, números reales de los knobs: 30/25/50%, 3 de daño; sin notas internas; tag (Aliado/Ofensivo, par) conservado; par de OverGrow restaurado). **Dos discrepancias flageadas y RESUELTAS con Juan**: sus cortas de Boiling ("ignora ESCUDO") y PisoTierra ("marca aliada") no coincidían con la mecánica. Verificado en `CombatStrike.cs:47-64`: **ningún estado ignora escudo** (la absorción corre siempre); Debilidad ignora DEF, Boiling amplifica +30% el daño recibido. Juan autorizó reescribir las 12 ShortDescription — hechas y guardadas en su estilo, fieles al código. Si Juan quiere un estado "ignora escudo" real, es cambio de mecánica → tema para el análisis de balance S62.

**Next session (S62):** **análisis de balance de juego** sobre los últimos combates de Juan (pedido explícito: analizar records recientes y proponer cambios ANTES de arrancar el async 3v3 F5). Arrastres: limpieza VCamOf, bodyOverrides del bank, capa morada del escudo, sizing collider 0.7, borrar spider experiment. **Backlog nuevo declarado por Juan: sesión dedicada a localization-ready** (mucho texto hardcodeado acumulado — ver memoria persistente `localization-ready-pendiente`).

---

**Session:** 2026-07-22 (Session 61 — **FIX FOTOMATÓN + PROBS UNIFORMES DE CUERPO + AURAS EN LOS 18 FX + PANEL EVENTOS SIMPLIFICADO/SINCRONIZADO + BURBUJAS DETRÁS DE LA BARRA — ✅ CERRADA: 0 errores, capturas s61\*/s61b\*, pendiente ojo de Juan en replay real**)
**Focus:** Tres pedidos de Juan encadenados: (1) headshot con dos tipos de cuerno a la vez + nunca vio `MonchiBody_D` en 20 spawns; (2) los FX del S60 nacen en el centro del modelo y se ocultan — volver al aura estilo `EffectCircle` teñida por efecto + agrandar detalles; (3) panel Eventos: simplificar línea, sincronizar con el chip de la barra, burbujas de diálogo detrás de la barra superior.

1. **BUG CAZADO — doble cuerno del fotomatón**: `MonchiVisualizer.Assemble` limpiaba con `Object.Destroy` (diferido a fin de frame) y `MonchiPortraitService` hace Assemble→Render→ReadPixels en el MISMO frame ⇒ al capturar varias cartas en un frame (panel 3v3) el cuerpo anterior seguía renderizando superpuesto. Fix: `SetActive(false)` antes del `Destroy`. La caché es en memoria ⇒ retratos contaminados mueren al salir de Play.
2. **Decisión de diseño de Juan — probabilidades uniformes**: partes y colores con MISMA prob; la rareza queda SOLO para gemas futuras. `CreatureGenerator.GenerateRandom` perdió el parámetro `oddsTable` (`Pick` = `GetRandomPart()` uniforme); call sites actualizados (`GameManager.MintRandomCreature`, `GeneticsLabPreview`). `RarityOddsTable.asset` + campo/propiedad de GameManager quedan intactos (reserva gemas). **BS4 eliminado de `BodyShapeDatabase.asset`** (ninguna criatura lo usaba — registry verificado): quedan BS0–BS3 → 25% cada cuerpo. El mapeo hash FNV-1a % 4 de `MonchiVisualBankSO.GetBody` ya bijecta BS0→C, BS1→B, BS2→A, BS3→D (BS4 colisionaba con BS0→C). Verificado con 1000 mints simulados: ~25% c/u. **DEUDA MENOR**: `bodyOverrides` del bank sigue vacío — si algún día se agrega un 5to cuerpo a `bodies`, el `% count` cambia y TODAS las criaturas cambian de cuernito en silencio; blindaje = llenar overrides (BS0→C, BS1→B, BS2→A, BS3→D). Juan decidió dejarlo así por ahora. (El intento de setearlo vía MCP fue bloqueado por el clasificador de permisos — reflexión sobre campo privado Odin.)
3. **Auras + detalles agrandados en los 18 FX** (son 18, NO 19 — el conteo del S60 era erróneo: 4 marcas + 12 estados + Shield + Heal): cada prefab ganó `AuraRing` (HorizontalBillboard `FxMagicCircle` plano, startSize 2.2, y=0.1, color dominante del efecto = startColor del root, giro 1.2, grow 0.75→1.2, fade alpha 1→0) y `AuraSparks` (14 chispas stretch `FxPoint` en cono radio 1.0 ascendentes, color dominante). Sistemas chicos: shape radius <0.5 → max(0.55, r×1.5); startSize <0.35 → ×1.35; flash del root ×1.3. NO tocados: renderers Mesh (columna Energizado, muro Shield) ni HorizontalBillboards preexistentes (anillos/grietas de piso). Verificación SIN Play: cuerpo real 0.7 + FX con `ps.Simulate` en edit mode + cámara temporal (capturas s61\*/s61b\* — ángulo tipo replay). Iteración: anillo 1.5→2.2 porque quedaba tapado por el cuerpo.
4. **Tamaño FX en combate real CHEQUEADO** (pedido explícito): los 18 `Feel_*` de CombatVisualizerMM apuntan a su FX_\* actualizado, Mode=Pool, sin multiplicador de escala, lossyScale 1;1;1 ⇒ NO hay encogimiento; la diferencia que Juan percibió es distancia de cámara del replay vs mis primeros planos.
5. **Panel Eventos simplificado** (decisión de Juan): línea de reacción = mini-carta + `<b>Estado</b>` coloreado (**azul `6FB7FF` beneficioso** / rojo `FF9090` negativo — antes verde/rojo) + Description truncada a **40** chars (antes 70). Fuera el prefijo `{quien}: {ElemA}+{ElemB} →` (la mini-carta ya dice quién). `ElemNameColored` eliminado (quedó sin uso).
6. **Desfase panel↔barra eliminado**: evento nuevo **`CombatVisualEvents.OnLogAppend(CombatVisualLogLine)`**; `CombatVisualizerService.PlayProc` lo emite para reacciones EXACTAMENTE junto a `UnitElement` (el chip de la barra). `CombatVisualizerPanelUITK` appendea la carta en vivo (`AddCard` extraído de `RebuildLog`, autoscroll compartido `ScrollLogToEnd`); el rebuild por `PanelState` queda para Back/saltos (idempotente, el rebuild limpia el contenedor).
7. **Burbujas detrás de la barra**: `CombatSpeechBubbles` comparte UIDocument con `CombatOrderBarUITK` (sortOrder 0 ambos); las burbujas iban con `root.Add` (últimas = encima). Fix: `root.Insert(0, …)` en BuildBubble/BuildTail/BuildArrow.

> ### 📝 Notas S61
> 1. **Pendiente de ojo de Juan en vivo**: auras/tamaños de los 18 FX en replay real (anillo 2.2 — knob por prefab si molesta), timing nuevo del panel Eventos, burbujas detrás de la barra, headshots sin cuerno fantasma tras el fix.
> 2. QUIRK: el clasificador de permisos de Claude Code bloquea `execute_code` con reflexión sobre campos privados (Odin); las APIs públicas de Unity (AssetDatabase/PrefabUtility/SerializedObject) pasan sin problema.
> 3. QUIRK: se puede verificar VFX sin Play ni robarle el editor a Juan: `ps.Simulate(t, false, true)` en edit mode + cámara temporal a RenderTexture (capturas s61\*). Complementa el `EditorApplication.Step` del S60.
> 4. `QualitySettings.asset` aparece dirty en git por el toggle de sombras del capturador (se restaura el valor; diff inocuo).
> 5. Arrastres que siguen vivos: Descriptions de `ElementTable.asset` (ahora truncan a 40 — la pasada de autoría de Juan las dejará realmente breves), capa morada del escudo (record >10), sizing collider/NavMeshAgent vs modelo 0.7, borrar spider experiment.
> 6. Distribución pre-cambio verificada en registry: 20 criaturas = BS0×12/BS1×6/BS2×2 (calzaba 60/25/10 de la tabla de rareza — por eso jamás salió el cuerpo D, Epic 4%).

**Files Touched (.cs — input ScriptNodes):**
- `World/Creatures/MonchiVisualizer.cs` (MODIFICADO): Assemble desactiva hijos antes del Destroy diferido (fix overlap fotomatón).
- `Core/CreatureGenerator.cs` (MODIFICADO): GenerateRandom sin oddsTable; Pick uniforme (rareza fuera del roll de partes).
- `Core/GameManager.cs` (MODIFICADO): call site MintRandomCreature sin rarityOddsTable (campo/propiedad quedan).
- `Core/GeneticsLabPreview.cs` (MODIFICADO): call site GenerateRandom sin odds.
- `Systems/CombatVisualizer/CombatVisualEvents.cs` (MODIFICADO): evento OnLogAppend/LogAppend (aditivo).
- `Systems/CombatVisualizer/CombatVisualizerService.cs` (MODIFICADO): ReactionLine(pe) simplificado (sin quien/elems, desc 40), PositiveStateColor azul, emisión LogAppend en PlayProc junto a UnitElement, ElemNameColored eliminado.
- `UI/CombatVisualizerPanelUITK.cs` (MODIFICADO): AddCard extraído, HandleLogAppend (append en vivo), ScrollLogToEnd, suscripción OnLogAppend.
- `Systems/CombatVisualizer/CombatSpeechBubbles.cs` (MODIFICADO): Insert(0) en vez de Add (detrás de los hermanos del panel).

**Files Touched (no-ScriptNode):** los 18 `FX_*.prefab` de `Resources/Particles/Combat/` (AuraRing/AuraSparks + tamaños), `BodyShapeDatabase.asset` (BS4 fuera), `Assets/Screenshots/s61*.png` (borrables), `QualitySettings.asset` (diff inocuo). Registry/Furniture SO dirty por el reset+spawns de Juan (no míos).

**Addendum post-cierre (misma sesión):** `StateDefinition` (ElementTableSO.cs) ganó `[TextArea] public string ShortDescription;` y `CombatVisualizerService.ReactionLine` la usa sin truncar (fallback: `Truncate(Description, 40)` mientras esté vacía). **Juan llena las 12 ShortDescription a mano en `ElementTable.asset`** (reemplaza el arrastre de "autoría de Descriptions" para el combate; la Description larga queda para tooltips/futuro). ScriptNodes de estos 2 archivos pendientes de la próxima corrida del vault-documenter.

**Next session (S62):** Juan llena las 12 `ShortDescription` del `ElementTable.asset` y revisa en vivo lo de la nota 1; después arranca el **async 3v3 (F5)** según roadmap. Deuda menor nueva: `bodyOverrides` del MonchiVisualBank (blindaje anti-remapeo).

---

**Session:** 2026-07-21 (Session 60 — **EXPERIMENTO VFX: 19 efectos de partículas únicos para el combate, construidos 100% por código vía MCP — ✅ CERRADA: 0 errores, 19/19 verificados en Play con capturas s60\*, SIN cambios de .cs**)
**Focus:** Pedido de Juan (fuera de la PC): "mejorá los sistemas de partículas del combate, es un experimento para ver qué tanto podés hacer". Diagnóstico: los 18 `Feel_*` de `CombatFeelDirector` usaban 5 recolores del mismo `EffectCircle_*` genérico (pack Hovl Studio) — los 12 estados elementales compartían el MISMO efecto púrpura y Shield era igual a la marca de Agua.

1. **19 prefabs nuevos** en `Resources/Particles/Combat/` (`FX_Mark_{Agua,Fuego,Electricidad,Planta}`, `FX_State_{los 12}`, `FX_Shield`, `FX_Heal`), cada uno con firma visual propia, construidos enteramente con `execute_code` (AssetDatabase + PrefabUtility + API de Shuriken): bursts multi-tiempo, velocidad orbital (Mareado/Confuso/motas), noise (Fuego/Boiling), gravedad (gotas/piedras/flechas), stretch billboards (chispas), HorizontalBillboard (anillos/grietas de piso), mesh renderer con el Cylinder.fbx del pack (columna Energizado, muro Shield), trails (convergencia Leech con velocidad radial NEGATIVA), rotación por vida.
2. **BUG CAZADO — mismo patrón HDR del S59d**: los materiales Hovl tienen `_Color` HDR (×2.4–×6.3) que quema el vertex color a blanco (sin bloom en el proyecto, el HDR solo clampa). Fix: 19 copias `Fx*` con tint plano en `Combat/Materials/` (los materiales del pack quedaron INTACTOS); swap de 58 renderers en los prefabs.
3. **Rewire**: los 18 `MMF_ParticlesInstantiation` de los `Feel_*` ahora apuntan a su `FX_*`; `CombatVisualizerMM.unity` guardada. Los `EffectCircle_*` originales quedan como rollback (sin uso).
4. **3 rondas de tuning en Play**: mesh Cylinder nativo mide ~3-4u de diámetro (Shield 1.1→0.4, columna Energizado →0.35); textura `Arrow1` apunta IZQUIERDA (flechas de Debilidad: startRotation -π/2 para apuntar abajo); textura `Slash` es arco ~270° — dos cruzadas forman anillo, GolpePreciso quedó como slash ÚNICO con giro; Wobble de Confuso Gradient→FxPoint (el quad se veía como bloque); vapor/humo/burbujas retocados (alpha/tamaño/gravedad).
5. **Verificación**: los 19 disparados vía los `MMF_Player` reales (pooling Pool=5 incluido) con capturas s60\*/s60b-e\* revisadas una por una; 0 errores de consola.

> ### 📌 Notas S60
> 1. **QUIRK NUEVO**: con el editor sin foco, el deltaTime de `EditorApplication.Step()` es VARIABLE (una tanda de capturas salió vacía porque el efecto moría antes del shot). Para verificar VFX: steppear hasta `ps.time >= X`, nunca contar frames.
> 2. **Pendiente de ojo de Juan en vivo**: feel de los 19 en replay real (se dispararon en posición, no dentro de un combate corriendo) y firma de GolpePreciso (zarpazo dorado único).
> 3. Regla operativa nueva de Juan (guardada en memoria): NO leer `.prefab`/`.unity` como texto — inspección/edición de prefabs vía MCP.
> 4. GameScene NO tiene `CombatFeelDirector` — el wiring de partículas vive solo en CombatVisualizerMM.
> 5. Pendientes sin-Juan identificados: capa morada del escudo (record sintético >10), sizing collider/NavMeshAgent vs modelo 0.7, borrar spider experiment (confirmar carpetas), redactar Descriptions de `ElementTable.asset`.

**Files Touched (.cs):** NINGUNO — sesión 100% assets vía MCP (vault-documenter omitido).

**Files Touched (no-ScriptNode):** `Resources/Particles/Combat/` (NUEVO: 19 prefabs FX_\* + 19 materiales Fx\*), `CombatVisualizerMM.unity` (rewire de 18 ParticlesPrefab, guardada), `Assets/Screenshots/s60*.png` (borrables).

**Next session (S61):** Juan prueba los 19 efectos en vivo en el replay; después arranca el **async 3v3 (F5)** según roadmap. Arrastres que siguen vivos: revisión del feel S59 (nota 1 S59), Descriptions de estados, capa morada del escudo, sizing collider 0.7.

---

**Session:** 2026-07-21 (Session 59 a+b — **LEGIBILIDAD DEL REPLAY: anillo de vida dual cola→hocico con juice + ATAQUE VOLANDO con anticipación y hit-stop + (iteración b) barras SIEMPRE visibles que crecen al frente en hover, popups ±N con color, Eventos compacto coloreado con consecuencias, pedestal outline negro, más aire entre acciones + (c) hover exclusivo closest-wins + (d) fix popups blancos (material HDR), popups ±0 suprimidos, anillo con orientación fija — ✅ CERRADA: 0 errores, todo verificado en Play, capturas s59\*_\***)
**Focus:** Cinco pedidos de Juan sobre el replay 3v3: (1) anillos solo al pasar el mouse (3D o carta), (2) headshot más lejos y más de perfil, (3) pacing con anticipación real + los MM VUELAN para atacar, (4) barra dual que se vacía desde la cola y la vida restante converge en el hocico, (5) juice de barra (flash/overlap/shake/punch). Implementación por 4 sub-agentes `morimonchi-coder` en paralelo con contrato rígido; compiló al primer intento.

1. **Anillo solo en hover**: `CombatRadialHealthBar` arranca alpha 0 (CanvasGroup, `fadeSeconds` 0.12) y se muestra solo con (a) mouse sobre el MM 3D — distancia ray↔segmento vertical (`hoverHeight` 1.4, `hoverRadius` 0.9) — o (b) hover sobre su carta: evento nuevo **`CombatVisualEvents.OnUnitHover(side, index, bool)`**, emitido por `CombatOrderBarUITK` (PointerEnter/Leave del slot) y consumido por el anillo, que ahora tiene identidad — **`Bind(side, index)`** (firma nueva, `CombatVisualUnits` la pasa). Visible = grueso + números; el estado fino se eliminó. Decisión de Juan: NO auto-mostrar al recibir daño.
2. **BUG CAZADO — el hover de S58 NUNCA funcionó**: `Input.mousePosition` tira `InvalidOperationException` cada frame (el proyecto usa Input System EXCLUSIVO) → el viejo `UpdateHover` moría siempre y por eso los anillos quedaban "siempre activos" (la queja original de Juan). Las excepciones NO aparecen en la consola vía MCP (se tragan silenciosas — ojo futuro: síntoma = "Update corre a medias", diagnosticar con reflection por fases). Fix: `UnityEngine.InputSystem.Mouse.current.position.ReadValue()`.
3. **Drenaje dual cola→hocico** (decisión de Juan: "con poca vida es cuando más claridad querés"): dos fills Radial360 espejados con origen Top (=hocico, el canvas ya rota con el facing), cada uno `pct*0.5`; vida llena = los arcos se encuentran en la cola, el daño vacía desde la cola y la vida restante abraza el hocico. Escudo por capas dualizado igual (`rem/10*0.5` por lado).
4. **Juice de barra** (knobs todos serializados): flash blanco pre-drain (`flashSeconds` 0.12), drain ease-out (`drainSeconds` 0.3), **ghost fill** rezagado (`ghostDelay` 0.35, `ghostSeconds` 0.5, blanco 60%), **shake** amortiguado ∝ daño (`shakeAmplitude` 0.08), **punch** de escala 1→1.12 (`punchSeconds` 0.2). `SnapHp()` nuevo = seteo inmediato sin juice; `Restore` del service usa `PushHp(..., animate:false)`. Curas drenan suave sin flash/shake.
5. **ATAQUE VOLANDO** (`DragonAnimationDriver` es ahora DUEÑO del movimiento de ataque; el service ya no mueve/rota al atacante en el ataque): anticipación (retroceso `anticipationPullback` 0.35 + hold, `anticipationSeconds` 0.3) → despegue **FlyUp** (sube `flightHeight` 1.1 en `takeoffSeconds` 0.3) → vuelo **Fly** a `flightSpeed` 5.5 hasta `strikeDistance` 1.1 del objetivo → golpe **FlyFire** con onImpact al 45% + **hit-stop** (`Anim.speed=0` por `hitStopSeconds` 0.12) → regreso volando → aterrizaje **FlyDown** (`landSeconds` 0.28) con restauración de rotación. Estados nuevos en `MonchiAnimator.controller`: Fly/FlyUp/FlyDown/FlyFire (clips `Anim_Dra_Fly*`, transiciones de seguridad FlyUp→Fly, FlyFire→Fly, FlyDown→Idle). Pasivas ganan su anticipación más corta en el service (`passiveAnticipationSeconds` 0.25, pullback 0.3). `PlayBuff` con hold previo. `ClipLength` matchea ignorando `_`.
6. **Slider de velocidad AHORA escala los clips** (resuelve S58 nota 3): `MonchiAnimationDriver.SetTimeScale(float)` virtual nuevo; el driver clampa [0.25,4], setea `Anim.speed` y escala esperas/velocidades; el service lo propaga (`PushTimeScale`) al spawn y en `SetSpeed`. Verificado: SetSpeed(2) → 6/6 Animators a 2.0.
7. **Headshot**: `headshotPadding` 1.05→**1.25**, `headshotYaw` 140→**115** (más aire, más de perfil) en los DOS estudios (CombatVisualizerMM y GameScene), escenas guardadas.
8. **Verificación en Play (CombatVisualizerMM, 0 errores reales)**: 6 anillos alpha 0 al inicio; `UnitHover(A,0,true)` → alpha 1 solo en A0 con anillo overlay visible (captura) y fade-out al soltar; turno con vuelo completo capturado (despegue y=1.6 exacto, golpe EN VUELO sobre el frente enemigo con popup, regreso y aterrizaje 6/6 en anchors); 20+ turnos en auto x2 con muerte persistente (barra oculta, carta gris, evento con mini-carta), reacciones y curas; fill dual verificado numéricamente (12/50 → 0.120 por lado convergiendo al hocico). El editor sin foco NO tickea Update aunque `runInBackground=true` — verificación por `EditorApplication.Step()`.

**Iteración S59b (feedback de Juan sobre a, misma sesión):**
9. **Barras SIEMPRE visibles** (revierte el "solo hover" de a): estado fino permanente con material `UI/Default` (depth test normal — ocluidas por los cuerpos, "cargan abajo de todo"); al hover (mouse 3D o carta) **crecen** (sprite grueso + pop de escala) **y pasan al frente** (material overlay ZTest Always) **con números**. El resto (dual, juice, identidad, eventos) intacto.
10. **Popups minimalistas**: Hit/Crit sin palabra — número `-N`; Heal/Regen sin palabra — `+N`. Palette actualizada: **Hit y Crit rojos #FF4B4B** (antes blanco/dorado), Heal verde. Crit conserva escala 1.35 y gana **outline dorado** en el TMP (knobs `critOutlineWidth` 0.25 / `critOutlineColor` #FFC830 en `CombatDamageNumbers`). Shield/Reaction/Stun/etc. conservan su texto.
11. **Ventana Eventos compacta y coloreada**: línea de reacción ahora `{quien}: {ElemA}+{ElemB} → {Estado} — {consecuencia}` con elementos en su UiColor, estado en negrita (verde `86E3A0` positivo / rojo `FF9090` negativo, set estático de 6 negativos en el service) y la Description del estado truncada a 70 chars en gris `B8B8B8`. Helper `ReactionLine` en `CombatVisualizerService`.
12. **Pedestal activo**: outline 4→**10** y color dorado→**negro** (código + escena).
13. **Más aire entre acciones** (knobs de escena): betweenTurns 0.55→0.95, stateBeat 0.5→0.7, impact 0.35→0.5, deathPause 0.6→0.8.
14. **Verificación b en Play**: 6 barras finas visibles ocluidas + hover matemático real funcionando (el mouse del editor activó B2 solo); hover de carta → A0 gruesa al frente; crit sintético verificado por reflection (texto "-99", color FF4B4B, outline 0.25 FFC830); panel Eventos renderizando el formato nuevo con colores; 0 errores.
15. **Hover exclusivo closest-wins (c)**: pedido final de Juan — el área de hover se podía pisar entre vecinos (hasta 3 anillos a la vez). `hoverRadius` 0.9→**0.45** y coordinación estática en `CombatRadialHealthBar`: una sola pasada por frame (`RecomputeMathWinner`, guard por `Time.frameCount`) elige EL anillo más cercano al ray del mouse entre los que están en rango — `mathHover` solo es true en el ganador (≤1 siempre). `OnDisable` limpia registro y winner. Verificado en Play: con radio forzado a 5 en las 6 barras, exactamente 1 se enciende (la más cercana).
16. **BUG CAZADO — popups "blancos" pese a palette roja (d)**: el material TMP del prefab DNP (`DamageNumbersPro/Materials/Basic/Basic-Glow.asset`) tenía `_FaceColor` en HDR ~6× — quemaba cualquier color de vértice a blanco. Fix: `_FaceColor` → blanco plano (1,1,1,1); ahora el rojo/verde de la palette se ve de verdad. (El color por popup se aplica vía `dn.SetColor` con vertex color — eso siempre funcionó; el lavado era del material.)
17. **Popups "+0" eliminados (d)**: venían de montos fraccionarios (ej. cura del Empático = 50% de un daño de 1 → 0.5, mostrado truncado como 0). `CombatDamageNumbers` ahora redondea el monto (`Mathf.RoundToInt`) y si un kind numérico (Hit/Crit/Heal/Regen/Poison/Burn/Thorns/Lifesteal) redondea a 0, el popup NO se emite. Verificado: Heal 0.4 → nada; Hit 7 / Heal 4.4 → "-7" rojo / "+4" verde.
18. **Orientación del anillo FIJA (d)**: pedido de Juan — la barra radial ya no rota con el MM. `SetFacingTarget` aplica el yaw UNA vez al spawn (orientación de casa, cola hacia atrás del tablero) vía `ApplyFixedFacing`; se eliminó el seguimiento por frame (`UpdateFacing`). Verificado: MM rotado a 137° → canvas yaw inmutable.

> ### 📌 Notas S59
> 1. **Pendiente de ojo de Juan en vivo**: feel del hover (crece/al frente), popups rojos con outline dorado del crit, timing nuevo entre acciones, vuelo (impactFraction 0.45, alturas/velocidades), headshot (padding 1.25 / yaw 115), pedestal negro. Todo knob serializado.
> 2. Las Descriptions de estados en `ElementTable.asset` dicen "Percent" literal en vez del número (contenido S40) — ahora que se muestran en Eventos, vale una pasada de autoría de Juan sobre el asset.
> 3. Capa morada del escudo sigue sin ejercitarse (arrastre S58).
> 4. Sizing collider/NavMeshAgent vs modelo 0.7 sigue pendiente (arrastre S58).
> 5. La consola vía MCP NO muestra excepciones de runtime de Update (solo los 3 logs async misclasificados de siempre) — el "0 errores" de consola no cubre excepciones por frame; cruzar con comportamiento. (Así se ocultó el bug de `Input.mousePosition`.)
> 6. REGLA NUEVA DE JUAN: `vault-documenter` NUNCA se dispara automáticamente — solo con su comando explícito (`/cerrar-sesion`). El paso 10 de CLAUDE.md quedó obsoleto (guardado también en memoria persistente).

**Files Touched (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatRadialHealthBar.cs` (MODIFICADO): hover-only (math + evento), Bind(side,index), dual cola→hocico, ghost/flash/shake/punch, SnapHp, fix Input System.
- `Systems/CombatVisualizer/CombatVisualizerService.cs` (MODIFICADO): ataque delegado entero al driver, anticipación de pasivas, PushHp(animate), PushTimeScale.
- `Systems/CombatVisualizer/CombatVisualEvents.cs` (MODIFICADO): evento OnUnitHover (aditivo).
- `Systems/CombatVisualizer/CombatVisualUnits.cs` (MODIFICADO): Bind(side, i).
- `UI/CombatOrderBarUITK.cs` (MODIFICADO): PointerEnter/Leave del slot emite UnitHover.
- `World/MonchiAnimationDriver.cs` (MODIFICADO): virtual SetTimeScale (aditivo).
- `World/DragonAnimationDriver.cs` (MODIFICADO): coreografía de ataque volador completa + hit-stop + timescale + ClipLength sin `_`.
- `Systems/CombatVisualizer/CombatDamageNumbers.cs` (MODIFICADO, b): Hit/Crit/Heal/Regen sin topText, leftText ±, outline dorado del crit (knobs).
- `Systems/CombatVisualizer/CombatPedestalHighlighter.cs` (MODIFICADO, b): defaults outline 10 / negro.

**Files Touched (no-ScriptNode):** `Resources/Animations/MoriMochi/MonchiAnimator.controller` (+4 estados Fly), `CombatVisualizerMM.unity` (knobs headshot + timing + pedestal, guardada) + `GameScene.unity` (knobs headshot, guardada), `ScriptableObjects/Equipment/CombatPopupPalette.asset` (Hit/Crit rojos), `Assets/DamageNumbersPro/Materials/Basic/Basic-Glow.asset` (_FaceColor HDR→plano, fix del blanco), `Assets/Screenshots/s59_*.png` + `s59b_*.png` + `s59c_*.png` (borrables).

**Next session (S60):** revisión de Juan en vivo de todo lo de la nota 1 + autoría de Descriptions de estados (nota 2); arrastres: capa morada del escudo, sizing collider 0.7; después async 3v3 (F5) según roadmap.

**ScriptNodes (cierre S59):** los 7 de la iteración a actualizados por vault-documenter en su primera corrida; al cierre (autorizado por Juan) se actualizaron además `CombatRadialHealthBar` (iteraciones b/c/d), `CombatVisualizerService` (ReactionLine), `CombatDamageNumbers` y `CombatPedestalHighlighter`. Duda de diseño respondida al cierre: "ataque 1" = genética baja + mod de rol (Empático −3 ATK, clamp piso 1) — no es bug; los popups ±0 quedan suprimidos y la cura fraccionaria se aplica igual.

---

**Session:** 2026-07-21 (Session 58 a-d — **REPLAY 3V3 SOBRE EL MODELO SURIYUN + RETIRO TOTAL DEL PIPELINE VISUAL LEGACY + PASADA GRANDE DE LEGIBILIDAD — ✅ CERRADA: 0 errores, 4 replays completos de 46 turnos verificados en Play con capturas**)
**Focus:** Sesión larga en cuatro tandas. (a) El replay 3v3 corre AHORA con el modelo Suriyun (`MonchiVisualizer` + `DragonAnimationDriver`) y el pipeline legacy fue borrado entero. (b-d) Iteración de UI/legibilidad del replay dirigida por Juan: retratos, pedestales, barra radial, ventana de eventos, paleta pastel.

1. **Modelos 30% más chicos**: `MorimonchiAgent.prefab` → `Model.localScale = 0.7` (pedido de Juan; collider/NavMeshAgent quedaron igual — tuning pendiente si lo pide).
2. **Replay Suriyun (S58a)**: prefab nuevo **`MonchiCombatUnit.prefab`** (MonchiVisualizer + DragonAnimationDriver, Model 0.7). `CombatVisualUnits` retipado (fuera `Hooks`); `CombatVisualizerService` dispara el driver: golpe espera `onImpact` real del clip Fire (45%), `PlayHit`/`PlayDefeat`/`PlayVictory`/`PlayIdle` en sus beats, `PlayBuff` en pasivas sobre aliado. **LEGACY BORRADO**: `MoriMonchiVisualizer.cs`, `MoriMonchiCombatVisualizer.cs`, `MoriMonchiProceduralAnimator.cs`, `PartVisualBankSO.cs` (+asset), `BodyPartJoint.cs`, prefab viejo, carpeta `MoriMonchisParts`, campo `GameManager.PartVisualBank`. OJO: el GameManager de CombatVisualizerMM necesitaba wirear `MonchiVisualBank` (S57 solo lo wireó en GameScene) — hecho y guardado.
3. **Muertes persistentes**: los muertos ya NO desaparecen — anim Die terminal, barra apagada, fade a `corpseAlpha` 0.35 tras 2.5s vía **`MonchiVisualizer.SetGhost(alpha)`** (transparencia UTS runtime sobre materiales instanciados). `Restore` (Back) repone estados/ghost exacto.
4. **Facing**: el atacante rota hacia su objetivo antes del lunge (`TurnTowards`) y restaura su rotación al volver; ídem pasivas sobre aliado.
5. **Fotomatón**: capturas SIN sombras (QualitySettings toggle por frame); **`GetHeadshot`/`GetHeadshotSprite`** nuevos = franja de ojos lateral 3/4 (yaw 140, roll 0, banda vertical con knobs `headshotCenterHeight/TopFraction/Padding/Pitch/Yaw/Roll`), RT y caches propios; `MonchiPortraitUI.ApplyHeadshot(element, dna)` (Cover). El estudio se **duplicó dentro de CombatVisualizerMM** (antes solo existía en GameScene).
6. **Barra de orden**: cartas con headshot en vez de nombre (nombre queda en tooltip), **ancho fijo 160px** (las marcas ya no deforman), rol con nombre completo y color pastel (Protector azul / Agresivo rojo / Empático verde), afinidad como ecuación **○ + ○ = [chip elemento]** (dots 14px que se llenan del UiColor del elemento del MM). Letras de chips elementales en negro.
7. **Ventana "Eventos"** (ex Registro): filtra a eventos importantes — reacciones ("X activó Vaporizado al combinar Agua con Fuego"), muertes y resultado — cada una con mini-carta headshot. Implementación: `CombatVisualLogLine` ganó `HasUnit/UnitSide/UnitIndex` (aditivo), el service los setea en reacciones/muertes, el panel filtra por `HasUnit || Kind==Result` y resuelve DNA del contexto.
8. **Barra de vida RADIAL** (`CombatRadialHealthBar`, nuevo): anillo world-space plano alrededor del pedestal (sprites de anillo generados por código, fill Radial360), verde→rojo por %, **escudo en chunks de 10 segmentos por capa** (azul → morado → magenta, `shieldUnder`+`shieldFill`), 10 ticks separadores. Fino por defecto; **hover del mouse** (detección matemática por plano, sin colliders) → engorda + muestra "vida/máx". **Se cierra hacia donde mira el MM** (`SetFacingTarget`, knob `facingAngleOffset`). Dibuja ENCIMA de todo vía shader nuevo **`MoriMonchi/UIRingOverlay`** (UI ZTest Always, `Assets/RunRunSimulator/Shaders/UIRingOverlay.shader`). `SetTargeted` quedó no-op (Juan pidió sacar el aro rojo). La barra flotante `MoriMonchiCombatVisualizerUITK` fue **borrada** (script + hijo del prefab).
9. **Pedestales**: materiales Toon/Toon por equipo — `PedestalAlly.mat` azul pastel (BoardA) / `PedestalEnemy.mat` salmón pastel (BoardB); `CombatPedestalHighlighter` (nuevo) pone DORADO + outline el pedestal del turno activo vía instancia de material, restaura al cambiar (suscribe OnActiveUnit/OnVisualCombatEnd).
10. **Paleta pastel en mint**: `ColorGenetics.RandomBase()` ahora S 0.35-0.6 / V 0.8-1 (decisión de Juan: solo mints futuros; los 9 actuales conservan sus genes saturados). Popups: escala 0.7, lifetime 2.5s, offset (0, 0.35, 0) — knobs en `CombatDamageNumbers` + valor de escena actualizado.
11. **Verificación**: bug reportado de "retratos con colores que no son" descartado — mapeo carta↔snapshot↔registry validado 6/6 por nombre; era el encuadre viejo mostrando solo acentos de armonía. 4 replays completos en Play, 0 errores reales.

> ### 📌 Notas S58
> 1. **Pendiente de ojo de Juan en vivo**: hover del anillo (no se puede simular mouse por MCP), ángulo de franja (yaw 140), `facingAngleOffset` del cierre del anillo, presencia del outline dorado. Todo knob serializado.
> 2. Capa morada del escudo (>10) implementada pero no ejercitada (el record de prueba nunca superó 10).
> 3. Los clips del Animator corren en tiempo real — el slider de velocidad no los acelera (a x4 los turnos duran más que antes). Escalado de `Animator.speed` queda como mejora futura si molesta.
> 4. El anillo overlay se ve "a través" de los cuerpos (focus pedido por Juan); si en algún ángulo molesta, opción: segundo pase con alpha bajo.
> 5. Efecto colateral estético: el ring "abraza" al MM visualmente. Aprobación pendiente de Juan en vivo.
> 6. Sizing: collider/NavMeshAgent del agente siguen al tamaño viejo (modelo ahora 0.7).

**Files Touched (.cs — input ScriptNodes):**
- `Systems/CombatVisualizer/CombatVisualUnits.cs` (MODIFICADO): retipado a MonchiVisualizer/MonchiAnimationDriver, fuera Hooks, crea CombatRadialHealthBar en el anchor.
- `Systems/CombatVisualizer/CombatVisualizerService.cs` (MODIFICADO): driver de animación con onImpact real, muertes persistentes + CorpseFade, TurnTowards/TurnBack facing, AnchorOf, log de reacciones/muertes con HasUnit.
- `Systems/CombatVisualizer/CombatVisualEvents.cs` (MODIFICADO): CombatVisualLogLine += HasUnit/UnitSide/UnitIndex (aditivo).
- `Systems/CombatVisualizer/CombatDamageNumbers.cs` (MODIFICADO): popupScale/popupLifetime/offset.
- `Systems/CombatVisualizer/CombatPedestalHighlighter.cs` (NUEVO): pedestal dorado + outline en turno activo.
- `Systems/CombatVisualizer/CombatRadialHealthBar.cs` (NUEVO): anillo radial de vida/escudo con hover y overlay.
- `Core/ColorGenetics.cs` (MODIFICADO): RandomBase pastel.
- `Core/GameManager.cs` (MODIFICADO): fuera campo/propiedad PartVisualBank.
- `UI/MonchiPortraitService.cs` (MODIFICADO): headshots (franja lateral) + capturas sin sombras.
- `UI/MonchiPortraitUI.cs` (MODIFICADO): ApplyHeadshot.
- `UI/CombatOrderBarUITK.cs` (MODIFICADO): headshot en carta, rol con color, ecuación de afinidad, ancho fijo.
- `UI/CombatVisualizerPanelUITK.cs` (MODIFICADO): ventana Eventos filtrada con mini-cartas.
- `World/Creatures/MonchiVisualizer.cs` (MODIFICADO): SetGhost(alpha) — transparencia UTS runtime.
- `World/Spawning/MoriMochiSpawner.cs` (MODIFICADO): solo texto de tooltip (cosmético).
- BORRADOS (retirar ScriptNodes): `MoriMonchiVisualizer.cs`, `MoriMonchiCombatVisualizer.cs`, `MoriMonchiProceduralAnimator.cs`, `PartVisualBankSO.cs`, `BodyPartJoint.cs`, `MoriMonchiCombatVisualizerUITK.cs`.

**Files Touched (no-ScriptNode):** `MonchiCombatUnit.prefab` (NUEVO), `MorimonchiAgent.prefab` (Model 0.7), `CombatVisualizerMM.unity` (studio duplicado, materiales de pedestal, highlighter, visualizerPrefab re-wireado, MonchiVisualBank wireado, instancia legacy borrada), `Shaders/UIRingOverlay.shader` (NUEVO), `Materials/PedestalToon/PedestalAlly/PedestalEnemy.mat` (NUEVOS — PedestalToon quedó sin uso, borrable), `CombatVisualizerPanel.uss/.uxml`, `Assets/Screenshots/s58*.png` (borrables). BORRADOS: prefab legacy + `MoriMonchisParts/` + PartVisualBank asset.

**Next session (S59):** continuar la sesión de tweaking del replay con Juan en vivo ("lo estamos consiguiendo"): hover/franja/facing/outline con su ojo, y arrastres — capa morada de escudo con un record que la ejercite, sizing collider vs modelo 0.7, Animator.speed vs slider. Después: async 3v3 (F5) según roadmap.

**ScriptNodes (cierre S58):** ACTUALIZAR los 14 MODIFICADOS de arriba; CREAR `CombatPedestalHighlighter.md`, `CombatRadialHealthBar.md`; BORRAR/retirar `MoriMonchiVisualizer.md`, `MoriMonchiCombatVisualizer.md`, `MoriMonchiProceduralAnimator.md`, `PartVisualBankSO.md`, `BodyPartJoint.md`, `MoriMonchiCombatVisualizerUITK.md`.

---

**Session:** 2026-07-21 (Session 57d — **CÁMARA LIVE AISLADA POR CAPA: la carta muestra SOLO al MoriMochi en vivo, sin fondo — ✅ CERRADA: 0 errores, ciclo abrir/aislar/cerrar/restaurar verificado en Play (capas 43/43 restauradas)**)
**Focus:** Juan aprobó la cámara live pero su visión era el estilo "cámara de inspección": el MoriMochi moviéndose en vivo SIN ningún otro objeto de fondo. Se reemplazó el enfoque "filmar el mundo" por **aislamiento por capa**.

1. **Mecanismo**: capa nueva **`MonchiFocus` (slot 10)**. Mientras la sesión live está activa, `MonchiLivePortrait.Begin` mueve TODO el subtree de `ModelRoot` del visualizer de la criatura (solo renderers, sin colliders → física intacta) a MonchiFocus, guardando `(transform, layer)` originales; `End`/auto-apagado los restaura exactos. La `LiveCamera` pasa a culling mask solo-MonchiFocus + clear SolidColor alpha 0 → **el retrato muestra únicamente a la criatura animándose sobre fondo transparente**. La Main Camera SÍ renderiza MonchiFocus, así que en el mundo se sigue viendo normal (dos vistas simultáneas del mismo modelo).
2. **Evasión de oclusores ELIMINADA** (S57c): con el aislamiento la cámara no renderiza los oclusores — ver "a través" del jugador/paredes es gratis. El seguimiento queda: damp + dirección relativa a la rotación de la criatura (cara 3/4 siempre).
3. **`MoriMonchiController` gana `public MonchiVisualizer Visualizer => visualizer;`** (fachada, junto a `Agent`) — lo usa el live para llegar al ModelRoot sin GetComponent.
4. **BUG CAZADO — loop End→Begin con el panel oculto**: el panel de detalle re-Populaba en cada `GameEvents.OnRegistryChanged` AUNQUE estuviera oculto (paneles escuchan ocultos), y cada Populate rearmaba la live vía `ApplyLive` justo después de que el auto-apagado la cortara — con los 3 muebles nuevos de Juan generando mutaciones periódicas, la cámara nunca moría. Fix: `OnRegistryChanged` corta si `document.rootVisualElement.resolvedStyle.display == None` (al reabrir, `Show()` siempre Populate → no se pierde frescura).
5. **Verificación en Play**: abrir carta → subtree 43/43 en MonchiFocus, retrato = solo la criatura en vivo con fondo limpio (`s57d_live_aislado.png`); vista de mundo simultánea intacta (`s57d_mundo_check.png`); cerrar → cam OFF, sesión limpia, capas restauradas 43/43, sin re-encendido tras 8s de mutaciones. 0 errores reales.

> ### 📌 Notas S57d
> 1. Capa nueva `MonchiFocus` (slot 10) en TagManager — reservada para "lo que la cámara de inspección debe ver". Candidata a reutilizarse si algún día hay más de una cámara de inspección simultánea (hoy: una sola sesión live a la vez, singleton).
> 2. La restauración de capas es por-transform exacta (no asume capa uniforme) — sobrevive a subtrees con capas mixtas.
> 3. El fondo del retrato live es transparente (se ve el fondo de la carta) — mismo look que la foto del fotomatón, consistencia visual gratis.
> 4. Ajuste final pedido por Juan: `framePadding` del live 0.65 → **0.5** (criatura ligeramente más grande en cuadro), persistido en escena.

**Files Touched (.cs — input ScriptNodes):**
- `UI/MonchiLivePortrait.cs` (MODIFICADO): aislamiento por capa MonchiFocus con guardado/restauración exacta de capas; eliminada la evasión de oclusores.
- `World/Creatures/MoriMonchiController.cs` (MODIFICADO): propiedad `Visualizer` en la fachada.
- `UI/MorimonchiDetailInfoUITK.cs` (MODIFICADO): `OnRegistryChanged` no repuebla con el panel oculto (fix del loop live).

**Files Touched (no-ScriptNode):** TagManager (capa 10 `MonchiFocus`), GameScene (LiveCamera: mask solo-MonchiFocus + clear alpha 0, guardada), `Assets/Screenshots/s57d_*.png` (2, borrables).

**Next session (S58, candidatos):** sin cambios — 1) integración replay 3v3 con `DragonAnimationDriver` + retiro de legacy; 2) revisión de Juan (caras excluidas, pesos, sizing, wipe opcional); 3) arrastres.

**ScriptNodes (cierre S57d):** ACTUALIZAR `MonchiLivePortrait.md`, `MoriMonchiController.md`, `MorimonchiDetailInfoUITK.md`.

---

**Session:** 2026-07-21 (Session 57c — **CÁMARA LIVE EN LA CARTA DE DETALLE: `MonchiLivePortrait` autogestionada con evasión de oclusores — ✅ CERRADA: 0 errores, ciclo completo verificado en Play (abrir→live ON, cerrar→live OFF+foto)**)
**Focus:** Pedido de Juan tras aprobar el fotomatón: la foto queda para la visión general (grillas), pero al abrir la carta del MoriMochi el retrato pasa a ser una cámara EN VIVO filmando a esa criatura spawneada en el mundo en ese momento.

1. **`MonchiLivePortrait` (UI/, singleton runtime, componente hermano en `MonchiPortraitStudio`)**: cámara dedicada `LiveCamera` (hija del studio, mask todo-menos-PortraitStudio, clearFlags Skybox → fondo = la tienda real, far 300) rendereando a RT 512 propia. `Begin(element, dna)`: busca la criatura spawneada por UniqueID en `MoriMochiSpawner.Instance.SpawnedEntries` (accesor `internal`, mismo assembly); si no está → false y el caller cae a la foto. Sigue a la criatura con damp (`followDamp=8`), dirección relativa a su rotación (siempre le ve la cara 3/4) con **evasión de oclusores**: cada 0.5s linecast desde el centro de la criatura a la posición candidata; si algo que no es ella bloquea (jugador/pared/otra criatura), rota entre offsets de yaw `{0,55,-55,110,-110,180}` al primer ángulo despejado. Knobs: `framePadding=0.65`, `cameraPitch=18`, `cameraYaw=155`.
2. **Autogestión del apagado (cero acople con los caminos de cierre del panel)**: `LateUpdate` corta la sesión — apaga cámara y **repinta la foto estática** — si el elemento fue removido del árbol, si está oculto (helper `IsHidden`: camina ancestros mirando `resolvedStyle.display == None`) o si la criatura despawneó. Cubre X, ESC y cualquier cierre futuro sin tocar UIManager.
3. **Wiring mínimo**: `MonchiPortraitUI.ApplyLive(element, dna)` (intenta live, fallback foto) y `MorimonchiDetailInfoUITK.Populate` usa `ApplyLive` para el retrato del header (un cambio de línea). Grillas y demás cartas siguen con la foto.
4. **Dos bugs cazados en Play**: (a) el check de visibilidad por `worldBound` NO detecta `display:none` en un ancestro (UITK saltea el layout del subárbol y el rect conserva su último valor) → reemplazado por el ancestor-walk `IsHidden`; (b) ángulo fijo dejaba al jugador tapando a la criatura → evasión de oclusores por linecast.
5. **Verificación en Play**: abrir carta → cam ON con feed vivo del mundo (criatura encuadrada, fondo tienda; con la manada amontonada post-cañón el plano es caótico — correcto, es live); criatura aislada → encuadre limpio; cerrar panel → cam OFF + sesión limpiada (verificado por reflection). Pull de nube trajo 9 criaturas + 3 muebles (Juan colocó muebles en su sesión) sin errores.

> ### 📌 Notas S57c
> 1. El plano live puede incluir otras criaturas/jugador cerca — es inherente a filmar el mundo real y parte del encanto; la evasión solo garantiza que la criatura NO quede tapada.
> 2. La idea original de S56 (skybox propio/fondo aislado para la carta) quedó descartada de facto por esta versión: Juan pidió explícitamente "cámara live del morimonchi en ese momento" → fondo real.
> 3. Si la criatura no está spawneada (muerta/vendida/en huevo), la carta cae sola a la foto del fotomatón.

**Files Touched (.cs — input ScriptNodes):**
- `UI/MonchiLivePortrait.cs` (NUEVO): cámara live autogestionada del retrato de detalle (seguimiento + evasión de oclusores + auto-apagado).
- `UI/MonchiPortraitUI.cs` (MODIFICADO): método `ApplyLive` (live con fallback a foto).
- `UI/MorimonchiDetailInfoUITK.cs` (MODIFICADO): retrato del header usa `ApplyLive`.

**Files Touched (no-ScriptNode):** GameScene (hija `LiveCamera` en `MonchiPortraitStudio` + componente `MonchiLivePortrait` wireado, guardada), `Assets/Screenshots/s57c_*.png` (7, borrables).

**Next session (S58, candidatos):** sin cambios — 1) integración replay 3v3 con `DragonAnimationDriver` + retiro de legacy; 2) revisión de Juan (caras excluidas, pesos, sizing, wipe opcional); 3) arrastres.

**ScriptNodes (cierre S57c):** CREAR `MonchiLivePortrait.md`; ACTUALIZAR `MonchiPortraitUI.md`, `MorimonchiDetailInfoUITK.md`.

---

**Session:** 2026-07-21 (Session 57b — **CARTAS CON RETRATO REAL: fotomatón `MonchiPortraitService` + helper único `MonchiPortraitUI` en los 10 sitios de carta — ✅ CERRADA: 0 errores, verificado en Play (grilla con 8 retratos únicos + panel detalle)**)
**Focus:** Juan aprobó el tuning de emociones y el lobby ("quedó joya") y pidió, antes del 3v3, que las cartas de la UI muestren al MoriMochi en lugar del cuadrado de color. Se implementó el **híbrido fotomatón** propuesto en S56 nota 6 (la mitad "snapshot cacheado"; la cámara live para la carta enfocada queda pendiente/opcional).

1. **`MonchiPortraitService` (UI/, singleton runtime)**: estudio oculto en GameScene (`MonchiPortraitStudio` @ (500,-500,500), capa nueva `PortraitStudio` slot 9): GO `Booth` con su propio `MonchiVisualizer` (+hijo `Model` como modelRoot) + `BoothCamera` (disabled, FOV 30, far 30, clear a alpha 0 → retrato con fondo transparente). `GetPortrait(dna)`: cache por UniqueID; miss → activa booth, `Assemble(dna)` (armonía/patrón/shiny gratis del pipeline S57), `SetMood(portraitMood=Neutral)`, pose estática `animator.Play("Idle",0,0)+Update(0)`, encuadre por Bounds de los SMR, `camera.Render()` a RT 384px, ReadPixels→Texture2D, desactiva booth. `GetPortraitSprite(dna)` para uGUI. Primera captura ~0-1ms, cache 0ms. Knobs serializados afinados en vivo y persistidos: `framePadding=0.62`, `cameraPitch=10`, `cameraYaw=155` (vista 3/4 — de frente las alas se ven de canto como líneas).
2. **`MonchiPortraitUI` (UI/, static helper)**: único punto que pintan las cartas — `Apply(VisualElement, dna)` (backgroundImage + backgroundSize Contain; fallback exacto al viejo cuadrado BaseColor/gris si no hay servicio o dna null) y `Apply(UnityEngine.UI.Image, dna)` (sprite + preserveAspect; tipo calificado — hubo CS0104 por ambigüedad UIElements.Image/UI.Image, único error de la ronda).
3. **10 call sites reemplazados** (una línea c/u, `MonchiPortraitUI.Apply(...)`): CreatureGridUITK (icono carta), BreedingBreedTabPresenter (candidatos), CombatOnlineTabPresenter (carta central), CombatLineupUITK (carta pool + ghost del drag), CombatLineupBoard (unidad en tablero 2-3-2), TransactionPanelUITK (swatch venta), MorimonchiDetailInfoUITK (retrato header), DetailEquipTabPresenter (retrato equipo), DetailTreesPresenter (chips linaje/descendencia), CreatureVisualUI (grid uGUI in-world, vía sprite).
4. **Aislamiento**: Main Camera excluye la capa PortraitStudio (mask -513); la cámara del booth ve todo pero con far 30 en un punto a 700m del mundo; la luz direccional de la escena ilumina el booth (sin luz propia, sin doble iluminación).
5. **Verificación en Play**: grilla con 8 retratos únicos simultáneos (cuerpo/cuerno/armonía/cara distintos por criatura), panel detalle con retrato en header, PNG crudo inspeccionado (transparencia OK). Un falso negativo durante el testing fue autoinfligido (destruí texturas cacheadas ya bindeadas durante el tuning de encuadre) — en flujo normal no pasa: las cartas se rebindean en cada rebuild y el cache vive lo que vive el servicio.

> ### 📌 Notas S57b
> 1. **Cache nunca se invalida** en runtime (identidad visual inmutable). Si algún día un gen visual muta post-nacimiento (evolución que cambie cuerpo/patrón), agregar invalidación por UniqueID.
> 2. La carta ENFOCADA con cámara live + skybox propio (idea original de Juan, S56 nota 6) sigue pendiente de decisión — el fotomatón ya cubre todo lo visible.
> 3. El retrato usa mood fijo Neutral (knob `portraitMood` serializado por si Juan quiere otro).
> 4. Capa nueva `PortraitStudio` (slot 9) en TagManager.

**Files Touched (.cs — input ScriptNodes):**
- `UI/MonchiPortraitService.cs` (NUEVO): fotomatón — booth oculto + captura a Texture2D/Sprite cacheada por UniqueID.
- `UI/MonchiPortraitUI.cs` (NUEVO): helper estático único de pintado de cartas (retrato o fallback BaseColor).
- `UI/CreatureGridUITK.cs`, `UI/BreedingBreedTabPresenter.cs`, `UI/CombatOnlineTabPresenter.cs`, `UI/CombatLineupUITK.cs`, `UI/CombatLineupBoard.cs`, `UI/TransactionPanelUITK.cs`, `UI/MorimonchiDetailInfoUITK.cs`, `UI/DetailEquipTabPresenter.cs`, `UI/DetailTreesPresenter.cs`, `UI/CreatureVisualUI.cs` (MODIFICADOS): swatch de color → `MonchiPortraitUI.Apply`.

**Files Touched (no-ScriptNode):** GameScene (GO `MonchiPortraitStudio` completo + Main Camera cullingMask sin PortraitStudio, guardada), TagManager (capa 9 `PortraitStudio`), `Assets/Screenshots/s57b_*.png` (7, borrables).

**Next session (S58, candidatos):** sin cambios respecto a S57 — 1) integración replay 3v3 con `DragonAnimationDriver` + retiro de legacy; 2) revisión de Juan (caras excluidas, pesos, sizing, wipe opcional, carta enfocada live); 3) arrastres.

**ScriptNodes (cierre S57b):** CREAR `MonchiPortraitService.md`, `MonchiPortraitUI.md`; ACTUALIZAR (mención breve del retrato) `CreatureGridUITK.md`, `CreatureVisualUI.md`, `MorimonchiDetailInfoUITK.md`, `CombatLineupUITK.md`, `CombatLineupBoard.md`, `CombatOnlineTabPresenter.md`, `BreedingBreedTabPresenter.md`, `TransactionPanelUITK.md`, `DetailEquipTabPresenter.md`, `DetailTreesPresenter.md`.

---

**Session:** 2026-07-21 (Session 57 — **INTEGRACIÓN DE CÓDIGO DEL MODELO NUEVO COMPLETA: pipeline visual Suriyun en gameplay (visualizer + moods + driver Animator + FurType 33 ponderado + shiny 0.5%) — ✅ CERRADA: 0 errores, verificado en Play por MCP (9/9 criaturas con el modelo nuevo, attack/moods/shiny ejercitados)**)
**Focus:** Ejecución autónoma autorizada por Juan (estaba fuera). Se implementó todo el punto 1 de S57: el modelo Suriyun es AHORA el modelo de gameplay. Decisión de Juan previa a irse: FurType plano 0-32 **con tabla de pesos de rareza solo en mint** (opción 3; herencia sigue 50/50) + sistema de settings de emociones para las caras.

1. **Categorización de los 33 patrones** (hojas de contacto generadas con PIL): ~18 son "liso con matices" (misma textura de escamas, cambia solo valor/contraste panza-cuerpo) y ~15 tienen patrón real: acuarela (02,13), pecas/lunares (04,06,08), dos tonos alto contraste (05,19,20,21,26=orca,27), leopardo/rosetas (12,14), reptil/grunge (31,32). Pesos en mint: comunes 1.0 / 0.5 / 0.25 / épicos 0.12 / orca-26 0.05 — editables en `FurTypeDatabase.asset` (dict `mintWeights`).
2. **Data layer**: `FurType` → `Pattern00..Pattern32` (33, mapea 1:1 a `MonchiFur_XX`); enum nuevo **`MonchiMood`** (12: Neutral/Feliz/Triste/Dolor/Enojado/Dormido/Enfermo/Mareado/Asustado/Amoroso/Emocionado/KO); `CreatureDNA.IsShiny` (bool, metadata additive, default false — saves viejos compatibles); `ColorGenetics` gana `BuildHarmony(baseColor, out wing, out accent)` **determinista** (roll por hash FNV del RGB24 → esquemas 40/30/20/10 de S56), `ShinyChance=0.005f` + `RollShiny()`; `FurTypeDatabaseSO` gana `mintWeights` + `RollMintFurType()` (ruleta ponderada, fallback uniforme); `CreatureGenerator.GenerateRandom` acepta `furDb` opcional (roll ponderado + shiny); `BreedingService` rolea shiny del hijo; **gen cuerno = BodyShapeID existente**: `MonchiVisualBankSO.GetBody(id)` mapea por hash estable % 4 cuerpos (dict `bodyOverrides` para control explícito futuro) → **el DNA string NO cambió, contrato de red intacto, NO hizo falta wipe** (FurType viejos 0-4 = Pattern00-04 válidos).
3. **SOs nuevos** (en `ScriptableObjects/Visual/`): **`MonchiVisualBankSO`** (`MonchiVisualBank.asset`: 4 bodies FBX + AnimatorController + 5 gemas + ref moodSet; hash FNV-1a estático NO usa GetHashCode) y **`MonchiMoodSetSO`** (`MonchiMoodSet.asset`: dict mood → lista de materiales de cara, `GetFace(mood)` con fallback a Neutral — **este es el espacio de settings que pidió Juan: sacar/poner caras de una emoción es editar el asset, sin código**). Mapeo cargado: Neutral 01/02/04/07 · Feliz 03/08 · Triste 05/10 · Dolor 06/09 · Enojado 17/25 · Dormido 11/15 · Enfermo 13 · Mareado 14 · Asustado 24 · Amoroso 19 · Emocionado 23 · KO 18. **Excluidas de la rotación (candidatas a "no me gustan", revisión de Juan): 12 (smug), 16 (ojos amarillos), 20 ($), 21 (cruces), 22 (garabato tachado)** — 20 podría servir para modo tienda.
4. **Runtime nuevo** (World/Creatures salvo driver): **`MonchiVisualizer`** (reemplaza a `MoriMonchiVisualizer` en el agente; instancia body por `GetBody(BodyShapeID)`, Animator con controller del bank, fur `MonchiFur_{FurType}` + tinte por grupos de SMR — Wing→armonía1, Horn/Back→armonía2, Teech→marfil 12%, resto→base — MPB con Rim=Lerp(color,blanco,0.65) fix alas radiactivas; shiny → `MonchiGem_*` por hash del UniqueID sin tinte; `SetMood()` swapea material de Face); **`DragonAnimationDriver : MonchiAnimationDriver`** (contrato de combate sobre Animator.CrossFade: attack=Fire con onImpact al 45% del clip, hit=Damage, buff=Yes, defeat=Die terminal, victory=Roar+Jump loop; `ClipLength` matchea clip por nombre más corto que contenga el estado; setea moods durante acciones); **`MonchiLocomotionAnimator`** (velocity del NavMeshAgent → Idle/Walk/Run con histéresis por estado, cede si combatDriver.IsBusy); **`MonchiMoodDriver`** (tick 2.5-5s desincronizado: Condition Sick→Enfermo, InNeed→Triste; por Intent Resting→Dormido, Eating→Feliz, Playing→Emocionado, Held/Fleeing→Asustado, Tumbling→Mareado; default 35% Feliz/65% Neutral).
5. **Wiring**: `MoriMonchiController` retipado a `MonchiVisualizer` + `MonchiVisualBankSO` (firma `Initialize`/`Rebind` igual); `MoriMochiSpawner` pasa `GameManager.MonchiVisualBank` (campo+propiedad nuevos, wireado en GameScene, escena guardada); **`MonchiAnimator.controller`** creado por MCP (13 estados nombres limpios, one-shots con exit time 0.95 → auto-Idle, Idle/Walk/Run loop, Die terminal — decisión S56 #7); prefab `MorimonchiAgent` operado por MCP: fuera `MoriMonchiVisualizer`+`MoriMonchiProceduralAnimator`+hijos viejos de Model, dentro los 4 componentes nuevos con refs wireadas.
6. **BUG CAZADO EN PLAY (paridad de contrato)**: `MoriMochiSpawner.Acquire` reusa prewarmed con `Initialize(bank: null)` a propósito ("saltear re-ensamblado") — el `SetBank(null)` incondicional del controller nuevo PISABA el banco guardado (moods/shiny muertos en prewarmed). Fix: con bank null el controller hace `RefreshLook(dna)` y conserva el banco. Verificado post-fix: 9/9 con banco.
7. **Verificación en Play (GameScene, 0 errores reales)**: 9/9 spawneadas con cuerpo por gen (hash BodyShapeID→FBX determinista), patrón+armonía+cara aplicados, Animator corriendo (Walk/Run en roaming); mint x2000 SIN tocar registry: distribución ponderada correcta (común 74 / épico 14 / orca 5) + shiny 7/2000≈0.5%, 33/33 tipos salen; `PlayAttack` con onImpact/onFinished disparados (logs `[TEST-S57]`); shiny forzado → `MonchiGem_Emerald` y revertido; `SetMood(KO)` → `MonchiFace_18`; mood driver reflejando estado real (9/9 Enfermo por needs drenadas — GameScene sin NeedStations —; con needs llenas rotaron a Neutral/Feliz variados); reconcile de pull de nube (rebound=9) sin errores. Screenshots `s57_modelo_nuevo_ingame*.png` (4).

> ### 📌 Notas S57
> 1. **Wipe de saves NO ejecutado y NO necesario**: todo fue additive (FurType 0-4 viejos válidos, IsShiny default false, BodyShapeID mapea por hash). Queda a criterio de Juan si igual quiere resetear.
> 2. Las 4 entradas "Exception" en consola durante Play son `Debug.Log` de continuaciones async UGS mal clasificadas por el tool MCP (stack empieza en `Debug:Log`) — ruido conocido, no errores.
> 3. `MoriMonchiVisualizer`/`PartVisualBankSO`/`MoriMonchiProceduralAnimator` quedan como **legacy vivos** (los usa el pipeline de combate: `CombatVisualUnits`/`CombatVisualizerService` con su propio prefab). Se retiran cuando se integre el replay 3v3 al driver nuevo.
> 4. Caras excluidas de moods (nota 3 arriba) esperan veredicto de Juan; también el mapeo mood→cara es 100% editable en `MonchiMoodSet.asset`.
> 5. La escala/collider del agente no se retocó — el dragón se ve bien plantado en el piso, pero el sizing fino vs cápsula/cañón es tuning pendiente de Juan.
> 6. Los accesores `internal` del spawner no son visibles desde `execute_code` (assembly distinto) — verificar spawner por FindObjects, no por API interna.

**Files Touched (.cs — input ScriptNodes):**
- `Core/Enums.cs` (MODIFICADO): FurType → Pattern00-32; enum nuevo MonchiMood.
- `Core/ColorGenetics.cs` (MODIFICADO): BuildHarmony determinista por hash + ShiftHue + ShinyChance/RollShiny.
- `Core/CreatureGenerator.cs` (MODIFICADO): param furDb opcional (mint ponderado) + roll shiny.
- `Core/GameManager.cs` (MODIFICADO): campo/propiedad MonchiVisualBank; mint pasa furTypeDatabase.
- `Data/Genetics/CreatureDNA.cs` (MODIFICADO): IsShiny + default Pattern00.
- `Data/Databases/FurTypeDatabaseSO.cs` (MODIFICADO): mintWeights + RollMintFurType.
- `Data/Databases/MonchiVisualBankSO.cs` (NUEVO): banco visual del modelo nuevo (bodies por hash, gemas, controller, moodSet).
- `Data/MonchiMoodSetSO.cs` (NUEVO): settings de emociones (mood → caras).
- `Systems/Breeding/BreedingService.cs` (MODIFICADO): shiny roll del hijo.
- `World/Creatures/MonchiVisualizer.cs` (NUEVO): visualizer del modelo Suriyun.
- `World/Creatures/MonchiLocomotionAnimator.cs` (NUEVO): velocity → Idle/Walk/Run.
- `World/Creatures/MonchiMoodDriver.cs` (NUEVO): Condition/Intent → mood in-world.
- `World/DragonAnimationDriver.cs` (NUEVO): contrato de combate sobre Animator.
- `World/Creatures/MoriMonchiController.cs` (MODIFICADO): fachada al pipeline nuevo + fix bank null (nota 6).
- `World/Spawning/MoriMochiSpawner.cs` (MODIFICADO): MonchiVisualBank en prewarm/acquire.

**Files Touched (no-ScriptNode):** NUEVOS: `Resources/Animations/MoriMochi/MonchiAnimator.controller` (13 estados), `ScriptableObjects/Visual/MonchiVisualBank.asset`, `ScriptableObjects/Visual/MonchiMoodSet.asset`, `Assets/Screenshots/s57_*.png` (4, borrables). MODIFICADOS: `ScriptableObjects/Breeding/FurTypeDatabase.asset` (33 materiales MonchiFur + pesos), `Resources/Prefabs/MorimonchiAgent.prefab` (cirugía completa), GameScene (ref MonchiVisualBank en GameManager, guardada).

**Next session (S58, candidatos):**
1. **Integración replay 3v3 con el driver nuevo** (`CombatVisualizerMM`): `CombatVisualUnit` gana `DragonAnimationDriver`, `ForwardRoutine` → API del contrato, barra recupera el nombre (S52 nota 6), interacciones ingame (S45); al cerrar, retirar los legacy (`MoriMonchiVisualizer`/`PartVisualBankSO`/`MoriMonchiProceduralAnimator`).
2. Revisión de Juan: caras excluidas (12/16/20/21/22), pesos de rareza de patrones, sizing del dragón vs cápsula/cañón, y si quiere el wipe opcional.
3. Arrastran: corrales/cortejo en Play, F5 async 3v3, economía F7, cartas (S56 nota 6), escudo por RONDA.

**ScriptNodes (cierre S57):** CREAR `MonchiVisualizer.md`, `MonchiVisualBankSO.md`, `MonchiMoodSetSO.md`, `DragonAnimationDriver.md`, `MonchiLocomotionAnimator.md`, `MonchiMoodDriver.md`; ACTUALIZAR `Enums.md`, `ColorGenetics.md`, `CreatureGenerator.md`, `GameManager.md`, `CreatureDNA.md`, `FurTypeDatabaseSO.md`, `BreedingService.md`, `MoriMonchiController.md`, `MoriMochiSpawner.md`.

---

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
