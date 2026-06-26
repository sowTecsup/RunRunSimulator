---
tags: [script, equipment, asset]
---

# EquipmentPaletteSO.cs

**Ruta:** `Data/Equipment/EquipmentPaletteSO.cs`

**Responsabilidad:** ScriptableObject de paleta de colores para equipamiento. Expone dos diccionarios Odin `[OdinSerialize]`: `rarityColors` (Rarity → Color pastel, pintando el nombre del ítem) y `slotColors` (EquipmentSlot → Color, acento de borde/diagonal en las cards). API público: `RarityColor(Rarity)` con fallback a `BodyPart.RarityColor()` si la rareza no está en el dict, y `SlotColor(EquipmentSlot)` con fallback a defaults internos por slot (Weapon rojo, Armor azul, Amulet púrpura, otros gris). Botón Odin "Seteo base (pastel)" precarga ambos diccionarios con una paleta suave por defecto. Consumido por `MorimonchiDetailInfoUITK` para colorear las cards de equipo en la tab Equipo.

**Conexiones:** [[EquipmentSO]], [[MorimonchiDetailInfoUITK]], [[Enums]]
