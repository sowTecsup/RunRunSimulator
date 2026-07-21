---
tags: [script, ui, uitk, combat, visualization, order-bar]
---

# CombatOrderBarUITK

**Ruta:** `UI/CombatOrderBarUITK.cs`

**Responsabilidad:** Barra superior de orden de acción para replay 3v3. **S58:** Cartas con headshot lateral (512×192) en lugar de nombre + rol con nombre completo (RoleText) + color por rol + dots de afinidad llenan del UiColor del elemento del MM. OrderCard ganó ElementColor. **S59:** Registra PointerEnter/Leave en slots de carta y emite evento CombatVisualEvents.OnUnitHover(side, index, hover) para que anillos de vida reaccionen a hover de UI.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe OnVisualCombatStart, OnActionOrder, OnUnitAffinity, OnActiveUnit, OnUnitElement |
| `OnDisable()` | Desuscribe eventos |

## Clase Interna: OrderCard (S58)

```csharp
private class OrderCard
{
    public VisualElement Root;
    public VisualElement Slot;
    public VisualElement AllyMarksRow;
    public VisualElement EnemyMarksRow;
    public VisualElement StatesRow;
    public VisualElement AffinityDot0;
    public VisualElement AffinityDot1;
    public Color ElementColor;              // S58 NEW
    public List<CombatElementMark> Marks = new List<CombatElementMark>();
    public List<ElementalState> States = new List<ElementalState>();
}
```

## Cambios S58

**CreateCard() cambios:**
1. **Headshot:** Reemplaza nombre con VisualElement headshot
   - Línea 155-157: `var headshot = new VisualElement(); headshot.AddToClassList("cv-ob-headshot"); MonchiPortraitUI.ApplyHeadshot(headshot, dna);`
   - Tooltip conserva nombre + elemento: `$"{snap.Name} — Elemento: {Identity(element).DisplayName}"`

2. **Rol con nombre completo:** RoleText en lugar de inicial
   - Línea 161-164: `var roleChip = new Label(RoleText(snap.Role)); roleChip.AddToClassList("cv-ob-role-chip"); roleChip.AddToClassList(RoleChipClass(snap.Role));`
   - `RoleText(role)`: retorna "Protector", "Agresivo", "Empático"
   - `RoleChipClass(role)`: retorna clase CSS por rol (color)

3. **ElementColor capturado:**
   - Línea 167: `var elementColor = MarkColor(element);`
   - Guardado en `card.ElementColor` (usado en SetAffinity)

4. **Dots de afinidad con ElementColor:**
   - Línea 354-355: `ApplyAffinityDot(card.AffinityDot0, affinity >= 1, card.ElementColor);`
   - Dots se llenan del UiColor del elemento del MM
   - ApplyAffinityDot: `dot.style.backgroundColor = filled ? (StyleColor)color : StyleKeyword.Null;`

5. **Ancho fijo 160px:** Probablemente en CSS (cv-ob-slot/cv-order-card)

## Cambios S59

**Hover events en BuildTeam():**
- Línea 139-140: 
  ```csharp
  slot.RegisterCallback<PointerEnterEvent>(_ => CombatVisualEvents.UnitHover(side, unitIndex, true));
  slot.RegisterCallback<PointerLeaveEvent>(_ => CombatVisualEvents.UnitHover(side, unitIndex, false));
  ```
- Cada slot registra callbacks de entrada/salida que emiten el evento OnUnitHover
- `unitIndex` capturado por closure (línea 138: `int unitIndex = i;`)
- Propósito: Al hoverar slot de carta, enciende anillo radial de la unidad correspondiente en mundo 3D

**Flujo hover S59:**
1. Mouse entra slot → PointerEnterEvent → CombatVisualEvents.UnitHover(side, index, **true**)
2. CombatRadialHealthBar suscriptor → HandleUnitHover(side, index, hover) → externalHover = true
3. CombatRadialHealthBar.UpdateVisibility() → canvasGroup.alpha fade a 1 (visible)
4. Mouse sale slot → PointerLeaveEvent → CombatVisualEvents.UnitHover(side, index, **false**)
5. externalHover = false → canvasGroup.alpha fade a 0 (invisible)

## Métodos Clave

| Método | Descripción |
|--------|-------------|
| `BuildTeam()` | **S58:** Headshot + RoleText + ElementColor; **S59:** Registra PointerEnter/Leave callbacks emitiendo OnUnitHover |
| `CreateCard()` | **S58:** Headshot + RoleText + ElementColor |
| `RoleText(role)` | "Protector", "Agresivo", "Empático" |
| `RoleChipClass(role)` | Clase CSS por rol (color) |
| `ApplyAffinityDot()` | Llena dot del ElementColor |
| `SetAffinity()` | Actualiza dots con ElementColor |

## Lógica S58–S59

**Cartas reordenable por turno (OnActionOrder):**
- Cada turno, cartas se reordenan a orden de acción
- Headshot visible 512×192 (3/4 lateral yaw 140°)
- Rol completo color-coded (Protector azul, Agresivo rojo, Empático verde)
- Afinidad dots llenan del elemento del MM (p.ej. Agua azul, Fuego naranja)
- Marcas y estados se actualizan per-proc (OnUnitElement)
- **S59:** Hover en slot emite OnUnitHover → anillo world-space reacciona (fade in/out)

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[CombatVisualEvents]] — **S59** publisher OnUnitHover; suscriptor OnActionOrder, OnUnitElement, OnActiveUnit, OnUnitAffinity
- [[CombatRadialHealthBar]] — **S59** suscriptor OnUnitHover, listener de eventos
- [[MonchiPortraitUI]] — ApplyHeadshot
- [[MonchiPortraitService]] — GetHeadshot

## Conexiones

**Entrada:**
- OnVisualCombatStart → BuildCards (contexto, snapshots)
- OnActionOrder → HandleOrder (reordena cartas, ApplyState)
- OnUnitAffinity → HandleAffinity (fill dots)
- OnActiveUnit → HandleActiveUnit (classe active)
- OnUnitElement → HandleUnitElement (actualiza marcas/estados)
- PointerEnterEvent/PointerLeaveEvent → registradas en BuildTeam() (S59 NEW)

**Salida:**
- Barra de orden visual (cartas reordenadas, headshots, roles, marcas, estados)
- Tooltips interactivos
- **S59 NEW:** CombatVisualEvents.OnUnitHover(side, index, hover) emitido al entrar/salir slot

## Notas S58–S59

- Cartas ahora mini-avatares con headshot lateral
- Rol visible en lenguaje natural (no acrónimos)
- Afinidad dots color-coded por elemento (visual consistency)
- Headshot 512×192 comprimido en tarjeta (Cover para crop)
- **S59:** Hover en UI slot activa anillo 3D world-space de la unidad vía evento; CanvasGroup.alpha del anillo controla visibilidad
