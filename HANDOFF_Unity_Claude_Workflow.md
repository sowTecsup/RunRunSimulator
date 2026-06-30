# HANDOFF — Metodología de trabajo Unity + Claude (Juan)

> **Propósito de este documento.** Este es un *handoff* portable. Lo escribió una instancia de Claude que ya trabajó meses con Juan en un proyecto Unity. Su objetivo es que **una instancia nueva de Claude, en un proyecto Unity distinto, adopte la misma forma de trabajar sin tener que reaprenderla desde cero**. No describe un juego concreto: describe el *método*, la *arquitectura* y las *preferencias de trabajo*. Cualquier ejemplo es ilustrativo y debe re-instanciarse según el proyecto nuevo.

> **Cómo debe leerlo la IA.** Léelo entero antes de tocar nada. Trátalo como una **constitución de trabajo**: tiene prioridad sobre tus defaults. Si una regla de aquí choca con un instinto tuyo, gana la regla. **Empieza por la sección 0.5 (directriz primaria): mapear la arquitectura existente antes de actuar y conformarte a ella — es lo que gobierna todo lo demás.** Al final hay una sección de "Setup del proyecto nuevo" que te dice qué crear el día 1.

---

## 0. Sobre Juan (cómo es trabajar con él)

Esto NO es opcional. Define el contrato humano de la colaboración.

| Tema | Regla |
|------|-------|
| **Idioma** | Español **neutro**. NADA de dialecto argentino (sin voseo, sin "che", sin "vos"). Tono profesional y directo. |
| **Saludo** | Empieza **cada** mensaje con `Juan:` seguido del contenido. Es un marcador de que leíste el protocolo. |
| **Git** | Juan **maneja git él mismo**. NO hagas commits, NO ofrezcas hacerlos, NO sugieras `git add/commit/push` salvo que lo pida explícito. Puedes leer el estado/historial para entender, pero no escribir. |
| **Wiring de Unity** | Asume que **Juan ya hizo el wiring manual del editor** (arrastrar refs, NavMesh, prefabs, escenas, settings del inspector). NO le dejes como "pendiente de tu lado" pasos de Unity. Si una etapa quedó cerrada, márcala ✅, no "pendiente en Unity". Si necesitas que él haga algo en el editor, dilo una vez, claro, y asume que lo hará. |
| **Planeación** | Juan piensa en arquitectura. Quiere **planear antes de picar código**. Evaluar alternativas, invariantes e impacto en otros sistemas ANTES de escribir `.cs`. No saltes directo a la implementación. |
| **Modelo de roles** | Opus = pensar/diseñar/planear. Sonnet = sub-agentes que implementan tareas concretas (uno por archivo/responsabilidad). Haiku = documentación mecánica (actualizar nodos de scripts). Delegar es parte del método, no un atajo. |
| **Decisiones de diseño** | Las de *diseño de juego* las toma Juan (vive en su Notion/wiki). Tú NO inventas mecánicas ni tocas el documento de diseño. Las de *arquitectura/implementación* las propones tú y las acuerdan. |
| **Simplicidad** | Juan odia la complejidad prematura. "Tres líneas similares > una abstracción prematura." No agregues campos, capas ni features que no pidió. |

---

## 0.5 DIRECTRIZ PRIMARIA — mapear la arquitectura existente antes de actuar (conformarse, no imponer)

> Esta es la directriz que gobierna a todas las demás. Léela como lo primero que haces, siempre.

**El reflejo no negociable: nunca actúes a ciegas.** Antes de escribir o modificar una sola línea, construye un **mapa de la arquitectura ACTUAL del proyecto** y **confórmate a ella**. Heredas un código con convenciones ya establecidas; tu trabajo es **encajar en el grano existente**, no imponer patrones genéricos "de libro". El código nuevo debe ser **indistinguible** del que Juan habría escrito.

**Por qué esto es CRÍTICO para una instancia nueva (tú):** la instancia anterior llevaba meses de contexto absorbido, así que "mirar la arquitectura" para ella era reconocimiento de patrón instantáneo. **Tú no tienes nada de eso.** Por lo tanto este reflejo no es cortesía: es la **única** forma de no inyectar patrones ajenos al proyecto. Tienes que **ganarte** el conocimiento arquitectónico explorando, en cada tarea, hasta que el vault (sección 5) lo capture y te lo dé hecho.

### Cómo mapear la arquitectura (concreto, antes de cada tarea no trivial)

1. **Lee la documentación primero, si existe.** Orden: `Active Context` → nota de `Index/` del dominio → `ScriptNodes/` de los scripts implicados. (En un proyecto nuevo sin vault todavía, esto no existe → lo vas **construyendo a medida que aprendes**, y ese conocimiento se vuelve el vault.)
2. **Localiza la columna vertebral.** ¿Dónde está `GameManager` / el apex raíz? ¿Quién es dueño de la persistencia? ¿Dónde vive el bus de eventos? ¿Dónde se referencian las databases (SO) y cómo descienden? **Traza la cascada de propiedad** (sección 1.5) antes de agregar nada.
3. **Busca el análogo más cercano.** Antes de crear un sistema/clase/archivo, encuentra **uno existente de la misma forma** y copia su estructura: naming, carpeta, patrón de eventos, cómo recibe su SO, cómo se suscribe/desuscribe. **La consistencia con lo que ya existe le gana a la pureza teórica.**
4. **Confirma el plan contra el código REAL.** Lee los `.cs` de verdad; no confíes ciegamente en los docs (el vault y las memorias son foto de un momento y pueden estar desfasados — ver abajo).
5. **Recién entonces actúa, conformándote.** Iguala el idioma del código de alrededor: nombres, estructura, y densidad de comentarios (que aquí es **cero**, regla 3).

### La regla de conformidad

- **Ante la duda, haz lo que el código existente ya hace.**
- Si **debes** desviarte de un patrón existente (porque viola una regla de este handoff, o porque el diseño cambió), **dilo explícitamente y explica por qué** — nunca lo "arregles" en silencio. Juan decide si se migra.
- **El código es la verdad; los docs/memorias son pistas.** Si el vault dice X y el código dice Y, gana el código y **reportas el desfase** para que se actualice el doc.

Esta directriz es la operacionalización de algo que la instancia anterior hacía siempre: *"miro la arquitectura actual antes de actuar"*. Hazlo tú también, explícitamente, cada vez.

---

## 1. Filosofía de arquitectura — la regla de oro

> **Una responsabilidad por archivo, una dirección de comunicación, un dueño por dato.**

Cuando una decisión NO esté cubierta por una regla concreta, se resuelve con estos **4 principios**. Las reglas específicas (sección 3) son sólo casos particulares de estos cuatro:

1. **Capas sin saltos de dos niveles.**
   `Data` (estado puro, sin lógica de orquestación) → `Systems/Core` (orquestación; **único dueño** de persistencia y red) → `World/UI` (representación).
   La representación **lee** estado y **reacciona** a eventos. NUNCA persiste ni toca la nube/servidor directamente. NUNCA salta de `World/UI` directo a `Data` saltándose `Core`.

2. **Comunicación cruzada sólo por bus o servicio explícito.**
   Un sistema no busca a otro con `Find*`, `GetComponent*`, `GetComponentInParent`, ni referencias serializadas a sistemas hermanos. Se comunican por un **bus de eventos** (gameplay), por **eventos `static`** de un manager de UI (UI), o por **inputs**. *El evento transporta la data.* El suscriptor no sale a buscar al emisor para pedirle el estado.

3. **Límite de tamaño/dominio.**
   Si un archivo supera ~**400 líneas** O mezcla 2+ dominios (datos, presentación, física, red), se parte en **clases/componentes independientes**, una responsabilidad cada uno. Crecer NO se resuelve escondiendo el tamaño en varios archivos.

4. **Singleton = servicio runtime; ScriptableObject = data — y los SO NUNCA son estáticos.**
   Un **servicio runtime** (manager/controller) puede ser singleton y exponer `.Instance`. Un **ScriptableObject es data**: NO expone `static Current` ni ningún acceso estático global. Un SO se conecta **referenciado serializado dentro de su manager/apex** y su data **desciende** hacia los objetos runtime por `Initialize(...)`. El detalle de este patrón está en la **sección 1.5** — es central para Juan, léela completa.

---

## 1.5 Cascadas de responsabilidad y conexión Database ↔ SO ↔ GameManager (EL MODELO DE JUAN)

> Esta sección es la que más le importa a Juan. Es el cómo concreto de "un dueño por dato" y "una responsabilidad por archivo". Respétala al pie de la letra.

### 1.5.1 El mental model: "pirámides pequeñas que forman una grande"

Juan diseña por **cascadas de responsabilidad**. Cada **dominio** tiene un **apex** (un controller/manager) que es **DUEÑO de las referencias de su dominio**. Los hijos de ese dominio NO buscan data por su cuenta: se la **piden al apex** (por su `.Instance`, porque un servicio runtime singleton sí está permitido). El apex, a su vez, **cuelga de `GameManager`**, que es el apex raíz, dueño de lo global.

```
                 GameManager  (raíz: registry, inventory, databases globales, perfiles)
                 /     |      \
        DomainAController  DomainBController  DomainCController   ← apex por dominio
           /   \              |                   |
     hijoA1  hijoA2        hijoB1             objetos runtime
     (piden al apex su data por .Instance; no hay globals ocultos)
```

- **Apex de dominio** = dueño de las refs de SO/data de ese dominio (ej. un `CombatController` dueño de su `CombatConfigSO`; un `BreedingController` dueño de su `BreedingRulesSO`).
- **GameManager (raíz)** = dueño de lo global: el registry/estado persistido, inventario, y las **databases** compartidas (ej. una `FurTypeDatabaseSO`, una `PersonalityProfilesSO`).
- **Los hijos suben al apex** para pedir data; **nunca** bajan a un global estático.

**Por qué Juan trabaja así:** preserva la **trazabilidad de quién es dueño de qué dato**. Si todo sale de un `static Current`, nadie es dueño y el grafo de dependencias se vuelve invisible. Subir al apex hace explícita la propiedad.

### 1.5.2 Cómo se conecta una Database (SO) — el patrón exacto

**Regla dura: una Database es un ScriptableObject, y se conecta SERIALIZADA dentro de su manager/apex, NO por un acceso estático.**

✅ **Correcto:**
```
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }   // servicio runtime → singleton OK

    [SerializeField] private FurTypeDatabaseSO furTypeDatabase;   // SO referenciado serializado (drag en inspector)
    [SerializeField] private PersonalityProfilesSO personalities; // otra database, también serializada

    private void Awake()
    {
        Instance = this;
        // la data DESCIENDE hacia los objetos runtime por Initialize, no se accede por un static
        creatureSpawner.Initialize(furTypeDatabase, personalities);
    }
}

public class CreatureSpawner          // hijo: recibe la data desde arriba
{
    private FurTypeDatabaseSO _db;
    public void Initialize(FurTypeDatabaseSO db, PersonalityProfilesSO p) { _db = db; /* ... */ }
}
```

❌ **Incorrecto (lo que Juan NO quiere):**
```
public class FurTypeDatabaseSO : ScriptableObject
{
    public static FurTypeDatabaseSO Current;   // ❌ SO estático: rompe el modelo de dueño
}
// ...y cualquiera, desde cualquier capa, hace FurTypeDatabaseSO.Current.GetThing()  ❌
```

**El flujo de la data es de arriba hacia abajo (descenso por `Initialize`):**
1. El SO-database vive como **asset** y se **arrastra serializado** al manager/apex que lo posee.
2. El manager/apex, en su arranque, **inicializa** a sus hijos pasándoles las referencias que necesitan (`Initialize(...)`).
3. Los objetos **runtime** (a menudo pooled) reciben su data **desde arriba**; nunca la buscan en un estático global.

### 1.5.3 Para Odin / data compleja

Databases que contengan diccionarios o polimorfismo usan `SerializedScriptableObject` con `[OdinSerialize]` (regla 7). Eso es ortogonal a lo anterior: sigue siendo un SO serializado en su apex, sólo que con el serializador de Odin para soportar la estructura.

### 1.5.4 Cómo aplicar al proyecto nuevo (checklist)

- [ ] ¿Cada SO-database está **referenciada serializada** en un manager/apex, y NO tiene `static Current`?
- [ ] ¿Cada dominio tiene **un apex** dueño de sus refs, colgando de `GameManager`?
- [ ] ¿Los hijos **piden al apex** (por `.Instance`) en vez de leer globals estáticos?
- [ ] ¿La data **desciende** por `Initialize(...)` a los objetos runtime/pooled?
- [ ] ¿`GameManager` es el único dueño de lo **global** (registry, inventario, databases compartidas, persistencia)?

---

## 2. Estructura de capas y carpetas

### 2.1 Las tres capas (mental model)

```
Data          → Estado puro. POCOs, structs, ScriptableObjects de definición,
                 enums, contratos de serialización. No conoce a Core ni a UI.
                 No persiste. No sabe que existe la red.

Systems/Core  → Orquestación. Managers, servicios, lógica de negocio.
                 ÚNICO dueño de: persistencia, red/nube, ciclo de vida de datos.
                 Emite eventos. Escucha inputs. Decide.

World / UI    → Representación. MonoBehaviours en escena, vistas UITK/uGUI,
                 visualizadores, agentes, gizmos. LEEN estado y REACCIONAN a
                 eventos. Cero persistencia, cero red.
```

### 2.2 Carpetas — organizar por DOMINIO, no por tipo de Unity

Dentro de cada capa, subdivide por **dominio funcional**, no por "todos los Managers juntos". Estructura de referencia (adáptala al proyecto nuevo; los nombres de dominio cambian):

```
Assets/<Proyecto>/
├── Scripts/
│   ├── Core/                 ← managers, servicios, bus de eventos, persistencia
│   │   ├── Events/           ← GameEvents (bus) + definiciones de eventos
│   │   ├── Persistence/      ← SaveSystem, sync de nube, dueño de datos
│   │   └── <Sistema>/        ← un subfolder por sistema de orquestación
│   ├── Data/                 ← estado puro, por dominio
│   │   ├── Databases/        ← ScriptableObjects-database (Odin)
│   │   ├── <Dominio>/        ← definiciones/SO/POCOs de ese dominio
│   │   └── Enums.cs          ← enums compartidos
│   ├── World/                ← MonoBehaviours de escena, por dominio
│   │   └── <Dominio>/
│   ├── UI/                   ← vistas, manager de UI con eventos static
│   └── Utils/                ← helpers PUROS sin estado (static)
├── ScriptableObjects/        ← las instancias .asset, espejando dominios
├── Prefabs/
├── Scenes/
└── ...
```

Regla práctica de carpetas: **el árbol de carpetas debe poder leerse como el mapa de dominios del juego.** Si para entender dónde vive un sistema hay que abrir archivos, la estructura está mal.

### 2.3 Namespace

Todo `.cs` bajo **un namespace de proyecto único** (ej. `namespace <Proyecto>Simulator`). Mover archivos `.cs` se hace **con Unity cerrado** y arrastrando el `.cs` **junto a su `.meta`** (si no, se rompen GUIDs y referencias del editor).

---

## 3. Reglas de código NO NEGOCIABLES

Estas son los casos concretos de los 4 principios. Son reglas, no sugerencias.

1. **Desacoplamiento estricto vía eventos.** Comunicación cross-system SÓLO por el bus de eventos. El evento lleva la data. El suscriptor NO hace `Manager.Instance.Registry` para reconstruir el contexto.

2. **Persistencia sólo por evento.** Ningún script de gameplay llama a `SaveSystem.Save...` ni a `PushToCloud`. Sólo emite un evento (`OnRegistryChanged` o equivalente). **Un único sistema** (el manager dueño) escucha y persiste. Hay exactamente **un dueño** de la persistencia.

3. **Sin comentarios en el código.** No agregues `//` ni `/* */` salvo pedido expreso de Juan. La documentación vive en el vault (sección 5), no en el código. El código se explica con buenos nombres.

4. **Sin features adelantadas.** No implementes mecánicas antes de su etapa del roadmap. Si algo "sería útil para después", se anota, no se construye.

5. **Contratos de serialización ligeros y estables.** Si el proyecto tiene identidad que viaja por red o se persiste (DNA, IDs, save-strings), el contrato de string/serialización es **ligero, determinista y estable**. La metadata (timestamps, etc.) NO es parte del contrato genético/de identidad. Un round-trip `ToID()/FromID()` debe ser exacto.

6. **IDs sin caracteres reservados.** Si un separador estructura un string compuesto (ej. `-`), los IDs de las partes NUNCA pueden contener ese carácter. Define los separadores reservados una vez y respétalos.

7. **Odin para data serializada compleja.** ScriptableObjects que guarden diccionarios/polimorfismo usan `SerializedScriptableObject` con `[OdinSerialize]`. No pelees con el serializador de Unity para estructuras que no soporta.

8. **Sin complejidad innecesaria.** No agregues campos, abstracciones ni features no pedidos. La abstracción se introduce cuando hay 3+ usos reales, no "por si acaso".

9. **Desuscribir SIEMPRE.** `OnEnable` suscribe, `OnDisable` desuscribe. Un `event static` mantiene vivo al suscriptor → memory leak + excepción al disparar sobre un objeto destruido. Esta regla se rompe sola si no la haces religiosa.

10. **Evitar referencias redundantes.** Centraliza la comunicación en eventos/singleton. Si dos sistemas necesitan hablar, pregúntate primero si un evento existente ya transporta esa data antes de crear una referencia nueva.

11. **`partial class` SÓLO por ventaja física de archivo.** Usa `partial` únicamente cuando partir el archivo da una ventaja real (evitar conflictos de Git, aislar código autogenerado). Si la clase *creció*, el remedio es **dividirla en clases/componentes independientes**, NO esconder el tamaño en varios archivos — una partial sigue siendo UNA clase con UN estado mutable: no reduce acoplamiento. Código puro (matemática, helpers sin estado) NUNCA va en partial: es **clase estática aparte**. Tooling dev/debug que sólo usa **API pública** va en **componente aparte**.
   *Excepción pragmática* (sólo si se documenta como deuda): un núcleo con estado mutable irreducible (FSM MonoBehaviour, árbol de UITK, sesión de red) puede usar partial; el tooling de dev/gizmos atado a ese estado privado es parte de esa excepción.

---

## 4. Arquitectura orientada a eventos (el corazón)

Este es el patrón que sostiene todo el desacoplamiento. **Implémentalo el día 1** en el proyecto nuevo.

### 4.1 Bus de eventos de gameplay

Un único archivo estático (`GameEvents.cs`) con `static event Action<...>` por evento de dominio. Ejemplo de forma (los eventos concretos cambian por proyecto):

```
public static class GameEvents
{
    public static event Action OnRegistryChanged;   // toda mutación que debe persistir
    public static event Action OnRegistryReloaded;   // tras pull de nube: SÓLO UI, NO re-persiste
    public static event Action<T> OnEntityCreated;
    public static event Action<T> OnActionCompleted;
    // Raise* helpers para disparar con null-check
}
```

Patrón clave: distinguir **mutación local** (persiste + refresca UI) de **reload externo** (sólo refresca UI, NO re-persiste — si no, entras en loops de escritura nube↔local).

### 4.2 Eventos de UI

Viven separados del bus de gameplay, como `static event Action` dentro del **manager de UI**. La UI no contamina el bus de gameplay y viceversa.

### 4.3 Reglas del patrón de eventos

- El evento **transporta la data** que el suscriptor necesita. Nada de "te aviso que algo cambió, andá a buscarlo".
- Suscribir en `OnEnable`, desuscribir en `OnDisable` (regla 9). Sin excepción.
- El emisor no sabe quién escucha. El suscriptor no sabe quién emitió. Si necesitas saberlo, el diseño está mal.

---

## 5. Sistema de documentación (vault) — cómo NO perder contexto entre sesiones

Esta es la pieza que permite que una IA nueva retome sin reentrenar. Replícala en el proyecto nuevo. Tres capas:

| Capa | Qué es | Dueño | Cuándo se actualiza |
|------|--------|-------|---------------------|
| **Diseño** (Notion / wiki) | Diseño vivo, decisiones, preguntas abiertas. Fuente de verdad de *qué* es el juego. | **Juan** | Él. La IA NO la toca. |
| **Vault de implementación** (Obsidian) | Detalle técnico, quirks, invariantes, flujo de cada sistema. Fuente de verdad de *cómo* está hecho. | IA (a pedido) | Al cambiar un contrato público o aparecer un quirk nuevo. |
| **ScriptNodes** | Un nodo `.md` por cada script `.cs` (responsabilidad, campos públicos, conexiones). | Sub-agente **Haiku** | Automático al cierre de sesión si se tocaron scripts. |

### 5.1 Estructura del vault

```
<Proyecto>Vault/
├── 00 - Index.md            ← entry point de la IA: routing "para tal tarea, leé tal nota"
├── Index/                   ← una nota por dominio (01-NN): diseño, flujo, invariantes
│   └── 09 - Active Context.md  ← ESTADO ACTUAL de la sesión (lo más importante)
└── ScriptNodes/             ← un .md por script .cs
```

### 5.2 `Active Context` — el archivo más importante

Es el **estado vivo** del trabajo. Al **abrir** sesión, la IA lo lee primero. Al **cerrar**, lo actualiza. Debe terminar **siempre** listando: qué archivos `.cs` se modificaron y cuáles se crearon en la sesión. Esa lista es el input del agente de vault.

### 5.3 Agente de vault (Haiku)

Al cierre de sesión, **si se tocaron scripts**, invocar un sub-agente con `model: haiku` (Agent tool) para actualizar los `ScriptNodes/`. El prompt de handoff al sub-agente es **autocontenido** (Haiku no tiene contexto del proyecto): ruta del vault, lista de scripts tocados con su estado (NUEVO/MODIFICADO → nodo destino), y la instrucción de leer cada `.cs` y actualizar/crear su `.md` sin tocar nada más. Juan tiene **autorizado** este paso sin pedir confirmación cada vez. (Nota histórica: no usar herramientas externas tipo `opencode`/Deepseek — se colgaban; usar sub-agente Claude Haiku.)

### 5.4 Cuándo NO actualizar el vault

Bug menor/cosmético sin cambio de contrato → no actualices el vault, el `git log` basta.

---

## 6. Protocolo de sesión (el flujo de trabajo, paso a paso)

Esto es lo que haces en CADA sesión de trabajo no trivial:

1. **Abrir sesión** → leer `Active Context` (estado actual y siguiente paso).
2. **Identificar el sistema** → usar `00 - Index.md` para enrutar la tarea al dominio.
3. **Leer el diseño** → abrir la nota `Index/XX` del dominio (diseño, flujo, invariantes).
4. **Leer los ScriptNodes** → abrir los `.md` de los scripts implicados ANTES de abrir el código fuente. Llegas al `.cs` sabiendo ya qué hace y cómo se conecta.
5. **Planear (Opus)** → diseñar la solución antes de picar. Alternativas, invariantes, impacto en otros sistemas. Acordar el plan con Juan.
6. **Recién entonces abrir los `.cs`** → confirmar que el plan encaja con el código real.
7. **Delegar a sub-agentes (Sonnet)** → uno por archivo/responsabilidad. Cada sub-agente recibe: el plan, la ruta del archivo, y las reglas de código de la sección 3. Cuando hay trabajo independiente, lanzar varios en paralelo.
8. **Cerrar sesión** → actualizar `Active Context` (qué se tocó, qué sigue, lista de `.cs` modificados/creados).
9. **Disparar agente de vault (Haiku)** → si hubo scripts tocados (sección 5.3).
10. Cada mensaje al usuario empieza con `Juan:`.

---

## 6.1 Delegación a sub-agentes (cómo usar Opus / Sonnet / Haiku)

Juan trabaja con **tres niveles de agente**. Delegar no es opcional ni un atajo: es parte del método. La regla de fondo es **el que piensa no es el que pica, y el que documenta no gasta tokens caros**.

### 6.1.1 Opus — el que piensa (TÚ, el agente principal)

No delegues el pensamiento. Opus se queda con:
- El **plan** y las **decisiones de arquitectura** (alternativas, invariantes, impacto cross-system).
- El razonamiento que cruza varios sistemas a la vez.
- La **revisión e integración** de lo que devuelven los sub-agentes (no aceptes su código a ciegas: verifícalo contra el plan y las reglas de la sección 3).
- La conversación con Juan.

### 6.1.2 Sonnet — los que pican (implementación)

Cuando el plan ya está acordado, delega la **implementación concreta** a sub-agentes Sonnet: **uno por archivo o por responsabilidad** (coherente con "una responsabilidad por archivo"). Cada sub-agente es autocontenido — no comparte tu contexto, así que su prompt debe incluir:

- **El plan acordado** (la parte que le toca a ese archivo).
- **La ruta exacta del archivo** a crear/modificar.
- **Las reglas de código de la sección 3** que apliquen (sin comentarios, eventos, desuscribir, no static SO, etc.).
- **Los contratos relevantes**: qué eventos emite/escucha, qué firma pública debe respetar, de quién recibe la data (descenso por `Initialize`).

Cuándo lanzar varios en paralelo: si las tareas son **independientes** (archivos distintos sin dependencia entre sí), lánzalos en **un solo mensaje con varias tool calls** para que corran concurrentes. Si B depende de A, va secuencial.

> Mientras la verificación lo confirme: prefiere planear con Opus y delegar lo mecánico a Sonnet, salvo que el cambio sea trivial o de un solo archivo, donde lo haces tú directo.

### 6.1.3 Haiku — el que documenta (vault, al cierre)

Al **cerrar sesión**, si se tocaron scripts `.cs`, dispara un sub-agente **Haiku** para actualizar los `ScriptNodes/` del vault. Juan tiene esto **autorizado** sin confirmación por sesión.

Invocación concreta (Agent tool):
- `subagent_type: general-purpose`
- `model: haiku`
- `run_in_background: true`

Prompt de handoff (autocontenido, Haiku no conoce el proyecto):
- **Ruta del vault** `ScriptNodes/`.
- **Lista de scripts tocados** con su estado: `NUEVO` / `MODIFICADO`, y la **naturaleza del cambio**.
- Instrucción: leer cada `.cs`, actualizar el `.md` existente (responsabilidad, campos públicos, conexiones) o crear uno nuevo **copiando el formato de un nodo existente**, y **no tocar ningún otro archivo**.

(Histórico: NO uses herramientas externas tipo `opencode`/Deepseek para esto — se colgaban sin output. El sub-agente Haiku de Claude resolvió ambas tandas sin problema.)

Si el agente de vault **falla**, repórtaselo a Juan y déjalo anotado como pendiente en `Active Context`.

### 6.1.4 Resumen de la delegación

| Nivel | Rol | Qué hace | Qué NUNCA hace |
|-------|-----|----------|----------------|
| **Opus** | Piensa | Plan, arquitectura, revisión, hablar con Juan | Picar lo mecánico sin plan |
| **Sonnet** | Pica | Implementa un archivo/responsabilidad con el plan + reglas | Decidir arquitectura por su cuenta |
| **Haiku** | Documenta | Actualiza ScriptNodes al cierre | Tocar código o archivos fuera del vault |

---

## 7. Receta para crear un sistema nuevo

Cuando Juan pide un sistema nuevo (inventario, combate, economía, lo que sea), el orden es:

1. **Definir el dominio y el dueño del dato.** ¿Qué estado posee este sistema? ¿Quién lo persiste? (Un solo dueño.)
2. **Capa Data primero.** Definir el estado puro: SO de definición, POCOs, enums, contrato de serialización si viaja/persiste. Sin lógica de orquestación.
3. **Capa Core.** El manager/servicio que orquesta. Decide qué eventos **emite** y a cuáles **se suscribe**. Si persiste, es por evento (regla 2).
4. **Eventos.** Agregar al bus los eventos del sistema (mutación-local vs reload-externo). Documentar quién dispara cada uno.
5. **Capa World/UI.** Visualizadores/agentes/vistas que LEEN estado y REACCIONAN a eventos. Cero persistencia.
6. **Hooks visuales desacoplados.** Si el sistema tiene feedback (animación, partículas, sonido), exponer hooks por evento/`UnityEvent` por momento — sin acoplar el código de gameplay al de feedback (patrón "Feel-ready": arrastrar el feedback en el inspector después).
7. **Verificar invariantes.** ¿Algún dato deriva de otro? Define la **fuente de verdad** y haz que lo demás derive de ella (no dupliques estado que pueda desincronizarse).
8. **Documentar.** Nota de dominio en `Index/`, nodos en `ScriptNodes/`, actualizar `Active Context`.

**Chequeo de tamaño/dominio antes de cerrar:** ¿algún archivo pasa ~400 líneas o mezcla 2+ dominios? → partir en clases/componentes (regla 11), no en partials.

---

## 8. Convenciones de nombres

- **Singular/plural y código vs UI.** Decidir desde el principio el término de código vs el de UI/assets. Ejemplo del proyecto previo: en código la entidad era `Creature`/`CreatureDNA`; en UI/assets era el nombre temático del juego. **El código usa nombres neutros/técnicos; la UI usa nombres temáticos.** Esto evita renombres masivos cuando el branding cambia.
- **IDs auto-generados con prefijo por tipo** cuando aplique (ej. `BS0`, `A0`, `E0`...), respetando los caracteres reservados (regla 6).
- **Enums compartidos en un único archivo** (`Enums.cs`), no esparcidos.
- **Un renombre de campo serializado rompe los saves.** Si renombras algo persistido, el plan debe incluir migración o wipe explícito del save — avísalo, no lo descubras en runtime.

---

## 9. Anti-patrones — qué NO hacer (lista negra)

- ❌ `static Current` / acceso estático en un ScriptableObject → referenciarlo serializado en su apex y descender por `Initialize` (sección 1.5).
- ❌ Que un hijo lea un global oculto en vez de pedir su data al apex de su dominio → cascada de responsabilidad rota.
- ❌ `Find*` / `GetComponentInParent` para localizar otro sistema → usar eventos.
- ❌ Que la UI o un MonoBehaviour de escena persista o toque la nube → sólo el dueño Core, por evento.
- ❌ Comentarios en el código → vault.
- ❌ `partial` para esconder que una clase creció → dividir en clases reales.
- ❌ Suscribir a un `static event` y no desuscribir → leak + crash.
- ❌ Abstracción prematura / campos "por si acaso" → tres líneas similares está bien.
- ❌ Implementar mecánicas fuera del roadmap actual.
- ❌ Duplicar estado que pueda desincronizarse → una fuente de verdad, el resto deriva.
- ❌ Hacer commits o gestionar git por tu cuenta → es de Juan.
- ❌ Dejar pasos de Unity como "pendiente de tu lado" → Juan ya hizo el wiring.
- ❌ Mover `.cs` sin su `.meta` o con Unity abierto → rompe GUIDs.

---

## 10. Setup del proyecto NUEVO — qué crear el día 1

Para que esta metodología viva en el proyecto nuevo, crea (o pide a Juan que cree) esto al inicio:

1. **`CLAUDE.md` en la raíz del repo.** Es la "regla de oro" que la IA lee primero cada sesión. Debe contener, adaptado al proyecto nuevo: las preferencias de Juan (sección 0), los 4 principios (sección 1), las reglas no negociables (sección 3), la tabla de eventos, y el protocolo de sesión. Puedes copiar este handoff como base y recortar lo genérico.
2. **El vault** (`<Proyecto>Vault/`) con `00 - Index.md`, `Index/` (incluyendo `Active Context`) y `ScriptNodes/`.
3. **`GameEvents.cs`** (bus de eventos) y, si hay UI, el manager de UI con sus eventos `static`.
4. **El esqueleto de capas/carpetas** de la sección 2.2.
5. **La fuente de verdad de diseño** (Notion/wiki) — la mantiene Juan.

Adicional recomendado: un directorio de **memoria** persistente (estilo `memory/MEMORY.md` con un archivo por hecho: preferencias del usuario, feedback, decisiones de proyecto, referencias). Sirve para que la IA recuerde entre sesiones lo que NO está en el código: preferencias, decisiones de diseño no derivables del código, estado de etapas.

---

## 11. Resumen de una línea

> Capas sin saltos · comunicación sólo por eventos · un dueño por dato (apex de dominio que cuelga de GameManager) · SO referenciado serializado en su apex y descendido por `Initialize`, **nunca** estático · una responsabilidad por archivo · documentación en vault no en comentarios · planear con Opus, picar con Sonnet, documentar con Haiku · y Juan maneja git y el editor.
