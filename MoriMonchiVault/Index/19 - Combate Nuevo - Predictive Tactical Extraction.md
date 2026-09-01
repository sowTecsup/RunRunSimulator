---
tags: [index, design, draft, rediseno, combate]
---

# 19 - Combate Nuevo — Predictive Tactical Extraction (Handoff S76)

> **Sesión 76 (2026-08-11).** Juan entregó un handoff de gameplay **totalmente nuevo** para el combate y lo declaró explícitamente: *"olvídate de lo que estuvimos hablando antes respecto al gameplay de combate [...] descarta lo del board de desvíos, al final no me gustó"*.
>
> ⚠️ **ESTO REEMPLAZA LAS PARTES 7 Y 8 DE [[Index/18 - Pilares del Rediseno (Draft)]]** (tablero de desvíos + archipiélago). La **Parte 1 de la nota 18 sigue viva** (ciclo día/noche, 6 genes, ítem único, Cutie Marks, monedas) — el combate reutiliza esas piezas.
>
> **ESTADO: DRAFT — rumbo elegido, mecánica sin cerrar.** Sigue vigente que **nada baja a código** hasta el documento de mecánica en limpio.
>
> ⚠️ **Actualización S80 (2026-08-25)**: Juan levantó el gate para un **prototipo aislado** y cerró la mayor parte de la ronda 4 en sesión. Las decisiones de mecánica del MVP viven en ~~Index/20 - Combat Prototype MVP (Plan)~~ (borrada S93) y **prevalecen sobre §4.1 y §4.4 donde contradigan** (mover vuelve como plantilla de vuelo · enemigos con iniciativa + intención telegrafiada relativa · reactividad en movimiento · vida en ticks con guardia/gracia · juggle aéreo · la bomba sobrevive como enemigo). No re-litigar acá lo que la 20 ya decide.
>
> **Convención (igual que la nota 18):** Parte 1 = el handoff de Juan, verbatim (fuente de verdad, no interpretar). Parte 2 = lo que Juan cerró en la sesión de evaluación. Parte 3 = lectura del orquestador. **Parte 4 (S77) = la visualización concreta de Juan del flujo completo** — cierra la pregunta madre de la ronda 3 y deja la ronda 4 lista.

Relacionado: [[Index/18 - Pilares del Rediseno (Draft)]] · [[Index/17 - Refundacion del Combate]] · [[Index/16 - Diagnostico por Frentes]]

---

## Qué murió con este handoff (para que nadie lo resucite por error)

- El **tablero de desvíos** completo (nota 18 Parte 7): cambiadores de dirección, perímetro, línea recta por ticks, ChuChu Rocket como núcleo.
- El **archipiélago** (Parte 8): el tablero que crece, cuota por etapa, rampa x2·x3·x4, modo infinito como corrida.
- **"Genes = conectores"** — la pieza integradora de S74. Su reemplazo es la decisión 1 de la Parte 2 de esta nota (genes = catálogo de habilidades).
- **La costura del lore (§8.6)** y sus 2 preguntas, y **las dudas D1–D6** de §8.8 — quedan sin objeto.
- **El PvP por snapshot + mailing** (§1.6 de la nota 18) — descartado por decisión de Juan en S76 (ver Parte 2, decisión 4). La competencia pasa a ser indirecta.

---

# PARTE 1 — El handoff de Juan (fuente de verdad, verbatim)

## MORIMONCHIS — GAMEPLAY HANDOFF
### Predictive Tactical Extraction + Breeding/Shop Simulator

### 1. VISIÓN GENERAL

MoriMonchis combina un **shop/breeding simulator** con una aventura táctica corta basada en:

- Predicción · Posicionamiento · Preparación · Resolución automática del combate · Lectura de patrones · Gestión del riesgo · Extracción · Rotación de un roster de MoriMonchis.

La aventura debe sentirse como una expedición de aproximadamente **10–20 minutos**, integrada dentro del ciclo día/noche del simulador.

El objetivo no es crear un autobattler tradicional donde el jugador simplemente arma una composición y observa quién gana.

La fantasía central es:

> **"Conozco lo suficiente para preparar un plan. Ahora quiero descubrir si realmente estaba preparado."**

Y después:

> **"Mi plan funcionó."**

Pero siempre debe existir suficiente incertidumbre para que el jugador tenga que decidir cuánto quiere arriesgar.

### 2. IDENTIDAD DEL COMBATE

El combate se puede conceptualizar como **Predictive Tactical Extraction** con resolución tipo autobattle.

El jugador NO debería controlar directamente a los MoriMonchis durante la resolución. El jugador juega principalmente la **fase de planificación**. Durante la ejecución: el jugador da PLAY y observa cómo su plan se enfrenta al comportamiento de los enemigos y al entorno.

La emoción buscada: *"Ajá... esto está funcionando."* — seguida ocasionalmente de: *"Espera... esto no lo había previsto."*

El fracaso idealmente debe sentirse como **"mi modelo mental estaba equivocado/incompleto"** y no como *"perdí porque el RNG decidió matarme"*.

### 3. FASE DE PLANIFICACIÓN

Antes de ejecutar, el jugador analiza: posiciones · enemigos · entorno · patrones conocidos · estado de sus MoriMonchis · ataques disponibles · composición · riesgo acumulado · información parcial del mapa.

El jugador construye un **setup inicial**. La intención no es necesariamente programar cada microacción futura. El objetivo es establecer **qué quiere que hagan sus MoriMonchis y cómo quiere que interactúen sus acciones con el espacio y los enemigos**.

**Control del jugador:** selección de MoriMonchis · posicionamiento · selección/colocación de ataques · preparación de la estrategia · lectura del enemigo · decisión de riesgo.

**Autonomía del sistema:** movimiento durante la resolución · ejecución de acciones · reacciones de enemigos · interacciones con el entorno · consecuencias emergentes.

El grado exacto de autonomía todavía queda abierto a diseño.

### 4. PREDICCIÓN DE ENEMIGOS

Los enemigos deben tener comportamientos relativamente comprensibles. El jugador puede aprender sus movimientos, reacciones, patrones, ritmos y condiciones. Tras enfrentarlos varias veces: *"Sé qué va a hacer."* — una capa de **conocimiento permanente del jugador**.

Sin embargo, **conocer un enemigo no significa conocer la expedición completa**. La incertidumbre viene de: qué enemigos aparecerán · en qué combinación · qué variante del entorno · cómo interactuarán comportamientos · qué oportunidades aparecerán · qué situaciones no fueron contempladas en el setup.

Un jugador experto domina las reglas básicas de los enemigos, pero todavía tiene que interpretar situaciones nuevas.

### 5. COREOGRAFÍA TÁCTICA

Cada MoriMonchi puede disponer de ataques representados mediante **plantillas/áreas de efecto**. El jugador decide cómo colocar esas acciones dentro de la situación.

El interés no está únicamente en *"¿cuánto daño hace este ataque?"* sino en *"¿dónde estará el enemigo cuando esta acción ocurra?"* y *"¿qué consecuencia tendrá moverlo?"*.

Las acciones pueden provocar cambios de posición y generar cadenas de acontecimientos. El entorno también puede afectar estas relaciones. Conceptualmente: un enemigo puede cambiar de posición tras un impacto · una acción puede desplazarlo · una interacción con el escenario puede alterar su posición · ese cambio puede modificar dónde impactará una acción posterior.

El jugador no calcula solamente *ataque → daño*. Intenta construir:

> **posición → reacción → desplazamiento → nueva posición → siguiente acción.**

Esto genera una **coreografía táctica**.

### 6. REFERENCIA CONCEPTUAL: AJEDREZ

No copiar sus reglas — la idea es: **"puedo conocer cómo se mueve una pieza, pero prever todas las consecuencias de varias piezas actuando simultáneamente es difícil."** El desafío está en anticipar dónde terminará, qué provocará, cómo interactuará con otro enemigo, cómo alterará el resto del tablero. El juego premia **pensar varios pasos adelante** sin exigir calcular absolutamente todo.

### 7. REFERENCIA CONCEPTUAL: DISHONORED

**Un problema puede tener múltiples soluciones.** No se busca que cada encuentro tenga una única respuesta correcta. No *"encuentra la combinación correcta"* sino **"encuentra una solución que funcione con las herramientas que tienes"**. Esto permite que diferentes MoriMonchis tengan valor en situaciones diferentes.

### 8. EL AUTOBATTLE

El autobattle existe principalmente en la **fase de resolución**. El jugador: analiza → prepara → posiciona → decide acciones → ejecuta PLAY → observa.

No debe sentirse como un juego de acción con reacción constante. El placer está en **ver una estrategia previamente construida convertirse en una secuencia de acciones**.

**Hipótesis:** *"Creo que si hago esto, el enemigo terminará aquí y entonces mi siguiente acción funcionará."* → **PLAY:** la simulación demuestra si la predicción era correcta. → **Resultado:** *"Funcionó."* o *"No había considerado esta interacción."*

### 9. INCERTIDUMBRE

El juego no debería depender exclusivamente de RNG. La incertidumbre debe provenir principalmente de **información incompleta y situaciones no completamente controlables**.

El jugador puede conocer patrones, comportamientos, tipos de enemigos, posibles condiciones del mapa. Pero no necesariamente conoce el orden exacto, todas las combinaciones, todas las situaciones, qué enemigo concreto encontrará dentro de un rango, cómo interactuarán las variables.

> **"Sé lo suficiente para tomar una decisión informada, pero nunca tengo certeza absoluta."**

### 10. DAÑO Y MAESTRÍA

El experto no necesariamente sale siempre sin daño. Puede existir una amenaza sin respuesta perfecta; la habilidad consiste en **mitigar el daño y decidir si ese costo vale la pena**. Un novato recibe mucho daño de una situación; un experimentado la reconoce, la anticipa y reduce el costo.

> **El daño puede ser una consecuencia de una decisión estratégica, no una penalización aleatoria.**

### 11. EL COMBATE NO TERMINA LA AVENTURA

Ganar un encuentro no significa *"terminé la misión"*. La expedición continúa. Después de cada sección: **¿puedo seguir?** La pregunta no es solo *"¿puedo ganar?"* sino **"¿puedo sobrevivir hasta la próxima extracción?"** — una segunda capa de estrategia encima del combate.

### 12. EXTRACTION / PUSH YOUR LUCK

La expedición tiene puntos de extracción: **extraer** (asegura recompensas) o **continuar** (mantiene el equipo expuesto). La recompensa aumenta con la profundidad, y el riesgo también.

*"Estoy bastante bien. Creo que puedo hacer una etapa más."* vs. *"Mis MoriMonchis ya están muy dañados. No creo que lleguemos."* — la decisión debe sentirse como una **apuesta consciente**.

### 13. PREVIEW DEL MAPA

El mapa no se muestra de forma literal. El jugador recibe información por **iconografía, siluetas, tipos de amenaza, indicaciones de entorno, posibles recompensas, puntos de recuperación**.

```text
Entrada → ⚔️ → 🌿 → ⚔️ → 💚 → ⚔️ → 💎 → Extracción
```

El jugador entiende aproximadamente qué le espera, sin conocimiento absoluto. La habilidad: **leer el mapa como información táctica** y decidir qué roster llevar.

### 14. PROFUNDIDAD

La profundidad introduce progresivamente: nuevos enemigos, nuevas interacciones, nuevas situaciones, nuevas mecánicas ambientales, mejores recompensas, materiales más valiosos, objetos de mayor calidad. **No** *"enemigos con más vida"* — sino **"finalmente llegué a un lugar donde aparecen cosas que todavía no conozco"**. La exploración como motivación.

### 15. ROSTER

No incentivar *"los 5 más fuertes"* — el objetivo es un **roster de posibilidades**. Cada MoriMonchi con diferentes características, partes, combinaciones, Cutie Marks, historias, personality, aplicaciones tácticas. Un MoriMochi mediocre hoy se conserva porque *"tengo una composición en mente que quiero explorar"* o *"podría funcionar cuando consiga determinada combinación"*. Descubrimiento y experimentación.

### 16. BREEDING COMO PARTE DEL SISTEMA TÁCTICO

El breeding no existe solo para *"mejores estadísticas"*: su función principal es construir nuevas **posibilidades tácticas**. Combinaciones de padres, partes, cuidados, alimentación, Cutie Marks y equipamiento hacen viables determinadas composiciones o estrategias.

```text
Problema encontrado → "Necesito otra solución" → Tienda/Breeding → Experimentación → Nuevo MoriMonchi → Nueva expedición
```

### 17. ROTACIÓN Y VIDA DEL ROSTER

Los MoriMonchis no son eternos: su uso eventualmente termina y pueden retirarse. Evita la composición definitiva única. Emoción deseada: **"este MoriMonchi me acompañó durante muchas aventuras."** Su personalidad, nombre, crianza y sonidos hacen que el jugador recuerde al individuo. Al retirarse puede volverse contenido **legacy** (etapa futura).

### 18. PERSONALIDAD

No tiene que traducirse en estadísticas: se manifiesta por sonidos, pitch, expresiones, reacciones, mensajes/gibberish, comportamiento expresivo. **"Ese es MI MoriMonchi."** Refuerza el apego y da peso emocional a perder o retirar uno.

### 19. EL LOOP COMPLETO

```text
TIENDA → (Breeding | Preparar) → ROSTER → EXPEDICIÓN
  → Leer información → Preparar estrategia → PLAY → SIMULACIÓN
  → (Éxito → Recompensas | Fracaso → Consecuencias)
  → ¿CONTINUAR? (SÍ → más riesgo | NO → Extracción)
  → TIENDA → analizar / mejorar / criar / preparar
```

### 20. LOS TRES PLACERES PRINCIPALES

1. **PLANEAR** — *"Creo que esta composición puede resolverlo."*
2. **VER FUNCIONAR EL PLAN** — *"¡Funcionó exactamente como lo había imaginado!"*
3. **ARRIESGAR** — *"Podría retirarme ahora... pero quizá pueda llegar una etapa más."*

Se alimentan mutuamente: el combate produce información → la información produce estrategias → las estrategias producen necesidades → las necesidades vuelven a la tienda → la tienda produce posibilidades → las posibilidades vuelven a la aventura.

### 21. DEFINICIÓN CORTA DEL SISTEMA

**MoriMonchis es un simulador de tienda y crianza conectado a expediciones tácticas de extracción donde el jugador selecciona y prepara un roster de criaturas únicas, estudia patrones enemigos, programa una estrategia espacial, pulsa PLAY para observar su ejecución automática y decide cuánto quiere arriesgar antes de regresar a casa.**

La experiencia central no es *"¿quién tiene mejores estadísticas?"* sino **"¿entendí la situación lo suficiente como para preparar una estrategia que funcione?"** — y después de cada éxito: **"¿me retiro ahora o arriesgo a mis MoriMonchis para conseguir algo todavía mejor?"**

### 22. LO QUE TODAVÍA DEBE QUEDAR ABIERTO (por decisión explícita del handoff)

Cuánto puede programarse · cuánta autonomía tiene cada MoriMonchi · duración exacta de los turnos · reglas exactas de movimiento · cantidad de acciones por MoriMonchi · funcionamiento definitivo de las plantillas · interacción exacta con el entorno · cuánto daño es inevitable · cantidad de información visible · funcionamiento preciso de Cutie Marks · reglas de breeding · sistema exacto de muerte/retiro.

**El núcleo que sí quedó definido:**

> **Preparar → predecir → ejecutar automáticamente → descubrir si tu modelo era correcto → decidir cuánto arriesgar → extraer → volver a la tienda y construir nuevas posibilidades.**

---

# PARTE 2 — Lo que Juan cerró en S76 (fuente de verdad)

Respuestas a la ronda de preguntas de la sesión de evaluación:

| # | Pregunta | Decisión de Juan |
|---|---|---|
| **1** | ¿Qué hacen cuerno/espalda/alas? | ⭐ **Genes = catálogo de habilidades.** Al preparar sus ataques-plantilla, según el **tipo** de parte el MoriMochi tiene **distintas habilidades disponibles para coreografiar**. → Criar es criar movesets. **Este es el eslabón nuevo de integración crianza↔combate** (reemplaza a "genes=conectores"). |
| **2** | ¿Frontera de control: espacio u órdenes? | ⭐ **Las dos cosas: plantillas sobre una cuadrícula + secuencia programada.** El jugador coloca plantillas sobre un escenario tipo grilla **y programa el orden de ejecución**: *"habilidad 1 de MoriMonchi 1, después habilidad 3 de M3, y así sucesivamente"*. La habilidad del jugador es **geometría + secuenciación**. |
| **3** | ¿Determinismo del encuentro? | **Diferido — explorar después.** Intención declarada: los **escenarios también interactúan y varían un poco cómo funcionan los enemigos** (ej. que reboten a otro lado). ⚠️ El filtro de la nota 17 (fracaso ≠ RNG) sigue siendo el criterio duro; el corte exacto queda pendiente. |
| **4** | ¿Dónde quedó el PvP? | ⭐ **PvP DESCARTADO** *"después de mucho pensarlo"*. La competencia será **indirecta: leaderboards**, y un **mercado** (implementación futura). Esas serán las interacciones sociales del juego por el momento. → El snapshot+mailing de §1.6 de la nota 18 queda retirado. |
| **5** | ¿Muerte o retiro? | ⭐ **Muerte permanente confirmada**: *"ciertos eventos llevan a eso"*. Los muertos **quedan como legacy** para un contenido futuro adicional que **todavía no existe** (no diseñar aún). El retiro (§17 del handoff) convive con la muerte. |

---

# PARTE 3 — Lectura del orquestador (S76)

## 3.1 · Qué pasa los filtros de la nota 17

- **Criterio de la hipótesis**: ✅ es literalmente el §8 del handoff (hipótesis → PLAY → confirmación).
- **Confirmación > suspenso**: ✅ el commit es la fase de planificación; la resolución es verificación.
- **Fracaso ≠ dado**: ✅ declarado como identidad (§2). ⚠️ Pendiente de blindar con la decisión de determinismo (Parte 2, #3).
- **Sin estado oculto**: ✅ **con partición nueva**: lo oculto vive **entre** encuentros (qué viene, en qué combinación); el encuentro a la vista se lee entero. Es la reconciliación acordada en sesión.
- **Texto plano**: el núcleo pasa (*"preparas, aprietas PLAY y descubres si tu plan era correcto"*). Las mecánicas concretas se testearán una a una cuando existan.

## 3.2 · Estado del código respecto a este rumbo (post-S75)

- **La demolición de S75 sigue 100% válida**: las bases (HornPart/BackPart/WingPart/FacePart + DatabaseSO, `HeldItemId`, monedas en PlayerInventorySO) son genéricas y sirven tal cual. **S94:** CutieMarkSO eliminado. Nada de lo creado estaba atado al tablero de desvíos.
- La decisión "genes = catálogo de habilidades" implica que las partes mecánicas terminarán **referenciando habilidades** (forma exacta sin diseñar — no adelantar).
- ⚠️ Los triggers del ítem (`ItemTriggerKind { None, LowHealth, Collision, Collected }`) se definieron pensando en el tablero. Probablemente sirvan (extracción recolecta, hay colisiones/desplazamientos), pero **revisar cuando el combate tenga forma**.
- El pendiente obligatorio de editor de S75 (assets nuevos + rewiring + limpieza de escena) **no cambia** con este rumbo.

## 3.3 · Abiertos para la ronda 3 (próxima sesión de diseño)

> ⚠️ **Actualización S77**: la Parte 4 cerró o encaminó varios de estos — el **1** quedó parcialmente cerrado (3 MoriMonchis por expedición, entran al tablero vía su primer despliegue, isla-cuadrícula estilo Bad North; tamaño sin número), el **2** parcialmente (habilidades = las 3 partes en la card; economía sin cerrar → Q5 de §4.4), el **7** cerrado (Cutie Marks = modificadores de coreografía). La herida persistente entre niveles también quedó confirmada. **La ronda 4 vive en §4.4.**

Ordenados por cuánto bloquean el documento de mecánica:

1. **¿Qué es espacialmente un encuentro?** Tamaño de la cuadrícula, cuántos MoriMonchis entran, cómo se mueven durante la resolución (la parte "autobattle" del movimiento es autónoma — ¿con qué reglas?).
2. **Economía de acciones**: ¿cuántas habilidades por MoriMochi por encuentro? ¿La secuencia programada es de N pasos fijos? ¿Qué pasa cuando la secuencia se agota?
3. **El corte del determinismo** (Parte 2, #3): propuesta a discutir — dentro del encuentro, mismo setup → mismo resultado; los escenarios varían **reglas visibles**, no tiradas.
4. **Estructura expedición ↔ ciclo día/noche**: ¿la expedición de 10–20 min vive en el bloque 23:00–6:00? ¿Cuánto dura un día en minutos reales? (pregunta #2 de la Parte 6 de la nota 18, sigue sin número).
5. **¿Qué eventos matan?** (permadeath confirmado, disparadores sin definir — y cómo se comunica el riesgo antes de aceptar seguir).
6. **Leaderboard**: ¿qué métrica rankea (profundidad, botín, eficiencia)? — conecta con el multiplicador/puntaje que quedó abierto en la 18.
7. **Cutie Marks en este sistema**: seguían definidas como "modificadores de comportamiento, no de stats" — ¿modifican la coreografía (habilidades extra, reacciones automáticas)?
8. La lista completa del §22 del handoff sigue **deliberadamente abierta** — no cerrarla de a pedazos sin Juan.

**El entregable que sigue faltando es el mismo de siempre: el documento de mecánica en limpio** (reglas de movimiento, resolución, secuencia, daño). Recién con eso se planea implementación.

---

# PARTE 4 — La visualización de Juan (S77, 2026-08-12 — fuente de verdad)

> Braindump entregado por Juan al abrir la ronda 3. Es la descripción más concreta que existe del combate: el flujo de punta a punta como él lo ve. Deliberadamente **sin números todavía** (vidas, daño, N de contadores, tamaño de grilla) — Juan pidió asentar primero la idea general.

## 4.1 · El flujo completo

1. **Marco**: llega la tarde → se hace de noche → **la tienda cierra** → se sale de aventura **por la parte trasera de la tienda**.
2. **Roster**: se eligen **3 MoriMonchis** que van con vos.
3. **Preview**: en el **celular** se ve **por simbología** cuáles son las etapas a enfrentar antes de llegar a la **salida / punto de extracción**.
4. **El tablero**: al aventurarse, se visualiza una cuadrícula **estilo isla — referencia: Bad North**, pero con las cuadrículas **más marcadas** (estilo tuning). Se ven **algunos enemigos**. **Aún no pasa nada: hay que desplegar la coreografía.**
5. **Las cards**: en la parte inferior, **3 cards** de tus MoriMonchis. Card = **imagen del MoriMonchi + las 3 partes que dan habilidades**. *(La card ES el genoma legible.)*
6. **Enemigos reactivos**: se sabe que **se mueven en reacción a cómo interactúes con ellos** — por eso el planeamiento. Cada enemigo tiene **N interacciones** que se hacen con él antes de ejecutar su acción. **Imaginarlos como bombas**: las golpeás N veces, el **siguiente** que la golpee **la detona** (ej.: al ser atacada, en su siguiente turno ataca a todos alrededor). ⭐ **La bomba es el primer enemigo para testear.**
7. **⭐ El despliegue define la posición**: el **primer despliegue es gratis** — podés tirar la plantilla desde donde quieras, y **tu MoriMonchi se queda en el lugar donde tiró la plantilla**. → **Las plantillas y ataques TAMBIÉN definen la posición de tus MoriMonchis sobre el tablero.**
8. **Controles descritos**: `F1/F2/F3` selecciona MoriMonchi · `1/2/3` selecciona su habilidad · `WASD` mueve el cursor sobre el tablero (se van mostrando las plantillas) · `Q/E` rota la plantilla 90° · `Enter`/click confirma la posición.
9. **Cada habilidad es un sistema de plantillas + efectos**: algunas son **de ataque**, otras sirven **para recolectar**, y **las de las alas son meramente movimientos**.
10. **Ejemplo de coreografía (de Juan, verbatim en estructura)**: *despliego MM A → golpeo → golpeo con el segundo → sé que le queda un tick a la bomba para explotar → me alejo → voy a las zonas de recolección → aplico la habilidad de recolección → listo.*
11. **Cutie Marks = el rol de equipo**: del tipo *"al golpear a tal, repite el ataque"*, *"te desplaza a tal lugar"*, *"lo empuja"*.
12. **Consumibles**: mismo concepto de siempre — se cumple una condición → se usan.
13. **Entre niveles**: al terminar el nivel se pasa al siguiente **con las mismas heridas**, y se continúa nivel a nivel **hasta llegar al punto de extracción**.

## 4.2 · Lectura del orquestador (S77)

- **⭐ La regla que unifica todo — "posición = consecuencia del ataque"**: no existe "mover" como sistema separado; actuar Y posicionarse son la misma acción (reubicarse cuesta una habilidad de alas). Una pieza, dos funciones — la economía de reglas que la nota 17 marcó como faltante (Elegance 4/10). **Resuelve la pregunta madre de la ronda 3 eliminándola**: no era "movimiento autónomo vs. programado" — el movimiento propio no existe.
- **El enemigo-contador es la versión más legible posible de "predicción"**: no se simula IA en la cabeza, **se cuenta**. Texto plano: *"cada enemigo aguanta N toques; el siguiente lo activa"* ✅. E invierte la presión: el peligro no viene de que el enemigo actúe — **viene de que VOS lo activás al interactuar**. Tu propio plan es el reloj de la bomba.
- **La card-genoma cierra crianza↔combate sin reglas extra**: criar es literalmente criar la card.
- **Técnicamente más barato que el handoff original**: sin pathfinding, sin IA autónoma, determinista, por eventos discretos. El simulador headless es trivial y el solver sale casi gratis.
- **⚠️ El autobattle se evaporó (decidir con ojos abiertos)**: el handoff decía *plan → PLAY → observar* (§2, §8); el flujo descrito es **acción-por-acción con lectura de estado entre medio** — un táctico por turnos tipo puzzle. Al orquestador le parece MEJOR, pero es un cambio de identidad que Juan debe elegir conscientemente (→ Q1).
- **⚠️ Falta la fuente de presión**: si los enemigos solo reaccionan al toque, ignorarlos es siempre seguro (recolecto, esquivo, salgo) — y sin riesgo no hay push-your-luck (→ Q3, **la más importante de la ronda 4**).
- **El riesgo de contenido (PE.1) sigue intacto**, aunque este modelo lo abarata: un enemigo = contador + reacción + forma, no una IA.

## 4.3 · Qué cerró esta parte respecto a rondas anteriores

| Qué | Estado |
|---|---|
| Pregunta madre ronda 3 (autonomía vs. coreografía) | ✅ Eliminada: posición = consecuencia de la plantilla; sin movimiento autónomo propio |
| Abierto 1 (espacio del encuentro) | 🟡 Parcial: 3 MM por expedición · entran vía primer despliegue gratis · isla-cuadrícula estilo Bad North · tamaño sin número |
| Abierto 2 (economía de acciones) | 🟡 Parcial: habilidades = las 3 partes en la card · límite de acciones sin definir (→ Q5) |
| Abierto 7 (Cutie Marks) | ✅ Modificadores de coreografía ("al golpear: repite / desplaza / empuja") |
| D2 ronda 2 (¿curación entre encuentros?) | ✅ Heridas persistentes nivel a nivel hasta la extracción |
| Ítems (ItemTriggerKind de S75) | ✅ Concepto confirmado: condición → uso → desaparece |

## 4.4 · RONDA 4 — las preguntas guardadas (abrir la próxima sesión por acá)

> ⚠️ **Actualización S80**: la ronda 4 quedó mayormente CERRADA por el draft MVP de Juan + decisiones en sesión — ver ~~Index/20 - Combat Prototype MVP (Plan)~~ (borrada S93) §2. **Cerradas**: Q1 (lote), Q2 (preview total), Q3 (enemigos con iniciativa, atacan al fin de la coreografía, reactivos en movimiento), Q5 (1 uso por plantilla), C4 (vida en ticks). **Siguen abiertas**: Q4 (fin de nivel — el MVP usa "matar todo" como placeholder), Q6 (mapeo parte→verbo — los kits del MVP son proto-genes sin partes), PE.1 (presupuesto de contenido).

Juan pidió explícitamente guardarlas para contestarlas en detalle al retomar. Por orden de bloqueo:

1. **Q1 · ¿Lote o paso a paso?** *(la de identidad)* ¿Se arma TODA la coreografía y se da PLAY (resolución en cadena, mirás), o cada acción se ejecuta al confirmarla y ves la reacción antes de decidir la siguiente? El ejemplo de Juan suena a paso-a-paso; el handoff (§2, §8) dice lote. Ambas válidas — cambian qué juego es.
2. **Q2 · ¿Preview o memoria?** Al apuntar una plantilla sobre un enemigo con el contador lleno, ¿el juego muestra qué va a pasar (flechitas estilo Into the Breach) o hay que saberlo de memoria por haber aprendido al enemigo? Decide dónde vive la maestría del §4.
3. **Q3 · ¿De dónde viene el daño?** *(la más importante)* ¿Los enemigos actúan alguna vez por su cuenta (cada X acciones tuyas, al entrar a su zona, por nivel) o solo al llenarse el contador? ¿Qué obliga a interactuar con ellos en vez de ignorarlos? Sin esto no hay riesgo; sin riesgo no hay extracción.
4. **Q4 · ¿Qué termina un nivel?** ¿Matar todo, llegar a la salida, cuota de recolección, o "la salida está abierta desde el principio y el resto es opcional" (la más extraction)?
5. **Q5 · ¿Qué limita las acciones?** ¿Usos por habilidad por nivel, presupuesto por monchi, o ilimitado con el daño como único costo? (Enlaza con Q3: con presión real puede no hacer falta límite artificial.)
6. **Q6 · Mapeo parte→verbo**: ¿cuerno = ataque, espalda = recolección, alas = movimiento? ¿Cada parte da UNA habilidad (card de 3) o el tipo de parte elige entre varias?
7. **C4 (arrastre ronda 3) · ¿Vida en hits?** Este modelo la pide: con contadores enteros en enemigos, la vida propia en enteros chicos es el mismo idioma. Recomendación del orquestador: sí.
8. **PE.1 (arrastre) · Presupuesto de contenido**: ¿con cuántos enemigos / escenarios / habilidades esto es jugable y demostrable? El número decide si el diseño se recorta o se expande, y es el presupuesto del documento de mecánica.

Los números finos (grilla, N de contadores, daño) vienen después de estas ocho.

---

## Estado

**DRAFT como visión macro · mecánica del encuentro DECIDIDA en S80.** Este documento reemplaza a las Partes 7–8 de la nota 18 como dirección del combate y sigue siendo la fuente de la visión macro (expedición, extracción, roster, breeding↔combate, preview de mapa). La ronda 4 (§4.4) quedó mayormente cerrada en S80; **el "documento de mecánica en limpio" fue reemplazado por el plan del prototipo: ~~Index/20 - Combat Prototype MVP (Plan)~~ (borrada S93)**, que prevalece donde contradiga. El gate "nada baja a código" se levantó SOLO para el prototipo aislado; la integración con el juego sigue congelada hasta que el MVP valide. Abiertas para diseño: Q4, Q6, PE.1 y el corte del determinismo de escenarios (Parte 2 #3).
