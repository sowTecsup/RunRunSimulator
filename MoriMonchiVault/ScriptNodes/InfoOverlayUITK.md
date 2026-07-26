---
tags: [script, ui]
---

# InfoOverlayUITK.cs

**Ruta:** `UI/InfoOverlayUITK.cs`

**Responsabilidad:** Overlay contextual siempre-visible (top-left hints leyenda, top-right fecha/dabloons). **S68:** InputHint.Action renombrado a ActionKey (guarda key de localización); días/meses ahora de `Loc.Culture.DateTimeFormat`. **S68 (addendum):** Selector de idioma v1: botones EN/ES al pie de la leyenda (clases `.lang-row`/`.lang-btn`/`.lang-btn--active` en InfoOverlayUITKStyle.uss); suscrita a `LocalizationSettings.SelectedLocaleChanged` para re-renderizar hints+fecha+dabloons al cambiar idioma; llama `Loc.ApplySavedLocale()` en Start para restaurar idioma guardado.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | UIToolkit doc tree (overlay siempre visible) |
| `hints` | `InputHint[]` | Array de controles mostrados top-left (WASD/E/Q/etc.) |

## InputHint struct

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Key` | `string` | Etiqueta key (e.g., "WASD", "E", "Click") |
| `ActionKey` | `string` | **S68** Clave de localización (e.g., "ui.overlay.hint.move", "ui.overlay.hint.interact") |

## Cambios S68 (Localization-ready)

**InputHint struct:**
- Campo `Action` → renombrado a `ActionKey` (string, clave de localización en lugar de action name)
- Ejemplo: `ActionKey = "ui.overlay.hint.interact"` → resuelve a `Loc.Tr("ui.overlay.hint.interact")` según locale activo

**DateTimeFormat:**
- `DayNames` y `MonthNames` ahora obtenidos de `Loc.Culture.DateTimeFormat.GetDayName(dayOfWeek)` / `GetMonthName(month)` en lugar de arrays privados
- Permite formato de fechas/horas según locale (e.g., "lunes" vs "Monday", "enero" vs "January")
- Helper `Capitalize()` usa `char.ToUpper(value[0], culture)` para respetar reglas de mayúscula del idioma

**Líneas de localización:**
- Línea 43: `const string DateFormatKey = "ui.overlay.date.format"` (template fecha con placeholders día/mes)
- Línea 44: `const string DabloonsKey = "ui.overlay.dabloons"` (formato dabloons con count)
- Línea 98: `Loc.Tr(DateFormatKey, dayName, day, monthName, year)` (fecha localizada)
- Línea 113: `Loc.Tr(DabloonsKey, inv.Dabloons)` (dabloons localizados)
- Línea 130: `Loc.Tr(hint.ActionKey)` (hints localizados)

## Cambios S68 (addendum) — Selector de idioma v1

**Botones EN/ES:**
- Línea 137-141: BuildHints() agrega `langRow` al final de hints container (footer)
- Línea 139-140: Dos botones `MakeLangButton("en", "EN")` y `MakeLangButton("es", "ES")`
- Clases CSS: `lang-row` (contenedor), `lang-btn` (base), `lang-btn--active` (estado actual)
- Definidas en `InfoOverlayUITKStyle.uss` (no en C#)

**Callback botón:**
- Línea 146: `new Button(() => Loc.SetLocale(code))` — cambio de idioma al click
- Línea 148: Aplica clase `lang-btn--active` al botón que corresponde a `Loc.CurrentCode`

**Suscripción a cambios locale:**
- Línea 55: OnEnable suscribe `LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged`
- Línea 62: OnDisable desuscribe
- Línea 152-160: Manejador `HandleLocaleChanged(locale)` re-renderiza hints+fecha+dabloons

**Restauración idioma guardado:**
- Línea 67: Start() llama `Loc.ApplySavedLocale()` para restaurar idioma guardado en PlayerPrefs "mm_locale"
- Precondición: `GameManager.Instance.Inventory` ya debe estar cargado (que lo está en punto de Start)

**Lifecycle:**

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe `GameEvents.OnInventoryChanged` + `OnInventoryReloaded` (dabloons) + `LocalizationSettings.SelectedLocaleChanged` (hints+fecha) |
| `Start()` | Restaura idioma guardado via `Loc.ApplySavedLocale()`. Resuelve UI refs (dateLabel, dabloonsLabel). BuildHints. Refresh inicial |
| `Update()` | Refresca fecha cada `DateRefreshInterval = 1s` (timer) |
| `OnDisable()` | Desuscribe todos los eventos |
| `RefreshDate(force)` | Recalcula fecha actual con locale + format args. Evita rebuild si texto no cambió (except si force=true) |
| `RefreshDabloons(inv)` | Renderiza dabloons count vía `Loc.Tr(DabloonsKey, inv.Dabloons)` |
| `BuildHints(container)` | **S68 addendum** Crea hints + langRow (botones EN/ES). Llamado en Start + HandleLocaleChanged |
| `MakeLangButton(code, label)` | **S68 addendum** Crea Button con callback `SetLocale(code)`, aplica clase active si CurrentCode == code |
| `HandleLocaleChanged(locale)` | **S68 addendum** Re-renderiza hints + fecha + dabloons tras cambio de idioma (suscriptor SelectedLocaleChanged) |

**Notas S68 (addendum):**
- Panel siempre visible (picking-mode Ignore en UIDocument, no come clicks)
- Botones idioma clickeables solo cuando cursor libre (overlay picking-Ignore, Button hijos reciben clicks)
- BuildHints() recreada en cada cambio de idioma (labels hints resuelven nuevas keys)
- Persistencia automática: SetLocale() guarda en PlayerPrefs, ApplySavedLocale() restaura al inicio
- Sin caché: cada frame Update() o HandleLocaleChanged() recurre a Loc.Tr() con locale actual

**Vinculado a:**
- [[Index/05 - UI System]]
- [[Index/14 - Localization]]

**Conexiones:**
- `Loc` (traducción + selector de idioma persistente)
- `LocEnumMaps` (indirecto, vía Loc.Tr)
- `GameManager.Inventory` (dabloons source)
- `GameEvents` (OnInventoryChanged, OnInventoryReloaded)
- `LocalizationSettings` (SelectedLocaleChanged event listener)
- `PlayerInputs` (indirecto, hints son reference)
