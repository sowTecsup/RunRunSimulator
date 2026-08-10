---
tags: [index, design, draft, rediseno]
---

# 18 - Pilares del Rediseño (DRAFT)

> **Sesión 73 (2026-08-09).** Juan entregó la base de su manuscrito: ciclo día/noche, moneda de evolución, genes visibles, ítem único consumible y **Cutie Marks** en reemplazo del panel de equipo.
>
> ⚠️ **ESTADO: DRAFT — LA IDEA NO ESTÁ CERRADA.** Este archivo existe para que la base no se pierda mientras se sigue explorando. **Nada de esto baja a código.** Cuando la idea se cierre, esta nota se promueve a documento de diseño y recién ahí se planea implementación.
>
> **Convención de esta nota:** la **Parte 1** es lo que dijo Juan (fuente de verdad, no interpretar). La **Parte 2 en adelante** es exploración del orquestador — opiniones y propuestas, **ninguna decidida**. ⭐ = idea de Juan.

Relacionado: [[Index/17 - Refundacion del Combate]] · [[Index/16 - Diagnostico por Frentes]] · [[Index/15 - Theorycrafting S71 - Autobattler y Marketing]]

---

## PARTE 1 — La base entregada por Juan (fuente de verdad)

### 1.1 · Moneda nueva de evolución

- Se introduce **una currency nueva**, distinta de Dabloons.
- Su uso: **"pasar o evolucionar"** a los MoriMonchis.
- Sin nombre todavía. Sin fuente definida todavía.

### 1.2 · Ciclo día/noche con reloj

Reloj visible **arriba a la derecha**. El día se parte en **4 bloques con propósito distinto**:

| Horario | Bloque | Qué pasa |
|---|---|---|
| **6:00 – 9:00** | Tiempo libre | "No pasa mucho". Manejar la tienda, manejar equipos. |
| **9:00 – 18:00** | **Modo SIM de tienda** | Vienen los clientes, se les vende. Bastantes mini-interacciones: **clientes pujando · limpiar el piso · separar peleas de MoriMonchis · alimentarlos · breedearlos · actualizar el stock**. |
| **18:00 – 23:00** | Management | Organización de equipos · **recolección de ciertos minerales** · **re-stockear la tienda**. |
| **23:00 – 6:00** | ❓ **LA GRAN INCÓGNITA** | Ver §1.6. |

### 1.3 · Economía de materiales

- Los MoriMonchis **recolectan cosas en las peleas**.
- Ese material tiene **doble salida**: se puede **vender** en la tienda, o usarse para **consumibles** de los MoriMonchis.

### 1.4 · Genética visible — los 6 genes definitivos

Las partes por genética pasan a ser:

| Gen | ¿Afecta gameplay? |
|---|---|
| Tipo de **cuerno** | ✅ **SÍ** |
| Tipo de **espalda** | ✅ **SÍ** |
| Tipo de **alas** | ✅ **SÍ** |
| Tipo de **color** | ❌ solo visual |
| Tipo de **patrón** de pintura del cuerpo | ❌ solo visual |
| Tipo de **rostro** | ❌ solo visual |

- **Rostro**: se crea un **set de tipos de rostro base**. Encima de eso, **todos** comparten un set de **emociones** que cambian durante las interacciones.
- ❓ **ABIERTO — qué hacen exactamente cuerno / espalda / alas en gameplay. Aún no se decidió.**

### 1.5 · Ítems y Cutie Marks

**Ítems:**
- Cantidad que puede llevar un MoriMonchi: **1**.
- **Fabricables**.
- **Consumibles de un solo uso**: *"se come cuando pasa algo y desaparece"* → **el disparo es por evento, no manual**.

**Cutie Marks** (referencia: *My Little Pony*):
- Pegatinas tipo **sticker** que se pegan **en el costado** del MoriMonchi.
- **Máximo 2 por MoriMonchi.**
- **REEMPLAZAN al panel de equipo.** Funcionan como el sistema de ítems (pero permanentes/equipables, no consumibles).
- Objetivo de diseño explícito: **mirando al MoriMonchi ya sabés qué clase de equipo tiene.**
- Idea técnica de Juan: implementarlas **estilo decal** para que parezcan pegadas al cuerpo.
- **Costo: dos tipos de material distintos** —
  1. Material que se consigue **en las aventuras**.
  2. Material que se consigue **pasivamente con dinero o con misiones que se le dan a los MoriMonchis**.

### 1.6 · La incógnita: 23:00 – 6:00

Lo que Juan **sí** tiene claro:

- Se **lleva una selección de MoriMonchis** a hacer una actividad.
- La actividad **debe incluir cierto grado de competitividad contra otro usuario**.
- Mecanismo de emparejamiento: **snapshot** — *"siempre que busques pelea vas a encontrar, porque basta con que un usuario quiera encolarse para guardar su composición"*.
- Después, **sistema de mailing**: si tu composición derrotó a alguien, **ganás cierta cantidad de dinero**.
- Explícito de Juan: **no hablar de tecnicismos todavía**; quedan muchos cabos sueltos; **todo arranca cuando se decida en qué consiste esta actividad**.

---

## PARTE 2 — Lectura del orquestador: qué acomoda esta base

Tres cosas que esta base arregla sin proponérselo, verificadas contra las notas 15/16/17:

1. **Resuelve la tensión "cozy vs estrategia"** que la nota 15 dejó marcada como sin resolver. No era una contradicción: **era un horario**. Día cozy, noche estratégica. Y encaja con el ADN de la criatura (Gremlins): *no los alimentes después de medianoche*.
2. **Es el mecanismo para ejecutar la Recomendación A de la nota 15** (sacar los timers de reloj real del núcleo del loop). Con tiempo de juego propio, breeding/misiones/expediciones se miden en **bloques del día**, no en horas reales.
3. **Cierra el hallazgo 1 de la nota 16** (el fenotipo son 4 cuerpos; brazos/ojos/bocas son stats invisibles). Con los 6 genes de §1.4, **todo gen es visible**. Deja de haber genética fantasma.

---

## PARTE 3 — La estructura que propone el orquestador: 4 bloques = 4 verbos

**Propuesta, no decisión.** El hallazgo: la pregunta *"¿qué actividad se hace de 23:00 a 6:00?"* puede estar mal formulada. Si cada bloque tiene **un verbo distinto**, el día se lee entero:

| Bloque | Verbo | Qué hace el jugador |
|---|---|---|
| 6:00–9:00 | **LEER** | Llega el **correo**: qué pasó anoche, quién ganó, qué trajeron, quién volvió herido. El bloque "donde no pasa mucho" pasa a ser el de **consecuencias**. |
| 9:00–18:00 | **ATENDER** | El sim de tienda en tiempo real. Cozy, manual, ruidoso. |
| 18:00–23:00 | **DECIDIR** | Craftear, equipar Cutie Marks, y sobre todo **asignar la noche de cada MoriMonchi**. Es el **commit**. |
| 23:00–6:00 | **VER** | La noche **se resuelve** con lo que comprometiste. Mirás el replay o te salteás a la mañana. |

**Por qué importa:** la nota 17 concluyó que *en un autobattler la satisfacción viene de la CONFIRMACIÓN, no del suspenso — el momento dramático es el commit*. Esta estructura pone el commit a las 18:00–23:00 y la verificación a las 23:00–6:00. **El día entero se vuelve el pre-combate del autobattler.**

### 3.1 · Las 4 puertas de la noche

Si el verbo de 18:00–23:00 es *decidir*, entonces la decisión concreta es: **cada MoriMonchi hace UNA sola cosa esa noche.**

| Puerta | Qué gana | Qué arriesga |
|---|---|---|
| **Salir de expedición** | Material de aventura (el caro, el de las Cutie Marks) + PvP por snapshot + dinero por correo | Vuelve herido, o no vuelve |
| **Ir a una misión** | Material pasivo (§1.5), Dabloons | Nada: pierde la noche |
| **Soñar / evolucionar** | Se gasta la **moneda nueva**; evoluciona | Nada: pierde la noche |
| **Quedarse de guardia** | Defiende la tienda; su composición **es el tablero que otro jugador ataca** | Que le entren |

**Por qué esto responde la incógnita:** el jugador no "juega" de 23:00 a 6:00 — **comprometió su noche a las 18:00**. Y con 4 puertas, el roster deja de dividirse en "los buenos" y "los otros": todos tienen noche, y elegir la puerta es la decisión de management del juego.

---

## PARTE 4 — Lluvia de ideas: qué puede ser la actividad nocturna

Formato de la nota 17: **qué es** / *qué conducta emerge*. Todas cumplen los 4 requisitos de Juan (selección de MoriMonchis · competitividad vs. otro usuario · snapshot · correo con dinero).

### Las que usan a otros jugadores como contenido

1. **⭐ El Turno de Noche (asedio asimétrico)** — Partís el roster: unos salen, otros **quedan de guardia en tu tienda**. Tu tienda (con sus muebles, su layout, sus MoriMonchis de guardia) **es el mapa que otro jugador asalta esa noche**. Al amanecer, el correo te cuenta si te entraron o los rechazaste.
   *Emerge:* la tienda que decorás de día **es tu build defensiva de noche**; el ataque y la defensa compiten por el mismo roster; leer el layout ajeno es habilidad.
   💡 **Efecto lateral grande:** rescata el sistema de **furniture/building**, que la nota 16 marcó como huérfano y que estaba en la lista de "remoción". Pasa de decoración a mecánica.

2. **La Ronda Nocturna (expedición por nodos)** — Mapa nocturno con semilla. Cada nodo es material, peligro de terreno, o **encuentro contra el snapshot de otro tendero**. Vos elegís cuánto avanzar.
   *Emerge:* push-your-luck. La pregunta de cada noche es *"¿sigo o vuelvo con lo que tengo?"* — y el permadeath deja de ser un dado: **muere el que vos decidiste llevar más lejos**.

3. **La Caravana** — Llevás material por una ruta; otros jugadores dejan **emboscadas** (una composición apostada) en las rutas. Elegís ruta corta y peligrosa o larga y segura.
   *Emerge:* dos roles asincrónicos con el mismo roster; el mapa de rutas se vuelve un metajuego social.

4. **Territorio / vetas en disputa** — Hay N vetas de mineral. Todos mandan escuadras la misma noche. El rendimiento por veta **cae cuanto más gente fue**.
   *Emerge:* juego de mayorías asíncrono; anti-meta automático (si todos van al fuego, el fuego no paga). Muy barato: no simula un mapa, resuelve disputas.

5. **La Bestia de la Semana** — Un enemigo compartido por todo el servidor; el daño de todos se acumula; el reparto es por contribución.
   *Emerge:* comunidad asíncrona sin PvP directo; siempre hay contra quién ir.

### Las que cambian el objetivo (no matar)

6. **El Show Nocturno** — Un programa de TV ochentero. Ganás **entreteniendo** (combos, rarezas, estilo), no matando. Competís contra la performance grabada de otros.
   *Emerge:* builds vistosas sobre builds óptimas. **Convierte el problema declarado ("ver un autobattler es aburrido") en la métrica del modo.** Es el que mejor le paga a los genes cosméticos.

7. **El Rally (carrera contra fantasmas)** — Circuito de obstáculos contra los **fantasmas** de otros jugadores (tipo Trackmania). Nadie pelea.
   *Emerge:* las **alas** y la movilidad se vuelven gameplay real; es el clip compartible que la nota 15 dice que falta; **cero permadeath** en este modo.

8. **⭐ Hordas paralelas** — Mismo escenario con la misma semilla para todos; gana quien sobrevive más o llega más lejos. Leaderboard estilo Balatro.
   *Emerge:* competencia real **sin construir nunca un oponente**.

### Las raras (pero que resuelven agujeros conocidos)

9. **Sonámbulos / la fuga** — A medianoche los MoriMonchis **se escapan solos** (canon gremlin). Cuánto y en qué estado vuelven depende de **cómo los trataste durante el día**: afecto, salud, energía, si separaste sus peleas.
   *Emerge:* **el cuidado por fin paga** — es el hallazgo 3 de la nota 16 (Health/Energy/Affect no los lee nadie) resuelto de raíz. El día alimenta a la noche.

10. **El Sueño (evolución)** — Los que se quedan **sueñan**, y el sueño es donde se gasta la moneda nueva y ocurre la evolución.
    *Emerge:* costo de oportunidad puro — un MoriMonchi que evoluciona **no** trae material esa noche. La progresión compite con la economía por el mismo recurso: las noches.

### Recomendación del orquestador (registrada, NO decidida)

> **Núcleo: (1) El Turno de Noche + (2) La Ronda Nocturna, con (10) El Sueño como tercera puerta.**

Razones, en orden de peso:

- **(1)** es el único que **convierte contenido que ya existe** (tienda + muebles + layout) en el tablero del PvP. Es el "oponente barato" que la nota 16 pedía, y de paso salva el sistema de furniture.
- **(2)** aporta la decisión de riesgo que le devuelve justicia al permadeath: *la noche no mata sola, mata si vos elegiste seguir avanzando*.
- **(10)** es lo que le da un lugar natural a la moneda nueva sin inventar una tienda de evolución.
- **(6)** y **(9)** quedan como candidatos fuertes de segunda ola: el Show es el que mejor le paga a los genes cosméticos, y los Sonámbulos son el arreglo más directo al sumidero del cuidado.

---

## PARTE 5 — Opinión sobre cada pilar (riesgos incluidos)

### 5.1 · Moneda de evolución — ⚠️ riesgo de inflación de economías

Con esto, el juego pasa a tener **cuatro monedas**: Dabloons · moneda de evolución · material de aventura · material pasivo. Es mucho para un juego cuya nota más baja en los lentes de Schell es **Elegancia**.

**Propuesta de separación limpia (1 verbo por moneda):**

| Moneda | De dónde sale | Para qué sirve |
|---|---|---|
| **Dabloons** | La tienda (día) | Operar: stock, muebles, materiales pasivos |
| **Moneda de evolución** | **Ganarle a otros jugadores** (el correo de la noche) | Progresión: evolucionar |
| **Material de aventura** | Expediciones (noche) | Cutie Marks |
| **Material pasivo** | Misiones / compra | Cutie Marks |

El giro que recomiendo: que **el correo del PvP pague moneda de evolución, no dinero**. Así el dinero es 100% del comercio y la progresión es 100% de competir — dos economías que no se pisan, y "el que compite, crece".

**Abierto:** ¿"evolucionar" es la etapa de vida que ya existe (`CreatureLifeStageTableSO`) o una capa nueva encima?

### 5.2 · Ciclo día/noche — ✅ el pilar más fuerte, con **un número crítico sin definir**

Es la mejor decisión estructural del manuscrito: da ritmo, resuelve cozy-vs-estrategia y saca los timers de reloj real del loop.

> ⚠️ **La pregunta más importante de todo el documento: ¿cuánto dura un día de juego en minutos reales?**

Todo cuelga de ese número: cuánto tarda el breeding, cuánto pesa perder una noche, si el jugador ve 3 días o 30 por sesión. La nota 15 identificó que el peor cuadrante del juego era **el ciclo de horas**. Recomendación: **día completo de 20–30 min reales**, con el bloque de tienda ocupando la mayor parte y la noche resolviéndose rápido o salteable.

Riesgo secundario: el bloque 9:00–18:00 es el 37% del día y concentra **6 mini-interacciones distintas**. Es el candidato número uno a explosión de scope. Sugerencia: arrancar con 2 (vender + separar peleas) y sumar de a una.

### 5.3 · Los 6 genes — ✅ correcto, pero hay que darle valor a los 3 cosméticos

Que todo gen sea visible es el arreglo directo al hallazgo de la nota 16.

**El riesgo:** si solo cuerno/espalda/alas afectan gameplay, **criar por color/patrón/rostro no tiene consecuencia** y la mitad de la genética se vuelve decorativa.

**Propuesta de reparto (resuelve además que la valuación económica hoy ignora todo):**

> **Los genes cosméticos pagan de día (la tienda) · los genes mecánicos pagan de noche (la actividad).**

Los clientes pagan por lo lindo y lo raro; la noche premia lo útil. Criar pasa a tener **dos ejes**, y el jugador enfrenta una decisión real: ¿crío para vender o para competir?

**Propuesta para los 3 mecánicos** (cada uno con un verbo distinto, y todos pasan el criterio del texto plano de la nota 17):

| Gen | Qué hace | En una frase |
|---|---|---|
| **Alas** | Movilidad / relación con el terreno | *"El que vuela no pisa el terreno."* |
| **Cuernos** | El golpe: tipo, si rompe defensa, si aplica su elemento | *"El cuerno decide cómo pega."* |
| **Espalda** | Capacidad de carga en la expedición | *"La espalda decide cuánto puede traer."* |

Lo interesante del reparto: **alas y cuernos pagan en el combate, espalda paga en la economía.** Un gen mecánico que no es de pelea evita que "genes útiles" signifique "genes de combate".

### 5.4 · Ítem único consumible — ✅ excelente, con **una condición**

Pasar de N usos a **1 ítem de 1 uso** es exactamente el recorte que pide la nota 17.

⚠️ **La condición:** *"se come cuando pasa algo"* es un disparo por evento, y eso es literalmente la **causa C** del diagnóstico de la nota 17 (*causalidad no local ni inmediata* — la causa queda lejos del efecto).

**Se arregla con un giro chico:** que **el disparador lo elija el jugador antes de salir**, de una lista corta y legible.

> *"Cuando le quede 1 golpe, se come la poción."*

Con eso el ítem deja de ser una sorpresa y pasa a ser **la única línea de programación del jugador** — el "gambit-lite" de la nota 15, y la fuente de la **hipótesis** que la nota 17 exige para que ver la pelea sea satisfactorio.

### 5.5 · Cutie Marks — ⭐ la mejor idea del manuscrito

Tres razones, en orden:

1. **Es legible en sus simientos** (tu propia regla): el equipo se lee **mirando al bicho**, sin abrir nada.
2. **Borra un panel entero** en vez de agregar uno. Menos UI, menos drag & drop, menos deuda.
3. **Le da ficción a la progresión.** Una cutie mark en MLP no se compra: se **gana**, y define quién sos. Eso alimenta el lente donde el juego ya es fuerte (valor endógeno 7/10).

**Recomendación de diseño: que sean permanentes.** Solo 2 slots + permanentes = cada MoriMonchi es una **build comprometida**, no un maniquí. Y si querés otra build, **la criás** — la genética y la progresión empujan hacia el mismo lado. Además le da a la valuación algo real que medir (hoy la tienda ignora todo).

⚠️ **Dos riesgos a tener en el radar (sin resolver hoy):**
- **Técnico:** "decal" sobre malla **skinned** no es directo en URP. La ruta practicable es de **espacio de textura** (capa de sticker en el material/shader del cuerpo), no `DecalProjector`. Es factible — el cuerpo ya tiene shader propio con propiedades de color genético — pero es trabajo de shader, no de prefab. Conviene saberlo antes de prometerlo.
- **Visual:** costado del cuerpo = 1 patrón genético + 2 stickers compitiendo por el mismo espacio. Hay que reservarles zona.

---

## PARTE 6 — Preguntas abiertas (ordenadas por cuánto bloquean)

1. ❓ **¿En qué consiste la actividad nocturna?** — bloquea todo lo demás (Juan dixit).
2. ❓ **¿Cuánto dura un día de juego en minutos reales?** — define el ritmo entero.
3. ❓ **¿La noche mata?** ¿Permadeath de noche, o solo herida persistente y la muerte queda como decisión del jugador que empuja más lejos?
4. ❓ **¿Qué hacen exactamente cuerno / espalda / alas?**
5. ❓ **¿La moneda nueva sale del PvP o de otro lado? ¿"Evolucionar" es la etapa de vida existente o una capa nueva?**
6. ❓ **¿Las Cutie Marks son permanentes o removibles?**
7. ❓ **¿El disparador del ítem lo elige el jugador o es fijo por ítem?**
8. ❓ **¿Qué pasa con el combate 3v3 actual?** — ¿la actividad nocturna lo reemplaza, lo contiene, o conviven? Las decisiones de la nota 17 (vida en hits · terreno · formato) **siguen abiertas y ahora dependen de la nocturna**.

---

---

# PARTE 7 — ⭐ LA MECÁNICA (propuesta de Juan, S73) — "el tablero de desvíos"

> Esto es lo que respondió a la pregunta 1. Ya no es lluvia de ideas: es una mecánica concreta. **Sigue siendo draft**, pero es la primera candidata real.

## 7.1 · Reglas, tal como las planteó Juan

- **Vista cenital (top-down)**, grilla de **5×5**.
- En la grilla hay **ítems, enemigos y obstáculos** — 3D toony simple, legible de un vistazo.
- El jugador tiene un **roster de 5 MoriMonchis** disponibles.
- **Los MoriMonchis se mueven solos, en línea recta y por ticks.** El jugador no los mueve.
- **Cada uno reacciona distinto**: unos solo atacan · otros solo recolectan · otros **alteran las dinámicas del mapa**.
- Como solo avanzan en línea recta, **el trabajo del jugador está en los BORDES del mapa: coloca cambiadores de dirección** que alteran el curso.
- **No se puede desplegar todo a la vez**: hay que programarlo con anticipación.
- **Los cambiadores son PERMANENTES.**
- **El objetivo es aguantar**: cuánto puedes mantenerte en el campo recolectando y recibiendo daño hasta que caes y tus MoriMonchis vuelven a casa.
- **El bucle de satisfacción**: *mirar → analizar → setear → reaccionar → dejar correr*.
- **Al superar 3 niveles se llega a la tienda de otro jugador y se le roba algo** (Juan lo considera etapa posterior; para el arranque le basta con el tablero).
- **Cutie Marks = modificadores de comportamiento**, no de estadísticas. Ejemplo dado por Juan: *"si mata a un enemigo, rebota hacia otro y mira hacia abajo"*.
- **Si dos MoriMonchis chocan, pasan cosas.**

## 7.2 · Por qué esta mecánica sí pasa los filtros de la nota 17

| Filtro | ¿Pasa? |
|---|---|
| **Criterio del texto plano** | ✅ *"Van derecho hasta que algo los desvía."* Una frase, sin tabla. |
| **Criterio de la hipótesis** | ✅ El jugador mira el tablero, predice la trayectoria y la confirma. La hipótesis **es** el juego. |
| **Confirmación > suspenso** | ✅ El drama está en colocar el desvío; correr es verificación. |
| **Sin estado oculto** | ✅ Todo está en el tablero: posición, dirección, obstáculo. |
| **Skill vs. Chance** | ✅ Determinista. No hay dado. |
| **Justicia del permadeath** | ✅ El jugador elige cuánto se queda. Cae porque se quedó, no porque salió un 5%. |

**Consecuencia:** decide sola la palanca abierta de la nota 17 — **la vida en hits** deja de ser discutible, porque en vista cenital hay que leer la salud de 5 unidades de un vistazo.

## 7.3 · Referentes directos (para saquear)

| Referente | Qué comparte | Qué robar |
|---|---|---|
| **ChuChu Rocket!** (Sonic Team, 1999) | Es *literalmente* el núcleo: bichos que caminan derecho en grilla, flechas que los desvían, gatos como amenaza | Su **modo competitivo** (fue su gran acierto) y su economía de flechas |
| **Opus Magnum / Zachtronics** | La máquina que armaste corre sola y verla correr es el premio | La comparación por eficiencia en vez de por victoria |
| **Pinball** | El jugador controla el perímetro, no la bola | La lectura del rebote como habilidad |
| **Lemmings** | No controlas a la unidad: controlas el entorno | Roles fijos por unidad |
| **Loop Hero / Balatro** | El estado se acumula y termina ahogándote | La curva de la partida |

## 7.4 · Las dos decisiones que definen todo lo demás

**(a) ¿Qué significa "permanente"?**
- *Compromiso*: colocación ilimitada pero irreversible → el perímetro se llena y **la partida termina cuando tu propia máquina te encierra**. Arco: libertad → máquina → jaula.
- *Recurso*: cantidad limitada de desvíos → el puzzle es de economía, no de calcificación.

**(b) ¿Hay ventanas de planeamiento o el tiempo corre siempre?**

| Nivel | Cómo funciona | Se parece a |
|---|---|---|
| 0 | Se planea todo antes; después solo se mira | Zachtronics |
| 1 | Corre N ticks → ventana de planeamiento → repite | **Puzzle por rondas** |
| 2 | Pausa libre en cualquier momento, colocación al tick siguiente | Puzzle relajado |
| 3 | Tiempo real, se coloca al vuelo | ChuChu Rocket original (reflejos) |

## 7.5 · La variante estructural que propone el orquestador

> **Que el perímetro viaje entre niveles.** Los interiores cambian (3 tableros distintos), pero **los desvíos que colocaste te acompañan**.

Con eso, los 3 niveles dejan de ser 3 niveles sueltos y se vuelven **una partida con arco**: la máquina que construyes en los tableros 1–3 es la máquina con la que asaltas la tienda del otro jugador al final. El PvP deja de ser un modo aparte y pasa a ser **el examen de la máquina**.

## 7.6 · Extensiones (banco de mecánicas)

**Tipos de cambiador**
- Giro 90° · rebote 180° · retención un tick · acelerador (2 casillas por tick)
- **Compuerta selectiva**: solo pasa quien tenga cierto gen
- **Alternante**: cambia de sentido cada vez que se usa → la máquina gana memoria
- Sentido único · par de teletransporte entre bordes
- Cambiador que coloca un MoriMonchi al pasar, en vez del jugador

**Choque entre dos MoriMonchis**
- Intercambian dirección (billar) · rebotan a 90° cada uno
- **Se pasan la carga** — el recolector le entrega el material al de espalda grande, que es el que puede sacarlo. Convierte el choque en **logística**
- Se disparan mutuamente las Cutie Marks
- Se intercambian una propiedad (rol, elemento)
- Se traban los dos (el resultado que hay que evitar)

**Enemigos**
- Fijo (daña al contacto) · patrulla en línea recta con las mismas reglas que tú
- ⭐ **Enemigo al que TUS cambiadores también desvían** → cada colocación es de doble filo y el puzzle se duplica sin costo
- Enemigo que come ítems
- **Enemigo que rompe cambiadores** → la válvula de escape para la calcificación del perímetro; a veces lo vas a querer vivo

**Los que "alteran las dinámicas del mapa"** (el tercer rol)
- Rota el tablero 90° · inunda una fila o columna con terreno
- Congela a los enemigos N ticks · convierte un obstáculo en ítem
- Invierte el sentido de todos los cambiadores

**Objetivos por tablero** (variedad sin sistemas nuevos)
- Recolectar N antes de caer · aguantar N ticks · romper el obstáculo central
- Sacar a los 5 vivos · llevar a una unidad concreta a una casilla concreta

**Cutie Marks** (modifican trayectoria/comportamiento, nunca números)
- Al matar: rebota hacia otro enemigo · al recolectar: gira 90°
- Al chocar: intercambia rol · al tocar borde: deja un cambiador gratis
- Al recibir daño: invierte el sentido · **inmune a los desvíos** (la comodín)
- Atraviesa un obstáculo una vez

**Los 3 genes mecánicos, ya como propiedades de trayectoria**
- **Alas**: pasa por encima del obstáculo en vez de frenar
- **Cuernos**: destruye el obstáculo que golpea en vez de desviarse
- **Espalda**: cuánto material puede cargar antes de tener que volver

---

## Estado

**DRAFT. Idea abierta, nada decidido, cero código.** La Parte 7 es la candidata en pie; las preguntas 1 y 3 de la Parte 6 quedan resueltas si se confirma.
