---
tags: [script, ui]
---

# MorimonchiDetailInfoUITK.md

**Ruta:** `UI/MorimonchiDetailInfoUITK.cs`

**Responsabilidad:** Panel de detalle de criatura modal (5 tabs: Info/Combate/Linaje/Descendencia/Equipo). Tabs Info: stats base (6: CON/ATK/SPD/DEF/LCK/EVA con bonus de partes), identidad, personalidad, partes, progresión. Tabs Combate/Linaje/Descendencia: historial + árbol genealógico. **Tab Equipo (Sesión 26-28)**: layout dos columnas — izquierda ScrollView con una card por slot equipable (itera enum `EquipmentSlot`), cada card muestra ícono (sprite o `IconColor` si no hay), nombre coloreado por rareza (vía `EquipmentPaletteSO.RarityColor()`), slot y rareza, descripción multilínea, y desglose de efectos (tanto StatModifierEffect como CombatProcEffect — Etapa 1 display solo); borde-izquierdo coloreado por slot (via `EquipmentPaletteSO.SlotColor()`), diagonal interior a la mitad en el color de rareza (pintada con `Painter2D`). Derecha: swatch del MM (BaseColor) + stats Base→Final (6 stats), mostrando deltas del equipo via `EquipmentStats.Apply()`. Tab Info también aplica `EquipmentStats.Apply()` para que los stats reflejen equipo. `IUINavigable` (A/D cambian tabs).

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[UIManager]], [[CreatureGridUITK]], [[CreatureDNA]], [[CreatureVisualUI]], [[CombatService]], [[EquipmentStats]], [[EquipmentDatabaseSO]], [[EquipmentPaletteSO]], [[EquipmentSO]], [[EquipmentEffectBase]], [[StatModifierEffect]], [[CombatProcEffect]], [[GameManager]]

**Campos públicos / eventos:**
- `OnCreatureSelected` (evento de UIManager): trae `CreatureDNA` + `CreatureRegistrySO`
- Campos serializados: `database` (CreatureDatabaseSO), `equipmentDatabase` (EquipmentDatabaseSO), `equipmentPalette` (EquipmentPaletteSO), `sortingOrder`
- Campos UI: `statCon`, `statAtk`, `statSpd`, `statDef`, `statLck`, `statEva` (Labels para los 6 stats)
- `portrait` (VisualElement, fondo ColorBase)
- `tabs` (TabView), `closeButton`
- **Equipo tab**: `teamPortrait` (swatch MM), `equipCards` (ScrollView para las cards), `equipStats` (VisualElement con breakdown de stats)

**Lógica clave:**
- `Wire()` busca elementos en UIDocument por `name`: stats ("stat-con", etc), tabs ("tabs"), equipo ("equip-portrait", "equip-cards", "equip-stats"), etc.
- `Populate()` calcula stats base vía `CombatService.GetEffectiveStats(dna, database)` luego los finales con `EquipmentStats.Apply()`. Tab Info muestra estos stats finales (con bonus de partes+equipo).
- `SetStat(label, name, final, baseVal)` mostrará `CON 45 (40 + 5)` con el bonus calculado
- `BuildEquipment()` → `BuildEquipCards()` (itera slots, pinta cards con icon/rareza/descripción/efectos) + `BuildEquipStats()` (computa Base→Final con delta)
- `AddEquipCard()` resuelve el item vía `ResolveEquip(dna, slot)` contra `equipmentDatabase`, pinta diagonal interior vía `PaintDiagonal(ctx, dc)` con el color de rareza, borde-izquierdo con color de slot. Muestra efectos (StatModifierEffect + CombatProcEffect polimórficos) vía `Summary()` de cada uno.
- `BuildCombatHistory()` muestra combates en pestaña "Combate" (uno por foldout, más reciente primero)

**Organización (partial class):**
- `MorimonchiDetailInfoUITK.cs` — núcleo + Info + Combat tab + SetStat
- `MorimonchiDetailInfoUITK.Trees.cs` — tabs Linaje/Descendencia
