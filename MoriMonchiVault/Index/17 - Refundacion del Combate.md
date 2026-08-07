---
tags: [index, design, theorycrafting, combate]
---

# 17 - Refundación del Combate (S72)

> **Sesión 72 (2026-08-07), segunda mitad.** Tras leer completos los informes [[Index/15 - Theorycrafting S71 - Autobattler y Marketing]] y [[Index/16 - Diagnostico por Frentes]], Juan decidió el rumbo: *"hay potencial en la idea, el pilar que siento más endeble es el del sistema de combate. Olvidate de todo lo demás por el momento."*
>
> Esta nota guarda el desarrollo completo: el juego pasado por los lentes de Jesse Schell, el diagnóstico estructural de por qué la pelea no se entiende, y la exploración de géneros y formatos para refundarla.
>
> **Estado: material de diseño en bruto, ninguna decisión tomada.** Juan está escribiendo un manuscrito propio con sus conclusiones y lo va a entregar en una sesión futura. **Nada de esto se implementa hasta que ese manuscrito llegue.**

---

## Punto de partida: el encargo de Juan

Tres correcciones que Juan hizo al orquestador durante la sesión, y que son el marco de todo lo que sigue:

1. **El canal doble de marcas (aliada/enemiga) NO se toca por la vía simple.** Ya se exploró unificarlo y el resultado fue peor: con una marca de significado único, atacar a un enemigo podía **beneficiarlo**. La ambigüedad de fuente existe por una razón.
2. **No se buscan soluciones visuales.** *"Los visuales solo sirven cuando una idea está bien implementada. Pokémon Rojo era solo texto e imágenes básicas y se entiende. Una buena idea es legible en sus simientos."*
3. **El resto del loop ya es entretenido** (tienda, breeding, traits, consumibles). El combate es lo único que necesita replanteo: **debe ser autobattle, pero con suficiente estrategia como para que valga la pena verlo y se sienta satisfactorio.**

Ideas propias de Juan al abrir el tema: **cambiar vida por hits**, **reformular por completo la aplicación de los estados conservándolos**, explorar **Into the Breach**, y que **el jugador vea el escenario y la composición enemiga antes de comprometerse**.

---

## PARTE 1 — El juego por los lentes de Schell

*(Nombres de lentes sin numerar: la numeración cambia entre ediciones.)*

### Donde es fuerte

| Lente | Nota | Por qué |
|---|---|---|
| **The Toy** | 9 | Agarrar, tirar, acariciar, ver el ragdoll levantarse. Ya es un juguete antes de ser juego. Sin explotar en el pitch. |
| **Curiosity** | 8 | "¿Qué me sale si cruzo estos dos?" es una pregunta genuina y renovable. |
| **Endogenous Value** | 7 | Una criatura criada, nombrada, con linaje y mortal, tiene valor real. |
| **Character** | 7 | La tensión tierno/perturbador está presente. |
| **Resonance** | 7 | Criar, cuidar, perder. Material fuerte sin explotar. |

### Donde es débil

| Lente | Nota | Por qué |
|---|---|---|
| **Judgment** | 3 | Ganás o perdés sin poder atribuirlo a nada que hiciste. Un juicio que no se entiende no es juicio, es ruido. |
| **Skill vs. Chance** | 3 | Crit 20%, evasión, targeting uniforme en fila, roll del Agresivo 50%, Mareado 50%, muerte 5%. El dado más importante (la muerte) es el más arbitrario. |
| **Meaningful Choices** | 3 | La única decisión pre-pelea es la grilla, y se toma a ciegas. |
| **Elegance** | 4 | 12 estados de un uso, 2 canales, 4 elementos, 3 roles, afinidad, escudo. Muchas piezas, cada una con una sola función. |
| **Simplicity/Complexity** | 3 | Complejidad **innata** (reglas), no **emergente** (situaciones). El peor tipo. |
| **Flow** | 4 | Sin curva: no hay contra quién, y el ciclo dura horas. |
| **Fairness** | 4 | Permadeath por un 5% tras una pelea incomprensible = injusticia percibida. |
| **The Interest Curve** | 4 | Todos los beats pesan igual; sin picos no hay curva. |
| **Unification** | 3 | Siete frentes, casi cero acoples (medido en la nota 16). |
| **Transparency / Feedback** | 4 | No por falta de indicadores: por exceso sin jerarquía. |

### Veredicto

**Juego como está: 5/10 · Potencial de la idea: 8/10 · Pilar de combate: 3/10.**

El hallazgo que valida la intuición de Juan: **el combate es el único subsistema que puntúa mal en TODOS los lentes que Schell considera no negociables** (Judgment, Meaningful Choices, Skill vs Chance, Fairness, Elegance). Todo lo demás puntúa entre aceptable y bueno.

Diagnóstico en una frase: **el juego no está roto, está desintegrado.** Un juguete de 9 con un juez de 3.

---

## PARTE 2 — Por qué la pelea no se entiende

Verificado leyendo `CombatVisualizerService`, `CombatVisualEvents`, `CombatDamageNumbers` y `CombatOrderBarUITK`.

**Los indicadores NO faltan.** Hay 22 eventos en el bus visual: popups numéricos (daño, crítico, cura, escudo, veneno, quemadura, espinas, robo de vida, stun, reacción con nombre y color de elemento), burbujas de voz con hablante y objetivo, chips de elemento, pips de afinidad, barras de HP, fases de turno, cámara por unidad, y una barra de orden donde **cada una de las 6 cartas tiene 4 filas de información** (marcas aliadas, marcas enemigas, estados armados, 2 puntos de afinidad).

**El problema es lo contrario: hay demasiada información, sin jerarquía, y con la causa lejos del efecto.** Cinco causas:

**A · Exceso de estado simultáneo.** Hasta 8 marcas por unidad × 6 unidades, más 12 estados armados posibles, más afinidad, más escudo. [[Index/13 - Combat Design Direction]] dice literalmente *"El volumen alto de marcas es INTENCIONAL en esta etapa: legibilidad antes que balance"* — **esa frase es el bug de diseño**: se sacrificó balance para comprar legibilidad y el volumen de marcas destruyó la legibilidad que se quería comprar.

**B · La misma pareja significa dos cosas.** Agua × Fuego = Vaporizado (fuente aliada) o Boiling (fuente enemiga). Elegante en la tabla, imposible en pantalla: exige recordar de dónde vino cada marca. Es estado oculto puro.

**C · Causalidad no local ni inmediata.** El Agresivo marca a un aliado **al azar**; la segunda marca llega turnos después desde otra fuente; la reacción estalla en una tercera unidad; el estado se consume dos turnos más tarde ante un disparador específico. Causa y efecto separados en tiempo, unidad y zona de pantalla. Super Auto Pets logra lo contrario: resolución estricta adelante→atrás y efectos inmediatos y locales.

**D · El resultado es un dado.** Aunque se entendiera todo, la respuesta honesta a "¿por qué gané?" es "salió el crítico dos veces". Un sistema que se entiende pero no se puede influir no satisface más que uno que no se entiende.

**E · Sin jerarquía dramática.** Un escudo de +1 y la reacción que decide la pelea usan el mismo canal visual.

> **A, B, C y D son de diseño de sistema. Solo E es de presentación.** Por eso agregar más texto flotante es un parche: ataca la causa menos grave.

---

## PARTE 3 — La tesis del autobattler

> **En un autobattler la satisfacción de ver no viene del suspenso: viene de la CONFIRMACIÓN.**

Into the Breach, Opus Magnum y Super Auto Pets comparten que **el momento dramático es el commit, no la ejecución**. Cuando das play, ya creés saber qué va a pasar; mirar sirve para descubrir si tenías razón.

De ahí, dos criterios operativos que sirven de filtro para todo el rediseño:

**Criterio 1 — de la hipótesis.**
> Si el jugador no puede formular una hipótesis antes de la pelea, la pelea no puede ser satisfactoria de ver.

**Criterio 2 — del texto plano** (formulado por Juan con el ejemplo de Pokémon Rojo).
> Toda mecánica tiene que poder explicarse en una frase, sin tabla.

Darkest Dungeon pasa: *"este ataque se usa desde las posiciones 3-4 y golpea las posiciones 1-2"*. El sistema actual no pasa: requiere dos tablas y el concepto de "fuente de la marca".

---

## PARTE 4 — Géneros a saquear

| Referente | Qué resuelve | Qué robar | Qué NO |
|---|---|---|---|
| **Into the Breach** | Legibilidad total con profundidad real | Vida en hits · determinismo · **terreno como actor** · telegrafiado de intenciones | Input por turno |
| **Darkest Dungeon** | Que la formación SEA la estrategia | **Cada habilidad declara desde qué fila actúa y a qué fila pega.** Legible en texto puro | Su RNG brutal |
| **Mechabellum** | Autobattle con agencia real | Ver el ejército rival, desplegar, mirar | La escala |
| **Backpack Battles** | Sinergia espacial | **Adyacencia como motor** | El tetris de inventario |
| **Super Auto Pets** | "Un novato entiende por qué ganó" | Resolución estricta adelante→atrás · números de un dígito | Cero info del rival |
| **Advance Wars** | Terreno legible | Bonus de terreno como entero visible | Escala de campaña |
| **RoboRally / Mechs vs Minions** | Placer de ver un plan comprometido | El drama vive en el commit | El caos cómico |
| **Opus Magnum** | Ver tu solución correr da placer | El reencuadre: la ejecución es verificación | Todo lo demás |

**El patrón común: números chicos, resolución determinista, estado visible en el tablero. Ninguno tiene estado oculto acumulado.**

---

## PARTE 5 — Las dos palancas grandes

### Palanca 1 · Vida en HITS (idea de Juan)

No es balance: es lo que habilita todo lo demás. Con vida 100 y daño 12,7 nadie computa nada; con **3 hits** todos pueden.

Cascada de consecuencias:
- **Tablero contable de un vistazo**: 6 criaturas × 3 pips = 18 unidades de información total (hoy: 6 barras + 24 micro-widgets).
- **Los estados pasan a valer algo enunciable**: bloquear un golpe = un tercio de una vida. Hoy "+30% de daño al próximo golpe" no significa nada para nadie.
- **La muerte permanente se vuelve justa**: se ve al bicho en 1 hit. El dado del 5% deja de ser necesario — puede ser *"si termina en 0 hits, muere"*, consecuencia leída y no lotería.
- **Los stats natos se vuelven enteros con significado**: CON = hits que aguanta (2-5) · ATK = hits que pega / si rompe escudo · SPD = orden y quién actúa dos veces. El point-buy 18 pasa a ser presupuesto real.
- **El crítico deja de ser multiplicador** y pasa a ser "un hit extra".

**Lo que rompe:** `CombatStats`, el aporte de HP de las partes, el escudo, los 12 estados (hay que reexpresarlos en unidades de hit), `CombatRecord` y su serialización, la grilla y su UI, y la valuación económica.

### Palanca 2 · El TERRENO como segundo tablero

Responde de una sola regla a las tres cosas que pidió Juan (que el escenario importe, que la formación importe, que los estados se conserven cambiando su aplicación).

**El planteamiento:**
- La grilla 2-3-2 deja de ser solo posiciones: **algunas casillas tienen elemento** (charco, brasa, raíces, cable pelado). El escenario las define.
- **Colocar a la criatura es elegir qué terreno pisa.** Decisión visible del jugador.
- **Reacción aliada** = elemento de la criatura × elemento del terreno que pisa.
- **Reacción ofensiva** = elemento del atacante × elemento del terreno donde está la víctima.
- **Se acaban las marcas, la acumulación, los canales y la fuente. El tablero ES el estado.**

**Lo que gana:**
- **La tabla de 12 reacciones sobrevive intacta** — cambia solo dónde nace la marca. Es exactamente lo que Juan pidió.
- **Resuelve la objeción al canal único**: la ambigüedad de fuente se reemplaza por una decisión espacial. Ya no se ayuda al enemigo por accidente — el vapor sale porque *vos* pegaste fuego sobre *su* charco.
- **Pasa el test de texto plano**: *"Si pegás con fuego a alguien parado en agua, se evapora."*
- **El escenario es el oponente barato que no existe** (ver nota 16): no hacen falta miles de equipos rivales, hacen falta diez tableros interesantes.
- **La genética empieza a pagar en combate**: el elemento heredado deja de ser un color y pasa a ser "esta criatura sirve en mapas de agua".

**Sin resolver:** quién define el terreno de cada lado (¿simétrico? ¿lo fija el mapa? ¿el defensor?) y si el terreno persiste o se consume. Instinto del orquestador: **persiste** (es paisaje, no recurso) y es **asimétrico y visible**, para que leer el mapa sea una habilidad.

**Combina naturalmente con el perfil posicional de Darkest Dungeon**: cada rol declara desde qué fila actúa y a qué fila pega.

---

## PARTE 6 — Tres planteamientos completos

**A · "El puzzle de formación"** *(Into the Breach + Darkest Dungeon)* — Hits, determinismo total, terreno elemental, perfil posicional por rol, información perfecta antes de confirmar (incluida la intención de la primera ronda). La pelea es la verificación de tu solución.
*Riesgo:* un puzzle se resuelve una vez; exige rotación de escenarios.

**B · "El tablero de adyacencia"** *(Backpack Battles + Super Auto Pets)* — La sinergia nace de **quién está al lado de quién**, no del terreno. Los estados dejan de ser temporales: son propiedades permanentes de la formación. Resolución estricta adelante→atrás.
*Riesgo:* poco reactivo al rival (optimizás tu máquina contra el meta, como SAP). *Ventaja:* es el que mejor exprime la genética — el elemento heredado se vuelve pieza de encastre.

**C · "El plan comprometido"** *(Mechabellum + RoboRally)* — Ves el ejército rival completo, desplegás, mirás. Cada criatura lleva 2-3 prioridades cerradas y legibles alimentadas por los diales heredados (el gambit-lite de la nota 15).
*Riesgo:* el más caro, y exige un rival real que hoy no existe.

**Recomendación del orquestador (registrada, no decidida):** A como núcleo + la adyacencia de B como capa secundaria; C guardado para cuando exista el async 3v3. A da el oponente barato (escenarios), B hace que la genética pague, C es el techo y no el piso.

---

## PARTE 7 — Formatos de enfrentamiento y conductas emergentes

Lluvia de ideas pedida por Juan para destrabar la imaginación. Formato: **qué es** / *qué conducta emerge*. Las marcadas ⭐ son ideas de Juan.

### Cambian la FORMA del enfrentamiento

1. **Relevo** ⭐ — 1v1 secuencial, el ganador sigue herido. *Emerge:* el **orden** como decisión principal; nace el "abridor sacrificable" que ablanda y el dilema de guardar al mejor.
2. **Relevo con cláusula** — cada bicho lleva regla de retirada ("si me queda 1 hit, salgo"). *Emerge:* switching de Pokémon sin input; cobardes útiles y cebos.
3. **Tres carriles simultáneos** — tres duelos a la vez, gana 2 de 3. *Emerge:* apostar dónde va el fuerte; sacrificar un carril a propósito.
4. **Arena con grilla y movimiento** ⭐ — se mueven y actúan por reglas fijas. *Emerge:* cuellos de botella, kiting, control de esquinas. El mapa es el rival.
5. **Cadena de detonación** — cada unidad al actuar dispara a la siguiente. *Emerge:* placer Zachtronics de armar la máquina.
6. **Un solo golpe cada uno** — cada monchi actúa UNA vez. *Emerge:* ajedrez de tres piezas; máxima legibilidad posible.

### Cambian el OBJETIVO (lo que más mueve el meta)

7. **Rey de la colina** — ocupar una zona, no matar. *Emerge:* velocidad para llegar y peso para no ser desalojado valen más que el daño; los tanques dejan de ser aburridos.
8. **Sumo** — sacar al rival del ring. *Emerge:* masa, empuje, posición. **Es el formato que más reusa el motor físico ya construido** (ragdoll + knockback).
9. **Escolta** — un monchi frágil lleva algo al otro lado. *Emerge:* formación protectora real; el Protector pasa de stat a función.
10. **Espectáculo** — ganás entreteniendo al público (combos, estilo, rarezas), no matando. *Emerge:* builds vistosas sobre builds óptimas. **Convierte el problema ("ver es aburrido") en la métrica.** Encaja con un show de TV ochentero.
11. **Ring que se achica** — el tablero se contrae por rondas. *Emerge:* presión temporal sin timers artificiales.

### Cambian CONTRA QUIÉN competís

12. **Hordas paralelas** ⭐ — mismo escenario para todos, gana quien sobrevive más o más rápido. *Emerge:* competencia asíncrona **sin necesitar equipos rivales**; leaderboard estilo Balatro. Barato y compartible.
13. **Tower defense central** ⭐ — los 3 en el medio, oleadas alrededor. *Emerge:* cobertura angular; los roles se vuelven posiciones cardinales.
14. **Contra el entorno** — tormenta, incendio, plaga. *Emerge:* la composición elemental pasa a ser lectura de clima. Casa exacto con el terreno elemental.
15. **Asedio asimétrico** — un día atacás, otro defendés, con el mismo roster. *Emerge:* especialización; criar dos linajes.
16. **Herida persistente** — la vida NO se cura entre peleas de una tirada. *Emerge:* economía de hits a largo plazo; **hace que el permadeath se sienta ganado y no sorteado**.

### Los raros

17. **Carrera de obstáculos** — cruzan un circuito con trampas, no se pelean. *Emerge:* traits utilitarios y desastre cómico; es el clip compartible que la nota 15 dice que falta.
18. **Autobattler no violento** — resolver tareas de la tienda bajo presión. *Emerge:* sinergia pura; la tienda deja de ser un frente separado.
19. **Dinastía** — el resultado no es ganar, es qué aprende el sobreviviente. *Emerge:* criar por experiencia vivida, no por stats.

**Los tres que el orquestador marcaría para pensar en serio:** 7 (cambiar el objetivo cambia el meta con cero contenido nuevo), 10 (convierte el problema en la métrica), 12 (competencia real sin construir el oponente).

---

## Dónde retomamos

**Juan está escribiendo un manuscrito con sus conclusiones y lo va a entregar en una sesión futura.** Hasta entonces, nada de esto baja a código.

Decisiones abiertas que el manuscrito debería resolver:

- [ ] **¿Vida en hits?** Es la palanca que habilita todo lo demás, y la que más rompe.
- [ ] **¿Cuál es el formato de enfrentamiento?** (Parte 7 — la decisión más upstream de todas: cambia el objetivo del combate y con eso el meta entero.)
- [ ] **¿Los estados pasan a nacer del terreno, de la adyacencia, o del golpe?**
- [ ] **¿Planteamiento A, B, C o híbrido?**
- [ ] **¿Se saca el azar de la resolución** y se lo deja en la cría y en la tienda?
- [ ] **¿Cuántos estados sobreviven?** (Recomendación: no tocarlos hasta tener el formato, para recortar con criterio y no a ojo.)

Relacionado: [[Index/16 - Diagnostico por Frentes]], [[Index/15 - Theorycrafting S71 - Autobattler y Marketing]], [[Index/13 - Combat Design Direction]], [[Index/03 - Combat]].
