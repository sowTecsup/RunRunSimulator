---
tags: [index, design, combate, v3]
---

# 21 - Combate v3 — Dragon RPS (S92)

> **Sesión 92 (2026-09-01).** Juan entregó el mini-draft **DRAGON RPS V1 FINAL** y con él se cierra la refundación abierta en S91. Este documento reemplaza al prototipo táctico S77-S88 (`Index/20`), **demolido por completo en S93** — código, escena, assets, tools MCP, ScriptNodes y nota borrados (git `3cc5eb5` los conserva); lo que se aprendió vive en la memoria persistente del orquestador y en el timeline de [[Index/09b - Session Digest (S8-S88)]].
>
> **ESTADO: núcleo CERRADO Y VERIFICADO POR SIMULACIÓN.** Los perks son **exploración, no decididos**.
>
> **S93 (2026-09-01):** Juan cerró las decisiones abiertas de S92 — ver **Parte 8**. Cambios que prevalecen sobre lo de abajo donde contradigan: **el espejo parejo NO lastima a los dos (nadie golpea) y sin cartas se rebaraja el descarte** (Partes 1 y 3 ya corregidas); **PvP asíncrono primero + cooldown al perder + modo historia PvE** (Parte 7 reescrita); **breeding = looks + perks** como concepto clave; 3/3/3 descartado; la potencia por tipo crea un RPS de linajes.
>
> **Convención:** ⭐ = idea de Juan (fuente de verdad, no interpretar). El resto es lectura del orquestador. Los números vienen del simulador construido en esta sesión, no de opinión.

Relacionado: [[Index/17 - Refundacion del Combate]] · [[Index/15 - Theorycrafting S71 - Autobattler y Marketing]] · [[Index/18 - Pilares del Rediseno (Draft)]]

---

## PARTE 1 — El diseño ⭐

**1 dragón vs 1 dragón. El primero que mete 3 golpes gana.**

**RPS rígido:** Cuernos > Alas > Espalda > Cuernos. **El tipo con ventaja gana siempre**, sin importar la potencia.

**Deck de 6:** x2 de cada parte (2 Cuernos, 2 Alas, 2 Espalda). **Mano de 3 robada al azar** — no controlás lo que te toca. Cada ronda jugás 1 carta de tu mano, se gasta, va al descarte y robás otra. Si gastaste tus 2 Cuernos, no podés usar Cuernos nunca más en ese combate: **hay que contar**.

**Espejo (mismo tipo):** gana el de más Potencia. **Si las potencias están parejas, nadie golpea** (las dos cartas igual se gastan). **Si los dos se quedan sin cartas, se rebaraja el descarte y se sigue** con los golpes acumulados — no existe el empate. *(S93 ⭐: "en ningún caso se lastiman los dos"; el golpe mutuo de S92 fue un parche del orquestador y quedó revertido.)*

**El combate dura 3-5 rondas.**

### Por qué este diseño sí

Pasa los cinco filtros que sobrevivieron a S91 (ver [[Index/09 - Active Context]]):

| Filtro | Cómo lo cumple |
|---|---|
| Texto plano ([[Index/17 - Refundacion del Combate]] criterio 2) | *"Cuernos vencen Alas, Alas vencen Espalda, Espalda vence Cuernos. Si son iguales, gana el más fuerte; si están parejos, se lastiman los dos."* |
| Vida en hits (17 §5, Palanca 1) | 3 golpes, literal |
| Drama en el commit | elección simultánea en secreto, el choque es la verificación |
| Ciclo 20-40s | 3,47 rondas con el golpe mutuo de S92; con la regla S93 (nadie golpea + rebaraje) **6-7 rondas entre potencias iguales, 4,7 con perfiles distintos** — ver Parte 8 |
| La profundidad vive en la crianza | Potencia y perks heredados |

**Además cierra una pregunta abierta desde S73:** [[Index/18 - Pilares del Rediseno (Draft)]] §1.4 dejó marcado *"❓ABIERTO — qué hacen exactamente cuerno / espalda / alas en gameplay"*. Acá los tres genes de gameplay **son los tres ataques**. No afectan al combate: son el combate.

---

## PARTE 2 — Lo que la simulación corrigió

Se construyó un simulador (Parte 6) y se corrieron **~200.000 combates**. Tres hallazgos cambiaron el diseño.

### 2.1 · El reparto genético del deck está DESCARTADO

El orquestador propuso que la genética repartiera las 6 cartas (`4-1-1`, `3-2-1`, etc. — 10 repartos posibles con mínimo 1 de cada tipo). **La simulación lo mató.** Los 10 repartos, todos contra todos, en tres variantes de reglas:

| Variante probada | Brecha mejor vs peor | Reparto dominante |
|---|---|---|
| Potencia = cantidad de cartas | **28 puntos** (67% vs 40%) | especialistas `4-1-1` |
| Potencia plana | **39 puntos** (73% vs 34%) | equilibrado `2-2-2` |
| Potencia independiente del reparto | **43 puntos** (69% vs 26%) | los que alinean potencia con reparto |

Siempre hay un reparto dominante y el margen es enorme. Con reparto genético se crían dragones **objetivamente basura** (26% de winrate). **El 2/2/2 fijo de Juan es el único balanceado, por simetría.** La variedad genética entra por Potencia y perks, no por el reparto.

### 2.2 · "Si empatan las potencias, nadie golpea" rompía el juego

Medido con dos dragones parejos: **34% de rondas nulas** y **44,4% de los combates terminaban en empate**. Casi la mitad de las peleas sin ganador. Invisible en papel, obvio en 40.000 combates.

### 2.3 · El golpe mutuo lo arregla y mejora todo a la vez

| | empates | rondas nulas | duración | habilidad |
|---|---|---|---|---|
| "nadie golpea" | 44,4% | 34,0% | 5,17 | 55,7% |
| **"se lastiman los dos"** | **9,7%** | **0%** | **3,47** | **63,1%** |

Los empates caen a la quinta parte, las rondas muertas desaparecen, la pelea se acorta al rango objetivo **y la habilidad sube**.

> ⚠️ **Revertido en S93.** Juan no acepta que los dos se lastimen a la vez ("si ambos sacamos tijera gana la de más potencia; si empatamos en potencia, recién ahí es empate"). La regla vigente es la original + rebaraje del descarte cuando se acaban las cartas. Sus números están en la Parte 8; esta sección queda como registro de por qué el orquestador había propuesto el golpe mutuo.

### 2.4 · La Potencia es la fuente principal de habilidad, y es BINARIA

- Dragones parejos: el que cuenta el descarte gana **63,1%** de las partidas decididas.
- Un dragón con más potencia: **82,5% y cero empates**.

O sea: **criar se siente**. Pero `potencia 2 vs 1` y `potencia 3 vs 1` dan resultados **idénticos** — solo importa quién tiene más, no por cuánto. **La Potencia es un número de un dígito**; los `+10` / `-5` del draft de perks no tienen sentido en este sistema.

⚠️ **Riesgo abierto:** 82,5% es mucho. Si en la aventura te toca de sorpresa un rival con más potencia, perdés casi siempre. **Falta decidir qué brecha de potencia puede existir entre dos dragones.**

---

## PARTE 3 — Reglas v1 (cerradas)

1. Deck **2/2/2 fijo** para todo dragón. Mano de 3, robada al azar; jugás 1 y robás 1.
2. **Cuernos > Alas > Espalda > Cuernos**, sin excepciones.
3. Espejo → gana más Potencia → si están parejos, **nadie golpea** (ronda nula; las cartas se gastan) ⭐ S93.
4. **3 golpes gana.** Si los dos se quedan sin cartas, **se rebaraja el descarte al deck y se sigue** con los golpes acumulados ⭐ S93. No existe el empate.
5. Potencia: entero chico (1-3), por tipo, visible. Es **binaria por tipo**: solo importa quién tiene más en ese tipo (Parte 8).
6. **Sin permadeath** ⭐ (decisión S92). Al perder, la criatura entra en **cooldown** ⭐ S93.
7. Sin perks en v1 ⭐ S93 ("probemos primero sin perks y después iteremos") — ver Parte 5.
8. IA rival = **la contadora, sin carácter** ⭐ S93.

**Invariante que NO se puede violar nunca:** el descarte de ambos lados es **público y visible**. Contar lo que le queda al rival es el único motor de habilidad del sistema. RPS puro no tiene decisiones correctas (el óptimo matemático es tirar al azar); lo que convierte esto en un juego con habilidad es el **agotamiento con descarte público**. Cualquier regla, perk o presentación que esconda esa información destruye el juego.

---

## PARTE 4 — Presentación: la mano es el cuerpo

El requisito de Juan: *"no quiero que parezca un juego de cartas nada más"*. La solución es que **la mano no exista como UI**.

- **El recurso es anatomía y se rompe.** 2 púas en los cuernos, 2 membranas en las alas, 2 placas en la espalda. Cada ataque **quiebra esa parte en pantalla**. Cuando no quedan púas, no podés embestir.
- **El conteo se hace mirando al bicho.** El descarte público del rival es su propio cuerpo destrozándose. Cero HUD.
- **Elegís tocando la parte del cuerpo de tu dragón**, no clickeando una carta.
- **El reveal es un choque**, no un volteo de cartas: los cuernos perforan la membrana, las alas envuelven y levantan, el coletazo parte la cornamenta. 3 matchups × 2 direcciones + 3 espejos = pocas animaciones.
- **El material sale solo y es diegético:** las peleas dan material ([[Index/18 - Pilares del Rediseno (Draft)]] §1.3) y **el material son los pedazos rotos** — púas astilladas, membranas desgarradas. Se venden en la tienda o se forjan en Cutie Marks.
- Marcador de 3 golpes sin barra de vida: retroceso hacia el borde del ring (reusa ragdoll + knockback ya construidos; formato "sumo" de la 17 §7.8).

Encaja con el objetivo declarado de las Cutie Marks (18 §1.5): *"mirando al MoriMochi ya sabés qué clase de equipo tiene"*.

---

## PARTE 5 — Perks (EXPLORACIÓN, NO DECIDIDO)

Juan entregó un sistema de perks ⭐ y luego lo marcó como *"ideas"*. La estructura de rareza se conserva:

- **Común / Raro:** 1 efecto. O ayuda al ganar, o molesta al perder. Nunca ambos.
- **Muy Raro / Legendario:** 2 efectos, positivo al ganar + negativo al perder. **El premio gordo del breeding.**
- Todos se activan **después** de resolver el RPS.

Y una corrección de etiqueta: como los positivos se activan **al ganar**, no sirven para remontar sino para **cerrar**; los que remontan son los negativos.

### 5.1 · Dónde viven los perks

**Propuesta: el perk es del GEN, no de la carta.** Si cada una de las 6 cartas tuviera perk propio serían 6 tuyos + 6 del rival = 12 reglas activas — exactamente el diagnóstico que hundió al combate anterior (17 §2A). Con perk por gen son **3 por dragón**, se leen mirando al bicho, y criar sigue siendo elegir tus perks.

### 5.2 · Identidad por gen

| Gen | Dominio | Qué toca |
|---|---|---|
| **Cuerno** | el golpe | resolución del choque |
| **Espalda** | el aguante | recursos, hits, mazo |
| **Alas** | la lectura | información y tempo |

### 5.3 · Catálogo tentativo

**Cuerno — resolución**
- *Terco*: en espejo parejo, el rival se lastima y vos no.
- *Duelo*: los espejos parejos los ganás vos.
- *Astilla*: al perder, el rival pierde además una carta de ese tipo de su deck.

**Espalda — aguante**
- *Coraza*: el primer golpe que recibís no cuenta.
- *Rencor*: al perder, la carta que jugaste vuelve a tu mano.
- *Reserva*: al perder, robás una carta extra.

**Alas — lectura**
- *Ojo*: al empezar, ves 1 carta de la mano rival.
- *Finta*: al perder, el rival no puede repetir ese tipo la próxima ronda (el "Bloqueo" ⭐ original, que es el mejor perk del draft).
- *Ventaja*: al ganar, elegís qué carta robás en vez de que sea al azar.

### 5.4 · Reglas de admisión de un perk

Derivadas de lo que midió el simulador. Un perk **no puede**:

1. **Esconder información pública** — mata el motor de deducción. Descarta *Miedo* ⭐ (que además no describe nada: nadie ve la acción del rival antes de elegir, se eligen simultáneas en secreto) y obliga a que *Descarte* ⭐ muestre **qué** se descartó.
2. **Acumularse** — es la vuelta de las marcas invisibles de [[Index/13 - Combat Design Direction]]. Descarta *Afilado* ⭐ y *Sangrado* ⭐ en su forma acumulable.
3. **Romper el RPS** — descarta *Segunda Piel* ⭐ ("ignorá la derrota por tipo"): en cuanto una carta dice "excepto que no", se pierde la frase única. Reformulable como *"tu próxima derrota no te quita un golpe"*.
4. **Existir solo para contrarrestar otro perk** — descarta *Descanso* ⭐ (cura Sangrado): carta muerta el 80% del tiempo.
5. **Mover la potencia de a mucho** — es binaria: `+1` ya vale 82,5% de winrate y `+10` no hace nada más. Todo perk de potencia es de los más fuertes del juego y debe ser **raro**.

⚠️ *Terco* y *Duelo* otorgan ventaja de espejo, que es equivalente a ventaja de potencia. Deben ser raros y hay que **simularlos antes de fijarlos** — el harness ya existe.

### 5.5 · Recomendación de alcance

**6 perks en v1** (3 positivos, 3 negativos), no 10. Arrancar corto deja lugar para agregar donde el playtest muestre que falta, en vez de recortar a ojo.

---

## PARTE 6 — Qué está construido (S92)

`Assets/RunRunSimulator/Scripts/DragonRps/` — 7 archivos, ~463 líneas, **cero dependencias de `UnityEngine`**. Compila en Unity (0 errores, 0 warnings) y **también corre standalone con `dotnet`**, lo que permite probar una variante de reglas y tener resultados en 2 segundos sin abrir el editor.

| Script | Rol |
|---|---|
| `DragonRpsRules` | enum de acciones + tabla RPS + constantes (deck 6, mano 3, 3 golpes) |
| `DragonRpsDragon` | reparto y potencia por tipo; `Standard()` = el 2/2/2 canónico |
| `DragonRpsSide` | deck, mano, descarte y golpes de un lado; `RemainingByType()` = el conteo público |
| `DragonRpsBrain` | dos políticas: azar y **contadora** (calcula valor esperado contra lo que le queda al rival) |
| `DragonRpsMatch` | resolución de ronda y combate completo |
| `DragonRpsSession` | combate interactivo ronda a ronda (el modo jugable por texto) |
| `DragonRpsHarness` | entry points de simulación y log verboso |

Verificado con `execute_code` **dentro del editor**, no solo standalone.

---

## PARTE 7 — El marco (decisiones S92 ⭐, reescrito en S93 ⭐)

- **Contra quién (S93):** **PvP asíncrono primero** — tu dragón contra el **snapshot** del dragón de otro jugador, pilotado por la IA contadora; el síncrono se explora después. *"Algo sencillo."* Revierte la decisión S76 de matar el PvP por snapshot; el patrón async viejo (matchmaker + seed + buzón en Cloud Save, demolido en S75) vuelve a ser la referencia de infraestructura. Además, un **modo historia con algunos combates PvE**. *(S92 decía "PvE local, aventura con rival sorpresa" — superado.)*
- **Al perder (S93):** la criatura **pasa un tiempo en cooldown**. Reemplaza al permadeath y a la "herida persistente" que S92 dejó abierta.
- **Permadeath:** **quitada** (S92).
- **Breeding — concepto clave (S93 ⭐):** *"la idea del breeding sería para los looks y conseguir perks interesantes; después iteramos en más ideas que deriven de esto, pero esto es el key concept."* La potencia por tipo existe y pesa (Parte 8), pero el gancho de criar son la apariencia y los perks.
- **Qué produce:** **material** (18 §1.3, S92) — vender en la tienda o fabricar consumibles. *(Las Cutie Marks como salida quedan en suspenso: su código muerto se borró en la limpieza S93; si vuelven, se rediseñan.)*
- Vive en el bloque nocturno **23:00-6:00** del ciclo día/noche (18 §1.2).

**Requisito de información:** cuando aparece el rival **lo ves entero antes de elegir tu primera carta** (potencia por tipo y partes intactas). Si no, la ronda 1 es adivinanza pura y el criterio de la hipótesis (17 criterio 1) no se cumple.

---

## PARTE 8 — Decisiones y mediciones S93 (2026-09-01)

Juan respondió las 6 preguntas abiertas de S92. Lo medido salió del simulador scratch (`%TEMP%\DragonRpsSim`, fuera del repo; replica exacto los números de S92 antes de variar nada), 40.000 combates por celda.

### 8.1 · Deck 2/2/2 vs 3/3/3 — el 2/2/2 gana ⭐ (Juan pidió probar 3/3/3)

| Deck | El que cuenta gana (vs azar) | Empates (regla S92) | Rondas |
|---|---|---|---|
| **2/2/2, a 3 golpes** | **63,1%** | 9,7% | 3,47 |
| 3/3/3, a 3 | 58,3% | 13,2% | 3,51 |
| 3/3/3, a 4 | 60,6% | 11,0% | 4,84 |
| 3/3/3, a 5 | 62,2% | 7,9% | 6,14 |
| 4/4/4, a 3 | 55,5% | 13,3% | 3,51 |

Cuanto más grande el deck, menos vale contar (el descarte dice menos sobre lo que queda). El 3/3/3 necesita ir a 5 golpes para recuperar la habilidad del 2/2/2, al doble de duración. Se mantiene el 2/2/2 de Juan.

### 8.2 · Empates: de dónde salían y la regla vigente ⭐

Con el golpe mutuo de S92, el único empate posible era el **3-3 simultáneo** (2-2 + espejo parejo): 9,7% contra azar, **18,6% humano que cuenta el 70% contra la IA, 23% entre dos que cuentan**. "Gana la carta con más poder" ya era la regla; los empates que quedaban eran potencias iguales.

Juan revirtió el golpe mutuo. **Regla vigente: espejo parejo = nadie golpea (las cartas se gastan); si los dos se quedan sin cartas, se rebaraja el descarte y se sigue con los golpes acumulados.** Medida:

| Regla | El que cuenta (vs azar) | Rondas nulas | Rondas de media | Combates > 8 rondas |
|---|---|---|---|---|
| Golpe mutuo (S92, revertida) | 63,1% | 32,5% | 3,47 | 0% |
| **Nadie golpea + rebaraje (S93)** | 56,1% | 34,2% | 6,23 (humano vs IA: 6,69) | 15-21% |
| Nadie golpea + el combate vuelve a 0-0 | 55,3% | 33,9% | 9,34 | 44% |
| S93 con potencias distintas (2/1/1 vs 1/2/1) | 56,6% | 9,8% | 4,73 | 0% |

Lectura honesta: cero empates y regla legible, a cambio de que entre dragones de **igual** potencia 1 de cada 3 choques sea nulo, la pelea dure el doble y el conteo valga menos (el rebaraje lo borra). Con perfiles de potencia distintos (lo normal si el breeding reparte potencia) los nulos caen al 10% y la pelea a ~4,7 rondas. Variantes medidas y descartadas: "gana la parte más entera" en el espejo (48% de empates), "gana la más gastada" (16,7%), muerte súbita (0% empates, +0,6 rondas — Juan no la eligió).

### 8.3 · La potencia por tipo crea un RPS de linajes

Como la potencia es binaria por tipo (Parte 2.4), lo que importa es **en cuántos tipos sos más fuerte**:

| Potencias (A vs B, ambos cuentan) | A gana |
|---|---|
| 2/2/2 vs 1/1/1 (+1 en los tres) | **85,4%** — y un jugador ciego con +1 global le gana **71%** a uno que cuenta |
| 2/2/1 vs 1/1/1 (+1 en dos) | 74,7% |
| 2/1/1 vs 1/1/1 (+1 en uno) | 60,2% (3/1/1 da exactamente lo mismo) |
| 2/1/1 vs 1/2/1 (igual presupuesto) | **63,9%** — fuerte en Cuernos > fuerte en Alas |
| 2/1/1 vs 1/1/2 (igual presupuesto) | 35,8% — fuerte en Espalda > fuerte en Cuernos |

**Emergente:** con igual presupuesto, el fuerte en Cuernos le gana 64/36 al fuerte en Alas, que le gana al fuerte en Espalda, que le gana al fuerte en Cuernos — tu tipo fuerte anula el del rival al que vencés. No hay build dominante; criar decide dónde sos fuerte. Riesgo: la brecha **global** pasa por encima de la habilidad. Sugerencia para el asíncrono: emparejar por presupuesto total de potencia (suma) con tolerancia ±1.

### 8.4 · Código

`DragonRpsSide.Reshuffle()` + `DragonRpsMatch` (espejo parejo sin golpes; rebaraje cuando ambos se quedan sin cartas; `IsOver` solo por golpes). Sigue sin `UnityEngine`. `CombatPrototype/` fue **demolido** en esta sesión (código, escena, assets, tools MCP, ScriptNodes y `Index/20`).

---

## Decisiones abiertas (post-S93)

- [x] ~~Empates~~ → regla S93 (8.2). Queda por ver en playtest si las rondas nulas entre iguales molestan.
- [x] ~~Perks v1~~ → sin perks en v1; iterar después (deben aportar diversión y alargar la vida del juego vía breeding).
- [x] ~~Destino de `CombatPrototype/`~~ → demolido S93.
- [x] ~~Carácter de la IA~~ → contadora sin carácter.
- [x] ~~Herida persistente / peleas por noche~~ → cooldown al perder.
- [ ] **Cooldown:** ¿cuánto dura, se acorta con cuidado (el agujero "que el cuidado pague" de la 15/16), y qué pasa con el snapshot del defensor cuando lo derrotan?
- [ ] **Matchmaking asíncrono:** ¿por presupuesto total de potencia ±1 (8.3)? ¿qué se guarda en el snapshot (potencias + perks + look)?
- [ ] **Modo historia:** alcance de los "algunos combates PvE".
- [ ] **Infra async:** reusar el patrón S-antiguo (matchmaker + seed + buzón) — los endpoints de Cloud Code viejos siguen por despublicar.
- [ ] Presentación (Parte 4): sin golpe mutuo ya no hay "los dos caen"; la ronda nula es un choque donde nadie cede.
