---
tags: [script, ui, combat]
---

# CombatLineupUITK

**Ruta:** `UI/CombatLineupUITK.cs`

**Responsabilidad:** Componente sibling de `CombatPanelUITK` (mismo GameObject UIManager, UIDocument compartido). Implementa tab "Equipo 3v3" del Combat Panel (Fase 2 autobattler 3v3, S37). Instancia dos `CombatLineupBoard` (A/B, espejadas), gestiona carrusel de selección de MoriMonchis elegibles (pool con exclusión de colocados), drag & drop por punteros entre pool/board/board, click derecho = overlay de detalle (stats Base→Final + equipo), rosters laterales ordenados por SPD efectiva, botón "¡Pelear!" que invoca `CombatController.SimulateLocal(idsA, idsB, rowsA, rowsB)` con lineup y muestra resultado en overlay. Suscribe a `GameEvents.OnRegistryChanged/OnRegistryReloaded` para prunear no-elegibles. Responsive: SetSlotSize via GeometryChangedEvent. **S57b:** Cartas del pool y ghost del drag usan retrato fotomatón vía [[MonchiPortraitUI]].Apply().

**S39 Cambio:** Chips de Elemento ahora muestran el **elemento real del DNA** vía helper `ElementText`, no placeholder "—".

## Componentes Hermanos en UIDocument

- **CombatPanelUITK** — tabs 0/1/2 (Online/Resultados/Historial)
- **CombatLineupUITK** — tab 3 (Equipo 3v3, este componente)

## Método ElementText (S39)

```csharp
private static string ElementText(CreatureDNA dna) =>
    dna != null ? dna.Element.ToString() : "—";
```

**Cambio S39:** Chips de Elemento en cartas pool/board/roster ahora muestran `dna.Element` (enum Element: None, Vaporizado, Fuego, Agua, Tierra, Eléctrico, etc.) en lugar de placeholder genérico.

## Vinculado a

- [[Index/05 - UI System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatLineupBoard]] — boards A/B espejados
- [[CombatController]] — `SimulateLocal(idsA, idsB, rowsA, rowsB)`
- [[GameEvents]] — suscriptor RegistryChanged/Reloaded
- [[CreatureDatabaseSO]] — stats
- [[EquipmentDatabaseSO]] — items
- [[CreatureRegistrySO]] — lista de criaturas
- [[CombatManagerSO]] — config combate
- [[CombatResult]] — resultado mostrado en overlay
- [[Element]] — enum elementos (S39)
- [[MonchiPortraitUI]] — **S57b** pinta retratos en cartas pool/board

## Conexiones

**Entrada:**
- `GameEvents.OnRegistryChanged/Reloaded` → prunea pool no-elegibles
- Drag events (PointerDown/Move/Up)
- Click events (detail overlay)
- Button "¡Pelear!" → SimulateLocal

**Salida:**
- `CombatController.SimulateLocal(idsA, idsB, rowsA, rowsB)`
- Result overlay visual

## Notas (S37 + S39 + S57b)

- **Sibling pattern:** Comparte UIDocument con CombatPanelUITK; gestión separada de tab 3.
- **S37 diseño:** Grilla 2-3-2 + lineup editor. Fase 1 (data + UI) completa; Fase 2+ (replay 3v3, etc.) pending.
- **S39 Element display:** Chips ahora muestran elemento real (no "—" placeholder).
- **S57b Portrait cards:** Retratos fotomatón en pool/board/ghost via MonchiPortraitUI.Apply() en lugar de backgroundColor BaseColor
- **Drag & Drop:** Pointer-driven (no mouse-specific); soporta touch.
- **Detail overlay:** Click card abre detalles (rol + elemento + stats + equipo).
