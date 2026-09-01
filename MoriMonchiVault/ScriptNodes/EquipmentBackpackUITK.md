---
tags: [script, ui, equipment, popup]
---

# EquipmentBackpackUITK

**Ruta:** `UI/EquipmentBackpackUITK.cs`

**Responsabilidad:** Popup/tooltip mochila de equipo — permite equipar items a un MoriMochi desde el inventory del jugador. Grilla 3×3 por página con pestañas, cell 0 = "None" (desequipa), click equipa (swap en DNA), drag&drop free placement. Cierra con click afuera. **S34:** devNoConsume fix simetrico — ambas operaciones (EquipItem y OnNoneCellPointerDown) respetan flag identicamente. **S66:** integración visual con Theme.uss (design system compartido "El Diario del Pet Shop") — popup y ghost llevan clase `mm-theme` + cargan `themeStyleSheet` para resolver tokens `var(--mm-*)`. Sin persistencia directa — muta `inventory`/`dna` y dispara `GameEvents.InventoryChanged + RegistryChanged`. **S93:** Usa helpers `CreatureDisplay` (RarityColor, ApplyRarityBorder, ApplyIconVisual).

## API Pública

| Método | Descripción |
|--------|-------------|
| `Open(dna, slot, anchor, registry)` | Abre popup posicionado junto al `anchor` VisualElement |
| `Close()` | Cierra popup, limpia referencias, desuscribe callbacks |

## Flujo de Interacción

1. **Popup positioning:** Se posiciona a la derecha del anchor (+8px); si sale de pantalla, se restringe al bounds
2. **Grid paginada:** 3×3 (GridSize=9), cell 0 siempre = "None" (desequipa), resto = items del grid libre
3. **Click cell:**
   - Cell "None" → desequipa (quita de `dna.Equipped[slot]`, devuelve prev item a inventory si !devNoConsume)
   - Cell con item → equipar (remueve del inventory, agrega prev equipado al inventory si !devNoConsume, actualiza DNA)
4. **Drag&drop:** Idem que antes, grilla libre placement
5. **Close:** Click afuera del popup

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `inventory` | `PlayerInventorySO` | Fuente de verdad de items |
| `equipmentDatabase` | `EquipmentDatabaseSO` | Resuelve IDs a EquipmentSO |
| `equipmentPalette` | `EquipmentPaletteSO` | Colores rareza + nombre item |
| `styleSheet` | `StyleSheet` | USS clases (backpack__*) — estilos locales del popup |
| `themeStyleSheet` | `StyleSheet` | **S66** Design system compartido (Theme.uss) con tokens `var(--mm-*)`. Se agrega a popup y ghost para que resuelvan colores unificados |
| `devNoConsume` | `bool` | **S34** Si true, equipar no toca inventory (testing). Aplica simétrico a EquipItem y OnNoneCellPointerDown |

## Drag&Drop Internals

- **dragStart, DragThreshold (6px):** Umbral para distinguir click de drag
- **ghost:** VisualElement con icon del item, se posiciona bajo cursor. **S66:** Recibe clase `mm-theme` y stylesheet para resolver tokens
- **dragOriginCell/dragTargetCell:** CSS classes para visual feedback
- **pointer capture:** Crítico para confiabilidad
- **Hover tab switching:** `SwitchPageUnderPointer()` detecta hit sobre botones tab durante drag y cambia página (rebuild si ocurre)

## Vinculado a

- [[Index/05 - UI System]]
- [[MorimonchiDetailInfoUITK]] — abre desde AddEquipCard (click en card)
- [[PlayerInventorySO]] — mutador, lee grid via GetEquipment()
- [[CreatureDNA]] — mutador via dna.Equipped[slot]
- [[EquipmentDatabaseSO]] — resuelve ID → EquipmentSO
- [[EquipmentPaletteSO]] — rareza/slot colors
- [[GameEvents]] — dispara InventoryChanged + RegistryChanged

## Conexiones

**Entrada:**
- `MorimonchiDetailInfoUITK.AddEquipCard()` → `backpack.Open()` (click en card)

**Salida:**
- Muta `PlayerInventorySO` (inventory grids) si !devNoConsume
- Muta `CreatureDNA.Equipped` (dict EquipmentSlot → ID) siempre
- Dispara `GameEvents.InventoryChanged(inventory)` si !devNoConsume + `RegistryChanged(registry)` siempre

**Helpers (S93):** [[CreatureDisplay]]
