# `.claude/` — Tooling de workflow MoriMonchis

Documentación del setup de Claude Code para RunRunSimulator: hooks, comandos y sub-agentes.
La fuente de verdad del **protocolo de trabajo** es `CLAUDE.md` (raíz). Este archivo documenta las **herramientas** que automatizan ese protocolo.

---

## Mapa rápido: ¿qué disparo yo y qué pasa solo?

| Pieza | ¿Quién lo dispara? | ¿Cuándo? |
|-------|--------------------|----------|
| Hook `SessionStart` | El harness, **automático** | Cada vez que abro/reanudo/limpio sesión |
| `/abrir-sesion` | **Vos**, opcional | Cuando querés la apertura formal guiada |
| `/cerrar-sesion` | **Vos**, obligatorio | Cuando terminás de trabajar |
| Sub-agentes | Yo (orquestador) | Durante el trabajo, según el protocolo |

**Regla de bolsillo**: lo que tiene que pasar SIEMPRE → hook (automático). Lo que decidís VOS según el momento → comando (a mano).

---

## Hooks

Los hooks son **programas** que el harness ejecuta solo, atados a un evento del ciclo de vida. No pasan por el modelo: ocurren sí o sí. Se registran en `settings.local.json`.

### `SessionStart` → `hooks/session-start.ps1`

- **Qué hace**: lee `MoriMonchiVault/Index/09 - Active Context.md` y lo inyecta automáticamente en el contexto de Claude al abrir sesión.
- **Por qué es hook y no comando**: el estado actual debe estar SIEMPRE disponible, sin depender de que alguien se acuerde de leerlo. Cubre el paso 1 del protocolo de forma garantizada.
- **Nota técnica**: el `.ps1` está en ASCII puro a propósito — Windows PowerShell 5.1 lee los scripts como ANSI y rompe con acentos / em-dash.

> **No hay hook de `Stop`.** Se evaluó y se descartó: el evento `Stop` se dispara al final de CADA turno, no "al terminar de trabajar". Un recordatorio ahí sería naggy e inútil. Cerrar sesión es una decisión deliberada → es trabajo de `/cerrar-sesion`, no de un hook.

---

## Comandos (slash commands)

Los comandos son **plantillas de prompt que disparás vos a mano** escribiendo `/nombre`. Comprimen pasos del protocolo en una palabra, pero requieren que vos los invoques. Viven en `commands/`.

> Los comandos y hooks nuevos se cargan al **iniciar sesión**. Si se crean/editan durante una sesión, no aparecen hasta reiniciar.

### `/abrir-sesion [tarea]` — OPCIONAL

- **Qué hace**: ejecuta los pasos 1-4 del protocolo:
  1. Resume el Active Context (usa lo que el hook ya inyectó).
  2. Identifica el dominio de la tarea con `00 - Index.md`.
  3. Lee la nota `Index/XX` y los `ScriptNodes` relevantes.
  4. Frena antes de tocar `.cs` y presenta el plan para confirmar.
- **Cuándo usarlo**: cuando querés la apertura formal y guiada. Si solo querés arrancar, el hook ya te dio el estado y podés tirarme la tarea directo.
- **`[tarea]`**: argumento opcional con el sistema/tarea del día.

### `/cerrar-sesion` — OBLIGATORIO al terminar

- **Qué hace**: ejecuta los pasos 8-9 del protocolo:
  1. Arma la lista de `.cs` tocados (NUEVO / MODIFICADO) vía `git status`.
  2. Actualiza `09 - Active Context.md` (qué se tocó + siguiente paso + lista de scripts).
  3. Si hubo cambio de contrato/responsabilidad, dispara el sub-agente `vault-documenter`.
  4. No toca Notion salvo autorización explícita.
- **Cuándo usarlo**: al cerrar el día de trabajo. Nada lo automatiza — si no lo disparás, el vault no se actualiza.

---

## Sub-agentes (`agents/`)

Definidos en disco, se cargan al iniciar sesión. Cada uno tiene su modelo fijo en el frontmatter.

| Agente | Modelo | Rol | Disparo |
|--------|--------|-----|---------|
| `morimonchi-coder` | sonnet | Implementa una tarea de código acotada según plan aprobado | Yo, al delegar implementación |
| `vault-documenter` | haiku | Actualiza `ScriptNodes/` al cierre | Automático vía `/cerrar-sesion` si hubo `.cs` con cambio de contrato |
| `notion-documenter` | haiku | Refleja impacto de diseño en Notion | Solo con autorización explícita de Juan, nunca por defecto |

El **modelo** lo fija el frontmatter de cada agente (puedo override por invocación). No existe un campo de "effort" configurable por agente; el effort explícito por tarea solo se pasa dentro de Workflows.

---

## Ciclo de una sesión típica

```
1. Abro Claude Code
   └─ hook SessionStart inyecta Active Context  (automático)

2. (opcional) /abrir-sesion [tarea]
   └─ routing de dominio + lectura de ScriptNodes + plan-first

3. Trabajo: planeo con Opus → delego a morimonchi-coder → reviso

4. /cerrar-sesion                                (a mano, al terminar)
   └─ actualiza Active Context
   └─ dispara vault-documenter si corresponde
```
