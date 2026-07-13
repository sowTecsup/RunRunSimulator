---
tags: [script, ui, combat]
---

# MoriMonchiCombatVisualizerUITK.cs

**Ruta:** `UI/MoriMonchiCombatVisualizerUITK.cs`

**Responsabilidad:** Barra de HP world-space + chips de estado + elementos de UN combatiente del visualizer. Componente HIJO del prefab del peleador, con un `UIDocument` que apunta a `CombatHpBar.uxml` (elementos base `name`, `hp-value`, `atk`, `spd`, `fill` y `effects`). **S42:** Bind expandido a 6 args (rol + nombre/color elemento), nuevas filas dinámicas (role-element, marks-ally, armed-row, marks-enemy) construidas por SetElementState() con ElementChipData (marcas/estados).

**Driven por el Service:** El `CombatVisualizerService` la maneja por referencia directa:
- `Bind(string displayName, float attack, float speed, Role role, string elementName, Color elementColor)` — **S42:** 6 args, fija nombre, ATK, VEL, rol, elemento, resetea HP a 100%
- `SetHp(float current, float max)` — interpola fill + actualiza hp-value label
- `SetStatus(List<CombatStatusMark>)` — actualiza chips de estado activo (legacy S35, aún vigente)
- **`SetElementState(List<ElementChipData> marks, List<ElementChipData> armed)`** — **S42 NEW** renderiza marcas aliadas (arriba), estados armados (centro), marcas enemigas (abajo)

**Binding resiliente + fix de árbol huérfano:** `EnsureRefs()` detecta cuando el `UIDocument` reconstruye su árbol comparando `docRoot != root`; re-resuelve elementos y marca dirty para reescribirlos. Sin esto, al retroceder (Back) la barra quedaría apuntando al árbol viejo.

**Billboard:** en `LateUpdate` orienta el panel hacia `Camera.main`, igual que [[NameTag]], independiente de rotación del slot.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Bind(string name, float atk, float spd, Role role, string elemName, Color elemColor)` | **S42:** 6 args — Fija nombre, stats, rol, elemento, resetea HP a 100% |
| `SetHp(float current, float max)` | Actualiza HP (interpola fill) |
| `SetStatus(List<CombatStatusMark> marks)` | **S35** Setea estado de efectos activos (legacy, aún vigente) |
| `SetElementState(List<ElementChipData> marks, List<ElementChipData> armed)` | **S42 NEW** Renderiza marcas y estados armados en filas dinámicas |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | Ref opcional (auto-resuelto si null) |
| `fillLerpSeconds` | `float` | Duración interpolación HP (default 0.4s) |
| `uprightOnly` | `bool` | Si true, solo rota Y (billboard uprightless) |
| `palette` | `CombatPopupPaletteSO` | Paleta para colorear chips de estado (S35, legacy) |

## Campos Internos (S42 NEW)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `desiredRole` | `Role` | Rol actual (para label: "Protector"/"Agresivo"/"Empático") |
| `desiredElementName` | `string` | Nombre del elemento (DisplayName de ElementTableSO) |
| `desiredElementColor` | `Color` | Color del elemento (UiColor) |
| `desiredMarks` | `List<ElementChipData>` | Marcas actuales (aliadas + enemigas) |
| `desiredArmed` | `List<ElementChipData>` | Estados armados actuales |
| `elementStateDirty` | `bool` | Marca update de elementos |
| `roleElementRow` | `VisualElement` | Row con role-label + element-chip + element-label (S42 creado si falta) |
| `marksAllyRow` | `VisualElement` | Row superior: marcas aliadas (S42 creado si falta) |
| `armedRow` | `VisualElement` | Row central: estados armados (S42 creado si falta) |
| `marksEnemyRow` | `VisualElement` | Row inferior: marcas enemigas (S42 creado si falta) |

## Método Bind (S42 ACTUALIZADO)

```csharp
public void Bind(string displayName, float attack, float speed, Role role, string elementName, Color elementColor)
{
    desiredName         = displayName;
    desiredAtk          = $"ATK {Mathf.RoundToInt(attack)}";
    desiredSpd          = $"VEL {Mathf.RoundToInt(speed)}";
    desiredRole         = role;
    desiredElementName  = elementName;
    desiredElementColor = elementColor.a <= 0f ? Color.white : elementColor;
    staticDirty = true;
    targetPct   = 1f;
    currentPct  = 1f;
    Apply();
}
```

**S42 Cambio:** Parámetros expandidos a 6 args (antes 3), incluyendo rol y elemento.

## SetElementState (S42 NEW)

```csharp
public void SetElementState(List<ElementChipData> marks, List<ElementChipData> armed)
{
    desiredMarks      = marks ?? new List<ElementChipData>();
    desiredArmed      = armed ?? new List<ElementChipData>();
    elementStateDirty = true;
}
```

En `Apply()` si `elementStateDirty`:
```csharp
if (marksAllyRow != null)
{
    marksAllyRow.Clear();
    foreach (var mark in desiredMarks)
        if (mark.AllySource) marksAllyRow.Add(BuildMarkChip(mark, top: true));
}
if (armedRow != null)
{
    armedRow.Clear();
    foreach (var armed in desiredArmed)
        armedRow.Add(BuildArmedChip(armed));
}
if (marksEnemyRow != null)
{
    marksEnemyRow.Clear();
    foreach (var mark in desiredMarks)
        if (!mark.AllySource) marksEnemyRow.Add(BuildMarkChip(mark, top: false));
}
elementStateDirty = false;
```

**Lógica S42:**
- Itera desiredMarks, filtra aliadas → BuildMarkChip(top: true) con borde superior coloreado
- Itera desiredArmed → BuildArmedChip() con borde 4-lados (★ prefix)
- Itera desiredMarks, filtra enemigas → BuildMarkChip(top: false) con borde inferior coloreado

## BuildMarkChip (S42 NEW)

```csharp
private static VisualElement BuildMarkChip(ElementChipData data, bool top)
{
    Color c = data.Color.a <= 0f ? Color.white : data.Color;
    var chip = new VisualElement();
    chip.style.paddingTop      = 1;
    chip.style.paddingBottom   = 1;
    chip.style.paddingLeft     = 3;
    chip.style.paddingRight    = 3;
    chip.style.marginLeft      = 1;
    chip.style.marginRight     = 1;
    chip.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
    if (top)
    {
        chip.style.borderTopWidth = 2;
        chip.style.borderTopColor = c;
    }
    else
    {
        chip.style.borderBottomWidth = 2;
        chip.style.borderBottomColor = c;
    }

    var label = new Label(data.Label);
    label.style.fontSize                = 8;
    label.style.unityFontStyleAndWeight = FontStyle.Bold;
    label.style.color                   = c;
    chip.Add(label);
    return chip;
}
```

Crea chip con Label (DisplayName del elemento) colorido, borde superior (aliada) o inferior (enemiga), fondo semitransparente.

## BuildArmedChip (S42 NEW)

```csharp
private static VisualElement BuildArmedChip(ElementChipData data)
{
    Color c = data.Color.a <= 0f ? Color.white : data.Color;
    var chip = new VisualElement();
    chip.style.paddingTop         = 1;
    chip.style.paddingBottom      = 1;
    chip.style.paddingLeft        = 3;
    chip.style.paddingRight       = 3;
    chip.style.marginLeft         = 1;
    chip.style.marginRight        = 1;
    chip.style.backgroundColor    = new Color(0f, 0f, 0f, 0.55f);
    chip.style.borderTopWidth     = 1;
    chip.style.borderBottomWidth  = 1;
    chip.style.borderLeftWidth    = 1;
    chip.style.borderRightWidth   = 1;
    chip.style.borderTopColor     = c;
    chip.style.borderBottomColor  = c;
    chip.style.borderLeftColor    = c;
    chip.style.borderRightColor   = c;

    var label = new Label("★" + data.Label);
    label.style.fontSize                = 8;
    label.style.unityFontStyleAndWeight = FontStyle.Bold;
    label.style.color                   = c;
    chip.Add(label);
    return chip;
}
```

Crea chip con Label ("★" + DisplayName del estado) colorido, borde 4-lados (rombo visual), fondo semitransparente. Diferencia visual clara de marcas (borde simple) vs estados armados (borde full).

## EnsureRefs (S42 ACTUALIZADO)

Además de elementos base (name, fill, hp-value, atk, spd, effects), construye dinámicamente si faltan:
- `roleElementRow` — Row con role-label + element-chip + element-label
- `marksAllyRow` — Row de marcas aliadas (top)
- `armedRow` — Row de estados armados (center)
- `marksEnemyRow` — Row de marcas enemigas (bottom)

Todos con estilos flexDirection: Row, flexWrap: Wrap, justifyContent: Center, margins de separación.

## SetStatus Implementation — S35 (SIN CAMBIOS)

```csharp
public void SetStatus(List<CombatStatusMark> marks)
{
    desiredStatus = marks ?? new List<CombatStatusMark>();
    statusDirty   = true;
}
```

Sigue vigente para legacy S35, ahora coexiste con SetElementState() S42.

## Dirty Pattern (S42 EXTENDIDO)

- `staticDirty` — marca update de nombre/ATK/SPD/rol/elemento
- `statusDirty` — marca update de estado (legacy S35)
- `elementStateDirty` — marca update de elementos (S42 NEW)
- `Update()` chequea 3 flags y aplica cambios — patrón dirty que **sobrevive rebuild** del árbol

## Estructura de Filas (S42)

**Orden en el árbol UXML (si creadas dinámicamente):**
1. marksAllyRow (marcas aliadas, borde superior coloreado, arriba del HP)
2. name (nombre base)
3. fill (barra HP)
4. hp-value (label HP)
5. roleElementRow (rol + chip color + nombre elemento)
6. atk/spd (stats)
7. effects (S35 legacy, chips de estado)
8. armedRow (estados armados con ★)
9. marksEnemyRow (marcas enemigas, borde inferior coloreado)

## Helper RoleLabel (S42 NEW)

```csharp
private static string RoleLabel(Role role)
{
    switch (role)
    {
        case Role.Protector: return "Protector";
        case Role.Agresivo:  return "Agresivo";
        case Role.Empatico:  return "Empático";
        default:             return role.ToString();
    }
}
```

Mapea Role enum a etiqueta española.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatVisualizerService]] — llamador de Bind/SetHp/SetStatus/SetElementState
- [[CombatStatusMark]] — input para SetStatus (S35 legacy)
- [[ElementChipData]] — input para SetElementState (S42)
- [[CombatPopupPaletteSO]] — colores de efectos
- [[ElementTableSO]] — **S42:** fuente de DisplayName + UiColor (accesible vía CombatVisualizerService)
- [[CreatureDNA]] — datos del combatiente

## Conexiones

**Entrada:**
- `Bind(name, atk, spd, role, elemName, elemColor)` — de CombatVisualizerService.BeginRoutine (S42: 6 args)
- `SetHp(current, max)` — de CombatVisualizerService.PushHp
- `SetStatus(marks)` — de CombatVisualizerService.Restore/ForwardRoutine (S35, legacy)
- `SetElementState(marks, armed)` — de CombatVisualizerService.PushElements (S42 NEW)

**Salida:**
- UIElements visuales (HP fill interpolado, chips de estado S35 + chips de elemento S42)

## Cambios S35

**StatusCode reemplaza StatusText/StatusInitial:** Nueva función `StatusCode()` retorna códigos de 3 letras directamente (POI, BUR, STA, PUL, STE, MIS, REG, ATU, ESP, CUR, ROB).

**Estructura de 2 filas:** Cada chip es un VisualElement Column con:
- Fila 1: Código colorido (pequeño, 9px, bold)
- Fila 2: Contador "×N" (8px, blanco) — solo si Stacks > 1

**5 nuevos StatusCode:** STA, PUL, STE, MIS, ROB.

## Cambios S42

**Aditivos (append-only, coexisten con S35):**
- **Parámetro Bind:** Expandido a 6 args (role, elementName, elementColor)
- **Método nuevo:** `SetElementState(marks, armed)` — renderiza chips elementales en 3 filas dinámicas
- **Nuevos helpers:** `RoleLabel()`, `BuildMarkChip()`, `BuildArmedChip()`
- **Nuevas filas dinámicas:** roleElementRow, marksAllyRow, armedRow, marksEnemyRow
- **Nuevo flag:** `elementStateDirty`
- **Nuevos desiredX:** desiredRole, desiredElementName, desiredElementColor, desiredMarks, desiredArmed
- **Estructura visual:** Marcas aliadas (borde top coloreado) | Rol + elemento chip | Estados armados (borde 4-lados) | Marcas enemigas (borde bottom coloreado)

**Invariante:** SetStatus() sigue vigente, se renderiza en `effectsRow` (S35 legacy). Coexiste con SetElementState().

## Notas

- **S42 Bind 6 args:** Ahora integra rol + elemento, necesario para barra de orden y unidades visuales
- **ElementChipData:** Estructura ligera (Label, Color, AllySource) para renderizar dinámicamente
- **Colores:** ElementChipData.Color con fallback a white si alpha 0
- **Filas dinámicas:** Creadas en EnsureRefs si faltan en UXML, permitiendo UXML viejo o vacío
- **Dirty pattern S42:** elementStateDirty persiste a través de rebuild
- **Billboard:** LateUpdate rota solo Y si `uprightOnly=true`, manteniendo panel legible desde cualquier ángulo
- **★ Prefix:** Estados armados se marcan visualmente con ★ (versus marcas sin símbolo)
