---
tags: [memory-bank, script, data]
---

# FurTypeDatabaseSO.cs

**Ruta:** `Data/FurTypeDatabaseSO.cs`

**Responsabilidad:** Database ScriptableObject que mapea cada `FurType` a un material CartoonShader via `Dictionary<FurType, Material>`. Singleton `Current` registrado en `OnEnable`. `GetMaterial(type)` devuelve el material para aplicar en runtime via `MaterialPropertyBlock`. Botón editor `PopulateFromEnum` que precarga las entradas del enum.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[MoriMonchiVisualizer]], [[FurType]], [[CreatureDNA]]
