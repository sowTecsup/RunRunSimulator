---
tags: [scriptable-object, genetics, registry]
---

# CreatureRegistrySO

**Ruta:** `Data/Genetics/CreatureRegistrySO.cs`

**Responsabilidad:** Dato puro (SO): cache en memoria de todas las criaturas. `SerializedScriptableObject` con `Dictionary<string, CreatureDNA>`. `LoadFrom(dict)` es el único embudo de carga (local JSON + nube). Llama `ReconcileColors()` (self-heal): para cada criatura, extrae `BaseColor` desde el primer token de `UniqueID` via `TryColorFromKey()`, y regenera `SecondaryColor` determinista. Blinda contra desync de color que quiebra lookups. Métodos públicos: `TryGet(id, out dna)`, `Register(dna)`, `GetAll()`, `Count`, `Wipe()`, `RerollRolesAndElements()`. **S93:** Editor tooling (botones) migrado a [[CreatureRegistryDevTools]]; aquí solo datos y lógica de estado.

## Campos Públicos

| Campo | Tipo | Acceso | Descripción |
|-------|------|--------|-------------|
| `creatures` | `Dictionary<string, CreatureDNA>` | [OdinSerialize] private | Todas las criaturas por UniqueID |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `TryGet(id, out dna)` | `bool` | Búsqueda por UniqueID |
| `Register(dna)` | `bool` | Registra nueva criatura |
| `GetAll()` | `Dictionary<string, CreatureDNA>` | Copia del diccionario completo |
| `LoadFrom(dict)` | `void` | Carga desde JSON vía SaveSystem, auto-reconcilia colores |
| `ReconcileColors()` | `void` | **Self-heal:** extrae BaseColor desde key, regenera SecondaryColor |
| `Wipe()` | `int` | Borra TODAS las criaturas, retorna cantidad eliminada |
| `RerollRolesAndElements()` | `void` | Rerollea Role + Element de todas las criaturas (data pura, sin persistencia) |

## Ciclo de Vida (carga)

1. `GameManager.Awake()` → `SaveSystem.LoadInto(registry)` carga JSON local
2. `Registry.LoadFrom(dict)` embudo de carga
3. `ReconcileColors()` auto-repair de colores
4. `GameEvents.RegistryReloaded(registry)` notifica UI + spawner

## Editor Tooling (S93)

Botones movidos a [[CreatureRegistryDevTools]] (menú items estáticos):
- `MoriMonchi/Registry/Sync From JSON` — carga desde JSON
- `MoriMonchi/Registry/Reroll Roles & Elements (current)` — rerollea ambos fields
- `MoriMonchi/Registry/Wipe Registry (DEV)` — borra todos

**Métodos internos:** `RerollRolesAndElements()` y `Wipe()` en CreatureRegistrySO son data purity; persistencia y eventos dispara el tool desde CreatureRegistryDevTools.

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Genetics/Creature Registry`

## Vinculado a

- [[Index/07 - Persistence & Identity]]

**Conexiones:** [[GameManager]], [[SaveSystem]], [[MoriMochiSpawner]], [[CreatureDNA]], [[CreatureRegistryDevTools]], [[GameEvents]]

