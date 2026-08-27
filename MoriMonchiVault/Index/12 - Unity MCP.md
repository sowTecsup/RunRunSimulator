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

## Unity CLI oficial (evaluado 2026-08-24 — NO adoptado, candidato a complemento)

Unity publicó en Unite Seoul (julio 2026) el **Unity CLI** oficial: binario standalone **gratis** (sin suscripción Unity AI, sin límite de conexiones) con modo servidor MCP (`unity mcp`), construido sobre el paquete `com.unity.pipeline` (beta, requiere Unity 6.0 LTS+ — el proyecto está en 6000.3.9f1 ✓). Unity deprecó su viejo server MCP in-editor (`com.unity.ai.assistant`); eso **NO afecta** al "MCP for Unity" de CoplayDev que usamos (independiente, MIT, activo — v10.0.0 de junio 2026, sigue gratis).

**Veredicto S79: seguir con CoplayDev como driver del editor.** Razones:

1. **Bug crítico reportado**: entrar a Play mode (domain reload) invalida los tokens del Pipeline y **rompe la sesión MCP** hasta reiniciar — le pega exactamente a nuestro loop central de "verificar en Play antes de declarar hecho".
2. **Catálogo más chico**: sin equivalente de `manage_probuilder` ni `manage_scriptable_object`; gira alrededor de eval + editor/builds/tests. Todo el pipeline Odin validado (quirk #1) depende del server actual.
3. **Beta con quirks propios**: a veces exige el editor en foreground, diálogos modales bloquean (mitigable con `-automated`), ~16x más lento por llamada en modo MCP (benchmark de comunidad, ~0.8s vs 0.05s), y el agente necesita skills instaladas aparte (`npx skills add Unity-Technologies/skills`) para descubrir capacidades.

**Dónde SÍ interesa (como complemento — pueden convivir sin conflicto):** builds headless, `unity test` con salida NUnit, CI/CD, gestión de editores/licencias, y sobre todo **`unity eval`** — ejecuta C# en el editor **sin domain reload**; vale probar si esquiva la limitación de C# 6 del quirk #2 (Roslyn roto). **Cuándo reevaluar:** cuando arreglen el bug de Play mode (al momento del análisis prometían fixes semanales).

> **Actualización 2026-08-26 (S84):** el bloqueante #1 **ya tiene fix**. El bug era que el domain reload de Play mode regeneraba el bearer token del server Pipeline y toda llamada posterior devolvía 401; Unity lo arregló a la semana del lanzamiento del beta. Pruebas de comunidad del 13-08-2026 sobre `CLI 1.0.0-beta.4` + `Pipeline 0.5.0-exp.1`. Además apareció `--detach` (devuelve job id; después `unity job status` / `unity job wait`). **La decisión no cambia** (sigue ~16x más lento y sin ProBuilder/ScriptableObjects), pero el interés como COMPLEMENTO sube: `unity eval` corre C# en el editor vivo sin domain reload y `unity test` da NUnit XML — es la red de seguridad natural para el quirk S81 #1 (bridge caído). Sus skills se instalan aparte con `npx skills add Unity-Technologies/skills` (trae `unity-cli`, `unity-package-management` y `new-unity-project`, que NO vienen en el plugin oficial de Claude Code).

**Alternativas también gratis, descartadas por ahora:** CLI cliente del propio CoplayDev (`unity-mcp status/scene/...` — habla con el server que ya tenemos, útil para CI, no reemplaza nada) · IvanMurzak/Unity-MCP (Apache-2.0, 70+ tools, CLI propio, corre también en builds compiladas).

Fuentes: [docs oficiales — Unity CLI reemplaza el MCP in-editor](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli) · [Unity Pipeline package](https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package) · [análisis Vindler (bugs y benchmarks)](https://vindler.solutions/blog/unity-cli-agent-automation) · [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) · [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)

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

**Las nuestras** viven en `Assets/RunRunSimulator/Scripts/Editor/MCP/` (los scripts del proyecto no tenían ninguna carpeta `Editor/` hasta S84):

| Tool | Para qué |
|------|----------|
| `verify_prototype_parity` | Corre el plan por los DOS caminos (`PlanProjection.Project` vs réplica del ejecutor: `ResolveBeat` por beat + `ResolveEnemyTurn`) sobre clones, compara estado y eventos beat por beat, y verifica que el **estado canónico no se filtró** — ojo que `CombatSimState.Clone()` comparte `Board` por referencia, así que si algo lo mutara la proyección se filtraría a la partida. Es la regla innegociable del prototipo convertida en un llamado. |
| `sim_prototype_turns` | Corre un plan sobre un clon y devuelve los `ResolutionEvent` fase por fase + snapshot de unidades, con `extraEnemyTurns` para observar el desgaste sin jugador. Nunca toca la partida en curso. |
| `PrototypeSimBridge` | No es tool: el colaborador compartido (parseo del plan JSON, firmas de estado/evento, diffs, describe). |

Ambas piden **Play mode** (necesitan `manager.Canonical`) y aceptan `plan` como JSON — `{"beats":[{"actions":[{"unitId":0,"abilityIndex":0,"targetCell":[3,4],"direction":[1,0],"slamCell":[5,4]}]}]}` — o sin `plan` usan el plan vivo del HUD.

Reemplazan los bloques largos de `execute_code` que arrastran el quirk #2 (cuerpo de método, sin `using`, todo calificado): el ritual de verificación queda en una llamada con salida JSON estable.

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

## Historial

- **2026-08-26 (S84):** upgrade del paquete a **v10.1.2** (pinneado por tag, antes `#main`) — arregla el quirk S81 #2 (`component_properties` en el create de `manage_gameobject`), el manejo de domain reload diferido y los 34 tools que pedían aprobación en cada llamada. Creadas las 2 primeras tools propias del proyecto, activados 6 grupos de tools ocultos, skills de Unity documentadas y actualizado el veredicto del Unity CLI. Ver secciones de arriba.
- **2026-08-25 (S81):** sesión de ejecución del MVP de combate (fases 1-4). Caída y resurrección del bridge (quirk 1), wiring por SerializedObject (quirk 2), Play desatendido (quirks 3-4), UITK huérfano (quirk 5). Ver sección de arriba.
- **2026-08-24 (S79):** investigado el Unity CLI oficial con modo MCP (y los CLI de CoplayDev e IvanMurzak). Decisión: no migrar; candidato a complemento. Ver sección de arriba.
- **2026-07-09 (sesión de exploración):** instalado el MCP; validados lectura de escena, escritura a diccionarios Odin (smoke test), reorg de GameScene (27→14 raíces, grupos WORLD/TEMPLATES/POOLS), ProBuilder. GameScene y CombatVisualizerMM son las 2 escenas de proyecto (build 0 y 1), en `Assets/RunRunSimulator/Resources/Scenes/`.

Relacionado: [[MoriMonchiVault/Index/11 - Technical Debt]], [[MoriMonchiVault/Index/09 - Active Context]].
