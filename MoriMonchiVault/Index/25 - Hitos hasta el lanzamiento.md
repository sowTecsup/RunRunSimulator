---
tags: [index, roadmap, plan]
---

# 25 - Hitos hasta el lanzamiento (plan general, S123)

> Pedido de Juan (2026-09-16): *"un plan general de hitos que englobe TODO lo que necesita el juego para salir"*. Escrito por el orquestador sobre el estado real del código (S123), el diseño vigente ([[Index/22 - Bajada Nocturna y Linaje (Draft)]] Partes 3, 4, 8 y 9; [[Index/18 - Pilares del Rediseno (Draft)]]) y el diagnóstico por frentes ([[Index/16 - Diagnostico por Frentes]]). Cada hito se define por **lo que el jugador puede hacer al cerrarlo**, no por sistemas. Las estimaciones son en sesiones de trabajo como las actuales (S119 cerró cuatro sesiones planeadas en una; el rango cubre ese ritmo y uno más lento).

---

## 0 · Regla del plan

> **Primero el loop entero, feo y corto; después profundidad; después contenido; después pulido y producción. Nada de un hito posterior entra antes de cerrar el criterio del anterior.**

Orden de dependencias: la bajada necesita al puente, el linaje necesita a la bajada (ahí se ganan las marcas), el rival real necesita al linaje (los ferales son linajes perdidos), la economía de adopción necesita al linaje y a los genes visibles, y el contenido necesita a todos para tener sobre qué variar.

---

## 1 · Punto de partida (S123)

| Frente | Hay | Falta |
|---|---|---|
| Tienda / economía | Clientes que entran, miran, eligen, hacen fila, negocian y pagan; inventario, vitrinas, muebles y modo construcción; dabloons y material. | Adoptantes que reaccionan (hoy compradores por precio); uso del material; catálogo de muebles casi vacío; corrales/ambientes sin efecto. |
| Genética / crianza | 6 genes en el ADN (`BODYSHAPE-HORN-BACK-WING-FACE-RRGGBB`), herencia 50/30/20, partes visibles S109 (H0 · BK0-3 · W0-1 · FC0-1), `Role` y diales, retratos. | Más variantes de parte, pelaje (asset comprado, `_BaseColor`), Cutie Marks y linaje, límite de crías con consecuencia. |
| Cuidado / mundo vivo | Necesidades, afecto, grafo social, caricias, comer de la mano, comportamientos emergentes. | Que se vea (UI) y que pague (valuación, gate real de energía). Hoy es un sumidero. |
| Bajada (arena) | Sala por semilla, splines, pasto, paleta, ocupaciones con tiempo, choque físico con tell en movimiento, súper por carga, bases por personalidad, HUD, cámara, puente tienda↔arena, panel de bajada, costo de energía. | Pisos, seguir/retirarse, piso de buffo, permadeath (flag), glifo de base, regresión de matriz, feel a 1× validado por Juan. |
| Nube (UGS) | Auth anónima/Unity, Cloud Save (registro, muebles, inventario), scope por jugador, pull al arrancar. | Rival real (snapshots ajenos), Cloud Code activo (los endpoints del combate viejo siguen publicados), reconciliación (la nube pisa lo local). |
| Presentación / UI | 17 pantallas UITK con paleta `--mm-*`, overlay, Feel/MMFeedbacks en el prefab, toon y post proceso en la arena. | Identidad visual de la tienda (Synty crudo), onboarding, localización completa (en/es parcial, mucho hardcodeado), audio. |
| Técnica | Arquitectura por composición (deuda de partials pagada), MCP + CLI, Odin. | 0 tests de lógica pura; 9 archivos > 400 líneas; NavMesh en build; tamaño del save; builds automatizadas. |

---

## 2 · Los hitos

| # | Hito | El jugador puede… | Criterio de cierre | Sesiones |
|---|---|---|---|---|
| **H0** | **Bajada por pisos** (🟡 implementada y verificada S124, falta la prueba de Juan; plan en [[Index/26 - Plan H0 - Bajada por pisos]]) | Bajar gratis desde la tienda arriesgando la vida de la terna, encadenar pisos de enemigos y de buffo, retirarse con el botín o perderlo todo. | Bajada de 3 pisos ida y vuelta con material y vida correctos ✅; matriz con 6 combos sin cambios silenciosos ✅ (estático); Juan juega 5 bajadas a 1× y anota qué aburre. | 2-4 |
| **H1** | **Loop vertical cerrado** | Criar con el material que trajo, ver las necesidades, quedarse sin energía y tener que esperar, perder una criatura de verdad. | Uso del material en la cría (moneda de evolución, `Index/18` 1.1); necesidades en UI y gate real de energía; permadeath activo con aviso; 30 min jugables sin MCP; "¿por qué crío otro?" tiene una primera respuesta jugada. | 3-5 |
| **H2** | **Linaje y Cutie Marks** | Ganar marcas abajo, verlas en la criatura, heredarlas al criar y perderlas si nadie las vuelve a ganar. | Decisión ⭐ de Juan (Parte 3 de `Index/22`); catálogo de 10-15 marcas con evento y efecto en una frase; harness de 10 generaciones que muestra un roster que sigue cambiando; marcas visibles (shader o accesorio). | 5-8 |
| **H3** | **Rival real asíncrono** | Bajar a la caverna del día (misma semilla para todos), cruzarse con ferales = linajes perdidos de otros jugadores, rescatarlos o quedárselos; ver el eco de otra partida. | Snapshots (ADN + marcas + diales) en Cloud Save; Cloud Code por REST (regla `feedback_cloud_code`); endpoints viejos despublicados; reconciliación nube/local con fechas; botín genético decidido (8.2). | 5-8 |
| **H4** | **Fenotipo y feel** | Reconocer a una criatura a 10 m por sus genes; que un choque, una picada y una pérdida se sientan. | Variantes por parte suficientes para que dos hermanos se distingan; pelaje y color; glifo de base; feedbacks MMF por momento (regla de VFX); toon en la tienda; identidad de UI aprobada por Juan; feel a 1× validado con la vara ITB/Bad North/Mewgenics. | 6-10 |
| **H5** | **Tienda completa** | Recibir adoptantes que se enamoran (o no) de lo que hay, donar con historia, comprar muebles que cambian cómo se crían, vivir el día en bloques y ver envejecer. | Adoptantes por reacción (nunca piden); valuación que mira cuidado, rol, rareza y linaje; catálogo de muebles lleno; corrales que dirigen mutación y diales; reloj con los 4 bloques (`Index/18` Parte 3) y la noche = bajada; durabilidad con 4 salidas visibles. | 6-10 |
| **H6** | **Contenido** | Encontrar salas distintas (asegurar, nido, oscura, eco), habilidades por parte, paletas y bots con carácter; aprender jugando. | Catálogo de pisos de `Index/22` Parte 5 con al menos 5 tipos; `AbilitySO` por parte completo; 3 profundidades; onboarding = primera bajada guiada sin texto; 2 h de juego sin repetir. | 6-10 |
| **H7** | **Producción técnica** | Jugar en una build, en su idioma, sin perder el save. | Localización en/es completa (deuda `project_localization_pending`); tests de lógica pura de genética, bases, run y valuación; archivos > 400 líneas partidos (`MoriMochiAgent` 706, `AgentClash` 517); NavMesh con colliders en build; save acotado; ajustes (audio, gráficos, input con gamepad); build automatizada por CLI. | 4-6 |
| **H8** | **Audio** | Oír la tienda, la caverna y a cada criatura. | Música por bloque del día y por profundidad; SFX en cada feedback MMF; voces/gestos por personalidad. Sin relevar en el código: asumir de cero. | 3-5 |
| **H9** | **Beta cerrada** | Jugar con otros jugadores reales durante una semana. | 10-20 jugadores con cuenta UGS, telemetría mínima (bajadas, profundidad, retiradas, crías), un ciclo de correcciones, lista de bugs cerrada de [[Index/08 - Known Bugs & Checkpoints]]. | 3-4 + una semana de espera |
| **H10** | **Lanzamiento** | Comprarlo. | Plataforma decidida (PC primero es el supuesto); página de tienda, tráiler y capturas (sale de H4); demo pública (patrón Next Fest); privacidad y términos de UGS; analytics; plan de parches del primer mes; Notion consolidado y `notion-documenter` al día. | 3-5 |

**Total orientativo: 46-75 sesiones** al ritmo actual. El camino crítico es H0 → H1 → H2 → H3: hasta H3 no hay juego que mostrar a nadie de afuera; H4-H6 se pueden intercalar por lotes a partir de H2 cuando una sesión de feel lo pida.

---

## 3 · Lo que decide Juan (bloquea hitos)

1. **H1** — Uso del material: ¿moneda de evolución de `Index/18` 1.1 (crecer una parte) o costo directo de la cría?
2. **H2** — ¿Adopta el linaje como núcleo (Parte 3 de `Index/22`)? Es la premisa de H2, H3 y H5.
3. **H3** — Botín genético (8.2): ¿superar al linaje de otro deja un huevo suyo?
4. **H4** — Alcance del fenotipo: ¿más variantes de parte o variación de escala/proporción sobre las bases S109?
5. **H5** — ¿Adopción entre jugadores reales (P2P del GDD 3.2) entra en v1 o queda para después del lanzamiento?
6. **H10** — Plataforma y modelo (premium PC es el supuesto del plan).

---

## 4 · Lo que se cae del plan (y por qué)

- Combate Dragon RPS y prototipo táctico: demolidos (S75, S93, S96); el código del RPS que quede se borra en H7.
- Mercado online P2P de criaturas (GDD 3.2): reemplazado por adopción con historia (H5) y botín genético (H3).
- Batalla en la nube por turnos (GDD Etapa 2): reemplazada por la bajada; de UGS queda el patrón asíncrono (semilla + snapshot + buzón).
- Multijugador síncrono (Relay/Lobby): fuera de v1; la taxonomía de presencia del rival (`Index/22` Parte 8) lo deja como lo más caro.

---

## 5 · Cómo se usa esta nota

Al abrir cada sesión, ubicar el hito activo y su criterio de cierre; el brief de la sesión dice qué lote del hito se ataca. Al cerrar un hito, marcarlo ✅ aquí con la sesión y mover la fila de "Lo que decide Juan" que haya quedado resuelta a la sección correspondiente de `Index/22` o `Index/18`. Esta nota no reemplaza a `09 - Active Context` (estado de sesión) ni a las notas de diseño: es el mapa, no el territorio.
