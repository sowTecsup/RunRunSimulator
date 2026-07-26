---
tags: [script, world, props]
---

# HotbarController.cs

**Ruta:** `World/Props/HotbarController.cs`

**Responsabilidad:** Hotbar 6-slots en modo play. Pickup de `WorldPropInstance`, uso, throw, drop. Singleton `Instance` + eventos estáticos. **S69:** Nueva propiedad `IsOfferingFood` (bool, true si slot activo contiene ítem con Category==Food). Nuevo método `TryConsumeActiveFood() → bool` (si hotbar activo es Food, quita del slot, destruye visual en mano, emite `OnHotbarChanged` + `InventoryChanged`, NO dispara `OnItemUsed`). Consumo por AgentBrain.TickHandFeed() para comida de la mano.

## Estructura

**Slots (0–5):**
- Cada slot contiene `WorldPropInstance` o null
- Un slot activo (default 0)
- Visual en mano cuando ocupado

**Singleton:**
- `HotbarController.Instance` — acceso global

## Propiedades

**Estado actual:**
- `ActiveSlotIndex` (int) — slot activo (0–5)
- `GetActiveItem() → WorldPropInstance` — retorna ítem en slot activo (null si vacío)
- `GetSlotCount() → int` — retorna 6

**S69 NUEVAS:**
- `IsOfferingFood` (bool) — **read-only** true si slot activo contiene ítem con `ItemDefinitionSO.Category == ItemCategory.Food`
- `Count` (int) — cantidad de ítems ocupados en hotbar

## Métodos

**Gestión de slots:**
- `TryEquipItem(WorldPropInstance prop) → bool` — intenta meter ítem en el hotbar (busca slot vacío)
- `RemoveItem(int slotIndex)` — quita ítem de slot
- `GetItem(int slotIndex) → WorldPropInstance` — lee ítem
- `SetActiveSlot(int slotIndex)` — cambio de slot activo, maneja visual en mano
- `SwapSlots(int a, int b)` — permuta slots

**Uso:**
- `TryUseActiveItem()` — **S69 DEPRECIADO/CAMBIADO** ya no es la mecánica para food (ver TryConsumeActiveFood)
- `TryThrowActiveItem()` — lanza ítem en mano del jugador
- `DropActiveItem()` — suelta a los pies

**S69 NUEVOS:**
- `IsOfferingFood` (property) — true si `GetActiveItem()?.Definition?.Category == ItemCategory.Food`
- `TryConsumeActiveFood() → bool` — **core S69**:
  1. Si `!IsOfferingFood`, retorna false
  2. Guarda ID del ítem activo
  3. Quita slot (RemoveItem)
  4. Destruye visual en mano (WorldPropInstance)
  5. Emite `OnInventoryChanged` (GameEvents)
  6. Emite `OnHotbarChanged` (UIManager)
  7. Retorna true
  8. **NO dispara** `OnItemUsed` (que sería para equipo; food es consumo)

## Cambios S69

**Nueva propiedad:**
```csharp
public bool IsOfferingFood
{
    get
    {
        var item = GetActiveItem();
        return item != null && 
               item.Definition != null && 
               item.Definition.Category == ItemCategory.Food;
    }
}
```

**Nuevo método:**
```csharp
public bool TryConsumeActiveFood()
{
    if (!IsOfferingFood) return false;
    
    var itemId = GetActiveItem()?.ItemId;
    RemoveItem(ActiveSlotIndex);
    
    // cleanup visual
    if (currentVisual != null) 
        Destroy(currentVisual.gameObject);
    currentVisual = null;
    
    // events
    GameEvents.InventoryChanged(GameManager.Instance.Inventory);
    UIManager.OnHotbarChanged?.Invoke();
    
    return true;
}
```

**Consumo por AgentBrain:**
```csharp
// En TickHandFeed() luego de comer
if (HotbarController.Instance.TryConsumeActiveFood())
{
    ctx.Dna?.Needs.AddHealth(owner.feedHealthBoost);
    ctx.Dna?.Needs.AddAffect(owner.feedAffectBoost);
}
```

## Eventos

**Estáticos (UIManager):**
- `OnHotbarChanged` (Action) — emitido tras cambio de contenido o slot
- `OnActiveSlotChanged` (Action<int>) — emitido tras cambio de slot activo

**Globales (GameEvents):**
- `OnInventoryChanged` (creatureRegistry) — emitido tras consumo (actualiza UI de inventario)

## Campos Tuning

- `slotCount` — readonly 6
- Prefabs de slot visual (en escena o asset)

## Notas S69

- `IsOfferingFood` es check barato (GetActiveItem + null/Category == Food)
- `TryConsumeActiveFood()` es IDEMPOTENT: si no hay comida activa, retorna false sin efectos
- `TryConsumeActiveFood()` **NO dispara** `OnItemUsed` (ese evento es para equipo consumible; food es handFeed)
- Diseño: separación clara entre "uso de ítem" (key, consumible) vs "consumo de comida" (handFeed, stream de petting)
- Flujo S69: PlayerController.EndPetting() NO consume; es AgentBrain.TickHandFeed() quien consume al terminar duración eat

## Vinculado a

[[Index/06 - Player & World]]

## Conexiones

[[WorldPropInstance]], [[ThrowableObject]], [[PlayerInputs]], [[HotbarHUDUITK]], [[AgentBrain]], [[GameEvents]], [[UIManager]]
