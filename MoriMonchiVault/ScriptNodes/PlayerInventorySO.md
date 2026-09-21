---
tags: [scriptable-object, inventory, persistence]
---

# PlayerInventorySO

**Ruta:** `Data/Player/PlayerInventorySO.cs`

**Responsabilidad:** Dato persistente del jugador (SO). Contiene dos monedas (`Dabloons`, `Minerita`), muebles desbloqueados, props del mundo, y slots hotbar. **S128:** métodos públicos `Balance()`, `Add()`, `TrySpend()`, `ResetCurrency()`; `adventureMaterial` renombrada a `minerita` con `[PreviouslySerializedAs]`. Nunca llama `SaveSystem` ni dispara eventos; lo hace [[Wallet]] (puerta única de mutaciones). **S129:** Eliminadas grillas de equipo y métodos de equipo (HC-1).

## Campos Públicos (Serializados Odin)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `furnitureOwned` | `List<string>` | IDs de muebles desbloqueados (set: sin duplicados) |
| `worldPropsStored` | `List<string>` | IDs de props del mundo (list: permite dupes) |
| `hotbarSlots` | `string[6]` | 6 slots hotbar (I# ids, persisten) |
| `dabloons` | `int` | Primera moneda (compras, venta, reembolso) |
| `minerita` | `int` | Segunda moneda (exploración, evolución); migrada de `adventureMaterial` (v1→v2) |

## Métodos Públicos

### Monedas (S128+)

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Balance(Currency c)` | `int` | Lee saldo de moneda (Dabloons o Minerita) |
| `Add(Currency c, int amount)` | `void` | Suma cantidad a moneda (sin validar amount > 0; lo hace [[Wallet]]) |
| `TrySpend(Currency c, int amount)` | `bool` | Intenta gastar; si saldo < amount → false; si ok → gasta, retorna true |
| `ResetCurrency(Currency c, int value)` | `void` | Setea moneda a valor exacto (debug) |

### Muebles

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `AddFurniture(string id)` | `bool` | Añade mueble si no existe; false si ya tiene |
| `HasFurniture(string id)` | `bool` | Consulta si posee mueble |
| `FurnitureOwned` (property) | `IReadOnlyList<string>` | Read-only lista de muebles |

### Props del Mundo

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `AddWorldProp(string id)` | `void` | Añade prop (permite dupes) |
| `RemoveWorldProp(string id)` | `bool` | Remueve primera instancia de prop |
| `WorldPropsStored` (property) | `IReadOnlyList<string>` | Read-only lista de props |

### Hotbar

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `SetHotbarSlot(int slot, string id)` | `bool` | Setea ID en slot 0-5; false si fuera de rango |
| `HotbarSlot(int slot)` | `string` | Lee ID del slot (null si vacío) |
| `ClearHotbarSlot(int slot)` | `void` | Vacía slot |

### Persistencia

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `LoadFrom(InventoryData data)` | `void` | Carga desde [[SaveSystem]]; null → valores por defecto |
| `GetData()` | `InventoryData` | Retorna struct serializable para guardar |
| `MarkDirty()` | `void` | Señala cambio (Odin, persiste el SO en disco inmediato) |

## Monedas S128

```csharp
public enum Currency { Dabloons, Minerita }
```

**Dabloons:** compras en tienda, venta de criaturas, reembolso de muebles.

**Minerita:** ganada en expediciones, usada en evolución. Antes `AdventureMaterial` (v1, migrada a v2).

## Migración v1 → v2

```csharp
[OdinSerialize, ReadOnly, PreviouslySerializedAs("adventureMaterial")]
private int minerita;
```

Odin `[PreviouslySerializedAs]` + [[SaveMigrations]] v1→v2 renombran `AdventureMaterial` → `Minerita`.

**Borrados:** `PassiveMaterial`, `EvolutionEssence` (v1 solo).

## Cambios S129

- **ELIMINADO:** Campo `equipmentGrids` y métodos de equipo (HC-1)
- **ELIMINADO:** `AddEquipment()`, `RemoveEquipmentAt()`, `EquipmentAt()`, `GridFor()`, `CellCountOf()`
- **MANTIENE:** Monedas, muebles, props, hotbar

## Patrón de Acceso

**Nunca mutación directa:**

```csharp
// ❌ NO:
GameManager.CurrentInventory.dabloons -= 50;

// ✅ SÍ:
Wallet.TrySpend(Currency.Dabloons, 50, "buy_ring");
```

[[Wallet]] es la puerta única. Registra en log, dispara `GameEvents.InventoryChanged`, persiste.

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Player/Player Inventory`

## Vinculado a

[[Index/28 - Cimientos y camino a Game Ready]] (§3 · dos monedas)
[[Index/29 - Plan HC - Cimientos (ejecutable)]] (§5 · C3 cartera)

**Conexiones:** [[Wallet]], [[GameManager]], [[SaveSystem]], [[StoreManager]], [[GameEvents]], [[CreatureDisplay]], [[StorePanelUITK]]
