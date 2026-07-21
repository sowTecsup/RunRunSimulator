---
tags: [script, visual, component]
---

# MonchiVisualizer.cs

**Ruta:** `World/Creatures/MonchiVisualizer.cs`

**Responsabilidad:** Visualizador del modelo Suriyun. Instancia body FBX por BodyShapeID, mapea renderers (Face, Wings, Arms, etc.), aplica tintado por ColorGenetics.BuildHarmony. `SetMood()` swapea material Face. **S58:** Nuevo `SetGhost(alpha)` para fade persistente de cadáveres — activa keywords de transparencia del Unity Toon Shader (_TransparentEnabled, _ClippingMode, _Tweak_transparency, renderQueue).

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `SetBank(MonchiVisualBankSO)` | Asigna banco visual |
| `SetFurDatabase(FurTypeDatabaseSO)` | Asigna database de pelajes |
| `Assemble(CreatureDNA dna)` | Instancia body, mapea renderers, aplica look |
| `RefreshLook(CreatureDNA dna)` | Retinta sin re-instanciar |
| `SetMood(MonchiMood)` | Swapea material Face |
| `SetGhost(float alpha)` | **S58** Activa transparencia (fade cadáver) |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Animator` | `Animator` | Animator del body |
| `ModelRoot` | `Transform` | Raíz del modelo |

## Cambios S58

**SetGhost(float alpha) línea 106-134:**

```csharp
public void SetGhost(float alpha)
{
    if (bodyInstance == null) return;
    bool ghost = alpha < 0.999f;
    foreach (var renderer in bodyInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
    {
        foreach (var mat in renderer.materials)
        {
            if (ghost)
            {
                mat.SetFloat("_TransparentEnabled", 1f);
                mat.SetFloat("_ClippingMode", 2f);
                mat.DisableKeyword("_IS_CLIPPING_OFF");
                mat.EnableKeyword("_IS_CLIPPING_TRANSMODE");
                mat.renderQueue = 3000;
                mat.SetFloat("_Tweak_transparency", Mathf.Clamp01(alpha) - 1f);
            }
            else
            {
                mat.SetFloat("_Tweak_transparency", 0f);
                mat.SetFloat("_TransparentEnabled", 0f);
                mat.SetFloat("_ClippingMode", 0f);
                mat.EnableKeyword("_IS_CLIPPING_OFF");
                mat.DisableKeyword("_IS_CLIPPING_TRANSMODE");
                mat.renderQueue = -1;
            }
        }
    }
}
```

**Parámetros:**
- `alpha` (0-1): transparencia objetivo
- alpha < 0.999f → activa ghost mode
- alpha >= 0.999f → restaura opaco

**Lógica:**
1. Si ghost=true: habilita keywords _TransparentEnabled, _ClippingMode=2 (transparencia), desactiva _IS_CLIPPING_OFF, habilita _IS_CLIPPING_TRANSMODE
2. Setea _Tweak_transparency = alpha - 1 (rango -1 a 0 en shader, donde 0=opaco, -1=transparente)
3. RenderQueue = 3000 (translúcido, detrás de opacos)
4. Si ghost=false: restaura valores opaco (keywords, queue -1)

**Uso S58 (CombatVisualizerService):**
- Línea 610: `unit?.Instance?.SetGhost(Mathf.Lerp(1f, corpseAlpha, ...))` en CorpseFade
- Anima alpha desde 1 (opaco) a corpseAlpha (0.35 transparente)
- Cadáver persiste visible pero fantasmal en tablero

## Flujo Muertes S58

1. PlayDefeat() — anima caída
2. Pausa deathPauseSeconds
3. Bar.SetActive(false) — oculta barra
4. CorpseFade inicia — lerp SetGhost(1 → 0.35)
5. Replay termina — cadáver transparente visible

**Al hacer Back (rewind):**
- Restore() llama SetGhost(1) si unit vuelve a vivo
- Cadáver opaco es visible, solo si murió en ese frame

## Vinculado a

- [[Index/10 - Visualization]]
- [[CombatVisualizerService]] — CorpseFade llama SetGhost (S58)
- [[MonchiVisualBankSO]], [[ColorGenetics]]

## Conexiones

**Entrada:**
- Assemble/RefreshLook: CreatureDNA
- SetGhost: CombatVisualizerService.CorpseFade

**Salida:**
- Modelo visual world-space
- Keywords Unity Toon Shader para fade

## Notas S58

- SetGhost solo afecta shader Toon (propiedades _Tweak_transparency, keywords)
- Alpha = 1 (opaco), 0.35 (fantasmal, default corpse)
- RenderQueue = 3000 (semi-transparentes detrás, pero encima de agua/cielo)
- Keywords: _IS_CLIPPING_TRANSMODE (clipping transparency), _IS_CLIPPING_OFF (normal)
- Muertes persistentes: cadáver queda visible en tablero durante replay
