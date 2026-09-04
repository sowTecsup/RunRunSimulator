---
tags: [index, design, draft, linaje, bajada]
---

# 22 - Bajada Nocturna y Linaje (DRAFT S96)

> **Sesión 96 (2026-09-02).** Juan jugó el prototipo Dragon RPS (E1-E3, construido en S95) y lo dio por **fallido**. La sesión fue de diseño puro: primero la pregunta *"¿por qué quiero criar otro MoriMochi?"*, después qué enfrentamiento la sirve. Esta nota guarda lo que dijo Juan y lo que propuso el orquestador.
>
> ⚠️ **ESTADO: DRAFT — NADA DE ESTO BAJA A CÓDIGO.** Cuando Juan cierre la idea, esta nota se promueve a documento de diseño (y a Notion) y recién ahí se planea implementación.
>
> **Convención:** la **Parte 1** y la **Parte 2** son lo que dijo Juan (fuente de verdad, no interpretar). De la **Parte 3** en adelante es exploración del orquestador — propuestas, **ninguna decidida**. ⭐ = idea o decisión de Juan.

Relacionado: [[Index/21 - Combate v3 - Dragon RPS]] (el prototipo fallido) · [[Index/18 - Pilares del Rediseno (Draft)]] (genes, Cutie Marks, ciclo día/noche) · [[Index/17 - Refundacion del Combate]] (filtros y formatos) · [[Index/19 - Combate Nuevo - Predictive Tactical Extraction]] (los tres placeres) · [[Index/16 - Diagnostico por Frentes]] (mundo vivo como sumidero) · [[Index/02 - Genetics & Breeding]] · [[Index/06 - Player & World]] · [[Index/10 - Furniture & Building]]

---

## PARTE 1 — El veredicto sobre Dragon RPS ⭐ (fuente de verdad)

Juan jugó el prototipo en la PC del trabajo (loop Tab tienda → Ring → E → Pick → Duel → Result). Textual, resumido:

- *"Muy interesante demo, demuestra lo que queríamos lograr."*
- **Caminos sin salida**: hay estados donde sabés que vas a perder porque no te quedan cartas y la última no sirve.
- **Contar cartas** no pesa al inicio pero gana peso al final y **no es difícil**, así que es fácil llegar a 2 puntos contra 2 puntos.
- **Feel**: no se pudo mover mucho; la micro-animación del triángulo **distraía más que ayudaba**.
- **Experimento fallido**: *"no siento que sea la actividad que necesitamos para que los jugadores puedan probar el breeding."*
- Pedido: ideas **disruptivas y viables**, **sin iterar sobre soluciones previas** (ni RPS, ni 3v3, ni táctico).

**Lectura del orquestador (causa estructural, registrada):** un estado cerrado y chico (6 cartas, 3 golpes, información casi completa) se resuelve en pocas partidas; un juego resuelto produce finales conocidos sin agencia y conteo que decide. El juice no lo arregla porque no era un problema de presentación. Y lo más grave para el objetivo: **la genética se comprimió en tres enteros**; lo que se cría (looks y perks, key concept S93) nunca entraba al duelo. Tres reglas derivadas para cualquier actividad nueva: (1) **el cuerpo entero** de la criatura es lo que se evalúa; (2) nunca puede existir un estado de "ya perdí y todavía tengo que jugar"; (3) la actividad debe existir **sin necesitar un rival humano conectado**.

**Qué queda del prototipo:** el código de `Scripts/DragonRps/` (motor puro + harness) y `Systems/Combat/`, el panel `CombatPanelUITK`, el mueble Ring y el potencial por parte (`Horn/Back/WingPotential`) **siguen en el proyecto**; su destino (conservar como harness/referencia o demoler como en S93) es una pregunta abierta (Parte 7).

---

## PARTE 2 — El marco que fijó Juan ⭐ (fuente de verdad)

### 2.1 · Sobre las cuatro ideas iniciales del orquestador

El orquestador propuso cuatro actividades no-combate (pedido del cliente, juez con gusto oculto, terrario nocturno, "el cuerpo es la llave"). Respuesta de Juan:

- **Pedido del cliente**: le interesa, pero *"tal como está planteada puede convertirse en una tarea tediosa y hacer que el breeding pierda protagonismo"*. No quiere el loop *"crío lo que me piden → se lo vendo al cliente"*.
- **Terrario**: no lo convence como sistema central. Pero quiere **profundizar en corrales, ambientes y comportamientos**: *"que el entorno tenga una relación real con el proceso de crianza y con las características que desarrollan las criaturas."*
- **El cuerpo es la llave** (recorrido asíncrono con física): **descartada**, no la ve viable.
- **Clientes**: *"posiblemente ahí esté una parte importante de la solución."*

### 2.2 · Centro de adopción, no tienda de mascotas ⭐

*"Conceptualmente me incomoda un poco la idea de criar mascotas para venderlas."* Propuesta de Juan: **centro de adopción**. Las criaturas no se venden: los clientes hacen **donativos** para adoptar un MoriMochi. Mantiene la fantasía de negocio/management sin que la relación con las criaturas sea puramente comercial.

### 2.3 · Durabilidad ⭐

Los MoriMonchis **no son eternos**. Pueden desaparecer o dejar de estar disponibles por: **combates · eventos · decisiones del jugador · el paso del tiempo · un número limitado de crianzas**. Esto le da valor a cada uno y evita que la colección sea estática.

### 2.4 · El problema de fondo ⭐

*"Estamos intentando abarcar demasiados géneros simultáneamente: Shop Simulator, Pet/Care Simulator, Breeding Simulator, Combat, Social/Collection."* La pieza que falta es **la razón fundamental para criar**: un motivo para volver a criar una y otra vez sin sentirse repetitivo, que combine **Breeding** (experimentar y obtener MoriMonchis diferentes), **Combat** (probar mis criaturas contra las de otra persona o desafíos) y **Social** (mostrar, intercambiar, adoptar, interactuar). **La tienda conecta esos sistemas, no es el objetivo principal.** *"No quiero simplemente añadir más sistemas."*

**La pregunta a resolver: "¿Por qué quiero criar otro MoriMochi?"**

Preguntas concretas de Juan (respondidas en Parte 3): qué hace que un jugador críe otra vez tras una buena criatura · qué sistemas dan múltiples iteraciones sin grinding · cómo los clientes dan sentido sin "cría bajo pedido" · cómo una característica vale en combate y socialmente · cómo corrales/ambientes influyen en crianza y comportamiento · cómo la durabilidad es fuente de decisiones y no frustración · qué papel tiene la tienda/adopción · cómo cerrar el loop criar → experimentar → combatir/socializar → perder o retirar → volver a criar.

### 2.5 · Sobre el enfrentamiento ⭐

Con el linaje como núcleo, Juan pidió *"un combate que ponga a prueba estas habilidades"*: el MoriMochi ganado desbloquea la prueba de concepto y el propósito de **ganar ítems para criar más y mejorar la tienda y los corrales para mejorar el potencial**. Condiciones:

- Contra **otro usuario**.
- **Nada genérico tipo Pokémon**; **nada de tirar cartas con muchos poderes**.
- **No intrínsecamente pelea**: *"sé que tiene que ser una especie de enfrentamiento, pero no quiero algo donde sea intrínsecamente pelea."*
- **Nada de carreras.**
- De **Another Door** le interesa: *continúo o me retiro* · **visualizar más o menos qué me encontraré en la run** (curas, múltiples enemigos) · **aseguro o pierdo todo** · combinado con **permadeath si se me muere alguno en el camino, similar a Darkest Dungeon**.
- Le interesa el **territorio tipo Splatoon** que tengan que completar; imaginó *"criaturas autómatas que van coloreando y reaccionando a su entorno"*. Después amplió: **no necesariamente autómatas, cualquier mecánica interesante**.
- Pidió lluvias de ideas **con referencias a otros juegos** para visualizar cómo lucirían las pruebas con MoriMonchis.

---

## PARTE 3 — La pieza propuesta: el progreso vive en el linaje (orquestador, NO decidida)

### 3.1 · La regla

Cada MoriMochi tiene dos capas: **lo que nace** (6 genes visibles, potenciales, los dos diales) y **lo que vive** (marcas ganadas por lo que le pasa, temperamento moldeado por el corral, historia). Las **Cutie Marks** de [[Index/18 - Pilares del Rediseno (Draft)]] §1.5/§5.5 son exactamente la segunda capa: máximo 2, permanentes, se ganan.

> **Lo vivido solo sobrevive si se hereda.** Al criar, cada cría hereda como mucho una marca de cada padre; las marcas se desvanecen con las generaciones si nadie las vuelve a ganar. Como el individuo se va (ring, edad, adopción, 4 crías), todo lo ganado tiene vencimiento. **Criar es la única forma de guardarlo.**

Respuesta en una frase: *"Crío otro porque este se va a ir, y lo único que queda es lo que le pase a sus hijos."* Segundo motor (Mewgenics, Wobbledogs): **cada nacimiento es una sorpresa legible**. Conservar + descubrir. Una buena criatura no cierra el loop: ahora tenés algo que perder.

### 3.2 · Qué le da a cada sistema existente

| Sistema | Papel bajo el linaje | Regla en texto plano |
|---|---|---|
| **Corrales y ambientes** | Dirigen el 20% de mutación y moldean los diales con los días; cada marca solo se gana en cierto ambiente | *"Lo que nace se parece al lugar donde se crió."* |
| **Enfrentamiento** | Donde se ganan marcas y material y se gasta durabilidad (consecuencia leída, nunca dado) | *"Ganar da marcas; perder cuesta días o una lesión visible."* |
| **Clientes = adoptantes** | **Nunca piden, reaccionan**: entran con su personalidad (mismas reglas de reacción social de los agentes) y se enamoran o no. Donativo. La criatura sigue existiendo: postales, visitas, una vuelta al corral de cría | *"Un adoptante no trae lista; se enamora de lo que hay."* |
| **Durabilidad** | Cuatro salidas, todas decisiones: una pelea más · última camada · adopción con historia · vejez hasta el pinboard de honorarios | *"Nunca una pérdida por dado; vejez visible con tiempo para actuar."* |
| **Tienda** | Escenario y ritmo (los 4 bloques del reloj), no la meta: donativos → muebles → crianza dirigida → lo que adoptantes y ring juzgan | — |
| **Genes cosméticos** | Pagan con los adoptantes (Index/18 §5.3); los mecánicos en el enfrentamiento; las marcas en los dos lados | — |

### 3.3 · Respuestas a las preguntas de Juan (2.4)

- **Criar otra vez tras una buena criatura**: porque es mortal y sus marcas también; su cuarta cría es una decisión.
- **Sin grinding**: escasez que obliga a curar (2 slots de marca, 4 crías, capacidad de corral, vida finita) + contexto que cambia solo (adoptantes y retadores distintos cada semana).
- **Valor social y en combate a la vez**: la misma marca que asusta en el ring enamora a cierto adoptante.
- **Durabilidad sin frustración**: causa visible siempre · vejez en días del reloj del juego, no tiempo real · una salida que se siente bien (adopción = se muda, no muere).

### 3.4 · Riesgos y cómo cerrarlos

- Herencia de marcas ilegible → catálogo de 10-15 marcas, cada una con un evento que la gana y un efecto enunciable; test del texto plano.
- Adoptantes que se sientan como pedidos → prohibido que pidan; su gusto se descubre por sus caras.
- Vida útil mal calibrada → simulación antes de Unity.
- Marcas como sticker sobre malla skinned → trabajo de shader (ya anotado en Index/18 §5.5).

**Validación propuesta:** reglas del linaje en texto plano (≤ 1 página) + harness de **10 generaciones** con adoptantes y enfrentamiento abstractos. Pregunta que debe responder: *¿después de 10 generaciones el roster sigue cambiando, o el jugador encontró un óptimo y dejó de criar?*

---

## PARTE 4 — La bajada nocturna (estructura propuesta por el orquestador)

Enfrentamiento que sirve al linaje, robado de donde corresponde. Es la propuesta que quedó **en pie al cierre de S96**; las tres anteriores de la misma sesión (4.4) quedan registradas como alternativas.

### 4.1 · La estructura

- **Vista previa** (Another Door, mapa de Slay the Spire): antes de cada puerta ves el tipo de la próxima sala (y a veces la de después). Elegís qué criaturas de tu terna entran.
- **La prueba de la sala**: un objetivo por sala (Parte 5). Resultado parcial paga parcial: no hay salas "perdidas", hay salas mal pagadas.
- **Asegurar o seguir** (Deep Sea Adventure): el botín viaja con las criaturas; retirarse por una puerta segura lo guarda, seguir lo apuesta entero. Cada sala más honda paga más y es más dura. Sala rara de **asegurar a mitad de camino** convierte el todo-o-nada en una escalera de apuestas.
- **El nervio** (Darkest Dungeon): cada criatura tiene un nervio que baja en salas oscuras, derrumbes o ferales y sube en charcos. Un atrevido lo pierde despacio, un tímido rápido. **Con nervio en cero la criatura se pierde en la caverna.** No muere de un golpe: se pierde.
- **La marea**: la caverna borra el progreso desde los bordes; presión sin reloj artificial; más rápida cuanto más hondo.
- **Marcas ganadas abajo**: "cubrió una sala sola", "cruzó el derrumbe", "rescató a un feral"; cada una habilita un verbo pequeño y se hereda (Parte 3).
- **Mazo por profundidad**: tres profundidades, charco antes del final, 5-7 salas por bajada.

### 4.2 · Los rivales son los perdidos de otros jugadores ⭐ (propuesta clave)

La sala con "múltiples enemigos" no tiene enemigos que pegan: tiene **ferales**, MoriMonchis que otros jugadores perdieron en su bajada, vueltos salvajes, actuando con el color y el temperamento de su antiguo linaje. Son **snapshots reales** (DNA string + marcas + diales) pilotados por sus reglas.

- La confrontación es **superarlos en la prueba**, nunca golpearlos. Si ganás, el feral **se rinde y podés rescatarlo**: vuelve a su dueño con una postal, o se queda contigo como adoptado. Perder una criatura deja de ser un agujero negro: **otro jugador puede encontrarla**.
- **Asíncrono gratis**: caverna del día con la misma semilla para todos; ferales como snapshots en Cloud Save (mismo patrón que el matchmaker + seed + buzón viejo, ver memoria `project_async_combat`); nadie necesita estar conectado a la vez.
- **Durabilidad con causas visibles**: nervio en cero por decidir seguir, recurso agotado por elegir mal la terna, silbato tardío.
- **El viejo linaje** (jefes de Pikmin 2 / Darkest Dungeon): un feral de muchas generaciones perdidas como final de la bajada; rescatarlo da una criatura con marcas raras para heredar.

### 4.3 · Cómo se mueven si son autómatas

Tres reglas legibles + verbos por gen + un silbato:

| Qué | Regla |
|---|---|
| Base | *Va al objetivo pendiente más cercano/grande.* |
| Atrevimiento | *Decide cuánto se acerca al peligro y a lo ajeno.* |
| Sociabilidad | *Decide si se separa del grupo (manada = denso y rápido en poco espacio; solitario = cubre mucho y se expone).* |
| Alas | *Cruzan huecos y lo ajeno sin frenarse.* |
| Cuernos | *Rompen paredes y deshacen lo ajeno más rápido.* |
| Espalda | *Es el tanque: cuánto carga/pinta antes de recargar.* |
| Silbato | Una **bandera** ("hagan eso ahí") o la **retirada** (salva a la criatura, pierde la sala). La sociabilidad decide si obedecen. |

**Regla de balance obligatoria: cada temperamento tiene que ganar en alguna sala.** Si el atrevido gana siempre, nadie cría tímidos.

### 4.4 · Alternativas de enfrentamiento propuestas antes en la misma sesión (registradas, superadas por la bajada)

1. **El adiestrador al borde del ring**: no peleás, gritás tres órdenes (¡Ahora!, ¡Atrás!, ¡Aguanta!); la criatura obedece según sociabilidad y actúa sola según atrevimiento; las marcas son trucos. Defensor = snapshot; atacante en vivo; semilla + log de gritos para reproducir en Cloud Code. Mecánica central = obediencia, no daño.
2. **El ring como trampa**: sin input en vivo; ves entero al rival y colocás 2-3 señuelos (comida, espejo, ruido, charco) que lo manipulan según su carácter; PLAY y mirar. Determinista por semilla, corre en servidor. Riesgo: puzzle que se resuelve.
3. **La doble visita**: formato encima de 1 o 2: cada pelea se juega en tu corral y en el del rival con los muebles reales; el snapshot incluye la grilla que ya se persiste.

Estas tres siguen siendo peleas; Juan pidió *"no intrínsecamente pelea"* y por eso la bajada las reemplaza. El adiestrador sobrevive como el **silbato** de 4.3, y los señuelos como una sala (5.3).

---

## PARTE 5 — Catálogo de pruebas de sala (tres lluvias de ideas, con referencias)

Formato: **Nombre**, referente → cómo se ve con MoriMonchis → qué prueba.

### 5.1 · Familia territorio (Splatoon como modos) — el color genético es la tinta

- **Sala abierta**, Turf War → tres estelas de color por una cueva ancha; gana quien más cubre antes de la marea → espalda, sociabilidad.
- **Sala de zonas**, Splat Zones → solo cuentan 2-3 círculos; atrevidos al lejano, tímidos al cercano → la bandera vale más.
- **Sala de repisas**, puentes de Pikmin / cornisas de Zelda → superficie en alto vale doble y solo llegan las alas → precio de criar alas.
- **Sala de tabiques**, Bomberman → paredes que rompen los cuernos; abrir también expone → cuernos, decisión.
- **Sala de mezcla**, de Blob → dos tintas se cruzan y se vuelven gris neutro → planificación de la terna.
- **Sala de marea**, Frogger → el agua borra desde un lado; pintar delante y salir por la puerta opuesta → tensión por paisaje.
- **Sala oscura**, antorcha de Darkest Dungeon / Spelunky → solo se ve la tinta propia; nervio baja doble → donde más se pierden; silbato de retirada.
- **Sala de derrumbe**, telegrafiado de Into the Breach → rocas caen en celdas marcadas 3 s antes; los atrevidos siguen bajo la marca → única sala con daño, leído.
- **Cuello de botella**, Lemmings → pasillo de a uno; sociables hacen fila; cuernos lo ensanchan → composición.
- **El feral**, 1v1 de Splatoon / depredadores de Rain World → un perdido pinta su color viejo; si lo superás se rinde y lo rescatás → persecución de manchas, no pelea.
- **La manada feral**, Salmon Run → oleadas de ferales mientras juntás huevos de material y los depositás → espalda, nervio, botín grande.
- **El eco**, espejo de Another Door → sala compartida con la partida grabada de otro jugador con la misma semilla; quien cubre más se lleva el extra, el otro no pierde → competencia sin castigo.
- **El viejo linaje**, jefes de Pikmin 2 / DD → feral enorme que pinta a chorros; hace falta la terna → final de bajada.
- **El charco**, fogata de DD / descanso de Slay the Spire → recupera nervio y pigmento, la marea avanza atrás → descansar cuesta.
- **Sala de asegurar**, Deep Sea Adventure → repisa que vuelve sola a la superficie con el botín.
- **Sala "?"**, eventos de Slay the Spire → huevo salvaje, mural que enseña una marca, charco envenenado.
- **Sala del nido**, tesoro de Spelunky → mucho material, atrae ferales, marea rápida → donde "sigo o me retiro" duele.
- **Primer corte sugerido (si se elige esta familia)**: abierta, zonas, repisas, oscura, feral, charco.

### 5.2 · Familias de prueba con autómatas fuera del territorio

**Cuerpo (fuerza, carga, alcance)**
- **La carga**, Pikmin → arrastrar una geoda entre todos → espalda = fuerza, sociabilidad = empujar juntos.
- **El empuje**, Rock of Ages / sumo → roca que tapa la salida y ferales empujan del otro lado → cuernos + espalda; lo más cerca de físico sin golpes.
- **La torre**, Tricky Towers / tótem → se apilan para llegar a una repisa; un ala arriba dobla la altura → sociabilidad; pirámide tambaleante.
- **La excavación**, Dig Dug / SteamWorld Dig → cuernos abren túnel, material incrustado, cavar de más derrumba → cuernos + apuesta.
- **El puente vivo**, Pikmin / Snipperclips → cadena de sociables sobre un abismo, alas pasan solas; si se rompe por nervio uno cae.

**Carácter (sigilo, nervio, ingenio)**
- **No despertar al gigante**, Untitled Goose Game a la inversa / Mark of the Ninja → juntar botín alrededor de un feral dormido sin ruido; cuernos pisan fuerte, alas planean en silencio → **gana el tímido**.
- **La persecución**, fantasma de Pac-Man / Alien Isolation → un feral rápido caza; alas escapan, tímidos se congelan, sociables se protegen → el silbato es la jugada.
- **El laberinto**, Zelda / Pac-Man → botín disperso y salida escondida; solitarios exploran ramas distintas → **gana el solitario**.
- **El espejismo**, espejos de Ooblets / feria → MoriMonchis falsos; sociables se distraen saludando → cómico, prueba diales.
- **El eco (rugido)**, Patapon / ocarina de Zelda → puerta que se abre con suficiente rugido; cuernos amplifican, tímidos se suman solo en manada → coro de dragones.

**Social (el otro no se vence, se convence)**
- **El cortejo**, Ooblets / pájaro pergolero → un feral se une si le gustan tus colores, patrón y marcas → genes cosméticos ganan una criatura entera; danza de exhibición.
- **El consuelo**, campamento de DD → un compañero entra en pánico; un sociable lo calma; si nadie, se pierde → afecto entre las propias, que hoy nadie lee (Index/16 §5).
- **El trueque**, mercader de Spelunky / Slay the Spire → material o huevo a cambio de ítem, botín o quedarse una sala más; **nunca una criatura**.

**Entorno (el paisaje es el rival)**
- **El frío**, Don't Starve / Frostpunk en chico → nervio baja salvo apiñarse o pelaje denso → **el gen de patrón de pelaje pasa a ser mecánico**.
- **El cruce**, Frogger / Crossy Road → géiseres periódicos; el timing sale del atrevimiento; la bandera marca cuándo ir.
- **El sube y baja**, Tower Control / plataformas de Donkey Kong → subirse juntos en el momento justo → sociabilidad + timing.
- **La cosecha**, Stardew / flores de Pikmin → musgo luminoso antes de que un feral lo coma → botín tranquilo, cambio de ritmo.
- **Primer corte sugerido (si se elige esta familia)**: la carga, no despertar al gigante, el laberinto, el frío, el cortejo, el charco.

### 5.3 · Familias por modelo de entrada (no necesariamente autómatas)

**Vos controlás a la criatura**
- **Plataformas**, Kirby / Yoshi's Island → alas planean como Kirby, cuernos embisten como el spin dash, espalda aguanta caídas; los otros dos siguen como los bebés de Yoshi → cada cuerpo se siente distinto en las manos.
- **El deporte**, Windjammers / Lethal League → geoda que rebota y un arco custodiado por ferales; cuernos golpean, alas interceptan, espalda bloquea → único enfrentamiento directo sin lastimar; da clips.
- **La cadena**, Snake / Pikuniku → controlás la cabeza, los demás siguen por sociabilidad; recolectás musgo; el solitario se suelta.

**Vos trazás y ellos siguen**
- **El pincel**, Kirby Canvas Curse / Yoshi Touch & Go → dibujás el camino; alas saltan huecos del trazo, cuernos rompen lo que cruza, espalda = tinta disponible; el atrevido se sale si ve botín → autómata y control a la vez; encaja con el pigmento genético.
- **La fila**, Lemmings → marcás 3 puntos de acción (cavar, saltar, frenar) y los soltás; lo ejecuta el primero que llega.

**Vos lanzás y la física decide**
- **El campo de golf**, Kirby's Dream Course / Golf Story → fuerza y ángulo por sala con pendientes; espalda pesa, alas planean, cuernos rebotan; 3 tiros.
- **El plinko**, Peggle / pachinko → sala vertical con clavijas de material; el cuerpo cambia el rebote → azar leído, sala de descanso.
- **El lanzamiento**, Pikmin / Angry Birds → sos el tendero al borde del pozo y los tirás a las repisas; distancia por espalda, vuelo por alas.

**Vos programás y después mirás**
- **Las tres órdenes**, Lightbot / RoboRally / Opus Magnum → 3 instrucciones por criatura de una lista de 6 (avanzá, girá, rompé, esperá, seguí al de adelante, volvé); la sala verifica; desobediencia determinista por carácter → placer de planear + ver funcionar (Index/19 §20).
- **Los señuelos**, Into the Breach / Hitman → 2-3 cebos que manipulan ferales según carácter conocido (versión chica de 4.4.2).

**Vos deducís**
- **El suelo minado**, Buscaminas / Hexcells → trampas ocultas; las tímidas sienten el peligro y lo marcan, las atrevidas no; vos deducís la ruta → **el tímido es el mejor** (nervio como detector).
- **La foto**, Pokémon Snap / Bugsnax → observar ferales e identificar linaje por color, patrón y rostro; acertar dice a cuál se rescata y quién es su dueño → genes cosméticos como información; puro social.

**Vos elegís en secreto**
- **Repartir o quedarse**, Another Door / Split or Steal → al final de una sala compartida con el fantasma de otro jugador, ambos eligen en secreto; **tu criatura tiene un "tell"**: un atrevido no disimula, un tímido sí.
- **La subasta ciega**, Modern Art / For Sale → huevo salvaje subastado entre los que bajaron esa semana; oferta de material a ciegas → asíncrono puro, produce criaturas nuevas.

**Vos cuidás bajo presión**
- **La sala de pánico**, Overcooked / Tamagotchi → todos se asustan a la vez; atenderlos en orden con la caricia hold-E que ya existe; sociables se calman entre sí.
- **La cuerda**, Heave Ho / Chained Together → dos criaturas atadas cruzan un abismo; vos controlás la tensión; sociabilidad = coordinación, espalda = peso muerto, alas = cruza primero.

**Principio de mezcla** (WarioWare / Mario Party): cambiar el modelo de entrada sala a sala evita el cansancio, pero cada modelo cuesta un controlador; **no más de tres modelos en el primer corte**. Terna sugerida: **el pincel** (sala base, une trazo y pigmento), **las tres órdenes** (planificación), **el suelo minado** (deducción, gana el tímido).

### 5.4 · Dónde gana cada temperamento (tabla de balance)

| Temperamento | Salas donde es el mejor |
|---|---|
| Tímido | No despertar al gigante · El suelo minado · Sala de zonas (círculo cercano) |
| Atrevido | Sala abierta / disputa · El cruce (temprano) · El eco (rugido) · Persecución (no se congela) |
| Sociable | La carga · La torre · El puente vivo · El consuelo · El frío (apiñarse) · El sube y baja |
| Solitario | El laberinto · La cosecha · La cadena (se suelta) · Cuello de botella |

---

## PARTE 6 — Qué medir antes de abrir Unity (propuesta)

Mismo camino que funcionó una vez (harness de `DragonRps/`): **motor en C# puro, tick fijo, sin UnityEngine**, sobre grilla; miles de bajadas simuladas con agentes de reglas. Tres números que deben salir antes de dibujar nada:

1. Tres temperamentos distintos producen **tres patrones reconocibles a simple vista** (mapas de cobertura / rutas).
2. La terna de cuerpos **cambia qué salas conviene entrar** (no hay terna dominante).
3. La tasa de perdidos por bajada queda entre **5% y 15%**: el nervio importa sin ser cruel.

Más el harness del linaje (3.4): 10 generaciones sin óptimo estable.

**Infra**: snapshots (DNA string + marcas + diales) en Cloud Save; semilla diaria; log de inputs por si algún día hace falta verificar en Cloud Code; visualización por los agentes existentes (`MoriMochiAgent` y colaboradores, `MonchiVisualizer`); salas sobre la grilla de muebles / ProBuilder.

---

## PARTE 7 — Preguntas abiertas al cierre de S96

- [ ] ⭐ **¿Juan adopta el linaje (Parte 3) como núcleo?** Es la premisa de todo lo demás.
- [ ] ⭐ **¿La bajada nocturna (Parte 4) es el enfrentamiento?** ¿Qué familia de salas (5.1 / 5.2 / 5.3) y qué terna para el primer corte?
- [ ] ¿La bajada ocupa el bloque **23:00-6:00** que Index/18 §1.6 dejó como incógnita?
- [ ] Destino del código Dragon RPS (`Scripts/DragonRps/`, `Systems/Combat/`, `UI/Combat*`, Ring, `CombatTuning.asset`): conservar como harness/referencia o demoler como en S93.
- [ ] ¿Cuántas criaturas bajan (terna = 3 es supuesto del orquestador)?
- [ ] Regla exacta del **nervio**: qué lo baja, cuánto, y qué pasa con la criatura perdida (¿feral inmediato? ¿ventana de rescate?).
- [ ] **Adopción entre jugadores reales** (P2P de la Etapa 3.2 del GDD) como donativo con historia: ¿entra en v1?
- [ ] Color de tinta vs patrón de pelaje: ¿el patrón dibuja la estela?
- [ ] Notion: pendiente de que Juan cierre; cuando lo haga, `notion-documenter` con autorización explícita (regla del CLAUDE.md).

---

## PARTE 8 — S97: la mecánica de Juan, el plan por etapas y el plan de realismo

> **Sesión 97 (2026-09-03).** Continuación del theorycrafting. Misma convención: ⭐ = Juan (fuente de verdad, no interpretar); el resto es propuesta del orquestador, **no decidida**. La implementación de lo construido en S97 vive en [[Index/23 - Arena Sandbox y Expedicion]].

### 8.1 · Lo que Juan fijó ⭐

- **Another Door es la espina**: superar una prueba paga; en cada puerta, cobrar o seguir. Las pruebas deben **involucrar las semillas de otros jugadores**. *"No estoy en contra del combate, pero sí de cómo se presenta"*: si hay enfrentamiento, tiene que tener **algo verdaderamente diferenciador**; nada Pokémon.
- **La mecánica "esquina a esquina"**: el nivel se genera con una semilla y todos juegan la misma; dos equipos entran por **esquinas opuestas**; **no controlás** a las criaturas, les das instrucciones básicas según lo que ves del mapa y el objetivo de la sección; tienen **comportamientos base** y las **Cutie Marks modifican comportamientos**, combinadas con instrucciones; lo divertido es **ver cómo reaccionan al entorno y al acercarse al enemigo**; varios objetivos por el mapa y **uno principal al medio**; el objetivo es **recolectar material**; la habilidad es **leer al equipo rival por sus tipos de partes** (ofensivas / balanceadas / recolectoras) y adivinar qué preferirá: asegurar una zona, pelear, defender primero; stages con niveles y **vista previa aproximada**: solo de recolección, de enfrentamiento, de curación.
- **Partes**: **todos tienen las 3 partes; lo que cambia es el tipo** (hoy 4 cuernos = los 4 prefabs de Suriyun; alas y espalda un solo modelo; meta: **Mega Tiny Dragon Pack** de SURIYUN, muchos tipos de formas simples, y podemos generar más). Cada tipo **se especializa en algo necesario**; las partes **no son mejores ni peores** (alas rápidas pero frágiles, que no ayudan a recolectar). Qué hace cada una se piensa después; lo importante es entender su **propósito junto con la personalidad** y que sea **escalable por ScriptableObjects** para múltiples comportamientos.
- **Lo más importante**: la infraestructura para **soltar 3 criaturas y ver qué comportamientos emergentes surgen**: libres en el escenario, cómo afrontan, cómo reaccionan cuando un enemigo se acerca, cómo reaccionan a lo que hacen los otros y entre sí. Reutilizar el SO de caras por reacción y el motor de criaturas. Debe **sentirse como ver de verdad 3 dragones explorar y pelear**, integrado con las animaciones del pack y los FX de Hovl enlazados con Feedbacks. **No preocuparse tanto por el escenario.**
- **Etapas**: Fase 1 = conexión, reutilización y preparación. Fase 2 = el agente inteligente que sabe moverse y desplazarse. *"Lo que ratifica el éxito de este prototipo es qué tan realista podemos hacer los agentes con sus animaciones y efectos."* Autónomos por el momento, pero **preparar el gancho para las intenciones futuras configuradas por ScriptableObjects**; **respetar la arquitectura**; **sin lógica ni complejidad innecesaria**.
- **Guías visuales**: ver **rango de visión** e **intención de movimiento**, estilo asset Shapes pero **hecho por nosotros**, limpio. Después: anillo de visión **punteado con puntas redondas y girando**, ruta **curva según el camino**, **transiciones suaves** al cambiar las flechas, **un mineral al medio y algunos chicos en las esquinas** para ver cómo reaccionan entre sí y yendo por los minerales. Del clip de Fase 1: *"me parece bien una etapa inicial"*.

### 8.2 · Lluvia de pruebas contra otros jugadores (orquestador, registrada)

Taxonomía de presencia del rival, de más barata a más cara: **fantasma** (grabación con la misma semilla) · **snapshot pilotado** · **huella** (lo que dejó en la sala) · **elección secreta diferida** (Another Door / Split or Steal por correo) · **síncrono en vivo** (Relay y Lobby). Regla transversal propuesta, **botín genético**: *"Superar al linaje de otro jugador te deja un huevo suyo"* (la caverna como única puerta por la que entran genes ajenos).

Doce pruebas, una línea cada una: **El Careo** (rugir / aguantar / irse; la cara como tell; si los dos rugen, chocan y decide el cuerpo; síncrono barato porque viaja una elección por compás) · **La Tinta Seca** (Turf War asíncrono sobre la tinta seca de otro) · **La Puerta Compartida** (cobrar / seguir / cerrar contra una partida grabada) · **Doble o Nada contra el Fantasma** (apostar a superar su marca por sala) · **El Duelo de Silbatos** (síncrono: solo silbás, ellos obedecen según sociabilidad) · **El Corral Ajeno** (Meet Your Maker: tu corral es la sala que otros prueban) · **La Caverna Única** (r/place: la tinta queda hasta que otro la tape) · **La Huella** (Death Stranding: charcos y huecos ajenos) · **El Cortejo Disputado** (un feral elige linaje por color, patrón y marcas) · **La Búsqueda del Perdido** (tres noches para que sangre o amigos rescaten) · **La Manada Robada** ⚠️ (sin nervio, se va con la otra manada) · **El Testigo** (relevo cooperativo por profundidad). Recomendación del orquestador antes de la mecánica de Juan: Careo + Tinta Seca + Puerta Compartida. La mecánica de 8.1 los reemplaza como núcleo; El Careo sobrevive como candidato para el **choque** entre criaturas.

### 8.3 · Tipos de parte: cada uno da algo y paga algo (orquestador, no decidido)

Regla en una frase: *"Todos tienen cuerno, espalda y alas; el tipo de cada una dice qué hace bien y qué hace mal; ninguno es mejor."* Leer al rival es leer sus tres tipos.

| Slot | Tipo | Da | Cuesta |
|---|---|---|---|
| Cuerno | Ariete | Rompe tabiques, empuja fuerte | Camina lento |
| Cuerno | Pala | Desentierra material escondido | No empuja |
| Cuerno | Antena | Siente material y ferales de lejos | Pierde nervio antes al chocar |
| Cuerno | Bocina | Su rugido baja el nervio ajeno a distancia | Delata: atrae ferales |
| Espalda | Alforja | Carga el doble | Lenta cuando va cargada |
| Espalda | Coraza | No suelta lo que lleva al chocar | Carga poco |
| Espalda | Cresta | Ilumina la sala oscura a los que la siguen | Se ve de lejos |
| Espalda | Concha | Al asustarse se encierra en vez de perderse | No recolecta mientras |
| Alas | Vela | Cruza huecos y agua | Suelta lo que lleva al aterrizar |
| Alas | Colibrí | La más rápida | No carga nada |
| Alas | Murciélago | No pierde nervio en la oscuridad | Lenta con luz |
| Alas | Élitro | No pierde nervio al chocar | No cruza huecos |

Los cuatro cuernos actuales pueden ser los primeros cuatro con nombre. Juan: los tipos se piensan después; primero el propósito y la infraestructura.

### 8.4 · Modelo de utilidad (orquestador, no decidido)

Referentes: The Sims, RimWorld, Dwarf Fortress, Creatures. Regla: *"Cada criatura mira lo que tiene cerca, le pone un valor según sus genes y va a lo que más vale."* Lo que tiene cerca lo entrega la percepción que ya existe (material, rival, peligro, compañeros, la bandera). Los pesos son los genes: **atrevimiento** pesa peligro y rival; **sociabilidad** pesa compañeros y obediencia; **tipos de parte** suman o restan a cada valor; **marcas** son un peso fijo en una frase; **la orden** del jugador es un percepto temporal con peso alto; **el nervio** baja con oscuridad, derrumbes, rugidos y choques perdidos y en cero solo vale huir. Tres capas de ScriptableObjects: efectos (`TraitEffectBase` → `PartTraitSO` / `CutieMarkSO`) · metas (`ExpeditionRuleBase` → `ExpeditionRulesSO`, **construida en S97 con la primera regla**) · ejecución (colaborador `AgentExpedition`, **construido en S97 en su versión mínima**). Choque propuesto: *"Pelear es hacer que el otro suelte el material"*: un solo empujón resuelto por el cuerpo; perder = soltar lo que llevás y perder nervio; nervio en cero = perderse. Sin barra de vida.

Riesgos registrados: reincidir en el prototipo táctico S77-S88 (evitable solo con órdenes pocas y toscas, máximo cuatro y contadas) y el aburrimiento de mirar un autobattler (salas cortas, la orden como intervención en vivo, apuesta Another Door encima, caracteres visibles a simple vista).

### 8.5 · Estado de la construcción al cierre de S97

- **Fase 1 ✅**: escena `ArenaSandbox` con semilla, Hovl convertido a URP, Feel enchufado al prefab (5 momentos), `ArenaSandbox.cs`, 3 dragones vagando, volando y peleando entre árboles y rocas. Clip enviado.
- **Fase 2 iniciada**: guías visuales propias (anillo punteado girando al radio real de percepción, ruta curva con fundido coloreada por intención, líneas de percepción por afinidad, discos bajo minerales) con `CueStyleSO` como gancho de datos; `ExpeditionRulesSO` + `SeekMaterialRule` + `AgentExpedition`; minerales en el mapa (1 central de valor 5, 4 de esquina). Verificado: el más osado tomó el central a los 17 s; los de esquina quedan fuera del radio de 6 m hasta que alguien pase cerca.
- Detalle de archivos, contratos, assets y auditoría en [[Index/23 - Arena Sandbox y Expedicion]].

### 8.6 · Plan de realismo (orquestador, S97; Juan pidió documentarlo antes de continuar)

Qué separa hoy a las criaturas de "sentirse reales", en orden de impacto por esfuerzo:

1. **Que el quieto esté vivo**: fidgets con Eat, Rest, Yes, No, Roar y Sick, frecuencia por carácter, y sobre todo **mirar**: girar el cuerpo hacia lo que perciben antes de decidir. Componente de presentación que lee intención y percepciones, como el driver de caras.
2. **Caminar como quien tiene un motivo**: correr solo por intención (persiguiendo, huyendo, yendo por material), caminar al vagar; velocidad por diales; aceleración y giro más suaves; giros en el lugar con los clips laterales importados y sin usar (`Walk_L/R`, `Run_L/R`, `Fly_L/R`). Hoy el rol Empático siempre corre por el umbral de velocidad.
3. **Beats de anticipación y consecuencia**: notar (pausa, emote, giro), tomar (Eat sobre el cristal, chispas, el cristal se encoge, Yes), perder un choque (Damage, Asustado, retroceso breve). Cada beat = un `MMF_Player` más y una llamada al driver de animación.
4. **Sonido**: no hay ninguno. Pasos, chirridos por emote, rugido, tintineo al tomar material; todo por Feel. Falta elegir un pack de sonidos de criatura.
5. **Compañía visible**: regla "acompañar a un amigo un rato" para que los sociables formen pares y los solitarios se aparten.
6. **Cámara de documental**: órbita lenta o seguimiento suave al que hace algo interesante, con Cinemachine.
7. **Detalles**: squash en aterrizajes por Feel, cristal central con brillo pulsante de Hovl, timers de cara y fidget desincronizados.

**Primer lote propuesto**: puntos 1, 2 y 3 (una sesión, sin tocar la tienda: componentes de presentación y `MMF_Player` nuevos). Lo que NO: más partículas, movimiento aleatorio por decorar, nada que no se explique en una frase con causa visible.

### 8.8 · Indicaciones: tres como máximo ⭐ (Juan, S97) y su mapeo (orquestador, no decidido)

**Juan ⭐:** *"Me gustaría mantenerlo simple: como 3 indicaciones como máximo de un pool de acciones decididas por su personalidad, y de acuerdo a eso empieza. Pueden haber elementos emergentes que los distraigan de cierta manera: algunos prefieren ir pegados a otro, otros más solos. Lo interesante es cómo estas directrices generales se combinan para plantear estrategias respecto a situaciones. Por ejemplo en Hades' Star, cuando explorás una estrella roja podés simplemente explorar e irte sin pelear, aunque podés elegir pelearte con el otro usuario; ese tipo de cosas puede ser muy interesante."*

**Regla en una frase (orquestador):** *"Le das hasta tres indicaciones de un pool que su carácter le permite; las sigue en ese orden, pero lo que encuentra y su carácter pueden distraerlo."* Consecuencia clave: **el pool es el fenotipo del carácter**: lo que podés ordenarle depende de lo que criaste. La profundidad vive en la cría, no en el input (nota 15 / S91).

**Mapeo a lo construido (propuesta):**
- Cada indicación = un asset `ExpeditionDirectiveSO` con la regla adentro (`ExpeditionRuleBase`, la misma base que ya corre para buscar material), nombre, icono y **requisito de carácter** (rangos de osadía y sociabilidad; más adelante tipos de parte y marcas).
- Cada criatura lleva un **set ordenado de hasta tres**; el orden da el peso: 1,0 · 0,7 · 0,4.
- **Innatas** (nadie las elige, pesan por dial): curiosidad por lo nuevo, compañía (sociable), apartarse (solitario), rugir (osado), huir cuando cae el nervio. Compiten con las indicaciones en cada evaluación; ganan cuando el estímulo está cerca. Así aparece la distracción y el "ir pegado a otro".
- **Situación por sala**: la sala decide qué percepciones existen; el mismo set se comporta distinto en recolección que en enfrentamiento. Hades' Star: `Explorar · Recolectar · Evitar rivales` sale sin pelear; `Confrontar · Custodiar el centro · Recolectar` va a buscar al otro usuario.
- Compromiso de unos segundos por decisión (el `GiveUpSeconds` actual) para que no cambie de idea cada frame.

**Pool inicial propuesto (8):** Explorar (todos) · Recolectar (todos) · Asegurar lo que carga en la salida (todos) · Evitar rivales (tímidos y balanceados) · Confrontar (osados) · Custodiar el centro (osados) · Pegarse a un compañero (sociables) · Ir solo (solitarios). Cada una explicable en una frase; el catálogo crece por assets, sin código.

**Cambios en el plan de Fase 2:** el asset global `ExpeditionRules` pasa a ser catálogo de indicaciones + bloque de innatas con pesos por dial; `AgentExpedition` evalúa set + innatas en lugar de una lista global; en el sandbox, mientras siguen autónomos, el set de cada criatura se asigna desde la semilla filtrado por su carácter para ver las combinaciones.

**La arquitectura del plan del jugador ⭐ (Juan, S97):** *"primero es selección de objetivo general, después qué rol quieres hacer, después las 3 direcciones iniciales; por el momento esa sería la arquitectura."*

**Mapeo (orquestador, no decidido):**
1. **Objetivo general** (por sala o por bajada, a nivel equipo): qué venís a hacer, al estilo Hades' Star: *Recolectar y salir · Explorar · Enfrentar · Asegurar el centro*. Fija qué cuenta como éxito y sesga las innatas (con "Recolectar y salir", evitar rivales pesa más para todos). Asset `ExpeditionObjectiveSO`.
2. **Rol** (por criatura): de los roles que **esa criatura puede jugar** según sus tipos de parte y su carácter (8.1: ofensiva / balanceada / recolectora, y de ahí *Recolector · Explorador · Guardián · Acompañante · Rompedor*). El rol **filtra el pool** de indicaciones y propone un set por defecto. Asset `ExpeditionRoleSO` (requisitos + indicaciones permitidas + set sugerido).
3. **Tres indicaciones iniciales** (por criatura, del pool del rol, en orden de prioridad). Asset `ExpeditionDirectiveSO` (8.8 arriba).

Es un embudo que va de lo estratégico a lo concreto y en cada paso lo que se ofrece depende de lo que se crió. La pantalla de plan (tres pasos antes de PLAY, UI Toolkit con la paleta `--mm-*`) es la primera UI de la expedición; hasta entonces el sandbox asigna objetivo, rol y set desde la semilla.

### 8.9 · Etapa 3: elenco, equipos, choque físico y UI de decisión (S99, acordado con Juan ⭐)

**Contexto.** Al cierre del pulido de la arena (S99, ver [[Index/23 - Arena Sandbox y Expedicion]]) Juan pidió planear la siguiente etapa con tres pedidos: nombres **verde pastel** para los propios y **rojo pastel** para los rivales; que la interacción entre criaturas (poderes, golpes, movimientos) sea **física, estilo Gang Beasts** (se golpean, salen volando, se reincorporan y siguen), tomando lo que ya hace la tienda con el lanzamiento y mejorándolo, "criaturas sueltas por doquier sin perder el realismo y que se sientan vivas"; y una **lluvia de ideas de IA** (su investigación) para ver qué queda y qué no.

**Lluvia de IA de Juan y filtro (orquestador, aceptado por Juan).** Lo que ya existe con otro nombre: utility scoring (`ExpeditionRuleBase` y las `ReactionRule` puntúan y gana el máximo, sin curvas) · needs / homeostático (salud, energía, afecto con umbrales) · personalidad (diales de sociabilidad y osadía sesgan score, cooldowns y fidgets; partes y marcas serán la capa siguiente) · percepción por radio (`AgentSenses`) · blackboard por criatura (`AgentContext`) · relaciones (`SocialGraphService`) · steering por NavMesh · simulación basada en agentes (el sandbox). **Se suma, en orden:** (1) consideraciones con curvas de respuesta = la versión formal del modelo de utilidad de 8.4, máximo cuatro por acción y cada una explicable en una frase; (2) memoria espacial mínima "lo último que vi" con caducidad (vuelven a lo que vieron, saben dónde está la salida, evitan donde los tumbaron; responde la pregunta de 8.7 sobre si conocen el mapa); (3) nervio como impulso interno; (4) cono de visión y oído (la guía pasa de anillo a arco) después del choque; (5) cohesión "pegarse a un compañero" como consideración innata, no como capa de steering; (6) pizarrón de equipo ("uno lo vio, el equipo lo sabe") después de tener equipos; (7) reputación entre linajes solo si el meta asíncrono lo pide. **No aplica:** GOAP y HTN (planifican un "cómo" que siempre es de uno o dos pasos y esconden la causa; las tres indicaciones ya son el plan), reinforcement learning y multiagente (no determinista ni explicable, rompe la reproducción del rival y la causalidad genética), arquitecturas cognitivas y memoria episódica o semántica completa, reputación como sistema propio. Regla transversal: cada decisión debe tener una causa visible en pantalla (el `?` y el "Se le fue" de S99); el riesgo de apilar sistemas es el autobattler que se mira.

**Lo aprendido en S97-S99 que aplica.** Los beats cortos y fijos leen mejor que la lógica inteligente; la emergencia sale de pocas reglas ("perdí el cristal, me enojo, peleo" apareció sola) y lo que falta son señales para leerla; datos por ScriptableObject con defaults = iteración sin código; medir con sondeo y mirar capturas son dos verificaciones distintas y hacen falta las dos; los efectos que no se ven a la distancia de la cámara no existen.

**Elenco básico de prueba (Juan: "si nuestros MoriMonchis no sirven, generemos nuevos para testear").** Hoy la arena mintea tres al azar por semilla con diales parecidos y el save de esta PC tiene a todos enfermos. Se define un elenco por asset (`ArenaRosterSO`) con arquetipos fijos y nombres que dicen lo que son, **los más básicos por ahora**: Osado (osadía alta, sociabilidad baja), Tímida (osadía baja, sociabilidad alta) y Equilibrado (ambos al medio), cada uno con un cuerno distinto, y un equipo rival espejo. El modo aleatorio queda como opción del sandbox.

**Equipos.** El equipo no es genética sino un dato de la bajada: vive en `Perceivable` (lo que los demás ven) y viaja en cada `Percept`. Dos equipos entran por esquinas opuestas con la misma semilla. Placas: verde pastel propios, rojo pastel rivales (la placa deja de colorear por género en la arena). Guías: percepción y enlace teñidos por equipo cuando el otro es rival. Conducta: con un rival no hay siesta ni persecución de juego.

**Choque físico v1 (decisiones de Juan, S99):**
- **Cuerpo único** (la criatura entera como un solo rigidbody que vuela, gira y rebota, como ya pasa en la tienda al lanzarla), con la blandura fingida: squash al impactar por Feel, giro en el aire, meneo de alas y cola. El **ragdoll articulado** estilo Gang Beasts (joints por hueso, animación mezclada con física) queda como **v2 opcional** si al verlo Juan quiere más flacidez.
- **Sin fuego amigo:** los movimientos solo afectan rivales.
- **Perder un choque no cuesta nada mientras se prueba** (soltar material y nervio quedan para después; la pregunta de 8.7 sigue abierta).
- **Tres movimientos, uno por slot**, desde ya; los tipos de 8.3 modifican después: **cuerno = embestida** (recta, empuje grande, retroceso propio chico) · **alas = picada** (despega, cae desde arriba, tumba y rebota) · **espalda = coletazo** (giro en el lugar, empuja a todos los rivales alrededor, más débil). Juan quiere verlos cuanto antes: botón de desarrollo en el sandbox para disparar cada uno a pedido.
- Ciclo del choque, fijo y legible: ver → encarar con anticipación de 0,3 s (rugido o agachada, el "tell") → embestida con velocidad real → impacto con hit-stop de 0,08 s y squash → vuelo (estado lanzado existente, rebotes contra piso, árboles, rocas y cristales, dominó por `knockTransfer`) → aterrizaje con polvo y cara Mareado → reincorporación con sacudida (`No`) → decisión (volver, alejarse, huir según nervio y osadía; por ahora sin costo).
- Datos: `ClashMoveSO` por movimiento (anticipación, alcance, impulso, sesgo hacia arriba, clip, feedbacks) y `ClashTuningSO` (masa por tamaño, damping, rebotes, ventanas de decisión). Código: colaborador `AgentClash` (anticipar, embestir, impactar, resolver) que compone con `AgentPhysics` y con el driver de animación; estado `Clashing`. `AgentSocial.Fighting` queda como pelea de juego entre amigos y no crece.
- Medición por sondeo: choques por minuto, distancia de vuelo, tiempo hasta levantarse, dominós, distancia mínima entre cuerpos; capturas en impacto, aterrizaje y levantada; clip para Juan.

**UI de decisión de acciones (Juan: "una vez terminemos de afinar el elenco básico, empecemos con la UI").** Es la pantalla de plan de 8.8: objetivo general → rol → tres indicaciones iniciales, en UI Toolkit con la paleta `--mm-*`, antes de PLAY. Supone el catálogo `ExpeditionObjectiveSO` / `ExpeditionRoleSO` / `ExpeditionDirectiveSO` y obliga a responder las dos preguntas ⭐ de 8.7; propuesta por defecto para la v1 si Juan no dice lo contrario: **por criatura** y **solo antes de PLAY**.

**Orden acordado:** (1) elenco básico y equipos (placas, guías, sin social entre rivales) → (2) UI de decisión de acciones → (3) movimientos por slot y choque físico v1 (el botón de desarrollo puede adelantarse si Juan quiere verlos antes) → (4) nervio y memoria corta → (5) utilidad con curvas y catálogo → (6) cono de visión, oído y pizarrón de equipo. Cada paso cierra con el loop de QA de S99 (sondeo + capturas miradas).

### 8.7 · Preguntas abiertas nuevas (S97)

- [ ] ⭐ ¿Las tres indicaciones son **por criatura o por equipo**?
- [ ] ⭐ ¿Se eligen **solo antes de empezar**, o también se puede cambiar una en vivo?

- [ ] ⭐ ¿Las criaturas **conocen los objetivos del mapa** desde el inicio (como la vista previa del jugador) o solo van a lo que ven? Cambia el ritmo de la sala (hoy: solo lo que ven, radio 6 m).
- [ ] ¿Cuántas criaturas bajan y cuánto dura una sala? (supuestos actuales: 3 y 60 s)
- [ ] ¿Las órdenes se dan antes de PLAY, en vivo, o ambas?
- [ ] ¿Perder un choque puede costar la criatura o solo el material?
- [ ] Pack de sonidos de criatura a elegir.
- [ ] Shapes (Freya Holmér, 100 USD): hoy no hace falta; se compra si aparecen polilíneas 3D o texto vectorial.
- [ ] ¿El paquete `com.unity.recorder` se queda?

---

## Referencias citadas en la sesión

Another Door (2026, multijugador por turnos semi-cooperativo: elección secreta simultánea, traición, "cobrar o abrir otra puerta") · Darkest Dungeon (estrés, antorcha, campamento, permadeath) · Deep Sea Adventure e Incan Gold (push-your-luck compartido) · Slay the Spire (mapa con vista previa, eventos, descanso) · Splatoon (Turf War, Splat Zones, Tower Control, Clam Blitz, Salmon Run) · de Blob · Pikmin 1/2 (carga, puentes, cuevas, perdidos) · Rain World · Into the Breach · Lemmings · Spelunky · Frogger / Crossy Road · Bomberman · Zelda · Pac-Man · Alien Isolation · Untitled Goose Game · Mark of the Ninja · Ooblets · Rock of Ages · Tricky Towers · Dig Dug / SteamWorld Dig · Snipperclips · Patapon · Don't Starve / Frostpunk · Donkey Kong · Stardew Valley · Kirby / Kirby Canvas Curse / Kirby's Dream Course · Yoshi's Island / Yoshi Touch & Go · Sonic · Windjammers / Lethal League · Snake / Pikuniku · Golf Story · Peggle · Angry Birds · Lightbot / RoboRally / Opus Magnum · Hitman · Buscaminas / Hexcells · Pokémon Snap / Bugsnax · Split or Steal · Modern Art / For Sale · Overcooked · Tamagotchi · Heave Ho / Chained Together · WarioWare / Mario Party · Mewgenics / Wobbledogs (sorpresa legible al criar) · Chao Garden / Umamusume / Nintendogs (mencionados en la lluvia inicial; carreras descartadas por Juan).

Fuentes sobre Another Door consultadas en sesión: página de Steam (app 2786760), Gematsu (anuncio 2026-06), PC Gamer, Turn Based Lovers, Game Rant.

---

## Estado

**DRAFT S96 (2026-09-02).** Dragon RPS ✅ jugado → 🪦 fallido (Parte 1). Marco de Juan registrado (Parte 2). Linaje (Parte 3) y bajada (Parte 4-6): **propuestas del orquestador, no decididas**.

**S97 (2026-09-03).** Juan trajo su mecánica ⭐ (Parte 8.1) y fijó etapas; se construyó la Fase 1 y arrancó la Fase 2 en Unity (Parte 8.5, implementación en [[Index/23 - Arena Sandbox y Expedicion]]). El plan de realismo (8.6) quedó documentado a pedido de Juan antes de continuar. Preguntas abiertas en 8.7; las de la Parte 7 sobre linaje siguen sin responder.

**S99 (2026-09-04).** Pulido de la arena verificado en loop de QA (ver [[Index/23 - Arena Sandbox y Expedicion]]). Juan pidió planear la Etapa 3: equipos con placas verde/rojo pastel, choque físico estilo Gang Beasts y filtro de su lluvia de IA → **Parte 8.9 acordada**: cuerpo único (ragdoll articulado como v2 opcional), sin fuego amigo, perder no cuesta mientras se prueba, tres movimientos por slot, elenco básico de tres arquetipos por equipo, y después del elenco la UI de decisión de acciones (8.8). Las dos preguntas ⭐ de 8.7 siguen abiertas; la UI obliga a responderlas.
