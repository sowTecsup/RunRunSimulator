# `.claude/` — tooling del workflow

Documentacion del setup de Claude Code: skills, hooks, comandos y sub-agentes.
La fuente de verdad del **protocolo** es el `CLAUDE.md` de la raiz y la skill
`metodo-de-sesion`. Este archivo documenta las **herramientas** que lo automatizan.

---

## Mapa rapido: que disparo yo y que pasa solo

| Pieza | ¿Quien lo dispara? | ¿Cuando? |
|-------|--------------------|----------|
| Skills | El modelo, **solo**, cuando aplica su descripcion | Durante todo el trabajo |
| Hook `SessionStart` | El harness, **automatico** | Al abrir, reanudar o limpiar sesion |
| `/abrir-sesion` | **Juan**, opcional | Cuando quiere la apertura formal guiada |
| `/cerrar-sesion` | **Juan**, obligatorio | Al terminar de trabajar |
| Sub-agentes | El orquestador | Durante el trabajo, segun el protocolo |

> **Regla de bolsillo:** lo que tiene que pasar SIEMPRE -> **hook** (automatico).
> Lo que decide Juan segun el momento -> **comando** (a mano).
> Lo que depende del contexto de la tarea -> **skill** (se carga sola).

---

## Skills (`skills/`)

Son la memoria de metodo. Se cargan solas cuando la tarea coincide con su descripcion, y
por eso la descripcion importa tanto como el cuerpo: tiene que decir **cuando** cargarla,
no solo de que trata.

| Skill | Cubre |
|-------|-------|
| `metodo-de-sesion` | Protocolo, delegacion, autorizaciones, harness, dos equipos |
| `arquitectura-unity` | Regla de oro, reglas no negociables, composicion, cascada de SOs |
| `unity-editor` | CLI vs MCP, grupos de tools, quirks, esperas largas, clasificador |
| `qa-visual` | Test del texto plano, legibilidad, capturas miradas |
| `presentacion-y-juice` | MMFeedbacks obligatorio, vara Shapes, quirks de Feel |
| `vault-obsidian` | Estructura del vault, nodos, quien escribe que |
| `auditoria-y-limpieza` | Sesiones de deuda tecnica: metricas, priorizacion, ejecucion |

---

## Hooks (`hooks/`)

Los hooks son **programas** que el harness ejecuta solo, atados a un evento del ciclo de
vida. No pasan por el modelo: ocurren si o si. Se registran en `settings.local.json`.

### `SessionStart` -> `hooks/session-start.ps1`

- **Que hace**: lee el Active Context del vault y lo inyecta en el contexto al abrir sesion.
- **Por que es hook y no comando**: el estado actual debe estar SIEMPRE disponible, sin
  depender de que alguien se acuerde de leerlo. Cubre el paso 1 del protocolo de forma
  garantizada.
- **Nota tecnica**: el `.ps1` va en **ASCII puro**. Windows PowerShell 5.1 lee los scripts
  como ANSI y se rompe con acentos o em-dash.

> **No hay hook de `Stop`.** Se evaluo y se descarto: `Stop` se dispara al final de CADA
> turno, no "al terminar de trabajar". Un recordatorio ahi seria molesto e inutil. Cerrar
> sesion es una decision deliberada, y por eso es un comando.

---

## Comandos (`commands/`)

Plantillas de prompt que dispara Juan escribiendo `/nombre`. Comprimen pasos del protocolo
en una palabra, pero requieren que el las invoque.

### `/abrir-sesion [tarea]` — OPCIONAL

Resume el Active Context, hace el health-check del editor, identifica el dominio con el
indice del vault, lee las notas y nodos relevantes, y **frena antes de tocar codigo** para
presentar el plan.

Si Juan solo quiere arrancar, el hook ya le dio el estado y puede tirar la tarea directo.

### `/cerrar-sesion` — OBLIGATORIO al terminar

Arma la lista de `.cs` tocados, actualiza el Active Context, dispara el `vault-documenter`
si hubo cambio de contrato, y hace commit y push.

**Nada lo automatiza**: si no se dispara, el vault no se actualiza.

---

## Sub-agentes (`agents/`)

| Agente | Modelo | Rol | Disparo |
|--------|--------|-----|---------|
| `unity-coder` | sonnet | Implementa una tarea acotada segun plan aprobado | El orquestador, al delegar |
| `vault-documenter` | haiku | Actualiza los ScriptNodes al cierre | Via `/cerrar-sesion`, si hubo `.cs` con cambio de contrato |
| `notion-documenter` | haiku | Refleja impacto de diseno en la wiki | Solo con autorizacion explicita de Juan, nunca por defecto |

El **modelo** lo fija el frontmatter de cada agente (se puede override por invocacion).

**Fallback**: los sub-agentes en disco se cargan al iniciar sesion. Si uno se crea o edita
durante la sesion actual, no aparece hasta reiniciar. En ese caso, usar
`subagent_type: general-purpose` con el modelo correcto y pegar el contenido del `.md`
(la parte despues del frontmatter) como prompt.

---

## Ciclo de una sesion tipica

```
1. Abro Claude Code
   └─ hook SessionStart inyecta el Active Context          (automatico)

2. (opcional) /abrir-sesion [tarea]
   └─ routing de dominio + lectura de nodos + plan-first

3. Trabajo: planeo → delego a unity-coder → verifico en el editor
   └─ las skills se cargan solas segun lo que toque

4. /cerrar-sesion                                          (a mano, al terminar)
   └─ Active Context + vault-documenter + commit + push
```
