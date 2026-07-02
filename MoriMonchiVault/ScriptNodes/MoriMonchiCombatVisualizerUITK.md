---
tags: [script, ui, combat]
---

# MoriMonchiCombatVisualizerUITK.cs

**Ruta:** `UI/MoriMonchiCombatVisualizerUITK.cs`

**Responsabilidad:** Barra de HP world-space + estado de efectos de UN combatiente del visualizer. Componente HIJO del prefab del peleador, con un `UIDocument` que apunta a `CombatHpBar.uxml` (elementos `name`, `hp-value`, `atk`, `spd`, `fill` y `effects`).

**Driven por el Service (sin `side`):** El `CombatVisualizerService` la maneja por referencia directa:
- `Bind(string displayName, float attack, float speed)`: fija nombre, ATK y VEL, resetea HP a 100%
- `SetHp(float current, float max)`: interpola fill + actualiza hp-value label
- **`SetStatus(List<CombatStatusMark>)`** ← **S34** actualiza chips de estado activo

**Binding resiliente + fix de árbol huérfano:** `EnsureRefs()` detecta cuando el `UIDocument` reconstruye su árbol comparando `docRoot != root`; re-resuelve elementos y marca dirty para reescribirlos. Sin esto, al retroceder (Back) la barra quedaría apuntando al árbol viejo.

**Billboard:** en `LateUpdate` orienta el panel hacia `Camera.main`, igual que [[NameTag]], independiente de rotación del slot.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Bind(string name, float atk, float spd)` | Fija nombre, stats, resetea HP a 100% |
| `SetHp(float current, float max)` | Actualiza HP (interpola fill) |
| `SetStatus(List<CombatStatusMark> marks)` | **S34** Setea estado de efectos activos |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | Ref opcional (auto-resuelto si null) |
| `fillLerpSeconds` | `float` | Duración interpolación HP (default 0.4s) |
| `uprightOnly` | `bool` | Si true, solo rota Y (billboard uprightless) |
| `palette` | `CombatPopupPaletteSO` | **S34** Paleta para colorear chips de estado |

## SetStatus Implementation — S34

```csharp
public void SetStatus(List<CombatStatusMark> marks)
{
    desiredStatus = marks ?? new List<CombatStatusMark>();
    statusDirty   = true;
}
```

En Apply():
```csharp
if (statusDirty && effectsRow != null)
{
    effectsRow.Clear();
    foreach (var mark in desiredStatus)
    {
        var chip = new Label(StatusText(mark));  // "V", "V×2", "A×3", etc.
        chip.style.fontSize          = 10;
        chip.style.unityFontStyleAndWeight = FontStyle.Bold;
        chip.style.paddingTop        = 1;
        chip.style.paddingBottom     = 1;
        chip.style.paddingLeft       = 3;
        chip.style.paddingRight      = 3;
        chip.style.marginRight       = 2;
        chip.style.borderTopLeftRadius     = 3;
        chip.style.borderTopRightRadius    = 3;
        chip.style.borderBottomLeftRadius  = 3;
        chip.style.borderBottomRightRadius = 3;
        chip.style.backgroundColor   = new Color(0f, 0f, 0f, 0.55f);
        chip.style.color             = palette != null ? palette.GetColor(MapKind(mark.Kind)) : Color.white;
        effectsRow.Add(chip);
    }
    statusDirty = false;
}
```

**Lógica:**
- Limpia `effectsRow` y recrea chips para cada mark
- Texto = inicial + stack count (ej: "V", "V×2", "A", "A×3")
- Color vía `palette.GetColor()` o fallback blanco
- Estilos: bold, pequeño (10px), borde redondeado, fondo semitransparente

## Status Helpers — S34

```csharp
private static string StatusText(CombatStatusMark mark)
{
    string initial = StatusInitial(mark.Kind);
    return mark.Stacks > 1 ? $"{initial}×{mark.Stacks}" : initial;
}

private static string StatusInitial(ModifierEffectKind kind)
{
    return kind switch
    {
        ModifierEffectKind.Poison       => "V",   // Veneno
        ModifierEffectKind.Burn         => "Q",   // Quemadura
        ModifierEffectKind.Regen        => "R",   // Regeneracion
        ModifierEffectKind.Stun         => "A",   // Aturdido
        ModifierEffectKind.ReturnDamage => "E",   // Espinas
        ModifierEffectKind.Heal         => "C",   // Cura
        _                               => kind.ToString().Substring(0, 1),
    };
}

private static CombatPopupKind MapKind(ModifierEffectKind kind)
{
    return kind switch
    {
        ModifierEffectKind.Poison       => CombatPopupKind.Poison,
        ModifierEffectKind.Burn         => CombatPopupKind.Burn,
        ModifierEffectKind.Regen        => CombatPopupKind.Regen,
        ModifierEffectKind.Stun         => CombatPopupKind.Stun,
        ModifierEffectKind.ReturnDamage => CombatPopupKind.Thorns,
        ModifierEffectKind.Heal         => CombatPopupKind.Heal,
        ModifierEffectKind.Synergy      => CombatPopupKind.Synergy,
        _                               => CombatPopupKind.Hit,
    };
}
```

## EnsureRefs — S34 Nota

```csharp
if (effectsRow == null)
{
    effectsRow = new VisualElement { name = "effects" };
    effectsRow.style.flexDirection = FlexDirection.Row;
    effectsRow.style.flexWrap      = Wrap.Wrap;
    effectsRow.style.marginTop     = 2;
    root.Add(effectsRow);
}
```

Si el UXML no trae `effectsRow`, se crea programáticamente. Fallback para compatibilidad con viejos UXML sin la fila de efectos.

## Dirty Pattern

- `staticDirty` — marca update de nombre/ATK/SPD
- `statusDirty` — marca update de estado (S34)
- `Update()` chequea flags y aplica cambios — patron dirty que **sobrevive rebuild** del árbol (EnsureRefs detecta cambio y re-crea)

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatVisualizerService]] — llamador de Bind/SetHp/SetStatus
- [[CombatStatusMark]] — input para SetStatus
- [[CombatPopupPaletteSO]] — colores de efectos
- [[CreatureDNA]] — datos del combatiente

## Conexiones

**Entrada:**
- `Bind(name, atk, spd)` — de CombatVisualizerService.BeginRoutine
- `SetHp(current, max)` — de CombatVisualizerService.PushHp
- `SetStatus(marks)` — de CombatVisualizerService.PushStatus ← S34

**Salida:**
- UIElements visuales (HP fill interpolado, estado de efectos)

## Notas

- **S34:** Nuevo método `SetStatus()` para visualización de efectos activos por turno
- **Iniciales:** V=Veneno, Q=Quemadura, R=Regeneracion, A=Aturdido, E=Espinas, C=Cura
- **Stack display:** "V×3" si 3+ apilados de mismo tipo
- **Color:** Via palette o blanco fallback
- **Efectsrow optional:** Si UXML no lo trae, se crea en EnsureRefs
- **Null-tolerante:** Palette null → color white; StatusA/B null deserializan como listas vacías
- **Dirty resilience:** El dirty pattern persiste cambios pendientes a través de re-creación de árbol
