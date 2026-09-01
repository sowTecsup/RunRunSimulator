---
tags: [scriptable-object, inventory, persistence]
---

# PlayerInventorySO

**Ruta:** `Data/Player/PlayerInventorySO.cs`

**Responsabilidad:** Inventario persistente del jugador (SO único). Gestiona seis categorías: furniture (F# set), world props (I# list con dupes), equipment (EQ# grids libres por slot), hotbar (6 I# slots), Dabloons (moneda principal), Materiales (AdventureMaterial, PassiveMaterial, EvolutionEssence). Dueno de verdad de "qué posee el jugador". Mutaciones solo a través de métodos explícitos; cada mutación llama `MarkDirty()` y dispara `GameEvents.InventoryChanged(inventory)`. Persiste via JSON local + Cloud Save.

**S93:** Materiales tienen solo getters (read-only); lógica de gasto/suma vive en GameManager o sistemas de gameplay especializados.

## Estructura de Datos (InventoryData)

| Categoría | Tipo | Descripción |
|-----------|------|-------------|
| **Furniture owned** | `List<string>` | FurnitureDefinitionSO; ownership = posesión |
| **World props** | `List<string>` | ItemDefinitionSO; instances con dupes |
| **Equipment grids** | `Dict<EquipmentSlot, List<string>>` | EquipmentSO; free-placement por slot |
| **Hotbar slots** | `string[6]` | I# refs, persistentes entre sesiones |
| **Dabloons** | `int` | Moneda principal |
| **AdventureMaterial** | `int` | Material de aventura (getter only) |
| **PassiveMaterial** | `int` | Material pasivo (getter only) |
| **EvolutionEssence** | `int` | Esencia de evolución (getter only) |

## API Pública

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `AdventureMaterial` | `int` | Getter (readonly) |
| `PassiveMaterial` | `int` | Getter (readonly) |
| `EvolutionEssence` | `int` | Getter (readonly) |
| `GetData()` | `InventoryData` | Retorna snapshot serializable |
| `LoadFrom(InventoryData data)` | `void` | Deserializa desde cloud/JSON |
| `AddWorldProp(itemId)` | `void` | Suma una instancia de prop |
| ... (furniture/equipment/hotbar methods) | ... | Operaciones específicas de cada categoría |

## Materiales (S93)

Campos internos solo lectura desde afuera:
- `AdventureMaterial` — getter int
- `PassiveMaterial` — getter int
- `EvolutionEssence` — getter int

**Lógica de gasto:** Delegada a GameManager/sistemas de gameplay que mutarían el SO directamente (no hay Add/Spend públicos). Cada mutación dispara `GameEvents.InventoryChanged(inventory)`.

## Persistencia

**InventoryData:** Estructura JSON con 8 campos. GameManager escucha `GameEvents.InventoryChanged(inventory)` y dispara:
1. `SaveSystem.SaveInventory(inventory)` → local JSON
2. Cloud push vía `CloudSyncService.PushAsync()`

## Ciclo de Vida (carga)

1. `GameManager.Awake()` → `SaveSystem.LoadInventory(inventory)` carga JSON local
2. `Inventory.LoadFrom(data)` embudo de carga
3. `GameEvents.InventoryReloaded(inventory)` notifica UI

## Vinculado a

- [[Index/07 - Persistence & Identity]]

**Conexiones:** [[GameManager]], [[SaveSystem]], [[CloudSyncService]], [[GameEvents]], [[CashRegister]]

