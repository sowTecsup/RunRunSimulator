---
tags: [script, ui, presenter]
---

# DetailEquipTabPresenter.cs

**Ruta:** `UI/DetailEquipTabPresenter.cs`

**Responsabilidad (S54):** Presenter colaborador de MorimonchiDetailInfoUITK — tab "Equipo" (3 slots: Arma/Armadura/Amuleto, mostrar items equipados + stats bonificados, abrir mochila al clickear). Implementa ro `Rebuild(dna)` — no navegación. **S93:** Usa helpers `CreatureDisplay.RarityColor()` y `CreatureDisplay.ApplyIconVisual()`.

**Datos UI:**
- `teamPortrait` (VisualElement retrato fotomatón vía [[MonchiPortraitUI]].Apply())
- `equipCards` (ScrollView, 3 cards por slot)
- `equipStats` (tabla 6x2 mostrando Base → Final con bonificación de items)

**Cards por slot:**
- Empty slot: "Slot: vacío" + gris
- Filled slot: icon (sprite o color fallback) + nombre (ID o Name) + rarity color + descripción + efectos (StatModifierEffect) + modificadores (ItemUseEffect) + diagonal accent en rarity color (45°, opacidad 0.5)
- Border left color by slot
- Click abre `EquipmentBackpackUITK` con slot + card visual ref + registry

**Construcción:**
- `BuildEquipCards()` — itera 3 slots (Weapon/Armor/Amulet), agrega card
- `AddEquipCard()` — resuelve item vía `ResolveEquip()`, construye card completa
- `PaintDiagonal()` — dibuja wedge 45° derecho-abajo (rarity color) usando Painter2D (para accent visual)
- `EffectsText()` — formatea StatModifierEffect como "• [Summary]" (multiline)
- `ModifiersText()` — formatea ItemUseEffect como "◆ [Summary]" (multiline, procs)
- `BuildEquipStats()` — tabla 6 filas (CON/ATK/SPD/DEF/LCK/EVA) mostrando base (from parts/tier) + final (con bonificación equipo). **S75:** usa `CreatureStats.GetEffectiveStats(dna, database)` en lugar de CombatStats
- `AddStatRow()` — label "NAME base → final" con clases `equip-stat__val--up` (verde) o `equip-stat__val--down` (rojo)
- `SlotName()` — "Arma"/"Armadura"/"Amuleto"
- `RarityColor()` / `SlotColor()` — via `equipmentPalette` o fallback BodyPart.RarityColor

**Métodos públicos:**
- `Rebuild(dna)` — limpia cards + stats, rebuildCards + rebuildStats. **S57b:** Pinta teamPortrait via MonchiPortraitUI.Apply(element, dna)

**Callbacks:**
- Card click → abre backpack (sin teardown de presenters, card solo callback)

**Conexiones:** [[MorimonchiDetailInfoUITK]], [[EquipmentBackpackUITK]], [[EquipmentStats]], [[CreatureDatabaseSO]], [[EquipmentDatabaseSO]], [[EquipmentPaletteSO]], [[MonchiPortraitUI]], [[CreatureStats]], [[CreatureDisplay]]
