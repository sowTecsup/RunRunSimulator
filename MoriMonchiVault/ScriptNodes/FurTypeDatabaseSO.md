---
tags: [script, core]
---

# FurTypeDatabaseSO.cs

**Ruta:** `Data/Databases/FurTypeDatabaseSO.cs`

**Responsabilidad:** Mapea cada `FurType` a un material CartoonShader via `Dictionary<FurType, Material>` (OdinSerialize). SerializedScriptableObject sin `static Current`; lo posee GameManager, llega al visualizer via `MoriMonchiController.Initialize()` que llama `visualizer.SetFurDatabase(furDb)`. `GetMaterial(type)` devuelve el material para aplicar en runtime. Botón editor `PopulateFromEnum` precarga entradas del enum.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[GameManager]], [[MoriMonchiController]], [[MoriMonchiVisualizer]], [[FurType]], [[CreatureDNA]]
