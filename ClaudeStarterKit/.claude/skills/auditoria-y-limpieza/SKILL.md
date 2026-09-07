---
name: auditoria-y-limpieza
description: Como se corre una sesion de auditoria de deuda tecnica y limpieza de codigo — relevamiento con numeros verificables archivo:linea, metricas que importan, priorizacion por impacto sobre esfuerzo, y como se ejecuta sin romper nada. Cargar cuando Juan pida analizar el proyecto, reducir duplicacion, eliminar complejidad, "acercar todo a la manera correcta de trabajar", o cuando toque revisar si la deuda volvio.
---

# Auditoria y limpieza

La disciplina de arquitectura **no se sostiene sola**. En este proyecto se pagaron nueve
fases de deuda hasta dejar cero `partial class`, y un mes despues **dos reglas ya se habian
re-endeudado**: nueve archivos por encima del limite de lineas, y los botones de debug de
vuelta adentro de clases de dominio. Por eso la auditoria es un ritual periodico, no un
evento unico.

---

## Fase 1 — Relevamiento con numeros, no con impresiones

Delegar el barrido a un agente de exploracion sobre el arbol real (grep, conteos de
lineas). **Todo hallazgo tiene que ser verificable con `archivo:linea`.** Una auditoria sin
referencias concretas no se puede ejecutar despues.

### Metricas que importan

| Metrica | Que revela |
|---------|------------|
| Total de archivos y lineas | La linea base para medir la limpieza |
| Archivos > 400 lineas | Violaciones del limite de tamano/dominio |
| Cantidad de `partial class` | Si la regla de composicion sigue viva |
| `OnEnable`/`OnDisable` balanceados en todos los suscriptores | Leaks de eventos estaticos |
| Handlers que consultan el singleton en vez de usar el payload | Violaciones del bus |
| `Find*` cross-system reales | Acoples que la regla prohibe |
| Lineas de comentario y su porcentaje por archivo | Deuda de la regla "sin comentarios" |
| Singletons sin limpieza de `Instance` en `OnDestroy` | Bug latente al cambiar de escena |
| Metodos publicos sin ningun llamador | Codigo muerto o features adelantadas |
| Bloques duplicados casi verbatim | Candidatos a helper compartido |
| Interfaces con un solo implementador | Abstraccion prematura |
| Escrituras a disco/nube fuera del dueno de persistencia | Violacion de la regla 2 |

### Criterio de codigo muerto en Unity

**No alcanza con "0 referencias en codigo".** Un script puede estar vivo solo desde una
escena o un prefab. El criterio correcto es **0 referencias de codigo Y 0 referencias por
GUID en escenas y prefabs**. Y aun asi, cuidado con lo que se invoca desde el Inspector
(botones de Odin, `UnityEvent`): parece muerto y no lo esta.

---

## Fase 2 — Priorizar por impacto sobre esfuerzo

Armar un **Top 10** ordenado por impacto ÷ esfuerzo, y clasificar cada item en una de dos
categorias, porque se ejecutan distinto:

- **Mecanicos, riesgo bajo** — codigo muerto, deduplicacion, helpers compartidos, poda de
  eventos huerfanos, purga de comentarios. Un sub-agente coder por item, **compilando y
  leyendo consola entre cada uno**.
- **Requieren re-wirear escena o prefabs** — mover configuracion a un SO, extraer dev
  consoles, partir componentes grandes. **Necesitan OK de Juan**, se planean con el modelo
  grande y **se hacen de a uno**.

Lo que no entra en el Top 10 y no es urgente va a una seccion de "a decidir": no se
resuelve por cuenta propia, se le presenta a Juan.

---

## Fase 3 — Ejecutar sin romper

- **Rescatar antes de purgar.** Los invariantes que vivian solo en comentarios se bajan a
  una nota del vault **antes** de borrar los comentarios. La regla es "sin comentarios en
  codigo", no "sin conocimiento".
- **Checkpoints de compilacion frecuentes**, no uno al final.
- **Con muchos sub-agentes en paralelo, compilar recien cuando todos terminaron**: los
  guardados simultaneos encadenan domain reloads.
- Preservar los **GUID** al renombrar o mover: se mueve el `.cs` con su `.meta`.
- Al absorber campos serializados en otra clase, **mantener los mismos nombres** para que
  el prefab conserve sus valores.
- Un cambio grande y mecanico sobre muchos archivos (por ejemplo aplicar un namespace raiz)
  se hace **con Unity cerrado, por script determinista**, preservando fin de linea y BOM, y
  sin re-indentar: asi el diff queda revisable.
- **Antes de un cambio masivo de serializacion, auditar que nada dependa de nombres de
  tipo**: referencias serializadas por tipo, manejo de nombres de tipo en JSON, binders.
  Si no hay ninguno, el cambio es seguro.

---

## Fase 4 — Registrar

La auditoria vive en una nota del vault (`Index/11 - Technical Debt` o equivalente):
mapa de salud por capa, hotspots medidos con lineas y dominios mezclados, hoja de ruta por
fases con su estado, y **la evidencia de las decisiones** — incluidas las que se tomaron y
despues se revirtieron.

> Guardar el razonamiento de una decision que despues resulto equivocada vale tanto como
> guardar la correcta: evita volver a recorrer el mismo camino.

Y una tabla resumen de prioridad con estado por item, para que la proxima auditoria empiece
sabiendo que ya se pago.
