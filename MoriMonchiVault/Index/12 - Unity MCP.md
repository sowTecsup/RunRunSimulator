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

**Alternativas también gratis, descartadas por ahora:** CLI cliente del propio CoplayDev (`unity-mcp status/scene/...` — habla con el server que ya tenemos, útil para CI, no reemplaza nada) · IvanMurzak/Unity-MCP (Apache-2.0, 70+ tools, CLI propio, corre también en builds compiladas).

Fuentes: [docs oficiales — Unity CLI reemplaza el MCP in-editor](https://docs.unity.com/en-us/unity-cli/replace-mcp-server-unity-cli) · [Unity Pipeline package](https://docs.unity.com/en-us/unity-production-pipeline/local-tools-cli/unity-pipeline-package) · [análisis Vindler (bugs y benchmarks)](https://vindler.solutions/blog/unity-cli-agent-automation) · [CoplayDev/unity-mcp](https://github.com/CoplayDev/unity-mcp) · [IvanMurzak/Unity-MCP](https://github.com/IvanMurzak/Unity-MCP)

---

## Historial

- **2026-08-24 (S79):** investigado el Unity CLI oficial con modo MCP (y los CLI de CoplayDev e IvanMurzak). Decisión: no migrar; candidato a complemento. Ver sección de arriba.
- **2026-07-09 (sesión de exploración):** instalado el MCP; validados lectura de escena, escritura a diccionarios Odin (smoke test), reorg de GameScene (27→14 raíces, grupos WORLD/TEMPLATES/POOLS), ProBuilder. GameScene y CombatVisualizerMM son las 2 escenas de proyecto (build 0 y 1), en `Assets/RunRunSimulator/Resources/Scenes/`.

Relacionado: [[MoriMonchiVault/Index/11 - Technical Debt]], [[MoriMonchiVault/Index/09 - Active Context]].
