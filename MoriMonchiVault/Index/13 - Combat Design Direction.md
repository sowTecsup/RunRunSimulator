---
tags: [index, combat, design]
---

# 13 - Combat Design Direction (norte de diseño)

**Status:** v4 — MODELO S46 VIGENTE (energía eliminada) + visualizer 3v3 implementado y aprobado visualmente por Juan (S45-S47). La tesis S37 bajó a código (Fases 1-3, 6, 7 hechas), los eventos elementales viven en el record desde S41 (ya no log-only), y el replay quedó cerrado en S47 (escudo por ronda, coreografía de pasivas, barra minimal). Falta: async 3v3 (F5) y economía (F7). Diseño canónico volcado a Notion en la consolidación 2026-07-20. OJO: las secciones históricas de abajo que mencionan "energía" describen el modelo pre-S46 — el modelo vigente está en la sección siguiente.

## MODELO VIGENTE S46–S47 (reemplaza el ciclo de energía)

La **energía como recurso contable DEJA DE EXISTIR** (decisión mayor S46: el modelo era ilegible). Quedan solo **afinidad** (2 circulitos) y **marcas**, con exactamente dos vías:

1. **Afinidad → marca PROPIA**: cada acción genera 1 punto de afinidad; al llegar a 2 se consume y el MM aplica su propia marca elemental **sobre sí mismo, en el mismo turno** (≈ cada 2 acciones). Estuneado no genera; Confuso/Mareado sí (accionaron y fallaron).
2. **Pasiva de rol → marca a OTRO**: sin gate de recurso, **todos los turnos**. El Agresivo marca a un aliado al azar acierte o no su activa (su roll 50% queda como puro targeting de la activa, ya no comparte energía).

Reglas asociadas del modelo vigente:
- **Marca duplicada SOBREESCRIBE** (ya no es no-op como en S39).
- **Orden del turno unificado** para los 3 roles: intención → daño (+marca enemiga) → afinidad (+marca propia) → pasiva (+marca al aliado). El escudo del Protector es post-golpe.
- **Escudo por RONDA** (S43 decisión de Juan, S47 implementación): se disipa al cierre de cada ronda — "para no quitarle el rol a la curación". Reemplaza el "persistente hasta consumirse" de la decisión S37 #10.
- El volumen alto de marcas es INTENCIONAL en esta etapa: legibilidad antes que balance.

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

### Afinidad → energía → proc (decisión cerrada S37 — **HISTÓRICO, reemplazado por el MODELO S46** de arriba)
- Cada **acción** realizada genera **1 punto de afinidad** (estuneado = no acciona = no genera).
- ~~Con **2 de afinidad** el MM genera su **energía**: la energía ES el combustible del proc de elemento~~ → S46: con 2 de afinidad el MM aplica su **marca propia** directamente, mismo turno; la energía no existe.

### Marcas y fuentes (decisión cerrada S37)
Las marcas elementales tienen 2 **fuentes que van separadas**: aliada y enemiga — la MISMA pareja de elementos produce estados distintos según la fuente. Cuando dos marcas de elementos distintos de la misma fuente coinciden sobre una unidad → **reacción** (se consumen las marcas, se aplica el estado). "Piso Tierra" remueve una MARCA aplicada, nunca el elemento innato.

Los dos canales de aplicación (redefinidos en S46):
- **Fuente ALIADA (ritmada por afinidad)**: (a) marca PROPIA cada 2 acciones vía afinidad, y (b) pasiva de rol que marca a OTRO aliado todos los turnos, sin gate de recurso.
- **Fuente ENEMIGA (libre, cada golpe)**: **todo ataque recibido deja además la marca del elemento del atacante en la víctima**. Sin gate.

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

## 6. Estados — TABLA IMPLEMENTADA (S39, enum `ElementalState`, lógica en `CombatElements` + `CombatService`)

Todos DE UN USO. "Inmediato" = se resuelve al detonar la reacción, nunca queda armado. "Armado" = vive en `Combatant.States` hasta su condición de consumo (sin duplicados: re-aplicar no hace nada). Knobs en `CombatManagerSO` → bloque **Elemental** (valores = default v1, tuning pendiente con data real).

### Positivos (reacción de fuente ALIADA → sobre el portador de las marcas)
| Estado | Par | Efecto implementado | Consumo | Knob (default) |
|---|---|---|---|---|
| **Energizado** | Fuego × Electricidad | Actúa PRIMERO en el orden de la siguiente ronda (clave de sort previa a SPD). | Al actuar con prioridad. | — |
| **Cleanse** | Agua × Planta | Al aplicarse: purga el primer estado negativo armado; si no tiene ninguno, cura % de MaxHp. | Inmediato. | `CleanseHealPercent` (0.20) |
| **Vaporizado** | Agua × Fuego | Suma bonus plano de chance de evasión. | Al lograr esquivar un ataque. | `VaporizadoEvaBonus` (0.30) |
| **GolpePreciso** | Agua × Electricidad | Suma bonus plano de chance de crítico. | Al conectar un crítico. | `GolpePrecisoCritBonus` (0.25) |
| **Charcoal** | Fuego × Planta | Devuelve % del daño recibido al agresor (puede matarlo). | Al recibir un golpe conectado. | `CharcoalReflectPercent` (0.50) |
| **OverGrow** | Electricidad × Planta | Duplica el escudo actual (`Shield *= 2`; 0 queda 0). | Inmediato. | — |

### Negativos (reacción de fuente ENEMIGA → sobre la víctima; el "reactor" = quien aplicó la 2ª marca)
| Estado | Par | Efecto implementado | Consumo | Knob (default) |
|---|---|---|---|---|
| **Boiling** | Agua × Fuego | El próximo golpe que reciba hace +% de daño (pre-escudo). | Al recibir un golpe conectado. | `BoilingDamageBonus` (0.30) |
| **Debilidad** | Fuego × Planta | El próximo golpe que reciba IGNORA su DEF (reduction = 0). | Al recibir un golpe conectado. | — |
| **Confuso** | Agua × Electricidad | Su próxima acción falla COMPLETA (no ataca, no traits, no ítems). SÍ genera afinidad (accionó y falló). | Al actuar. | — |
| **Leech** | Agua × Planta | Drena HP fijo al instante y se lo da al reactor (caps: no baja de 0, no cura sobre MaxHp). | Inmediato. | `LeechAmount` (4) |
| **Mareado** | Fuego × Electricidad | En su próxima acción: % de chance de golpearse a sí mismo o a un aliado (uniforme, incluye self) por daño FIJO en vez de atacar; si resiste el roll, actúa normal. Consume igual. Genera afinidad. | Al actuar. | `MareadoChance` (0.50) · `MareadoDamage` (3) |
| **PisoTierra** | Electricidad × Planta | Remueve UNA marca al azar del portador (cualquier canal); sin marcas = no-op logueado (no consume rng). | Inmediato. | — |

Nota Boiling+Debilidad: si la víctima tiene ambos armados, ambos aplican y se consumen en el mismo golpe.

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
- ~~**Motor de stacks + recetas (`SynergyTableSO`)** para las reacciones~~ → **DIVERGENCIA S39**: las reacciones se implementaron en `CombatElements` (clase estática dedicada: marcas por fuente + tablas de pares + estados one-use en `Combatant.States`), NO sobre el motor de recetas. Más simple y legible que forzar el mapeo marca=stack. El motor de sinergias fue RETIRADO COMPLETO en el mismo S39 (decisión de Juan): clases, resolver hooks, campo de config y asset borrados.
- **Pipeline de equipo** — stats quedan; las pasivas migran a "usos + regla de disparo" (evolución de `CombatProcEffect`).
- **Motor del visualizer** (replay, nodos, barras, chips) — gana 6 combatientes + grilla.
- **Record granular por proc (S35, `TargetStatusAfter`)** — sirve tal cual para narrar marcas → reacción.

## Qué CAMBIA (lift de ingeniería)
- **1v1 → 3v3 con posiciones**: sim (`CombatService`/`SimulateCore`), `CombatRecord`/snapshots (equipos + grilla), matchmaking JS, visualizer (6 unidades). Sigue siendo el trabajo principal.
- **Personalidad → Rol**: `CreatureDNA` + herencia + UI + comportamiento de mundo (el Rol hereda TODA la función de Personality, decisión S37).
- **Elemento innato en el DNA**: nuevo gen heredable (¿entra al genetic string `ToStringID()`? — contrato de red, decidir con cuidado).
- **Escudo**: mecánica nueva del sim (Protector, OverGrow).
- **Contenido S35** (ítems de elementos, recetas v1, labels): se archiva/re-autora.

## Preguntas abiertas — RESUELTAS EN S39 (decisiones de Juan + implementación)
1. ✅ **Herencia de ELEMENTO**: 50/50 padre/madre + MUTACIÓN (elemento random) — knob `ElementMutationChance` (0.10) en `InheritanceOddsTableSO`.
2. ✅ **Marcas**: máx 1 por elemento+fuente por unidad (S39: re-aplicar = no-op → **S46: sobreescribe**); persisten hasta reaccionar o PisoTierra; mismo elemento nunca reacciona. Máx teórico 8 marcas por unidad.
3. ✅ **Cleanse**: se evalúa AL APLICARSE (purga un negativo o cura 20% en el acto) — un beat, legible. Revisable si Juan prefiere que quede armado negando el próximo negativo.
4. ✅ **Confuso**: falla la ACCIÓN COMPLETA del turno (ataque, trait e ítems); genera afinidad igual.
5. 🟡 **Magnitudes de tuning**: defaults v1 en la tabla de arriba, todos knobs — tuning con data real pendiente. Cap del escudo sigue abierto; clamp de hoja post-rol se mantiene [1,10].
6. ⏳ **Compradores por arquetipo** (tabla invertida): después (con PriceModifier, Fase economía).
7. ✅ **Frontline del Agresivo sin backline**: ~~el 50% acertado se convierte en COMPARTIR ENERGÍA~~ → obsoleto con S46 (la energía no existe): el roll 50% es puro targeting de la activa y la pasiva marca a un aliado todos los turnos, acierte o no.
8. ✅ **Afinidad**: toda acción de turno genera 1 (incluye turnos de Confuso/Mareado); estuneado NO genera; muerte por tick NO genera.

## Preguntas abiertas (S39+)
1. ✅ **Motor de sinergias**: RETIRADO por decisión de Juan (mismo S39) — `SynergyTableSO`/`SynergyRule`/`SynergyEffectBase` borrados, `CheckSynergies` y los helpers bearer fuera de `CombatResolver`, campo `Synergies` fuera de `CombatManagerSO`, asset borrado. Los kinds `Synergy` del enum y los mapeos del visualizer quedan (append-only, inertes).
2. **Cap del escudo** (OverGrow lo duplica — ¿hasta cuánto?).
3. **Ítems que aplican estados/marcas** (la spec §7 los prevé): ¿se autoran ahora que existen los estados, o post-visualizer?

## ROADMAP de implementación (definido con Juan al cierre de S37)

**✅ Fase 1 — SIM 3V3 CORE (S37, HECHO Y VERIFICADO EN PLAY)**: Role heredable en DNA + RoleTableSO + capa de rol en stats + SimulateCore por equipos (orden por velocidad dinámica, targeting 2-3-2, traits, escudo, consecuencias 1-evolución/5%-muerte) + records por unidad + DevConsole 3v3 + Verify Determinism OK.

**Fase 2 — UI DE POSICIONAMIENTO + FLUJO LOCAL COMPLETO** (próxima sesión, prioridad de Juan):
- UI de grilla hex 2-3-2 pre-pelea: elegir 3 MoriMonchis + colocarlos en slots (2 front / 3 mid / 2 back) — el beat de agencia.
- Pasa el lineup real a `SimulateLocal(teams, rows)` (hoy solo el default {Front,Front,Mid}).
- Rediseño de la tab local del Combat Panel: selección de equipo + grilla + lanzar combate. Que TODO el flujo local 3v3 corra desde UI sin dev console.

**✅ Fase 3 — LIMPIEZA DE LEGACY (S39, HECHA)** — tab 1v1 retirada (tabs: Online/Resultados/Historial/Equipo 3v3, la 3v3 entró a navegación por teclado), overload transicional `SimulateLocal(a,b)` eliminado, TODOS los procs de ítems retirados (`CombatProcEffect.cs` borrado) → nuevo sistema `ItemUseEffect` (N usos + regla Always/SelfHpBelow, leaves Heal/Damage, sin rolls), recetas de SynergyTable archivadas (asset vacío), `EvolutionChance`/`TriggerType` fuera. Hard reset: Juan wipeó local+nube antes de S39.

**✅ Fase 6 — CAPA ELEMENTAL, CORE SIM (S39, ADELANTADA Y HECHA)** — `Element` en DNA (campo plano, mint random, herencia 50/50+mutación), afinidad/energía, marcas por fuente, 12 reacciones/estados (tabla implementada en §6), todo verificado por log en Play + Determinism OK. **Falta de F6**: eventos elementales al RECORD (hoy log-only — prerequisito del visualizer) y contenido de ítems que apliquen estados.

**✅ Fase 7 — PERSONALITY→ROLE TOTAL (S39, ADELANTADA Y HECHA)** — enum `Personality` y `PersonalityProfileSO` eliminados; `RoleWorldProfileSO` (3 perfiles de mundo), `BreedingAffinityTableSO` re-keyeada 3×3 por Role, NameTag/UI/containers por rol. **Falta de F7**: PriceModifier en venta + arquetipos de comprador (economía).

**Detalle original de Fase 3 (histórico):**
- **Hard reset / migración de MMs antiguos**: decidir wipe registry vs botón de migración (roles ya rerolleados en S37; el elemento innato llegará con la capa elemental y necesitará su propio backfill).
- **Items ya NO proquean estados**: retirar/re-autorar `CombatProcEffect` y los leaves de elementos S35 (Static/Pulse/Steel/Mist/etc. como procs de ítem) — nuevo diseño: ítem = mods de stats + **N usos con regla de disparo** (curar / dañar / aplicar estado, restricciones tipo "con menos de X% de vida").
- Archivar recetas viejas de `SynergyTableSO` (Explosión tóxica, Robo de vida, Cortocircuito…) — las reacciones nuevas son elementales por fuente.
- Retirar la sobrecarga transicional `SimulateLocal(a,b)` y el camino 1v1 del Combat Panel cuando la UI 3v3 exista.
- Barrer referencias muertas (labels/popups de kinds retirados, EvolutionChance sin uso, etc.).

**Fase 4 — VISUALIZER 3V3 (SIGUIENTE, orden de Juan)**: **paso 0 obligatorio = enriquecer el record** (el visualizer NO lee el log de texto): eventos elementales en `CombatProcEvent` (o lista nueva por turno) + `CombatUnitState` con marcas elementales/estados armados/energía — todo aditivo. Después: replay de 6 unidades + grilla en escena (reusar `CombatVisualizerMM`), quitar guard de `CanReplay`, tarjetas 3v3, chips de marca por canal, popups de reacción, iconos de los 12 estados, pips de energía, storytelling (ghost bar/banner).

**Visión de Juan para F4 (cierre S40, 2026-07-12)** — resuelve el problema #1 (peleas legibles):
- **Cámaras**: cámara general de la escena + **una vcam Cinemachine SIMPLE dentro de cada MM** del replay. En su turno, su cámara lo enfoca y se acerca hacia su objetivo al pegar — sistema sencillo por **cambio de prioridad**. Nosotros dejamos los MMs seteados con la vcam adentro; **Juan se encarga del foco/prioridad**.
- **Barra superior = orden de acción** con cartas por unidad: marcas ALIADAS en el borde SUPERIOR de la carta, marcas ENEMIGAS en el borde INFERIOR, y **2 circulitos de afinidad** dentro de la carta que se llenan con cada acción (al llenarse los 2 → energía, se vacían).
- **Parte inferior**: las plataformas colocadas (las grillas 2-3-2 enfrentadas).
- El **log ya narra en el orden del visualizer** (ajuste S40 post-cierre): golpe (quién→quién, daño) → marca enemiga → reacción/estado → cura Empático → `[afinidad]` +1 por acción → `[energía]` al convertir. El replay debe seguir ese mismo beat.

**Fase 5 — ASYNC 3V3 (AL FINAL, orden de Juan)**: enqueue de EQUIPO con lineup, JS matchmaker de equipos (blob con 6 snapshots + rows), `ApplyResult` de equipos, test online end-to-end.

## PLAN S40 — REFACTOR DE EXTENSIÓN — ✅ EJECUTADO ENTERO EN S40 (2026-07-12, verificado por paridad de log al hash + Determinism OK; detalle en [[Index/09 - Active Context]])

**Meta de Juan**: poder tweakear desde assets, sin tocar código: los roles, sus stats, sus pasivas (y que un rol pueda tener pasivas distintas a futuro), sus activas de combate, la tabla de reacciones elementales y lo que hace cada reacción. **El patrón de referencia es la sección de ítems (`EquipmentSO.Effects`): lista polimórfica Odin con leaves cerrados parametrizados.**

**A. `RoleTableSO` v2** — por rol: sección de stats fija (`ConMod/AtkMod/SpdMod/PriceModifier`) + `[OdinSerialize] List<RolePassiveBase> Passives` (leaves iniciales: `ShieldAllyPassive {AmountPerTurn}` Protector · `HealLowestAllyOnHitPassive {PercentOfDamage}` Empático; cada leaf declara su target de marca elemental) + `List<RoleActiveBase> Actives` (leaf inicial: `BacklineHunterActive {Chance}` Agresivo, con fallback de compartir energía). El sim consulta hooks (OnTurnStart / targeting override / OnDamageDealt) iterando las listas en orden — determinismo intacto. Botón "Poblar v2" reproduce el contenido actual EXACTO.

**B. `ElementTableSO` (NUEVO)** — 3 secciones: (1) identidad de elementos (display name — string v1, key de localización a futuro — + color UI); (2) definiciones de estados (display name + descripción + magnitudes — **los 8 knobs elementales SE MUDAN acá** desde CombatManagerSO, cada estado con sus números); (3) tabla de reacciones: `List<ElementReaction> {ElementA, ElementB, Fuente} → List<ReactionEffectBase>` polimórfica (leaves: `ArmStateEffect {estado}`, `HealEffect`, `DamageEffect`, `RemoveMarkEffect`, `GrantEnergyEffect`, `DoubleShieldEffect`) — remapeable y componible. `CombatElements` pasa de dueño de tablas hardcodeadas a EJECUTOR del SO. Cableado: `CombatManagerSO.Elements` (patrón Roles). Botón "Poblar v1" reproduce las 12 reacciones actuales.

**C. Descomposición de `CombatService`** (regla 11, jamás partial): `TakeTurn` se parte en colaboradores estáticos — `CombatRoleHooks` (pasivas/activas de rol), `CombatItems` (paso N usos), `CombatStrike` (evasión/crit/daño/escudo/Charcoal/marca enemiga). El service queda núcleo delgado: validación, orden de ronda, secuencia de pasos, consecuencias.

**Verificación S40 (es un REFACTOR — cero cambio de gameplay)**: capturar log de una pelea de referencia con semilla fija ANTES; tras el refactor, misma semilla → log IDÉNTICO (paridad al hash) + Verify Determinism. Wiring de assets por MCP (crear/poblar ElementTable, re-poblar RoleTable v2, wirear CombatManager, retirar knobs viejos).

**Contexto original del pedido (cierre S39):**
1. **Tablas elementales a SO**: los pares→estado y la identidad de elementos/estados NO deben vivir hardcoded en `CombatElements.cs` — todo setting/contenido global = servicio respaldado por SO (tweaks, renames, ruteo a localización futura). Patrón: RoleTableSO.
2. **Descomponer `CombatService`/`TakeTurn`** (creció monstruoso con la capa elemental): regla 11 — mini-managers con una responsabilidad coordinados por un núcleo delgado. Juan reafirmó rechazo TOTAL a `partial` (la remoción de partials existentes sigue en Index/11 Fases 6-9).
3. **`RoleTableSO` extensible**: además de la sección de stats, listas polimórficas SEPARADAS de **Pasivas** y **Activas** por rol (patrón `EquipmentSO.Effects` con Odin), para agregar traits futuros sin tocar el sim. El diseño se construye PARA la extensión.

**Pendientes menores**: economía de F7 (PriceModifier + arquetipos), ítems con estados, tuning de knobs con data real.

## Opinión del orquestador (registrada)
La tesis es un buen aterrizaje: 3 roles net-zero legibles, elementos innatos con reacciones de 2 ingredientes por fuente (asimetría aliado/enemigo del mismo par = elegante y telegrafiable), y la grilla 2-3-2 como beat de agencia. El motor S32/S35 se retargetea casi 1:1 (marcas=stacks, reacciones=recetas, estados=leaves). El canal de marcas quedó cerrado con una asimetría sana: ofensivas fluyen con cada golpe (presión constante), aliadas ritmadas por energía (payoff legible). Orden acordado: **sim 3v3 core (roles+grilla+targeting) → capa elemental → contenido de estados**; la herencia de rol/elemento (pregunta #1) hay que cerrarla antes de tocar `CreatureDNA` porque puede tocar el genetic string (contrato de red).

Relacionado: [[Index/03 - Combat]], [[Index/09 - Active Context]], [[Index/08 - NPC Customers]], [[Index/12 - Unity MCP]].
