---
tags: [script, ui, helper, static]
---

# MonchiPortraitUI.cs

**Ruta:** `UI/MonchiPortraitUI.cs`

**Responsabilidad:** Helper estático de pintado de cartas retrato. Abstrae lógica fallback (intenta retrato vía MonchiPortraitService, cae a BaseColor/gris). Tres entrypoints: `Apply()` foto full-body (VisualElement), `ApplyLive()` criatura viva, `Apply()` uGUI legacy (Image). **S93:** Pipeline de headshot eliminado (`ApplyHeadshot()` removido).

## Métodos Públicos

| Método | Firma | Descripción |
|--------|-------|-------------|
| `Apply` | `(VisualElement, dna)` | Retrato full-body foto estática |
| `ApplyLive` | `(VisualElement, dna)` | Retrato criatura viva (fallback Apply) |
| `Apply` | `(Image, dna)` | uGUI legacy foto full-body |

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
        element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
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

Intenta vivo vía [[MonchiLivePortrait]], fallback a Apply().

## Método Apply (uGUI Legacy)

Sprite full-body para Image components.

## Cambios Históricos

**S58 (obsoleto S93):**
- Introducción de `ApplyHeadshot()` para cartas de combate
- Método removido completamente en S93

**S93:**
- **Eliminado:** `ApplyHeadshot()` (pipeline headshot eliminado de MonchiPortraitService)
- Impacto: cartas de combate que usaban headshots ahora desaparecidas (combate demo S75-S92 fue descartado)

## Campos

```csharp
private static readonly Color FallbackEmpty = new Color(0.24f, 0.24f, 0.28f);
```

## Vinculado a

- [[MonchiPortraitService]] — proveedor GetPortrait
- [[MonchiLivePortrait]] — proveedor RenderTexture live

## Conexiones

**Entrada:**
- `Apply(VisualElement, dna)` desde UI (VisualElement)
- `ApplyLive(VisualElement, dna)` desde UI (live criatura)
- `Apply(Image, dna)` uGUI legacy

**Salida:**
- Mutación style (backgroundImage/backgroundColor)
- Mutación sprite/color (uGUI)

## Notas

- Estático, sin estado
- Fallback robusto (BaseColor si service no existe)
