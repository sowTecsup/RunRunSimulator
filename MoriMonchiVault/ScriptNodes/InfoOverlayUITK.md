---
tags: [script, ui]
---

# InfoOverlayUITK.cs

**Ruta:** `UI/InfoOverlayUITK.cs`

**Responsabilidad:** Overlay contextual siempre-visible (top-left hints leyenda, top-right fecha/dabloons/material, **S121:** toast de retorno expedición). **S68:** InputHint.Action renombrado a ActionKey. **S68 (addendum):** Selector de idioma v1 (botones EN/ES). **S93:** Usa `UiPanels.RootOf()`. **S95:** Agregado `materialLabel`. **S121:** Suscriptor de `GameEvents.OnExpeditionReturned` para mostrar toast 6s con resultado.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|----------|
| `document` | `UIDocument` | UIToolkit doc tree (overlay siempre visible) |
| `hints` | `InputHint[]` | Array de controles mostrados top-left |
| `toastSeconds` | float, Min(0) | Duración del toast expedición (default 6s, **S121**) |

## Campos Privados (S121)

- `Label expeditionToastLabel` — elemento de toast expedición (puede ser null en edición)
- `float toastTimer` — cuenta atrás del toast (se decrementa cada frame)
- `ExpeditionReturn? pendingToast` — resultado en cola si toast label no está wired aún

## Lifecycle (S121)

| Método | Descripción |
|--------|----------|
| `OnEnable()` | Suscribe a `GameEvents.OnExpeditionReturned += HandleExpeditionReturned` |
| `Start()` | Resuelve `expeditionToastLabel`. Si hay `pendingToast` muestra (`ShowExpeditionToast`) |
| `Update()` | Decrementa `toastTimer`; si ≤0 oculta toast label |
| `OnDisable()` | Desuscribe `OnExpeditionReturned` |
| `HandleExpeditionReturned(ExpeditionReturn r)` | **(S121)** Callback de evento. Si toast label wired: `ShowExpeditionToast(r)`. Si no: guarda en `pendingToast` |
| `ShowExpeditionToast(ExpeditionReturn r)` | **(S121)** Renderiza: `ui.overlay.expedition.return` con params (Seed, PlayerSecured, RivalSecured, MaterialGained, EnergySpent). Aplica clase `toast--{win\|lose\|draw}` según Winner. Fija `toastTimer = toastSeconds` |

## Toast Expedición (S121)

**Componentes:**
- Label con clase `.expedition-toast` (texto localizador + colores dinámicas)
- Color según resultado: `toast--win` (azul), `toast--lose` (rojo), `toast--draw` (gris)
- Duración: 6 s (hardcodeado, configurable via `toastSeconds`)

**Formato localizador:** `"ui.overlay.expedition.return"` con 5 params:
```
Volviste de la sala {0} · {1}-{2} · +{3} material · −{4} energía
```
Ej: "Volviste de la sala 20234078 · 38-25 · +33 material · −46 energía"

**Flujo:**
1. `ExpeditionBridge.ApplyResult()` calcula totales → `GameEvents.ExpeditionReturned(return)`
2. `InfoOverlayUITK.HandleExpeditionReturned(r)` recibe evento
3. `ShowExpeditionToast(r)`: renderiza label, aplica clase, fija timer
4. `Update()`: decrementa timer, oculta cuando ≤0

## Cambios por Sesión

- **S68:** InputHint.ActionKey renombrado, localizador key
- **S68 addendum:** Selector EN/ES, suscriptor SelectedLocaleChanged
- **S93:** UiPanels.RootOf() helper
- **S95:** materialLabel para AdventureMaterial
- **S121:** Toast de retorno expedición con GameEvents.OnExpeditionReturned (nuevo evento)

## S120-S122

- **S120:** Sin cambios (ExpeditionPanelUITK es componente separado).
- **S121:** `OnExpeditionReturned` nuevo evento (GameEvents). Toast label + campos toastTimer/pendingToast. `HandleExpeditionReturned`, `ShowExpeditionToast` métodos nuevos. Clase `.expedition-toast` y tonos `toast--{win|lose|draw}`.
- **S122:** Sin cambios.

## Invariantes

- Toast label puede estar null (edición o docs sin UXML); pendingToast guarda resultado para mostrar cuando esté disponible
- Textos y idiomas sincronizados vía Loc
- Suscripción/desuscripción simétrica OnEnable/OnDisable

## Vinculado a

[[Index/05 - UI System]], [[Index/14 - Localization]], [[Index/24 - Puente Tienda-Arena]] (S121)

**Conexiones:** [[Loc]], [[GameManager]], [[GameEvents]], [[ExpeditionBridge]], [[UiPanels]]
