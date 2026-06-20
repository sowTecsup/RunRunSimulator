---
tags: [memory-bank, script, player-world]
---

# MoriMonchiVisualizer.cs

**Ruta:** `World/MoriMonchiVisualizer.cs`

**Responsabilidad:** Ensamblaje 3D: 6 sockets (body, armL/R, eyeL/R, mouth). `Assemble(dna, bank)` instancia prefabs y aplica color + fur material via `MaterialPropertyBlock` (`_Base_Color`, `_Shadows_Color`, `_Outline_Color`). `ApplyFur()` consulta `FurTypeDatabaseSO.Current` para el material CartoonShader según `FurType`. Botón `[Setup]` en editor. Gizmos de sockets siempre visibles en escena.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[BodyPartJoint]], [[PartVisualBankSO]], [[CreatureDNA]], [[MoriMonchiController]], [[FurTypeDatabaseSO]], [[FurType]]
