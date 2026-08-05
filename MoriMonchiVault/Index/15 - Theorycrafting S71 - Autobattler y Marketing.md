---
tags: [index, design, theorycrafting, marketing]
---

# 15 - Theorycrafting S71 — El autobattler y la llegada al público

> **Sesión 71 (2026-08-05).** Juan abrió sesión pidiendo theorycrafting y planteó el bloqueo real: *"mi mayor problema es el autobattler, ¿siquiera será divertido? Deberíamos optar por algo donde el jugador tenga que tomar decisiones. Esa es de las razones por las que no he avanzado en este proyecto por 10 días, no dejo de pensar en eso."*
>
> Esta nota es la respuesta completa: diagnóstico de diseño, análisis de mercado con datos verificados, y el experimento propuesto para desbloquear.
> **Estado: opinión del orquestador entregada, esperando decisión de Juan.** Nada de esto bajó a código todavía.

---

## TL;DR (si leés una sola cosa, que sea esto)

1. **El combate automático NO es el problema.** Backpack Battles (2 devs) vendió +1M de copias con combate 100% automático. Super Auto Pets y Mechabellum también. El jugador nunca toca nada durante la pelea en ninguno de los tres.
2. **El problema es la VELOCIDAD DEL CICLO.** En esos juegos: decidís → peleás → ves el resultado → reajustás, en menos de un minuto. En MoriMonchis ese ciclo dura horas o días. Sin iteración rápida no hay aprendizaje, y sin aprendizaje el autobattler deja de ser un experimento y pasa a ser una lotería que mirás.
3. **La asincronía está bien elegida y no se toca.** Es exactamente el modelo de Super Auto Pets / Backpack Battles, el único patrón del género con éxitos de equipos de 1-2 personas. Lo que rompe el ritmo son **los timers de tiempo real encima** (30 min de cría, "4 cruzas / 5 peleas por día"): son gates de F2P móvil injertados en un premium de PC.
4. **Estás parado en el peor punto del espacio de diseño**: cero información del rival + decisiones caras e irreversibles + ciclo lento. Es la queja que hundió a The Bazaar (−83% de jugadores). Y encima tenés muerte permanente, que agrava las tres.
5. **El fix de mayor impacto y menor costo: mostrar la composición rival antes de confirmar el lineup.** Convierte la grilla 2-3-2 de sorteo en decisión, usando código que ya está escrito.
6. **En marketing, el que va adelante es la CRIATURA**, no la tienda ni el autobattler. El autobattler es el peor frente de batalla posible (saturado, dominado por gratuitos con años de contenido). Que sea el motor está bien; que sea la primera frase de la página de Steam sería un error.
7. **Diez días pensándolo es la señal de que la pregunta no se responde con la cabeza.** El experimento para responderla empíricamente está al final de esta nota y cuesta una o dos sesiones.

---

## PARTE 1 — DIAGNÓSTICO DE DISEÑO

### 1.1 Por qué el diagnóstico original está mal ubicado

La premisa "el jugador no toma decisiones, por eso no va a ser divertido" asume que la diversión del autobattler vive en el combate. No vive ahí. En los tres éxitos del género el combate es **la respuesta inmediata a una decisión que acabás de tomar**: es el momento en que descubrís si tu idea era buena. Es un experimento, no un espectáculo.

Cuando el combate llega horas después de la decisión, pierde esa función. Ya no podés atribuir el resultado a nada que hiciste. Eso es lo que se siente como "pasivo" — y es un problema de **ritmo**, no de mecánica.

### 1.2 El ciclo de MoriMonchis hoy vs. los referentes

| Juego | Dónde vive la decisión | Duración del ciclo completo | Info del rival antes de comprometerse | Reversibilidad |
|---|---|---|---|---|
| **Super Auto Pets** | compra + posicionamiento, sin timer | ~1-3 min por turno, combate ~30s | **CERO** | baja por turno, **alta por run** (runs de ~5 min) |
| **Backpack Battles** | tienda + tetris de mochila | 2-4 min por ronda, combate 20-40s | **CERO** (conocés el meta, no el snapshot) | **alta** dentro de la ronda (recolocar es gratis) |
| **Mechabellum** | despliegue por rondas | 1-2 min por ronda | **MÁXIMA del género** (ves el ejército rival completo) | **nula** — por eso cada despliegue pesa |
| **The Bazaar** ⚠️ | día PvE + compras | runs de 45-60+ min | CERO | el run entero es el commit, y es larguísimo |
| **MoriMonchis hoy** | cría + equipo + grilla 2-3-2 | **horas o días** | **CERO** | **nula** (permadeath, cría irreversible) |

⚠️ The Bazaar es la advertencia: mismo cuadrante que MoriMonchis. Pasó de 17.136 CCU de pico a ~2.900 (−83%).

### 1.3 Los dos polos que funcionan (y el medio tibio que no)

**Polo A — cero info, decisiones baratas, ciclo corto** (Super Auto Pets, Backpack Battles).
La gracia es optimizar tu propia máquina contra el meta. Perder es barato y te enseña algo enseguida.

**Polo B — información total, decisiones caras** (Mechabellum).
La gracia es leer al rival y contrarrestarlo. Cada despliegue pesa porque sabés contra qué jugás. Las reseñas lo describen como "ajedrez con mechas".

**El medio tibio — poca info + decisiones caras + ciclo largo** es donde está MoriMonchis, y es donde ningún juego del género funcionó.

Y hay un agravante que ninguno de los referentes tiene: **muerte permanente**. Cero información + decisiones irreversibles + una criatura que se muere de verdad es la receta de la frustración compuesta. El permadeath no prohíbe nada, pero **obliga a ser mucho más generoso con la información previa y mucho más legible en el resultado**, no menos.

### 1.4 Recomendación A — Sacar los timers de tiempo real del núcleo del loop

**El async no es el culpable; los gates de reloj sí.**

En Super Auto Pets el async es *instantáneo*: peleás contra una foto guardada ahora mismo, no esperás a nadie. Podés jugar 20 peleas en 20 minutos. La arquitectura de MoriMonchis (Cloud Code, snapshots server-side) puede hacer exactamente lo mismo — hoy no lo hace porque los treinta minutos de cría y el pilar de "4 cruzas / 5 peleas por día" lo estrangulan.

> **Si querés conservar la escasez, que sea escasez de RECURSOS que administrás** (comida, Dabloons, espacio en la tienda, salud de las criaturas), **no de reloj de pared**. La escasez de recursos genera decisiones; la escasez de reloj genera abandono.

Nota de implementación: esto toca `BREED_DURATION_MS` (hoy hardcodeado en JS, ver [[Index/08 - Known Bugs & Checkpoints]]) y el pilar declarado en [[Index/01 - GDD Core]]. Es un cambio de diseño mayor, no un tweak.

### 1.5 Recomendación B — Información del rival antes de confirmar el lineup ⭐

**El cambio de mayor impacto por menor costo de toda la sesión.**

Hoy colocás tres unidades en siete casilleros de la grilla 2-3-2 **a ciegas**. Eso no es agencia, es un formulario. En el momento en que ves que enfrente hay dos Protectores de Planta, esa misma grilla se convierte en una decisión real: concentrar o repartir el fuego, contra-elemento, dónde poner al Agresivo.

Propuesta: mostrar **roles y elementos** del equipo rival (no necesariamente los números exactos de stats) antes de confirmar. Te mueve del medio tibio al **Polo B (Mechabellum)**, que es el polo que le corresponde a un juego con decisiones caras e irreversibles.

El contenido para que esto sea interesante **ya está construido**: 3 roles, 4 elementos, 12 reacciones, targeting por fila. Ver [[Index/13 - Combat Design Direction]].

### 1.6 Recomendación C — "Gambit-lite", con moderación

Idea considerada: reglas condicionales tipo **gambits de Final Fantasy XII** ("si mi Protector baja del 30%, hacé X"). Es determinista, asincrónico, y es theorycraft puro — encaja perfecto con el pilar del sim determinista por semilla.

**Pero los números obligan a moderarla.** La programación explícita de comportamiento es un género de nicho duro:
- **Gladiabots** (dev solo, 88% positivo): el propio dev dice que "los juegos de programación son un género bastante de nicho".
- **Screeps**: ~500 jugadores activos, ~100 competitivos, según estimación en su propio foro.

Ninguno de los éxitos masivos deja programar reglas: **dejan elegir piezas cuyo comportamiento fijo y legible YA es la regla**.

**La versión recomendada** es la intermedia que Dragon Age: Origins validó para público masivo (su lead designer dijo que se inspiró en los gambits de FFXII y que el juego fue mejor por eso): **2-3 ranuras de prioridad por criatura, con opciones cerradas y legibles**, no un editor de árboles lógicos.

Y lo mejor: **la materia prima ya existe**. Los diales genéticos de Sociabilidad y Osadía (S69) pueden inclinar el comportamiento de combate — que la osadía heredada determine a quién ataca, que el rol determine a quién protege. **La decisión pasa a vivir en la crianza, que es tu pilar**, en vez de en un editor de scripts.

### 1.7 Legibilidad causal (requisito, no adorno)

La observación clave sobre Super Auto Pets: *"un novato puede ganar una partida, entender por qué, y sentir que sus decisiones importaron."* Eso exige **orden de activación determinista y visible** (SAP resuelve estrictamente de adelante hacia atrás).

Aplicable directo a `CombatResolver` / `CombatService`. Con permadeath, la legibilidad no es opcional: si el jugador no entiende **por qué** murió su MoriMochi, la pérdida se lee como injusticia.

### 1.8 Riesgos conocidos del async (para no repetir errores ajenos)

1. **Pool de snapshots mal curado.** Super Auto Pets tuvo que parchear su matchmaking async porque generaba emparejamientos injustos; lo rehizo con dificultades explícitas (Normal / Hard / Super Hard) y matcheo solo contra equipos del mismo pack. → **La curación del pool de snapshots ES el sistema de dificultad. Diseñalo como feature, no como matchmaking.**
2. **Combate largo o no salteable.** El error de The Bazaar: hilos enteros pidiendo skip. Ellos metieron un timer diegético (una tormenta de arena a los 30s) para forzar el cierre. → Combate comprimido, opción x2 y skip.
3. **Pool envejecido**: si el balance cambia, las fotos viejas quedan desfasadas. SAP lo mitiga matcheando por turno + pack.
4. **El contenido nuevo ES la retención en este género** (Dota Underlords murió de eso: 111.000 CCU en julio 2019 → menos de 15.000 en diciembre). Un dev solo no compite en cadencia de parches con un live-service. → **La variedad tiene que ser sistémica/generativa (tu genética), no inyectada a mano.**

---

## PARTE 2 — MARKETING Y LLEGADA AL PÚBLICO

### 2.1 El problema del pitch: tenés tres juegos peleando

Simulador de tienda ochentera + criador con genética visible + autobattler asincrónico. **En Steam, un pitch que necesita tres frases no es ningún pitch.**

Dato duro (Zukowski / How To Market A Game): de 416 indies exitosos de 2022 con +1000 reseñas, **solo el 11,8% eran mezclas novedosas de género**; el 88,2% eran géneros conocidos con una variación leve. Recomienda la regla "Most Advanced Yet Acceptable": un género conocido + 20-30% de twist.

**Los híbridos que funcionaron eligieron UNO adelante y presentaron el otro como consecuencia de una fantasía:**

| Juego | Cifra | Qué puso adelante |
|---|---|---|
| **Moonlighter** | 500K año 1, 1M en 2 años, **2M en 4 años** | "Tendero de día, héroe de noche" — un personaje con doble vida, no dos géneros |
| **Recettear** | ~300K copias (esperaban 10K en 6 meses) | La TIENDA en el título ("An Item Shop's Tale"); el dungeon es el medio de abastecimiento. El meme "Capitalism, ho!" hizo el marketing |
| **Potion Craft** | 100K copias en 3 días | UNA sola mecánica visual e hipnótica; la tienda es contexto |
| **Stardew Valley** | — | La granja adelante; el combate/minas nunca aparece en el pitch principal |

### 2.2 El que va adelante es la CRIATURA ⭐

**Evidencia de que la criatura generada es el gancho más fuerte que tenés:**

- **Wobbledogs** (Tom Astle, **dev solo**, $19.99): ~$11.3M brutos estimados, 200-500K owners, **98% positivo sobre ~11.800 reseñas**. Perros procedurales que mutan según lo que comen. **El gancho comunicado NO fue "sistema genético profundo", fue "mirá el bicho horrible que me salió"** — el propio dev posteaba clips de perros deformes en TikTok.
- **Spore**: regaló el Creature Creator **3 meses ANTES** del juego. 100K criaturas en 24h, **1.5M antes del lanzamiento**, 100M totales. Es la jugada de marketing de criaturas procedurales más probada que existe.
- **Palworld**: explotó por juntar lo tierno con lo perturbador ("Pokémon con armas"). 1M de copias en 8 horas, 7M en menos de una semana.
- **Bugsnax**: viralizó por una canción pegajosa y un nombre absurdo ("kinda bug, kinda snack").
- **Cassette Beasts** (2 devs): ~$5.1M brutos estimados, 96% positivo. Gancho: **fusión de monstruos** (combinatoria compartible) + "Pokémon para el fan desilusionado".
- **Niche - a genetics survival game**: puso "genetics" en el título — ~200K owners en 9 años. Compará con los 200-500K de Wobbledogs en 3 años. **El framing científico atrae menos que el framing "criatura graciosa emergente".**
- **Species: ALRE** (evolución "seria" sin personalidad de criatura): ~28.000 copias. **La ciencia sola no vende; la criatura carismática sí.**

**Qué comparte la gente, según el patrón de todos estos casos:**
1. El espécimen roto / feo / absurdo **que me salió a mí** (Spore, Wobbledogs)
2. La transgresión tierno + oscuro (Palworld)
3. El earworm o la frase repetible (Bugsnax, Recettear)

**MoriMonchis tiene materia prima para las tres.** Gremlins + Furby + Tamagotchi con ADN visible y muerte permanente es exactamente la tensión tierno/oscuro. La genética da variedad infinita de imágenes **sin costo de arte**. Lo que falta es **la frase**.

Hueco de mercado detectado: **Monster Rancher murió en 2005 y Creatures (1996) no tiene sucesor moderno.** "Monster Rancher moderno" es demanda nostálgica sin oferta actual. Wobbledogs y Niche capturaron pedazos; **nadie lo capturó entero con combate.**

### 2.3 Por qué el autobattler es el peor frente de batalla

Género saturado y dominado por gratuitos con años de contenido acumulado. Además:
- **Dota Underlords** (Valve): 111.000 CCU → <15.000 en 5 meses, abandonado.
- **Auto Chess** (el original): ~263 concurrentes hoy.
- **Storybook Brawl**: cerrado (murió por el negocio — comprado por FTX —, no por diseño).
- **The Bazaar**: −83% del pico, se autodestruyó con monetización y combates largos sin skip.

Que el autobattler sea el **motor** del juego está bien. Que sea la **primera frase de la página de Steam** sería un error.

### 2.4 Muerte permanente en el mensaje: amenaza vs. legado

- **Wobbledogs** terminó agregando la opción de **desactivar la muerte**. Cita del dev: *"la gente reacciona más fuerte a la muerte de animales que de personas en los medios… multiplicado en el caso de perros."* Hay hasta una guía comunitaria de "inmortalidad" y entrada en DoesTheDogDie.com. **El público pet-sim busca activamente evitar la muerte.**
- **Niche**: la muerte es central y no desactivable — funciona porque **se vende como supervivencia/estrategia, no como pet-sim**.
- **Wildermyth**: permadeath con doble válvula de escape (podés quedar "maimed" en vez de morir, y los muertos entran a un sistema de Legacy que los trae de vuelta en campañas futuras). **El marketing vendió "historias que emergen de la pérdida", no "tus personajes mueren".**
- **Dwarf Fortress**: "Losing is fun" como lema — la pérdida reencuadrada como generador de anécdotas.

**Conclusión:** la muerte permanente no espanta si (a) el juego se posiciona como estrategia/supervivencia y **no como pet-sim cozy**, y (b) **la muerte deja algo detrás**. Espanta cuando el pitch es "cuidá a tu mascota" y la muerte es castigo sin residuo.

> **MoriMonchis ya tiene el sistema de legado construido y no lo está usando como argumento: se llama HERENCIA GENÉTICA.** "Muere, pero su linaje continúa" convierte la amenaza en gancho. Wildermyth es el modelo de messaging; Wobbledogs advierte que una parte del público va a pedir el toggle igual.

⚠️ **Tensión de posicionamiento a resolver:** el juego tiene una capa cozy fuerte (tienda, caricias, comer de la mano, cuidar necesidades) y permadeath. Los datos dicen que ese cruce es el que peor tolera la muerte. Hay que decidir conscientemente de qué lado cae el mensaje.

### 2.5 Números de Steam que conviene tener a mano

**Conversión wishlist → venta:**
- Mediana **0,15x** de wishlists a ventas en la primera semana (juegos con 25K+ wishlists, sept 2024–ago 2025, GameDiscoverCo). Arriba de $10 convierte ~0,10x. El rango real varía hasta 10-20x entre juegos.
- La conversión general cayó de ~20% (2018) a 5-10% (2026). Los publishers apuntan a **30K+ wishlists** como colchón.

**Cuándo abrir la página de Steam:** Zukowski — **lo antes posible**, apenas decidas mostrar algo público, porque un post puede viralizar en cualquier momento y sin página perdés miles de wishlists. *"Wishlists don't age, they aren't bread"*: solo dejan de convertir si cambiás drásticamente de género o estilo.

**Next Fest:** usar el **último** antes del lanzamiento, con demo publicada semanas antes. ~2.000 wishlists es el umbral donde el fest empieza a trabajar para vos; 3.000-5.000 para entrar en "Popular Upcoming". Lanzar unas semanas después, no inmediatamente. **El fest amplifica lo que ya traés** (correlación r=0,825 entre wishlists previas y ganadas).

**Capsule y página:** un solo gancho apropiable (silueta rara, cara distintiva). Primera línea = género + mecánica + promesa emocional en UNA frase. Trailer con gameplay en los primeros 5 segundos. **>4% de click-to-wishlist** es el umbral que la mayoría no alcanza.

**TikTok:** el reach orgánico puro colapsó a inicios de 2025. El playbook actual es "paid organic": postear orgánico, identificar ganadores en 24-48h, amplificar con Spark Ads. **$1-10 por wishlist** vs $25-167 en ads tradicionales.

**Tendencia macro:** "cozy" y "solo" son las dos keywords que más crecieron en juegos exitosos de Steam en 5 años (+675% y +450%). Audiencia cozy: 45-55% mujeres, 25-45 años, con poder adquisitivo.

### 2.6 Nota sobre shop-sims (por si la tienda sube de prioridad)

Hay **dos públicos de shop-sim que casi no se solapan**:
- **(a) "Simulator" en primera persona**, vía streamers, loop logístico. **Supermarket Simulator**: 2.6M unidades, ~$27M brutos, 107K CCU de pico, 46K canales creando contenido en 30 días. **TCG Card Shop Simulator**: ~400K copias en 10 días, ~$32M brutos estimados — y combina tienda + abrir sobres, o sea **el híbrido tienda/coleccionismo ya demostró demanda masiva**.
- **(b) Cozy/management**, vía estética (Recettear, Travellers Rest ~$8.8M brutos, Potion Craft).

Un juego retro-80s con criaturas cae naturalmente en (b), pero el fenómeno TCG Card Shop demuestra que "tienda + coleccionar bichos en vitrina" también puede pescar en (a) si el loop de reponer y vender es satisfactorio por sí solo.

---

## PARTE 3 — EL EXPERIMENTO PROPUESTO (cómo desbloquearse)

> Diez días pensando si el combate va a ser divertido es la señal de que se está intentando responder con la cabeza una pregunta que **solo se responde jugando**.

Y ya está casi todo construido para responderla: el sim determinista funciona, la consola de desarrollo 3v3 existe, el visualizador está aprobado visualmente. **Lo que falta es sentarse a jugar veinte peleas seguidas.**

### El "Gauntlet de prueba" — 1 o 2 sesiones de trabajo

Un modo **local** de ~8 peleas encadenadas contra composiciones de la casa, con estas cuatro condiciones:

1. **Ves contra qué vas** antes de cada pelea (roles + elementos del rival).
2. **Podés reposicionar la grilla y cambiar el equipo** entre pelea y pelea.
3. **Sin muerte permanente** dentro de la tirada.
4. **Sin ningún timer** de tiempo real.

Eso reproduce las condiciones de los tres juegos que funcionan: ciclo de ~1 minuto, información adelante, decisiones baratas y repetibles.

**Qué responde:** si con esas condiciones el combate sigue sin divertir, el problema es el sistema y hay que rediseñarlo de raíz. **Apuesta del orquestador:** se va a sentir completamente distinto, porque el motor ya tiene la profundidad (3 roles, 4 elementos, 12 reacciones, targeting por fila) y lo único que le falta es poder iterar lo bastante rápido como para descubrirla.

**Costo:** bajo. Reusa `CombatService.SimulateLocal`, la grilla 2-3-2 y el visualizador existentes. Es un modo de prueba, no una feature de producto — si funciona, se convierte en el esqueleto del modo real.

---

## Hallazgos laterales de esta sesión (deuda del vault)

1. **El sistema de clientes está más avanzado de lo que dice el vault.** Existen `CustomerService` (apex singleton con `ValuationHandler` + `NegotiationFlow`), `CustomerPricingSO` (base por tier + multiplicadores de stats, cantidad de cría, winrate de combate, tier, y paso de renegociación) y `CustomerArchetypeSO`/`Database`. La "Fase 7 — economía" que [[Index/13 - Combat Design Direction]] lista como pendiente **ya bajó a código**; esa nota quedó desactualizada.
2. **Dos enlaces rotos.** Varios ScriptNodes de clientes apuntan a `[[Index/04 - Customer System]]` y el `00 - Index.md` apunta a `[[Index/08 - NPC Customers]]`, pero el 04 es "UGS & Cloud" y el 08 es "Known Bugs". **El dominio de economía y clientes es el único sin nota Index propia** — su diseño no vive en ningún lado.
3. **Hueco de diseño de fondo (vale para cualquier rumbo que se elija):** la capa de simulación de mundo vivo acumulada (necesidades, grafo social, diales genéticos, caricias, comer de la mano) **no paga nada mensurable**. `CustomerPricingSO` valúa por tier, stats, cría y winrate — **no mira si lo cuidaste**. El cuidado hoy es un fin en sí mismo, desconectado de la economía y del combate.

---

## Otros temas de theorycrafting que quedaron sobre la mesa

Se ofrecieron cuatro al abrir la sesión; Juan eligió pivotear a autobattler + marketing. Los otros tres siguen abiertos:

1. **El día de juego** — hoy el juego es un sandbox de sistemas sueltos, sin ritmo ni cierre. Los pilares dicen "4 cruzas / 5 peleas máximo" pero nada enmarca esos límites. *(Nota: la Recomendación A de arriba propone directamente eliminarlos — este tema quedó parcialmente absorbido.)*
2. **Que el cuidado pague** — puente entre el mundo vivo y la economía/combate (ver hallazgo lateral 3).
3. **Balance del combate 3v3** — pendiente 5 de S39: magnitudes de los 12 estados en defaults v1 sin data real, cap del escudo abierto, ítems que aplican estados nunca autorados.

---

## Dónde retomamos

Juan tiene que decidir el rumbo. Las decisiones abiertas, en orden de impacto:

- [ ] **¿Se sacan los timers de tiempo real del núcleo del loop?** (Recomendación A — cambia un pilar del GDD)
- [ ] **¿Se muestra la composición rival antes de confirmar el lineup?** (Recomendación B — mayor impacto por menor costo)
- [ ] **¿Se construye el Gauntlet de prueba como próxima sesión?** (Parte 3 — la propuesta concreta del orquestador)
- [ ] **¿Cuál es el género que va adelante en el pitch?** (Parte 2 — la criatura, según los datos)
- [ ] **¿De qué lado cae el mensaje: estrategia/supervivencia o cozy?** (2.4 — determina si el permadeath suma o resta)

Relacionado: [[Index/13 - Combat Design Direction]], [[Index/01 - GDD Core]], [[Index/09 - Active Context]], [[Index/11 - Technical Debt]].

---

## Fuentes

**Autobattlers:** [GameDiscoverCo — Backpack Battles](https://newsletter.gamediscover.co/p/how-backpack-battles-sold-650k-copies) · [Game World Observer — Backpack Battles 640K](https://gameworldobserver.com/2024/04/25/backpack-battles-sales-640k-copies-china-top-country) · [Shochiku — 1M copias](https://game.shochiku.co.jp/news/over-1-million-copies-sold-worldwide-a-pvp-inventory-management-auto-battler-backpack-battles-is-finally-releasing-full-1-0-version/) · [Gamalytic — Mechabellum](https://gamalytic.com/game/669330) · [MonsterVine — Mechabellum](https://monstervine.com/2025/04/mechabellum-review/) · [Steambase — The Bazaar charts](https://steambase.io/games/the-bazaar/steam-charts) · [TheGamer — The Bazaar](https://www.thegamer.com/the-bazaar-steam-launch-censorship-accusations-silencing-criticism/) · [Noisy Pixel — entrevista Reynad sobre async](https://noisypixel.net/the-bazaar-interview-reynad-asynchronous-pvp-deckbuilder/) · [Steam News — matchmaking de SAP](https://store.steampowered.com/news/app/1714040/view/2901998026690491150) · [Destructoid — Dota Underlords](https://www.destructoid.com/i-cant-believe-this-abandoned-valve-game-still-has-players-and-is-fun-4-years-later/) · [Decrypt — Storybook Brawl](https://decrypt.co/137875/ftx-sam-bankman-fried-storybook-brawl-video-game-shutting-down) · [1v9 — scouting en TFT](https://1v9.gg/blog/tft-scout-system-explained-guide)

**Gambits / programación de comportamiento:** [Game Developer — FFXII](https://www.gamedeveloper.com/design/why-i-final-fantasy-iv-i-was-key-to-i-ffxii-i-s-ai-driven-gambit-system) · [Medium — entrevista al dev de Gladiabots](https://medium.com/@gofig.news/a-coffee-break-with-s%C3%A9bastien-dubois-gladiabots-45609a63e39f) · [Foro de Screeps — población real](https://screeps.com/forum/topic/553/a-request-from-the-dev-do-not-cater-to-the-elites)

**Criaturas / crianza:** [Steam Revenue Calculator — Wobbledogs](https://steam-revenue-calculator.com/app/1424330/wobbledogs) · [Game Developer — IA y física de Wobbledogs](https://www.gamedeveloper.com/design/behind-the-ai-and-physics-of-i-wobbledogs-i-procedurally-goofy-wobbledogs) · [Crossplay — devs sobre la muerte de mascotas](https://www.crossplay.news/p/game-developers-explain-how-they) · [Wikipedia — Niche](https://en.wikipedia.org/wiki/Niche_(video_game)) · [Steam Revenue Calculator — Cassette Beasts](https://steam-revenue-calculator.com/app/1321440/cassette-beasts) · [VGChartz — Slime Rancher 5M](https://www.vgchartz.com/article/452258/slime-rancher-sales-top-5-million-units/) · [EA — 100M de criaturas en Spore](https://www.ea.com/news/100-million-creatures-take-over-spore-universe) · [GamesBeat — Creature Creator como marketing](https://gamesbeat.com/electronic-arts-releases-spore-creature-creator-to-create-buzz-for-its-biggest-game/) · [PC Gamer — Palworld](https://www.pcgamer.com/palworld-is-2024s-first-breakout-hit-why-is-it-so-popular/)

**Marketing / Steam:** [HTMAG — mezclar géneros es mala idea](https://howtomarketagame.com/2023/02/22/editorial-maybe-mixing-genres-is-a-bad-idea/) · [GameDiscoverCo — estado de la conversión de wishlists](https://newsletter.gamediscover.co/p/the-state-of-steam-wishlist-conversions) · [GWO — cuándo abrir la página de Steam (Zukowski)](https://gameworldobserver.com/2025/03/11/steam-page-launch-guide-wishlists-zukowski) · [presskit.gg — guía de Next Fest](https://presskit.gg/field-guides/steam-next-fest-guide) · [presskit.gg — optimización de la página](https://presskit.gg/field-guides/steam-page-optimization-guide) · [PC Gamer — el boom cozy](https://www.pcgamer.com/games/life-sim/the-cozy-game-boom-is-the-clearest-trend-on-steam-over-five-years-of-data/) · [Game Developer — Moonlighter 500K](https://www.gamedeveloper.com/game-platforms/-i-moonlighter-i-sells-500-000-copies-in-less-than-a-year) · [Siliconera — Recettear](https://www.siliconera.com/recettear-sales-say-capitalism-ho/) · [GWO — Supermarket Simulator](https://gameworldobserver.com/2024/03/05/supermarket-simulator-viral-success-40k-ccu-turkish-devs) · [GameDiscoverCo — TCG Card Shop Simulator](https://newsletter.gamediscover.co/p/tcg-card-shop-simulator-the-second)

> **Nota de método:** las cifras de revenue de terceros (Gamalytic, Steam Revenue Calculator, SteamSpy) son estimaciones por método Boxleiter (~±30% de error), no cifras oficiales. Las cifras de CCU, reseñas y las declaraciones de desarrolladores son verificables en las fuentes.
