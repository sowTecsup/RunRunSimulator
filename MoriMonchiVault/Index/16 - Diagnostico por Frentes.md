---
tags: [index, design, diagnostico]
---

# 16 - Diagnóstico por Frentes (S72)

> **Sesión 72 (2026-08-07).** Juan pidió retomar el análisis del juego "por distintos frentes". Si [[Index/15 - Theorycrafting S71 - Autobattler y Marketing]] miró hacia AFUERA (mercado, referentes, pitch), esta nota mira hacia ADENTRO: qué existe de verdad en cada frente, qué le falta, y **con qué se conecta**.
>
> **Método:** ninguna afirmación de acá sale de una nota del vault. Todo se leyó del `.cs`, del asset por MCP, o del archivo de save. Las notas Index se usaron solo para saber dónde mirar — y varias resultaron stale (ver [[#Deuda de documentación]]).
>
> **Estado: diagnóstico entregado, esperando decisión de Juan.** Nada de esto bajó a código.

---

## TL;DR

1. **El problema de MoriMonchis no son los sistemas: son los ACOPLES.** Hay siete frentes construidos, varios con calidad real. Casi ninguno alimenta a otro. El juego es un archipiélago.
2. **"Genética visible" — el pilar #1 y el gancho de marketing de S71 — hoy son 4 cuerpos.** Hay 500 genotipos posibles (4 body shapes × 5 brazos × 5 ojos × 5 bocas) pero el visualizador instancia **un solo prefab de cuerpo** elegido por hash del `BodyShapeID`, y hay **4 prefabs en el banco**. Brazos, ojos y bocas son datos invisibles: aportan stats y nombre, no se ven. El "mirá el bicho que me salió" de Wobbledogs no tiene con qué salir.
3. **El combate 3v3 no tiene rival.** El motor está completo (3 roles, 4 elementos, 12 reacciones, grilla 2-3-2, sim determinista) pero la única UI que lo lanza te hace llenar **los dos tableros con tus propias criaturas**. El async sigue siendo **1v1** (manda un `creatureId`, no un equipo). La Recomendación B de S71 — "mostrar la composición rival" — no se puede implementar: no hay rival que mostrar.
4. **El cuidado no paga NI se ve.** Ningún sistema fuera de `World/` y `Data/Social/` lee `Affect`. Ningún panel de UI muestra Health/Energy/Affect: el jugador no puede ver las necesidades en ninguna de las 17 pantallas. El `Energy` se descuenta al criar y al encolar combate, pero **ningún chequeo lo exige**: es un número que baja y no bloquea nada.
5. **La economía funciona de punta a punta y es el frente más sano** — el cliente entra, mira, elige, hace fila, ofrece, negocia y paga. Pero su valuación mira **solo tiers, stats, cantidad de cría y winrate**. No mira cuidado, rol, elemento, rareza, linaje, edad ni shiny. Los 3 arquetipos de comprador son el mismo comprador con distintos pesos: todos quieren lo más caro.
6. **La muerte permanente es exclusivamente de combate.** No hay muerte por vejez ni por abandono. Se puede tener una criatura en Affect −100 y Health 0 indefinidamente: solo cambia la carita y deja de socializar.
7. **Riesgo técnico medido:** el registro de 18 criaturas pesa **1,48 MB** con solo 6 combates guardados, y se empuja a Cloud Save como un blob único. El `CombatHistory` es explícitamente ilimitado.
8. **El cuello de botella real es el punto 3**, no el punto 2 ni el 4: sin oponente no hay ciclo, y sin ciclo no se puede responder la pregunta que tiene a Juan trabado hace dos semanas.

---

## Mapa rápido

| # | Frente | Salud | Una frase |
|---|--------|-------|-----------|
| 1 | Criatura / genética | 🔴 | Genotipo rico, fenotipo de 4 siluetas |
| 2 | Crianza / ciclo de vida | 🟡 | Herencia completa; el ciclo de vida no hace nada |
| 3 | Combate | 🔴 | Motor excelente sin partido que jugar |
| 4 | Economía / clientes | 🟢 | El único loop cerrado; valúa poco |
| 5 | Mundo vivo / cuidado | 🔴 | Sumidero confirmado: entra trabajo, no sale nada |
| 6 | Presentación | 🟡 | Estructura completa, identidad en cero |
| 7 | Infraestructura | 🟢 | Sana; un riesgo de tamaño de save |

🟢 sirve como está · 🟡 funciona pero le falta la mitad · 🔴 bloquea a otros frentes

---

## 1 · Criatura / genética 🔴

**Qué existe.** `CreatureDNA` (`Data/Genetics/CreatureDNA.cs`) carga: 4 IDs de parte (`:14-17`), `BaseColor` + `SecondaryColor` derivado (`:19-23`), `FurType` (`:25`), `IsShiny` (`:26`), `Role` (`:41`), `Element` (`:45`), diales `Sociability`/`Boldness` (`:47-48`), tier por slot (`:56-59`), 3 stats natos + 3 derivados de equipo (`:62-69`), linaje, historial de combate y `NeedsState`. El genetic string es `BODYSHAPE-ARM-EYE-MOUTH-RRGGBB` (`:7`).

**Contenido real, contado por MCP:**

| Database | Entradas |
|---|---|
| `BodyShapeDatabase` | **4** |
| `ArmDatabase` | **5** |
| `EyeDatabase` | **5** |
| `MouthDatabase` | **5** |
| `MonchiVisualBank.bodies` | **4 prefabs** |
| `MonchiVisualBank.gemMaterials` | 5 (shiny) |

**El hallazgo del frente.** `MonchiVisualizer.Assemble` (`World/Creatures/MonchiVisualizer.cs:58`) hace `bank.GetBody(dna.BodyShapeID)` e instancia **ese único prefab**. No hay ensamblaje por sockets. `MonchiVisualBankSO.GetBody` (`Data/Databases/MonchiVisualBankSO.cs:35`) hashea el ID contra una lista de 4 cuerpos. `ArmID`/`EyeID`/`MouthID` **no se leen en ninguna parte del pipeline visual**.

Lo que el jugador ve de la genética: **4 siluetas × color de tinte × 12 patrones de pelaje × cara de humor**, más el material de gema si es shiny. Lo que el jugador NO ve: 125 combinaciones de brazo/ojo/boca que sí existen en el DNA, aportan stats (`Data/Parts/BodyPart.cs:33-37`), tienen rareza, tier, set y nombre generado — y son invisibles.

**Qué falta.** No es "implementar el ensamblaje": es que **no hay arte de partes**. `PartVisualBankSO` ya no existe en el código (0 ocurrencias en `Scripts/`) — el mapa parte→prefab fue retirado cuando se pasó al modelo de cuerpo entero comprado. El sistema de partes quedó huérfano de su capa visual.

**Entradas/salidas.** Recibe de: crianza (herencia) y combate (evolución de tier). Alimenta a: combate (stats/rol/elemento), economía (tiers/stats), presentación (retrato). **No alimenta al mundo vivo** salvo por los diales.

**Riesgo si se ignora.** Es el gancho de marketing #1 según los datos de S71 (Wobbledogs, Spore, Cassette Beasts). Un pet-sim genético cuyo output visual son 4 cuerpos no tiene material compartible, que es exactamente el mecanismo por el que esos juegos crecieron.

---

## 2 · Crianza / ciclo de vida 🟡

**Qué existe.** `BreedingService.BreedCreatures` (`Systems/Breeding/BreedingService.cs`) hereda partes (50/30/20 vía `InheritanceOddsTableSO`), color y pelaje (`:67-68`), stats (`:77-79`) y los dos diales (`:80-81`). Rol y elemento se heredan como metadata fuera del genetic string. Corrales con cortejo (`BreedingContainer`), flujo async con timer autoritativo del servidor (`CloudCode/start-breeding.js:12`: `BREED_DURATION_MS = 30 min`), y coste de energía a ambos padres (`AsyncBreedingService.cs:80-81`).

**Qué falta — el ciclo de vida es decorativo.** `CreatureLifeStageTableSO` mapea edad→etapa, y esa etapa se usa en exactamente dos lugares: el texto del NameTag (`World/Creatures/NameTag.cs:311-313`) y el gate de adultez para criar (`World/Containers/BreedingContainer.cs:337-338`). No hay envejecimiento con consecuencias, no hay declive, no hay muerte natural.

**La muerte permanente es solo de combate.** `IsDead = true` aparece en dos sitios en todo el código: `Systems/Combat/CombatService.cs:336` (roll de `DeathChance` = 0,05 al perder) y `Systems/Combat/AsyncCombatService.cs:348` (aplicar resultado remoto). No hay ninguna otra causa de muerte en el juego.

**Entradas/salidas.** Recibe de: genética (padres), mundo vivo (energía, y el gate de adultez). Alimenta a: genética (hijos), economía (`BreedCount` sube el precio). **No recibe nada del cuidado ni de la afinidad social** — dos criaturas que se odian crían igual que dos que duermen juntas.

**Riesgo si se ignora.** El pilar "muerte permanente" no está sostenido por el resto del juego: hoy la única forma de perder una criatura es un dado del 5% en un combate que casi no se juega.

---

## 3 · Combate 🔴 — el cuello de botella

**Qué existe (y es mucho).** Sim determinista por semilla 3v3 con grilla 2-3-2 y targeting por fila (`Systems/Combat/CombatService.cs:95-240`), descompuesto en colaboradores según la regla 11. Capa elemental completa: `ElementTable` tiene **4 identidades, 12 estados y 12 reacciones** cargadas. `RoleTable` tiene los 3 perfiles. Visualizador 3v3 aprobado visualmente (S45-S47). Consecuencias implementadas: evolución de tier al ganador (`:316-322`), roll de muerte al perdedor (`:334-336`).

**Qué falta — y es lo que importa.**

**(a) No hay fuente de oponentes para 3v3.** `CombatLineupUITK` (`UI/CombatLineupUITK.cs:9-16`) es, según su propio encabezado, un *"visual prototype"*: un carrusel de tus MoriMonchis elegibles y **dos tableros enfrentados que llenás vos**. "¡Pelear!" llama a `CombatController.SimulateLocal` con los ids/filas de ambos tableros. Es decir: **hoy el 3v3 solo se puede jugar contra vos mismo**.

**(b) El async sigue siendo 1v1.** El payload de `AsyncCombatService.EnqueueInternal` (`:107-113`) manda `creatureId` + `creatureJson` — una criatura, no un equipo. La Fase 5 del roadmap de [[Index/13 - Combat Design Direction]] no arrancó.

**(c) Consecuencia peligrosa del (a):** el sim local aplica muerte permanente real sobre el DNA (`CombatService.cs:336`). Pelear tu equipo contra tu propio equipo puede matarte una criatura de verdad, con 5% por combate.

**(d) El tope de peleas no se aplica.** `MaxFightCount = 5` solo filtra en la consola dev (`CombatDevConsole.cs:66` y `:162`). `SimulateLocal` (`CombatController.cs:37-46`) no lo mira.

**(e) Balance sin data.** Todos los knobs de `CombatManager.asset` están en el default v1: `DeathChance 0,05` · `CritChance 0,2` · `CritMultiplier 3` · `MaxRounds 10` · `SuddenDeathStartRound 6` · `EnergyCostToQueue 15`. Nunca se tunearon porque nunca se jugaron suficientes peleas — que es exactamente el argumento del Gauntlet de S71.

**Esto reencuadra la Recomendación B de S71.** "Mostrar la composición rival antes de confirmar el lineup" no es un cambio de UI barato sobre algo existente: **primero hay que crear el rival**. Y eso es, palabra por palabra, el Gauntlet de prueba de la Parte 3 de S71 — composiciones de la casa contra las que jugar. Las dos propuestas son la misma sesión.

**Entradas/salidas.** Recibe de: genética (stats/rol/elemento/tier), equipo (9 items en `EquipmentDatabase`). Alimenta a: genética (evolución de tier), crianza (mata al padre), economía (winrate). **No recibe nada del cuidado.**

---

## 4 · Economía / tienda / clientes 🟢

**Qué existe — el único loop cerrado del juego.** `NpcAgent` corre una FSM completa: deambula → inspecta un `StoreContainer` → elige → va a la caja → hace fila → espera → el jugador acepta o contraoferta → paga y se va. El pago acredita Dabloons (`World/Npc/NpcAgent.cs:275`) y dispara `CustomerSold`/`RegistryChanged`/`InventoryChanged`. Hay pujas cruzadas: si otro cliente compra tu objetivo, este se va con `LeaveReason.Outbid` (`:372-377`). La contraoferta funciona (`:283-298` sobre `NegotiationFlow`). Los Dabloons se gastan en muebles e ítems (`Systems/Store/StoreManager.cs:46` y `:77`).

**Qué falta — la valuación es ciega a casi todo.** `ValuationHandler.Estimate` (`Systems/Customers/ValuationHandler.cs:14-34`) suma: precio base por tier de las 4 partes + los 6 stats + `BreedCount` + winrate + suma de tiers, todo ponderado por el arquetipo y su presupuesto.

**Lo que NO entra en el precio:** cuidado (Health/Energy/Affect), rol, elemento, rareza de las partes, `IsShiny`, `FurType`, edad/etapa, linaje, nombre. El campo `PriceModifier` del rol existe y está poblado (`Data/Combat/RoleTableSO.cs:15`, `:45`, `:55`, `:65` con 0 / −0,10 / +0,10) y **no lo lee absolutamente nadie** — el diseño de [[Index/13 - Combat Design Direction]] §"Impacto económico" nunca bajó.

**Los 3 arquetipos son el mismo comprador.** `CustomerArchetypeSO` (`Data/Customers/CustomerArchetypeSO.cs:16-19`) solo expone pesos sobre los mismos cuatro ejes, presupuesto y tolerancia. No hay preferencia por rol ni por elemento, y por lo tanto no existe la "tabla invertida por arquetipo" que el diseño prevé. Peor: `BestPickFromContainer` (`NpcAgent.cs:345-361`) elige **la criatura más cara**, así que todos los clientes convergen al mismo objetivo.

**El catálogo está casi vacío.** `ShopCatalog` tiene **1 listing de mueble y 2 de ítem**, contra 9 definiciones de mueble y 2 de ítem existentes. Y el registro de muebles colocados está vacío (`furniture_registry_*.json` = 2 bytes): **ahora mismo no hay ninguna estación, corral ni vitrina colocada** — el loop de clientes no puede correr sin un `StoreContainer` puesto.

**Entradas/salidas.** Recibe de: genética (tiers/stats), crianza (`BreedCount`), combate (winrate). Alimenta a: el jugador (Dabloons) → muebles/ítems → mundo vivo (estaciones). **Es el único frente que cierra un circuito**, y encima es el que junta la salida de los otros tres.

---

## 5 · Mundo vivo / cuidado 🔴 — sumidero confirmado

**Qué existe.** Es, por volumen, el frente más trabajado de las últimas 8 sesiones: necesidades con decay, estaciones con reserva de slot, `AgentBrain`/`AgentSenses`/`AgentSocial`/`AgentPhysics`/`AgentContext` (descomposición S55), reacciones sociales polimórficas, grafo social con historia persistida, diales genéticos alimentando umbrales, sesión de caricias hold-E, comer de la mano, ragdoll con red anti-void, emotes, moods, NameTags vivos.

**El hallazgo del frente — la hipótesis del sumidero se confirma con tres pruebas.**

**Prueba 1 — nadie lee `Affect`.** Búsqueda de `Affect` en todo `Scripts/`: 11 archivos, **todos** en `World/AI`, `World/Needs`, `World/Containers`, `Data/Social`, `Data/NeedsState.cs` y `Core/Enums.cs`. Cero ocurrencias en `Systems/Combat`, `Systems/Breeding`, `Systems/Store`, `Systems/Customers` y `UI/`.

**Prueba 2 — nadie lee `Condition`.** El estado `Sick`/`InNeed`/`Healthy` (`World/AI/MoriMochiAgent.cs:367-375`) se consume en exactamente dos lugares: los gates de las reglas sociales (`Data/Social/ReactionRuleBase.cs`, `World/AI/AgentSocial.cs`) y la cara del humor (`World/Creatures/MonchiMoodDriver.cs:33-34`). Un MoriMochi enfermo no vale menos, no pelea peor y no cría peor. Solo deja de jugar y pone cara triste.

**Prueba 3 — el cuidado ni siquiera se VE.** Búsqueda de `Health|Energy|Affect` en `Scripts/UI/`: **cero archivos**. Ninguna de las 17 pantallas muestra las necesidades. La única lectura numérica vive en el inspector Odin del agente, que es dev-only. El jugador infiere el estado por la carita y el NameTag.

**El grafo social y los diales, igual.** `SocialGraphService` lo consumen `AgentSenses` (mundo) y `DetailRelationsPresenter` (una pestaña de lectura). `Sociability`/`Boldness` los hereda `BreedingService` y los consumen las reglas de `Data/Social/`. Nada más. Y el archivo `social_graph_*.json` pesa 2 bytes: la historia acumulada está vacía.

**El `Energy` es el caso más raro.** Es la única necesidad con un canal hacia afuera: se descuenta al criar (`AsyncBreedingService.cs:80-81`) y al encolar combate (`AsyncCombatService.cs:87`). Pero **no hay ni un solo chequeo que exija un mínimo** — `CombatController.EnqueueForAsyncCombat` (`:55`) solo valida muerto/ocupado. Es un número que baja y no impide nada.

**Riesgo si se ignora.** Es el frente donde se fue el grueso del trabajo reciente y el único que hoy no le devuelve nada al jugador. Es también, según S71 §2.4, la capa que define si el juego se lee como cozy o como estrategia — o sea que su desconexión no es solo mecánica, es de posicionamiento.

---

## 6 · Presentación 🟡

**Qué existe.** 17 UXML, 36 stylesheets, `Theme.uss` con los tokens `--mm-*` y todos los paneles tokenizados (S66). Localización fase 1 completa: `Loc.Tr` + `LocEnumMaps` + tabla `Strings` en/es con 364 entradas, selector de idioma. Visualizador de combate con cámaras, barra de orden y burbujas. Retratos fotomatón. NameTags world-space con burbuja de emote integrada.

**Qué falta.** El kit de arte "El Diario del Pet Shop" **no tiene ni una pieza producida**: el proyecto entero contiene **4 sprites**, tres de los cuales son el fondo y los marcos del panel de equipo. Toda la identidad visual de la UI son colores y bordes en USS. Y como se documentó en S66, UITK runtime no soporta box-shadow ni gradiente: sin sprites 9-slice no hay profundidad posible. La dirección está decidida y aprobada desde hace 6 sesiones, y sigue en cero.

Pendiente aparte: localización fase 2 (los textos que viven en assets SO — `ElementTableSO` es el más pesado).

**Riesgo si se ignora.** Es el frente que convierte a los otros seis en algo mostrable. Ninguna captura del juego hoy comunica "Diario del Pet Shop".

---

## 7 · Infraestructura / meta 🟢

**Qué existe.** Bus de eventos con **17 eventos estáticos** (`Core/GameEvents.cs`), namespacing único, deuda de `partial class` cerrada por completo (Fases 6-9 de [[Index/11 - Technical Debt]]). Persistencia local + Cloud Save con reconciliación. 12 scripts de Cloud Code. Workflow MCP maduro (verificación en Play como parte del protocolo).

**Corrección importante:** el servidor **ya no simula combate** — `CloudCode/run-combat.js:55` solo empareja y entrega los dos snapshots; los clientes simulan localmente con el sim determinista. El bug "DeathChance hardcoded en JS (15%)" de [[Index/08 - Known Bugs & Checkpoints]] está obsoleto.

**El riesgo medido.** El registro local de **18 criaturas pesa 1,48 MB** (`creature_database_<playerId>.json`), y ese blob se empuja entero a Cloud Save. El peso no viene de las criaturas sino del `CombatHistory`: solo **6 combates guardados** generan ~240 bloques de proc y ~1440 entradas de marcas. El comentario en `CreatureDNA.cs:72-74` declara la lista explícitamente **ilimitada**. Con el tope de diseño (5 peleas × 18 criaturas) el orden de magnitud es preocupante, y el Gauntlet de S71 —que existe justamente para jugar decenas de peleas seguidas— lo empeoraría rápido.

---

## Matriz de conexiones

Filas = quién produce. Columnas = quién consume. `●` = acople real verificado.

| ↓ produce \ consume → | Criatura | Crianza | Combate | Economía | Mundo vivo | Presentación |
|---|---|---|---|---|---|---|
| **Criatura** | — | ● partes/stats | ● stats/rol/elemento | ● tiers/stats | ● diales | ● retrato |
| **Crianza** | ● hijos | — | | ● BreedCount | | ● árboles |
| **Combate** | ● evolución de tier | ● mata al padre | — | ● winrate | | ● visualizador |
| **Economía** | | | | — | ● muebles→estaciones | ● paneles |
| **Mundo vivo** | | | | | — | ● NameTag / humor |
| **Presentación** | | | | | | — |

**La fila 5 está vacía salvo su propia representación.** El mundo vivo no produce nada que consuma ningún otro frente. Es el sumidero.

**La columna "Mundo vivo" también está casi vacía**: solo la economía le entrega muebles. Nada de lo que pasa en combate o en la crianza cambia la vida del bicho en la tienda.

---

## Sistemas huérfanos

Construidos, verificados en Play, y sin ningún consumidor fuera de sí mismos.

| Sistema | Sesión | Evidencia de orfandad |
|---|---|---|
| **Grafo social** (historia de afinidad por par) | S65 | Solo lo leen `AgentSenses` y la pestaña Relaciones. El JSON está en 2 bytes. |
| **Diales Sociabilidad/Osadía** | S69 | Solo mueven umbrales de reglas de mundo. No tocan combate ni precio. |
| **Sesión de caricias (hold-E)** | S69 | Su único efecto es `Affect`, que no lee nadie. |
| **Comer de la mano** | S69-S70 | Sube Health y Affect. Ninguno de los dos tiene consecuencia. |
| **`Affect` completo** | pre-S64 | Cero lectores fuera de `World/`+`Data/Social`. Cero UI. |
| **`CreatureCondition`** | — | Solo gates sociales y cara de humor. |
| **`RoleTableSO.PriceModifier`** | S37/S40 | Campo poblado, **cero lectores** en todo el código. |
| **Rareza y sets de partes** (`Rarity`, `PartSet`) | — | Existen en `BodyPart`, no entran a la valuación ni al visual. |
| **`LifeStage`** | — | Texto del NameTag + gate de adultez. Nada más. |
| **Partes brazo/ojo/boca como VISUAL** | — | 125 combinaciones sin ninguna expresión en pantalla. |

---

## Ranking de cuellos de botella

Ordenado por *(a cuántos frentes desbloquea)* × *(qué tan barato es)*.

**1 · No hay oponente para el 3v3.** Bloquea: combate, progresión, economía (el winrate no se mueve), y la pregunta de diseño que tiene el proyecto frenado desde hace dos semanas. Barato en relación a lo que desbloquea — el motor, la grilla y el visualizador ya están. **Es literalmente el Gauntlet de S71**, y ahora sabemos que la Recomendación B (mostrar al rival) no es un extra sino parte del mismo trabajo.

**2 · El cuidado no se ve ni paga.** Bloquea: que ocho sesiones de mundo vivo valgan algo, y la decisión cozy-vs-estrategia del pitch. Es el arreglo **más barato de la lista**: la valuación es un método de 20 líneas en un archivo (`ValuationHandler.cs`), y la UI de necesidades es un bloque en un panel que ya existe. Dos acoples chicos convierten un sumidero en el diferenciador del juego — "un MoriMochi cuidado vale más" es, además, exactamente lo que ningún competidor del género tiene.

**3 · El fenotipo son 4 cuerpos.** Bloquea: el gancho de marketing #1 según los datos de S71. Es el más caro (es arte, no código) y el que más se beneficia de una decisión de alcance: no hace falta volver al ensamblaje por partes — variación de escala, proporción y accesorios sobre los 4 cuerpos ya multiplicaría la silueta. Pero **decidir esto es urgente aunque ejecutarlo no lo sea**, porque define si el sistema de partes sigue vivo o se archiva.

**4 · El catálogo y la tienda están vacíos.** 1 listing de mueble, sin muebles colocados. Barato de llenar, y sin esto el loop de clientes —el único cerrado— no corre.

**5 · El peso del save.** No urge hoy (1,48 MB) pero escala mal y el Gauntlet lo acelera. Acotar `CombatHistory` es un cambio pequeño y preventivo.

---

## Próximas 3 sesiones sugeridas

Encajan con las decisiones abiertas de S71, no compiten con ellas.

- **S73 — Gauntlet de prueba.** El experimento de S71 Parte 3, ahora con el alcance real que este diagnóstico revela: crear composiciones rivales de la casa, mostrarlas antes de confirmar, permitir reposicionar entre peleas, y **desactivar la muerte permanente dentro de la tirada** (hoy el sim la aplica siempre). Responde la pregunta que bloquea a Juan.
- **S74 — Que el cuidado pague.** Dos acoples: (a) el cuidado entra en la valuación, (b) las necesidades se ven en la UI. Opcionalmente (c) un gate de energía real para criar/pelear, que es lo que convierte el cuidado en administración de recursos — la escasez de recursos que S71 §1.4 propone en reemplazo de los timers de reloj.
- **S75 — Decisión de fenotipo.** Sesión de diseño, no de arte: decidir si el sistema de partes se resucita visualmente, se reemplaza por variación sobre los 4 cuerpos, o se archiva y las partes pasan a ser puro stat. De esa decisión depende el pitch entero.

---

## Deuda de documentación

Divergencias vault↔código encontradas en el camino. No se corrigieron acá.

1. **[[Index/02 - Genetics & Breeding]]** lista `PersonalityProfileSO` (eliminado en S39), `MoriMonchiVisualizer` y `BodyPartJoint` (el ensamblaje por sockets ya no existe) y `PartVisualBankSO` (**cero ocurrencias en el código**). La nota describe una arquitectura visual retirada.
2. **[[Index/06 - Player & World]]** referencia `MoriMochiAgent.cs` como "cerebro" monolítico y `MoriMonchiVisualizer`; post-S55 el agente son 4 colaboradores + `AgentContext`, y el visualizador es `MonchiVisualizer`.
3. **[[Index/13 - Combat Design Direction]]** lista la Fase 7 (economía) como pendiente: `CustomerService`/`ValuationHandler`/`NegotiationFlow` ya existen. Pero su parte de `PriceModifier` y arquetipos invertidos sigue **realmente** pendiente — la nota está mal en un sentido y bien en el otro.
4. **[[Index/08 - Known Bugs & Checkpoints]]**: "DeathChance hardcoded en JS (15%)" está obsoleto — el servidor ya no simula (`run-combat.js:55`).
5. **El dominio de economía y clientes no tiene nota Index.** Los ScriptNodes de clientes apuntan a `[[Index/04 - Customer System]]` y `00 - Index.md` a `[[Index/08 - NPC Customers]]`; ninguna de las dos existe. Siendo el frente más sano del juego, es el único sin diseño documentado.
6. **`CLAUDE.md`** lista 8 eventos de `GameEvents`; hay **17** (faltan los de NavMesh, inventario y los 5 de clientes).
7. **`00 - Index.md`** rutea a `[[MoriMonchiVisualizer]]`, `[[BodyPartJoint]]`, `[[PartVisualBankSO]]` y `[[PersonalityProfileSO]]`, todos inexistentes.
8. **Quirk MCP nuevo**: `execute_code` corre como **cuerpo de método** — las directivas `using` en la cabecera son error de compilación. Hay que calificar los tipos (`UnityEditor.AssetDatabase`, `System.Text.StringBuilder`) y desambiguar `UnityEngine.Object` de `object`. Complementa la nota S68 sobre Roslyn en [[Index/12 - Unity MCP]].

---

## Decisiones que este diagnóstico habilita

- [ ] **¿El Gauntlet se construye ya?** Ahora se sabe que incluye crear al oponente, no solo encadenar peleas.
- [ ] **¿El cuidado entra en el precio?** Es el acople más barato del proyecto y el que define el diferenciador frente a Wobbledogs/Niche.
- [ ] **¿Las necesidades se muestran en la UI?** Hoy son invisibles para el jugador.
- [ ] **¿El sistema de partes se resucita, se reemplaza o se archiva?** Define el pitch.
- [ ] **¿Se acota `CombatHistory`?** Preventivo, antes de jugar decenas de peleas.
- [ ] **¿Se pone un gate real de energía?** Convierte el cuidado en recurso administrable — la alternativa de S71 §1.4 a los timers de reloj.

Relacionado: [[Index/15 - Theorycrafting S71 - Autobattler y Marketing]], [[Index/13 - Combat Design Direction]], [[Index/11 - Technical Debt]], [[Index/09 - Active Context]].
