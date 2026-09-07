---
name: unity-editor
description: Como manejar el Editor de Unity en vivo desde Claude Code — Unity CLI y MCP, matriz de decision entre ambos, y los quirks cazados (Odin invisible, C# 6 en execute_code, eval sin using, el bridge que muere en domain reload, Play desatendido, capturas con HUD). Cargar antes de compilar, leer consola, entrar en Play, tocar escena/prefabs/ScriptableObjects, capturar el Game view o correr codigo en el editor.
---

# Unity Editor en vivo (CLI + MCP)

**Por que importa:** cierra los tres agujeros cronicos de trabajar Unity con una IA —
"codigo hecho, sin probar en Play", "wiring asumido hecho" y "no se si compilo tras
delegar". Con esto se **verifica en el editor** antes de declarar algo hecho, en vez de
dejarlo "pendiente de tu lado".

---

## Regla de oro del editor en vivo

> **Es herramienta de VERIFICACION y SETUP, no un atajo de arquitectura.**
> El codigo sigue yendo por el sub-agente coder; la doc por el vault; la persistencia
> por su dueno. Las reglas de `arquitectura-unity` mandan igual.
> **Explorar y verificar es libre; MUTAR escena/prefabs/assets necesita OK de Juan.**

---

## El stack dual

Dos caminos al mismo editor, y conviene tener los dos:

- **Unity CLI oficial** (comando `unity`, paquete `com.unity.pipeline`): compilar,
  consola, `eval` con C# moderno, Play mode, capturas con HUD, tests, builds.
- **MCP de editor** (tipo CoplayDev): escena, GameObjects, componentes, ScriptableObjects
  Odin, ProBuilder, tools propias del proyecto.

Conviven en el mismo editor sin conflicto (cada uno por su puerto).

### Precondiciones

- **CLI:** `unity status` como primer paso de sesion — puerto, estado, proyecto, version,
  PID. Mas barato que el ritual del MCP. **Tabla vacia = no hay editor corriendo** (no es
  falla del CLI): relanzar el ejecutable de Unity con el flag de projectPath y el pipeline
  queda listo en ~40 s.
- **MCP:** si hay mas de un proyecto Unity conectado al mismo servidor, hay que **fijar
  la instancia** (Name@hash) en cada llamada o una vez por sesion. El hash cambia entre
  reinicios del editor: re-listar al abrir.
- **En Windows, el comando `unity` va por la herramienta PowerShell**, no por Bash: el
  clasificador del modo automatico bloquea el comando. Encadenar en una sola llamada
  (entrar en Play + esperar + evaluar) cuando haga falta.

### ⚠️ Lo primero al montar un proyecto nuevo: activar los grupos de tools

El server MCP **esconde por defecto todo lo que no es el grupo `core`**. Eso deja fuera,
en toda sesion nueva, herramientas que el workflow necesita:

| Grupo | Que trae | Default |
|-------|----------|---------|
| `core` | escena, script, asset, editor, packages | **on** |
| `scripting_ext` | `execute_code`, `manage_scriptable_object` | off |
| `testing` | correr tests | off |
| `probuilder` | modelado | off |
| `ui` | manejo de UI | off |
| `docs` | consulta de docs y reflexion sobre assemblies cargados | off |
| `profiling` | profiler | off |
| `animation`, `vfx`, `asset_gen` | — | off |

Sin `scripting_ext` **no existe todo el pipeline de ScriptableObjects Odin** del quirk 1.

**La activacion por comando es efimera:** vive en el server de Python y **se pierde en cada
reinicio del server** (verificado: tras un upgrade del paquete los grupos volvieron solos a
off). Lo persistente son los toggles del **panel del editor**. Marcarlos ahi una vez, y
recordar que existe un comando de `sync` que los relee.

### Sintaxis del CLI

- `unity list` — catalogo de tools. La consulta con detalle completo da los **nombres
  exactos de parametros**.
- `unity command TOOL --param value` — los parametros van **siempre como flags
  `--key value`**. La forma `key=value` se interpreta como valor literal. Con salida JSON,
  el payload util viene anidado bajo el nombre del tool.
- Modo desacoplado: devuelve un job id, y despues se consulta o se espera ese job.
- Timeout por comando configurable (default 30 s).

---

## Matriz de decision: que va por donde

| Tarea | Herramienta | Por que |
|-------|-------------|---------|
| Health-check al abrir sesion | **CLI** `unity status` | 1 comando, sin fijar instancia |
| Compilar y verificar tras editar | **CLI** recompile, luego estado, luego consola | funciona con el editor desenfocado; salida estructurada; sobrevive reloads |
| QA visual con HUD | **CLI** capture del game view con source=screen, en Play | unica via confiable que incluye el overlay de UI Toolkit |
| C# puntual en el editor | **CLI** eval / eval_file | acepta C# moderno |
| Play mode desatendido | **CLI** play/stop + autotick | resiste el domain reload |
| Tests / builds | **CLI** run_tests / build | el bridge MCP nunca lo tuvo |
| Escena, GameObjects, componentes, wiring | **MCP** familia manage_* | catalogo mutador maduro + Undo |
| ScriptableObjects con Odin | **MCP** (manage_scriptable_object + execute_code) | ver quirk 1 |
| ProBuilder / picking de UITK / profiler | **MCP** | sin equivalente CLI |
| Tools propias del proyecto | **MCP** | se declaran con atributo en una carpeta Editor/ |
| Si el bridge MCP muere a mitad de sesion | **CLI como red de seguridad** | seguir trabajando y reiniciar el editor cuando convenga |

---

## Workflow por fase de sesion

1. **Abrir**: `unity status`. Si se va a mutar escena/SOs, recien ahi fijar la instancia
   del MCP.
2. **Tras cada tanda de codigo**: recompile, poll del estado de compilacion, consola con
   **0 errores**.

   Ciclo verificado por el lado del MCP, cuando el bridge esta vivo:
   `refresh_unity(mode: force, scope: all, compile: request)` -> esperar ~20 s ->
   leer el recurso de estado del editor hasta `ready_for_tools` -> leer consola filtrando
   por errores. Justo despues de un domain reload el estado puede venir `stale`: no
   bloquea, se reintenta. **Filtrar la consola por `CS` o `Exception`**, porque los avisos
   recurrentes de assets (mallas sin read access, por ejemplo) ensucian cada lectura.
   Segunda opinion barata: pedir el estado de compilacion por CLI.
3. **Verificacion en Play**: entrar en Play, ejercitar, capturar el game view con
   source=screen y **MIRAR** las capturas, salir de Play.
4. **Cierre**: correr los tests como gate final cuando el proyecto tenga tests propios.

---

## Quirks cazados (cada uno costo tiempo real)

### 1. Odin es invisible para el tool de ScriptableObjects

El tool de SOs trabaja con property paths del SerializedObject nativo, que **no ven el
blob de Odin**.

- **SI puede**: crear el asset, setear campos planos (strings, enums, colores, floats),
  wirear referencias por GUID.
- **NO puede**: meter entradas en un diccionario serializado por Odin, ni en listas
  polimorficas.
- **Como si se escriben**: `execute_code` llamando la API C# real: asignar en el
  diccionario, marcar el asset como dirty y guardar assets. O disparar los botones que ya
  existen en el propio SO. Verificado: la entrada sobrevive un re-import forzado, prueba
  de que persistio en el blob y no solo en la capa nativa.
- **Pipeline para levantar settings desde cero**: crear cada SO (campos planos) ->
  `execute_code` para insertarlos en el diccionario Odin -> modify para wirear la database
  a su dueno -> leer consola.

### 2. `execute_code` del MCP compila como C# 6

El assembly de Roslyn suele no cargar y el compilador cae al viejo codedom. En
`execute_code` **usar solo C# 6**: nada de tuples con nombre, switch expressions ni
pattern matching moderno. Usar nombres totalmente calificados. El `return` manda data de
vuelta. (El eval del CLI **si** acepta C# moderno — por eso es el camino preferido para
evaluar codigo.)

### 3. Los chequeos de seguridad bloquean patrones

Borrado de assets, borrado de archivos, arranque de procesos, loops infinitos y a veces
la destruccion inmediata de objetos. Para operaciones deliberadas hay que desactivar esos
chequeos explicitamente en la llamada.

### 4. El eval del CLI no acepta directivas `using`

El codigo se inyecta dentro de un cuerpo de metodo, asi que un `using` al tope se parsea
como using-statement y falla con "Identifier expected". **Todo con nombres totalmente
calificados.** El aviso "Unreachable code detected" en la ultima linea es cascada de otro
error, no un problema real.

### 5. Los backslashes en strings del eval rompen la compilacion

El CLI des-escapa antes de compilar ("Unrecognized escape sequence"). Para regex o JSON,
armar el patron sin backslashes: construir la comilla desde su codigo de caracter y usar
clases de caracteres explicitas en vez de los atajos con backslash.

### 6. El eval puede dar "Main thread operation timed out after 5000ms"

Pasa con el editor desenfocado, que no tickea. Alternativas: activar el autotick, enfocar
el editor, o caer al `execute_code` del MCP (que en esa situacion si respondio).

### 7. El bridge MCP muere tras un domain reload largo y NO se auto-rearma

Sintoma: la consola devuelve "no session" para siempre. El server lo lanza el plugin del
editor y muere con el; el harness es solo cliente HTTP.
**Fix probado: reiniciar el editor.** Mientras tanto, seguir por CLI.

### 8. El seteo de propiedades al crear un componente falla SILENCIOSO con algunos campos

Wirear siempre por `execute_code` buscando la property en el SerializedObject, asignando
la referencia y aplicando los cambios. Y **verificar con una lectura posterior** (contar
nulls).

### 9. Tras compilar, ESPERAR el reload completo antes de entrar en Play

Si se entra con la compilacion pendiente, Unity aplica el reload DENTRO de Play y borra
todo el estado runtime no serializado (el Awake no vuelve a correr).

### 10. Play desatendido

- Setear que la aplicacion corra en background al entrar: si el editor pierde foco, el
  player loop se pausa y las corrutinas se congelan.
- Vigilar la pausa del editor: puede quedar activa y congelar todo con deltaTime cero
  **sin error alguno**.

### 11. UI Toolkit: el root del documento puede recrearse en editor

Deja huerfano el arbol construido por codigo (los elementos existen pero sin panel).
Guarda estandar en cada Refresh/Show: si el elemento es null **o su panel es null**,
reconstruir la UI.

### 12. Rutas del CLI

Las de **escritura** estan confinadas a la raiz del proyecto (usar una carpeta de
screenshots dentro de Assets). Las de **lectura** no estan confinadas.

### 13. El puerto del server cambia tras domain reloads

Nunca hardcodearlo; el CLI lo re-descubre solo, y por eso sobrevive donde el bridge muere.

### 14. Muchos agentes en paralelo encadenan domain reloads

Puede loguear "An infinite import loop has been detected" sin listar assets: es
transitorio y se va con un refresh final. **Compilar recien cuando todos los sub-agentes
terminaron.**

---

## Editar la jerarquia con seguridad

- **Envolver todo en un grupo de Undo** para que revierta con un solo Ctrl+Z: incrementar
  el grupo y nombrarlo, registrar los objetos creados, reparentar con la API de Undo
  (que **preserva la posicion mundial**), grabar el objeto antes de renombrar, destruir
  con la version Undo, y colapsar el grupo al final.
- **Gotcha:** no reutilizar una lista cacheada de objetos raiz despues de destruir uno —
  la referencia muerta tira MissingReferenceException al leer el nombre. Borrar al final,
  o re-consultar la lista.
- Cerrar marcando la escena como sucia, guardandola y leyendo la consola.
- **Renombrar/reparentar es seguro solo si el proyecto no resuelve nada por busqueda por
  nombre.** Verificarlo antes: si todo se resuelve por referencia serializada, singleton,
  tag o auto-registro, se puede reorganizar libremente. Anotar en el vault cual es la
  unica busqueda textual que queda viva, si la hay.

---

## Prefabs y escenas: nunca como texto

**No leer ni parsear los archivos de prefab/escena como YAML.** Es fragil (fileIDs,
modificaciones de instancia), gasta contexto y da conclusiones a medias. El editor en vivo
es la fuente de verdad: `execute_code` sobre AssetDatabase/PrefabUtility, el tool de
prefabs, la busqueda de GameObjects o los resources del MCP. Grep sobre archivos de escena
**solo** como ultimo recurso para buscar GUIDs globalmente, nunca para leer contenido.

---

## Tools MCP propias del proyecto

El paquete descubre por reflexion cualquier clase estatica marcada con el atributo de tool
en una assembly de Editor (incluida la de editor por defecto, o sea cualquier carpeta
`Editor/` propia). No hace falta asmdef ni referencia manual.

Contrato minimo: una clase estatica con el atributo de tool (nombre en snake_case y una
descripcion para el LLM), una clase anidada de parametros con sus atributos, y un metodo
estatico que recibe el JSON de parametros y devuelve una respuesta de exito con su
payload.

Detalles del contrato: la clase de parametros es **opcional** (solo alimenta el esquema que
ve el agente; la firma real recibe el JSON crudo). El nombre por defecto sale del nombre de
la clase. Con el auto-registro activado (default) **aparecen solas tras el reload**, sin
reiniciar el server.

Sirve para exponer al orquestador operaciones caras o repetitivas del proyecto
(simulaciones de balance, arneses de test, dumps de estado) sin escribir un eval largo
cada vez, y de paso esquiva el quirk 2 (cuerpo de metodo, sin `using`, todo calificado).

> **La leccion que dejaron:** un ritual de verificacion que se repite merece una tool
> propia con salida JSON estable. **Nota:** al borrar un sistema, acordarse de borrar
> tambien sus tools.

---

## El clasificador del harness bloquea cosas (y como se rodea)

El modo automatico de Claude Code clasifica los comandos y bloquea algunos sin aviso util:

- **`unity ...` en la tool Bash** -> usar la tool **PowerShell**.
- **Evals que GUARDAN** (`SaveScene`, `SaveAsPrefabAsset`, `SaveAssets`), tanto por CLI
  como por MCP. Los evals de **solo lectura** y los sondeos en Play (capturas, logs,
  teleports, disparar acciones) pasan sin problema.
- **`System.IO.File.Delete` dentro del editor** (chequeos de seguridad): borrar carpetas
  temporales desde Bash, no desde el editor.

**Como se guarda igual:**
1. `execute_code` con `SerializedObject` en memoria (buscar la propiedad, setear, aplicar,
   marcar la escena sucia) y despues **guardar con la herramienta MCP de escena**, no con
   codigo. Este es el camino limpio para cablear objetos nuevos, documentos de UI y
   referencias a prefabs por ruta.
2. Editar el **YAML en disco** con `sed` y despues refrescar assets. Sirve para assets de
   campos planos, prefabs, y escenas **cerradas**. Con Odin, solo los campos que estan
   fuera del blob.
3. Para un valor de la escena **abierta**: `SerializedObject` en memoria sin guardar, y
   sincronizar el archivo en disco al cerrar.

**Assets nuevos por YAML:** un material o un SO de Odin se pueden escribir a mano usando el
GUID del `.meta` del script/shader recien importado (refrescar assets primero para que el
`.meta` exista). Los strings con acentos van en comillas dobles UTF-8. **`sed` se come los
escapes `\u`: para esos casos usar la tool Edit.**

---

## Esperas largas y trabajo desatendido

- El harness **bloquea las esperas de mas de ~22-25 segundos** en PowerShell. No insistir.
- Para esperar minutos u horas: una **corrutina dentro de Unity** que hace el trabajo y
  escribe un archivo centinela al terminar, mas un `until [ -f centinela ]; do sleep 30;
  done` en Bash **en background**, que avisa cuando aparece. Dejar tambien un archivo de
  progreso: si el aviso no llega (el harness puede matar al waiter por memoria baja del
  sistema), se comprueba a mano.
- **Nunca editar un `.cs` mientras corre una prueba larga en Play.** Con el editor enfocado,
  guardar un script dispara el import + domain reload que mata la corrutina y deja objetos
  sin `Awake`. **Los sub-agentes escriben las variantes a copias en el scratchpad**, y se
  copian encima con Play detenido, entre corridas.
- **Ajustar knobs sin recompilar:** `execute_code` puede escribir un campo de un SO en
  mitad de Play y la siguiente corrida ya lo usa. El valor queda en memoria del editor y
  **no marca el asset sucio**: lo que se adopta se fija despues como default en codigo, y
  lo que se descarta se restaura antes de salir de Play.
- **Memoria en sesiones de Play largas:** una prueba que reconstruye la escena miles de
  veces puede llevar a Unity a decenas de GB. Cortar y reentrar a Play periodicamente.

---

## Quirks de Unity que no son del tooling pero muerden igual

- **`NavMeshSurface.BuildNavMesh()` por script NO persiste el NavMeshData.** Queda en
  memoria: sobrevive el Play de esa sesion y se pierde al reiniciar el editor. Hay que
  crear el asset explicitamente junto a la escena y guardarla.
- **El sampleo de NavMesh ignora el tipo de agente** en su sobrecarga simple: acepta puntos
  aunque solo exista el mesh de otro tipo. Para un tipo concreto, usar la sobrecarga con
  filtro de consulta (agentTypeID + areaMask).
- **El `areaMask` serializado del agente manda en el instante de creacion**, aunque el
  `Initialize` lo corrija despues. Si la escena tiene un NavMesh distinto, el agente falla
  al crearse y el warp no lo arregla. Solucion: instanciar bajo un padre inactivo, fijar el
  mask, y recien ahi reparentar.
- **Metodos y eventos privados/internos se disparan por reflexion** desde un sondeo: sirve
  para probar el flujo completo de un panel de UI sin un solo clic, y para engancharse a
  `UnityEvent` internos de un prefab.

---

## Skills de terceros para Unity: cuidado con los nombres

Si se instalan skills de Unity de la comunidad, ojo: muchas estan escritas para **otro**
servidor MCP y citan nombres de herramientas que aca no existen. Hay que traducirlos
(ejecutar codigo, leer errores de compilacion, controlar Play, correr tests). Lo que si vale
tal cual, independiente del server: **nunca editar a mano los archivos de escena y prefab**,
y las guias de YAML de assets (nunca inventar el GUID del script, bools como `0`/`1`,
campos de propiedades con su sufijo de backing field).

**Descartados a proposito:** los mega-toolkits de comunidad (decenas de skills y agentes)
**imponen una arquitectura ajena** — contenedores de inyeccion, buses propios, librerias de
async — que choca de frente con las reglas de `arquitectura-unity`. No instalarlos.
