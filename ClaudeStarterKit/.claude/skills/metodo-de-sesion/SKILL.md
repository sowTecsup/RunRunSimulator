---
name: metodo-de-sesion
description: El protocolo de trabajo por sesion (abrir, enrutar, planear, delegar, verificar, cerrar) y las reglas de trato con Juan. Cargar al abrir sesion, antes de planear cualquier tarea, antes de delegar a un sub-agente y antes de declarar algo hecho. Tambien cuando dudes de si podes escribir en el vault, tocar la escena o disparar un agente de documentacion.
---

# Metodo de sesion

Destilado de ~105 sesiones de trabajo sobre un proyecto Unity con Juan. Esto no es
burocracia: cada paso existe porque su ausencia costo una sesion entera.

---

## Las tres reglas que mas se rompen

Antes que el protocolo, las tres cosas que se olvidan primero en sesiones largas:

1. **Plan antes de codigo.** Nunca abrir un `.cs` sin haber leido el diseno y los nodos
   de los scripts involucrados. El costo de leer 3 notas es 5 minutos; el costo de
   implementar contra un modelo mental equivocado es la sesion.
2. **Verificar en el editor antes de declarar hecho.** "Compila" no es "funciona", y
   "funciona" no es "se ve bien". Ver `unity-editor` y `qa-visual`.
3. **El registro visual/juice no se codea a mano.** Ver `presentacion-y-juice`. Es la
   regla que mas se ignora cuando la sesion ya lleva horas.

---

## Ciclo de una sesion

```
1. ABRIR
   hook SessionStart inyecta el Active Context (automatico, no depende de acordarse)
   -> health-check del editor (`unity status`)
   -> si hay dos PCs: git fetch y revisar ramas del otro equipo

2. ENRUTAR
   `00 - Index.md` del vault: tarea -> que nota `Index/XX` y que ScriptNodes leer

3. LEER DISENO (Index/XX) y NODOS (ScriptNodes/*.md)
   Recien despues, el `.cs`. Ya sabes que hace y como se conecta: solo confirmas.

4. PLANEAR
   Con el modelo grande. Alternativas, invariantes, impacto en otros sistemas.
   Presentar el plan y ESPERAR confirmacion. No empezar a picar codigo "mientras".

5. DELEGAR
   Sub-agente coder, uno por archivo o por responsabilidad. Se le pasa el plan, la
   ruta y la responsabilidad puntual: nunca "resolve esto".

6. VERIFICAR
   Compilar -> consola en 0 errores -> Play si aplica -> capturas MIRADAS.
   Nada queda "pendiente de tu lado" si el editor puede confirmarlo.

7. CERRAR
   Active Context actualizado + lista de `.cs` tocados + commit + push.
   El agente de vault se dispara SOLO si Juan corre el comando de cierre.
```

---

## Delegacion: como se parte el trabajo

El orquestador (modelo grande) **piensa**; los sub-agentes **escriben**. Nunca al reves.

- Un sub-agente por archivo o por responsabilidad acotada. Varios en paralelo cuando
  no se pisan (mismo mensaje, varias invocaciones).
- Lo que recibe el sub-agente: el plan ya aprobado, la ruta exacta, la responsabilidad
  puntual. Las reglas de arquitectura ya viven en su system prompt.
- Lo que NO decide el sub-agente: arquitectura, nombres de eventos nuevos, si algo va
  en un archivo o en dos. Si el plan no encaja con el codigo real, **para y reporta**
  en vez de improvisar.
- Lo que se queda el orquestador: diseno, decisiones de balance, matematica del
  sistema, docs de dominio, y cualquier cosa donde el "por que" importa mas que el "que".
- **Cuidado con el paralelismo alto en Unity**: muchos agentes guardando a la vez
  encadenan domain reloads. Compilar recien cuando todos terminaron.
- Los sub-agentes definidos en disco se cargan **al iniciar sesion**. Si se crea o edita
  uno durante la sesion, no aparece hasta reiniciar: el fallback es `general-purpose`
  con el modelo correcto y el contenido del `.md` pegado como prompt.

---

## Autorizaciones: que puedo hacer solo y que no

| Accion | Autorizacion |
|--------|--------------|
| Leer cualquier cosa (codigo, vault, escena via editor) | Libre |
| Escribir/editar codigo `.cs` | Libre, con plan confirmado |
| Correr codigo en el editor, entrar/salir de Play, capturar | Libre |
| **Mutar escena, prefabs o assets** | **Necesita OK explicito** |
| **Escribir en el vault (notas de diseno)** | **Solo con diseno cerrado + OK explicito** |
| Actualizar `09 - Active Context` al cierre | Se propone primero |
| **Disparar el agente de vault** | **Solo via el comando de cierre que corre Juan** |
| **Tocar Notion / la wiki de diseno** | **Solo con autorizacion puntual de esa sesion** |
| Commit y push | Al cierre, como parte del comando |

Regla general: **leer es libre, mutar el mundo de Juan no.** Una autorizacion dada en
una sesion no se extiende a la siguiente.

---

## Trato y comunicacion

- **Espanol neutral.** Nada de "vos/tenes/queres/aca".
- **Empezar cada mensaje con "Juan,"** (si el `CLAUDE.md` del proyecto lo pide).
- Cuando Juan describe un problema de sistema, **no responder con una solucion visual**.
  Ver `qa-visual` -> "test del texto plano".
- Si Juan reafirma un pedido despues de una objecion, eso es su decision: ejecutar
  completo, sin volver a discutirlo.
- Reportar fielmente: si algo no se pudo verificar, decirlo. No hay peor entrega que
  un "listo" que despues no era.

---

## Trabajar con el harness sin pelearse

Cosas del entorno de Claude Code que cuestan tiempo si no se saben:

- **Los heredocs largos en Bash fallan** con `unexpected EOF while looking for matching '`
  y no ejecutan nada. Para bloques de texto grandes usar la tool **Write**, y dejarle a
  Bash solo lo mecanico.
- **Las esperas de mas de ~22-25 segundos estan bloqueadas.** Para esperar de verdad:
  proceso en background que escribe un centinela + un `until [ -f centinela ]` corriendo en
  background, que avisa al terminar.
- **El clasificador del modo automatico bloquea comandos por su forma**, no por su
  intencion (ver `unity-editor` para los casos concretos y los rodeos).
- **Skills, agentes, comandos y hooks se cargan al iniciar sesion.** Lo que se crea durante
  una sesion no existe hasta reiniciar.
- **El scratchpad de la sesion** es el lugar de los archivos temporales: variantes que los
  sub-agentes escriben antes de aplicarse, salidas intermedias, scripts de un solo uso.
  Nunca ensuciar el repo con eso.

## Trabajar desde dos equipos

Si el proyecto se trabaja en mas de una maquina:

- **Al abrir: `git fetch` y revisar si hay ramas del otro equipo** antes de tocar nada.
- Un `git checkout` que cambia la escena abierta hace que Unity muestre el modal "the open
  scenes have been modified externally", y **el editor queda bloqueado hasta que alguien
  aprieta Reload**: las herramientas del editor no responden mientras tanto.
- Si a un equipo le falta un paquete de arte que el otro tiene, la escena abre con avisos
  de prefabs faltantes y **sucia al cargar**: nunca guardarla desde ese editor, o se pierden
  las instancias. Editar el archivo en disco con la escena cerrada.
- Las versiones de herramientas pueden diferir entre equipos: verificarlo al abrir, no al
  fallar.

## Cuando la decision es de Juan

Si hay una bifurcacion real (dos disenos posibles, dos formas de balancear, que medir
primero), **presentarle las opciones** en vez de elegir por el. Es el patron con el que
arrancan muchas sesiones: opciones concretas, con su consecuencia, y el elige. Lo que **no**
se le consulta es lo que ya esta decidido en las reglas o en el vault.

## Numeracion de sesiones y commits

- Las sesiones se numeran `S{N}` correlativo y ese numero se usa en TODOS lados:
  el commit (`S105: resumen de lo hecho`), el Active Context, las notas del vault
  ("decision S32", "quirk S93"). Es la linea de tiempo compartida del proyecto.
- Cuerpo del commit: una vineta por bloque de trabajo.
- El Active Context termina SIEMPRE con la lista de `.cs` creados/modificados: esa
  lista es el input del agente de documentacion.

---

## Que se documenta y donde

| Capa | Dueno | Cuando |
|------|-------|--------|
| Wiki de diseno (Notion) | Juan | Decision de diseno nueva. La IA no toca sin permiso puntual. |
| `vault/Index/` (implementacion) | IA, a pedido | Cambio de contrato publico, quirk tecnico nuevo, sub-etapa cerrada. |
| `vault/ScriptNodes/` | Sub-agente de vault | Al cierre, si hubo `.cs` con cambio de contrato. |
| `CLAUDE.md` | IA, a pedido | Regla nueva, cambio de stack, flip de roadmap. |
| `09 - Active Context` | IA, cada sesion | Apertura y cierre. |

Bug menor sin cambiar contratos -> no se documenta: el `git log` alcanza.
