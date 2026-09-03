---
tags: [index, tooling, mcp]
---

# 12 - Unity MCP (editor en vivo)

**Qué es:** un servidor MCP ("MCP for Unity") que da a la IA acceso al Editor de Unity EN VIVO: leer/editar escenas y jerarquía, crear/wirear ScriptableObjects, correr C# arbitrario en el editor, manejar Play mode, leer consola, ProBuilder, tests, etc. Descubierto y validado en la sesión del 2026-07-09.

**Por qué importa (cambia el workflow):** cierra los tres agujeros crónicos del proyecto — "código hecho, sin probar en Play", "wiring de Unity asumido hecho" y "no sé si compiló tras delegar". Ahora la IA **verifica en el editor** antes de declarar algo hecho, en vez de dejarlo "pendiente de tu lado".

---

## Regla de oro del MCP

> **El MCP es herramienta de VERIFICACIÓN y SETUP, no un atajo de arquitectura.** El código sigue yendo por `morimonchi-coder`, la doc por el vault, la persistencia solo por `GameManager`. Las 11 reglas y la regla de oro técnica mandan igual. Y no se modifica la escena/prefabs/assets por iniciativa propia: se explora y verifica libre, pero mutar necesita OK de Juan (igual que Notion).

---

## Precondición SIEMPRE: fijar la instancia

Hay **dos proyectos Unity** que se conectan al mismo servidor (`RunRunSimulator` y `FasterTheBetter`). Si no se fija instancia, el server tira error por ambigüedad.

- Listar instancias: recurso `mcpforunity://instances` (devuelve `Name@hash`).
- **Pasar `unity_instance: "RunRunSimulator@<hash>"` en CADA llamada** (o `set_active_instance` una vez por sesión). El hash cambia entre reinicios del editor — re-listar al abrir sesión.

---

## Mapa de herramientas (lo usado y probado)

| Necesidad | Tool / recurso | Nota |
|-----------|----------------|------|
| Estado de escena | `manage_scene` (get_active/get_hierarchy/get_build_settings) | `get_hierarchy` pagina hijos por `childrenCursor`; para árbol profundo conviene `execute_code` que recorre transforms |
| Buscar objetos | `find_gameobjects` (by_name/tag/layer/component/path) | devuelve instanceIDs; inactivos NO salen por `GameObject.Find` |
| Leer componentes | recurso `mcpforunity://scene/gameobject/{id}/components` | read-only |
| Editar GO | `manage_gameobject` (create/modify/delete/duplicate) | reparent, primitivas, prefabs |
| Editar componentes | `manage_components` (add/remove/set_property) | refs por `{guid}` o instanceID |
| SO / databases | `manage_scriptable_object` (create/modify) | **OJO Odin, ver abajo** |
| C# en vivo | `execute_code` | la navaja suiza; ver abajo |
| Consola | `read_console` (get/clear, filtros por tipo) | correr SIEMPRE tras editar |
| Play mode | `manage_editor` (play/pause/stop) | permite cerrar el loop de testeo |
| Modelado | `manage_probuilder` | primitivas + poly shapes + edición de malla |
| Assets | `manage_asset` (import/create/move/delete/search) | |
| Tests | `run_tests` | EditMode/PlayMode |

---

## ⚠️ Quirk crítico #1 — Odin y los ScriptableObjects

Todas las databases del proyecto son `SerializedScriptableObject` (Odin) y su data pesada vive en blobs `[OdinSerialize]` (diccionarios, listas polimórficas). El tool `manage_scriptable_object` trabaja con **property paths de `SerializedObject` nativo de Unity**, que **NO ven el blob Odin**.

**Lo que SÍ hace `manage_scriptable_object` (confiable):**
- `create` el `.asset` (`type_name: MoriMonchiSimulator.<Tipo>`).
- Setear campos `public` planos (Unity-nativos): strings, enums, colores, floats.
- Wirear referencias por GUID (Sprites, y refs planas de orquestadores como `CreatureDatabaseSO.BodyShapes/Arms/Eyes/Mouths`).

**Lo que NO puede tocar (blob Odin invisible):**
- Meter entradas en `[OdinSerialize] Dictionary<...>` (ej. `EquipmentDatabaseSO.equipment`, `PartDatabaseSO`, `SynergyTableSO.Rules`).
- Listas polimórficas `[OdinSerialize]` (ej. `EquipmentSO.Effects`).

**Cómo SÍ se escriben los diccionarios Odin:** `execute_code` llamando la API C# real —
```csharp
db.Equipment["EQ0"] = so;
UnityEditor.EditorUtility.SetDirty(db);
UnityEditor.AssetDatabase.SaveAssets();
```
o disparando los botones que ya existen en el SO (`PopulateFromBuffer()`, `SyncAllIDs()`). Bypassa el problema porque usa la API que Odin entiende. **Verificado** (smoke test 2026-07-09): la entrada sobrevive un `AssetDatabase.ImportAsset(path, ForceUpdate)`, que fuerza a Odin a re-deserializar desde el blob — prueba de que persistió de verdad y no solo a la capa nativa.

**Pipeline para "levantar settings" desde cero:** `create` cada SO (campos planos) → `execute_code` para insertarlos en el diccionario Odin + `SyncAllIDs()` → `manage_scriptable_object modify` para wirear la database al `GameManager` (ref plana) → `read_console`.

---

## ⚠️ Quirk crítico #2 — `execute_code`

- **`safety_checks` bloquea patrones** (`AssetDatabase.DeleteAsset`, `File.Delete`, `Process.Start`, loops infinitos, `DestroyImmediate` a veces). Para operaciones deliberadas (limpieza de assets de descarte, borrar GO de test) pasar `safety_checks: false`.
- **Compilador cae a `codedom` (C# 6)** porque el assembly Roslyn del proyecto no carga (`Microsoft.CodeAnalysis.dll will not be loaded due to errors` — warning pre-existente en consola). Consecuencia: en `execute_code` **usar solo C# 6** — nada de tuples con nombre, switch expressions, interpolación con `$"{x,n}"` compleja, pattern matching moderno. Forzar `compiler: roslyn` falla mientras el assembly esté roto.
- El código corre como cuerpo de método con `UnityEngine`/`UnityEditor` disponibles; usar nombres totalmente calificados (`UnityEditor.AssetDatabase`, `MoriMonchiSimulator.<Tipo>`). `return` manda data de vuelta.

---

## Editar la jerarquía con seguridad (patrón probado)

- **Envolver en Undo** para que TODO revierta con un Ctrl+Z: `Undo.IncrementCurrentGroup()` + `Undo.SetCurrentGroupName(...)` → `Undo.RegisterCreatedObjectUndo` (grupos nuevos), `Undo.SetTransformParent(child, parent, ...)` (reparenta **preservando posición mundial**), `Undo.RecordObject` (rename), `Undo.DestroyObjectImmediate` (borrar) → `Undo.CollapseUndoOperations(grp)`.
- **GOTCHA cazado:** NO reutilizar una lista cacheada de `GetRootGameObjects()` después de destruir un objeto — la referencia muerta tira `MissingReferenceException` al leer `.name`. Borrar al final, o re-consultar la lista.
- Cerrar con `EditorSceneManager.MarkSceneDirty` + `manage_scene save` + `read_console`.
- **En este proyecto renombrar/reparentar es seguro:** no hay `GameObject.Find`-por-nombre en el código; todo se resuelve por referencia serializada, `static Instance`, tag, o auto-registro (`AnchorRegistry`). Única búsqueda por identidad textual: `FindGameObjectWithTag("Player")` en `SpawnBallistics`.

---

## ProBuilder (`manage_probuilder`)

Puede crear **formas complejas** (no solo primitivas): `create_poly_shape` desde footprint 2D + `extrudeHeight`, más `create_shape` (Cube/Cylinder/Sphere/Cone/Torus/Pipe/Arch/Stair/CurvedStair/Door/Prism) y edición de malla (extrude/bevel/subdivide/bridge/merge/weld/mover vértices…). **Verificado**: footprint en L de 6 puntos → 8 caras, 36 vértices.

**Caveat de uso:** las ops de edición sobre caras/aristas concretas piden **índices explícitos** (`faceIndices`/`edgeIndices`) — inspeccionar la malla primero para obtenerlos (un `subdivide` sin selección no cambia nada).

---

## Unity CLI oficial — ADOPTADO COMO COMPLEMENTO (instalado S89, verificado S90)

Historia corta: evaluado en S79 (veredicto: no migrar — bug de Play mode, ~16x más lento, sin ProBuilder/SOs Odin), el bug crítico se arregló a la semana (S84) y en S89 Juan lo instaló: `winget install Unity.CLI` → `unity` **v1.0.0-beta.6** + paquete **`com.unity.pipeline` 0.5.0-exp.1** en el proyecto (via `unity pipeline install` con el editor abierto). En S90 se ejercitó end-to-end y quedó adoptado como **complemento permanente** del MCP de CoplayDev. El stack del editor ahora es DUAL: los dos servers viven en el mismo editor sin conflicto (verificado S90: CLI por su puerto pipeline, CoplayDev por el suyo en 8080).

### Cómo se usa (sintaxis verificada S90)

- `unity status` — health-check: puerto, estado, proyecto, versión, PID. **Nuevo primer paso al abrir sesión** (más barato que el ritual de instancias del MCP).
- `unity list` — catálogo (**143 tools** built-in). Con filtros: `unity command --query <term> --detail full` muestra los **nombres exactos de parámetros**.
- `unity command <tool> --param value [--json]` — ejecutar. ⚠️ Los parámetros van SIEMPRE como flags `--key value` (NO `key=value`, que se interpreta como valor literal). `--json` da salida estructurada estable; el payload útil viene en `data.<tool>.result`.
- `unity command <tool> ... --detach` → job id; después `unity job status <id>` / `unity job wait <id>` / `unity job list`.
- `--timeout <segundos>` por comando (default 30).

### Verificado en vivo (S90)

| Capacidad | Resultado |
|-----------|-----------|
| `editor_status` | estructurado: status/compiling/domainReload/playMode/heartbeat |
| `eval` / `eval_file --file` | ✅ **C# moderno** (switch expressions, tuples) — **mata el quirk #2 (C# 6) para evaluación de código**. ~2s por llamada. El warning *duplicate assembly Microsoft.CodeAnalysis.CSharp.dll* (S89) NO lo rompió. `eval_file` puede leer archivos de CUALQUIER ruta (no confinado) |
| `console --tail N` | estructurada (seq/timestamp/level/stackTrace), con follow por cursor |
| `recompile` / `recompile_status` | funciona con el editor desenfocado (dolor S81); + `set_autotick` para tick en background |
| `editor_play` / `editor_stop` | ✅ y — LO IMPORTANTE — **el CLI SOBREVIVE al domain reload de Play**: el server pipeline re-spawnea en otro puerto (7800→7801 observado) y el CLI lo re-descubre solo. Donde el bridge CoplayDev muere (quirk S81 #1), el CLI sigue |
| `capture_game_view --source screen --save_path <ruta>` | ✅ **captura el backbuffer compuesto CON overlay UITK** (solo en Play) — **mata el quirk S88** de capturas sin HUD. ⚠️ `save_path` confinado a la raíz del proyecto (usar `Assets/Screenshots/`). `--source camera` (default) funciona fuera de Play pero pierde overlay |
| `list_tests` / `run_tests` | ✅ Test Runner real por primera vez. Hoy lista 1 solo test (stub de Addressables): **el proyecto no tiene tests propios** — el asmdef de EditMode tests para la lógica pura del prototipo (ActionResolver/AbilityTargeting/CombatEffects) es la pieza que falta |
| `--detach` + `unity job status` | ✅ job queued → completed |

No probado aún (existe en catálogo): `build`/`build_status` con BuildReport, Project Auditor (`audit`), Unity Search (`search`), `manage`-familia de animation/timeline, bakes (lighting/navmesh/occlusion), `save_prefab_contents` (prefabs aislados nested-safe), hot reload (`reload_file`).

### Matriz de decisión: qué va por dónde

| Tarea | Herramienta | Por qué |
|-------|-------------|---------|
| Health-check al abrir sesión | **CLI** `unity status` | 1 comando, sin fijar instancia |
| Compilar y verificar consola tras editar | **CLI** `recompile` → `recompile_status` → `console --tail` | funciona desenfocado; salida estructurada; sobrevive reloads |
| QA visual con HUD | **CLI** `capture_game_view --source screen` en Play | única vía confiable con overlay UITK (regla: capturas MIRADAS) |
| C# puntual en el editor | **CLI** `eval` / `eval_file` | C# moderno; `execute_code` MCP queda para cuando haga falta `safety_checks: false` |
| Play mode desatendido | **CLI** `editor_play/stop` + `set_autotick` | resiliencia al domain reload |
| Tests / builds / CI | **CLI** `run_tests` / `build` | nunca lo tuvo el bridge |
| Escena, GameObjects, componentes, wiring | **MCP CoplayDev** `manage_*` | catálogo mutador maduro + Undo |
| ScriptableObjects Odin (quirk #1) | **MCP CoplayDev** (`manage_scriptable_object` + `execute_code`) | pipeline validado; el CLI no lo cubre |
| ProBuilder / UI Toolkit picking / profiler | **MCP CoplayDev** | sin equivalente CLI |
| Tools propias del proyecto (hoy ninguna — las del prototipo se borraron en S93) | **MCP CoplayDev** | se declaran con `[McpForUnityTool]` en una carpeta `Editor/` (ver sección abajo) |
| Si el bridge CoplayDev muere a mitad de sesión | **CLI como red de seguridad** | antes: reiniciar editor; ahora: seguir por CLI y reiniciar cuando convenga |

### Workflow por fase de sesión (v1, a pulir con uso)

1. **Abrir**: `unity status` (ready?) → si se va a mutar escena/SOs, recién ahí fijar instancia MCP.
2. **Tras cada tanda de código**: `unity command recompile` → poll `recompile_status` → `console --tail 20` con 0 errores. (Reemplaza el patrón "refresh_unity + pausa 20s + read_console".)
3. **Verificación en Play**: `editor_play` → ejercitar (tools propias MCP para paridad/sims) → `capture_game_view --source screen` y MIRAR las capturas → `editor_stop`.
4. **Cierre**: cuando exista el asmdef de tests, `run_tests` como gate final.

### Quirks propios del CLI (S89-S90)

1. *Duplicate assembly `Microsoft.CodeAnalysis.CSharp.dll`* al cargar: choque entre `Assets/Plugins/Roslyn` (copia del proyecto) y la del paquete pipeline; Unity resuelve usando la nuestra. `eval` funcionó igual — si algún día falla raro, este es el sospechoso.
2. El primer `unity pipeline list` post-instalación puede dar "Server Reachable false" durante el refresh de assets (~99s). No es bug.
3. Rutas de ESCRITURA (`save_path`, `write_text_file`) confinadas a la raíz del proyecto; rutas de LECTURA (`eval_file --file`) no.
4. El puerto del server cambia tras domain reloads — nunca hardcodearlo; el CLI lo resuelve.
5. **(S93)** `unity status` con la tabla VACÍA = no hay ningún editor corriendo (no es un fallo del CLI). El editor se cerró a mitad de la sesión sin crash; relanzarlo con `Start-Process "C:\Program Files\Unity\Hub\Editor\6000.3.9f1\Editor\Unity.exe" -ArgumentList '-projectPath','"<ruta>"'` deja el pipeline `ready` en ~40 s. Antes de borrar una escena, comprobar cuál está abierta (`Library/LastSceneManagerSetup.txt` o MCP `execute_code` + `SceneManager.GetActiveScene()`), y cambiarla con `EditorSceneManager.OpenScene`.
6. **(S93)** `eval`/`eval_file` por CLI pueden fallar con *"Main thread operation timed out after 5000ms"* si el editor está desenfocado y no tickea; en la misma situación MCP `execute_code` respondió. Alternativas: `set_autotick`, o enfocar el editor. `AssetDatabase.Refresh()` vía `eval_file` sí funcionó tras el relanzamiento.
7. **(S93)** Con muchos agentes guardando archivos en paralelo (13 coders), Unity encadena domain reloads y puede loguear *"An infinite import loop has been detected"* sin listar assets: transitorio, desaparece con un refresh final. Compilar recién cuando todos terminaron.
8. **(S93)** El sandbox de PowerShell del harness bloquea comandos que contengan `Remove-Item` con expresiones raras (`-replace '\\','/'`) — para `git rm` masivo usar la herramienta Bash.
9. **(S95)** `eval`/`eval_file` **no aceptan directivas `using`** al tope (el código se inyecta dentro de un cuerpo de método: `using X;` se parsea como using-statement → "Identifier expected"). Escribir todo con nombres **totalmente calificados** (`UnityEngine.Object.FindFirstObjectByType<MoriMonchiSimulator.GameManager>()`, `UnityEditor.SceneManagement.EditorSceneManager.OpenScene(...)`). `return` al final funciona. El aviso "Unreachable code detected" en la última línea es cascada de otro error, no un problema real.
10. **(S95)** Los **backslashes dentro de strings** del `eval_file` rompen la compilación ("Unrecognized escape sequence"): el CLI des-escapa antes de compilar. Para regex/JSON armar el patrón sin `\`: `string q = ((char)34).ToString();` y `[ ]*` en vez de `\s*`.
11. **(S95)** El CLI está instalado en las **dos PCs** de Juan (winget, `1.0.0-beta.6`; tras instalar, refrescar `PATH` en la shell o llamar `%LOCALAPPDATA%\Microsoft\WindowsApps\unity.exe`). En la PC secundaria el editor abre el proyecto por la **junction** `C:\Users\USUARIO\Documents\GitHub\RunRunSimulator` → `E:\GitHub\RunRunSimulator`: `unity status` reporta la ruta C:, pero es el mismo repo (git y ediciones en E: se ven al instante).

**Alternativas descartadas (S79, sin cambios):** CLI cliente de CoplayDev · IvanMurzak/Unity-MCP.

Fuentes: [docs oficiales — Unity CLI](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli) · [Unity Pipeline package](https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package) · [análisis Vindler (bugs y benchmarks)](https://vindler.solutions/blog/unity-cli-agent-automation) · [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) · [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)

---

## ⚠️ Quirks S81 (sesión larga de código + Play desatendido)

1. **El bridge muere tras un domain reload largo y NO se auto-rearma.** Síntoma: `read_console` devuelve `no_unity_session` para siempre; en Editor.log aparece `"Server no longer running; ending orphaned session"` + `"Cannot verify connection: Bridge is not running"`. El server HTTP (127.0.0.1:8080) lo lanza EL PLUGIN del editor y muere con él; el harness de Claude Code es solo cliente HTTP. **Fix probado: reiniciar el editor Unity** (CloseMainWindow → esperar → relanzar `Unity.exe -projectPath`); al arrancar, el plugin relanza el server (~4-7 min tras el arranque del editor) y reconecta solo. Diagnóstico útil: `%LOCALAPPDATA%\UnityMCP\Logs\unity_mcp_server.log` (server) y `%LOCALAPPDATA%\Unity\Editor\Editor.log` (plugin, buscar "MCP-FOR-UNITY").
2. **`component_properties` del create de `manage_gameobject` falla SILENCIOSO con algunos campos** (pasó con un `[SerializeField]` de SO y con `Camera.orthographic`). Wirear siempre por `execute_code` + `SerializedObject.FindProperty(...).objectReferenceValue` + `ApplyModifiedProperties`, y verificar con una lectura posterior (contar nulls).
3. **Play desatendido**: activar `Application.runInBackground = true` por código al entrar (si el editor pierde foco, el player loop se pausa y las corrutinas se congelan) y vigilar `EditorApplication.isPaused` (puede quedar activa y congela todo con `deltaTime = 0` sin error alguno).
4. **Tras compilar, ESPERAR el reload completo antes de Play**: si se entra a Play con la compilación pendiente, Unity aplica el reload DENTRO de Play ("Recompile And Continue Playing") y borra todo el estado runtime no serializado (Awake no re-corre). Patrón: `refresh_unity` → pausa de ~20s → `read_console` limpio → recién entonces `play`. El prototipo de combate tolera esto vía getter lazy de `CombatBoardBuilder.Board` + `RestartEncounter()`.
5. **UITK: el `rootVisualElement` de un `UIDocument` puede recrearse en editor** y deja huérfano el árbol construido por código (los elementos viejos existen pero sin panel). Guarda estándar: `if (elemento == null || elemento.panel == null) BuildUi();` en cada Refresh/Show (aplicada en `CombatPrototypeHUD` y `EnemyBriefPanel`).
6. **El screenshot de `manage_camera` por cámara SÍ capturó el overlay UITK** en este proyecto (PanelSettings estándar) — útil para verificar HUD sin foco humano.

---

## Tools MCP propias del proyecto (S84)

El paquete de CoplayDev descubre por reflexión (`ToolDiscoveryService`: `TypeCache` + barrido de AppDomain) **cualquier clase estática marcada con `[McpForUnityTool]` en una assembly de Editor** — incluida `Assembly-CSharp-Editor`, o sea cualquier carpeta `Editor/` nuestra. `MCPForUnity.Editor.asmdef` tiene `autoReferenced: true`, así que no hace falta asmdef propio ni referencia manual.

**Contrato mínimo:**

```csharp
[McpForUnityTool("nombre_snake_case", Description = "que hace, para el LLM")]
public static class LoQueSea
{
    public class Parameters
    {
        [ToolParameter("descripcion", Required = false, DefaultValue = "false")]
        public bool flag { get; set; }
    }

    public static object HandleCommand(JObject @params)
    {
        return new SuccessResponse("mensaje", new { data = 1 });
    }
}
```

- La clase anidada `Parameters` es **opcional**: solo alimenta el esquema que ve el agente. La firma real que se invoca es `HandleCommand(JObject)`.
- El nombre por defecto sale del nombre de clase (PascalCase → snake_case, se le saca el sufijo `Tool`).
- `AutoRegister = true` (default) las publica como herramientas MCP de primera clase: **aparecen solas tras el reload**, sin reiniciar el server y sin pasar por `execute_custom_tool`. El recurso `mcpforunity://custom-tools` las lista.
- `Group` default `"core"` → visibles. Los demás grupos arrancan ocultos (ver sección siguiente).
- Respuestas: `SuccessResponse` / `ErrorResponse` / `PendingResponse` de `MCPForUnity.Editor.Helpers` (serializan `success` + `message` + `data`).

**Estado S93: el proyecto NO tiene tools propias.** Las dos que existieron (`verify_prototype_parity` y `sim_prototype_turns`, con su colaborador `PrototypeSimBridge`, S84) vivían en `Assets/RunRunSimulator/Scripts/Editor/MCP/` y servían solo al prototipo táctico; se borraron con él en S93 (recuperables en git `3cc5eb5`). La carpeta `Scripts/Editor/` desapareció con ellas. El contrato de arriba sigue siendo la receta si el combate v3 necesita una tool propia (candidata natural: correr `DragonRpsHarness` dentro del editor con salida JSON — aunque hoy alcanza con `dotnet` fuera del editor).

La lección que dejaron: reemplazan los bloques largos de `execute_code` que arrastran el quirk #2 (cuerpo de método, sin `using`, todo calificado) — un ritual de verificación repetido merece una tool con salida JSON estable.

---

## Grupos de tools — `manage_tools` (S84)

El server esconde por defecto **todo lo que no es `core`**. Inventario real de este proyecto y estado tras S84:

| Grupo | Tools | Default | S84 |
|-------|-------|---------|-----|
| `core` | 25 (escena, script, asset, editor, packages...) | on | on |
| `docs` | `unity_docs`, `unity_reflect` | off | **activado** |
| `scripting_ext` | `execute_code`, `manage_scriptable_object` | off | **activado** |
| `testing` | `run_tests`, `get_test_job` | off | **activado** |
| `ui` | `manage_ui` | off | **activado** |
| `probuilder` | `manage_probuilder` | off | **activado** |
| `profiling` | `manage_profiler` | off | **activado** |
| `animation` | `manage_animation` | off | off |
| `asset_gen` | `generate_image`, `generate_model`, `generate_audio`, `import_model*` | off | off (pide API key propia) |
| `vfx` | `manage_shader`, `manage_texture`, `manage_vfx` | off | off |

⚠️ **La activación es efímera** (`manage_tools activate <grupo>`): vive en el server de Python, así que **se pierde en cada reinicio del server** — verificado en S84, tras el upgrade del paquete los 6 grupos volvieron solos a `off`. Lo persistente son los toggles del panel del editor (`EditorPrefs`); `manage_tools sync` los relee. Lo que esto implicaba: `manage_scriptable_object` (todo el pipeline Odin del quirk #1), `manage_probuilder` y `unity_reflect` estaban **ocultos por default en toda sesión nueva**. Si no se marcan en el panel, hay que reactivarlos a mano cada vez.

---

## Skills de Unity instaladas (S84)

1. **Plugin oficial de Unity para Claude Code** ([claude.com/plugins/unity](https://claude.com/plugins/unity), repo [Unity-Technologies/skills](https://github.com/Unity-Technologies/skills)) — instalado por Juan; se cargan solas al trabajar en un proyecto Unity. Las que pegan en este stack: `unity:ui-uitk` (todos nuestros paneles), `unity:build-live-game` (UGS Auth/Cloud Save/Cloud Code), `unity:optimize-text-mesh-pro`, `unity:validate-urp-render-graph-renderer-feature`. El resto (IAP, LevelPlay, tilemaps, pixel-perfect) no aplica.
2. **Subconjunto de [nowsprinting/unity-coding-skills](https://github.com/nowsprinting/unity-coding-skills)** (Unlicense) copiado a `~/.claude/skills/`: `edit-scene`, `unity-yaml-editing-guide`, `run-tests`.

⚠️ **Traducción obligatoria de nombres de tools**: esas 3 skills están escritas para el **MCP Server Extension de JetBrains Rider**, no para CoplayDev. Los nombres que citan NO existen acá:

| Dice la skill | Nuestro equivalente |
|---------------|---------------------|
| `run_method_in_unity` | `execute_code` (o una tool propia con `[McpForUnityTool]`) |
| `get_unity_compilation_result` | `read_console` con `types=["error"]` |
| `unity_play_control` | `manage_editor` (`play` / `pause` / `stop`) |
| `run_unity_tests` | `run_tests` (grupo `testing`) |
| `execute_run_configuration` | no aplica |

Lo que SÍ vale tal cual, independiente del server: la regla de **nunca editar a mano `.unity`/`.prefab`** (script de editor + `SaveScene`/`SaveAsPrefabAsset`, borrarlo después) y toda la guía de YAML de assets (allowlist `.asset`/`.mat`, header `%YAML 1.1`, nunca inventar el GUID de `m_Script`, bools como `0`/`1`, `<Prop>k__BackingField`). No hay Rider instalado en la máquina, así que [JetBrains/rider-skills](https://github.com/JetBrains/rider-skills) (el motor de refactor semántico de ReSharper expuesto al agente) queda descartado hasta que eso cambie.

**Descartados a propósito**: los mega-toolkits de comunidad (`everything-claude-unity`, `claude-unity-game-studio` — 40-120 skills + 49 agentes) imponen arquitectura ajena (VContainer + MessagePipe + UniTask) que choca con las 11 reglas de CLAUDE.md. Y [Codeturion/unity-api-mcp](https://github.com/Codeturion/unity-api-mcp) (firmas exactas por versión de Unity) se solapa con `unity_docs` + `unity_reflect`, que además leen los assemblies cargados de verdad (ven Odin, Feel, DamageNumbersPro).

---

## ⚠️ Quirks S96-S97 (dos PCs · arena sandbox · Feel · NavMesh por script)

1. **(S96)** Tras un `git checkout` que cambia la escena abierta, Unity muestra el modal "The open scene(s) have been modified externally" y el ping del MCP queda bloqueado hasta que Juan aprieta Reload. El CLI `1.0.0-beta.7` rechaza `com.unity.pipeline` 0.5.0-exp.1: `unity pipeline upgrade` lo lleva a 0.6.0-exp.1, pero los comandos con parámetros siguen fallando hasta **reiniciar el editor** (el server en memoria es el viejo). Verificado en S97 tras el reinicio: `recompile_status`, `eval_file`, `capture_game_view` y `console --tail N` aceptan parámetros.
2. **(S97) El bridge CoplayDev muere en el primer domain reload de la sesión** (consola: `[WebSocket] Connection closed`) y `execute_code` devuelve `success:false` sin mensaje. El CLI siguió todo el día: escena, prefab, materiales, Play, capturas y sondas en Play se hicieron enteras por `eval_file`. Para wirear refs por `SerializedObject` y editar prefabs por `PrefabUtility.LoadPrefabContents` / `SaveAsPrefabAsset` / `UnloadPrefabContents` el CLI alcanza.
3. **(S97) `NavMeshSurface.BuildNavMesh()` por script NO persiste el `NavMeshData`**: queda en memoria (sobrevive al Play de esa sesión, se pierde al reiniciar el editor). Hay que `AssetDatabase.CreateAsset(surface.navMeshData, "<carpeta de la escena>/NavMesh-NavMesh.asset")` y guardar la escena. Patrón aplicado en `Resources/Scenes/ArenaSandbox/`.
4. **(S97) `NavMesh.SamplePosition(pos, out hit, dist, areaMask)` ignora el tipo de agente**: acepta puntos aunque solo exista el NavMesh de otro tipo. Para un tipo concreto usar la sobrecarga con `NavMeshQueryFilter { agentTypeID, areaMask }`. IDs reales del proyecto: `Morimonchi = -1372625422`, `Customer = -334000983`, `Humanoid = 0` (la surface `NAVMESHSURFACEMORIMONCHIS` de GameScene usa el primero).
5. **(S97) El prefab `MorimonchiAgent` tiene `NavMeshAgent.areaMask = 56`** (ShopFrontDesk + ShopBackroom + Storage). En cualquier escena cuyo NavMesh sea Walkable(0) el agente falla al crearse ("Failed to create agent because it is not close enough to the NavMesh") aunque el punto esté sobre el mesh, y `Warp` tampoco lo arregla. `MoriMochiAgent.Initialize` fija después `AllAreas & ~BreedingRoom`, pero el instante de creación usa el valor serializado. Solución del sandbox (`ArenaSandbox.Spawn`): instanciar bajo un padre inactivo, poner `areaMask = AllAreas`, reparentar. Diagnóstico decisivo: un `NavMeshAgent` pelado con los mismos radio/altura/tipo sí entra en el mismo punto.
6. **(S97) Feel en modo edición:** `MMF_Player.FeedbacksList` es `null` en un componente recién agregado por script → inicializarla antes de `AddFeedback(typeof(...))`. `MMF_ParticlesInstantiation` en modo `Pool` no devuelve al pool sistemas que nunca se apagan: los prefabs de demo de Hovl son `loop=true` y el pool crece sin tope (45 → 68 sistemas en un minuto, la arena tapada de humo). Variantes propias con `loop=false`, `playOnAwake=false`, `stopAction=Disable` y escala 0,15-0,2 en `Resources/FX/Arena/` (`FX_DustGround`, `FX_DustPuff`, `FX_SmokePuff`); con eso el pool se estabiliza en 4.
7. **(S97) Hovl Magic effects pack viene para built-in**: 34 materiales (32 `Particles/Standard Unlit`, 1 `Particles/Standard Surface`, 1 `Standard`). Conversión in situ por `eval_file` con `UnityEditor.Rendering.MaterialUpgrader.Upgrade(mat, new UnityEditor.Rendering.Universal.ParticleUpgrader("Particles/Standard Unlit"), UpgradeFlags.None)` y `StandardUpgrader("Standard")`; reversible por git.
8. **(S97) `unity command console` no tiene `--count`**: es `--tail N`. El payload JSON viene como string dentro de `data.result` → parsear dos veces. `recompile_status` devuelve `{"status":"completed","failed":false,"errors":[]}` también como string.
9. **(S97) Clips de video.** `com.unity.recorder` 5.1.7 quedó instalado (`unity command package_add --identifier com.unity.recorder --confirm true --wait true`, ~20 s con reload). Su API por `eval_file` NO funcionó: `PrepareRecording()` solo acepta Play, y ya en Play el `eval` cae en el timeout de 5 s del hilo principal (quirk CLI 6) dejando un MP4 de 80 bytes sin frames. **Lo que sí funcionó:** un delegado en `EditorApplication.update` registrado por `eval_file` en Play que llama `ScreenCapture.CaptureScreenshot(ruta absoluta con `/`)` cada 1/30 s de `Time.time` y rota la cámara (`RotateAround`), escribiendo a una carpeta FUERA de `Assets/` (evita importar cientos de PNG); al terminar deja `done.txt` con `frames tiempo`. Después `ffmpeg -framerate <frames/tiempo> -i f%04d.png -c:v libx264 -pix_fmt yuv420p -r 30` (ffmpeg 8 está en la máquina). La captura PNG baja el loop a ~15 fps, por eso el framerate se calcula con el tiempo real para conservar el ritmo. Script: `capture_frames.cs` de la sesión S97.

---

## Historial

- **2026-09-03 (S97):** Fase 1 del sandbox de arena hecha entera por CLI (el bridge murió en el primer reload): escena `ArenaSandbox`, NavMesh persistido, Hovl → URP, Feel cableado en el prefab, 3 dragones en Play verificados con sondas `eval_file` y capturas. Quirks 1-8 de la sección S96-S97.
- **2026-08-31 (S90):** Unity CLI **adoptado como complemento** tras ejercitarlo end-to-end: sintaxis `--key value`, eval con C# moderno, captura con overlay UITK (`--source screen`), supervivencia al domain reload de Play (re-spawn de puerto), list_tests/jobs. Matriz de decisión CLI vs MCP + workflow por fases escritos arriba. Convivencia con CoplayDev verificada en el mismo editor.
- **2026-08-31 (S89):** CLI oficial instalado (`winget install Unity.CLI`, v1.0.0-beta.6) + `com.unity.pipeline` 0.5.0-exp.1 en el proyecto; server verificado con `unity status` y `editor_status`. Quirk nuevo: duplicate assembly Roslyn (ver sección CLI).
- **2026-08-26 (S84):** upgrade del paquete a **v10.1.2** (pinneado por tag, antes `#main`) — arregla el quirk S81 #2 (`component_properties` en el create de `manage_gameobject`), el manejo de domain reload diferido y los 34 tools que pedían aprobación en cada llamada. Creadas las 2 primeras tools propias del proyecto, activados 6 grupos de tools ocultos, skills de Unity documentadas y actualizado el veredicto del Unity CLI. Ver secciones de arriba.
- **2026-08-25 (S81):** sesión de ejecución del MVP de combate (fases 1-4). Caída y resurrección del bridge (quirk 1), wiring por SerializedObject (quirk 2), Play desatendido (quirks 3-4), UITK huérfano (quirk 5). Ver sección de arriba.
- **2026-08-24 (S79):** investigado el Unity CLI oficial con modo MCP (y los CLI de CoplayDev e IvanMurzak). Decisión: no migrar; candidato a complemento. Ver sección de arriba.
- **2026-07-09 (sesión de exploración):** instalado el MCP; validados lectura de escena, escritura a diccionarios Odin (smoke test), reorg de GameScene (27→14 raíces, grupos WORLD/TEMPLATES/POOLS), ProBuilder. GameScene y CombatVisualizerMM son las 2 escenas de proyecto (build 0 y 1), en `Assets/RunRunSimulator/Resources/Scenes/`.

Relacionado: [[MoriMonchiVault/Index/11 - Technical Debt]], [[MoriMonchiVault/Index/09 - Active Context]].
