---
tags: [script, ui, world]
---

# ArenaPaletteControl.cs

**Ruta:** `World/Expedition/ArenaPaletteControl.cs`

**Responsabilidad:** Presenta botones UIToolkit para cambiar de paleta en el HUD del arena. Se suscribe a `ArenaPaletteApplier.CurrentIndex` en cada frame y resalta el botón activo con la clase CSS `hud-palette-btn--on`. Construye los botones lazy (`TryBuild`) a partir de los palaes en `ArenaPaletteApplier.Palettes`, cada uno llama `ArenaSandbox.SetPaletteIndex()` al hacer click. Requiere refs a `UIDocument`, `ArenaSandbox` y `ArenaPaletteApplier`.

**Vinculado a:** [[Index/05 - UI System]], S114

**Conexiones:** [[ArenaPaletteApplier]], [[ArenaSandbox]], [[UIManager]]

**Campos Públicos (Serialized)**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `uiDocument` | `UIDocument` | Raíz UIToolkit (requerida) |
| `sandbox` | `ArenaSandbox` | Para `SetPaletteIndex()` |
| `palette` | `ArenaPaletteApplier` | Para `Palettes` y `CurrentIndex` |

**Métodos Privados Clave**

| Método | Descripción |
|--------|-------------|
| `TryBuild()` | Busca contenedor `#hud-palettes`, crea botones lazy, guarda handlers |
| `Update()` | Sincroniza resaltado del botón activo cada frame |

**Detalles S114**

Nuevo componente que materializa la UI de paletas. La construcción lazy permite que la UI esté lista antes de que este componente intente conectarse. Los handlers se guardan para poder desuscrirse en `OnDisable`. No persiste estado: solo lee y refleja el índice actual.
