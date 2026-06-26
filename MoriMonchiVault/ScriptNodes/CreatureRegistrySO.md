---
tags: [script, cloud]
---

# CreatureRegistrySO.cs

**Ruta:** `Data/Genetics/CreatureRegistrySO.cs`

**Responsabilidad:** Cache en memoria de todas las criaturas. `SerializedScriptableObject` con `Dictionary<string, CreatureDNA>`. `LoadFrom(dict)` es el único embudo de carga (local JSON + nube). Llama `ReconcileColors()` (self-heal): para cada criatura, extrae `BaseColor` desde el primer token de `UniqueID` via `TryColorFromKey()`, y regenera `SecondaryColor` determinista. Blinda contra desync de color que quiebra lookups. Editor: `Sync from JSON`, `Randomize Personalities (current)`, `Wipe Registry (DEV)` buttons en el inspector — el wipe borra todas las criaturas locales + refresca escena sin push a nube.

**Vinculado a:** [[Index/07 - Persistence & Identity]]

**Conexiones:** [[GameManager]], [[SaveSystem]], [[MoriMochiSpawner]], [[CreatureDNA]], [[CombatService]]
