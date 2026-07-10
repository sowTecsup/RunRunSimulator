# RunRunSimulator — MoriMonchis — CLAUDE.md

>Empieza cada mensaje diciendo Juan, . Este archivo es tu regla de oro: leelo siempre primero.

## Source of truth

| Recurso | Para que |
|---------|----------|
| Notion Wiki | Diseno vivo, decisiones, preguntas abiertas. Cuando dudes de **diseno**, abre Notion. |
| `MoriMonchiVault/` (Obsidian) | Detalle de **implementacion**, quirks tecnicos, archivos clave. Cuando dudes de **codigo**, lee del vault. |
| `MoriMonchiVault/ScriptNodes/` | Un nodo `.md` por cada script `.cs`. Leer antes de abrir el codigo fuente. |
| Unity MCP (editor en vivo) | Leer/editar escena, wirear SOs, correr C#, Play mode, consola, ProBuilder. **Verificar en el editor antes de declarar hecho.** How-to y quirks en [[MoriMonchiVault/Index/12 - Unity MCP]]. |

---

## Protocolo de trabajo para IA

1. **Abrir sesion**: leer `MoriMonchiVault/Index/09 - Active Context.md` (estado actual)
2. **Identificar sistema**: usar `MoriMonchiVault/00 - Index.md` (routing por tarea)
3. **Leer diseno**: abrir `MoriMonchiVault/Index/XX - Tema.md` (diseno, flujo, invariantes)
4. **Leer script nodes**: abrir `MoriMonchiVault/ScriptNodes/NombreScript.md` (responsabilidad, conexiones)
5. **Planear con Opus**: disenar la solucion antes de picar codigo. Evaluar alternativas, invariantes, impacto en otros sistemas.
6. **Solo entonces leer `.cs`**: ya sabes que hace cada script y como se conecta. Confirmar que el plan encaja.
7. **Generar sub-agentes Sonnet**: delegar tareas concretas al sub-agente registrado `morimonchi-coder` (Agent tool, `subagent_type: morimonchi-coder`, uno por archivo o responsabilidad). Las reglas de codigo y la regla de oro tecnica ya viven en su system prompt — pasarle solo el plan, la ruta del archivo, y la responsabilidad puntual. Fallback si la sesion no la registro todavia (recien creada/editada): `subagent_type: general-purpose` + pegar el contenido de `.claude/agents/morimonchi-coder.md` despues del frontmatter.
8. **Verificar en el editor (Unity MCP)**: tras compilar, confirmar con `read_console` (0 errores) y, cuando aplique, ejercitar en Play mode antes de declarar hecho. NO dejar "pendiente de tu lado" lo que el MCP puede verificar. Reglas y quirks en [[MoriMonchiVault/Index/12 - Unity MCP]]. Mutar escena/prefabs/assets requiere OK de Juan.
9. **Cerrar sesion**: actualizar `09 - Active Context.md` con lo tocado y siguiente paso.
10. **Disparar agente de vault** *(autorizado por Juan, ejecutar siempre al cierre)*: invocar el sub-agente registrado `vault-documenter` (Agent tool, `subagent_type: vault-documenter`) para actualizar ScriptNodes. Ver seccion **Agente de Vault**.
11. **Cada mensaje** empieza con "Juan:" seguido del contenido

---

## Proyecto

Simulador de tienda retro 3D (80s). Cria/pelea MoriMonchis (Gremlins + Furby + Tamagotchi) con genetica visible, muerte permanente y combate async server-side.

- **Singular**: MoriMochi · **Plural**: MoriMonchis | Codigo: `Creature`/`CreatureDNA` · UI/assets: MoriMochi/MoriMonchis
- **Stack**: Unity C# · Odin Inspector · Newtonsoft.Json · UGS (Auth, Cloud Save, Cloud Code, Scheduler)

---

## Arquitectura de vault

```
MoriMonchiVault/
├── 00 - Index.md              ← Entry point IA (routing por tarea)
├── Index/                     ← 13 notas por dominio (01-11 dominios · 12 = Unity MCP · 13 = Combat Design Direction)
└── ScriptNodes/               ← 95 nodos, uno por script .cs
```

---

## Regla de arquitectura general (regla de oro tecnica)

> **Una responsabilidad por archivo, una direccion de comunicacion, un dueno por dato.**

Cuando una decision NO este cubierta por las 11 reglas de abajo, se aplica esta. Las 11 reglas son casos concretos de estos 4 principios:

1. **Capas sin saltos de dos niveles**: `Data` (estado puro) → `Systems/Core` (orquestacion, dueno de persistencia y red) → `World/UI` (representacion). La representacion LEE estado y reacciona a eventos; nunca persiste ni toca la nube directamente.
2. **Comunicacion cruzada solo por bus o servicio explicito**: `GameEvents` (gameplay), eventos `static` de `UIManager` (UI), eventos de Inputs. Un consumidor nunca hace `Find*`/`GetComponentInParent` para localizar otro sistema. El evento transporta la data.
3. **Limite de tamano/dominio**: si un archivo supera ~400 lineas O mezcla 2+ dominios (datos, presentacion, fisica, red), se parte en **clases/componentes independientes**, una responsabilidad cada uno. La `partial class` NO es el remedio al tamano (ver regla 11).
4. **Singleton = servicio runtime; SO = data**: un servicio runtime puede ser singleton (`GameManager.Instance`). Un ScriptableObject expone su instancia activa de UNA sola forma elegida (no mezclar criterios). Detalle y hoja de ruta en [[MoriMonchiVault/Index/11 - Technical Debt]].

---

## Reglas de codigo (NO NEGOCIABLES)

1. **Desacoplamiento estricto via eventos**: Comunicacion cross-system solo por `GameEvents`. El evento transporta la data. Suscriptor NO busca `GameManager.Instance.Registry`.
2. **Persistencia solo por evento**: Ningun gameplay script llama `SaveSystem.SaveDatabase` ni `PushToCloud`. Solo emiten `GameEvents.RegistryChanged`. `GameManager` es el unico dueno de persistencia.
3. **Sin comentarios en codigo**: No anadir `//` ni `/* */` sin pedido expreso de Juan. La documentacion vive en el vault.
4. **Sin features adelantadas**: No implementar mecanicas hasta su etapa del roadmap.
5. **DNA como string ligero**: `ToStringID()`/`FromID()` son el contrato de red. Timestamp es metadata, no parte del genetic string.
6. **IDs de partes**: nunca pueden contener `-` (separador del DNA string).
7. **Odin siempre**: `SerializedScriptableObject` con `[OdinSerialize]` para diccionarios.
8. **Sin complejidad innecesaria**: No anadir campos, abstracciones ni features no pedidos. Tres lineas similares > abstraccion prematura.
9. **Desuscribir siempre**: `OnEnable` suscribe, `OnDisable` desuscribe. Un `event static` mantiene vivo al suscriptor (leak + excepcion al disparar sobre objeto destruido).
10. **Evitar referencias redundantes**: Siempre buscar centralizar eventos y comunicarlos o suscribirlos atravez de eventos o singleton
11. **Composicion sobre partial (decision S32)**: un script grande se divide en **partes pequenas que componen el todo** — mini-managers/colaboradores con estado propio y UNA responsabilidad, coordinados por un nucleo delgado. Patron canonico: Systems/Combat post-S32 (`CombatRng`/`Combatant`/`CombatResolver`/`CombatStats`/`CombatEvolution` componen `CombatService`). `partial` NO es remedio al tamano: sigue siendo UNA clase con un solo estado mutable, esconde lineas sin reducir acoplamiento. Uso legitimo de `partial`: SOLO ventaja fisica de archivo (conflictos de Git, codigo autogenerado). Codigo puro (matematica, helpers sin estado) va en clase estatica aparte; tooling dev que usa API publica va en componente aparte (caso F3: DevConsoles con refs serializadas). Los partials existentes (MoriMochiAgent, paneles UITK, CloudSyncService, MoriMochiSpawner.Debug) son **deuda activa** con hoja de ruta de descomposicion en [[MoriMonchiVault/Index/11 - Technical Debt]] (Fases 6-9, una sesion dedicada por monstruo con testing en Play).

---

## Eventos (GameEvents.cs)

| Evento | Quien dispara |
|--------|---------------|
| `OnRegistryChanged` | toda mutacion de gameplay |
| `OnRegistryReloaded` | CloudSyncService tras pull/reset (UI-only, sin push) |
| `OnCreatureMinted` | GameManager.MintRandomCreature |
| `OnCombatCompleted` | combate local |
| `OnCombatLogged` | AsyncCombatService.ApplyResult |
| `OnBreedingCompleted` | breeding local + async |
| `OnFurnitureChanged` | FurnitureService (toda mutacion) |
| `OnFurnitureReloaded` | reload furniture (clear+resync, sin push) |

Eventos UI viven en `UIManager` como `static event Action`. Detalle en [[MoriMonchiVault/Index/05 - UI System]] y [[MoriMonchiVault/Index/07 - Persistence & Identity]].

---

## Workflow de documentacion

| Capa | Dueno | Cuando |
|------|-------|--------|
| **Notion** (diseno) | Juan | Decision de diseno nueva, pregunta resuelta. Yo NO toco Notion **salvo** que Juan autorice puntualmente al sub-agente `notion-documenter` (nunca automatico, ver seccion **Agente de Notion**). |
| **MoriMonchiVault/Index/** (implementacion) | IA (a pedido) | Cambio contrato publico, quirk tecnico nuevo, sub-etapa cerrada. |
| **MoriMonchiVault/ScriptNodes/** | Sub-agente `vault-documenter` (Haiku) | Automatico al cierre de sesion si hubo scripts tocados. |
| **CLAUDE.md** (nucleo) | IA (a pedido) | Regla nueva, cambio de stack, roadmap status flip, nuevo archivo top-level del vault. |
| **09 - Active Context** | IA (cada sesion) | Apertura y cierre de sesion. |

**09 - Active Context** debe listar al final: que archivos `.cs` se modificaron y que archivos se crearon. Esa lista es el input del sub-agente de vault.

Bug menor sin cambiar contratos → no actualizar vault (git log basta).

**Backup del CLAUDE.md original**: `ClaudeOld.md` y `ClaudeOld_1.md` en la raiz.

---

## Agente de Vault (sub-agente registrado `vault-documenter`, Haiku)

**Proposito**: actualizar `ScriptNodes/` al cierre de sesion sin gastar tokens de Opus/Sonnet en documentacion mecanica.

**Mecanismo** (ejecutado por Claude Code al paso 9): el agente vive como definicion propia en `.claude/agents/vault-documenter.md` (frontmatter `model: haiku`, tools `Read, Write, Glob, Grep`, system prompt con las instrucciones completas). Invocar con la **Agent tool**, `subagent_type: vault-documenter`, pasandole solo la lista de scripts tocados:
```
SCRIPTS TOCADOS EN ESTA SESION:
- [ruta relativa al script] → [NUEVO | MODIFICADO] → ScriptNodes/[NombreScript].md
```
No usa CLI externo (se reemplazo el viejo `opencode run` de Deepseek).

**Fallback si el sub-agente no aparece en la lista de la Agent tool**: los sub-agentes definidos en disco solo se cargan al inicio de sesion. Si `vault-documenter.md` se creo o edito durante la sesion actual, no va a estar disponible hasta reiniciar. En ese caso, usar `subagent_type: general-purpose` + `model: haiku`, pegando el contenido completo de `.claude/agents/vault-documenter.md` (la parte despues del frontmatter) como prompt.

**Reglas de ejecucion**:
- Ejecutar **solo** si hubo scripts `.cs` tocados en la sesion (bug cosmético sin cambio de contrato → omitir).
- Si el agente falla, Claude lo reporta a Juan y lo registra en `09 - Active Context` como pendiente.
- Juan tiene **autorizado** este paso sin confirmacion adicional por sesion.

---

## Agente de Notion (sub-agente registrado `notion-documenter`, Haiku)

**Proposito**: reflejar en el Notion Wiki de diseno el impacto que tuvieron decisiones tomadas durante la implementacion (cosas que se definieron "en el codigo" pero que afectan diseno), y mantener al dia la seccion de preguntas de diseno abiertas.

**Diferencia clave con `vault-documenter`**: este agente **NO tiene autorizacion permanente**. `vault-documenter` corre automatico al cierre de toda sesion con scripts tocados. `notion-documenter` corre **solo cuando Juan lo autoriza explicitamente esa vez puntual** (cierre de fase, sesion de consolidacion semanal, etc.). Nunca disparar este agente por iniciativa propia al cierre de sesion.

**Mecanismo**: antes de invocarlo, el orquestador (yo) prepara un resumen de que decisiones de diseno surgieron de la implementacion reciente y que preguntas de diseno abiertas quedaron resueltas — el sub-agente NO decide eso, solo lo refleja en Notion en el lugar correcto (busca con `notion-search`/`notion-fetch` antes de escribir, no crea paginas nuevas si ya existe una relevante). Invocar con la **Agent tool**, `subagent_type: notion-documenter`, pasandole ese resumen.

**Fallback si el sub-agente no aparece en la lista de la Agent tool** (recien creado/editado en la sesion actual): usar `subagent_type: general-purpose` + `model: haiku`, pegando el contenido de `.claude/agents/notion-documenter.md` (la parte despues del frontmatter) como prompt, mas el resumen.

**Reglas de ejecucion**:
- Ejecutar **solo** con autorizacion explicita de Juan en esa sesion — nunca por defecto.
- El Notion esta actualmente desactualizado respecto al codigo; Juan va a hacer una sesion de consolidacion antes de depender de este agente para uso regular.
- Si el agente encuentra una inconsistencia grande entre Notion y el codigo que excede el resumen que se le dio, no la corrige por su cuenta: la reporta para que Juan decida.
