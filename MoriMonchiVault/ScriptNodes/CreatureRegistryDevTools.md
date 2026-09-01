---
tags: [editor, dev-tools, genetics]
---

# CreatureRegistryDevTools.cs

**Ruta:** `Editor/CreatureRegistryDevTools.cs`

**Responsabilidad:** Menu items editor estáticos para operaciones de desarrollo en CreatureRegistrySO. Centraliza botones (RerollRolesAndElements, Wipe, SyncFromJSON) que antes estaban como Odin buttons directamente en el SO. Métodos: `TryFindRegistry()` (búsqueda por AssetDatabase), `SyncFromJson()` (carga creatures desde JSON vía SaveSystem), `RerollRolesAndElements()` (rerollea Role + Element de todas, guarda local, dispara RegistryReloaded), `WipeRegistry()` (borra todas las creatures).

**S93:** Traslado de tooling editor desde CreatureRegistrySO.cs a módulo independiente.

## Métodos Públicos (estáticos)

| Método | Descripción |
|--------|-------------|
| `SyncFromJson()` | MenuItem: `MoriMonchi/Registry/Sync From JSON` — carga creatures desde creature_database.json local vía `SaveSystem.LoadInto(registry)` |
| `RerollRolesAndElements()` | MenuItem: `MoriMonchi/Registry/Reroll Roles & Elements (current)` — rerollea Role + Element de todas las creatures, guarda local, dispara `RegistryReloaded` si en Play mode |
| `WipeRegistry()` | MenuItem: `MoriMonchi/Registry/Wipe Registry (DEV)` — borra TODAS las creatures, guarda local, dispara `RegistryReloaded` si en Play mode |

## Comportamiento

**SyncFromJson():**
- Busca CreatureRegistrySO vía `AssetDatabase.FindAssets("t:CreatureRegistrySO")`
- Retorna error si 0 o >1 encontrados
- Llama `SaveSystem.LoadInto(registry)` para leer JSON

**RerollRolesAndElements():**
```
Requiere: registry no vacío
Itera: cada dna en registry.Creatures
  dna.Role = random enum Role
  dna.Element = random enum Element
Guarda: SaveSystem.SaveDatabase(registry)
Refresca UI: GameEvents.RegistryReloaded(registry) si Application.isPlaying
Log: "{count} roles/elementos rerolleados. Pulsá 'Push to Cloud' para subir."
```

**WipeRegistry():**
```
Itera: registry.Wipe()  -- retorna cantidad eliminada
Guarda: SaveSystem.SaveDatabase(registry)
Refresca UI: GameEvents.RegistryReloaded(registry) si Application.isPlaying
Log: "Registro borrado ({had} criaturas). Pulsá 'Push to Cloud' para limpiar Cloud Save."
```

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[CreatureRegistrySO]] — dato que modifica
- [[SaveSystem]] — I/O JSON
- [[GameEvents]] — dispara RegistryReloaded

**Conexiones:** [[CreatureRegistrySO]], [[SaveSystem]], [[GameEvents]]

