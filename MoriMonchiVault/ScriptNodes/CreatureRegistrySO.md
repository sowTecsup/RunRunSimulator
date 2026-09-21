---
tags: [scriptable-object, genetics, registry]
---

# CreatureRegistrySO

**Ruta:** `Data/Genetics/CreatureRegistrySO.cs`

**Responsabilidad:** Dato puro (SO): cache en memoria de todas las criaturas, divididas en vivas (`creatures`) e idas (`departed`). `SerializedScriptableObject` con dos `Dictionary<string, CreatureDNA>`. Funciona como estante dual: `GetAll()` = solo vivas (UI gameplay), `GetAllKnown()` = vivas + idas (árbol genealógico). `LoadFrom(RegistryData)` es el único embudo de carga (local JSON + nube). Llama `ReconcileColors()` (self-heal): para cada criatura, extrae `BaseColor` desde el primer token de `UniqueID` via `TryColorFromKey()`, y regenera `SecondaryColor` determinista. Blinda contra desync de color que quiebra lookups. **S93:** Editor tooling (botones) migrado a [[CreatureRegistryDevTools]]; aquí solo datos y lógica de estado. **S129:** Introducido estante `departed` + métodos `Depart(id)`, `GetAllKnown()`, `GetData()`, límite `MaxDeparted = 300`.

## Campos Públicos

| Campo | Tipo | Acceso | Descripción |
|-------|------|--------|-------------|
| `creatures` | `Dictionary<string, CreatureDNA>` | [OdinSerialize] private | Criaturas vivas |
| `departed` | `Dictionary<string, CreatureDNA>` | [OdinSerialize] private | Criaturas idas (muertas/vendidas), LIFO hasta MaxDeparted |
| `MaxDeparted` | `const int` | public | 300; límite de historia de idas |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `TryGet(id, out dna)` | `bool` | Búsqueda por UniqueID (vivas + idas) |
| `Register(dna)` | `bool` | Registra nueva criatura en vivas |
| `GetAll()` | `Dictionary<string, CreatureDNA>` | Copia solo de criaturas vivas (UI gameplay) |
| `GetAllKnown()` | `Dictionary<string, CreatureDNA>` | Copia de vivas + idas (árbol genealógico) |
| `Departed` [property] | `IReadOnlyDictionary<...>` | Acceso read-only al estante de idas |
| `Depart(id)` | `bool` | Mueve criatura de vivas a idas, auto-trim si excede MaxDeparted |
| `LoadFrom(data)` | `void` | Carga desde `RegistryData` (V3), auto-reconcilia colores |
| `ReconcileColors()` | `void` | **Self-heal:** extrae BaseColor desde key, regenera SecondaryColor, rellena Generation |
| `Wipe()` | `int` | Borra TODAS las criaturas (vivas + idas), retorna cantidad eliminada |
| `RerollRolesAndElements()` | `void` | Rerollea Role + Element de todas las criaturas (data pura, sin persistencia) |

## Ciclo de Vida (carga)

1. `GameManager.Awake()` → `SaveSystem.LoadInto(registry)` carga JSON local
2. `Registry.LoadFrom(RegistryData)` embudo de carga
3. `ReconcileColors()` auto-repair de colores + rellena Generation
4. `GameEvents.RegistryReloaded(registry)` notifica UI + spawner

## Ciclo de Vida (escritura de IsDead/Sold)

1. Gameplay event (muerte/venta) → `CreatureLifecycle.Kill/Adopt()`
2. `CreatureLifecycle` → `registry.Depart(id)` mueve a idas
3. `Depart()` auto-trimea si `departed.Count > MaxDeparted` (LIFO + protege padres)
4. `GameEvents.RegistryChanged(registry)` → persist a disco

## Trim Logic (MaxDeparted)

Cuando `departed` supera 300:
1. Identifica todas las criaturas vivas cuyas madres/padres están en idas → protege esos IDs
2. Ordena por `Timestamp` ascendente (más viejo primero)
3. Elimina N más viejo hasta caer a 300, respetando protected IDs

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

**Conexiones:** [[GameManager]], [[SaveSystem]], [[MoriMochiSpawner]], [[CreatureDNA]], [[CreatureRegistryDevTools]], [[GameEvents]], [[CreatureLifecycle]], [[RegistryData]]
