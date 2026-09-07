# Claude Starter Kit — el metodo, portable

Sintesis de ~105 sesiones de trabajo entre Juan y Claude sobre un proyecto Unity.
El objetivo: que un proyecto nuevo **no empiece de cero**, sino con todo lo aprendido —
arquitectura, preferencias de codigo, pipeline del editor, ritual de verificacion y flujo
de sesion.

Nada de esto habla del contenido de MoriMonchis. Todo lo que quedo aca es **metodo**.

---

## Que hay adentro

```
ClaudeStarterKit/
├── .claude/
│   ├── README.md         <- hook vs comando vs skill: que dispara que, y por que
│   ├── skills/           <- LA PIEZA CENTRAL: 7 skills con el metodo destilado
│   │   ├── metodo-de-sesion/       protocolo, delegacion, autorizaciones, harness, dos equipos
│   │   ├── arquitectura-unity/     regla de oro, composicion, cascada de SOs, singletons
│   │   ├── unity-editor/           CLI vs MCP, grupos de tools, quirks, esperas largas
│   │   ├── qa-visual/              test del texto plano, legibilidad, capturas MIRADAS
│   │   ├── presentacion-y-juice/   MMFeedbacks obligatorio, vara Shapes, quirks de Feel
│   │   ├── vault-obsidian/         estructura del vault, nodos, quien escribe que
│   │   └── auditoria-y-limpieza/   sesiones de deuda: metricas, priorizacion, ejecucion
│   ├── agents/
│   │   ├── unity-coder.md          (sonnet) implementa un plan aprobado, no disena
│   │   ├── vault-documenter.md     (haiku)  actualiza ScriptNodes al cierre
│   │   └── notion-documenter.md    (haiku)  refleja diseno en la wiki, solo con permiso
│   ├── commands/
│   │   ├── abrir-sesion.md         apertura guiada, plan-first
│   │   └── cerrar-sesion.md        Active Context + vault-documenter + commit + push
│   ├── hooks/session-start.ps1     inyecta el Active Context al abrir (automatico)
│   └── settings.example.json       registro del hook + permisos base
├── plantillas/
│   ├── CLAUDE.md                   regla de oro del proyecto nuevo, con placeholders
│   └── vault/                      00 - Index, 08 - Known Bugs, 09 - Active Context,
│                                   11 - Technical Debt, plantilla de ScriptNode
└── memorias/                       15 memorias portables (feedback + tooling) + indice
```

---

## Como arrancar un proyecto nuevo (10 minutos)

### 1. Copiar la carpeta `.claude/` a la raiz del repo nuevo

```
<repo-nuevo>/.claude/{skills,agents,commands,hooks}
```

Las skills funcionan tal cual, sin editar. Los agentes y comandos tienen marcas
`<PROYECTO>` y bloques `PERSONALIZAR` que hay que completar.

### 2. Registrar el hook

Copiar `settings.example.json` a `.claude/settings.local.json` y reemplazar
`C:\RUTA\AL\PROYECTO` por la ruta real (los backslashes van dobles dentro del JSON).
Editar tambien la ruta del vault dentro de `hooks/session-start.ps1`.

> El `.ps1` debe quedar en **ASCII puro**. Windows PowerShell 5.1 lee los scripts como ANSI
> y se rompe con acentos o em-dash.

### 3. Crear el `CLAUDE.md` del proyecto

Copiar `plantillas/CLAUDE.md` a la raiz y completar: nombre, stack, descripcion en dos
lineas, tabla de eventos, y reglas propias del proyecto (contratos de datos, naming).

### 4. Crear el vault

```
<repo-nuevo>/<PROYECTO>Vault/
├── 00 - Index.md                <- de plantillas/vault/
├── Index/09 - Active Context.md <- de plantillas/vault/Index/
└── ScriptNodes/                 <- vacio; el _PLANTILLA.md define el formato
```

### 5. Sembrar las memorias

Copiar los `.md` de `memorias/` a:

```
C:\Users\<USUARIO>\.claude\projects\<slug-del-proyecto-nuevo>\memory\
```

El slug lo genera Claude Code a partir de la ruta del proyecto (formato:
`C--Users-...-NombreProyecto`). Si la carpeta ya existe, pegar las lineas de
`memorias/MEMORY.md` dentro del `MEMORY.md` que ya este ahi en vez de pisarlo.

### 6. Instalar el editor en vivo

- Unity CLI: `winget install Unity.CLI`, despues `unity pipeline install` con el editor
  abierto. Verificar con `unity status`.
- MCP de editor: segun el paquete que se use.
- **Activar en el panel del editor los grupos de tools que estan ocultos por default**
  (`scripting_ext`, `testing`, `probuilder`, `ui`, `docs`, `profiling`). Sin
  `scripting_ext` no hay pipeline de ScriptableObjects Odin. La activacion por comando es
  efimera; la del panel persiste.

Ambos conviven. La matriz de que va por donde esta en la skill `unity-editor`.

### 7. Reiniciar Claude Code

Skills, agentes, comandos y hooks se cargan **al iniciar sesion**. Nada de lo que se cree
durante una sesion aparece hasta reiniciar.

---

## Que NO viajo (y por que)

- Diseno, lore y sistemas de MoriMonchis: son del proyecto, no del metodo.
- Reglas de codigo especificas del dominio (el formato del string de ADN, los IDs sin
  guion): quedaron generalizadas a "los contratos de serializacion son sagrados y los
  separadores no pueden aparecer dentro de los campos".
- El historial de sesiones. Lo que sobrevivio son los **quirks** que ese historial dejo.

---

## El resumen de todo en cinco lineas

1. **Leer diseno y nodos antes que codigo.** El plan primero, la confirmacion despues.
2. **Pensar en grande, escribir en chico.** El orquestador disena; los sub-agentes escriben
   una responsabilidad cada uno.
3. **Verificar en el editor antes de decir "listo".** Y si hay algo visible, con capturas
   miradas.
4. **Una responsabilidad por archivo, una direccion de comunicacion, un dueno por dato.**
5. **Leer es libre; mutar el mundo de Juan pide permiso** — escena, prefabs, vault, wiki.
