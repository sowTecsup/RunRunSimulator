---
tags: [script, core]
---

# FurTypeDatabaseSO.cs

**Ruta:** `Data/Databases/FurTypeDatabaseSO.cs`

**Responsabilidad:** Mapea cada `FurType` a un material CartoonShader via `Dictionary<FurType, Material>` (OdinSerialize). SerializedScriptableObject sin `static Current`; lo posee GameManager, llega al visualizer via `MoriMonchiController.Initialize()` que llama `visualizer.SetFurDatabase(furDb)`. `GetMaterial(type)` devuelve el material para aplicar en runtime. **S57:** Nuevo `Dictionary<FurType, float> mintWeights` (OdinSerialize) con pesos relativos (mayor valor = más probable). `RollMintFurType()` ejecuta ruleta ponderada: suma total de pesos, genera roll aleatorio, itera acumulando hasta encontrar el que gana. Fallback uniforme si tabla vacía o deshabilitada. Heredencia en breeding NO usa tabla (50/50 determinista de padres). Botón editor `PopulateFromEnum` precarga entradas del enum para ambos diccionarios.

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/10 - Visualization]]

**Conexiones:** [[GameManager]], [[MoriMonchiController]], [[MonchiVisualizer]], [[FurType]], [[CreatureDNA]], [[CreatureGenerator]]
