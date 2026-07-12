---
tags: [scriptable-object, genetics, registry]
---

# CreatureRegistrySO

**Ruta:** `Data/Genetics/CreatureRegistrySO.cs`

**Responsabilidad:** Cache en memoria de todas las criaturas. `SerializedScriptableObject` con `Dictionary<string, CreatureDNA>`. `LoadFrom(dict)` es el único embudo de carga (local JSON + nube). Llama `ReconcileColors()` (self-heal): para cada criatura, extrae `BaseColor` desde el primer token de `UniqueID` via `TryColorFromKey()`, y regenera `SecondaryColor` determinista. Blinda contra desync de color que quiebra lookups. Editor: `Sync from JSON`, **`Reroll Roles & Elements (current)`** (S39 botón renombrado), `Wipe Registry (DEV)` buttons en el inspector — el wipe borra todas las criaturas locales + refresca escena sin push a nube.

## Cambios S39

**Botón renombrado:**
- Antes: `"Reroll Personalities (current)"` (rerolleaba Personality solo)
- Ahora: **`"Reroll Roles & Elements (current)"`** (rerollea ambos Role + Element)

**Lógica del botón (RerollRolesAndElements):**
```csharp
[Button("Reroll Roles & Elements (current)", ButtonSizes.Large), ...]
private void RerollRolesAndElements()
{
    if (creatures.Count == 0) { ...; return; }

    var roleValues    = (Role[])System.Enum.GetValues(typeof(Role));
    var elementValues = (Element[])System.Enum.GetValues(typeof(Element));
    
    foreach (var dna in creatures.Values)
    {
        dna.Role    = roleValues[UnityEngine.Random.Range(0, roleValues.Length)];
        dna.Element = elementValues[UnityEngine.Random.Range(0, elementValues.Length)];
    }

    MarkDirty();
    SaveSystem.SaveDatabase(this);  // persiste local

    if (Application.isPlaying)
        GameEvents.RegistryReloaded(this);  // re-spawnea con nuevos roles/elementos, SIN push

    Debug.Log($"[CreatureRegistrySO] {creatures.Count} roles/elementos rerolleados...");
}
```

**Cambios de lógica:**
- Rerollea `dna.Role` (enum Role: Protector/Agresivo/Empático) en lugar de `dna.Personality` (deprecated)
- Rerollea también `dna.Element` (enum Element: None, Fuego, Agua, etc.) en el mismo botón
- Mantiene el flujo: local save + refresca escena (sin push a nube)

## Campos Públicos

| Campo | Tipo | Acceso | Descripción |
|-------|------|--------|-------------|
| `creatures` | `Dictionary<string, CreatureDNA>` | [OdinSerialize] private | Todas las criaturas por UniqueID |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `TryGet(id, out dna)` | `bool` | Búsqueda por UniqueID |
| `Register(dna)` | `bool` | Registra nueva criatura |
| `LoadFrom(dict)` | `void` | Carga desde JSON vía SaveSystem, auto-reconcilia colores |
| `ReconcileColors()` | `void` | **Self-heal:** extrae BaseColor desde key, regenera SecondaryColor |

## Métodos Editor (Odin)

| Método | Descripción |
|--------|-------------|
| `SyncFromJson()` | Botón: carga desde creature_database.json local vía SaveSystem |
| `RerollRolesAndElements()` | **S39** Botón: rerollea Role + Element de todas las criaturas, guarda local, refresca escena sin push |
| `WipeRegistry()` | Botón: borra TODAS las criaturas (local + opcionalmente nube via push) |

## Ciclo de Vida (carga)

1. `GameManager.Awake()` → `SaveSystem.LoadInto(registry)` carga JSON local
2. `Registry.LoadFrom(dict)` embudo de carga
3. `ReconcileColors()` auto-repair de colores
4. `GameEvents.RegistryReloaded(registry)` notifica UI + spawner

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Genetics/Creature Registry`

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[GameManager]] — propietario de registry instance
- [[SaveSystem]] — I/O JSON (local + cloud)
- [[MoriMochiSpawner]] — consumer de creatures
- [[CreatureDNA]] — elementos individuales
- [[Role]] — enum (S37/S39)
- [[Element]] — enum (S39)
- [[GameEvents]] — dispara RegistryReloaded

## Conexiones

**Entrada:**
- `SaveSystem.LoadInto()` — carga desde JSON
- Editor buttons (Sync, Reroll, Wipe)
- `GameEvents.RegistryChanged()` listeners (persist)

**Salida:**
- Diccionario creatures (leído por spawner, UI, etc.)
- `GameEvents.RegistryReloaded()` al recargar desde nube o editor

## Notas

- **Backward compat:** `ReconcileColors()` auto-repara dessyncs. Si BaseColor derivado de key falla, mantiene el valor actual.
- **S39 cambio crítico:** Botón reroll ahora es "Roles & Elements" (antes solo Personalities). Rerollea ambos campos simultáneamente.
- **Persistent identity:** BaseColor almacenado en UniqueID (RRGGBB token 1); invariante color↔identidad protegida.
- **Editor safety:** Todos los botones piden `MarkDirty()` + `SaveSystem.SaveDatabase()` para persist local.
