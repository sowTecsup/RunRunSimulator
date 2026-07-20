---
tags: [script, prototype, visual, genetics]
---

# SpiderPaletteApplier.cs

**Ruta:** `Prototype/Spider/SpiderPaletteApplier.cs`

**Responsabilidad:** Aplicador de colores genéticos basado en regla 60/30/10. Implementa el contrato visual de color sin dependencia del modelo específico. Método `Apply(baseColor, secondaryColor)` tinta: bodyRoots con baseColor (60%), faceRoots con secondaryColor (30%), detailRoots con derivado de secundario (10% acento). `ApplyFromDna(dna)` extrae colores de `CreatureDNA` e invoca `Apply()`. `ApplyMaterial(material)` swappea material (p.ej. FurType database) en todos los renderers.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `bodyRoots` | `Transform[]` | Transforms para cuerpo (tinta baseColor) |
| `faceRoots` | `Transform[]` | Transforms para cara (tinta secondaryColor) |
| `detailRoots` | `Transform[]` | Transforms para detalles (tinta acento derivado) |

## Métodos Públicos

| Método | Parámetros | Retorna | Descripción |
|--------|-----------|---------|-------------|
| `Apply` | Color baseColor, Color secondaryColor | void | Tinta 60/30/10 via MaterialPropertyBlock |
| `ApplyFromDna` | CreatureDNA dna | void | Extrae colores y llama Apply |
| `ApplyMaterial` | Material material | void | Swappea material en todos los renderers |

## Implementación

### Apply (Regla 60/30/10)

```
Color accent = ColorGenetics.DeriveSecondary(secondaryColor)
Tint(bodyRoots, baseColor)         // 60% = cuerpo
Tint(faceRoots, secondaryColor)    // 30% = cara
Tint(detailRoots, accent)          // 10% = detalles
```

Cada `Tint()` itera renderer en los transforms y aplica `MaterialPropertyBlock` con `_BaseColor` y `_Color` (ambas propiedades para compatibilidad shader).

### ApplyFromDna

Extrae `dna.BaseColor` y `dna.SecondaryColor` → llama `Apply()`.

### ApplyMaterial

Itera renderers, reemplaza `sharedMaterials` con el material pasado (p.ej. del `FurTypeDatabaseSO`).

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `Swap(Transform[] roots, Material material)` | Reemplaza material en todos los renderers de la jerarquía |
| `Tint(Transform[] roots, Color color)` | Aplica MaterialPropertyBlock con color a todos los renderers |

## Notas

- **Regla 60/30/10 SOBREVIVE:** Este sistema de colores se mantiene como regla definitiva del juego, aunque el prototipo araña sea descartado. El implementador siguiente debe usar esta clase o equivalente.
- **Sin Instantiate:** Usa `MaterialPropertyBlock` (no crea material), garantiza cambios dinámicos.
- **Compatibilidad shader:** Setea `_BaseColor` y `_Color` para compatibilidad con diferentes shaders (StandardShader, custom fur).
- **Null-safe:** Valida roots != null, renderers != null en iteraciones.
- **Consumidor:** `MoriMonchiController` lo usa opcionalmente vía `spiderVisual` serializado (si != null en Initialize/Rebind).

## Vinculado a

- [[Index/04 - Genetics & DNA]] — regla de color genético
- [[CreatureDNA]] — source de colores
- [[ColorGenetics]] — derivación de acento
- [[FurTypeDatabaseSO]] — material database (compatible con ApplyMaterial)

## Conexiones

**Usado por:**
- [[MoriMonchiController]] → Initialize/Rebind aplican color si `spiderVisual != null`
- [[SpiderDevPanel]] → botón "Colores random" invoca Apply con colores random

**Depende de:**
- [[ColorGenetics.DeriveSecondary()]] → calcula acento
- [[ColorGenetics.RandomBase()]] → genera base random (usado en DevPanel)
