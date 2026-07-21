---
tags: [script, ui, helper, static]
---

# MonchiPortraitUI.cs

**Ruta:** `UI/MonchiPortraitUI.cs`

**Responsabilidad:** Helper estático de pintado de cartas retrato. Abstrae lógica fallback (intenta retrato/headshot vía MonchiPortraitService, cae a BaseColor/gris). **S58:** Nuevo `ApplyHeadshot()` para cartas de combate con headshot lateral. Tres entrypoints + nuevo: `Apply()` foto full-body, `ApplyLive()` criatura viva, `ApplyHeadshot()` cabeza lateral. Consumido por ~12 sitios UI.

## Métodos Públicos

| Método | Firma | Descripción |
|--------|-------|-------------|
| `Apply` | `(VisualElement, dna)` | Retrato full-body foto estática |
| `ApplyLive` | `(VisualElement, dna)` | Retrato criatura viva (fallback Apply) |
| `ApplyHeadshot` | `(VisualElement, dna)` | **S58** Headshot lateral (512×192) |
| `Apply` | `(Image, dna)` | uGUI legacy foto full-body |

## Método S58: ApplyHeadshot

```csharp
public static void ApplyHeadshot(VisualElement element, CreatureDNA dna)
{
    if (element == null) return;

    var tex = MonchiPortraitService.Instance != null && dna != null
        ? MonchiPortraitService.Instance.GetHeadshot(dna)
        : null;

    if (tex != null)
    {
        element.style.backgroundImage = new StyleBackground(tex);
        element.style.backgroundColor = Color.clear;
        element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);  // Crop
    }
    else
    {
        element.style.backgroundImage = StyleKeyword.Null;
        element.style.backgroundColor = dna != null ? dna.BaseColor : FallbackEmpty;
    }
}
```

**Diferencias vs Apply:**
- Llama `GetHeadshot()` en lugar de `GetPortrait()`
- BackgroundSize **Cover** (crop, no scale) — headshot siempre llena el espacio
- Si falla: fallback a BaseColor (como Apply)

**Consumidores S58:**
- [[CombatOrderBarUITK]] — headshots en cartas de orden (línea 157: `MonchiPortraitUI.ApplyHeadshot(headshot, dna)`)
- [[CombatVisualizerPanelUITK]] — headshots en log "Eventos" (línea 106: `MonchiPortraitUI.ApplyHeadshot(headshot, ResolveDna(...))`)

## Método Apply (Full-Body Foto)

```csharp
public static void Apply(VisualElement element, CreatureDNA dna)
{
    if (element == null) return;

    var tex = MonchiPortraitService.Instance != null && dna != null
        ? MonchiPortraitService.Instance.GetPortrait(dna)
        : null;

    if (tex != null)
    {
        element.style.backgroundImage = new StyleBackground(tex);
        element.style.backgroundColor = Color.clear;
        element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);  // Scale fit
    }
    else
    {
        element.style.backgroundImage = StyleKeyword.Null;
        element.style.backgroundColor = dna != null ? dna.BaseColor : FallbackEmpty;
    }
}
```

**BackgroundSize Contain:** Escala sin crop, mantiene aspect ratio.

## Método ApplyLive

Intenta vivo (MonchiLivePortrait), fallback a Apply().

## Método Apply (uGUI Legacy)

Sprite full-body para Image components.

## Cambios S58

**Nuevo método ApplyHeadshot:**
- Encapsula GetHeadshot() con fallback a BaseColor
- BackgroundSize.Cover para crop (headshots 512×192 llenan tarjeta)
- Usado por cartas de combate 3v3 (CombatOrderBarUITK, CombatVisualizerPanelUITK)

**Impacto:** Cartas de combate 3v3 muestran headshot lateral (yaw 140°) en lugar de nombre/full-body.

## Campos

```csharp
private static readonly Color FallbackEmpty = new Color(0.24f, 0.24f, 0.28f);
```

## Vinculado a

- [[MonchiPortraitService]] — proveedor GetPortrait/GetHeadshot
- [[MonchiLivePortrait]] — proveedor RenderTexture live
- [[CombatOrderBarUITK]] — **S58** consumer ApplyHeadshot
- [[CombatVisualizerPanelUITK]] — **S58** consumer ApplyHeadshot

## Conexiones

**Entrada:**
- `Apply/ApplyLive/ApplyHeadshot(VisualElement, dna)` desde UI
- `Apply(Image, dna)` uGUI legacy

**Salida:**
- Mutación style (backgroundImage/backgroundColor)
- Mutación sprite/color (uGUI)

## Notas

- Estático, sin estado
- Fallback robusto (BaseColor si service no existe)
- ApplyHeadshot usa Cover (crop) vs Apply Contain (scale)
- S58: ApplyHeadshot para cartas mini headshot 512×192
