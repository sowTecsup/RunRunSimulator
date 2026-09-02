---
tags: [script, ui]
---

# InfoOverlayUITK.cs

**Ruta:** `UI/InfoOverlayUITK.cs`

**Responsabilidad:** Overlay contextual siempre-visible (top-left hints leyenda, top-right fecha/dabloons/material). **S68:** InputHint.Action renombrado a ActionKey (guarda key de localización); días/meses ahora de `Loc.Culture.DateTimeFormat`. **S68 (addendum):** Selector de idioma v1: botones EN/ES al pie de la leyenda (clases `.lang-row`/`.lang-btn`/`.lang-btn--active` en InfoOverlayUITKStyle.uss); suscrita a `LocalizationSettings.SelectedLocaleChanged` para re-renderizar hints+fecha+dabloons al cambiar idioma; llama `Loc.ApplySavedLocale()` en Start para restaurar idioma guardado. **S93:** Usa `UiPanels.RootOf()` para resolver root. **S95:** Agregado `materialLabel` para mostrar AdventureMaterial en top-right junto dabloons.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|----------|
| `document` | `UIDocument` | UIToolkit doc tree (overlay siempre visible) |
| `hints` | `InputHint[]` | Array de controles mostrados top-left (WASD/E/Q/etc.) |

## InputHint struct

| Campo | Tipo | Descripción |
|-------|------|----------|
| `Key` | `string` | Etiqueta key (e.g., "WASD", "E", "Click") |
| `ActionKey` | `string` | **S68** Clave de localización (e.g., "ui.overlay.hint.move", "ui.overlay.hint.interact") |

## Lifecycle

| Método | Descripción |
|--------|----------|
| `OnEnable()` | Suscribe `GameEvents.OnInventoryChanged` + `OnInventoryReloaded` (dabloons/material) + `LocalizationSettings.SelectedLocaleChanged` (hints+fecha) |
| `Start()` | Restaura idioma guardado via `Loc.ApplySavedLocale()`. Resuelve UI refs (dateLabel, dabloonsLabel, materialLabel). BuildHints. Refresh inicial |
| `Update()` | Refresca fecha cada `DateRefreshInterval = 1s` (timer) |
| `OnDisable()` | Desuscribe todos los eventos |
| `RefreshDate(force)` | Recalcula fecha actual con locale + format args. Evita rebuild si texto no cambió (except si force=true) |
| `RefreshDabloons(inv)` | **S95** Renderiza dabloons count vía `Loc.Tr(DabloonsKey, inv.Dabloons)` y material vía `Loc.Tr(MaterialKey, inv.AdventureMaterial)` |
| `BuildHints(container)` | **S68 addendum** Crea hints + langRow (botones EN/ES). Llamado en Start + HandleLocaleChanged |
| `MakeLangButton(code, label)` | **S68 addendum** Crea Button con callback `SetLocale(code)`, aplica clase active si CurrentCode == code |
| `HandleLocaleChanged(locale)` | **S68 addendum** Re-renderiza hints + fecha + dabloons/material tras cambio de idioma (suscriptor SelectedLocaleChanged) |

**Vinculado a:**
- [[Index/05 - UI System]]
- [[Index/14 - Localization]]
- [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:**
- `Loc` (traducción + selector de idioma persistente)
- `LocEnumMaps` (indirecto, vía Loc.Tr)
- `GameManager.Inventory` (dabloons + material source)
- `GameEvents` (OnInventoryChanged, OnInventoryReloaded)
- `LocalizationSettings` (SelectedLocaleChanged event listener)
- `PlayerInputs` (indirecto, hints son reference)
- [[UiPanels]] (helper S93)

