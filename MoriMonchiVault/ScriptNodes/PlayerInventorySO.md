---
tags: [script, inventory, equipment, persistence]
---

# PlayerInventorySO

**Ruta:** `Data/Player/PlayerInventorySO.cs`

**Responsabilidad:** Inventario persistente del jugador (SO único, singleton runtime via GameManager). Gestiona seis categorías: furniture (F# set), world props (I# list dupes), equipment (EQ# free-placement grids per slot), hotbar (6 I# slots), Dabloons (moneda principal), **S75:** Materiales de recursos (AdventureMaterial, PassiveMaterial, EvolutionEssence). Dueno de verdad de "qué posee el jugador". Muta solo a través de métodos explícitos; cada mutación llama `MarkDirty()` y es seguida por `GameEvents.InventoryChanged(inventory)`. Persiste via JSON local + Cloud Save.

## Estructura de Datos

| Categoría | Tipo | Descripción |
|-----------|------|-------------|
| **Furniture owned** | `List<string>` (set) | FurnitureDefinitionSO; ownership = posesión |
| **World props** | `List<string>` (list+dupes) | ItemDefinitionSO; instances únicas |
| **Equipment grids** | `Dict<EquipmentSlot, List<string>>` | EquipmentSO; free-placement grid por slot |
| **Hotbar slots** | `string[6]` | I# refs, persisten entre sesiones |
| **Dabloons** | `int` | Moneda única |
| **AdventureMaterial** | `int` | **S75** Material de aventura |
| **PassiveMaterial** | `int` | **S75** Material pasivo |
| **EvolutionEssence** | `int` | **S75** Esencia de evolución |

## API Pública — Materiales (S75)

**Espejo simétrico de API Dabloons:**

### Adventure Material

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `AdventureMaterial` | `int` | Getter (readonly) |
| `AddAdventureMaterial(amount)` | `void` | Suma (si > 0). Marca dirty. |
| `SpendAdventureMaterial(amount)` | `bool` | Resta (si amount > 0 y >= amount). Marca dirty. True si éxito. |

### Passive Material

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `PassiveMaterial` | `int` | Getter (readonly) |
| `AddPassiveMaterial(amount)` | `void` | Suma (si > 0). Marca dirty. |
| `SpendPassiveMaterial(amount)` | `bool` | Resta (si amount > 0 y >= amount). Marca dirty. True si éxito. |

### Evolution Essence

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `EvolutionEssence` | `int` | Getter (readonly) |
| `AddEvolutionEssence(amount)` | `void` | Suma (si > 0). Marca dirty. |
| `SpendEvolutionEssence(amount)` | `bool` | Resta (si amount > 0 y >= amount). Marca dirty. True si éxito. |

## Persistencia (S75)

**InventoryData (Cloud):** Estructura serializable con 8 campos:
- `furnitureOwned`
- `worldPropsStored`
- `equipmentGrids`
- `hotbarSlots`
- `dabloons`
- `adventureMaterial` — **NUEVO S75**
- `passiveMaterial` — **NUEVO S75**
- `evolutionEssence` — **NUEVO S75**

GameManager escucha `GameEvents.InventoryChanged(inventory)` y dispara save local + cloud.

## Vinculado a

- [[Index/07 - Persistence & Identity]]

**Conexiones:** [[GameManager]], [[GameEvents]], [[CutieMarkDatabaseSO]] (consumos de materiales)
