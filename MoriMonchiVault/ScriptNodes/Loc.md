---
tags: [script, localization, utility]
---

# Loc.cs

**Ruta:** `Systems/Localization/Loc.cs`

**Responsabilidad:** Wrapper estático sobre Unity Localization (package 1.5.12). Interfaz simplificada para traducción de strings: `Tr(key)` → localización actual con fallback a la key si entry ausente. `Culture` property accede CultureInfo del locale activo (usado por NameTag y otros para formatos de fecha/hora). **S68 (addendum):** Selector de idioma persistente: `SetLocale(code)` cambia locale + guarda en PlayerPrefs "mm_locale"; `ApplySavedLocale()` restaura locale guardado (llamado por InfoOverlayUITK.Start); `CurrentCode` devuelve código del locale activo. Tabla única de strings: `"Strings"` (364+ entradas).

**Datos públicos:**
- `const string TableName = "Strings"` — nombre de la String Table Collection
- `const string LocalePrefKey = "mm_locale"` — clave PlayerPrefs para persistencia locale (**S68**)

**Properties:**
- `Culture` (get-only) — `System.Globalization.CultureInfo` del locale seleccionado via `LocalizationSettings.SelectedLocale`, fallback a `CurrentCulture` si ausente
- `CurrentCode` (get-only) — código del locale activo (e.g., "en", "es"), string vacío si no disponible (**S68**)

**Métodos públicos:**
- `Tr(string key) → string` — traduce `key` de la tabla activa; si entry no existe, retorna la key literal (fallback robusta)
- `Tr(string key, params object[] args) → string` — traduce con format args (e.g., `"You have {0} items"` + [5])
- `SetLocale(string code) → void` — **S68** cambia locale a `code`, guarda en PlayerPrefs "mm_locale", persiste entre sesiones. No-op si código inválido
- `ApplySavedLocale() → void` — **S68** restaura locale guardado de PlayerPrefs (sin-op si código guardado = CurrentCode). Llamado en InfoOverlayUITK.Start al iniciar

**Implementación:**
- Accede `LocalizationSettings.StringDatabase.GetTable(TableName)` (lazy-load)
- Consulta `table.GetEntry(key)`, luego `entry.GetLocalizedString()` (sin/con args)
- Sin caché: lookup cada llamada (performance OK para UI runtime)
- `SetLocale()` usa `LocalizationSettings.AvailableLocales.GetLocale(code)` para resolver; persiste en PlayerPrefs

**Uso típico:**
```csharp
Loc.Tr("nametag.petting")                                    // → "Acariciando…"
Loc.Tr("ui.detail.identity", gender, status, birthDate)     // → "Hembra · Libre · 25/07/2026 10:30"
if (Loc.CurrentCode != "es") Loc.SetLocale("es");            // cambiar a español, persiste
```

**Cambios S68 (addendum):**
- API nueva para selector de idioma persistente: `SetLocale()`, `ApplySavedLocale()`, `CurrentCode`
- Consumido por `InfoOverlayUITK` (botones EN/ES, llama `SetLocale` y suscrita a `SelectedLocaleChanged`)

**Vinculado a:**
- [[Index/05 - UI System]]
- [[Index/14 - Localization]]

**Conexiones:**
- `LocEnumMaps` (wrapper enum-specific sobre Loc.Tr)
- `InfoOverlayUITK` (selector de idioma UI, llama SetLocale/ApplySavedLocale)
- `NameTag`, `DetailInfoTabPresenter`, `CombatVisualizerService`, `NpcDialogueBank`, y 19+ scripts de UI/gameplay
- Unity Localization `LocalizationSettings` (único dueño de selector locale, emite SelectedLocaleChanged)
- PlayerPrefs (persistencia idioma guardado vía "mm_locale")
