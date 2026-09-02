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

---

## PARTE 9 — Plan de la DEMO JUGABLE dentro del juego (E1-E5) — aprobado por Juan al cierre de S93, arranca en S94

> Pedido textual de Juan (S93): *"preparemos las siguientes etapas para tener la demo de este prototipo; quiero que ejecutes la paleta de diseño que tenemos a lo largo del juego y auditorías cada etapa"*. Juan se fue antes de responder las 3 preguntas de cierre (potencia = Tier, OKs de mutación, escribir el plan): el "Sí documentá todo esto" vale como OK para el plan; **las decisiones de 9.1 son defaults del orquestador, vetables al abrir S94**. Este texto está escrito para que S94 arranque directo en E1 sin volver a explorar nada.

### 9.0 · Inventario verificado en S93 (NO volver a explorar; todo confirmado por grep/lectura)

**Paleta = `Assets/RunRunSimulator/UI Toolkit/Theme.uss`** (39 líneas, solo variables, clase `.mm-theme`). Los 17 tokens del modo día:

| Fondo/superficie | Marco/tinta | Acentos | Semánticos |
|---|---|---|---|
| `--mm-bg` 234,211,174 · `--mm-bg-deep` 221,191,147 · `--mm-surface` 251,240,220 · `--mm-surface-2` 243,225,196 | `--mm-frame` 94,62,41 · `--mm-frame-soft` 185,138,99 · `--mm-ink` 62,42,29 · `--mm-ink-soft` 138,106,82 · `--mm-scrim` rgba(62,42,29,.45) | `--mm-coral` 239,100,64 · `--mm-coral-dark` 194,68,40 · `--mm-teal` 31,158,138 · `--mm-gold` 233,161,31 · `--mm-plum` 158,79,178 | `--mm-good` 90,166,70 · `--mm-warn` 231,146,51 · `--mm-crit` 220,74,56 |

Coinciden exacto con los 5 colores del kit "Diario del Pet Shop" de [[Index/14 - Art Prompts]] (coral `#EF6440`, teal `#1F9E8A`, gold `#E9A11F`, papel `#FBF0DC`, tinta `#3E2A1D`): **el USS es la paleta canónica**. Existe **`.mm-theme--night`** (mismos 17 tokens en oscuro: `--mm-bg` 36,26,25, `--mm-ink` 244,227,206, `--mm-scrim` rgba(0,0,0,.55)) y **nadie la usa** — el combate vive de 23:00 a 6:00 (18 §1.2), así que el panel la estrena. No hay fuente custom (todo `UnityDefaultRuntimeTheme.tss`; solo `-unity-font-style: bold` y `best-fit`). No hay tokens de rareza: `BodyPart.RarityColor(Rarity)` en C#. Formas recurrentes: marco de panel/botón **3px `--mm-frame`**, internos 2px; radios **8** (pill/tab), **10** (botón), **12-16** (panel chico), **22** (panel grande); títulos con `letter-spacing: 4px`; modal = `position:absolute` a 0 en los 4 lados + `--mm-scrim` + centrado. Tamaños: 11-12 labels, 14-16 cuerpo, 18-20 subtítulo/tab, 28-30 título/cifra. Pixel art del kit (marco 9-slice, washi, sellos): **todavía no existe ninguna pieza**; hoy la identidad son tokens + formas.

**Clases reutilizables** (copiar, no inventar): de `TransactionPanel.uss` — `.tx-backdrop` (scrim modal), `.panel` (`--mm-surface`, 2px, radius 16, `width:640px; max-width:92%`), `.panel__header`/`.panel__title` (barra `--mm-surface-2`, título gold spaced), `.col`/`.col__label`, `.divider`, **`.mm-swatch`** (64px radius 14: el slot de retrato que llena `MonchiPortraitUI.Apply`), `.price-tag`/`.offer`, **`.actions` + `.action` + `--accept` (good) / `--cancel` (crit) / `--more` (gold) + `.action:disabled {opacity:.40}`** — la fila de botones de decisión que usan Cuernos/Alas/Espalda. De `CreatureGridUITKStyle.uss` — `.card` (+`:hover`, `--selected` borde coral), `.card__name`, `.card__icon`, `.card__state`. De `MorimonchiDetailInfoUITKStyle.uss` (599 líneas, el catálogo más rico) — tabs pill (`.unity-tab__header` radius 8, `:checked` plum), `.stat--con/atk/spd/def/lck/eva`, `.section-title`, `.part-row`/`.part-swatch`/`.part-text`, y **`.combat-card` + `--win/--lose/--draw` + `__header/__badge/__body/__opponent/__swatch/__chips/__tierchip/__stat/__footer/__date` (líneas ~230-400): CSS HUÉRFANO del 3v3, listo para la tarjeta de resultado**. PanelSettings: `StandartPanelSettings.asset` (Scale With Screen Size, 1920×1080, match alto) — lo usan los 10 `UIDocument` de `GameScene`.

**Infraestructura de paneles**: `UIPanelType` (`Core/Enums/UIEnums.cs`) = `None 0, CreatureGrid 1, MorimonchiDetail 2, Breeding 3, Storage 5, Store 6, Transaction 7` → **el 4 está libre** (era `Combat`). En `GameScene.unity` el `UIManager` (`SerializedMonoBehaviour`, dict `panels` llenado a mano en el inspector) **todavía mapea la clave 4 al GameObject `UIManager/CombatPanelUITK`** (fileID `1051803673`), que tiene un `UIDocument` con `StandartPanelSettings` y `sourceAsset` apuntando a un guid muerto (`6cdc177fbc58e9a4ca573f802dc2369e`, el UXML borrado). **Un slot de panel entero esperando un UXML.** API de `UIManager` (bus estático): `RequestPanelToggle(UIPanelType)`, `RequestPanelSet(type, bool)`, `RegisterNavigable(type, IUINavigable)`/`UnregisterNavigable`, `SelectCreature(dna, registry)`; stack LIFO; `IUINavigable` = `OnUINavigate(Vector2)`, `OnUISubmit()`, `bool OnUICancel()` (true = consumido). `PanelTrigger` (16 líneas, `IInteractable`, campo `UIPanelType panel` → `RequestPanelToggle`). Ciclo de vida molde = `StoragePanelUITK.cs`: `Start()` → Q<> + `Loc.Tr` + `clicked +=` + `RegisterNavigable`; `OnDestroy()` → `-=` + `Unregister`. Construcción estándar = UXML con `<Style src="Theme.uss"/>` + `<Style src="Propio.uss"/>` y raíz `class="mm-theme ..."`; filas repetidas por código con `AddToClassList`. Árbol 100% por código (`EquipmentBackpackUITK`) exige `AddToClassList("mm-theme")` + `styleSheets.Add(themeStyleSheet)` o las `var(--mm-*)` no resuelven. Recetario obligatorio: [[Index/05 - UI System]] (flex-grow en 3 niveles de TabView, sizing por `GeometryChangedEvent`, hot-reload de USS en Play huerfaniza el árbol → reiniciar Play).

**Retratos**: `MonchiPortraitUI.Apply(VisualElement, CreatureDNA)` (estático, one-liner, funciona sin spawnear; cache por `UniqueID` con fallback `ToStringID()`; si falla pinta `BaseColor`). `ApplyLive(...)` solo si la criatura está spawneada (`MoriMochiSpawner.SpawnedEntries`). La cabina `MonchiPortraitStudio` (`Booth` + `BoothCamera` en escena) es el molde de "dragón visual-only": GameObject con `MonchiVisualizer` (+ `modelRoot`) → `SetBank(MonchiVisualBankSO)` + `SetFurDatabase(FurTypeDatabaseSO)` + `Assemble(dna)` + `SetMood(MonchiMood)`.

**Dragones**: `MonchiVisualizer.Assemble` instancia UN cuerpo entero (`bank.GetBody(BodyShapeID)` por hash sobre 4 FBX `MonchiBody_A..D`) y tinta sub-renderers **por nombre**: `Wing*` → color wing, `Horn*`/`Back*` → accent, `Teech`, `Face` (material por mood), resto `BaseColor`; `Tint()` usa `MaterialPropertyBlock` (`_BaseColor`, `_1st_ShadeColor`, `_2nd_ShadeColor`, `_RimLightColor`). **No es modular por partes**: `HornID/BackID/WingID` no eligen mallas → "la parte se rompe" se hace por **tinte/escala del renderer por prefijo**, no por swap de malla. `DragonAnimationDriver` (`[Required] MonchiVisualizer`; único implementador de `MonchiAnimationDriver`): `IsBusy`, `MoveTo(dest, onArrived)`, **`PlayAttack(targetPos, onImpact, onFinished)`** (pullback → `FlyUp` → `Fly` hasta `strikeDistance` → `FlyFire` → `onImpact()` a 45% del clip → hit-stop `Anim.speed=0` 0,12 s → vuelve → `FlyDown` → `Idle` → `onFinished()`), `PlayHit(intensity)` (mood Dolor + `Damage`; ignora `intensity`), `PlayBuff(onFinished)` (`Yes`), `PlayDefeat()` (`Die`, sin callback), `PlayVictory()` (`Roar` + **loop infinito de `Jump` → cortar con `PlayIdle()`**), `PlayIdle()`, `SetTimeScale(0.25-4)`. Estados del `MonchiAnimator.controller` = los strings exactos (`Idle, Walk, Run, Jump, Fly, FlyUp, FlyDown, FlyFire, Fire, Roar, Damage, Die, Yes, No, Eat, Rest, Sick`). `MonchiLocomotionAnimator` y `MonchiMoodDriver` **ceden si `combatDriver.IsBusy`**. Único prefab de dragón: `Resources/Prefabs/MorimonchiAgent.prefab` (paquete completo con NavMesh; `MonchiLocomotionAnimator` tiene `[Required] navAgent` → para el ring NO usar variante, armar a mano como la cabina). **→ `DragonAnimationDriver` sale de la lista de borrado de `Index/11`.**

**Datos**: `CreatureDNA` — `HornID/BackID/WingID/FaceID/BodyShapeID`, `BaseColor`, `CustomName`, `UniqueID` (vacío hasta `Stamp()`), **`Tier HornTier/BackTier/WingTier/BodyTier` (`Tier1=1, Tier2=2, Tier3=3`)**, `BusyReason BusyState` (`IsBusy`, `IsSold`), `IsDead`, `Needs` (`Health/Energy/Affect`, `AddEnergy`), `BreedReadyAt` (**long ticks — patrón para el cooldown**), `Equipped`. `BodyPart` (SO): `Rarity` (Common..Legendary 0-4), `Tier`, `HP/Attack/Speed` 0-10. `PlayerInventorySO`: `adventureMaterial/passiveMaterial/evolutionEssence` con **solo getters** (los `Add*/Spend*` se borraron en S93 por muertos → **reponer `AddAdventureMaterial` copiando el patrón de `AddDabloons`**: guard `amount<=0`, `MarkDirty()`); acceso `GameManager.CurrentInventory`; eventos `GameEvents.InventoryChanged(inv)`. `CreatureRegistrySO.GetAll()` devuelve **copia** sin filtrar (filtrar `!IsDead && !IsSold && !IsBusy`). `GameManager.Now` = hora de servidor. `DevToolsConsole` (`Core/`, MonoBehaviour en escena con botones Odin `[Button] + BoxGroup`, ref `gameManager`) = el lugar para un botón "Open Combat Panel (DEV)".

**Restos aprovechables**: `Resources/Prefabs/Arena/Podium.prefab` (cilindro ProBuilder gris, 0 refs, `Resources.Load` directo) y `DefaultDamageNumberPo.prefab` (DamageNumbersPro, 0 refs; el paquete entero está en `Assets/DamageNumbersPro/` sin usar). `Assets/Feel/` completo y sin un solo `MMF_Player` en el proyecto; slot de inspector reservado en `MoriMochiAgent.cs` ("Feedbacks (Feel-ready)"). **No existe reloj día/noche** en código.

**Localización**: `Loc.Tr(key)` / `Loc.Tr(key, args)` sobre la tabla única `Strings` (`Assets/RunRunSimulator/Localization/Strings/Strings Shared Data.asset` + `Strings_en.asset` + `Strings_es.asset`, 366 claves); clave inexistente → devuelve la clave (se ve en pantalla). Se agregan desde la ventana Localization Tables (asigna `m_Id`). Convención `ui.<panel>.<elemento>`; enums por `LocEnumMaps`. `DragonRpsRules.Name()` está en español hardcodeado: **queda así** (la clase es pura, la usa el harness); la UI traduce con `ui.rps.action.*`.

**Gotchas heredados**: `Horn/Back/Wing/Face`DatabaseSO tienen **0 assets** (pendiente S75) → no se puede generar un rival tirando partes al azar: el rival se **clona** de una criatura del registro. Las 18 criaturas del registro vienen con tiers `Tier1` por defecto salvo que Juan las haya tocado → en E1 verificar la distribución real de tiers antes de medir.

### 9.1 · Decisiones de diseño de la demo (defaults del orquestador, vetables)

| # | Decisión | Default | Por qué |
|---|---|---|---|
| D1 | **Potencia por tipo** | ~~`Power = (int)dna.HornTier/WingTier/BackTier` (1-3)~~ **Reemplazada en S95 por el Potencial (§9.10):** `Power[Horns] = dna.HornPotential`, `Power[Wings] = dna.WingPotential`, `Power[Back] = dna.BackPotential` (1-10). Presupuesto = suma (3-30). | Juan corrigió el supuesto: ninguna parte es intrínsecamente mejor (`Tier` era un campo muerto: nadie lo asignaba ni lo heredaba). Lo que se hereda es el potencial de cada parte. |
| D2 | **Rival de la demo** | Clon de una criatura viva del registro distinta de la elegida: `CreatureDNA.FromID(src.ToStringID())` (partes + color), `CustomName` generado ("Salvaje" + nombre), `BaseColor` nueva por `ColorGenetics`, tiers re-tirados hasta que `Budget(rival)` esté en `Budget(jugador) ± BudgetTolerance`; **sin `Stamp()` ni `Register`** (no entra al registro; el retrato cachea por `ToStringID()`). Seed determinista `unchecked((int)(player.Timestamp ^ GameManager.Now.Ticks))`. | Las databases de partes están vacías; el clon da retrato y look reales. Determinista = replay/async después. |
| D3 | **Cooldown al perder** | `CreatureDNA.CombatCooldownUntil` (long ticks UTC, mismo patrón que `BreedReadyAt`); `CombatTuningSO.CooldownMinutes = 20` (tiempo real, `GameManager.Now`). Se persiste por `GameEvents.RegistryChanged`. | Decisión S93 de Juan; "¿el cuidado lo acorta?" queda para después. |
| D4 | **Premio** | Victoria → `inventory.AddAdventureMaterial(t.MaterialPerWin = 3)` + `InventoryChanged`. Derrota → cooldown. Sin permadeath, sin tocar `Needs`. | Parte 7 "qué produce: material". |
| D5 | **Elegible para pelear** | `!IsDead && !IsSold && !IsBusy && CombatCooldownUntil <= Now.Ticks` (+ `Needs.Energy >= t.MinEnergyToFight`, default 0 = apagado). | Coherente con breeding. |
| D6 | **IA rival** | `DragonRpsPolicy.Counting`, sin carácter. | Decisión S93. |
| D7 | **Qué muestra la UI** | Tu **mano** = hasta 3 botones `.action` (una carta = un botón; dos Cuernos = dos botones "Cuernos"); **intactas por tipo** de ambos (`RemainingByType()`, el descarte público); potencias de ambos (3 números); golpes como 3 pips; log de una línea por choque. Nada se esconde (invariante Parte 3). | Texto plano + descarte público. |
| D8 | **Sin evento nuevo** | El panel es dueño de la `DragonRpsSession`; `DragonRpsService` emite `InventoryChanged`/`RegistryChanged` al resolver. No se agrega `OnCombat*` a `GameEvents` hasta que alguien lo escuche (regla S93). | CLAUDE.md eventos. |
| D9 | **Tema** | Raíz del panel `mm-theme mm-theme--night rps`. | Bloque nocturno. |
| D10 | **Cerrar a mitad del duelo (demo)** | Abandonar = sin premio y sin cooldown; el duelo se descarta. Al cerrar desde el resultado, se aplica lo resuelto. | Simple para la demo; en async será derrota. |
| D11 | **Dónde vive la tuning** | `CombatTuningSO : ScriptableObject` (sin diccionarios → sin Odin) en `ScriptableObjects/Combat/CombatTuning.asset`; referencia serializada en `CombatPanelUITK` (E2) y después en `GameManager` cuando la necesite el ring (E4). | Regla 4: SO = data; una sola forma de exponerla. |

### 9.2 · E1 — Puente datos → combate (solo código; 1 asset)

**Carpeta nueva** `Assets/RunRunSimulator/Scripts/Systems/Combat/` (namespace `MoriMonchiSimulator`, usa `MoriMonchiSimulator.DragonRps`):

| Archivo | Tipo | Responsabilidad (una sola) | Contrato |
|---|---|---|---|
| `DragonRpsGenes.cs` | `static class` | DNA → `DragonRpsDragon` | `DragonRpsDragon ToDragon(CreatureDNA dna)` (nombre = `CustomName`, `Standard` 2/2/2, `Power` por D1) · `int Budget(CreatureDNA dna)` · `bool CanFight(CreatureDNA dna, CombatTuningSO t, DateTime now)` (D5) |
| `DragonRpsRival.cs` | `static class` | Genera el rival de la demo (D2) | `CreatureDNA Generate(CreatureRegistrySO registry, CreatureDNA player, CombatTuningSO t, System.Random rng)` — devuelve null si no hay candidatos |
| `DragonRpsService.cs` | `static class` | Orquesta y APLICA el resultado (dueño de la mutación) | `DragonRpsSession Start(CreatureDNA player, CreatureDNA rival, int seed)` · `CombatOutcome Resolve(DragonRpsSession session, CreatureDNA player, CreatureRegistrySO registry, PlayerInventorySO inventory, CombatTuningSO t, DateTime now)` (D3/D4 + eventos) · `int Seed(CreatureDNA player, DateTime now)` |
| `CombatOutcome.cs` | `struct` | Resultado ya aplicado | `bool Won; int HitsPlayer; int HitsRival; int Rounds; int MaterialGained; long CooldownUntilTicks` |
| `CombatTuningSO.cs` | `ScriptableObject` (`[CreateAssetMenu(menuName = "RunRunSimulator/Combat/Tuning")]`) | Data | `int CooldownMinutes = 20; int MaterialPerWin = 3; int BudgetTolerance = 1; float MinEnergyToFight = 0;` (E5 agrega `StoryRivals`) |

**Modificados**: `Data/Genetics/CreatureDNA.cs` (+ `public long CombatCooldownUntil;` junto a `BreedReadyAt`) · `Data/Player/PlayerInventorySO.cs` (+ `AddAdventureMaterial(int)` con el patrón de `AddDabloons`; `SpendAdventureMaterial` recién cuando algo lo gaste) · `Core/DevToolsConsole.cs` (+ botón "Simulate Combat (DEV)": 5 combates por código entre dos vivas, log del outcome — es la auditoría funcional de E1 a mano).

**Mutación (OK de Juan)**: crear `Assets/RunRunSimulator/ScriptableObjects/Combat/CombatTuning.asset` por MCP `manage_scriptable_object`/`create_asset`.

**Reparto en coders**: (1) `DragonRpsGenes` + `CombatOutcome` + `CombatTuningSO` + campo en `CreatureDNA`; (2) `DragonRpsRival`; (3) `DragonRpsService` + `AddAdventureMaterial`; (4) botón en `DevToolsConsole`. Todos sin comentarios, sin `Find*`, sin nuevas suscripciones.

**Auditoría E1**: compila 0/0 · `eval` en el editor: tomar 2 vivas del registro, `Start` + `Play(0)` hasta `Finished` + `Resolve` × 5 seeds → verificar `AdventureMaterial` sube en victorias, `CombatCooldownUntil` se setea en derrotas y **se persiste** (leer `creature_database.json` tras `RegistryChanged`), rival nunca registrado (`registry.Count` no cambia) · distribución real de tiers de las 18 criaturas (si todas son Tier1, todas las potencias son 1/1/1 y el rival también → 34% de rondas nulas: decidir si el dev button re-tira tiers) · harness intacto (56,1% / 82,5% / 0%) · reglas de la casa (≤400 líneas, dominios, eventos).

### 9.3 · E2 — Panel con la paleta (tema nocturno)

**Código**: `Core/Enums/UIEnums.cs` (+ `Combat = 4`) · `UI/CombatPanelUITK.cs` (MonoBehaviour, `IUINavigable`; campos serializados `UIDocument document`, `CombatTuningSO tuning`, `CreatureRegistrySO registry` o `GameManager` por `Instance` en `Start`; estado `Pick → Duel → Result`; dueño de la `DragonRpsSession`; `RegisterNavigable(UIPanelType.Combat, this)`) · 3 presenters planos en `UI/`: `CombatPickPresenter` (lista de elegibles; los que no, apagados con motivo), `CombatDuelPresenter` (`Rebuild(session)` tras cada choque), `CombatResultPresenter` (`Show(outcome, player, rival)`).

**UXML `UI Toolkit/CombatPanelUITK.uxml`** (raíz `name="rps-root" class="mm-theme mm-theme--night rps"`, estilos `Theme.uss` + `CombatPanelUITKStyle.uss`):
```
rps-root
└─ rps-backdrop (.tx-backdrop)
   └─ rps-panel (.panel .rps-panel)
      ├─ header (.panel__header) → rps-title (.panel__title) [ui.rps.title]
      ├─ view-pick        → pick-list (ScrollView) de .card (card__icon retrato · card__name · card__state: potencia "2·1·3" o motivo) · pick-fight (.action.action--accept) [ui.rps.pick.fight]
      ├─ view-duel        → side-player (.rps-side): portrait (.mm-swatch) · name · power (3 chips .rps-power) · intact (3 contadores .rps-intact) · hits (3 .rps-pip)
      │                    → clash (.rps-clash): log (Label .rps-log) · hand (.actions) con hasta 3 .action.rps-action [ui.rps.action.horns/wings/back]
      │                    → side-rival (.rps-side) idéntico, sin hand
      └─ view-result      → .combat-card.combat-card--win|--lose: result-title [ui.rps.result.win/lose] · result-line [ui.rps.result.material / cooldown] · btn-again (.action.action--more) · btn-close (.action.action--cancel)
```
**USS nuevo (`CombatPanelUITKStyle.uss`)**: solo clases `rps-*` con `var(--mm-*)`: `.rps-panel {width: 900px; max-width: 94%}`, `.rps-side {flex-basis:0; flex-grow:1}`, `.rps-power` (chip radius 8, fondo `--mm-surface-2`, número 18 bold gold), `.rps-intact` (tres columnas Cuernos/Alas/Espalda con "×N", el 0 en `--mm-crit` y tachado), `.rps-pip` (círculo 18px borde 2px `--mm-frame`; lleno `--mm-coral` = golpe), `.rps-action` (botón 3px, 20px bold, `--mm-teal` cuernos / `--mm-plum` alas / `--mm-gold` espalda **solo como color de borde**, texto `--mm-ink`), `.rps-log` (16px, `--mm-ink-soft`, una línea, `best-fit`). Nada de colores nuevos.

**Claves Loc** (agregar en la ventana Localization, en/es): `ui.rps.title` (PELEA NOCTURNA), `ui.rps.pick.title`, `ui.rps.pick.fight`, `ui.rps.pick.cooldown` ("{0}: en recuperación hasta {1}"), `ui.rps.pick.busy`, `ui.rps.action.horns/wings/back`, `ui.rps.hand` (Listas), `ui.rps.intact` (Intactas), `ui.rps.power` (Potencia), `ui.rps.hits`, `ui.rps.rival`, `ui.rps.round.win` ("{0} rompe {1}"), `ui.rps.round.mirror` ("Espejo: potencia {0} contra {1}"), `ui.rps.round.null` ("Nadie cede"), `ui.rps.reshuffle` ("Se rearman"), `ui.rps.result.win` (VICTORIA), `ui.rps.result.lose` (DERROTA), `ui.rps.result.material` ("+{0} material de aventura"), `ui.rps.result.cooldown` ("{0} descansa hasta {1}"), `ui.rps.again`, `ui.rps.close`, `ui.rps.abandon`.

**Navegación** (`IUINavigable`): en Pick, ← → mueven la selección (`UiPanels.SetActiveIndex`), Submit = pelear; en Duel, ← → mueven entre los botones de la mano, Submit = jugar la carta; Cancel en Duel = abandonar (D10), en Pick/Result = cerrar (`RequestPanelSet(Combat, false)`). Ratón: `clicked` en cada botón.

**Abrir hasta E3**: botón "Open Combat Panel (DEV)" en `DevToolsConsole` → `UIManager.RequestPanelSet(UIPanelType.Combat, true)`.

**Mutaciones (OK de Juan)**: asignar el UXML nuevo al `UIDocument` de `UIManager/CombatPanelUITK` (`sourceAsset`), asignar `CombatTuning.asset` y `document` en el componente `CombatPanelUITK` que se agrega a ese mismo GameObject. Todo por MCP `manage_components`/`set_serialized_field` + verificación leyendo el campo.

**Auditoría E2**: compila 0/0 · abrir por código en Play (`UIManager.RequestPanelSet(UIPanelType.Combat, true)` vía `execute_code`) y jugar 5 duelos completos disparando `Play(i)` del presenter (exponer `internal` para test) · **3 capturas MIRADAS** (`unity command capture_game_view --source screen --save_path Assets/Screenshots/s94_e2_pick.png` / `_duel` / `_result`) a 1280×720 y 1920×1080 con checklist: solo tokens `--mm-*` (grep del USS), sin overlaps, glifos presentes (la fuente default no tiene ☠/×: usar texto), potencias e intactas legibles a distancia, estado de cooldown claro, botones de la mano deshabilitados cuando no corresponde, **test del texto plano** ("¿podés decir en una frase por qué ganaste?" mirando solo la pantalla) · navegación por teclado completa sin ratón · ScriptNodes + `Index/05` (fila nueva del panel).

### 9.4 · E3 — Entrada en el mundo + loop

**Mueble Ring** (clon del patrón `Furniture3x3_BreedingRoom.asset`): `FurnitureDefinitionSO` en `ScriptableObjects/FurnitureSystem/Furniture3x3_Ring.asset` (`DisplayName` "Ring", `Footprint` 3x3, `Category` 2, `Price` a definir) + prefab `Resources/Prefabs/Furnitures/Containers/Ring.prefab` (raíz con `PlacedFurnitureMarker` lo estampa el spawner; hijo `Podium` = `Podium.prefab`; componente `PanelTrigger` con `panel = Combat` sobre un collider interactuable; hijo vacío `Stage` con 2 `Transform` `SpotPlayer`/`SpotRival` para E4) + alta en `FurnitureDatabase.asset` (arrastrar al `dropBuffer` + `PopulateFromBuffer`) + entrada en el catálogo de la tienda (`ShopCatalogSO`). **Todo esto es mutación de assets → OK de Juan** (es la etapa con más MCP).

**Loop visible**: material en el overlay (`InfoOverlayUITK`: label junto a los dabloons, clave `ui.overlay.material`); cooldown en la ficha (`DetailInfoTabPresenter`: línea "en recuperación hasta HH:MM" cuando `CombatCooldownUntil > Now`) y en las cards (`CreatureDisplay.StateOf` gana el caso cooldown con clave `status.cooldown` — es la única función de estado, no duplicar).

**Auditoría E3**: comprar el Ring en la tienda, colocarlo en build mode, interactuar (E) → abre el panel; pelear 3 veces; salir y volver a entrar al juego → cooldown y material persisten (JSON + Cloud: `[CloudSync] Pushed`); NavMesh rebakea con el mueble (los agentes lo rodean); capturas del Ring colocado (de día y de noche del tema del panel) MIRADAS.

### 9.5 · E4 — "La mano es el cuerpo" (Parte 4)

**`RingStage.cs`** (`World/Combat/`, MonoBehaviour en `Ring.prefab`): en `Begin(playerDna, rivalDna)` arma dos dragones visual-only a mano (GameObject vacío + `MonchiVisualizer` con `modelRoot` + `DragonAnimationDriver`; `SetBank/SetFurDatabase/Assemble`; mood `Neutral`) en `SpotPlayer`/`SpotRival` mirándose; en `End()` los destruye. El panel (E2) le pasa cada choque: `PlayClash(DragonAction mine, DragonAction theirs, RoundOutcome outcome)` → ambos `PlayAttack(posición del otro, onImpact, onFinished)`; en `onImpact` del atacante ganador el perdedor hace `PlayHit`; choque nulo = ninguno; fin → `PlayDefeat`/`PlayVictory` (+ `PlayIdle` al cerrar para cortar el loop de `Jump`). **Partes rotas**: método nuevo `MonchiVisualizer.SetPartWorn(PartRole role, int intact, int total)` que atenúa (tinte hacia `--mm-ink` y escala 0,85) los renderers cuyo nombre empieza por `Horn*`/`Wing*`/`Back*` — usa la misma clasificación por prefijo que `ApplyLook`. El panel reduce su HUD: se quedan potencias, pips y la mano; las "intactas" pasan a leerse en el cuerpo (y siguen como número chico por legibilidad — regla "legibilidad primero").

**Cámara del ring**: `CinemachineCamera` hija del prefab, activa solo durante el duelo (prioridad), perspectiva FOV 30, pitch 38°, encuadre a los dos spots (parámetros de partida = cámara Bad North del prototipo, `Index/09b`). El jugador queda en Menu (cursor libre) mientras el panel está abierto — ya lo hace `UIManager`.

**Auditoría E4**: 5 duelos con capturas por beat (antes del choque, impacto, rotura, KO, victoria) MIRADAS; timing: el choque completo ≤ 3 s (`SetTimeScale` si hace falta); el perdedor de cada choque se lee sin mirar el log (test: tapar el HUD); `IsBusy` nunca queda trabado (volver a Idle al cerrar); consola limpia; sin `Find*` (el stage recibe todo por `Begin`).

### 9.6 · E5 — Juice + demo cerrada

Feel según la regla de la casa: en `Ring.prefab` un hijo `Feedbacks/` con un GameObject por momento — `OnClash` (camera shake suave + `MMF_ScaleShake` del podio), `OnBreak` (`MMF_Flash` + `MMF_Scale` punch en el renderer de la parte), `OnKO` (shake fuerte + `MMF_TimescaleModifier` breve), `OnVictory` (`MMF_Position` spring) — cada uno con su `MMF_Player`, slots serializados en `RingStage`, wiring de eventos arriba del script. DamageNumbersPro (`DefaultDamageNumberPo.prefab`, ya en `Resources/`) para "¡ROMPE!" / "NADIE CEDE" sobre el impacto. **Modo historia mínimo**: `CombatTuningSO.StoryRivals` (3 entradas: nombre, potencias fijas 1/1/1 · 2/1/1 · 2/2/1, color) accesibles desde Pick como pestaña "Historia"; el rival libre sigue por D2. Localización completa en/es. Playtest de Juan con el checklist de `Index/17` (texto plano, drama en el commit, 20-40 s).

### 9.7 · La auditoría (idéntica al cierre de CADA etapa)

1. **Compila**: `unity command recompile` → `recompile_status` = completed → `console --tail 20` con 0 errores / 0 warnings; un ciclo de Play (`editor_play` → 30 s → `editor_stop`) con consola limpia.
2. **Reglas de la casa** (grep): ningún `Find*`/`GetComponentInParent` cross-system nuevo; ningún archivo > 400 líneas; 0 comentarios; SO con diccionarios = Odin; `OnEnable`/`OnDisable` balanceados; una responsabilidad por archivo; el evento transporta la data.
3. **Funcional por código** (CLI `eval_file` o MCP `execute_code`): el script de la etapa (9.2-9.6) corre 5 veces sin intervención; el harness sigue dando 56,1% / 82,5% / 0% empates.
4. **Visual MIRADA** (memoria `feedback_verificacion_visual_screenshots`): capturas `s94_eN_*.png` a 720p y 1080p con checklist — tokens de la paleta únicamente, tipografía del tema, sin overlaps ni oclusión, glifos presentes, legible a distancia, tema nocturno aplicado, test del texto plano.
5. **Registro**: esta Parte 9 (estado ✅/⏳ por etapa), `Index/09 - Active Context`, ScriptNodes (vault-documenter en `/cerrar-sesion`), `Index/05` para lo de UI.

### 9.8 · Primera hora de S94, paso a paso

1. `/abrir-sesion` → leer esta Parte 9 (no volver a explorar) → confirmar con Juan D1-D11 (en especial D1 y D2) y el OK para el asset de E1.
2. Comprobar el editor (`unity status`), la escena activa (`GameScene`) y la distribución de tiers de las 18 criaturas (`eval`: contar `HornTier/WingTier/BackTier`).
3. Lanzar los 4 coders de E1 (reparto en 9.2) en paralelo; mientras, crear `CombatTuning.asset` por MCP.
4. Compilar → auditoría E1 completa (9.2) → registrar estado aquí.
5. Si sobra tiempo: E2 código (enum + `CombatPanelUITK` + presenters + UXML/USS + Loc keys) y pedir el OK de las dos asignaciones en escena.

### 9.9 · Registro E1 — hecho en S95 (2026-09-01, rama `s95-pc2`)

**Construido tal cual 9.2**, con D1-D11 vigentes (Juan dio el OK global "continuar todo donde se quedó"): `Systems/Combat/DragonRpsGenes` (31 líneas) · `DragonRpsRival` (51) · `DragonRpsService` (50) · `CombatOutcome` (13) · `CombatTuningSO` (13) · `CreatureDNA.CombatCooldownUntil` · `PlayerInventorySO.AddAdventureMaterial` · `ColorGenetics.RandomBase(System.Random)` (overload determinista para el rival, mismos rangos que el original) · `DevToolsConsole`: `BoxGroup("Combat (DEV)")` con `combatTuning` (si está vacío usa `CreateInstance` con defaults, así no hace falta cablear la escena) + botones **Simulate Combat (DEV)** y **Reroll Tiers (DEV)**. Asset `ScriptableObjects/Combat/CombatTuning.asset` creado por `eval_file` (20 / 3 / 1 / 0).

**Hallazgo ⭐ — tiers en 0**: las 18 criaturas del registro tienen `HornTier/WingTier/BackTier = 0`, fuera del enum (`Tier1 = 1`). `DragonRpsGenes.PowerOf` clampa a 1-3 (0 → 1), así que hoy todo dragón propio es 1/1/1 (presupuesto 3) y el rival sale 3-4. El botón **Reroll Tiers (DEV)** tira 1-3 por parte y persiste por `RegistryChanged`; **no se apretó** (los tiers también mueven precio y stats de esas 18 — decisión de Juan).

**Auditoría 9.7 ✅**: compila 0/0 (`recompile_status` completed, `errors: []`) · Play limpio (0 errores, 0 warnings salvo el aviso "not in automated mode" del pipeline) · reglas de la casa (grep: sin `Find*`, sin comentarios, sin suscripciones nuevas, todo < 170 líneas) · **funcional en Play** (`e1_audit.cs`, 5 duelos por código con `Play(0)`): 2 victorias → material 0 → 6, 3 derrotas → `CombatCooldownUntil` = ahora + 20 min, el elegible rota solo (Yucky Creep → Gloomy Sprout → Frosty Squish); `registry.Count` 19 → 19 (rival nunca registrado, `UniqueID` vacío); **persistido**: `creature_database_<uid>.json` reescrito con 19 campos `CombatCooldownUntil` (3 ≠ 0) y `player_inventory_<uid>.json` con `AdventureMaterial: 6` · harness: 55,9% / 82,3% / 0% empates sobre 20k (ruido de muestreo vs 56,1 / 82,5). Sin captura visual: E1 no tiene UI.

### 9.10 · Potencial por parte — decisión de diseño de Juan (S95, 2026-09-01) ⭐

> Textual: *"todas las criaturas pueden tener cualquier tipo de parte, ninguna es intrínsecamente mejor que otra, varían los quirks de cada una; cada parte tiene potencial, un valor que se hereda de los padres"* → *"promedio de los padres ±1, los valores del potencial del 1 al 10, entre 1 y 3 cuando los obtienes por compra"*.

| Regla | Implementación |
|---|---|
| Cada parte (cuerno, espalda, ala) tiene un **potencial entero 1-10** | `CreatureDNA.HornPotential / BackPotential / WingPotential` (default 1; las criaturas viejas del JSON nacen con 1 al deserializar) |
| **Al nacer por compra/generación: 1-3** | `CreatureGenerator.RandomMintPotential()` (`PotentialMin = 1`, `MintPotentialMax = 3`, `PotentialMax = 10`) en `GenerateRandom` — único camino de mint (`GameManager` y `GeneticsLabPreview` pasan por ahí) |
| **Herencia = promedio de los padres ±1** | `BreedingService.InheritPotential`: `(m + f + azar{0,1}) / 2` (desempate aleatorio del .5) `+ azar{-1,0,1}`, clamp 1-10. Mismo patrón que `InheritStat` |
| **La potencia del combate ES el potencial** | `DragonRpsGenes.PowerOf(int)` lee los 3 potenciales; `Budget` = suma (3-30). El rival (`DragonRpsRival`) re-tira potenciales en `[min(jugador)-1, max(jugador)+1]` hasta caer en presupuesto ±1 |
| Ninguna parte es mejor que otra: lo que varía son los **quirks** | = Parte 5 (identidad por gen, perks). v1 sin perks; sin código |
| `Tier` / `Rarity` / HP-Attack-Speed de `BodyPart` y `*Tier` del DNA | **Contradicen esta decisión.** Fuera del combate desde S95; siguen vivos en valuación (`ValuationHandler`) y stats (`CreatureStats`) y en la ficha. Registrado como deuda de diseño en [[Index/11 - Technical Debt]] |

Botón dev: **Reroll Potentials (DEV)** en `DevToolsConsole` (tira 1-3 por parte y persiste por `RegistryChanged`) para dar variedad a las 19 criaturas de la demo.

**Auditoría del potencial (S95, Play en `GameScene`, `pot_audit_diag.cs`) ✅**: compila 0/0 · `InheritPotential` por reflexión ×2000: (2,5) → 2..5 media 3,48 · (1,1) → 1..2 · (10,10) → 9..10 · (3,4) → 2..5 (promedio ±1 con clamp 1-10, confirmado) · reroll aplicado UNA vez a las 17 vivas (1/2/3 = 18/15/18, 0 fuera de rango) y persistido: `creature_database_<uid>.json` con 19 `HornPotential` (13 > 1) · 5 duelos: rivales siempre a ±1 de presupuesto y con perfil distinto (2/3/1 vs 2/1/2, 1/3/1 vs 3/1/2…), 2 victorias, material 6 → 12, registro 19 → 19, rival sin `Stamp`. Quirk: al entrar en Play la escena activa era `SampleScene` (el editor la restauró tras el recompile) → el `GameManager` de esa escena no tiene inventario y la auditoría dio NRE; reabrir `GameScene` por `eval` antes de cada Play.

### 9.11 · Registro E2 — hecho en S95 (rama `s95-pc2`) ✅

**Construido según 9.3 con dos ajustes**: (1) el log del choque no parsea el texto de `DragonRpsSession.Play` sino que lee **`DragonRpsSession.LastRound`** (`DragonRpsRoundInfo`: acciones, potencias, `Scorer` 0/1/2, `Mirror`, `Reshuffled`) y el presenter lo traduce con `Loc` — el string en español del harness queda intacto; (2) los lados del duelo se construyen **por código** en `CombatDuelPresenter.Begin` (retrato `.mm-swatch.rps-portrait`, nombre, filas POTENCIA / INTACTAS / GOLPES), el UXML solo deja los contenedores `side-player`/`clash`/`side-rival`.

| Pieza | Archivo |
|---|---|
| Enum | `UIPanelType.Combat = 4` (el slot de escena `UIManager/CombatPanelUITK` ya mapeaba la clave 4) |
| Dueño del panel | `UI/CombatPanelUITK.cs` (188 líneas): `IUINavigable`, estados Pick → Duel → Result, dueño de la `DragonRpsSession`; se refresca al abrir escuchando `UIManager.OnPanelSetRequested` y, para el toggle del `PanelTrigger`, `OnPanelToggleRequested` + `schedule.Execute` (lee `display` al frame siguiente). Cancel en Duel = abandonar → vuelve a Pick sin premio ni cooldown (D10); Cancel en Pick/Result = `false` → el `UIManager` cierra |
| Presenters | `UI/CombatPickPresenter.cs` (cards `.rps-card` con retrato, nombre y potenciales "2·3·1" o motivo: descanso hasta HH:mm / ocupado / sin energía; los no elegibles llevan `.rps-card--off`), `UI/CombatDuelPresenter.cs` (`Begin`/`Rebuild`/`Describe`), `UI/CombatResultPresenter.cs` (`Show`) |
| UXML / USS | `UI Toolkit/CombatPanelUITK.uxml` (raíz `mm-theme mm-theme--night rps`, reusa `TransactionPanel.uss`: `.tx-backdrop`, `.panel`, `.panel__header/__title`, `.actions/.action--accept/--cancel/--more`) + `CombatPanelUITKStyle.uss` (solo `rps-*` con `var(--mm-*)`; único color literal: el gris placeholder del retrato, copiado de `.card__icon`) |
| Loc | 27 claves `ui.rps.*` en/es agregadas por `eval_file` (`LocalizationEditorSettings.GetStringTableCollection("Strings")` + `StringTable.AddEntry`) |
| Escena (con OK de Juan: "continuá hasta el prototipo jugable") | `UIDocument` de `UIManager/CombatPanelUITK` → `sourceAsset` = el UXML nuevo; componente `CombatPanelUITK` con `document` + `tuning`; los 2 `DevToolsConsole` con `combatTuning`; mapa `panels[4]` verificado por reflexión; escena guardada por `eval_file` (`wire_combat_panel.cs`) |
| Apertura | botón **Open Combat Panel (DEV)** en `DevToolsConsole` → `UIManager.RequestPanelSet(Combat, true)` (hasta que E3 traiga el Ring con `PanelTrigger`) |

**Auditoría 9.7 ✅**: compila 0/0 · reglas de la casa (sin `Find*`, sin comentarios, `OnEnable/OnDisable` balanceados, 4 archivos < 200 líneas) · **funcional en Play** (`e2_open/e2_submit/e2_play/e2_cancel.cs`, todo por `IUINavigable`, cero ratón): 17 cards (3 en descanso de la auditoría anterior), 5 duelos completos por teclado con log localizado ("You: Wings breaks Back", "Mirror of Back: power 1 vs 1 · Nobody yields", "Both regroup"), 3 victorias (+3 material cada una) y 2 derrotas (cooldown en la card al volver a Pick: `off` pasó de 3 a 5), abandono desde Duel → Pick consumido, 0 errores de consola · **visual MIRADA y enviada a Juan** (`Assets/Screenshots/s95_e2_pick/duel/result.png`, 1920×1080): tema nocturno, tokens únicamente, retratos reales, sin overlaps, glifos (×, ·) presentes, test del texto plano aprobado (el log dice quién rompió qué). Pendientes cosméticos: scrollbar horizontal por defecto en la lista de pick; texto del panel en inglés porque el locale activo es `en`.

### 9.12 · Registro E3 — hecho en S95 (rama `s95-pc2`) ✅

**Mueble Ring** (todo por `eval_file` `mk_ring.cs`, con el OK global de Juan "continuá hasta el prototipo jugable"): `Furniture3x3_Ring.asset` (`F9`, 3x3, `Functional`, precio 100) registrado por reflexión en el dict `items` de `FurnitureDatabase.asset` y listado en `ShopCatalog.asset` (stock 1, sin descuento) · `Ring.prefab` = raíz (`FurniturePivotAligner` 3x3, `BoxCollider` trigger 3×1×3 centrado en y 0,54 igual que el BreedingRoom, `PanelTrigger` con `panel = Combat`) + `Podium` (instancia anidada de `Podium.prefab`, escala 1,7) + `Stage/SpotPlayer` y `Stage/SpotRival` a ±0,75 en X mirándose (E4). Detalle del mueble en [[Index/10 - Furniture & Building]].

**Loop visible**: `InfoOverlayUITK` muestra `Material: N` bajo los dabloons (label `material`, clave `ui.overlay.material`, se refresca con `OnInventoryChanged`) · `CreatureDisplay.StateOf` gana el caso cooldown (`status.cooldown`, "Resting until HH:mm") → cards del grid, ficha (línea Identity) y `CreatureVisualUI` lo heredan sin tocarlos · `DetailInfoTabPresenter`: las filas de partes muestran **Potencial** en vez de `Tier` (`ui.detail.partrow.potential`; `ui.detail.partrow` perdió el `Tier{4}`; `ui.detail.partrow.potential.empty` cuando la parte no tiene asset, que es el caso de hoy porque `Horn/Back/Wing`DatabaseSO siguen vacías desde S75).

**Auditoría 9.4 ✅** (`e3_buy_place.cs` / `e3_interact.cs` / `e3_verify2.cs`, todo en Play sin ratón): compila 0/0 · **compra real** por `StoreManager.BuyFurniture` (150 → 50 dabloons, stock 1 → 0, `HasFurniture("F9")`) · **colocación real** por `FurnitureService.TryPlace` en la celda libre más cercana a 4,5 m frente al jugador (celda (20, 25)) → el spawner instancia `F9@20_25` con `PlacedFurnitureMarker` y el `FurnitureService` dispara `NavMeshWillRebake` (consola: "[SpawnDiag] NavMeshWillRebake → agentes pasan a física") · **E** = `PanelTrigger.Interact()` → `RequestPanelToggle(Combat)` → el panel se abre en Pick (vía `OnPanelToggleRequested` + `schedule`) · 3 duelos jugados desde el Ring (1 victoria, 2 derrotas) · **salir y volver a Play**: el Ring reaparece desde `furniture_registry_<uid>.json` (contiene `"F9"`), `player_inventory_<uid>.json` tiene el mueble y `AdventureMaterial: 24`, overlay `Dabloons: 50 · Material: 24`, 7 criaturas en cooldown con `StateOf` = "Resting until HH:mm" · ficha abierta por `UIManager.SelectCreature` con estado y potenciales · 0 errores de consola · **capturas MIRADAS y enviadas a Juan**: `s95_e3_ring.png` (Ring frente al jugador + overlay con material), `s95_e3_ring_panel.png` (panel nocturno sobre el Ring), `s95_e3_ficha.png` (ficha con "Resting until" y filas de partes). No hay reloj día/noche: la "captura de noche" es el panel con `mm-theme--night`.

### 9.13 · Triángulo RPS en el duelo + micro-animaciones (S95, pedido de Juan tras ver el prototipo) ✅

**`UI/RpsTriangleElement.cs`** (`VisualElement` puro, instanciado por código en `CombatDuelPresenter` — sin `[UxmlElement]` para no reintroducir `partial`): dibuja con **Painter2D** (`generateVisualContent`) el triángulo Cuernos (arriba) → Alas (abajo derecha) → Espalda (abajo izquierda) → Cuernos. Las aristas salen de `DragonRpsRules.Beats` (no hay pares hardcodeados) y llevan punta de flecha en el perdedor; cada nodo es un círculo relleno `--mm-surface-2` con borde del color de su tipo (teal / plum / gold, los mismos bordes que los botones de la mano). `Highlight` = acción de la carta seleccionada: anillo coral **pulsante** (`schedule.Execute(...).Every(33)`, se pausa al deseleccionar), su flecha se engrosa en coral y el nodo que rompe recibe un punto interior. Colores por propiedades custom `--tri-*` declaradas en `.rps-triangle` como `var(--mm-*)` y leídas en `CustomStyleResolvedEvent` → sigue siendo solo paleta. Los 3 labels son hijos absolutos (`.rps-triangle__label`, `Loc` via `SetLabels`). Vive en la columna `clash`, entre los retratos y el log (210×176).

**Micro-animaciones por USS** (`transition-*` en `CombatPanelUITKStyle.uss`): botón de la mano seleccionado escala 1,06 en 120 ms · card de pick seleccionada 1,04 · pip de golpe salta a 1,18 con `ease-out-back` · el log parpadea en coral 350 ms en cada choque (`rps-log--flash`, lo pone y quita el presenter con `schedule`) · la tarjeta de resultado entra de 0,88/opacidad 0 a 1 con `ease-out-back` (`rps-result--enter` que `CombatResultPresenter.Show` quita a los 40 ms).

**Auditoría**: compila 0/0 · duelo en Play con captura `s95_e2_triangle.png` MIRADA y enviada (triángulo legible a distancia, resaltado correcto: Cuernos seleccionado → flecha coral hacia Alas) · 0 errores. Quirk: `new UQueryBuilder<RpsTriangleElement>(root).First()` devolvió null en `eval` aunque el elemento está en el árbol; para tests buscar por clase `rps-triangle`.

**Estado**: E1 ✅ (S95, potencial integrado) · E2 ✅ (S95, + triángulo y micro-animaciones) · E3 ✅ (S95) · E4 ⏳ · E5 ⏳
