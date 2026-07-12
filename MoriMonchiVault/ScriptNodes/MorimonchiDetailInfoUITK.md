---
tags: [script, ui, creature-detail, partial]
---

# MorimonchiDetailInfoUITK

**Ruta:** `UI/MorimonchiDetailInfoUITK.cs` (partial class)

**Responsabilidad:** Panel modal detalle de criatura (5 tabs: Info/Combate/Linaje/Descendencia/Equipo). **Tab Info:** stats base con bonus de partes (6: CON/ATK/SPD/DEF/LCK/EVA), identidad, **rol** (S39), **elemento** (S39), partes, progresión. **Tab Combate (S33+S34):** historial con tarjetas compact — badge, fecha, rival con propietario, swatch color + stats compact de ambos + chips de tiers, comentario y boton replay. **Tab Linaje/Descendencia:** árbol genealógico. **Tab Equipo (S26-28):** dos columnas — izquierda ScrollView con card por slot, derecha swatch MM + stats Base→Final. `IUINavigable` (A/D cambian tabs). **S39 cambios:** Sección "Rol y Elemento" reemplaza "Personalidad"; ModifiersText itera `ItemUseEffect` (S39), no `CombatProcEffect` (deprecated).

## Cambios S39

**Tab Info — Nueva sección "Rol y Elemento":**
```csharp
// Antes: roleElementLabel (personalidad)
// Ahora: roleElementLabel con 2 chips
var roleText = $"{RoleDisplay(dna.Role)} · {ElementDisplay(dna.Element)}";
roleElementLabel.text = roleText;  // "Protector · Fuego" (ejemplo)
```

**Query UXML:** `"role-element-label"` (antes `"personality-label"`)

**Métodos helpers:**
```csharp
private static string RoleDisplay(Role r) => r switch
{
    Role.Protector => "Protector",
    Role.Agresivo => "Agresivo",
    Role.Empatico => "Empático",
    _ => r.ToString()
};

private static string ElementDisplay(Element e) => e switch
{
    Element.None => "Sin elemento",
    Element.Fuego => "Fuego",
    Element.Agua => "Agua",
    Element.Tierra => "Tierra",
    Element.Electrico => "Eléctrico",
    Element.Vaporizado => "Vaporizado",
    Element.Hielo => "Hielo",
    _ => e.ToString()
};
```

**Tab Info — ModifiersText actualizado (S39):**
- Antes: iteraba `CombatProcEffect` (lista de procs de equipment)
- Ahora: itera `ItemUseEffect` (efectos polimórficos de equipment, S39)

```csharp
// S39 version
var modText = "";
if (dna.Equipped != null && equipmentDatabase != null)
{
    var usedEffects = new List<ItemUseEffect>();
    foreach (var slotId in dna.Equipped.Keys)
    {
        var itemId = dna.Equipped[slotId];
        var itemSo = equipmentDatabase.GetItem(itemId);
        if (itemSo?.Effects != null)
            usedEffects.AddRange(itemSo.Effects);
    }
    if (usedEffects.Count > 0)
        modText = string.Join(", ", usedEffects.Select(e => e.DisplayName));
}
modifiersLabel.text = modText;
```

## Tabs

| Tab | Contenido |
|-----|----------|
| **Info** | Stats (base+equipment), identidad, **rol** (S39) + **elemento** (S39), partes, progresión |
| **Combate (S34)** | Historial con tarjetas compactas — badge, fecha, rival, swatch + stats compactos, tiers, comentario, boton replay |
| **Linaje** | Árbol genealógico (padres/abuelos) |
| **Descendencia** | Árbol de crías |
| **Equipo** | Slots equipables (clickeable), stats Base→Final con delta |

## Organización (partial class — Deuda Activa)

| Archivo | Responsabilidad |
|---------|-----------------|
| `MorimonchiDetailInfoUITK.cs` | Núcleo, Info (S39), Combat (S34), Equipo (S33) |
| `MorimonchiDetailInfoUITK.Trees.cs` | Tabs Linaje/Descendencia |

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `titleLabel` | `Label` | Nombre |
| `statCon`, `statAtk`, `statSpd`, `statDef`, `statLck`, `statEva` | `Label` | 6 stats |
| `roleElementLabel` | `Label` | **S39** Sección "Rol y Elemento" (antes `personalityLabel`) |
| `identityLabel`, `progressionLabel` | `Label` | ID + progresión |
| `portrait`, `partsContainer` | `VisualElement` | Visuales |
| `combatHistory` | `ScrollView` | Tab Combate (tarjetas con replay) |
| `equipCards`, `equipStats` | `ScrollView`, `VisualElement` | Tab Equipo |
| `current` | `CreatureDNA` | Criatura actualmente mostrada |
| `registry` | `CreatureRegistrySO` | Registry al momento de Show() |

## Métodos Principales (S34 nuevos)

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `BuildCombatHistory(dna)` | `void` | Itera CombatHistory newest-first, agrega vía BuildCombatCard |
| `BuildCombatCard(dna, record)` | `VisualElement` | **S34 reescrito** Card compacta con stats+tiers+comentario+replay |
| `BuildCombatColumn(title, snapshot, fallback)` | `VisualElement` | **S34** 2 lineas stats + swatch color + chips tiers |
| `SnapshotColor(snapshot, fallback)` | `Color` | **S34** Resuelve ColorHex del snapshot |
| `AddTierChips(col, snapshot)` | `void` | **S34** Agrega chips de tiers >1 |
| `CommentText(record)` | `string` | **S34** Linea descriptiva del outcome |

## Vinculado a

- [[Index/05 - UI System]]
- [[CreatureGridUITK]] — abre este panel
- [[EquipmentBackpackUITK]] — popup
- [[CombatReplayRequest]] — S34 replay request button
- [[CreatureDNA]] — fuente de datos (Role S39, Element S39)
- [[CombatStats]], [[EquipmentStats]] — cálculos
- [[CombatRecord]], [[CombatFighterSnapshot]] — S33/S34 stats + tiers
- [[ItemUseEffect]] — efectos equipment (S39)
- [[Role]] — enum (S39)
- [[Element]] — enum (S39)
- [[GameEvents]] — suscriptor

## Conexiones

**Entrada:**
- `UIManager.OnCreatureSelected` → `Show()` + `Populate()`
- `GameEvents.OnRegistryChanged` → `OnRegistryChanged()` → re-Populate

**Salida:**
- UI visual (5 tabs, cards, combat replay)
- `CombatReplayRequest.Request()` → carga escena combate (S34)

## Notas

- **S34 Combat tab:** Tarjetas compactas con swatch color (ColorHex), 2 lineas stats, chips tiers, comentario contextual, boton replay.
- **S39 cambios:** Tab Info ahora muestra Rol + Elemento (sección "Rol y Elemento"). ModifiersText itera ItemUseEffect polimórficos.
- **Newest first:** Historial ordenado por fecha descendente (combates recientes primero).
- **Boton replay:** ▶ en footer, valida CanReplay antes de habilitar.
- **Backward compat:** Records viejos (sin ColorHex/tiers) muestran gracefully con fallbacks.
- **Registry validación:** Replay button revalidado en cada render para captar cambios (rival vendido/muerto, etc.).
- **Partial class:** Deuda activa (Fase 8, refactor a componentes pequeños).
