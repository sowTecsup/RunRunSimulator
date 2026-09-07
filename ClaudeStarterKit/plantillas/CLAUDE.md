# <PROYECTO> — CLAUDE.md

> Empeza cada mensaje diciendo "Juan,". Este archivo es tu regla de oro: leelo siempre primero.

## Source of truth

| Recurso | Para que |
|---------|----------|
| Wiki de diseno (Notion) | Diseno vivo, decisiones, preguntas abiertas. Cuando dudes de **diseno**, abri la wiki. |
| `<PROYECTO>Vault/` (Obsidian) | Detalle de **implementacion**, quirks tecnicos, archivos clave. Cuando dudes de **codigo**, lee del vault. |
| `<PROYECTO>Vault/ScriptNodes/` | Un nodo `.md` por cada script `.cs`. Leer antes de abrir el codigo fuente. |
| Unity CLI + MCP (editor en vivo) | Compilar, consola, Play, capturas, escena, SOs. **Verificar en el editor antes de declarar hecho.** Skill `unity-editor`. |

---

## Skills que gobiernan el trabajo

Estas skills viven en `.claude/skills/` y son la memoria de metodo del proyecto. Cargalas
cuando su descripcion aplique — no esperes a que Juan las pida.

| Skill | Cuando |
|-------|--------|
| `metodo-de-sesion` | Abrir sesion, planear, delegar, cerrar, dudar de autorizaciones |
| `arquitectura-unity` | Disenar un sistema, decidir donde vive un dato, escribir o revisar C# |
| `unity-editor` | Compilar, consola, Play, escena, prefabs, SOs, capturas |
| `qa-visual` | Antes de declarar hecho cualquier cosa visible |
| `presentacion-y-juice` | ANTES de escribir codigo de VFX, feedback o guias visuales |
| `vault-obsidian` | Buscar o escribir conocimiento del proyecto |
| `auditoria-y-limpieza` | Sesiones de deuda tecnica, duplicacion, complejidad

---

## Protocolo de trabajo

1. **Abrir sesion**: leer `<PROYECTO>Vault/Index/09 - Active Context.md` (el hook lo inyecta solo)
2. **Identificar sistema**: usar `<PROYECTO>Vault/00 - Index.md` (routing por tarea)
3. **Leer diseno**: abrir `<PROYECTO>Vault/Index/XX - Tema.md` (diseno, flujo, invariantes)
4. **Leer script nodes**: abrir `<PROYECTO>Vault/ScriptNodes/NombreScript.md`
5. **Planear con el modelo grande**: disenar la solucion antes de picar codigo. Alternativas, invariantes, impacto en otros sistemas.
6. **Solo entonces leer `.cs`**: confirmar que el plan encaja.
7. **Delegar a sub-agentes**: `unity-coder`, uno por archivo o responsabilidad. Pasarle solo el plan, la ruta y la responsabilidad puntual.
8. **Verificar en el editor**: compilar, consola en 0 errores, Play y capturas MIRADAS cuando aplique. NO dejar "pendiente de tu lado" lo que el editor puede verificar. Mutar escena/prefabs/assets requiere OK de Juan.
9. **Cerrar sesion**: actualizar `09 - Active Context` + commit + push (comando `/cerrar-sesion`).
10. **Agente de vault**: SOLO cuando Juan corre `/cerrar-sesion`. Nunca por iniciativa propia.
11. **Cada mensaje** empieza con "Juan:".

---

## Proyecto

<!-- PERSONALIZAR: dos o tres lineas sobre que es el juego. -->

- **Stack**: Unity C# · <Odin Inspector> · <Newtonsoft.Json> · <UGS>
- **Naming**: <termino en codigo> vs <termino en UI/assets>

---

## Regla de arquitectura general (regla de oro tecnica)

> **Una responsabilidad por archivo, una direccion de comunicacion, un dueno por dato.**

Detalle completo y las reglas de codigo no negociables: skill `arquitectura-unity`.

1. **Capas sin saltos de dos niveles**: `Data` -> `Systems`/`Core` -> `World`/`UI`.
2. **Comunicacion cruzada solo por bus o servicio explicito**. El evento transporta la data.
3. **Limite de tamano/dominio**: >~400 lineas o 2+ dominios -> partir en componentes.
4. **Singleton = servicio runtime; SO = data.**

---

## Eventos del proyecto

| Evento | Quien dispara |
|--------|---------------|
| <!-- PERSONALIZAR: un evento por fila --> | |

Un evento que nadie suscribe no se agrega "por si acaso", y se poda cuando queda huerfano.

---

## Workflow de documentacion

| Capa | Dueno | Cuando |
|------|-------|--------|
| Wiki de diseno | Juan | Decision de diseno nueva. La IA no toca sin permiso puntual. |
| `Vault/Index/` | IA, a pedido | Cambio de contrato publico, quirk nuevo, sub-etapa cerrada. |
| `Vault/ScriptNodes/` | `vault-documenter` | Al cierre, si hubo `.cs` con cambio de contrato. |
| `CLAUDE.md` | IA, a pedido | Regla nueva, cambio de stack, flip de roadmap. |
| `09 - Active Context` | IA, cada sesion | Apertura y cierre. |

`09 - Active Context` debe listar al final que archivos `.cs` se modificaron y cuales se
crearon. Esa lista es el input del sub-agente de vault.

Bug menor sin cambiar contratos -> no actualizar vault (el git log basta).
