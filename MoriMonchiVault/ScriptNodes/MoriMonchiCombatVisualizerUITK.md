---
tags: [script, ui, combat]
---

# MoriMonchiCombatVisualizerUITK.cs

**Ruta:** `UI/MoriMonchiCombatVisualizerUITK.cs`

**Responsabilidad:** Barra de HP world-space + chips de estado + elementos + ESCUDO + reacciones DE UN combatiente del visualizer. Componente HIJO del prefab del peleador, con un `UIDocument` que apunta a `CombatHpBar.uxml` (elementos base `name`, `hp-value`, `atk`, `spd`, `fill` y `effects`). **S42:** Bind expandido a 6 args (rol + nombre/color elemento), nuevas filas dinámicas (role-element, marks-ally, armed-row, marks-enemy) construidas por SetElementState() con ElementChipData (marcas/estados). **S43:** SetShield() dibuja barra azul 4px sobre hp-track, FlashReaction() muestra label transient 2s (narrador/reacciones), BuildArmedChip borrado → BuildStateLabel, estados armados en cajas rojo (negativo) / verde (positivo) con nombres completos.

**Driven por el Service:** El `CombatVisualizerService` la maneja por referencia directa:
- `Bind(string displayName, float attack, float speed, Role role, string elementName, Color elementColor)` — **S42:** 6 args, fija nombre, ATK, VEL, rol, elemento, resetea HP a 100%
- `SetHp(float current, float max)` — interpola fill + actualiza hp-value label
- `SetStatus(List<CombatStatusMark>)` — actualiza chips de estado activo (legacy S35, aún vigente)
- `SetElementState(List<ElementChipData> marks, List<ElementChipData> armed)` — **S42 NEW** renderiza marcas aliadas (arriba), estados armados (centro), marcas enemigas (abajo)
- `SetShield(float shield)` — **S43 NEW** renderiza barra azul 4px sobre hp-track, escala shield/MaxHp, oculta si <0.5
- `FlashReaction(string text, Color color)` — **S43 NEW** muestra label transient reactionFlashSeconds=2s (narrador estados armados, reacciones elementales)

**Binding resiliente + fix de árbol huérfano:** `EnsureRefs()` detecta cuando el `UIDocument` reconstruye su árbol comparando `docRoot != root`; re-resuelve elementos y marca dirty para reescribirlos. Sin esto, al retroceder (Back) la barra quedaría apuntando al árbol viejo.

**Billboard:** en `LateUpdate` orienta el panel hacia `Camera.main`, igual que [[NameTag]], independiente de rotación del slot.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Bind(string name, float atk, float spd, Role role, string elemName, Color elemColor)` | **S42:** 6 args — Fija nombre, stats, rol, elemento, resetea HP a 100% |
| `SetHp(float current, float max)` | Actualiza HP (interpola fill) |
| `SetStatus(List<CombatStatusMark> marks)` | **S35** Setea estado de efectos activos (legacy, aún vigente) |
| `SetElementState(List<ElementChipData> marks, List<ElementChipData> armed)` | **S42 NEW** Renderiza marcas y estados armados en filas dinámicas |
| `SetShield(float shield)` | **S43 NEW** Renderiza barra azul 4px sobre hp-track, escala a shield/MaxHp |
| `FlashReaction(string text, Color color)` | **S43 NEW** Muestra label transient reactionFlashSeconds=2s con color custom |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | Ref opcional (auto-resuelto si null) |
| `fillLerpSeconds` | `float` | Duración interpolación HP (default 0.4s) |
| `uprightOnly` | `bool` | Si true, solo rota Y (billboard uprightless) |
| `palette` | `CombatPopupPaletteSO` | Paleta para colorear chips de estado (S35, legacy) |
| `reactionFlashSeconds` | `float` | **S43 NEW** Duración del flash de reacción (default 2f) |

## Campos Internos (S42/S43)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `desiredRole` | `Role` | Rol actual (para label: "Protector"/"Agresivo"/"Empático") |
| `desiredElementName` | `string` | Nombre del elemento (DisplayName de ElementTableSO) |
| `desiredElementColor` | `Color` | Color del elemento (UiColor) |
| `desiredMarks` | `List<ElementChipData>` | Marcas actuales (aliadas + enemigas) |
| `desiredArmed` | `List<ElementChipData>` | Estados armados actuales |
| `desiredShield` | `float` | **S43 NEW** Valor de escudo a renderizar (0..MaxHp) |
| `desiredReactionText` | `string` | **S43 NEW** Texto del flash de reacción |
| `desiredReactionColor` | `Color` | **S43 NEW** Color del flash de reacción |
| `elementStateDirty` | `bool` | Marca update de elementos |
| `shieldDirty` | `bool` | **S43 NEW** Marca update de escudo |
| `reactionDirty` | `bool` | **S43 NEW** Marca update de reacción flash |
| `roleElementRow` | `VisualElement` | Row con role-label + element-chip + element-label (S42 creado si falta) |
| `marksAllyRow` | `VisualElement` | Row superior: marcas aliadas (S42 creado si falta) |
| `armedRow` | `VisualElement` | Row central: estados armados (S42 creado si falta) |
| `marksEnemyRow` | `VisualElement` | Row inferior: marcas enemigas (S42 creado si falta) |
| `shieldTrack` | `VisualElement` | **S43 NEW** Barra azul 4px sobre hp-track (height 4, width 100%, marginBottom 1) |
| `shieldFill` | `VisualElement` | **S43 NEW** Fill azul dentro shieldTrack (width % basado en shield/MaxHp) |
| `reactionRow` | `Label` | **S43 NEW** Label transient para narrador/reacciones (reactionFlashSeconds duration) |
| `reactionUntil` | `float` | **S43 NEW** Time.time límite para mostrar reacción |
| `reactionVisible` | `bool` | **S43 NEW** Bandera si reacción está visible |

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
    desiredShield = 0f;      // S43 NEW: reset escudo
    shieldDirty   = true;    // S43 NEW
    reactionUntil  = 0f;     // S43 NEW
    reactionVisible = false; // S43 NEW
    if (reactionRow != null) reactionRow.style.display = DisplayStyle.None;
    Apply();
}
```

**S42 Cambio:** Parámetros expandidos a 6 args (antes 3), incluyendo rol y elemento.
**S43 Cambio:** Reset escudo a 0, reaction a invisible.

## SetHp (S42/S43 IGUAL)

```csharp
public void SetHp(float current, float max)
{
    maxHp     = Mathf.Max(0f, max);
    targetPct = maxHp > 0f ? Mathf.Clamp01(current / maxHp) : 0f;
}
```

Anima fill y hp-value label sobre fillLerpSeconds.

## SetShield (S43 NEW)

```csharp
public void SetShield(float shield)
{
    desiredShield = shield;
    shieldDirty   = true;
}
```

En `Apply()` si `shieldDirty`:
```csharp
if (shieldFill != null && maxHp > 0f)
{
    float shieldPct = Mathf.Clamp01(desiredShield / maxHp);
    shieldFill.style.width = Length.Percent(shieldPct * 100f);
    if (shieldFill.parent != null) shieldFill.parent.style.display = shieldPct >= 0.005f ? DisplayStyle.Flex : DisplayStyle.None;
}
shieldDirty = false;
```

Oculta si pct < 0.5% (umbral visual).

## FlashReaction (S43 NEW)

```csharp
public void FlashReaction(string text, Color color)
{
    desiredReactionText  = text;
    desiredReactionColor = color;
    reactionUntil = Time.time + reactionFlashSeconds;
    reactionDirty = true;
}
```

En `Apply()` si `reactionDirty`:
```csharp
if (reactionRow != null)
{
    reactionRow.text = desiredReactionText;
    reactionRow.style.color = desiredReactionColor;
    reactionRow.style.display = DisplayStyle.Flex;
    reactionVisible = true;
}
reactionDirty = false;
```

Llamado por `CombatVisualizerService.PlayProc()` en rama StateArmed: emite narrador "¡Quedé {stateName}!" con color rojo (negativo) o verde (positivo).

## SetElementState (S42 NEW, S43 IGUAL)

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
        armedRow.Add(BuildStateLabel(armed));  // S43: renamed from BuildArmedChip
}
if (marksEnemyRow != null)
{
    marksEnemyRow.Clear();
    foreach (var mark in desiredMarks)
        if (!mark.AllySource) marksEnemyRow.Add(BuildMarkChip(mark, top: false));
}
elementStateDirty = false;
```

**Lógica S42/S43:**
- Itera desiredMarks, filtra aliadas → BuildMarkChip(top: true) con borde superior coloreado
- Itera desiredArmed → BuildStateLabel() con borde 4-lados (sin ★ prefix, solo DisplayName)
- Itera desiredMarks, filtra enemigas → BuildMarkChip(top: false) con borde inferior coloreado

## BuildMarkChip (S42/S43 IGUAL)

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

## BuildStateLabel (S43 NUEVO, reemplaza BuildArmedChip S42)

```csharp
private VisualElement BuildStateLabel(ElementChipData armed)
{
    Color c = armed.Color.a <= 0f ? Color.white : armed.Color;
    
    var stateBox = new VisualElement();
    stateBox.style.backgroundColor = armed.Negative 
        ? new Color(140f / 255f, 24f / 255f, 24f / 255f, 0.75f)      // rojo
        : new Color(60f / 255f, 140f / 255f, 60f / 255f, 0.75f);     // verde
    stateBox.style.borderTopColor    = armed.Negative ? new Color(1f, 90f / 255f, 90f / 255f) : new Color(120f / 255f, 1f, 120f / 255f);
    stateBox.style.borderBottomColor = stateBox.style.borderTopColor;
    stateBox.style.borderLeftColor   = stateBox.style.borderTopColor;
    stateBox.style.borderRightColor  = stateBox.style.borderTopColor;
    stateBox.style.borderTopWidth    = 1;
    stateBox.style.borderBottomWidth = 1;
    stateBox.style.borderLeftWidth   = 1;
    stateBox.style.borderRightWidth  = 1;
    stateBox.style.borderTopLeftRadius     = 3;
    stateBox.style.borderTopRightRadius    = 3;
    stateBox.style.borderBottomLeftRadius  = 3;
    stateBox.style.borderBottomRightRadius = 3;
    stateBox.style.paddingTop    = 1;
    stateBox.style.paddingBottom = 1;
    stateBox.style.paddingLeft   = 3;
    stateBox.style.paddingRight  = 3;
    stateBox.style.marginTop     = 1;
    stateBox.style.marginBottom  = 1;
    stateBox.style.alignItems    = Align.Center;

    var label = new Label(armed.Label);
    label.style.fontSize                = 8;
    label.style.unityFontStyleAndWeight = FontStyle.Bold;
    label.style.color                   = c;
    stateBox.Add(label);
    return stateBox;
}
```

**S43 Cambio:** Borrada rama BuildArmedChip con ★ prefix. Reemplazada por BuildStateLabel que:
- Usa ElementChipData.Negative para decidir color (rojo si negativo, verde si positivo)
- Muestra DisplayName completo sin prefijo (antes tenía ★)
- Borde 4-lados (rojo/verde según Negative)
- Caja semitransparente

**Invariante con S42:** ElementChipData.Negative viene de CombatVisualizerService.PushElements(), que marca negativo si es ElementalState en NegativeStates HashSet o mark.AllySource == false.

## EnsureRefs() Improvements (S42/S43)

**Nuevo en S42:** Resolución dinámica de rows (roleElementRow, marksAllyRow, armedRow, marksEnemyRow) si faltan.

**S43 NUEVO:** Resolución dinámica de shieldTrack/shieldFill:
```csharp
shieldTrack = root.Q<VisualElement>("shield-track");
if (shieldTrack == null && fill != null)
{
    // Crear shieldTrack como hermano de fill (insertarlo ANTES de fill en el padre)
    var track = fill.parent;
    var trackParent = track != null ? track.parent : null;
    if (track != null && trackParent != null)
    {
        shieldTrack = new VisualElement { name = "shield-track" };
        shieldTrack.style.height          = 4;
        shieldTrack.style.width           = Length.Percent(100f);
        shieldTrack.style.marginBottom    = 1;
        shieldTrack.style.backgroundColor = new Color(0f, 0f, 0f, 0.4f);

        shieldFill = new VisualElement { name = "shield-fill" };
        shieldFill.style.height          = Length.Percent(100f);
        shieldFill.style.width           = Length.Percent(0f);
        shieldFill.style.backgroundColor = new Color(90f / 255f, 160f / 255f, 255f / 255f);  // Azul
        shieldTrack.Add(shieldFill);

        int trackIdx = trackParent.IndexOf(track);
        trackParent.Insert(trackIdx, shieldTrack);  // Inserta ANTES de fill
    }
}
```

Crea la barra azul dinámicamente si no existe en UXML.

## LateUpdate (S42/S43 IGUAL)

```csharp
private void LateUpdate()
{
    if (!EnsureRefs()) return;

    // Billboard hacia Camera.main
    if (cam != null && uprightOnly)
    {
        var dir = (cam.transform.position - root.worldBound.center).normalized;
        root.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
    }

    // Animar fill (HP)
    if (currentPct != targetPct)
    {
        currentPct = Mathf.Lerp(currentPct, targetPct, Time.deltaTime / fillLerpSeconds);
        if (fill != null) fill.style.width = Length.Percent(currentPct * 100f);
        if (hpValueLabel != null) hpValueLabel.text = $"{Mathf.RoundToInt(currentPct * maxHp)} / {Mathf.RoundToInt(maxHp)}";
    }

    // Ocultar reacción si tiempo expiró (S43)
    if (reactionVisible && Time.time >= reactionUntil)
    {
        reactionVisible = false;
        if (reactionRow != null) reactionRow.style.display = DisplayStyle.None;
    }

    // Apply() si dirty flags
    if (staticDirty || statusDirty || elementStateDirty || shieldDirty || reactionDirty)
        Apply();
}
```

**S43:** Chequea si reactionUntil expiró y oculta label.

## S42 Cambios

**Nuevo en S42:**
- Bind expandido a 6 args (role + elemento)
- SetElementState() new method
- Filas dinámicas: roleElementRow, marksAllyRow, armedRow, marksEnemyRow
- BuildMarkChip() helper

## S43 Cambios

**Aditivos (append-only):**
- **Campos públicos:** SetShield(), FlashReaction()
- **Campos internos:** desiredShield, desiredReactionText, desiredReactionColor, shieldTrack, shieldFill, reactionRow, reactionUntil, reactionVisible, shieldDirty, reactionDirty
- **Serializado:** reactionFlashSeconds (default 2f)
- **EnsureRefs() nueva rama:** Crea shieldTrack/shieldFill dinámicamente
- **BuildStateLabel() NEW:** Reemplaza BuildArmedChip, usa ElementChipData.Negative para color (rojo/verde)
- **LateUpdate() rama S43:** Chequea reactionUntil, oculta label si expiró
- **Apply() lógica S43:** Renderiza shieldFill width basado en shield/MaxHp, aplica reactionRow text/color si reactionDirty

**Invariante:** Bind, SetHp, SetStatus, SetElementState sin cambios visibles; métodos publicados, implementación interna extendida.

## Vinculado a

- [[Index/03 - Combat System]]
- [[CombatVisualizerService]] — publicador SetHp/SetShield/FlashReaction/SetElementState via ref
- [[MoriMonchiVisualizer]] — prefab padre (component child)
- [[ElementTableSO]] — paleta de colores
- [[UIDocument]] — ref a CombatHpBar.uxml (elementos base name, hp-value, fill, effects)
- [[CombatVisualEvents]] — suscriptor OnUnitHpChanged (legacy) para SetHp

## Notas

- **S42:** Dinámico por completo — si el UXML es minimal, construye todo en C#
- **S43:** Shield track azul 4px sobre HP (marginBottom 1 para separación)
- **S43:** FlashReaction label transient, oculto automático tras reactionFlashSeconds
- **Negative classification:** true si estado en NegativeStates HashSet O mark.AllySource == false (marca enemiga)
- Todos los colores pueden tweakearse vía Color constants en BuildStateLabel
