---
tags: [utility, static, ui, styling]
---

# NeedsDisplay

**Ruta:** `UI/NeedsDisplay.cs`

**Responsabilidad:** Helper estático compartido para cálculo de color y relleno de barras de necesidades. Normaliza valores heterogéneos (Health/Energy [0-100], Affect [-100, 100]) a visuals coherentes (clases CSS, fill [0-1]).

## Métodos Públicos

| Método | Parámetros | Retorna | Descripción |
|--------|-----------|---------|-------------|
| `ColorClass` | `NeedType need`, `float value` | `string` | Clase UITK (CSS) según estado: `exp-bar--good` (≥60%), `exp-bar--warn` (≥30%), `exp-bar--crit` (<30%) |
| `Fill01` | `NeedType need`, `float value` | `float` | Normaliza [0-1] para barras visuales |

## Lógica

**Fill01:**
- Health/Energy: `Clamp01(value / 100)` → rango [0, 100] mapea a [0, 1]
- Affect: `Clamp01((value + 100) / 200)` → rango [-100, 100] mapea a [0, 1]

**ColorClass:**
- `Fill01 >= 0.6f` → `"exp-bar--good"` (verde)
- `Fill01 >= 0.3f` → `"exp-bar--warn"` (amarillo)
- `Fill01 < 0.3f` → `"exp-bar--crit"` (rojo)

## Caso de uso

**UI de ficha (MorimonchiDetailInfoUITK):**
```csharp
var fill = NeedsDisplay.Fill01(NeedType.Health, dna.Needs.Health);
var css = NeedsDisplay.ColorClass(NeedType.Health, dna.Needs.Health);
bar.style.width = new Length(fill * 100, LengthUnit.Percent);
bar.AddToClassList(css);
```

**Grid:**
- Mostrar las tres barras con relleno y color coherente
- Actualizar en eventos `OnRegistryChanged`

## Notas

- Sin estado: métodos puros, deterministas
- Sin referencias: solo parámetros, retorna primitivos
- CSS classes pre-definidas en UITK (deben estar en USS o pandilla de Shapes)

## Vinculado a

[[Index/23 - Arena y bajada nocturna]]
[[Index/05 - UI System]]

**Conexiones:** [[NeedsState]], [[MorimonchiDetailInfoUITK]], [[CreatureGridUITK]], [[InfoOverlayUITK]]
