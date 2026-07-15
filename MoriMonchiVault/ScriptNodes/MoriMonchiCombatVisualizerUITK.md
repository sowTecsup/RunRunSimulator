---
tags: [script, ui, combat, uitk]
---

# MoriMonchiCombatVisualizerUITK.cs

**Ruta:** `UI/MoriMonchiCombatVisualizerUITK.cs`

**Responsabilidad:** Barra de HP world-space + escudo (S47) DE UN combatiente del visualizer. Componente HIJO del prefab del peleador, con un `UIDocument` que apunta a `CombatHpBar.uxml` (elementos base `name`, `hp-value`, `atk`, `spd`, `fill` y `effects`). **S42:** Bind expandido a 6 args (rol + nombre/color elemento), nuevas filas dinámicas (role-element, marks-ally, armed-row, marks-enemy) construidas por SetElementState() con ElementChipData (marcas/estados). **S43:** SetShield() dibuja barra azul 4px sobre hp-track, FlashReaction() muestra label transient 2s (narrador/reacciones), BuildArmedChip borrado → BuildStateLabel, estados armados en cajas rojo (negativo) / verde (positivo) con nombres completos. **S45:** Nuevo campo serializado `hideForDebug` (bool) — si true, oculta el root del cuadro world-space (display None en Apply()) y saltea el resto del Apply(). **S47:** Bind reescrito SIN argumentos; API simplificada a SetHp/SetShield/SetActiveTurn/SetTargeted — eliminados SetStatus, SetElementState, FlashReaction. Barra minimal: solo HP + escudo como segmento azul + marcos dorado (turno activo) / rojo (objetivo del ataque).

**Driven por el Service:** El `CombatVisualizerService` la maneja por referencia directa:
- `Bind()` — **S47:** Sin argumentos, barra inicializada en blanco
- `SetHp(float current, float max)` — interpola fill + actualiza hp-value label
- `SetShield(float shield)` — dibuja segmento azul dentro del track, sin animación (S47: simplificado)
- `SetActiveTurn(bool value)` — marca turno activo (marco dorado)
- `SetTargeted(bool value)` — marca objetivo de ataque (marco rojo)

**Binding resiliente + fix de árbol huérfano:** `EnsureRefs()` detecta cuando el `UIDocument` reconstruye su árbol comparando `docRoot != root`; re-resuelve elementos y marca dirty para reescribirlos. Sin esto, al retroceder (Back) la barra quedaría apuntando al árbol viejo.

**Billboard:** en `LateUpdate` orienta el panel hacia `Camera.main`, igual que [[NameTag]], independiente de rotación del slot.

**S45 hideForDebug:** Si true, Apply() setea root.style.display = DisplayStyle.None y retorna early, ocultando todo el cuadro. Permite QA/debug sin que UI tapee la pantalla.

**S47 Simplificación:** Eliminadas filas dinámicas de marcas/estados; barra minimal muestra solo HP + escudo (azul) + marcos (dorado/rojo en bordes). Stats/elemento/rol ya no necesarios — se rellenan dinámicamente vía API.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Bind()` | **S47:** Sin argumentos — inicializa barra en blanco, resetea HP a 100%, escudo a 0, marcos a inactivos |
| `SetHp(float current, float max)` | Actualiza HP (interpola fill) |
| `SetShield(float shield)` | **S47 ACTUALIZADO** Renderiza barra azul dentro del track, sin animación separada |
| `SetActiveTurn(bool value)` | **S47 NEW** Activa/desactiva marco dorado (turno activo del unit) |
| `SetTargeted(bool value)` | **S47 NEW** Activa/desactiva marco rojo (unit es objetivo del ataque) |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | Ref opcional (auto-resuelto si null) |
| `hideForDebug` | `bool` | **S45** Si true, oculta el cuadro root (display None en Apply) y saltea lógica |
| `fillLerpSeconds` | `float` | Duración interpolación HP (default 0.4s) |
| `uprightOnly` | `bool` | Si true, solo rota Y (billboard uprightless) |

## Campos Internos (S47)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `targetPct` | `float` | Porcentaje HP deseado (0..1) |
| `currentPct` | `float` | Porcentaje HP actual (interpola a targetPct) |
| `maxHp` | `float` | HP máximo (para cálculos de escudo) |
| `desiredShield` | `float` | Valor de escudo a renderizar (0..MaxHp) |
| `activeTurn` | `bool` | Si el unit tiene turno activo (marco dorado) |
| `targeted` | `bool` | Si el unit es objetivo del ataque (marco rojo) |
| `cam` | `Transform` | Cache de Camera.main.transform (para billboard) |
| `root` | `VisualElement` | Root del documento UXML |
| `nameLabel` | `Label` | Elemento `name` (oculto en S47) |
| `atkLabel` | `Label` | Elemento `atk` (oculto en S47) |
| `spdLabel` | `Label` | Elemento `spd` (oculto en S47) |
| `hpValueLabel` | `Label` | Elemento `hp-value` (muestra "valor / máx" + escudo) |
| `fill` | `VisualElement` | Elemento `fill` (barra HP) |
| `track` | `VisualElement` | Padre de `fill` (recibe bordes) |
| `shieldFill` | `VisualElement` | **S47 ACTUALIZADO** Segmento azul dentro del track, ancho = (escudo/MaxHp) |

## Método Bind() (S47 REESCRITO, Sin Argumentos)

```csharp
public void Bind()
{
    targetPct     = 1f;
    currentPct    = 1f;
    desiredShield = 0f;
    activeTurn    = false;
    targeted      = false;
    Apply();
}
```

**S47 Cambio:** Parámetros completamente eliminados (antes 6 args). Barra inicializada en blanco con HP al 100%, sin datos de nombre/rol/elemento — esos datos llegan vía API dinámicamente si se necesitan en otras partes de la UI.

## SetHp (S42/S43/S47 IGUAL)

```csharp
public void SetHp(float current, float max)
{
    maxHp     = Mathf.Max(0f, max);
    targetPct = maxHp > 0f ? Mathf.Clamp01(current / maxHp) : 0f;
}
```

Anima fill y hp-value label sobre fillLerpSeconds.

## SetShield (S43/S47 SIMPLIFICADO)

```csharp
public void SetShield(float shield)
{
    desiredShield = shield;
}
```

**S47 Cambio:** Antes era procesado con `shieldDirty` flag. Ahora es directo: en Update/Apply() se actualiza `shieldFill` en tiempo real sin flag separado.

En `Apply()`:
```csharp
float hpPct     = Mathf.Clamp01(currentPct);
float shieldPct = maxHp > 0f ? Mathf.Clamp01(desiredShield / maxHp) : 0f;
shieldPct       = Mathf.Min(shieldPct, 1f - hpPct);  // No supera el track disponible
if (shieldFill != null)
{
    shieldFill.style.left    = Length.Percent(hpPct * 100f);
    shieldFill.style.width   = Length.Percent(shieldPct * 100f);
    shieldFill.style.display = desiredShield >= 0.5f && shieldPct > 0f ? DisplayStyle.Flex : DisplayStyle.None;
}
```

Dibuja barra azul como segmento adyacente al HP, sin animación separada.

## SetActiveTurn (S47 NEW)

```csharp
public void SetActiveTurn(bool value)
{
    activeTurn = value;
}
```

**S47 NEW:** Marca si el unit tiene turno activo (usado al comenzar TurnStart).

En `Apply()`:
```csharp
Color borderColor = BorderColor();
track.style.borderTopColor    = borderColor;
track.style.borderBottomColor = borderColor;
track.style.borderLeftColor   = borderColor;
track.style.borderRightColor  = borderColor;

private Color BorderColor()
{
    if (targeted)   return new Color(1f, 72f / 255f, 72f / 255f);  // Rojo
    if (activeTurn) return new Color(1f, 200f / 255f, 60f / 255f); // Dorado
    return Color.clear;  // Sin marco
}
```

## SetTargeted (S47 NEW)

```csharp
public void SetTargeted(bool value)
{
    targeted = value;
}
```

**S47 NEW:** Marca si el unit es objetivo del ataque actual.

Marco dibujado en `Apply()` vía `BorderColor()` (rojo si targeted, dorado si activeTurn, invisible si ninguno).

## Método Apply() (S47 SIMPLIFICADO)

```csharp
private void Apply()
{
    if (!EnsureRefs()) return;
    if (root != null) root.style.display = hideForDebug ? DisplayStyle.None : DisplayStyle.Flex;
    if (hideForDebug) return;

    if (!Mathf.Approximately(currentPct, targetPct))
    {
        float t = fillLerpSeconds > 0f ? Mathf.Min(1f, Time.deltaTime / fillLerpSeconds) : 1f;
        currentPct = Mathf.Lerp(currentPct, targetPct, t);
    }
    if (fill != null) fill.style.width = Length.Percent(currentPct * 100f);
    
    if (hpValueLabel != null)
    {
        hpValueLabel.text = desiredShield >= 0.5f
            ? $"{Mathf.RoundToInt(currentPct * maxHp)} / {Mathf.RoundToInt(maxHp)}  +{Mathf.RoundToInt(desiredShield)}"
            : $"{Mathf.RoundToInt(currentPct * maxHp)} / {Mathf.RoundToInt(maxHp)}";
    }

    float hpPct     = Mathf.Clamp01(currentPct);
    float shieldPct = maxHp > 0f ? Mathf.Clamp01(desiredShield / maxHp) : 0f;
    shieldPct       = Mathf.Min(shieldPct, 1f - hpPct);
    if (shieldFill != null)
    {
        shieldFill.style.left    = Length.Percent(hpPct * 100f);
        shieldFill.style.width   = Length.Percent(shieldPct * 100f);
        shieldFill.style.display = desiredShield >= 0.5f && shieldPct > 0f ? DisplayStyle.Flex : DisplayStyle.None;
    }

    if (track != null)
    {
        Color borderColor = BorderColor();
        track.style.borderTopColor    = borderColor;
        track.style.borderBottomColor = borderColor;
        track.style.borderLeftColor   = borderColor;
        track.style.borderRightColor  = borderColor;
    }
}

private Color BorderColor()
{
    if (targeted)   return new Color(1f, 72f / 255f, 72f / 255f);
    if (activeTurn) return new Color(1f, 200f / 255f, 60f / 255f);
    return Color.clear;
}
```

**S47 CAMBIO:** Completamente reescrito. Eliminados SetStatus, SetElementState, FlashReaction. Ahora solo:
1. Interpola HP (currentPct → targetPct)
2. Renderiza fill (HP) + shieldFill (escudo azul)
3. Renderiza label con valor + escudo
4. Renderiza borders (marco dorado/rojo)

## EnsureRefs() (S47 SIMPLIFICADO)

```csharp
private bool EnsureRefs()
{
    if (document == null) document = GetComponentInChildren<UIDocument>(true);
    if (document == null) return false;
    var docRoot = document.rootVisualElement;
    if (docRoot == null) return false;
    if (docRoot == root && fill != null) return true;

    root         = docRoot;
    nameLabel    = root.Q<Label>("name");
    fill         = root.Q<VisualElement>("fill");
    hpValueLabel = root.Q<Label>("hp-value");
    atkLabel     = root.Q<Label>("atk");
    spdLabel     = root.Q<Label>("spd");

    if (nameLabel != null) nameLabel.style.display = DisplayStyle.None;
    if (atkLabel  != null) atkLabel.style.display  = DisplayStyle.None;
    if (spdLabel  != null) spdLabel.style.display  = DisplayStyle.None;

    track = fill != null ? fill.parent : null;
    if (track != null)
    {
        track.style.borderTopWidth    = 2;
        track.style.borderBottomWidth = 2;
        track.style.borderLeftWidth   = 2;
        track.style.borderRightWidth  = 2;

        shieldFill = track.Q<VisualElement>("shield-fill");
        if (shieldFill == null)
        {
            shieldFill = new VisualElement { name = "shield-fill" };
            shieldFill.style.position        = Position.Absolute;
            shieldFill.style.top             = 0;
            shieldFill.style.bottom          = 0;
            shieldFill.style.backgroundColor = new Color(90f / 255f, 160f / 255f, 255f / 255f);
            track.Add(shieldFill);
        }
    }

    return fill != null;
}
```

**S47 CAMBIO:** Eliminadas resoluciones de roleElementRow, marksAllyRow, armedRow, marksEnemyRow, reactionRow. Queda solo el track + shieldFill.

## Update() (S47 IGUAL)

```csharp
private void Update() => Apply();
```

Llama Apply() cada frame.

## LateUpdate (S47 IGUAL)

```csharp
private void LateUpdate()
{
    if (cam == null)
    {
        if (Camera.main == null) return;
        cam = Camera.main.transform;
    }
    Vector3 toCam = transform.position - cam.position;
    if (uprightOnly) toCam.y = 0f;
    if (toCam.sqrMagnitude > 0.0001f)
        transform.rotation = Quaternion.LookRotation(toCam);
}
```

Billboard hacia Camera.main (sin cambios desde S43).

## S47 Cambios (VERSIÓN SIMPLIFICADA)

**Radicales:**
- `Bind()` reescrito sin argumentos
- Eliminados métodos `SetStatus()`, `SetElementState()`, `FlashReaction()`
- Eliminadas filas dinámicas (roleElementRow, marksAllyRow, armedRow, marksEnemyRow, reactionRow)
- Eliminados helpers `BuildMarkChip()`, `BuildStateLabel()`

**Nuevos métodos:**
- `SetActiveTurn(bool value)` — marca turno activo (marco dorado)
- `SetTargeted(bool value)` — marca objetivo (marco rojo)

**Nuevos campos:**
- `activeTurn` (bool)
- `targeted` (bool)

**Simplificaciones de Apply():**
- Solo interpola HP
- Solo renderiza fill + shieldFill
- Solo renderiza label hp-value
- Solo renderiza borders (coloreados por BorderColor)

**Impacto:** Barra minimal, datos desacoplados de la visualización. CombatVisualizerService llama SetHp/SetShield/SetActiveTurn/SetTargeted en secuencia.

## S45 Cambios (Preservados en S47)

- Nuevo campo serializado `hideForDebug` (bool, default false)
- Gate en Apply(): root.style.display = hideForDebug ? DisplayStyle.None : DisplayStyle.Flex
- Early return si hideForDebug (eficiencia)

## Vinculado a

- [[Index/03 - Combat System]]
- [[CombatVisualizerService]] — llama Bind() / SetHp() / SetShield() / SetActiveTurn() / SetTargeted()
- [[MoriMonchiVisualizer]] — prefab padre (component child)
- [[UIDocument]] — ref a CombatHpBar.uxml (elementos base name, hp-value, fill)

## Notas

- **S47:** Completamente reescrito a barra minimal. Antigua funcionalidad de marcas/estados removida (vivía en CombatOrderBarUITK antes).
- **Marcos:** Bordes de 2px, colores dinámicos (rojo/dorado/clear) en BorderColor()
- **Escudo:** Segmento azul continuo dentro del track, sin animación separada
- **Billboard:** Orienta hacia cámara en LateUpdate, independiente de slot rotation
- **hideForDebug:** Toggle rápido en inspector para ocultar durante debug
