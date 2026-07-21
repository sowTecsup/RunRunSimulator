---
tags: [script, ui, grid, equipment]
---

# CreatureGridUITK

**Ruta:** `UI/CreatureGridUITK.cs`

**Responsabilidad:** Grid UITK de criaturas (residente en UIManager siempre-activo, NO en UIDocument; referencia documento serializado). Popula desde evento registry, cards clonadas de UXML template. Soporta selección keyboard/gamepad (IUINavigable: A/D = horiz, W/S = vert), Submit = abre detail. Event-driven, cero referencias gameplay directas. **S33:** Cada card muestra fila de 3 iconos equipo DISPLAY-ONLY (BindEquipSlot; sin interacción — click sigue abriendo detail). **S57b:** Icono swatch principal ahora es retrato fotomatón vía [[MonchiPortraitUI]].Apply().

## Descripción General

**Grid paginado con scroll:** Cards 120×150px (configurable) que wrappean y scrollean. Newest MoriMonchis primero (OrderByDescending BirthDate). UIManager mantiene este script siempre subscrito a `GameEvents.OnRegistryChanged` y `OnRegistryReloaded`, así que al abrir grid panel (que oculta via `display`, no via GameObject inactive) la UI está **instantáneamente fresca**.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | El panel UITK (UIDocument en panel object separado) |
| `panel` | `UIPanelType` | Enum: CreatureGrid (routing UIManager) |
| `cardTemplate` | `VisualTreeAsset` | CreatureCardUITK.uxml, clonada per MM |
| `cardSize` | `Vector2` | 120×150 (configurable — knob card size) |
| `equipmentDatabase` | `EquipmentDatabaseSO` | **S33** Resuelve IDs → EquipmentSO para equip-row |
| `equipmentPalette` | `EquipmentPaletteSO` | **S33** Colores slot para equip-row swatches |

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `scroll` | `ScrollView` | Container "grid-container", resolved lazy |
| `closeButton` | `Button` | In-UI close button, wired en Start |
| `cards` | `List<VisualElement>` | Tarjetas en orden display (newest first) |
| `currentRegistry` | `CreatureRegistrySO` | Registry del último Rebuild (para Submit) |
| `selectedIndex` | `int` | -1 si grid vacío, else 0..cards.Count-1 |

## Flujo de Rebuild

```
GameEvents.OnRegistryChanged(registry) o OnRegistryReloaded(registry)
  → Rebuild(registry)
    → ResolveContainer() — Q<ScrollView>("grid-container")
    → container.Clear() + cards.Clear()
    → foreach mm in registry.GetAll().Values.OrderByDescending(BirthDate):
        → cardTemplate.Instantiate() → Q<VisualElement>("card")
        → BindCard(card, dna)
          → BindIdentity(dna) — nombre/rarity display
          → BindStats(dna) — 6 stats base
          → BindEquipSlot(dna, Weapon/Armor/Amulet) — S33, icon trio
          → **S57b:** MonchiPortraitUI.Apply(iconElement, dna) — retrato fotomatón
          → RegisterCallback<ClickEvent>(...) — open detail
    → cards.Add(card)
    → container.Add(card)
    → Select(0) si cards.Count > 0
```

## Equip Row — S33

**Display-only row de 3 iconos, sin interacción:**

```csharp
private void BindEquipSlot(CreatureDNA dna, EquipmentSlot slot)
{
    // Resuelve equipped ID desde dna.Equipped[slot]
    // Busca icon en equipmentDatabase
    // Crea VisualElement con background-image = icon
    // Color borde/swatch via equipmentPalette.SlotColor(slot)
    // Registra SOLO PointerEnter/Leave para tooltip (future)
    // NO click listener — click en card abre detail, no abre backpack aquí
}
```

Iterado 3 veces (Weapon, Armor, Amulet) → fila de 3 celdas equipadas bajo stats.

## Métodos IUINavigable

| Método | Input | Efecto |
|--------|-------|--------|
| `OnUINavigate(dir)` | Vector2 dir (A/D/W/S / stick) | Mueve selección: x = ±1 col, y = ±cols filas (ColumnsPerRow). Select(idx). Scroll automático. |
| `OnUISubmit()` | Enter / gamepad A | Abre detail: `UIManager.SelectCreature(cards[selectedIndex].userData as CreatureDNA, currentRegistry)` |
| `OnUICancel()` | ESC / gamepad B | Retorna false — UIManager cierra grid |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `Rebuild(registry)` | Wipes container, clona cards from template, Bind, Select(0) |
| `ResolveContainer()` | `document?.rootVisualElement?.Q<ScrollView>("grid-container")`, lazy cache |
| `WireCloseButton()` | Q<Button> "close-button", registra clicked |
| `BindCard(card, dna)` | Llama BindIdentity, BindStats, BindEquipSlot (x3), **S57b:** MonchiPortraitUI.Apply(icon, dna), RegisterCallback click |
| `BindIdentity(card, dna)` | Settea Q<Label> "name", "rarity", "customization-count", colors |
| `BindStats(card, dna)` | Settea 6 labels CON/ATK/SPD/DEF/LCK/EVA via CombatStats.GetEffectiveStats |
| `BindEquipSlot(dna, slot)` | **S33** Popula equip icon, color, tooltip. |
| `Select(idx)` | Clamp idx, selecciona card, applica CSS class "card--selected", ScrollTo visibilidad |
| `ColumnsPerRow()` | Calcula cuántas cards caben por fila (medidas vivas del layout) |
| `OnCloseClicked()` | Wrapper para close button → `UIManager.RequestPanelSet(panel, false)` |

## Métodos Públicos (IUINavigable)

```csharp
public void OnUINavigate(Vector2 dir) { ... }
public void OnUISubmit() { ... }
public bool OnUICancel() => false;
```

## Suscripciones

**OnEnable:**
```csharp
GameEvents.OnRegistryChanged  += Rebuild;
GameEvents.OnRegistryReloaded += Rebuild;
```

**OnDisable:**
```csharp
GameEvents.OnRegistryChanged  -= Rebuild;
GameEvents.OnRegistryReloaded -= Rebuild;
```

**OnDestroy:**
```csharp
UIManager.UnregisterNavigable(panel);
```

## Vinculado a

- [[Index/05 - UI System]]
- [[UIManager]] — orquestador, routing panels, solicita navegable
- [[CreatureRegistrySO]] — fuente de criaturas (event payload)
- [[CreatureDNA]] — userData en card, abierto al click
- [[MorimonchiDetailInfoUITK]] — abierto al Submit/click
- [[CombatStats]] — calcula stats base display
- [[EquipmentDatabaseSO]], [[EquipmentPaletteSO]] — **S33** resuelven equip display
- [[MonchiPortraitUI]] — **S57b** pinta retrato en card
- [[GameEvents]] — suscriptor OnRegistryChanged + OnRegistryReloaded

## Conexiones

**Entrada:**
- `GameEvents.OnRegistryChanged(registry)` → `Rebuild(registry)`
- `GameEvents.OnRegistryReloaded(registry)` → `Rebuild(registry)` (clear + resync sin push)
- `UIManager.RequestPanelSet(CreatureGrid, true)` → hace visible grid
- Keyboard/gamepad → `OnUINavigate()`, `OnUISubmit()`, `OnUICancel()`

**Salida:**
- Click card / Submit → `UIManager.SelectCreature(dna, registry)` → abre MorimonchiDetailInfoUITK

## Notas

- **Siempre activo:** Script MonoBehaviour en UIManager, no en UIDocument (critica para persistencia de suscripción)
- **Document hidden via display:** UIDocument panel es inactive-false (para keep alive), pero display:none cuando cerrado — Rebuild sigue ejecutándose en background
- **S57b Portrait swatch:** Retrato fotomatón vía MonchiPortraitUI.Apply() en lugar de backgroundColor BaseColor
- **S33 Equip row:** Display-only (RGBA colores borde, icon bg). Click en card sigue abriendo detail (donde equip-cards son clickeables → backpack popup). Aquí es solo vistazo rápido de lo equipado.
- **Newest first:** OrderByDescending BirthDate — recién creados arriba
- **Scroll auto:** Select() llama ScrollTo() con ScrollVisibility — keep selection on screen
