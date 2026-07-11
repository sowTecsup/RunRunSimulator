---
tags: [index, combat, design]
---

# 13 - Combat Design Direction (norte de diseño)

**Status:** v2 — TESIS DE DISEÑO de Juan (S37, 2026-07-10) que baja a tierra la dirección decidida al cierre de S36. Las 5 preguntas abiertas de la v1 quedaron RESPONDIDAS (ver "Decisiones cerradas"). Diseño canónico → Notion (pendiente de volcar); esta nota es la captura viva. La implementación actual (S32–S35) es la base técnica.

## Referencias
- **Pokémon Quest**: equipo de 3, combate auto-simulado.
- **Super Auto Pets**: cada acción muestra impacto CLARO; el **momento eureka** cuando tu build hace click.

## Decisión marco: combate = PAYOFF DEL THEORYCRAFT (autobattler)
La agencia vive en armar/criar/equipar el equipo; el combate lo resuelve. El **async determinista por semilla (S32) es pilar y SE QUEDA**. El fix de "se siente pasivo" NO es input en vivo: es (a) drama legible + (b) un beat de agencia pre-pelea (la grilla de posicionamiento, ver abajo).

## Los 5 pilares (v1, siguen vigentes)
1. **3v3 team autobattler.**
2. **Cada MoriMonchi = UN rol legible.**
3. **El equipo EXPRESA el rol (palanca del eureka).**
4. **Sinergias simplificadas, telegrafiadas** — ahora: reacciones elementales de 2 ingredientes (ver tablas).
5. **Visualizer: un beat dramático por vez.**

---

# TESIS S37 — Especificación

## 1. Roles (reemplazan al sistema de Personalidades)

El sistema de Personalidades se REFORMULA como sistema de Roles — se eliminan los extras, quedan 3:

| Personalidad vieja | Rol nuevo |
|---|---|
| Tanque | **Protector** |
| DamageDealer | **Agresivo** |
| Support | **Empático** |

**El rol SE HEREDA** (genética, como las partes). Mecánica exacta de herencia: abierta (ver preguntas).

### Stat mods planos (sobre los stats natos 1–10, decisión cerrada S37)
Aplican sobre la hoja de stats visible (point-buy 18). Los tres roles son **net-zero** (suman 0):

| Rol | Vida (CON) | Ataque (ATK) | Velocidad (SPD) |
|---|---|---|---|
| Protector | **+4** | −2 | −2 |
| Agresivo | −3 | **+2** | **+1** |
| Empático | +1 | −3 | **+2** |

Un ±4 en escala 1–10 es enorme a propósito: el rol define fuertemente al MM.

### Traits de combate (v1 — regla de autoría: todo trait tiene una manera de aplicar su elemento a un aliado)
- **Protector**: cada turno pone **1 de escudo** a un aliado al azar (puede ser él mismo). Proquea elemento (a ese aliado).
- **Agresivo**: 50% de chance de golpear a un enemigo al azar que NO esté en la frontline. Cuando lo hace, proquea elemento a un aliado al azar (lore: le comparte su emoción por el combate).
- **Empático**: pega su daño completo al enemigo Y ADEMÁS cura al **aliado** con menos vida por el 50% de ese daño (decisión cerrada S37: era typo — aliado, cura adicional, no daño repartido). Proquea elemento (al aliado curado).

En el futuro puede haber más traits; la regla de diseño se mantiene.

### Impacto económico (tienda)
Modificador de precio simple: **Empático +10% · Protector +0% · Agresivo −10%**. La tabla se INVIERTE según el **arquetipo de comprador** (habrá compradores que prefieren agresivos o empáticos). Se conecta con el sistema de NPCs compradores (Index/08).

## 2. Posicionamiento — grilla hex 2-3-2 (el beat de agencia pre-pelea)

Antes de enviar al combate se muestra una grilla en forma de hex: **2 posiciones frontales, 3 medias, 2 back** (7 slots, colocás tus 3 MMs). Es la respuesta a la pregunta v1 #5.

**Targeting**: los golpes van a la fila ocupada MÁS ADELANTE. Dentro de la fila, uniforme al azar: 2 frontales llenas → 50/50 izquierda/derecha; 3 medias → ~33% cada una; etc. Esa es la gracia del posicionamiento (concentrar o repartir el fuego).

El trait del Agresivo (golpear a quien NO está en frontline) es el counter natural al posicionamiento defensivo.

## 3. Elementos — innatos, 4 definidos

Los elementos son **intrínsecos al MoriMonchi: nace con UNO** (decisión cerrada S37), heredable. Set v1 (nombres placeholder, se renombrarán): **Agua · Fuego · Electricidad · Planta**.

Ningún elemento hace nada por sí solo — solo reaccionan **en pares**.

### Afinidad → energía → proc (decisión cerrada S37)
- Cada **acción** realizada genera **1 punto de afinidad** (estuneado = no acciona = no genera).
- Con **2 de afinidad** el MM genera su **energía**: la energía ES el combustible del proc de elemento — el trait solo proquea su elemento cuando hay energía (≈ cada 2 acciones → 1 proc). Esto ritma las reacciones y las hace legibles.

### Marcas y fuentes (decisión cerrada S37)
Las marcas elementales tienen 2 **fuentes que van separadas**: aliada y enemiga — la MISMA pareja de elementos produce estados distintos según la fuente. Cuando dos marcas de elementos distintos de la misma fuente coinciden sobre una unidad → **reacción** (se consumen las marcas, se aplica el estado). "Piso Tierra" remueve una MARCA aplicada, nunca el elemento innato.

Los dos canales de aplicación:
- **Fuente ALIADA (ritmada por energía)**: el trait del rol proquea el elemento del MM a un aliado cuando tiene energía (≈ cada 2 acciones).
- **Fuente ENEMIGA (libre, cada golpe)**: **todo ataque recibido deja además la marca del elemento del atacante en la víctima**. Sin gate de energía.

Asimetría resultante (a propósito): las reacciones ofensivas detonan con frecuencia (presión constante de composición rival); las aliadas son el payoff ritmado que se ve venir.

## 4. Reacciones ALIADAS (proqueadas por dos aliados → estado positivo en el portador)

| Par | Estado |
|---|---|
| Agua × Fuego | **Vaporizado** |
| Agua × Electricidad | **Golpe Preciso** |
| Agua × Planta | **Cleanse** |
| Fuego × Electricidad | **Energizado** (prioridad) |
| Fuego × Planta | **Charcoal** |
| Electricidad × Planta | **OverGrow** |

## 5. Reacciones OFENSIVAS (proqueadas por dos enemigos → estado negativo en la víctima)

| Par | Estado |
|---|---|
| Agua × Fuego | **Boiling** |
| Agua × Electricidad | **Confuso** |
| Agua × Planta | **Leech** |
| Fuego × Electricidad | **Mareado** |
| Fuego × Planta | **Debilidad** (consume el stack) |
| Electricidad × Planta | **Piso Tierra** |

Canal resuelto (S37): la marca enemiga llega con **cada ataque recibido** (el golpe deja la marca del elemento del atacante en la víctima).

## 6. Estados (todos DE UN USO — se consumen al detonar su condición)

### Positivos
| Estado | Efecto |
|---|---|
| **Energizado** | Golpeará primero (prioridad de turno). |
| **Cleanse** | Niega el siguiente estado negativo; si no tiene ninguno, cura el 20% de la vida. |
| **Vaporizado** | El siguiente ataque que reciba: +30% de evasión. Se mantiene hasta que logre evadir un ataque. |
| **Golpe Preciso** | El siguiente ataque tiene más chance de crítico. Se mantiene hasta que logre un crítico. |
| **Charcoal** | El siguiente ataque que reciba devuelve la mitad del daño al agresor. |
| **OverGrow** | Duplica el escudo del MoriMonchi. |

### Negativos
| Estado | Efecto |
|---|---|
| **Boiling** | Vulnerable: el siguiente ataque que reciba hace más daño. Se mantiene hasta recibir un golpe. |
| **Debilidad** | El siguiente ataque que reciba IGNORA defensa. Se mantiene hasta recibir un golpe. |
| **Confuso** | La siguiente acción siempre falla. |
| **Leech** | Se le sustrae algo de vida y se le da al MM que activó la reacción. |
| **Mareado** | Chance de golpearse a sí mismo o a un aliado por una cantidad FIJA (no proquea nada — simple a propósito). |
| **Piso Tierra** | Remueve una marca elemental al azar del MM. |

## 7. Equipo (take S37 — simplificación de pasivas)

- Los ítems SIGUEN modificando stats (pipeline `EquipmentSO`/StatSheet intacto).
- Las pasivas se simplifican: **ítems con número de usos** (ej. 3 usos) + **reglas de disparo/restricción** (ej. "cuando tenés menos de X% de vida"). Efectos del estilo: **curar, infligir daño, aplicar estados/marcas al portador o al oponente**.

## 8. Mejora de partes (take S37)

Abrirán **opciones de ataque** para los MoriMonchis. En esta update **siguen vacías** — hasta aterrizar el combate.

---

## Decisiones CERRADAS en S37 (respuestas de Juan)
1. **Energía = proc de elemento** (2 afinidad → 1 energía → el trait proquea; ritma las reacciones). También responde v1 #1-2: roles = 3 (Protector/Agresivo/Empático), derivan de genética (heredados, reemplazan Personalidad).
2. **1 elemento innato por MM**, heredable; **Piso Tierra quita marcas**, no el innato.
3. **Empático cura al ALIADO** con menos vida, como cura ADICIONAL (50% del daño infligido, daño completo al enemigo).
4. **Stat mods del rol sobre la hoja 1–10** (point-buy 18); net-zero por diseño.
5. Targeting/posición (v1 #3): grilla hex 2-3-2, fila más adelantada primero, uniforme dentro de fila.
6. Elementos S35 (v1 #4): el contenido de 6 elementos + recetas de stacks se **ARCHIVA**; el motor se retargetea.
7. Beat de agencia pre-pelea (v1 #5): la grilla de posicionamiento.
8. **Canal de marcas ofensivas**: todo ataque recibido deja la marca del elemento del atacante en la víctima (sin gate de energía; la energía solo ritma el proc aliado del trait).
9. **Rol hereda TODO de Personality**: combate, economía Y comportamiento de mundo (un Protector deambula distinto que un Agresivo). Las personalidades extra se eliminan.
10. **Escudo = HP temporal persistente**: absorbe daño antes que la vida, se acumula y persiste durante el combate hasta consumirse (cap: tuning).
11. **Orden de implementación: SIM 3V3 CORE PRIMERO** — roles (stats+traits) + grilla 2-3-2 + targeting en el sim determinista, SIN elementos; la capa elemental entra después sobre un 3v3 que ya corre.
12. **Consecuencias 3v3**: equipo ganador → UNA unidad al azar evoluciona; equipo perdedor → UNA unidad al azar rollea muerte permanente con **5%** (baja de 15%).
13. **El 1v1 DEJA DE EXISTIR**: siempre 3v3. La escena de combate actual (`CombatVisualizerMM`) se reusa como escenario.
14. **Herencia de rol confirmada**: el Rol ES la personalidad y se hereda (campo plano en DNA, mint azar, cría 50/50 padre/madre, FUERA del genetic string — el async viaja por JSON completo).
15. **Frontline = las 2 posiciones frontales**; con 3 vivos siempre hay ≥1 fuera del front. Fallback del Agresivo: si no queda nadie fuera de la fila frontal viva, al acertar su 50% el efecto se convierte en **generar energía a un aliado** (relevante desde la capa elemental; en el core sin elementos es no-op logueado).

## Qué SOBREVIVE del motor (mapeo actualizado)
- **Sim determinista por semilla (S32)** — se extiende a 3v3.
- **Motor de stacks + recetas (`SynergyTableSO`, S32/S35)** — mapea casi 1:1: marca elemental = stack con dimensión nueva de FUENTE (aliada/enemiga); reacción = receta de 2 variedades que detona y quema; estados = leaves `SynergyEffectBase` nuevos. El engine de "variedades requeridas → detonan sobre el portador → queman FIFO → efecto polimórfico" es EXACTAMENTE lo que las reacciones necesitan.
- **Pipeline de equipo** — stats quedan; las pasivas migran a "usos + regla de disparo" (evolución de `CombatProcEffect`).
- **Motor del visualizer** (replay, nodos, barras, chips) — gana 6 combatientes + grilla.
- **Record granular por proc (S35, `TargetStatusAfter`)** — sirve tal cual para narrar marcas → reacción.

## Qué CAMBIA (lift de ingeniería)
- **1v1 → 3v3 con posiciones**: sim (`CombatService`/`SimulateCore`), `CombatRecord`/snapshots (equipos + grilla), matchmaking JS, visualizer (6 unidades). Sigue siendo el trabajo principal.
- **Personalidad → Rol**: `CreatureDNA` + herencia + UI + comportamiento de mundo (el Rol hereda TODA la función de Personality, decisión S37).
- **Elemento innato en el DNA**: nuevo gen heredable (¿entra al genetic string `ToStringID()`? — contrato de red, decidir con cuidado).
- **Escudo**: mecánica nueva del sim (Protector, OverGrow).
- **Contenido S35** (ítems de elementos, recetas v1, labels): se archiva/re-autora.

## Preguntas abiertas (S37+)
1. **Herencia de ELEMENTO**: regla exacta (¿50/50 como el rol? ¿mutación?) — el rol ya quedó cerrado (decisión 14).
2. **Marcas**: ¿persisten indefinidamente hasta reaccionar? ¿stackea el mismo elemento? ¿cap de marcas por unidad? ¿dos MMs del MISMO elemento nunca reaccionan entre sí (pares iguales no hacen nada)?
3. **Cleanse**: el "si no tiene ningún estado negativo cura 20%" — ¿se evalúa al aplicarse o queda armado hasta el final?
4. **Confuso**: "la siguiente acción" — ¿incluye el proc del trait o solo el ataque?
5. **Magnitudes de tuning**: Boiling (+% daño), Leech (cantidad), Mareado (cantidad fija), Golpe Preciso (+% crit), cap del escudo, clamps de la hoja tras el mod de rol (¿[1,10]? ¿puede quedar <1?).
6. **Compradores por arquetipo** (tabla invertida): ¿esta update o después?
7. **Frontline del Agresivo**: si SOLO la fila frontal está ocupada, ¿el 50% falla, pega al front, o re-rollea?
8. **Afinidad y ataque**: ¿el golpe del Agresivo a backline y la cura del Empático cuentan como "acción" para afinidad? (probable sí — toda acción genera 1).

## ROADMAP de implementación (definido con Juan al cierre de S37)

**✅ Fase 1 — SIM 3V3 CORE (S37, HECHO Y VERIFICADO EN PLAY)**: Role heredable en DNA + RoleTableSO + capa de rol en stats + SimulateCore por equipos (orden por velocidad dinámica, targeting 2-3-2, traits, escudo, consecuencias 1-evolución/5%-muerte) + records por unidad + DevConsole 3v3 + Verify Determinism OK.

**Fase 2 — UI DE POSICIONAMIENTO + FLUJO LOCAL COMPLETO** (próxima sesión, prioridad de Juan):
- UI de grilla hex 2-3-2 pre-pelea: elegir 3 MoriMonchis + colocarlos en slots (2 front / 3 mid / 2 back) — el beat de agencia.
- Pasa el lineup real a `SimulateLocal(teams, rows)` (hoy solo el default {Front,Front,Mid}).
- Rediseño de la tab local del Combat Panel: selección de equipo + grilla + lanzar combate. Que TODO el flujo local 3v3 corra desde UI sin dev console.

**Fase 3 — LIMPIEZA DE LEGACY** (pedida por Juan):
- **Hard reset / migración de MMs antiguos**: decidir wipe registry vs botón de migración (roles ya rerolleados en S37; el elemento innato llegará con la capa elemental y necesitará su propio backfill).
- **Items ya NO proquean estados**: retirar/re-autorar `CombatProcEffect` y los leaves de elementos S35 (Static/Pulse/Steel/Mist/etc. como procs de ítem) — nuevo diseño: ítem = mods de stats + **N usos con regla de disparo** (curar / dañar / aplicar estado, restricciones tipo "con menos de X% de vida").
- Archivar recetas viejas de `SynergyTableSO` (Explosión tóxica, Robo de vida, Cortocircuito…) — las reacciones nuevas son elementales por fuente.
- Retirar la sobrecarga transicional `SimulateLocal(a,b)` y el camino 1v1 del Combat Panel cuando la UI 3v3 exista.
- Barrer referencias muertas (labels/popups de kinds retirados, EvolutionChance sin uso, etc.).

**Fase 4 — VISUALIZER 3V3**: replay de 6 unidades + grilla en escena (reusar `CombatVisualizerMM` como escenario, decisión S37), quitar el guard de `CanReplay`, tarjetas de historial 3v3, storytelling (ghost bar/banner) al servicio del nuevo formato.

**Fase 5 — ASYNC 3V3**: enqueue de EQUIPO con lineup, JS matchmaker de equipos (blob con 6 snapshots + rows), `ApplyResult` de equipos, test online end-to-end.

**Fase 6 — CAPA ELEMENTAL**: elemento innato en DNA + herencia + backfill, afinidad/energía, marcas por fuente (golpe recibido = marca enemiga / trait con energía = marca aliada), reacciones y los 12 estados de un uso (leaves nuevos sobre el motor de recetas).

**Fase 7 — MIGRACIÓN TOTAL PERSONALITY→ROLE + ECONOMÍA**: el Rol absorbe el comportamiento de mundo (PersonalityProfileSO→3 perfiles, breeding affinity, NameTag, UI), retirar el enum Personality (⚠️ rompe saves → coordinar con el hard reset de Fase 3), PriceModifier en venta + arquetipos de comprador con tabla invertida.

## Opinión del orquestador (registrada)
La tesis es un buen aterrizaje: 3 roles net-zero legibles, elementos innatos con reacciones de 2 ingredientes por fuente (asimetría aliado/enemigo del mismo par = elegante y telegrafiable), y la grilla 2-3-2 como beat de agencia. El motor S32/S35 se retargetea casi 1:1 (marcas=stacks, reacciones=recetas, estados=leaves). El canal de marcas quedó cerrado con una asimetría sana: ofensivas fluyen con cada golpe (presión constante), aliadas ritmadas por energía (payoff legible). Orden acordado: **sim 3v3 core (roles+grilla+targeting) → capa elemental → contenido de estados**; la herencia de rol/elemento (pregunta #1) hay que cerrarla antes de tocar `CreatureDNA` porque puede tocar el genetic string (contrato de red).

Relacionado: [[Index/03 - Combat]], [[Index/09 - Active Context]], [[Index/08 - NPC Customers]], [[Index/12 - Unity MCP]].
