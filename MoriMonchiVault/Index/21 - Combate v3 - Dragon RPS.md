---
tags: [index, design, combate, v3]
---

# 21 - Combate v3 — Dragon RPS (S92)

> **Sesión 92 (2026-09-01).** Juan entregó el mini-draft **DRAGON RPS V1 FINAL** y con él se cierra la refundación abierta en S91. Este documento reemplaza a [[Index/20 - Combat Prototype MVP (Plan)]], que pasa a **histórica**.
>
> **ESTADO: núcleo CERRADO Y VERIFICADO POR SIMULACIÓN.** Los perks son **exploración, no decididos**.
>
> **Convención:** ⭐ = idea de Juan (fuente de verdad, no interpretar). El resto es lectura del orquestador. Los números vienen del simulador construido en esta sesión, no de opinión.

Relacionado: [[Index/17 - Refundacion del Combate]] · [[Index/15 - Theorycrafting S71 - Autobattler y Marketing]] · [[Index/18 - Pilares del Rediseno (Draft)]] · [[Index/20 - Combat Prototype MVP (Plan)]]

---

## PARTE 1 — El diseño ⭐

**1 dragón vs 1 dragón. El primero que mete 3 golpes gana.**

**RPS rígido:** Cuernos > Alas > Espalda > Cuernos. **El tipo con ventaja gana siempre**, sin importar la potencia.

**Deck de 6:** x2 de cada parte (2 Cuernos, 2 Alas, 2 Espalda). **Mano de 3 robada al azar** — no controlás lo que te toca. Cada ronda jugás 1 carta de tu mano, se gasta, va al descarte y robás otra. Si gastaste tus 2 Cuernos, no podés usar Cuernos nunca más en ese combate: **hay que contar**.

**Espejo (mismo tipo):** gana el de más Potencia. **Si las potencias están parejas, se lastiman los dos.**

**El combate dura 3-5 rondas.**

### Por qué este diseño sí

Pasa los cinco filtros que sobrevivieron a S91 (ver [[Index/09 - Active Context]]):

| Filtro | Cómo lo cumple |
|---|---|
| Texto plano ([[Index/17 - Refundacion del Combate]] criterio 2) | *"Cuernos vencen Alas, Alas vencen Espalda, Espalda vence Cuernos. Si son iguales, gana el más fuerte; si están parejos, se lastiman los dos."* |
| Vida en hits (17 §5, Palanca 1) | 3 golpes, literal |
| Drama en el commit | elección simultánea en secreto, el choque es la verificación |
| Ciclo 20-40s | **3,47 rondas** de media medidas |
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

### 2.4 · La Potencia es la fuente principal de habilidad, y es BINARIA

- Dragones parejos: el que cuenta el descarte gana **63,1%** de las partidas decididas.
- Un dragón con más potencia: **82,5% y cero empates**.

O sea: **criar se siente**. Pero `potencia 2 vs 1` y `potencia 3 vs 1` dan resultados **idénticos** — solo importa quién tiene más, no por cuánto. **La Potencia es un número de un dígito**; los `+10` / `-5` del draft de perks no tienen sentido en este sistema.

⚠️ **Riesgo abierto:** 82,5% es mucho. Si en la aventura te toca de sorpresa un rival con más potencia, perdés casi siempre. **Falta decidir qué brecha de potencia puede existir entre dos dragones.**

---

## PARTE 3 — Reglas v1 (cerradas)

1. Deck **2/2/2 fijo** para todo dragón. Mano de 3, robada al azar; jugás 1 y robás 1.
2. **Cuernos > Alas > Espalda > Cuernos**, sin excepciones.
3. Espejo → gana más Potencia → si están parejos, **golpe mutuo**.
4. **3 golpes gana.** Si se agotan las cartas, gana quien tenga más golpes.
5. Potencia: entero chico (1-3), por tipo, visible.
6. **Sin permadeath** ⭐ (decisión S92).
7. Sin perks en v1 — ver Parte 5.

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

## PARTE 7 — El marco (decisiones S92 ⭐)

- **Contra quién:** PvE local, **aventura con rival sorpresa**. No sabés quién te toca; si ganás, ganás material. Competencia indirecta (leaderboard), sin matchmaking ni servidor. Coherente con la decisión de S76 de matar el PvP por snapshot.
- **Permadeath:** **quitada por ahora.**
- **Qué produce:** **material** (18 §1.3), con doble salida — vender en la tienda o fabricar consumibles y Cutie Marks.
- Vive en el bloque nocturno **23:00-6:00** del ciclo día/noche (18 §1.2).

**Requisito de información:** no sabés quién te toca *antes* de la pelea, pero **cuando aparece lo ves entero antes de elegir tu primera carta**. Si no, la ronda 1 es adivinanza pura y el criterio de la hipótesis (17 criterio 1) no se cumple.

---

## Decisiones abiertas

- [ ] **¿Qué brecha de potencia puede haber entre dos dragones?** (82,5% de winrate con solo +1 — ver §2.4)
- [ ] **¿Qué pasa con el 9,7% de empates** en la aventura? (¿se repite, cuenta como derrota, da medio material?)
- [ ] **¿Cuántas peleas tiene una noche**, y **¿el cuerpo roto se arrastra entre peleas?** (la "herida persistente" de la 17 §7.16 es el reemplazo natural del riesgo que se fue con el permadeath, y haría que el cuidado pague — el agujero que la 15 marcó y sigue abierto)
- [ ] **¿Qué perks entran en v1?** (Parte 5 — simular antes de fijar)
- [ ] **¿Destino de `CombatPrototype/`?** (¿demolición estilo S75?)
- [ ] **¿Cómo elige la IA?** Recomendación: **una IA que cuenta** (ya implementada), sesgada por carácter. Un script memorizable ("el Terco abre con cuernos") se descifra una vez y muere a la partida 30.
