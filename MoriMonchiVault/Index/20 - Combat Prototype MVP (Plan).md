---
tags: [index, design, combate, mvp, plan]
---

# 20 - Combat Prototype MVP (Plan aprobado S80)

> **Sesión 80 (2026-08-25).** Juan entregó un draft de MVP del combate y lo cerró en sesión con 3 rondas de decisiones. **ESTADO: PLAN APROBADO — listo para ejecución.** Este documento ES la fuente de verdad de la mecánica del prototipo: reemplaza al "documento de mecánica en limpio" que pedían las notas 17/19 (el prototipo es el instrumento de validación).
>
> ⚠️ **El gate "nada baja a código" fue levantado por Juan SOLO para este prototipo**: escena y carpeta aisladas, cero integración con los sistemas del juego. Todo lo demás sigue congelado hasta que el prototipo valide (o no) el concepto.
>
> **Prevalece sobre [[Index/19 - Combate Nuevo - Predictive Tactical Extraction]] donde contradigan** (las revocaciones están listadas abajo). La visión macro (expedición, extracción, roster, breeding↔combate) sigue viviendo en la 19 — este MVP solo valida el núcleo del encuentro.

Relacionado: [[Index/19 - Combate Nuevo - Predictive Tactical Extraction]] · [[Index/17 - Refundacion del Combate]]

---

## 1 · Objetivo del MVP

Validar si **planificación + lectura de intenciones + ejecución simultánea** es divertido, legible y profundo — ANTES de integrar Horn/Back/Wings/Personalidad/Cutie Marks/Equipment. Escena independiente (`CombatPrototype`), primitivas como terreno, unidades simplificadas, cero arte, cero persistencia, cero cloud.

**Prioridad declarada por Juan**: gameplay > legibilidad > iteración rápida > arquitectura extensible > presentación.

---

## 2 · Decisiones S80 (fuente de verdad) y qué revocan

| # | Decisión | Revoca / cierra |
|---|----------|-----------------|
| 1 | **El prototipo reemplaza al documento de mecánica** como instrumento de validación | El entregable pendiente de las notas 17/19 |
| 2 | **Q1 = LOTE**: se arma toda la coreografía y se da PLAY | Q1 de la 19 §4.4 |
| 3 | **Coreografía por BEATS**: línea de tiempo de turnos internos; cada beat acepta N acciones de distintos dragones que se ejecutan SIMULTÁNEAMENTE | La cola lineal del draft inicial de S80 |
| 4 | **Todo es plantilla** (ataques Y movimientos/vuelo); no existe movimiento libre por pasos (sin BFS) | El "mover por pasos" del draft inicial; restaura la identidad de las alas de S77 §4.1.9 |
| 5 | **Vida en TICKS, daño uniforme**: todo golpe = 1 tick, no existe stat de daño. Dragones 5, enemigos 3 | Cierra C4 ("vida en hits") de la 19 §4.4 |
| 6 | **Enemigos = guardia 2 + golpe de gracia 1**: los primeros 2 ticks rompen la defensa, el 3ro remata (presentación + estado en data). En v0 cualquier fuente quita cualquiera | — (ganchos futuros: recuperación de guardia en ciertos turnos, gracia-solo-ataque) |
| 7 | **El entorno es la única fuente de ticks extra**: empuje contra muro/unidad = +1 (a ambos si unidad), caída ≥2 niveles = +1. Texto plano: *"todo golpe quita 1; el escenario quita los demás"* | — |
| 8 | **Altura sin bonus de daño**: el melé no alcanza celdas 2+ niveles arriba (arriba = intocable para cuerpo a cuerpo); volar/caídas son la ventaja | El "+1 daño desde arriba" del draft inicial |
| 9 | **Q5 = 1 uso por plantilla por planificación** (3 dragones × 3 plantillas = máx 9 acciones repartidas en los beats que quieras) | Q5 de la 19 §4.4 |
| 10 | **Q3 = los enemigos SÍ tienen iniciativa**: intención telegrafiada + ejecutan al final de tu coreografía. La variante countdown ("actúa en el beat N") es experimento de fase 5 | Q3 de la 19 §4.4; revoca "solo reaccionan al toque" de S77 §4.1.6 (la Bomba sobrevive como enemigo) |
| 11 | **Enemigos REACTIVOS en movimiento**: golpeado en un beat → movimiento de reacción al cierre de ese beat; solo ATACAN cuando les toca. Anti-cargamontón | — |
| 12 | **Telegraph RELATIVO** (estilo Into the Breach real): la intención committea forma + dirección ancladas al enemigo; si se desplaza, sus casillas de ataque viajan con él; se cancela solo si queda imposible. Fuego amigo entre enemigos emerge solo | Revoca el "fijo a casillas absolutas; desplazado → cancela" de la 1ra ronda S80 |
| 13 | **Juggle determinista**: ataques con "lanza al aire" → 1 beat en el aire, aterrizaje en celda fija y visible; solo plantillas aéreas lo tocan; el Agarre aéreo del Tanque lo estrella donde elijas (≤2). Enemigo que estuvo en el aire NO reacciona ese beat | — |
| 14 | **Q2 = preview total**: la proyección muestra todo (estado por beat, reacciones, telegraphs desplazados, aterrizajes). Nada de memoria | Q2 de la 19 §4.4 |
| 15 | **Kits distintos por dragón** (proto-genes: valida "genes = catálogo de habilidades" sin partes) | — |
| 16 | **Determinismo total en el MVP**: cero RNG (filtro nota 17 intacto) | — |

**Siguen abiertas (diseño, NO las decide el prototipo)**: Q4 (qué termina un nivel — el MVP usa "matar todo" como placeholder) · Q6 (mapeo parte→verbo) · PE.1 (presupuesto de contenido) · el corte del determinismo de escenarios (19 Parte 2 #3).

---

## 3 · Reglas v0 (todo tunable)

- **Tablero**: grilla 8×8, alturas 0–2 (medio cubo por nivel). Las plantillas de vuelo ignoran desnivel según su spec; el melé no alcanza 2+ niveles arriba.
- **Ticks**: dragones 5 · enemigos 3 (Bomba 2) · todo ataque (propio o enemigo) = 1 tick · entorno: choque contra muro/unidad +1 (a ambos si unidad), caída ≥2 niveles +1.
- **Coreografía**: fase de planificación → se arma la línea de beats → EXECUTE → corre beat a beat → enemigos vivos ejecutan su intención → nueva planificación. Límite: 1 uso por plantilla por fase.
- **Resolución del beat (orden fijo, determinista)**: 1) acciones del jugador contra el snapshot del inicio del beat, aplicadas por orden de slot (izq→der) · 2) muertes · 3) aterrizajes de aéreos · 4) reacciones de enemigos golpeados este beat (orden por índice; aéreos no reaccionan) · cierre. Overkill posible (2 golpes al mismo en un beat = plantilla gastada) y la proyección lo muestra.
- **Reacciones**: reposicionamiento corto propio de cada enemigo, respeta terreno (no entra a ocupadas, no sube 2+); bloqueada por completo → se queda quieto (**arrinconar habilita el cargamontón**). Enemigo que reaccionó al menos una vez → en su turno solo ataca (su movimiento se gastó); nunca tocado → intención completa (mover + atacar).
- **Contras del jugador a la reactividad**: arrinconar · lanzar al aire (niega la reacción) · ráfaga (3 ticks en un solo beat = muere sin reaccionar).
- **Juggle**: lanzado → aire durante el beat siguiente → aterriza al final de ese beat 1 celda en la dirección del golpe (celda mostrada). Aterrizaje sobre ocupada: +1 tick a ambos, cae a la libre más cercana en la dirección del lanzamiento. Máx 1 enemigo en el aire por lanzador.
- **Fin**: victoria = sin enemigos (placeholder de Q4) · derrota = 3 dragones a 0 · R = restart. Sin permadeath en el MVP. Sin curación (heridas persistentes si se encadenan niveles).
- **Input** (controles de S77 §4.1.8): F1-F3 dragón · 1-3 plantilla · WASD/mouse apunta al piso · Q/E rota · Enter/click confirma al beat actual · Tab crea beat nuevo · Backspace deshace último slot · clic derecho sobre enemigo = brief · R restart.

## 4 · Kits (3 plantillas por dragón: 1 vuelo + 2 ataques)

| Dragón | Vuelo | Ataques |
|---|---|---|
| **Tanque** (5t) | Vuelo corto: celda libre ≤2, ignora desnivel | Empujón (adyacente, 1 tick, empuja 2) · **Agarre aéreo**: solo contra enemigo EN EL AIRE, 1 tick + slam dirigido a ≤2 |
| **Tirador** (5t) | Planeo: celda libre ≤3 en línea recta | Disparo (línea de 3, 1 tick, empuja 1 alejándose) · Tiro en arco (celda a distancia 2–3, ignora altura y obstáculos, 1 tick, sin empuje) |
| **Ágil** (5t) | Gran salto: celda libre ≤3, ignora altura | Golpe (adyacente, 1 tick) · **Voltereta**: adyacente, 1 tick + LANZA AL AIRE |

**Combo canónico a validar**: beat 1 — Ágil lanza con Voltereta · beat 2 — Tanque agarra en el aire y estrella contra muro = 1+1+1 entorno = 3 ticks. Es el movimiento-firma del prototipo.

## 5 · Enemigos v0

| Enemigo | Ticks | Intención | Reacción al ser golpeado | Brief (ejemplo) |
|---|---|---|---|---|
| **Goblin** | 3 (2G+1) | Persigue al dragón más cercano, golpe adyacente 1 tick; empate → prefiere la derecha | Se reacomoda 2 casillas alejándose del último atacante | "Se acerca al más cercano · Prefiere la derecha · Al ser golpeado se reacomoda" |
| **Arquero** | 3 (2G+1) | Mantiene distancia 2–3, dispara en línea de 3, 1 tick | Retrocede 2 manteniendo la línea | "Mantiene distancia · Dispara en línea · Retrocede si lo tocás" |
| **Bomba** | 2 | No ataca. A 0 ticks DETONA: 1 tick a las 8 celdas + empuje radial 1 (encadena caídas). Con 1 tick restante su intent muestra el área | Sin reacción (estática, EMPUJABLE) | "No se mueve · 2 golpes y detona · Empujala" |

La Bomba es el modelo-contador de S77 traducido al idioma nuevo (ticks = contador) y la pieza de combo estrella.

---

## 6 · Arquitectura

**Carpeta**: `Assets/RunRunSimulator/Scripts/CombatPrototype/` (Assembly-CSharp; namespace o prefijo propio para no colisionar con el combate S75 enterrado en git — decidir en fase 1 según convención). **Escena**: `Assets/RunRunSimulator/Resources/Scenes/CombatPrototype.unity`. ~20 archivos, todos <~250 líneas, composición estricta (regla 11 de CLAUDE.md).

**REGLA TÉCNICA INNEGOCIABLE**: `ActionResolver` es LA única función determinista que aplica un beat a un estado (daño, empujes, caídas, lanzamientos, aterrizajes, reacciones). La consumen DOS clientes: `PlanProjection` (preview) y `PlanExecutor` (ejecución real). **Nunca dos lógicas** — si divergen, el jugador pierde confianza en el preview y muere el pilar central.

- **Data (SOs, sin Odin — no llevan diccionarios)**: `CombatAbilitySO` (tipo {Movimiento, Ataque}, plantilla de offsets rotable, empuje, flags {LanzaAlAire, SoloAéreo, SlamDirigido}, reglas de altura) · `PlayerUnitDefinitionSO` (ticks, 3 habilidades) · `EnemyDefinitionSO` (ticks, patrón de intención + parámetros, patrón de reacción + distancia, líneas del brief) · `BoardLayoutSO` (filas de texto con alturas + spawns).
- **Board**: `CombatBoard` (modelo lógico puro: celdas, altura, ocupación, consultas, mundo↔celda) · `CombatBoardBuilder` (primitivas desde layout) · `BoardHighlighter` (pool de quads: plantilla, intención, ruta, aterrizaje).
- **Units**: `CombatUnit` (ticks, celda, loadout, `AirborneState`, estado guardia/gracia) · `PlayerUnit` / `EnemyUnit` · `CombatUnitView` (primitiva coloreada, tweens de vuelo/arco, pips de guardia + gracia con TMP).
- **Planning**: `PlannedAction` · `Choreography` (lista de `Beat`; `Beat` = lista de acciones simultáneas; presupuesto 1-uso-por-plantilla; undo por slot) · `PlanProjection` (estado simulado beat a beat vía ActionResolver) · `TargetingController` · `CombatInputController` (polling directo `Keyboard.current`/`Mouse.current`, SIN tocar los action maps del juego).
- **Resolución**: `ActionResolver` · `PlanExecutor` (corrutina beat a beat, tweens simultáneos) · `EnemyBrain` (intención determinista, desempates fijos) · `EnemyIntent` (forma + dirección ancladas) · `EnemyTurnController`.
- **Flow/UI**: `CombatPrototypeManager` (fases: Planificación → Ejecución → Turno enemigo → chequeo → loop) · `CombatPrototypeHUD` (**UITK construido en C#, sin UXML**; reutiliza `StandartPanelSettings.asset`): tira de beats con slots, 3 cards (ticks + plantillas + usos), EXECUTE, banner de turno, resultado · `EnemyBriefPanel` (clic derecho).

**Se reutiliza**: `StandartPanelSettings.asset` · TextMeshPro · DamageNumbersPro (popups, fase 5) · Feel/MMFeedbacks (opcional). **NO se toca**: `GameEvents`, `GameManager`, `UIManager`, `SaveSystem`, InputActions, `GameScene`, databases, cloud. Comunicación interna del prototipo por eventos C# locales/referencias directas (es UN sistema — no viola la regla 1, que es cross-system). **Puente futuro sin código extra**: las unidades comen un loadout plano (ticks + lista de AbilitySO); el día de la integración, Horn/Back/Wing/CutieMarks/Equipment se traducen a ese loadout.

---

## 7 · Fases de ejecución (una sesión por fase; morimonchi-coder + verificación MCP)

| Fase | Contenido | Verificación de cierre |
|---|---|---|
| **1. Tablero** | SOs, CombatBoard, Builder, Highlighter, escena, cámara isométrica fija | Consola 0 errores · tablero con desniveles en Play |
| **2. Vuelo y coreografía** | Unidades + views, input, plantillas de movimiento, tira de beats, proyección y ejecución beat a beat (solo movimiento) | Coreografía de 3 vuelos en 2 beats, undo, EXECUTE |
| **3. Ataques y entorno** | Plantillas de ataque, ActionResolver completo (ticks, guardia/gracia, empujes, caídas, muros, lanzamiento/agarre/slam, muerte), resolución simultánea | El combo Voltereta→Agarre se previsualiza y ejecuta idéntico |
| **4. Enemigos** | Definiciones, brains, intents relativos telegrafiados, reacciones, turno enemigo, brief, victoria/derrota/restart | Partida completa de varios turnos |
| **5. Contenido y tuning** | Bomba, 2–3 layouts con verticalidad real, números, popups de daño, **experimentos: countdown de intents · recuperación de guardia · gracia-solo-ataque** | Checklist de evaluación (§8) jugado por Juan |

Cada fase cierra con `read_console` 0 errores + ejercicio en Play por Unity MCP. Mutaciones (escena + .assets del prototipo) autorizadas por Juan en S80 — todo en carpeta/escena nuevas.

---

## 8 · Evaluación (las 7 preguntas de Juan → observables)

1. **¿Leer intenciones divierte?** → ¿cancelás/esquivás ataques por lectura? ¿aprendés a PROVOCAR reacciones a propósito (golpear para reacomodar al enemigo hacia donde te conviene)? Si eso aparece, la reactividad pasó de defensa a herramienta — validación doble.
2. **¿Planificar en lote funciona?** → si querés ejecutar de a una acción para ver qué pasa, Q1 se reabre (cambio barato: ejecutar la cola de a 1).
3. **¿Los combos satisfacen?** → ¿el kill típico usa 2+ dragones? ¿el combo aéreo aparece espontáneamente y es el momento fuerte? ¿los beats llevan 2-3 acciones o siempre 1 (→ recortar simultaneidad)?
4. **¿La info permite anticipar?** → ¿el brief se consulta y luego se deja de consultar (= patrón aprendido)?
5. **¿La verticalidad aporta?** → ¿se disputa el alto? ¿empujar al vacío y el juggle se buscan? Si no, se recorta.
6. **¿Ganaste por planificación?** → test del texto plano: ¿podés explicar en una frase por qué ganaste?
7. **¿Justifica integrar los sistemas?** → si pedís más habilidades y combinaciones, esa hambre valida "genes = catálogo de movesets".

## 9 · Riesgos y prohibiciones

- **Riesgo 1 — proyección vs ejecución divergen**: mitigado por ActionResolver único (§6). Innegociable.
- **Riesgo 2 — lote sin feedback frustra**: es Q1; el MVP existe para medirla, no se arregla por adelantado.
- **Riesgo 3 — sobrecarga de lectura** (reacciones + telegraphs desplazados + aéreos): mitigación = pocas entidades (3v3-4), reacciones cortas (≤2), TODO mostrado por la proyección, nada de memoria. Aire: máx 1 enemigo, dura 1 beat, aterrizaje siempre marcado. El juggle es modular — se quita sin tocar el resto.
- **PROHIBIDO en v0**: niebla/preview de mapa, extracción/push-your-luck, consumibles, Cutie Marks, personalidad, permadeath, curación, animaciones, arte, sonido, más de 3 tipos de enemigo, más de 3 plantillas por dragón. Todo eso se gana el derecho a existir si el núcleo diverte.

---

## Estado

**PLAN APROBADO (S80) — listo para fase 1.** Próximo paso: sesión de ejecución de la fase 1 (tablero). El pendiente obligatorio de editor de S75 (assets Horn/Back/Wing/Face/CutieMark + rewiring + limpieza de GameScene) sigue vigente e independiente de este prototipo.
