---
tags: [index, roadmap, plan, cimientos]
---

# 28 - Cimientos y camino a Game Ready (S127)

> Pedido de Juan (2026-09-21): *"ingresa los cimientos de todo lo mencionado, hay que empezar a unir las partes del juego… centrémonos en dos currencies, los dabloons y la Minerita… mejoremos las actividades pasivas que puedan desarrollar los MoriMonchis así como las partes de la tienda… la meta es pensar qué tanto le falta a esto para que sea game ready"*.
>
> Escrito sobre cinco auditorías de solo lectura contra el código (tienda/economía · crianza/ciclo de vida/guardado · mundo vivo · habilidades/evolución · UI/tienda online/producción). Complementa a [[Index/25 - Hitos hasta el lanzamiento]]: aquella nota ordena **qué puede hacer el jugador**; esta ordena **sobre qué se apoya**. Cada cimiento y cada etapa tendrá su plan ejecutable propio (formato [[Index/26 - Plan H0 - Bajada por pisos]]) cuando le toque.

---

## 0 · Regla del plan

> **Primero lo que todas las etapas comparten (dinero, guardado, vida de la criatura, tiempo, catálogo, arranque); después cada etapa se enchufa a eso. Un cimiento no decide diseño: deja el enchufe listo para que la decisión de diseño sea un dato, no una reescritura.**

Prueba de que un cimiento está bien hecho: vender o dar en adopción, poner un juguete, abrir la tienda online o pedir una caja de MoriMonchis se agregan **como contenido** (un asset, una fila de catálogo, un trabajo nuevo), sin tocar el guardado ni el bus de eventos.

---

## 1 · Las dos monedas (contrato de economía)

| | **Dabloons** | **Minerita** |
|---|---|---|
| Fantasía | El negocio | La criatura |
| Entra por | Adopción/venta · actividades pasivas de la tienda | **Solo la exploración** (lo asegurado al retirarse) — decisión de Juan S127 |
| Sale por | Muebles · ítems y juguetes · cosméticos · mejoras de la tienda · caja de MoriMonchis | **Eclosionar huevos** · evolución de partes · cambio y reroll de habilidad |
| Hoy en el código | Ciclo completo (entra por venta, sale por compra) | Existe como `adventureMaterial`: **se suma y nunca se gasta** |

Se borran `passiveMaterial` y `evolutionEssence` (declarados, guardados, sin fuente ni gasto). Regla: **todo gasto y todo ingreso pasa por una sola puerta** (intentar gastar / sumar, con motivo), dueña del evento de inventario. Ninguna pantalla resta monedas por su cuenta. El motivo se registra para balancear después (cuánto entra y sale por cada vía).

---

## 2 · Inventario de sistemas (estado real, S127)

🟢 funciona y está unido al resto · 🟡 funciona aislado o a medias · 🔴 ausente · 🪦 muerto, hay que borrarlo

| Sistema | Estado | Lo que hay | Lo que falta para game ready |
|---|---|---|---|
| Clientes (fila, negociación, pago) | 🟡 | FSM completa, contraoferta única | Los 3 arquetipos son idénticos; eligen siempre la criatura más cara |
| Valuación | 🟡 | Tiers + stats + crías | No mira cuidado, rol, rareza, shiny, edad ni linaje; los tiers nunca cambian (siempre 1) |
| Catálogo y tienda online | 🟡 | Panel de 3 pestañas, descuentos, restock, caja de entrega para ítems | 2 muebles y 2 ítems listados (hay 10 muebles definidos); **comprar un mueble no desbloquea nada** (el modo construcción lista todo); no hay criaturas, cosméticos ni mejoras en el catálogo |
| Muebles y construcción | 🟢 | Grilla, fantasma, NavMesh en vivo, persistencia | Muebles sin efecto de juego más allá de restaurar necesidades; bug abierto de rebakes solapados |
| Ítems y hotbar | 🟡 | 6 slots, lanzar, soltar, comer de la mano, caricias | 2 ítems; el trapeador no hace nada; las criaturas nunca usan juguetes solas |
| Genética y herencia | 🟢 | 6 genes visibles, padres/abuelos/mutación, diales 50/30/20, `Role`, 4 crías máx., padres e hijos guardados | Pocas variantes (1 cuerno, 2 alas, 4 espaldas, 2 caras); sin generación ni marcas |
| Crianza (flujo) | 🟡 | 30 min autoritativos por Cloud Code, corral con cortejo | Cuesta 20 de energía y nada más; despliegue de los `.js` sin manifiesto en el repo; tiempo real, no de juego |
| Evolución de la criatura | 🔴 | — | No existe nada que transforme a una criatura ya nacida |
| Habilidades | 🟡 | 7 habilidades, 3 por criatura, derivadas de la parte | Ningún dato de habilidad por criatura: no se puede cambiar ni rerollear; 1 sola opción de cuerno |
| Stats y equipo | 🪦 | UI de detalle y mochila de equipo | Los stats no afectan la arena ni la tienda; el equipo solo alimenta stats que nadie lee; 3 de 6 stats siempre en 0 |
| Necesidades y cuidado | 🟡 | Vida, energía, afecto; estaciones; enfermo por vida baja. **S129:** son la puerta de la bajada (`CareGateSO` 60/60/0) y se ven como barras de color en la ficha y en el panel de bajada | No pagan (ni precio ni cría); **los umbrales están sin calibrar: hoy ninguna criatura pasa** porque la vida se drena sola hasta 0 |
| Vida social | 🟡 | Juego, siesta, peleas, grafo de afinidad | Se guarda solo en local (se pierde al cambiar de PC); no afecta nada fuera de sí mismo |
| Actividades pasivas / trabajos | 🔴 | — | Ninguna conducta produce nada; no hay trabajos asignables; los diales nunca cambian |
| Bajada (arena) | 🟡 | Sala por semilla, ocupaciones, choque, súper, run por pisos con vida en riesgo, puente ida y vuelta | Falta la prueba de Juan (5 bajadas a 1×); un solo tipo de sala más buffo; permadeath apagado; intenciones en vivo ([[Index/27 - Intenciones en vivo (Draft)]]) reemplazarían las órdenes (~20 archivos) |
| Rival real | 🔴 | Auth, Cloud Save, patrón asíncrono conocido | Snapshots ajenos, ferales, caverna del día |
| Guardado local y nube | 🟢 | **S128:** sobre `{Version, SavedAtTicks, Data}` con cadena de migraciones, reconciliación por fecha con respaldo de conflicto, push agrupado (5 s, tiempo sin escala), 5 claves en la nube (entra `socialgraph`). **S129:** guardado en `v3` con dos estantes (`Alive`/`Departed`) | Una build vieja no puede leer un guardado v3 (la cadena es hacia adelante) |
| Muerte | 🟢 | **S129:** `CreatureLifecycle.Kill`/`Adopt` son la única salida; evento `OnCreatureDeparted` con aviso en el overlay y borrado de aristas sociales; las idas salen del registro vivo pero siguen resolviendo ancestros | Sin despedida ni memorial; permadeath sigue apagado |
| Reloj de juego | 🔴 | — | Edad, restock, descuentos y cría corren en tiempo real |
| Arranque del jugador | 🔴 | — | Save vacío = 0 criaturas y ningún camino para conseguir la primera; sin menú, pausa, ajustes ni salir |
| Combate RPS | ✅ | **S128: borrado.** 18 `.cs`, su UXML/USS, `CombatTuning.asset`, el `PanelTrigger` del Ring, 45 claves de la tabla `Strings` y el valor `Combat` del enum de paneles | — |
| Localización | 🟡 | 352 claves en/es completas en la tienda (S128-S129 podaron 78 huérfanas) | La arena entera (48 archivos) con texto fijo en español |
| Audio | 🔴 | — | El juego es mudo: cero fuentes, clips y mezcladores propios |
| Input | 🟡 | 3 mapas, bindings de gamepad | Sin rebind; leyenda de controles solo de teclado |
| Build | 🟡 | 2 escenas, PC | 257 assets en `Resources`; NavMesh en runtime con mallas no legibles; nunca se probó una build |
| Pruebas | 🟡 | **S128:** assembly `MoriMonchi.Logic` + `MoriMonchi.Logic.Tests` con 10 pruebas EditMode de las migraciones | Solo cubre el guardado; el resto del código sigue sin pruebas (se muda lógica pura en H7) |
| Tamaño del código | 🟡 | 273 archivos, 35.800 líneas, composición sin partials | 14 archivos sobre 400 líneas; [[Index/11 - Technical Debt]] quedó viejo |

---

## 3 · Los cimientos (hito nuevo **HC**, entre H0 y H1)

Siete piezas. El orden es de dependencias: primero se achica la superficie, después se blinda el guardado (todo lo demás cambia el guardado), después lo que el resto usa.

### C1 · Limpieza del combate viejo ✅ (S128)
- **Regla:** lo que no es del juego vigente no viaja más.
- **Alcance:** borrar Dragon RPS completo (lógica, servicio, UI de combate, tuning), el disparador de combate del mueble Ring, el cooldown de combate del ADN, el peso de combate huérfano de los arquetipos, el campo de ítem sostenido que nadie lee y el disparador de ítem sin lector.
- **Stats y equipo (Juan S127): se borran** porque no afectan nada, y la ficha pasa a mostrar necesidades y nivel de partes (pieza C1b de `Index/29` §12). **✅ S129:** 12 `.cs` y 11 assets fuera; la ficha muestra cuidado (tres barras + apta para bajar) y las tres partes con nivel y techo. La valuación perdió el bonus de stats y quedó en tiers + crías: **los precios bajaron** hasta que E1 la rehaga.
- **Desbloquea:** C3 (una moneda menos pagada por un sistema muerto), panel `Combat` fuera del enum de paneles.
- **Muta fuera de código (OK de Juan):** prefab del Ring, assets de arquetipos, `CombatTuning.asset`, entradas del diccionario de paneles.

### C2 · Guardado robusto ✅ (S128)
- **Regla:** el guardado tiene versión, fecha y tamaño acotado; nunca gana nadie a ciegas.
- **Subetapas:**
  1. **Versión de esquema y migraciones** en cadena (v1 → v2 → …), aplicadas al cargar de disco y al bajar de la nube. Es el requisito de C3, C4 y C6, que cambian el formato.
  2. **Reconciliación por fecha:** al iniciar sesión se comparan las marcas de tiempo local y de nube; gana la más nueva y la otra se conserva como respaldo de una vuelta. Hoy el pull pisa siempre.
  3. **Push agrupado:** las mutaciones se acumulan y se sube una vez cada pocos segundos, más el volcado al salir, al pausar y al bajar. Hoy cada cambio sube el registro entero.
  4. **Grafo social a la nube** (quinta clave).
  5. **Manifiesto de Cloud Code** en el repo (qué scripts están publicados y en qué versión); despublicar los endpoints del combate viejo.
- **Desbloquea:** todo lo que agrega campos; el rival real de H3; jugar en las dos PCs sin perder trabajo.

### C3 · Cartera de dos monedas ✅ (S128)
- **Regla:** sección 1.
- **Subetapas:** renombrar el material de aventura a Minerita (migración de C2); borrar las dos monedas muertas; puerta única de ingreso y gasto con motivo; ambos saldos siempre visibles en el overlay; la tienda, el puente y los futuros consumidores pasan por la puerta.
- **Desbloquea:** evolución, reroll, mejoras, cosméticos, caja de MoriMonchis.
- **Muta fuera de código:** `PlayerInventory.asset`, UXML/USS del overlay, claves en/es.

### C4 · Ciclo de vida de la criatura ✅ (S129)
- **Regla:** una sola respuesta a "¿dónde está esta criatura y se puede usar?", y una sola forma de irse.
- **Subetapas:**
  1. **Disponibilidad única:** hoy cría, venta y bajada preguntan cada una a su manera. Una consulta común (libre · criando · de bajada · trabajando · vendida · muerta) que usan todos los paneles.
  2. **Salidas con dueño:** morir, ser adoptada y retirarse pasan por un único servicio que emite el evento (con suscriptores reales: spawner, paneles, memorial) y dispara la persistencia.
  3. **Archivo acotado:** muertas y adoptadas salen del registro vivo a un historial liviano (nombre, ADN en texto, padres, fechas, destino) con tope. **Invariante:** la herencia consulta abuelos y bisabuelos, así que el historial debe seguir resolviendo ancestros.
  4. **Generación** como dato (hoy solo se reconstruye caminando padres).
  5. **El cuidado es la puerta de la bajada (Juan S127):** solo un MoriMochi bien cuidado puede bajar; dentro de la exploración las necesidades de la tienda no influyen (todos entran con la vida de la run llena). Hoy 6 de 10 criaturas están en vida 0: hay que calibrar el decaimiento para que una criatura atendida siga apta durante una sesión de tienda.
- **Desbloquea:** permadeath real (H1), durabilidad con salidas visibles (H5), trabajos (E3), save acotado (H7).

### C5 · Catálogo unificado y propiedad
- **Regla:** todo lo que se compra es una fila de catálogo con un tipo, un precio en una de las dos monedas y una forma de entrega; lo comprado se posee.
- **Subetapas:**
  1. **Tipos de fila:** mueble · ítem/juguete · **caja de MoriMonchis** · cosmético · mejora de tienda. Hoy solo mueble e ítem.
  2. **Propiedad que cuenta:** el modo construcción ofrece lo que se posee y en la cantidad poseída (hoy lista la base entera y comprar no cambia nada).
  3. **Entrega única:** todo lo físico llega por la caja de entrega, incluida la caja de MoriMonchis (se abre y salen criaturas minteadas).
  4. **Llenar el catálogo** con los 10 muebles que ya existen.
- **Desbloquea:** el arranque del jugador (C7), los sumideros de dabloons, la tienda online como contenido.
- **Muta fuera de código:** `ShopCatalog.asset`, prefab de la caja, UXML de la tienda y del modo construcción.

### C6 · Reloj de juego
- **Regla:** el tiempo del juego es del juego: un día con bloques, que se guarda y se puede pausar; nada de gameplay mira el calendario real.
- **Subetapas:** servicio de reloj (día, hora, bloque, velocidad, pausa con paneles abiertos) con eventos de cambio de bloque y de día; reloj visible; pasar al reloj la edad, el restock, los descuentos, el decaimiento de necesidades y la llegada de clientes; la noche habilita la bajada.
- **El diseño de los bloques no se decide aquí:** el cimiento entrega el enchufe (bloques configurables por asset). La estructura de 4 bloques de [[Index/18 - Pilares del Rediseno (Draft)]] 1.2 es el primer dato que se carga.
- **Punto delicado:** la cría dura 30 min reales y la autoriza el servidor. Pasarla al reloj de juego la vuelve local (decisión 4).
- **Desbloquea:** trabajos con duración (E3), envejecimiento (H5), ritmo día/noche del loop.

### C7 · Cáscara del juego y arranque
- **Regla:** se puede abrir el juego, empezar de cero, pausar, ajustar y salir sin el editor.
- **Subetapas:** pantalla de título (continuar / nueva partida); **arranque del jugador nuevo** = una caja de MoriMonchis de regalo, dabloons iniciales y muebles básicos poseídos (usa C5); pausa; ajustes (volumen, calidad, idioma, leyenda de controles por dispositivo); salir con volcado; dueño único del flujo de escenas (título → tienda ↔ arena).
- **Desbloquea:** los "30 minutos jugables sin MCP" de H1, cualquier prueba con otra persona, la build.

### Transversal · Red de seguridad
Assembly de lógica pura con pruebas de EditMode, creciendo con cada cimiento: migraciones (C2), cartera (C3), disponibilidad y archivo (C4), reloj (C6), más las que ya son puras (`ArenaRun`, herencia, valuación). Primera build de PC al cerrar C7 para destapar NavMesh, `Resources` y Odin temprano.

**Si falta tiempo se corta C6** (el reloj): el resto no lo necesita para unirse; E3 usaría duraciones en tiempo real hasta que llegue.

| Pieza | Sesiones |
|---|---|
| C1 Limpieza | 1 |
| C2 Guardado | 2 |
| C3 Cartera | 1 |
| C4 Ciclo de vida | 1-2 |
| C5 Catálogo | 1-2 |
| C6 Reloj | 2 |
| C7 Cáscara | 2 |
| **HC total** | **10-12** (5-7 al ritmo de S119-S124) |

---

## 4 · Las etapas sobre los cimientos

### E1 · Tienda (adelanta parte de H5)
1. **Valuación que mira a la criatura:** cuidado (necesidades y ánimo), rol, rareza de partes, shiny, edad, generación. Test del texto plano: el jugador puede decir por qué una vale más.
2. **Tres clientes distintos de verdad:** presupuesto, tolerancia y gusto diferenciados; que no elijan siempre la más cara. Es el paso previo al adoptante que reacciona de [[Index/22 - Bajada Nocturna y Linaje (Draft)]] 3.2 (venta o adopción es un dato, no una reescritura).
3. **Mejoras de la tienda (dabloons):** capacidad de corrales, lugares de vitrina, clientes simultáneos, tamaño del almacén, velocidad de entrega.
4. **Cosméticos (dabloons):** paletas y piezas de la tienda, accesorios visibles.
5. **Muebles con efecto:** cada categoría hace algo enunciable en una frase (descanso más rápido, juego que sube afecto, estación de trabajo de E3).
6. **Suciedad y limpieza (Juan S127: entra):** es uno de los **sistemas de entretenimiento de la tienda**, pensados para mantener ocupado al jugador durante el día junto a alimentar, separar peleas y atender clientes. Las criaturas ensucian con el tiempo, el trapeador limpia, y una tienda sucia baja el afecto y lo que ofrecen los clientes.

### E2 · Evolución con Minerita
1. **Habilidad como dato de la criatura:** tres habilidades guardadas (cuerno, alas, espalda), iniciadas desde la parte al nacer; viajan junto al ADN en texto como metadatos, no dentro de él (regla 5).
2. **Cambio y reroll (Minerita):** reroll = otra habilidad al azar del mismo lugar; cambio = elegir entre las descubiertas. Requisito de contenido: **al menos 3 habilidades por lugar** (hoy 1 / 2 / 4).
3. **Evolución de partes (Minerita):** por ahora evolucionar = subir el nivel de la parte (reutiliza los tiers que ya existen y nunca se escriben). **La regla vive en un ScriptableObject ejecutable** (Juan S127): cambiar qué es evolucionar es cambiar de asset, no de código. Contrato en [[Index/29 - Plan HC - Cimientos (ejecutable)]] §8.
4. **Herencia:** qué pasa a la cría (la habilidad sí, el nivel no, propuesta) para que criar siga siendo el motor y evolucionar no lo reemplace.
5. **Pantalla de evolución** en la ficha de la criatura, donde hoy está el equipo.

### E3 · Actividades pasivas
1. **Trabajos asignables:** un trabajo es un asset (duración en bloques del reloj, requisito por rol/parte/rasgo, rendimiento, costo en energía); la criatura queda "trabajando" (usa C4) en un mueble-estación. Primer lote:
   | Trabajo | Afinidad | Rinde |
   |---|---|---|
   | Atender el mostrador | Empático | Más dabloons por adopción mientras dura |
   | Vigilar la tienda | Protector | Menos peleas y necesidades que bajan más lento en su corral |
   | Escarbar en el patio | Agresivo | Encuentra ítems y juguetes (nunca Minerita: solo la exploración la da) |
   | Entrenar | cualquiera | Arranca la próxima bajada con carga de súper |
   | Cuidar crías | sociable | La cría hereda algo de su rasgo |
2. **Vida propia con sentido:** usar juguetes por su cuenta, comer y dormir por gusto y no solo por urgencia, preferencias visibles.
3. **El ambiente moldea:** los rasgos se mueven despacio según corral y compañía (hoy son inmutables), como pide [[Index/22 - Bajada Nocturna y Linaje (Draft)]] 3.2.
4. **El cuidado paga:** ánimo alto multiplica rendimiento del trabajo y valuación; necesidades visibles en la ficha y sobre la criatura.
5. **Resumen de lo pasado:** al volver de la bajada o al empezar el día, un aviso con lo que produjeron.

### E4 · Aventura
1. Cerrar H0: 5 bajadas de Juan a 1×, números de vida, fin del buffo.
2. Intenciones en vivo ([[Index/27 - Intenciones en vivo (Draft)]]) **antes** de sumar contenido a la arena: reemplaza el sistema de órdenes sobre el que se apoyaría todo lo nuevo.
3. Permadeath encendido sobre C4 (despedida, memorial, aviso previo).
4. Variedad de pisos (H6) y rival real (H3).

### E5 en adelante
Linaje y marcas (H2) · fenotipo y feel (H4) · contenido (H6) · localización de la arena, audio, build y pruebas finales (H7-H8) · beta y lanzamiento (H9-H10). Sin cambios respecto de [[Index/25 - Hitos hasta el lanzamiento]].

---

## 5 · Orden de ejecución

```
H0 (prueba de Juan) ─┐
                     ├─ HC: C1 ✅ → C2 ✅ → C3 ✅ → C4 ✅ → C5 → C6 → C7
                     │          (pruebas y build al cerrar)
                     └─ E4.2 intenciones (paralelo: no toca el guardado)
HC ─→ H1 loop cerrado = E1.1-E1.2 + E2.1-E2.3 + E3.1 + E4.3
   ─→ H2 linaje ─→ H3 rival real ─→ H4 · H5 (resto de E1, E3) · H6 ─→ H7 · H8 ─→ H9 ─→ H10
```

H1 cambia de contenido: deja de ser "usar el material en la cría" y pasa a ser **el loop de dos monedas cerrado**: bajar trae Minerita → evoluciona y cambia habilidades → la criatura rinde más abajo y vale más arriba → los dabloons mejoran la tienda → la tienda sostiene más y mejores criaturas.

---

## 6 · ¿Cuánto falta para game ready?

- **Lo construido es ancho y está poco unido.** Hay tres juegos que funcionan por separado (tienda, crianza, bajada) y un solo hilo que los une (el puente, que hoy solo trae un número que no se gasta).
- **Sistemas en verde:** 2 de 25. **En amarillo:** 15. **Ausentes:** 6. **Muertos:** 2.
- **Camino crítico a un juego mostrable** (loop completo, feo y corto, jugable sin editor): H0 + HC + H1 ≈ **16-22 sesiones**.
- **Camino a lanzamiento:** las 46-75 sesiones de [[Index/25 - Hitos hasta el lanzamiento]] ya contaban parte de estos cimientos repartidos en H1, H3 y H7. Sacarlos al frente suma poco en total (**≈ 52-82**) y quita el riesgo de reescribir el guardado con contenido encima.
- **Lo que más pesa y todavía no se empezó:** audio (de cero), arranque y cáscara, rival real, contenido (partes, habilidades, pisos, muebles).

---

## 7 · Decisiones de Juan

**Resueltas (S127):**
1. La segunda moneda se llama **Minerita**: es el material que se asegura en la bajada, renombrado; las otras dos monedas se borran.
2. **Solo la exploración da Minerita**; ningún trabajo de tienda la produce.
3. **Solo un MoriMochi bien cuidado puede bajar**; dentro de la exploración las necesidades que se cuidan en la tienda no influyen.
4. Evolucionar = **subir el nivel de la parte**, con la regla en un **ScriptableObject ejecutable** para poder cambiarla.

5. **La cría pasa al reloj de juego** (local, sin Cloud Code) y **eclosionar el huevo cuesta Minerita**: segundo sumidero de la Minerita y el lazo directo bajada → crianza.
6. **La suciedad entra** como sistema de entretenimiento de la tienda (E1).
7. **Stats y equipo se borran** y se actualiza la UI: la ficha muestra necesidades y nivel de partes.
8. **El tutorial guía hasta comprar la primera caja de MoriMonchis en la PC, gratis con 100 % de descuento**; la misma regla rescata al jugador que se quedó sin criaturas.

9. **Los potenciales de parte se conservan** como techo del nivel de la parte, y **el proyecto Cutie Marks se mantiene** (H2).
10. **El costo de eclosionar sube con cada parte** (con el nivel de las partes de los padres; fórmula en datos, `Index/29` §9.2).
11. **Volver de la bajada amanece al día siguiente.**

**Plan ejecutable de los cimientos:** [[Index/29 - Plan HC - Cimientos (ejecutable)]] (C1-C7 con contratos, sesiones HC-1 a HC-5).

---

## 8 · Cómo se usa esta nota

Al abrir sesión: ubicar la pieza activa (C1-C7 o E1-E4) y escribir su plan ejecutable en una nota propia antes de repartir a los coders. Al cerrar una pieza: marcarla ✅ aquí con la sesión y actualizar la fila del inventario de la sección 2. Esta nota y [[Index/25 - Hitos hasta el lanzamiento]] se leen juntas: aquella es el mapa del jugador, esta el de la obra.
