---
name: prefabs-via-mcp
description: No leer/parsear archivos .prefab/.unity como texto — inspeccionarlos y editarlos vía Unity MCP
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 7022bdde-8192-4b89-bd47-851436d5edfc
  modified: 2026-07-21T22:12:16.659Z
---

Juan pidió (S60, 2026-07-21) no leer prefabs directamente como YAML sino usar el MCP de Unity.

**Why:** El YAML de Unity es frágil de interpretar (fileIDs, modificaciones de prefab-instance) y leerlo como texto gasta contexto y da conclusiones a medias; el editor en vivo es la fuente de verdad.

**How to apply:** Para inspeccionar prefabs/escenas usar `execute_code` (AssetDatabase/PrefabUtility), `manage_prefabs`, `find_gameobjects` o los resources del MCP. Grep sobre archivos .unity/.prefab solo como último recurso para búsquedas globales de GUIDs, no para leer contenido. Relacionado: [[unity-mcp]].
