---
tags: [script, ui, uitk, combat, visualization, order-bar, animations]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatOrderBarUITK

**Ruta:** `UI/CombatOrderBarUITK.cs`

**Responsabilidad:** Barra superior de orden de acción para replay 3v3. **S61b:** Animaciones UITK nuevas con `using UnityEngine.UIElements.Experimental`: `PopIn(el, fromScale, durationMs)` easeOutBack (escala fromScale→1 + fade 0→1), puntos de afinidad 2.6f scale/420ms al llenar (detectado vía OrderCard.Affinity previo), chips de marcas/estados 1.9f scale/320ms con flags reactedNew/armedNew para evitar no-ops, carta activa pop 1→1.10→1 en 240ms al tomar turno. Tooltip dinámico `StatsTooltip(card)` armado al momento del hover con Nombre/Elemento/Rol, **HP actual** (OrderCard.CurrentHp actualizado por OnUnitHpChanged, sincronizado en ApplyState desde CombatUnitState.Hp), MaxHp, Attack, Defense, Speed, Luck, Evasion del CombatFighterSnapshot. **S58:** Cartas con headshot lateral (512×192) en lugar de nombre + rol con nombre completo (RoleText) + color por rol + dots de afinidad llenan del UiColor del elemento del MM. OrderCard ganó ElementColor. **S59:** Registra PointerEnter/Leave en slots de carta y emite evento CombatVisualEvents.OnUnitHover(side, index, hover) para que anillos de vida reaccionen a hover de UI.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe OnVisualCombatStart, OnActionOrder, OnUnitAffinity, OnActiveUnit, OnUnitElement, OnUnitHpChanged |
| `OnDisable()` | Desuscribe eventos |

## Clase Interna: OrderCard (S58 + S61b)

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
    public Color ElementColor;                           // S58 NEW
    public CombatFighterSnapshot Snap;                   // S61b NEW
    public Element Element;                              // S61b NEW
    public float CurrentHp;                              // S61b NEW
    public int Affinity;
    public List<CombatElementMark> Marks = new List<CombatElementMark>();
    public List<ElementalState> States = new List<ElementalState>();
}
```

## Cambios S61b (Animaciones UITK, Tooltip dinámico con HP)

**OrderCard cambios:**
- Nueva propiedad `Snap` (CombatFighterSnapshot) — snapshot stats (Name, MaxHp, Attack, Defense, Speed, Luck, Evasion, Role)
- Nueva propiedad `Element` (Element) — elemento del MM (capturado en CreateCard)
- Nueva propiedad `CurrentHp` (float) — HP actual, actualizado en HandleUnitHp

**Animación PopIn() helper:**
```csharp
private static void PopIn(VisualElement el, float fromScale, int durationMs)
{
    el.experimental.animation.Start(0f, 1f, durationMs, (ve, t) =>
    {
        float back = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
        float s    = Mathf.LerpUnclamped(fromScale, 1f, back);
        ve.style.scale   = new Scale(new Vector3(s, s, 1f));
        ve.style.opacity = Mathf.Clamp01(t * 1.6f);
    }).OnCompleted(() =>
    {
        el.style.scale   = Scale.None();
        el.style.opacity = 1f;
    });
}
```

**Propósito:**
- EaseOutBack interpolación (curva overshoot de bounce al final)
- Escala de fromScale → 1 (entrada con "rebote visual")
- Fade paralelo (opacity 0 → 1) para suavidad
- Limpieza post-animación (scale/opacity reset a defaults)

**Uso:**
- Chips de marcas: `PopIn(row[row.childCount - 1], 1.9f, 320)` al aplicar marca
- Chips de estados: `PopIn(card.StatesRow[card.StatesRow.childCount - 1], 1.9f, 320)` al armar reacción/estado
- Dots de afinidad: `PopIn(card.AffinityDot0/1, 2.6f, 420)` al llenar punto (detectado cambio `prev < X && affinity >= X`)

**PopActiveCard() helper:**
```csharp
private static void PopActiveCard(VisualElement root)
{
    root.experimental.animation.Start(0f, 1f, 240, (ve, t) =>
    {
        float s = 1f + 0.10f * Mathf.Sin(Mathf.PI * t);
        ve.style.scale = new Scale(new Vector3(s, s, 1f));
    }).OnCompleted(() => root.style.scale = Scale.None());
}
```

**Propósito:**
- Seno suave: escala 1 → 1.10 → 1 (bounce natural)
- Duración 240ms (rápido, feedback inmediato)
- Llamado en HandleActiveUnit() al marcar carta como activa (clase cv-order-card--active)

**StatsTooltip() dinámico:**
```csharp
private string StatsTooltip(OrderCard card)
{
    var snap = card.Snap;
    return $"{snap.Name} — {Identity(card.Element).DisplayName} · {RoleText(snap.Role)}\n" +
           $"HP {card.CurrentHp:F0}/{snap.MaxHp:F0}\n" +
           $"ATK {snap.Attack:F0} · DEF {snap.Defense:F0} · SPD {snap.Speed:F0}\n" +
           $"Suerte {snap.Luck:F0} · Evasión {snap.Evasion:F0}";
}
```

**Propósito:**
- Tooltip armado al momento del hover (no pre-baked)
- Muestra HP **actual** (card.CurrentHp) vs MaxHp
- Incluye Nombre, Elemento, Rol (Identity + RoleText)
- Incluye 6 stats finales (Attack, Defense, Speed, Luck, Evasion, Hp implícito en línea 2)
- Actualizado automáticamente vía HandleUnitHp (card.CurrentHp = current)

**HandleUnitHp() nuevo:**
```csharp
private void HandleUnitHp(CombatVisualSide side, int index, float current, float max)
{
    if (!cards.TryGetValue((side, index), out var card)) return;
    card.CurrentHp = current;
}
```

**Impacto S61b:**
- Animaciones más vivas (entrada elementos con bounce)
- Tooltip dinámico → HP sincronizado exactamente (no desincronización de valores)
- Feedback visual de "carta activa" con pop suave
- Marcas/estados visualmente destacadas al aparecer (no silenciosas)

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
| `CreateCard()` | **S61b:** Captura Snap, Element, inicializa CurrentHp a snap.MaxHp; **S58:** Headshot + RoleText + ElementColor |
| `RoleText(role)` | "Protector", "Agresivo", "Empático" |
| `RoleChipClass(role)` | Clase CSS por rol (color) |
| `ApplyAffinityDot()` | Llena dot del ElementColor |
| `SetAffinity()` | Actualiza dots con ElementColor, detecta cambios prev→curr para PopIn |
| `PopIn(el, fromScale, durationMs)` | **S61b NEW** Anima escala + opacity con easeOutBack |
| `PopActiveCard(root)` | **S61b NEW** Anima carta activa con bounce sin sin |
| `StatsTooltip(card)` | **S61b NEW** Tooltip dinámico con CurrentHp, Nombre, Elemento, Rol, Stats |
| `HandleUnitHp(side, index, current, max)` | **S61b NEW** Actualiza card.CurrentHp para sincronizar tooltip |

## Lógica S58–S59–S61b

**Cartas reordenable por turno (OnActionOrder):**
- Cada turno, cartas se reordenan a orden de acción
- Headshot visible 512×192 (3/4 lateral yaw 140°)
- Rol completo color-coded (Protector azul, Agresivo rojo, Empático verde)
- Afinidad dots llenan del elemento del MM (p.ej. Agua azul, Fuego naranja)
- Marcas y estados se actualizan per-proc (OnUnitElement)
- **S61b:** Animaciones PopIn suaves, tooltip dinámico, carta activa popup
- **S59:** Hover en slot emite OnUnitHover → anillo world-space reacciona (fade in/out)

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[CombatVisualEvents]] — **S61b** accede Snap/Element en estruct; **S59** publisher OnUnitHover; suscriptor OnActionOrder, OnUnitElement, OnActiveUnit, OnUnitAffinity, OnUnitHpChanged
- [[CombatRadialHealthBar]] — **S59** suscriptor OnUnitHover, listener de eventos
- [[MonchiPortraitUI]] — ApplyHeadshot
- [[MonchiPortraitService]] — GetHeadshot
- [[ElementTableSO]] — **S61b** Identity() para DisplayName/UiColor

## Conexiones

**Entrada:**
- OnVisualCombatStart → BuildCards (contexto, snapshots)
- OnActionOrder → HandleOrder (reordena cartas, ApplyState)
- OnUnitAffinity → HandleAffinity (fill dots, detecta cambios para PopIn)
- OnActiveUnit → HandleActiveUnit (classe active, PopActiveCard anim)
- OnUnitElement → HandleUnitElement (actualiza marcas/estados, PopIn chips)
- OnUnitHpChanged → **S61b NEW** HandleUnitHp (actualiza CurrentHp para tooltip)
- PointerEnterEvent/PointerLeaveEvent → registradas en BuildTeam() (S59 NEW)

**Salida:**
- Barra de orden visual (cartas reordenadas, headshots, roles, marcas, estados)
- Tooltips interactivos dinámicos (S61b: con HP actual)
- Animaciones UITK (S61b: PopIn marcas/estados/afinidad, PopActiveCard)
- **S59 NEW:** CombatVisualEvents.OnUnitHover(side, index, hover) emitido al entrar/salir slot

## Notas S61b

- **Animaciones experimentales:** using UnityEngine.UIElements.Experimental requerido
- **PopIn reutilizable:** mismo helper para marcas, estados, afinidad (parámetros fromScale/durationMs)
- **EaseOutBack:** curva con overshoot (bounce de entrada suave y satisfactoria)
- **Tooltip sincronizado:** CurrentHp actualizado en tiempo real por HandleUnitHp
- **Card data completa:** Snap captura todos los stats, Element el tipo elemental — disponible en cualquier momento

## Notas S58–S59

- Cartas ahora mini-avatares con headshot lateral
- Rol visible en lenguaje natural (no acrónimos)
- Afinidad dots color-coded por elemento (visual consistency)
- Headshot 512×192 comprimido en tarjeta (Cover para crop)
- **S59:** Hover en UI slot activa anillo 3D world-space de la unidad vía evento; CanvasGroup.alpha del anillo controla visibilidad
