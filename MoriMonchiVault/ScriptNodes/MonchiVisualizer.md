---
tags: [script, visual, component]
---

# MonchiVisualizer.cs

**Ruta:** `World/Creatures/MonchiVisualizer.cs`

**Responsabilidad:** Visualizador del modelo Suriyun. Instancia body FBX por BodyShapeID, mapea renderers (Face, Wings, Arms, etc.), aplica tintado por ColorGenetics.BuildHarmony. `SetMood()` swapea material Face. **S61:** `Assemble()` ahora hace `SetActive(false)` a los hijos viejos antes de `Object.Destroy()` — Destroy es diferido a fin de frame y el fotomatón renderiza en el mismo frame, causando superposición del cuerpo viejo en headshots batch.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `SetBank(MonchiVisualBankSO)` | Asigna banco visual |
| `SetFurDatabase(FurTypeDatabaseSO)` | Asigna database de pelajes |
| `Assemble(CreatureDNA dna)` | Instancia body, mapea renderers, aplica look; desactiva hijos viejos antes de destruir |
| `RefreshLook(CreatureDNA dna)` | Retinta sin re-instanciar |
| `SetMood(MonchiMood)` | Swapea material Face |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Animator` | `Animator` | Animator del body |
| `ModelRoot` | `Transform` | Raíz del modelo |

## Cambios S61

**Assemble() línea 40-45:**
```csharp
for (int i = modelRoot.childCount - 1; i >= 0; i--)
{
    var child = modelRoot.GetChild(i).gameObject;
    child.SetActive(false);              // NUEVO: desactiva antes de destruir
    Object.Destroy(child);
}
```

**Contexto:**
- Destroy() es una operación diferida que se ejecuta al fin del frame actual
- El fotomatón (headshot batch render) renderiza en el MISMO frame antes de que Destroy() se ejecute
- Sin SetActive(false), el body viejo sigue visible en el render, superponiéndose al cuerpo nuevo
- Con SetActive(false), el renderer se desactiva inmediatamente, saliendo de la vista del fotomatón

**Impacto:**
- Evita ghosting visual en headshots batch (artefactos de dos cabezas/cuerpos superpuestos)
- La instancia aún existe en memoria hasta fin de frame, pero es invisible

## Cambios S93

- **Removido:** método `SetGhost(float alpha)` (fue descartado; ghosting visual de cadáveres se maneja en otro lado o no se soporta más)

## Vinculado a

- [[Index/10 - Visualization]]
- [[MonchiVisualBankSO]], [[ColorGenetics]]

## Conexiones

**Entrada:**
- Assemble/RefreshLook: CreatureDNA
- SetMood: MonchiMoodDriver

**Salida:**
- Modelo visual world-space
- Material Face swapped por mood

## Notas S61

- Assemble() desactiva visualmente los hijos viejos inmediatamente (SetActive), luego los destruye diferido
- Fotomatón renderiza en el mismo frame; desactivar antes de Destroy evita ghosting
- Previene artefactos visuales en headshot batch (dos criaturas superpuestas)
