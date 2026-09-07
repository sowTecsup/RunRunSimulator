---
name: vault-obsidian
description: Estructura y uso del vault Obsidian como memoria tecnica del proyecto — entry point de routing, notas de dominio, un ScriptNode por script, Active Context como estado vivo, y las reglas de quien escribe que y cuando. Cargar al abrir sesion, al buscar donde vive el conocimiento de un sistema, antes de escribir cualquier nota, y al cerrar sesion.
---

# El vault como memoria tecnica

El vault Obsidian es **la memoria de implementacion** del proyecto: lo que hace cada
script, como se conecta, que invariantes tiene y por que las decisiones tecnicas se
tomaron asi. Existe para que la IA sepa que hace un sistema **sin abrir el `.cs`**, y para
que ese conocimiento sobreviva al final de la ventana de contexto.

**Division de aguas:**
- **Wiki de diseno (Notion o similar)** = diseno vivo, decisiones de juego, preguntas
  abiertas. Dueno: Juan. Cuando dudes de **diseno**, ahi.
- **Vault (Obsidian)** = implementacion, quirks tecnicos, archivos clave. Cuando dudes de
  **codigo**, aca.

---

## Estructura

```
<Proyecto>Vault/
├── 00 - Index.md          <- ENTRY POINT: routing tarea -> archivos a leer
├── Index/                 <- una nota por dominio y por tema de diseno (01, 02, 03...)
│   ├── 08 - Known Bugs & Checkpoints.md
│   ├── 09 - Active Context.md          <- estado vivo de la sesion actual
│   ├── 09b - Session Digest.md         <- historia comprimida de sesiones viejas
│   ├── 11 - Technical Debt.md          <- hoja de ruta de saneamiento
│   └── 12 - Unity MCP.md               <- tooling del editor
└── ScriptNodes/           <- un .md por cada .cs (~200 en un proyecto maduro)
```

### `00 - Index.md` — el entry point

Es lo primero que se lee. Contiene:
- Una **tabla de routing**: `Tarea -> que nota Index leer -> que ScriptNodes leer`.
- El **arbol de carpetas del codigo**, comentado.
- Los **patrones arquitectonicos no negociables** resumidos.
- Un **indice por palabra clave** (el usuario dice "corral" y la tabla dice donde mirar).

Cuando se agrega un sistema nuevo, se agrega su fila de routing. Si no esta en la tabla,
para efectos practicos no existe.

### `Index/XX - Tema.md` — notas de dominio

Diseno, flujo, invariantes de un sistema. Se numeran correlativamente y **no se
renumeran** (los enlaces las referencian por numero). Cuando un sistema muere, su nota se
marca como historica en vez de borrarse: el "por que fallo" es tan valioso como el "como
funciona".

### `ScriptNodes/NombreScript.md` — un nodo por script

Formato (ver `plantillas/vault/ScriptNodes/_PLANTILLA.md`):

```markdown
---
tags: [script, dominio, subdominio]
---

# NombreScript.cs

**Ruta:** `Carpeta/Sub/NombreScript.cs`

**Responsabilidad:** una frase. Que hace y que NO hace.

**Metodos publicos:**
- `Firma()` — que hace, que devuelve, cuando falla

**Estado / propiedades:**
- `campo` — que representa

**Invariantes:** las reglas que no se pueden romper

**S{N} Cambios:** que cambio en cada sesion relevante

**Conexiones:** [[OtroScript]], [[Index/XX - Tema]]
```

Los `[[wikilinks]]` son el grafo: seguirlos es como se navega el proyecto. Un link a un
nodo que todavia no existe es valido — marca algo que falta escribir.

### `Index/09 - Active Context.md` — el estado vivo

La nota mas importante del dia a dia. Se inyecta automaticamente al abrir sesion via hook.
Contiene: numero y fecha de sesion, foco, que se hizo, decisiones tomadas, y **al final la
lista de `.cs` creados/modificados** — esa lista es el input del agente de documentacion.

Cuando crece demasiado, la historia vieja se comprime a `09b - Session Digest`.

---

## Quien escribe que y cuando

| Archivo | Dueno | Cuando |
|---------|-------|--------|
| `00 - Index.md` | IA, a pedido | Sistema nuevo, o routing que quedo mal |
| `Index/XX` | IA, **con OK explicito** | Diseno CERRADO. Nunca a mitad de una iteracion. |
| `ScriptNodes/` | Sub-agente de vault | Al cierre de sesion, si hubo `.cs` con cambio de contrato |
| `09 - Active Context` | IA, cada sesion | Apertura y cierre (se propone antes de escribir) |

**Regla dura:** no actualizar el vault a mitad de un diseno. Durante la teorizacion, las
propuestas viven en la conversacion. Se escribe **solo cuando el diseno esta cerrado Y
Juan aprueba explicitamente**.

**Regla dura 2:** el agente de vault **nunca** se dispara por iniciativa propia. Solo
cuando Juan corre el comando de cierre.

**Bug menor sin cambio de contrato -> no se documenta.** El `git log` alcanza.

---

## Como usar el vault (orden de lectura)

1. Identificar la tarea en la tabla de routing de `00 - Index.md`.
2. Leer la nota `Index/XX` del dominio: intencion de diseno, flujo, invariantes.
3. Leer los `ScriptNodes` involucrados: responsabilidad, conexiones. Seguir los wikilinks.
4. Revisar `08 - Known Bugs` por si el problema ya esta identificado.
5. **Recien ahora** abrir los `.cs`. Ya sabes que hacen: solo confirmas que el plan encaja.

Saltarse los pasos 2-4 es la causa numero uno de implementar contra un modelo mental
equivocado.

---

## Higiene del vault

- **Los sistemas muertos se marcan, no se borran.** Un sistema demolido deja: la nota
  marcada como historica, el hash de git donde vive el codigo, y **las lecciones
  reutilizables**. Eso ultimo es lo que hace que la demolicion no sea perdida total.
- **Los invariantes que vivian en comentarios del codigo se rescatan al vault** antes de
  borrar los comentarios (la regla es "sin comentarios en codigo", no "sin conocimiento").
- El digest de sesiones viejas se comprime periodicamente: la linea de tiempo importa,
  el detalle de hace 60 sesiones no. Cuando el archivo historico se vuelve enorme, se
  destila a un digest de una linea por sesion y **el texto completo se borra del vault
  citando el commit de git donde queda**. El vault es memoria de trabajo, no archivo
  muerto.
- En el digest, marcar los sistemas que murieron y **en que sesion**. Una linea que dice
  "sobreviven las lecciones, no el codigo" ahorra que alguien salga a buscar ese codigo.
- Cuando una nota deja de ser la fuente de verdad, decirlo **en la propia nota y en la
  tabla de routing**, con el puntero a la que la reemplaza.

---

## La nota de bugs conocidos

Vale la pena mantenerla con esta forma, porque cada seccion se usa distinto:

- **Bugs activos** — tabla `Bug | Causa | Estado`. El estado incluye "mitigado" y
  "aceptable en testing": no todo se arregla, pero todo se sabe.
- **Checkpoints de diseno** — cosas que hoy funcionan del lado del cliente y algun dia
  tienen que moverse al servidor (o equivalente). Deuda conocida y aceptada.
- **Pendientes de codigo** — mejoras chicas identificadas y no urgentes.
- **Bugs resueltos** — tabla `Bug | Fix`. Es la seccion que mas se consulta: cuando algo
  vuelve a fallar parecido, el fix anterior suele ser la pista.
