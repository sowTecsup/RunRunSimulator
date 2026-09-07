---
name: arquitectura-unity
description: Regla de oro y reglas de codigo no negociables para proyectos Unity C# de Juan (capas, bus de eventos, tamano de archivo, composicion sobre partial, cascada de acceso a ScriptableObjects, singletons, Odin, desuscripcion). Cargar antes de disenar cualquier sistema nuevo, antes de decidir donde vive un dato o una responsabilidad, y antes de escribir o revisar C#. Tambien al evaluar si un archivo hay que partirlo y como.
---

# Arquitectura Unity

Reglas destiladas de ~105 sesiones y dos auditorias completas de deuda tecnica. No son
preferencias de estilo: cada una salio de un dolor medido, y varias se corrigieron a si
mismas cuando la primera version resulto equivocada.

---

## Regla de oro tecnica

> **Una responsabilidad por archivo, una direccion de comunicacion, un dueno por dato.**

Cuando una decision NO este cubierta por las reglas concretas de abajo, se aplica esta.
Las reglas concretas son casos particulares de estos cuatro principios:

1. **Capas sin saltos de dos niveles**
   `Data` (estado puro) -> `Systems`/`Core` (orquestacion; dueno de persistencia y red)
   -> `World`/`UI` (representacion).
   La representacion **lee** estado y **reacciona** a eventos. Nunca persiste, nunca
   toca la nube, nunca decide reglas de dominio.

2. **Comunicacion cruzada solo por bus o servicio explicito**
   Un bus de eventos estatico para gameplay, otro para UI, otro para inputs.
   **El evento transporta la data.** Un suscriptor NUNCA hace `Find*`,
   `GetComponentInParent` ni `Manager.Instance.Registry` para ir a buscar lo que el
   evento deberia haberle traido.

3. **Limite de tamano/dominio**
   Si un archivo supera ~400 lineas **o** mezcla 2+ dominios (datos, presentacion,
   fisica, red), se parte en **clases/componentes independientes**, una responsabilidad
   cada uno.

4. **Singleton = servicio runtime; ScriptableObject = data**
   Un servicio de runtime puede ser singleton. Un SO **no** expone instancia estatica
   propia: se llega a el por la cascada de responsabilidad (ver abajo).

---

## Reglas de codigo (no negociables)

1. **Desacoplamiento estricto via eventos.** Comunicacion cross-system solo por el bus.
   El evento lleva el payload; el suscriptor no va a buscar el singleton.

2. **Persistencia solo por evento.** Ningun script de gameplay llama a guardar en disco
   ni a subir a la nube. Solo emite el evento de "cambio". Hay **un** dueno de la
   persistencia y es el manager de nivel Core.

3. **Sin comentarios en codigo.** No agregar `//` ni `/* */` sin pedido expreso.
   La documentacion vive en el vault. **Antes de purgar comentarios viejos, rescatar los
   invariantes que estuvieran solo ahi** a una nota del vault: la regla es "sin
   comentarios", no "sin conocimiento".

4. **Sin features adelantadas.** No implementar mecanicas antes de su etapa del roadmap,
   aunque "ya que estamos" parezca barato. Sintoma tipico detectado en auditoria: metodos
   publicos sin ningun llamador, escritos para una feature que todavia no existe.

5. **Sin complejidad innecesaria.** No agregar campos, abstracciones ni features que
   nadie pidio. **Tres lineas similares son mejores que una abstraccion prematura.**
   Corolario: **una interfaz con un solo implementador es un smell**, y una interfaz cuyos
   metodos quedarian vacios en un consumidor no se implementa (serian metodos muertos).

6. **Desuscribir siempre.** `OnEnable` suscribe, `OnDisable` desuscribe. Un `static event`
   mantiene vivo al suscriptor: leak + excepcion al disparar sobre un objeto destruido.

7. **Evitar referencias redundantes.** Centralizar la comunicacion en eventos o en el
   servicio dueno. Nunca resolver una dependencia recorriendo la jerarquia.

8. **Odin siempre** (si el proyecto lo tiene): `SerializedScriptableObject` con
   `[OdinSerialize]` para diccionarios y listas polimorficas.

9. **Un evento que nadie suscribe no se agrega "por si acaso".** El bus se poda: los
   eventos huerfanos se borran cuando se detecta que nadie escucha. Y un handler que
   descarta el payload y llama a `Refresh()` esta desperdiciando el evento.

10. **Contratos de red/serializacion son sagrados.** Si el proyecto tiene un string ID, un
    formato de save o un DTO que cruza a la nube, su forma es contrato: cambiarlo es una
    decision de diseno, no un refactor. Los separadores del formato no pueden aparecer
    dentro de los campos.

11. **Composicion sobre `partial`.** Ver la seccion propia — es la que mas se
    malinterpreta, y la que ya se equivoco una vez.

12. **Todo singleton limpia su `Instance` en `OnDestroy`.** Sin eso queda un bug latente
    al cambiar de escena: la referencia estatica apunta a un objeto destruido.

---

## Regla 11 en detalle: composicion sobre `partial`

**El problema:** una `partial class` sigue siendo UNA clase con UN estado mutable.
Repartirla en archivos esconde lineas sin reducir acoplamiento. No es un remedio al
tamano; es maquillaje.

**El remedio real:** partir en **mini-managers con estado propio y UNA responsabilidad**,
coordinados por un **nucleo delgado**, sobre un **blackboard plano** que es el dueno unico
del estado compartido.

### Historia importante (por que esta regla es firme)

La primera auditoria concluyo que un FSM de MonoBehaviour con estado mutable irreducible
era una **excepcion legitima**: separarlo obligaria a hacer publicos ~30 campos y
"relocalizaria el acoplamiento en vez de reducirlo". Se documento con evidencia y se dejo
como deuda aceptada.

**Juan lo revirtio.** La direccion quedo fijada: composicion, sin excepciones de estado.
La deuda se pago entera y el proyecto quedo con **cero `partial class`**. La conclusion
que sobrevive: *si te parece que no hay costura limpia, la costura es un blackboard, no
una `partial`.*

### Patron canonico (el que funciono con el archivo mas grande del proyecto)

```
AgenteMonstruoso.cs (1189 lineas: FSM + fisica + navegacion + necesidades + confinamiento)
        |
        v
AgentContext            <- clase PLANA, dueno unico del estado compartido:
                           estado, componentes cacheados, masks, referencias vivas,
                           + helpers compartidos sin logica de dominio

AgentBrain              <- mini-manager: conducta, needs, reacciones. Timers PROPIOS.
AgentPhysics            <- mini-manager: ragdoll, knock, throw, handoff. Estado PROPIO.
AgentConfinement        <- mini-manager: corral, cortejo, supervivencia a rebake.

MoriMochiAgent          <- nucleo MonoBehaviour, YA NO partial:
                           todos los [SerializeField], lifecycle, dispatch del Update
                           en el orden exacto, fachada publica INTACTA,
                           + SWITCHBOARD interno
```

**El switchboard es la pieza clave:** el nucleo expone `RequestRoam`,
`RequestReleaseStation`, `RequestEnterRagdoll`, `RequestDetachToPhysics`, etc.
**Ningun mini-manager llama a otro directamente.** Eso es lo que hace que la
descomposicion reduzca acoplamiento de verdad en vez de moverlo de lugar.

**Regla practica de migracion:** conservar los MISMOS nombres de los `[SerializeField]`
al absorberlos en el nucleo (`private` -> `internal`) para que el prefab preserve sus
valores. Y mantener la fachada publica intacta, para que la escena y los otros sistemas no
se enteren.

### Cuidado: sacar el estado no es sacar la configuracion

En la descomposicion del agente se extrajo el estado, pero los ~385 lineas de
`[SerializeField]` de tuning volvieron al nucleo y lo re-inflaron a 705 lineas. **La
configuracion tambien es un dominio**: su lugar natural es un SO de tuning, no el
componente. Planearlo desde el principio.

### Uso legitimo de `partial`

Solo **ventaja fisica de archivo**: evitar conflictos de Git, aislar codigo autogenerado.
Nunca como remedio al tamano.

### Siempre afuera del nucleo

- Codigo puro (matematica, helpers sin estado) -> **clase estatica aparte**.
- Tooling dev que usa solo API publica -> **componente aparte** con referencia serializada
  al dominio (patron "dev console"). Los `[Button]` de debug NO viven en clases de dominio.
- Gizmos: **se quedan** en el componente (son visualizacion propia; convencion de Unity).

---

## Como partir un panel de UI (patron establecido)

- **Tabs con foco jerarquico** -> una interfaz `ITabPresenter`
  (`Enter/Navigate/Submit/Cancel/ClearFocus/Rebuild/Teardown`), un presenter por pestana
  con sus propias refs. El nucleo coordina solo `TabBar <-> Content`; los sub-estados de
  foco viven en el presenter, que devuelve `false` para devolver el control a la tab bar.
- **Panel de solo contenido, sin navegacion interna** -> colaboradores **planos** con un
  `Rebuild(datos)`, **sin** la interfaz. Implementarla seria llenar de metodos muertos
  (regla 5).
- Los presenters hacen sus consultas sobre el root y reciben los datos por `Func<>`:
  **no cachean** el registro.

---

## Acceso a ScriptableObjects: cascada de responsabilidad

**Decision tomada y aplicada:** se eliminan los `static Current` de los SO.

- Cada dominio tiene un **apex** (su controller) que es **dueno de las referencias
  serializadas** de ese dominio.
- Los hijos piden la referencia al apex via el singleton del **servicio runtime**
  (`BreedingController.Instance.InheritanceOdds`).
- Los apexes cuelgan del manager raiz.

**Por que:** un `static Current` en el SO parece comodo pero pierde la trazabilidad de
quien es el dueno del dato — y "un dueno por dato" es medio principio de la regla de oro.

---

## Chequeo antes de agregar un campo o un componente

1. ¿Este objeto ya tiene la informacion, o estoy duplicando algo que existe en otro lado?
2. ¿Este componente necesita realmente saber de los otros, o puede ser completamente
   ignorante de ellos?
3. Si la respuesta a (2) es "no necesita saber", **no le des referencias cruzadas.**

Ejemplo real: el primer diseno puso el mapa de sockets dentro del prefab del cuerpo.
Estaba mal — el cuerpo no sabe que otras partes existen. El ensamblador visual ya tenia
todas las posiciones de anchors: **el ensamblador ERA el mapa de sockets.** Los prefabs
de partes solo necesitan saber su propio punto de insercion.

> Antes de disenar un sistema, preguntar: **¿quien ya tiene la informacion que necesito?**

---

## Estructura de carpetas de referencia

```
Assets/<Proyecto>/Scripts/
├── Core/          # Manager raiz, bus de eventos, save system, enums, interfaces
├── Data/          # Estado puro: SOs, structs de datos, databases. Sin orquestacion.
├── Systems/       # Orquestacion por dominio, desacoplada via bus
├── World/         # Representacion 3D: AI, spawn, contenedores, props
├── UI/            # Paneles y HUD
├── Player/        # Controller e inputs
├── Shaders/
└── Editor/        # Tooling dev, dev consoles y tools propias del editor
```

Logica pura de reglas (un resolver de combate, un simulador) va en su propia carpeta
**sin ninguna dependencia de `UnityEngine`**: se testea en EditMode y se simula fuera del
editor. Un namespace raiz unico para todo el proyecto, block-scoped.

---

## La deuda se re-endeuda

Las dos reglas que volvieron a romperse despues de haberse pagado por completo:

1. **El limite de ~400 lineas** — nueve archivos lo violaban de nuevo un mes despues.
2. **El tooling dev fuera del dominio** — los `[Button]` de debug reaparecieron dentro de
   clases de dominio.

Por eso existe la skill `auditoria-y-limpieza`: la disciplina no se sostiene sola, se
audita cada tanto con numeros.
