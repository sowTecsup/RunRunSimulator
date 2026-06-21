---
tags: [script, world]
---

# MoriMonchiVisualizer.cs

**Ruta:** `World/Creatures/MoriMonchiVisualizer.cs`

**Responsabilidad:** Ensamblaje 3D: 6 sockets (body, armL/R, eyeL/R, mouth). `SetFurDatabase(furDb)` cachea la ref a FurTypeDatabaseSO. `Assemble(dna, bank)` instancia prefabs de banco, alinea insertionJoints a sockets, aplica espejos según BodyPartJoint.isMirror, luego llama `ApplyFur()`. `RefreshFur(dna)` reaplica fur/colores sin re-ensamblar modelo (util para reloads). **`ApplyFur()` migrado a Unity Toon Shader**: (1) obtiene material Toon via `furDatabase.GetMaterial(dna.FurType)`; (2) consulta `ColorGenetics.BuildFurPalette(dna.BaseColor, dna.SecondaryColor)` → struct `FurPalette` con 4 colores; (3) cachea 4 PropertyIDs estáticos (`_BaseColor`, `_1st_ShadeColor`, `_2nd_ShadeColor`, `_RimLightColor`); (4) aplica en MaterialPropertyBlock para cada renderer. **Es el ÚNICO punto de acople a nombres de propiedad del shader.** Botón `[Setup]` crea 6 sockets como hijos. Gizmos siempre visibles en escena.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[BodyPartJoint]], [[PartVisualBankSO]], [[CreatureDNA]], [[MoriMonchiController]], [[FurTypeDatabaseSO]], [[FurType]]
