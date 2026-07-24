---
tags: [script, ui, equipment, popup]
---

# EquipmentBackpackUITK

**Ruta:** `UI/EquipmentBackpackUITK.cs`

**Responsabilidad:** Popup/tooltip mochila de equipo — permite equipar items a un MoriMochi desde el inventory del jugador. Grilla 3×3 por página con pestañas, cell 0 = "None" (desequipa), click equipa (swap en DNA), drag&drop free placement. Cierra con click afuera. **S34:** devNoConsume fix simetrico — ambas operaciones (EquipItem y OnNoneCellPointerDown) respetan flag identicamente. **S66:** integración visual con Theme.uss (design system compartido "El Diario del Pet Shop") — popup y ghost llevan clase `mm-theme` + cargan `themeStyleSheet` para resolver tokens `var(--mm-*)`. Sin persistencia directa — muta `inventory`/`dna` y dispara `GameEvents.InventoryChanged + RegistryChanged`.

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

## EquipItem (S34 devNoConsume simétrico)

```csharp
private void EquipItem(int storedIndex, EquipmentSO item)
{
    if (item == null || dna == null) return;

    dna.Equipped ??= new Dictionary<EquipmentSlot, string>();
    string prev = dna.Equipped.TryGetValue(slot, out var prevId) ? prevId : null;
    dna.Equipped[slot] = item.ID;

    if (!devNoConsume)
    {
        inventory.RemoveEquipmentAt(slot, storedIndex);
        if (!string.IsNullOrEmpty(prev))
            inventory.AddEquipment(slot, prev);
        GameEvents.InventoryChanged(inventory);
    }

    GameEvents.RegistryChanged(registry);
    Rebuild();
}
```

**S34 fix:** El bloque de mutación de inventory (`RemoveEquipmentAt`, `AddEquipment`, `InventoryChanged`) está **completamente dentro** del `if (!devNoConsume)`. Cero inconsistencia: si devNoConsume=true, DNA cambia pero inventory nunca se toca.

## OnNoneCellPointerDown (S34 devNoConsume simétrico)

```csharp
private void OnNoneCellPointerDown(PointerDownEvent evt)
{
    if (evt.button != 0) return;
    if (dna?.Equipped == null) return;
    if (!dna.Equipped.TryGetValue(slot, out var prev) || string.IsNullOrEmpty(prev)) return;

    dna.Equipped.Remove(slot);

    if (!devNoConsume)
    {
        inventory.AddEquipment(slot, prev);
        GameEvents.InventoryChanged(inventory);
    }

    GameEvents.RegistryChanged(registry);
    Rebuild();
}
```

**S34 fix:** Idem — `AddEquipment` e `InventoryChanged` **completamente dentro** del `if (!devNoConsume)`. `RegistryChanged` se dispara siempre (para actualizar detail panel), pero inventory untouched si devNoConsume=true.

**Simetría S34:** Ambas operaciones (equipar/desequipar) respetan devNoConsume identicamente:
- Si devNoConsume = false → consumen/devuelven items
- Si devNoConsume = true → solo mutan DNA, inventory intacto

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `inventory` | `PlayerInventorySO` | Fuente de verdad de items |
| `equipmentDatabase` | `EquipmentDatabaseSO` | Resuelve IDs a EquipmentSO |
| `equipmentPalette` | `EquipmentPaletteSO` | Colores rareza + nombre item |
| `styleSheet` | `StyleSheet` | USS clases (backpack__*) — estilos locales del popup |
| `themeStyleSheet` | `StyleSheet` | **S66** Design system compartido (Theme.uss) con tokens `var(--mm-*)`. Se agrega a popup y ghost para que resuelvan colores unificados |
| `devNoConsume` | `bool` | **S34** Si true, equipar no toca inventory (testing). Aplica simétrico a EquipItem y OnNoneCellPointerDown |

## S66: Integración visual con Theme.uss

**Problema:** El popup y el ghost (VisualElement draggable durante drag&drop) viven fuera del árbol principal que lleva la clase `mm-theme`. Sin esa clase, no resuelven los tokens CSS `var(--mm-*)` del design system compartido.

**Solución:**
- **Open():** Popup recibe clase `mm-theme` (`popup.AddToClassList("mm-theme")`) y carga `themeStyleSheet` si existe (`popup.styleSheets.Add(themeStyleSheet)`)
- **CreateGhost():** Ghost recibe la misma clase y stylesheet (líneas 391 y 394)
- **Orden de stylesheets:** `themeStyleSheet` primero, luego `styleSheet` (specificity)

**Impacto:** Popup y ghost son ahora **visualmente coherentes** con el resto del UI bajo "El Diario del Pet Shop" (tokens de color centralizados en Theme.uss).

## Drag&Drop Internals

- **dragStart, DragThreshold (6px):** Umbral para distinguir click de drag
- **ghost:** VisualElement con icon del item, se posiciona bajo cursor. **S66:** Recibe clase `mm-theme` y stylesheet para resolver tokens
- **dragOriginCell/dragTargetCell:** CSS classes para visual feedback
- **pointer capture:** Crítico para confiabilidad
- **Hover tab switching:** `SwitchPageUnderPointer()` detecta hit sobre botones tab durante drag y cambia página (rebuild si ocurre)

## Tabs & Pagination

- Número de páginas = `ceil((cells.Count + 2) / 9.0)` (cell 0 "None" + items)
- Botones tab (1-indexed)
- Click tab dispara `page = p; Rebuild()` → regenera grid + reaplica drag visuals si activos

## Rebuild Pipeline

1. `BuildHeader()` — slot name (Arma/Armadura/Amuleto)
2. `BuildTabs()` — si pageCount > 1, crea botones tab
3. `BuildGrid()` — itera 9 celdas, resuelve items, wirea callbacks
4. `BuildFooter()` — label vacío para hover tooltip
5. `ReapplyDragVisuals()` — si dragging activo, reactiva origen cell si visible

## Eventos Disparados

| Evento | Cuándo | Payload |
|--------|--------|---------|
| `GameEvents.InventoryChanged` | Equipar/desequipar si !devNoConsume, mover en grid | `inventory` |
| `GameEvents.RegistryChanged` | Equipar/desequipar siempre (actualizar DNA) | `registry` |

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

## Notas

- **S34 devNoConsume fix:** Simetrico en EquipItem y OnNoneCellPointerDown — si devNoConsume=true, inventory nunca se toca en ambas operaciones
- **S66 visual unification:** Popup y ghost llevan `mm-theme` + Theme.uss para coherencia con "El Diario del Pet Shop"
- **No persiste directo:** GameManager escucha y persiste automáticamente
- **Pointer capture:** Crítico para drag&drop confiable
- **cell 0 = "None":** Siempre existe, no almacenada (cell lógica)
- **devNoConsume:** Para testing rápido de swaps sin gastar items; ahora simétrico (S34)
