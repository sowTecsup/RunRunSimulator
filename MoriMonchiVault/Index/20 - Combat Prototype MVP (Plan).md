---
tags: [index, design, combate, mvp, plan]
---

# 20 - Combat Prototype MVP (Plan aprobado S80) 🪦 HISTÓRICA

> 🪦 **NOTA HISTÓRICA DESDE S92 (2026-09-01). NO IMPLEMENTAR NADA DE ACÁ.**
> El prototipo táctico S77-S88 **no validó**: Juan lo descartó en S91 porque no divertía lo suficiente y — lo central — **le quitaba el foco a lo principal (breeding, cuidado, tienda)**. El combate pasó a ser un extra sencillo al servicio del breeding.
> **La mecánica vigente vive en [[Index/21 - Combate v3 - Dragon RPS]].** Toda la agenda QA heredada de S88-S90 (contraste de dragones, TurnLogPanel vs zoom, presión de oleadas, alturas de cards, juice OnFizzle, UI por beat, quick wins §15) está **MUERTA**.
> Esta nota se conserva por dos motivos: el **§15 (auditoría recuperada)** tiene hallazgos técnicos reutilizables, y `CombatPrototype/` sigue en disco hasta que se decida su demolición.

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

> ⚠️ **Enmiendas S82 (§11)**: la decisión 8 queda revocada en su mitad de acceso (la altura ya no limita al melé), la 9 matizada (techo nuevo: 2 acciones por turno), la 10 matizada (la iniciativa queda, la puntería inteligente se va) y la 11 revocada (ya no hay reacción por beat). Las secciones §3/§4/§5 de abajo ya están enmendadas.

---

## 3 · Reglas v0 (todo tunable — enmendado S82, ver §11)

- **Tablero**: grilla 8×8, alturas 0–2 (medio cubo por nivel). La altura NO limita aterrizajes ni movimientos (decisión 22): cuenta solo para empujes contra desnivel y caídas. Guideline de layout: perímetros irregulares, no lisos (decisión 24 — la rotación al bloqueo los necesita).
- **Ticks**: dragones 5 · enemigos 3 (Bomba 2) · todo ataque (propio o enemigo) = 1 tick · entorno: choque contra muro/unidad +1 (a ambos si unidad), caída ≥2 niveles +1. El movimiento voluntario (aterrizajes de habilidad propia, movimiento enemigo de fin de turno) nunca daña.
- **Turno (orden fijo, determinista)**: 1) planificación (máx 2 acciones + 1 uso por plantilla, preview total) → 2) EXECUTE: beats del jugador → 3) ataque enemigo: TODOS los vivos ejecutan su ataque telegrafiado en su apuntado actual, haya o no objetivo (activadores automáticos) → 4) movimiento enemigo: los golpeados este turno (cualquier fuente) ejecutan su patrón de ajedrez; patrón bloqueado → NO se mueve y rota 90° horario su apuntado; estuvo en el aire este turno → no se mueve → 5) nueva planificación (telegraphs re-pintados).
- **Resolución del beat (orden fijo, determinista)**: 1) acciones del jugador contra el snapshot del inicio del beat, aplicadas por orden de slot (izq→der): el dragón VIAJA a su celda de aterrizaje y la plantilla golpea en el anclaje · 2) muertes · 3) aterrizajes de aéreos · cierre. Ya NO hay reacciones por beat (decisión 18). Overkill posible (2 golpes al mismo en un beat = plantilla gastada) y la proyección lo muestra.
- **Targeting (decisiones 21-22)**: cursor libre por TODO el tablero; anclaje + rotación definen el área de impacto Y la celda de aterrizaje del dragón (fija por la forma de la plantilla). Validez = celda de aterrizaje libre. Si al ejecutarse el aterrizaje se ocupó → FIZZLE: acción cancelada, plantilla gastada, mostrado en proyección.
- **Apuntado enemigo**: estado persistente (inicial definido por el layout); solo cambia por rotación al bloqueo. El telegraph relativo (decisión 12) viaja con el enemigo desplazado; se cancela solo si queda imposible.
- **Juggle**: lanzado → aire durante el beat siguiente → aterriza al final de ese beat 1 celda en la dirección del golpe (celda mostrada). Aterrizaje sobre ocupada: +1 tick a ambos, cae a la libre más cercana en la dirección del lanzamiento. Máx 1 enemigo en el aire por lanzador. Aéreo este turno → sin movimiento de fin de turno.
- **Fin**: victoria = sin enemigos (placeholder de Q4) · derrota = 3 dragones a 0 · R = restart. Sin permadeath en el MVP. Sin curación (heridas persistentes si se encadenan niveles).
- **Input** (controles de S77 §4.1.8): F1-F3 dragón · 1-3 plantilla · WASD/mouse mueve el cursor de anclaje por el tablero · Q/E rota · Enter/click confirma al beat actual · Tab crea beat nuevo · Backspace deshace último slot · clic derecho sobre enemigo = brief · R restart.

## 4 · Kits (3 plantillas por dragón: 1 vuelo + 2 ataques — enmendado S82: anclaje + aterrizaje)

Idioma nuevo (decisión 21): cada habilidad = **forma de plantilla** (área de impacto en el anclaje elegido) + **aterrizaje** (celda del dragón, fija por la forma) + efectos. El anclaje va a cualquier parte del tablero (decisión 22).

| Dragón | Vuelo | Ataques (forma → aterrizaje → efectos) |
|---|---|---|
| **Tanque** (5t) | Vuelo corto: celda libre, solo destino | Empujón: enemigo objetivo + dirección; aterrizás adyacente del lado desde donde empujás; 1 tick, empuja 2 · **Agarre aéreo**: solo contra enemigo EN EL AIRE; aterrizás adyacente al objetivo; 1 tick + slam dirigido a ≤2 |
| **Tirador** (5t) | Planeo: celda libre, solo destino | Disparo: línea de 3; aterrizás en la BASE de la línea; 1 tick, empuja 1 alejándose · Tiro en arco: celda única; NO te mueve (aterrizaje = quedarse); 1 tick, sin empuje |
| **Ágil** (5t) | Gran salto: celda libre, solo destino | Golpe: celda única; aterrizás adyacente (lado elegido con la rotación); 1 tick · **Voltereta**: celda única; aterrizás adyacente; 1 tick + LANZA AL AIRE |

⚠️ Observación S82 (tunable): con el anclaje sin límite los 3 vuelos quedan funcionalmente idénticos (celda libre, solo destino). Se mantienen por su valor de esquive sin gastar ataque; diferenciarlos o recortarlos lo decide el playtest.

**Combo canónico a validar**: beat 1 — Ágil lanza con Voltereta · beat 2 — Tanque agarra en el aire y estrella contra muro = 1+1+1 entorno = 3 ticks. Es el movimiento-firma del prototipo, y entra justo en el presupuesto nuevo de 2 acciones por turno.

## 5 · Enemigos v0 (enmendado S82: activadores automáticos + movimiento ajedrez)

Modelo S82 (decisiones 17-20): el enemigo NO persigue ni apunta — es un **activador automático** con apuntado persistente (inicial por layout). Ataca SIEMPRE en su turno, haya o no objetivo. Solo se mueve al final del turno SI FUE GOLPEADO, ejecutando su **patrón de ajedrez** (visible en el brief); bloqueado → rota 90° horario.

| Enemigo | Ticks | Ataque (automático, en su apuntado) | Movimiento (si golpeado, fin de turno) | Brief (ejemplo) |
|---|---|---|---|---|
| **Goblin** | 3 (2G+1) | Golpe a la celda frontal adyacente, 1 tick | **Torre-2**: avanza 2 al frente; bloqueado → rota 90° | "Siempre golpea al frente · Si lo golpeás, avanza 2 · Sin lugar, gira" |
| **Arquero** | 3 (2G+1) | Proyectil en línea de 3 al frente, 1 tick | **Alfil-2**: 2 en diagonal frontal-derecha; bloqueado → rota 90° | "Siempre dispara en línea · Si lo golpeás, se desliza en diagonal · Sin lugar, gira" |
| **Bomba** | 2 | No ataca. A 0 ticks DETONA: 1 tick a las 8 celdas + empuje radial 1 (encadena caídas). Con 1 tick restante su intent muestra el área | Estática (sin patrón, EMPUJABLE) | "No se mueve · 2 golpes y detona · Empujala" |

La Bomba es el modelo-contador de S77 traducido al idioma nuevo (ticks = contador) y la pieza de combo estrella. Los patrones Torre-2/Alfil-2 son asignación v0 del orquestador — tunables.

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

## 10 · Feedback S81 (primera jugada de Juan) — HOJA DE RUTA S82

> Anotaciones de Juan tras jugar el prototipo al cierre de S81. **Esta lista ES la agenda de la próxima sesión**, por encima de la fase 5 genérica. Los puntos 2 y 3 son cambios de mecánica (documentados como decisión de Juan, pendientes de bajar a reglas exactas al abrir S82); el resto es presentación/juice.

1. **Verticalidad Bad North + cámara**: estructuras más verticales; que rotar la isla y observarla sea RECOMPENSADO — no se debe poder leer todo el tablero a simple vista. Probar bloques más altos (subir `LevelHeight` y/o elevaciones más allá de 2). Implica añadir rotación de cámara (orbitar por pasos) y layouts con oclusión deliberada.
2. **Enemigos SIN movimiento propio** — ✅ **CERRADO S82, reglas exactas en §11**: solo se reposicionan al ser GOLPEADOS (la reacción se mantiene); en su turno solo ejecutan su ataque telegrafiado. Juan: *"los enemigos solo debían moverse si eran golpeados; ahora solo me moví y ellos también se movían"*. Matiza la decisión 10-11 de S80: la INICIATIVA de ataque queda, el movimiento de intención (persecución/reposicionamiento del `EnemyBrain`) se elimina.
3. **⭐ Plantilla = zona de impacto + desplazamiento del MoriMonchi** — ✅ **CERRADO S82, reglas exactas en §11**: se selecciona la ZONA de impacto en el tablero y el MoriMochi SE MUEVE y ejecuta ahí — no "plantillas que salen del MoriMonchi quieto hacia los extremos". Juan: *"no nos estamos moviendo hacia la posición: estamos ejecutando un ataque de plantilla EN la posición"*. Restaura §4.1.7 de S77 (posición = consecuencia de la plantilla): atacar y moverse vuelven a ser EL MISMO verbo. Cambio grande de targeting + resolver: la acción lleva celda de anclaje del template + celda de aterrizaje del dragón.
4. **UI de secuencia por beat**: marcar sobre el tablero/HUD cómo se mueve cada uno en cada tick (números de orden de beat sobre las celdas, lectura de la coreografía completa de un vistazo).
5. **Presupuesto visible**: cada habilidad usada queda bloqueada hasta que los enemigos actúan al final (la lógica ya lo hace — reforzar la lectura en UI).
6. **Popups de tick de daño** encima de las entidades (DamageNumbersPro, ya en el proyecto).
7. **Juice de impacto**: los bloques del tablero tiemblan/se mueven con el impacto de la plantilla. **TODO el VFX/juice se monta con Feel/MMFeedbacks** (pedido explícito de Juan).

---

## 11 · Decisiones S82 (reglas exactas de los puntos 2 y 3 del feedback)

> Cerradas con Juan al abrir S82 (2026-08-26). Prevalecen sobre §2 donde contradigan; §3/§4/§5 ya están enmendadas con este contenido. Numeración continúa la tabla de §2.

| # | Decisión | Revoca / matiza |
|---|----------|-----------------|
| 17 | **Enemigos = activadores automáticos**: en su turno SIEMPRE ejecutan su ataque telegrafiado en su dirección de apuntado, haya o no objetivo en la plantilla (el Arquero dispara su línea igual). No apuntan a nadie: el apuntado es un ESTADO que el jugador manipula | Matiza la decisión 10 (la iniciativa queda; la puntería inteligente se va) |
| 18 | **Cero movimiento propio**: el enemigo solo se mueve al FINAL del turno SI FUE GOLPEADO ese turno (cualquier fuente). Se eliminan la persecución/reposicionamiento del `EnemyBrain` Y la reacción por beat | Revoca la decisión 11 y las "reacciones" de §3; los contras (arrinconar/ráfaga) pierden objeto — el limitador nuevo es el presupuesto (dec. 23) |
| 19 | **Movimiento = patrón estilo ajedrez** propio de cada tipo, visible en el brief/tooltip, determinista, relativo a su apuntado. Patrón bloqueado → NO se mueve y ROTA 90° horario su apuntado (*los muros redirigen enemigos* — de ahí la guideline de perímetros irregulares, dec. 24) | Reemplaza "se aleja 2 del último atacante" |
| 20 | **Apuntado inicial definido en el layout** (cada spawn trae su facing). El telegraph relativo (decisión 12) sigue: forma+dirección viajan con el enemigo desplazado | — |
| 21 | **Plantilla = anclaje + aterrizaje**: el jugador elige dónde ejecutar (celda de anclaje + rotación); la FORMA define el área de impacto Y la celda de aterrizaje del dragón. Atacar y moverse son EL MISMO verbo | Restaura §4.1.7 de S77; revoca el targeting "desde el dragón quieto" de las fases 2-3 |
| 22 | **Transición sin límite**: el anclaje va a cualquier parte del tablero; la altura NO limita el aterrizaje (cuenta solo para empujes y caídas). Validez = celda de aterrizaje libre. Aterrizaje ocupado al ejecutarse → FIZZLE (acción cancelada, plantilla gastada, la proyección lo muestra). El movimiento voluntario nunca daña | Revoca la decisión 8 en su mitad de acceso (arriba deja de ser intocable) |
| 23 | **Presupuesto: máx 2 acciones por planificación** (además del 1 uso por plantilla de la decisión 9). La presión vuelve por desgaste: los enemigos atacan TODOS los turnos y el jugador solo hace 2 cosas | Matiza la decisión 9 (el techo ya no es 9) |
| 24 | **Ganchos S82 (NO en v0)**: stun por elementos del mapa (anula el movimiento de fin de turno) · habilidades que alteran el desplazamiento enemigo más allá del empuje · **perímetros irregulares como herramienta de diseño** (los layouts nuevos del punto 1 deben evitar bordes lisos) | — |

**Decisiones v0 del orquestador (tunables, con veto de Juan)**: orden del turno = beats del jugador → ataque enemigo → movimiento de golpeados · "golpeado" cuenta cualquier fuente (fuego amigo enemigo y entorno incluidos) · la rotación al bloqueo REEMPLAZA al movimiento ese turno (no rota y mueve) · rotación siempre horaria · el patrón es todo-o-nada (si no entra completo → rota; genera más rotaciones = más control por geometría) · el movimiento de ajedrez ignora altura y no genera caídas (es voluntario) · enemigo que estuvo en el aire ese turno no ejecuta movimiento (hereda la decisión 13) · los ataques enemigos golpean sus celdas sin importar la altura (coherente con dec. 22) · patrones v0: Goblin Torre-2, Arquero Alfil-2, Bomba estática · el slam del Agarre conserva su ≤2 (es distancia de lanzamiento, no de anclaje).

---

## 12 · Auditoría QoL S85 — hallazgos y plan de fixes (AGENDA S86) — ✅ EJECUTADA COMPLETA EN S86

> S86 (2026-08-28): los 9 fixes del plan ejecutados y verificados con capturas (ver Estado). El hallazgo 18 (desfase del brief) NO se reprodujo a 1920×1080 — sin fix. Bonus S86: el zoom de rueda de S83 era un dolly no-op (la cámara es ORTOGRÁFICA — la distancia no cambia el tamaño); ahora escala `orthographicSize` de verdad.

> S85 (2026-08-27): auditoría de quality-of-life del nivel pedida por Juan, hecha con verificación VISUAL (capturas del Game view en Play: `Assets/Screenshots/qol_planning_seleccion.png`, `qol_post_turno.png`, `qol_camara_rotada.png`). 20 hallazgos, 8 confirmados en captura. Lección de pipeline en memoria persistente del orquestador (`feedback_verificacion_visual_screenshots`): la verificación por estado (fases/eventos/consola) no detecta NADA de esto — capturas obligatorias al cerrar trabajo de presentación.

### Hallazgos

**A. UI que se pisa (confirmados en captura)**
1. El banner de planificación (2 líneas + la línea SEMILLA agregada en S84) invade el `selectionLabel` (top:56) — texto sobre texto semitransparente.
2. `beatStrip` (bottom:216) y `actionBudgetLabel` (bottom:246) quedan DETRÁS de las cards, que crecieron con las minigrids S84.
3. Las cards + EXECUTE tapan el borde inferior del tablero: celdas y enemigos invisibles e inclickeables de facto.
4. El banner tapa las filas altas de la isla (el Tanque voló a (7,7) y quedó invisible detrás del banner).
5. Labels `G2·1` y markers de spawn se apilan texto-sobre-texto cuando el telegraph cae junto a enemigos existentes.

**B. Texto de mundo**
6. Labels de ticks y markers se orientan a cámara SOLO al crearse (`CombatUnitView.Init`, `NightSpawner.PaintTelegraph`) — tras orbitar con ←/→ quedan de canto, ilegibles.
7. El glifo ☠ del marker no existe en la font TMP → se dibuja tofu "□".
8. El "1" del marker es fijo — no informa nada.

**C. Encuadre y contraste**
9. La órbita rota el pivote pero no re-encuadra: a 90° el tablero queda descentrado (dos tercios del frame vacíos) y las unidades cortadas por el banner.
10. El encuadre base ya deja la fila superior de la isla debajo del banner.
11. Checkerboard (0.78 vs 0.52) y elevaciones lavados en pantalla — la lectura vertical estilo Bad North sufre (colores ya serializados en `CombatBoardBuilder` + revisar luz).

**D. Feedback de acciones**
12. El FIZZLE es invisible (`ResolutionAnimator`, rama vacía): la acción se cancela en silencio y la plantilla se gasta sin explicación.
13. Los highlights viven todos en el mismo plano (y+0.02) con alpha — al solaparse producen colores intermedios sin significado.
14. Popups de daño (§10.6) y UI de secuencia por beat (§10.4): pendientes YA agendados, no ceguera nueva.

**E. Input y flujo**
15. El clic atraviesa la UI: `CombatPrototypeHUD.IsPointerOver` es un stub que devuelve false y `CombatInputController` no consulta nada — clic en EXECUTE/cards también actúa sobre el tablero detrás.
16. No hay tecla de deselección (Esc sin mapear).
17. Victoria/Derrota sin botón de reinicio (solo el texto "R").
18. Riesgo no verificado: `EnemyBriefPanel` posiciona con píxeles de pantalla vs panel UITK escalado — posible desfase según resolución.
19. El texto de victoria dice "materiales obtenidos" — placeholder mentiroso, el sistema no existe.
20. El label de la semilla es blanco como todos — la unidad a proteger no se distingue tipográficamente.

### Plan de fixes propuesto (orden de dolor)

1. **Clic-a-través**: implementar `IsPointerOver` real (picking del panel UITK) y consultarlo en `CombatInputController` antes de raycastear.
2. **Re-layout del HUD**: reservar franjas (superior: banner+selección apiladas con layout de flujo, no offsets mágicos; inferior: cards) y re-encuadrar el tablero al área libre entre franjas.
3. **Billboard por frame** (LateUpdate) para labels de unidades y markers de spawn.
4. **FIZZLE visible**: feedback Feel en la celda + línea en HUD/log de turnos.
5. **Marker de spawn**: glifo compatible con la font (o sprite TMP), sin número fijo.
6. **Órbita que re-encuadra**: pivot al centro real de la isla + framing consistente en los 4 yaws.
7. **Contraste del tablero**: retune de colores serializados + iluminación.
8. **Jerarquía de highlights**: offset de altura por tipo o prioridad de color al solaparse.
9. **Menores**: Esc deselecciona · botón Reiniciar en fin de partida · texto de victoria sin placeholder · label de semilla tintado.

---

## 13 · Kit unificado y reglas de disparo (S87 — feedback de Juan, prevalece sobre kits y reglas anteriores)

> S87 (2026-08-28): Juan pidió que los 3 MoriMonchis compartan los mismos 3 poderes para testear, disparos con reglas propias, proyectiles visibles, giro hacia el ataque y animación de vuelo. Implementado y verificado con capturas en 3+ turnos jugados.

### Kit unificado (assets `AB_Unified*` en `CombatPrototype/Abilities/`; los 9 viejos quedan sin wirear como rollback)

| Poder | Data | Comportamiento |
|-------|------|----------------|
| **Salto sismico** (`AB_UnifiedQuake`) | Attack · DirectionalTemplate · anillo 8 vecinos · `Landing=AtAnchor` · `PushDistance=1` · `PushFromCenter=true` · `IgnoresObstacles=true` | Saltás al anclaje; los 8 vecinos reciben 1 tick + empuje RADIAL (dirección por víctima = `DominantCardinal(landing→víctima)`). Es también el único movimiento del kit: **válido aunque la onda no toque a nadie** (excepción de plantilla vacía para `AtAnchor`). Respeta altura. |
| **Disparo perforante** (`AB_UnifiedPierce`) | Attack · línea (0,0)-(3,0) · `Landing=Stay` · `IgnoresHeight=true` · `IgnoresObstacles=true` | Línea de 4 que golpea a TODAS las unidades de la línea, sigue de largo sobre huecos y **ignora la altura**. No te movés. (La minigrid 5×5 solo muestra 3 de las 4 celdas — límite conocido.) |
| **Alzavuelo** (`AB_UnifiedLift`) | Attack · (0,0) · `Range=1` · `Landing=Stay` · `LaunchesAirborne=true` | Golpea una celda adyacente (Chebyshev ≤ 1, validación de rango NUEVA) y eleva al aire; el drag orienta hacia dónde se desplaza 1 celda al aterrizar. Respeta altura. El aéreo aterriza solo al cierre del beat siguiente / inicio del turno enemigo (sin Agarre en el kit no hay soft-lock). |

### Reglas nuevas (decisiones v0 del orquestador, vetables)

1. **Disparo ⇔ `IgnoresHeight=true`** (campo que estaba muerto, ahora ES la semántica): ignora altura, proyectil visible, y en el kit siempre va con `IgnoresObstacles` (atraviesa).
2. **Filtro de altura** para ataques no-disparo: celda excluida de la plantilla si `|elev(celda) − elev(landing del atacante)| ≥ 2`. Vive en `GetAffectedCells` (firma nueva con `unit`) → proyección==ejecución comparten la verdad. Exclusión con `continue` (no corta la plantilla).
3. **`IgnoresObstacles` también continúa sobre celdas fuera de bounds/huecos** (antes cortaba la plantilla).
4. **`Range > 0` limita el anclaje** (Chebyshev desde el atacante) en DirectionalTemplate — antes el campo no se validaba.
5. **Enemigos = SOLO disparos, perforantes** (Goblin pasó a `RangedLine`/2 por data; Arquero ya era línea-3): golpean a todas las unidades de su línea (fuego amigo entre enemigos incluido, estilo ITB), siguen sobre huecos, briefs actualizados.

### Presentación (S87 + pulido)

- **Proyectiles**: prefab `CombatProjectile` + tunables `projectilePrefab`/`projectileSpeed`/`projectileHeight` en `ResolutionAnimator`; vuela del atacante a la última celda de la plantilla, antes de shakes/hits. `ResolutionEvent.Projectile` lo marca (disparos del jugador + todo `EnemyAttack`).
- **Giro EN VIVO al apuntar**: componente nuevo `SelectionFacingPreview` (en `Combat`) rota el visual del dragón seleccionado hacia `CurrentDirection` durante Planning. Requisito: `SetDirection`/`Rotate`/`SetCursor` (si cambió la dirección) ahora disparan `SelectionChanged` — antes solo refrescaban highlights y el giro nunca se veía (bug reportado por Juan).
- **Facing post-ataque**: si la unidad viajó, `Impact.Facing` = dirección de viaje (`DominantCardinal(origen→landing)`); si no, la dirección de la acción.
- **`baseYawOffset` = 0 en el prefab `UnitView`** (era 180 — los DragonSD miran +Z natural; apuntar al este mostraba la cara al oeste; nunca se notó porque los dragones jamás rotaban y los enemigos son cápsulas).
- **Spawn facing**: jugadores se despliegan mirando a cámara (`SetFacingInstant(0,-1)` en `Init`).
- **Vuelo**: `Anim_Dra_Fly`/`Anim_Dra_Idle` (con guarda `HasState`) alrededor de los lerps de `MoveTo`/`LandTo`/`LaunchUp`.

### Enmienda S88 (Juan, durante el playtest): el disparo también es plantilla
`AB_UnifiedPierce` ya NO dispara quieto: **el MoriMochi viaja al anclaje y dispara desde ahí** (`Landing` Stay→AtAnchor, `TemplateOffsets` (0,0)-(3,0)→**(1,0)-(4,0)**: la línea de 4 sale hacia adelante desde la celda de aterrizaje). Aplican las reglas de siempre de anclaje/aterrizaje (FIZZLE si se ocupa). En `ActionResolver`, el `Impact.Facing` de una habilidad con `IgnoresHeight` (disparo) es SIEMPRE la dirección del disparo, no la de viaje — el dragón aterriza y gira hacia donde dispara. Los enemigos siguen disparando quietos (activadores, sin cambio).

### Pendientes menores registrados
- ~~Minigrid recorta la plantilla del perforante~~ → RESUELTO S88: `AbilityCardVisuals` con auto-encuadre (bounds de offsets hasta ±4, celdas de 5px si >6 columnas, aterrizaje verde para `AtAnchor`, `flexShrink 0` — la causa raíz del grid invisible era el colapso flex a 0.67px de ancho) + cards 250→282.
- Texto guía del HUD para `AirborneEnemy` quedó muerto (sin habilidad de ese tipo en el kit).
- El hook `Feedbacks/OnFizzle` (MMF_Player) sigue vacío — montar juice cuando entren los popups (§10.6).

---

## 14b · Ciclo v2: gasto por PODER + ciclo fijo de 3 + cámara Bad North (S88 tarde — enmienda al §14 tras el primer playtest de Juan)

Juan jugó el ciclo v1 y corrigió: *"solo se gasta la habilidad, no todo el morimonchi — solo se bloquea el poder"*. Cerrado por AskUser:

1. **Gasto por poder**: usar un poder lo bloquea POR EL CICLO (`spentAbilities`, clave unitId·8+abilityIndex). El dragón sigue disponible con sus otros poderes. Se elimina el agotamiento por dragón del §14.
2. **Ciclo FIJO de 3 turnos** (`cycleLength` serializado): al cierre del turno 3 del ciclo → ataque enemigo → refuerzos → TODOS los poderes se restauran. Ya no depende de agotarse.
3. **Combo libre** (revoca "1 acción por dragón" del §14): un dragón puede gastar sus 2 acciones del turno con poderes distintos. Sigue: 2 acciones/turno, un poder no se repite dentro del turno.
4. **HUD**: banner con countdown "⚔ ATAQUE ENEMIGO en N turnos"; en N=1 "⚠ ÚLTIMO TURNO — ¡atacan al terminar!" con fondo rojo. Poder gastado = "✗ nombre" gris en la card; card entera gris solo si el dragón no tiene NINGÚN poder. Si nadie tiene poderes, el botón EXECUTE pasa a "PASAR" (turno vacío permitido solo en ese caso).
5. **Cámara Bad North** (elegida por Juan sobre ortho-ITB y órbita libre): Main Camera en perspectiva FOV 30, pitch 38° (antes ortho 50°); el encuadre perspectiva ahora muestrea la silueta real (hU/hR compartido con la rama ortho) + compensa franjas de UI + `perspectiveFill` (1.3, serializado) para llenar pantalla estilo Bad North. Órbita por pasos, zoom (dolly real) y pan intactos.
6. Verificado en Play: T1 mismo dragón gastó 2 poderes y siguió seleccionable · countdown 3→2→1 con warning · cierre de ciclo con ataque (dragones apilados comieron líneas: t5→t2/t3), refuerzos 2→4 y restauración total · reacciones en turnos 1-2 sin ataque. Consola 0/0.
7. **Quirk de pipeline (nota 12)**: las capturas de `manage_camera` pueden omitir el overlay UITK según el timing de repaint del Game view — la UI se verifica por estado del panel (`root.childCount`, `panel` del banner), NUNCA solo por captura.

## 14 · Ciclo de turnos por agotamiento (S88 — decisiones de Juan, enmienda la decisión 23 de §11; **SUPERSEDIDO por §14b en lo que contradigan**)

Juan corrigió el modelo de presión: los enemigos NO atacan cada turno. El ritmo que pidió: "turno 1, turno 2, turno 3 recién atacan los enemigos", con descanso de los dragones usados.

### Reglas (cerradas con Juan por AskUser, S88)
1. **Agotamiento**: cada dragón que actúa queda AGOTADO hasta después del ataque enemigo. Máximo **1 acción por dragón por turno** (el combo del turno = 2 dragones distintos; presupuesto de 2 sigue). Elegido por Juan: "fuera hasta el ataque enemigo" (NO descanso de 1 turno).
2. **Ataque enemigo por agotamiento**: los enemigos ejecutan TODOS sus ataques cuando TODA la bandada viva está agotada. Con 3 dragones: T1 usás 2 → T2 usás el restante → el cierre del T2 dispara el ataque enemigo (= "turno 3"). Emergente aceptado: con dragones muertos el ciclo se acorta (más presión al perder unidades).
3. **Telegraph siempre**: en los turnos sin ataque las intenciones siguen visibles (elegido por Juan sobre "ocultos" y "countdown individual").
4. **Reacciones cada turno**: golpeado → rota hacia el atacante y ejecuta su patrón de movimiento al cierre de ESE turno (fase nueva `Reacting`; `ActionResolver.ResolveEnemyReactions` = aterrizajes + movimientos, sin ataques). El turno enemigo completo (`ResolveEnemyTurn`) queda solo para el cierre de ciclo.
5. **Refuerzos por ciclo**: las oleadas entran SOLO tras el ataque enemigo (elegido por Juan). El telegraph de la oleada siguiente queda visible durante todo el ciclo.
6. **Movilidad visible**: el movimiento por patrón se anima saltando de casilla en casilla (evento `Move` con `Path`; un salto con arco por celda, tunable `hopDuration`).

### Decisiones v0 del orquestador (vetables)
- La germinación se chequea al cierre del turno del jugador, ANTES del ataque enemigo (el countdown "germina en N" cuenta turnos del jugador; si el turno 8 cae a mitad de ciclo, los enemigos no llegan a atacar).
- La semilla solo recibe daño en el cierre de ciclo → la presión real es la acumulación de enemigos telegrafiando.
- HUD: línea "BANDADA: frescos N/M" en el banner + aviso rojo "ESTE PLAN AGOTA LA BANDADA" cuando el plan dispara el ataque + cards agotadas en gris con leyenda + línea "⚔ ATAQUE ENEMIGO" en el TurnLog.
- UI: PanelSettings propio del prototipo (`CombatPrototypePanelSettings.asset`, refRes 1280×720 → todo ×1.5 en 1080p; el juego sigue con `StandartPanelSettings`). Franjas de cámara retuneadas en escena (top 0.19 / bottom 0.44). `minZoom` 0.5→0.25 (pedido de Juan: acercarse más).
- Fase `Reacting` se salta en silencio si nadie fue golpeado y no hay aéreos (guard `HasPendingReactions` — evita el banner-parpadeo; el reseteo de flags corre igual).
- **Bug cazado en el playtest S88**: al entrar a Play el panel UITK puede recrearse dejando el árbol del HUD colgado de un panel viejo NO-null → la guarda S81 (`panel == null`) no lo detectaba y Juan entró a Setup SIN UI. Fix: detección de panel *stale* (`elemento.panel != document.rootVisualElement.panel`) con auto-curación por `Update` en `CombatPrototypeHUD` y `TurnLogPanel`, y guarda mejorada en `EnemyBriefPanel`.

---

## 15 · Auditoría recuperada (sesión Remote Control post-S84, volcada en S90)

> Origen: sesión "Unity motor technologies research" corrida en otra máquina (~S85, entre las tools propias de S84 y los fixes de S86) y nunca volcada al vault. Recuperada en S90 leyendo claude.ai. Diagnóstico puro con `verify_prototype_parity`, `sim_prototype_turns`, `manage_profiler` y `unity_reflect`. Los hallazgos ya cubiertos por S86 (FIZZLE visible) se omiten; el resto sigue vigente salvo nota.

### Vigente — rendimiento
1. **118 materiales únicos en el tablero**: `CombatBoardBuilder` usa `.material` (instancia por bloque, `enableInstancing=false`) → **314 draw calls / 0 batching** medidos. Fix de una línea: `sharedMaterial` + `MaterialPropertyBlock` (como YA hace `BoardHighlighter`). No urgente a 12ms de frame, pero la isla va a crecer y el juice viene en camino.
2. En planificación idle: ~27KB / 521 allocations por frame (parte es overhead de editor; medir en build antes de actuar). Ahora medible por `manage_profiler` (MCP) o `get_performance_stats` (CLI).

### Vigente — legibilidad del targeting
3. **El combo canónico fizzlea según la dirección, sin aviso**: `BehindAnchor = TargetCell − Direction` puede aterrizar sobre tu PROPIO dragón (el que atacó el beat anterior) → el remate se cancela y el juego no comunica por qué. Verificado por sim: Voltereta (3,5) + Agarre dir (0,-1) = fizzle; misma jugada dir (1,0) = kill de 3 ticks.
4. **Confirmar un target inválido es silencioso**: `TargetingController` devuelve null sin distinguir "aterrizaje ocupado" / "no hay nadie en el aire" / "slam no cardinal". Candidato natural para la UI de secuencia por beat (§10.4).

### Vigente — data y tests
5. **`Range`/`RangeMin` son campos muertos** en `CombatAbilitySO`: cero usos en el código (solo se lee `SlamRange`), pero están poblados en los 9 assets viejos → cualquier tuneo es un no-op silencioso. Decidir: borrar o reconectar. *(Nota S90: `IgnoresHeight`, que la auditoría también marcaba muerto, fue reciclado en S87 §13 como semántica de disparo — ese punto quedó desactualizado.)*
6. **Cero tests en el proyecto** (confirmado de nuevo en S90 vía `unity list_tests`): `ActionResolver`, `AbilityTargeting` y `CombatEffects` son lógica pura estática — caso ideal para un asmdef de EditMode tests que corra por `unity run_tests` sin abrir Play.
7. Dato de diseño a tener nombrado: con "transición sin límite" (S82), cada ataque es válido en ~110 de 118 celdas → el tablero funciona como superficie de ruteo de daño; la tensión viene del presupuesto de 2 y del entorno. No es bug — es la consecuencia de la decisión, dicha en voz alta.

### Verificado sano (entonces y hoy)
- Paridad proyección==ejecución: idéntica en planes de 1-2 beats, fizzles, juggle y turnos enemigos encadenados; el estado canónico nunca se filtró.
- Cero `partial class` en todo el código (el commit `f1129fd` cerró esa deuda; CLAUDE.md corregido en S90).
- Archivos >400 líneas FUERA del prototipo (regla 3 pendiente, sin urgencia): MoriMochiAgent 705 · MoriMochiSpawner 626 · AgentSocial 560 · AgentBrain 524 · EquipmentBackpackUITK 479 · Enums 462 · BreedingContainer 454 · BuildModeController 421 · StorePanelUITK 409. El prototipo limpio (mayor: CombatPrototypeHUD 350).

---

## Estado

**FASES 1-4 EJECUTADAS Y VERIFICADAS (S81).** Prototipo jugable: escena `CombatPrototype.unity` + 29 scripts en `Scripts/CombatPrototype/` + assets en `CombatPrototype/` (9 habilidades, 3 dragones con prefabs DragonSD, 2 enemigos, 1 layout). Verificación central cumplida: 7 comparaciones proyección-vs-ejecución idénticas (ActionResolver único, §6) incluyendo el combo canónico Voltereta→Agarre→Slam y una partida completa de 4 turnos hasta la victoria.

**S82 (2026-08-26): puntos 2 y 3 del feedback BAJADOS A REGLAS EXACTAS (§11, decisiones 17-24) E IMPLEMENTADOS Y VERIFICADOS** — enemigos activadores automáticos con movimiento ajedrez + plantilla anclaje/aterrizaje con transición sin límite + presupuesto de 2 acciones por turno. 20 scripts tocados (18 modificados + 2 nuevos: `BoardImpactFeedback`, `CombatCameraController`), 11 assets re-parametrizados, `BoardLayout_Isla` NUEVO (12×12, alturas 0-4, celdas-hueco `.` para perímetro irregular, spawns con facing `>`/`<`/`^`/`v`), `levelHeight` 1.06 (altura real del DragonSD), vibración de bloques con Feel (`MMWiggle` por bloque + hook `MMF_Player`), cámara orbital por pasos de 90° (flechas ←/→). Verificación MCP: consola 0 errores/0 warnings · **proyección==ejecución IDÉNTICAS en 2 rondas jugadas** (ronda de empuje-contra-muro + Torre-2 + Alfil-2, y el combo canónico Voltereta→Agarre→Slam-contra-muro = 3 ticks = muerte, que entra justo en el presupuesto de 2 acciones) · rotación-al-bloqueo y fizzle verificados en frío sobre clones. Falta el playtest de Juan (checklist §8). El resto del §10 (punto 4 UI de secuencia, punto 6 popups DamageNumbersPro) sigue en agenda. El pendiente obligatorio de editor de S75 (assets Horn/Back/Wing/Face/CutieMark + rewiring + limpieza de GameScene) sigue vigente e independiente de este prototipo.

**S86 (2026-08-28): QoL §12 COMPLETA.** Los 9 fixes ejecutados y verificados con capturas en los 3 yaws: picking UITK real + gate de input (clic ya no atraviesa la UI) · HUD por franjas de flujo (cero overlaps) · `WorldLabelBillboard` por frame (labels legibles tras orbitar) · FIZZLE visible (shake + línea en TurnLog + hook MMF) · marker "×" sin tofu · encuadre ORTOGRÁFICO real (pivote de bounds de celdas jugables, `orthographicSize` calculado de la silueta por-celda, franjas top/bottom tunables, consistente en 4 yaws; el zoom de rueda ahora escala de verdad) · contraste serializado (light 0.82/0.79/0.70 · dark 0.40/0.46/0.44) · jerarquía de highlights por prioridad (Selection>Spawn>Intent>Landing>Path>Template, solo el mayor visible por celda + `stackStep`) · Esc/botón Reiniciar/textos/label de semilla tintado. Hallazgo 18 descartado (no se reproduce).

**S88 (2026-08-28): CICLO DE TURNOS POR AGOTAMIENTO (§14) IMPLEMENTADO Y VERIFICADO + UI ×1.5.** 10 scripts tocados (8 por morimonchi-coder + 2 micro-ediciones del orquestador), `CombatPrototypePanelSettings.asset` NUEVO, franjas de cámara retuneadas. Verificado jugando un ciclo completo por código: T1 dos acciones → fase `Reacting` (el golpeado giró y ejecutó su patrón, SIN ataque enemigo ni refuerzos) → T2 con el único fresco (aviso rojo en banner) → ataque enemigo (fuego amigo mató a un goblin dañado) → refuerzos por borde → bandada reseteada. Consola 0 errores/0 warnings; capturas miradas (`s88_*` en Screenshots). Pendiente visual menor: salto multi-celda del patrón (Path con 2+ celdas) no se ejercitó a ojo — el goblin de la prueba movió 1 sola celda.

**S87 (2026-08-28): KIT UNIFICADO + REGLAS DE DISPARO (§13) + pulido de giros.** Ver §13 — es la fuente de verdad del idioma vigente: 3 poderes compartidos, disparo ignora altura/atraviesa/proyectil, filtro de altura para el resto, enemigos solo-disparo perforantes, giro en vivo al apuntar, facing por dirección de viaje, `baseYawOffset` corregido, vuelo `Anim_Dra_Fly`. Verificado con capturas en múltiples turnos jugados; consola limpia en todas las tandas.

**S83 (2026-08-26): LEGIBILIDAD — feedback del primer playtest de Juan sobre S82, implementado y verificado.** Regla de trabajo nueva de Juan: en presentación nunca cumplir el mínimo — preguntarse siempre si es legible para el usuario. Cambios: (a) vibración de bloques en TODO impacto — evento `Impact` nuevo emitido por `ActionResolver.ResolveAttack` con las celdas de plantilla (sin tocar estado; proyección==ejecución intacta) + shake en ataques enemigos; amplitud/duración de wiggle subidas en escena; (b) selección legible — anillo blanco bajo el dragón seleccionado, línea de estado-guía bajo el banner (qué está seleccionado y qué falta hacer), card con fondo+borde, habilidad seleccionada como pill amarillo y usadas "— usada", clic sobre dragón propio selecciona (estilo ITB); (c) cámara — zoom con rueda (la órbita ←/→ de S82 ya funcionaba; era descubribilidad) + controles visibles en el banner. 8 scripts modificados, 0 nuevos, verificación MCP completa (0 errores, ejercitado en Play). Del §10 quedan: punto 4 (UI de secuencia por beat), punto 6 (popups) y la mitad de layouts del punto 1 (verticalidad/oclusión deliberada).
