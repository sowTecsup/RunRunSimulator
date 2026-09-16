---
tags: [script, ui]
---

# InfoOverlayUITK.cs

**Ruta:** `UI/InfoOverlayUITK.cs`

**Responsabilidad:** Overlay contextual siempre-visible (top-left hints leyenda, top-right fecha/dabloons/material, **S121:** toast de retorno expedición). **S68:** InputHint.Action renombrado a ActionKey. **S68 (addendum):** Selector de idioma v1 (botones EN/ES). **S93:** Usa `UiPanels.RootOf()`. **S95:** Agregado `materialLabel`. **S121:** Suscriptor de `GameEvents.OnExpeditionReturned`. **S124:** Toast diferencia entre Lost (derrota) y retorno exitoso; muestra Floors + HealthLost.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | UIToolkit doc tree (overlay siempre visible) |
| `hints` | `InputHint[]` | Array de controles mostrados top-left |
| `toastSeconds` | `float` | Duración del toast expedición (default 6s) |

## Campos Privados (S121+S124)

- `Label dateLabel` — hora y fecha top-right
- `Label dabloonsLabel` — cantidad de dabloons
- `Label materialLabel` — cantidad de material de aventura
- `Label expeditionToastLabel` — toast de retorno expedición
- `float toastTimer` — cuenta atrás del toast (se decrementa cada frame)
- `ExpeditionReturn? pendingToast` — resultado en cola si toast label no está wired aún

## Lifecycle (S121-S124)

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe a `GameEvents.OnInventoryChanged`, `InventoryReloaded`, `OnExpeditionReturned` |
| `Start()` | Resuelve labels (date, dabloons, material, expeditionToast). Si hay `pendingToast` muestra |
| `Update()` | Decrementa `toastTimer`; si ≤0 oculta toast label |
| `OnDisable()` | Desuscribe todos |
| `HandleExpeditionReturned(ExpeditionReturn r)` | **(S121+S124)** Callback. Si toast label wired: `ShowExpeditionToast(r)`. Si no: guarda en `pendingToast` |
| `ShowExpeditionToast(ExpeditionReturn r)` | **(S121+S124)** Renderiza toast diferente según r.Lost |

## Toast Expedición (S121-S124)

**Dos formatos de toast:**

### Derrota (Lost=true)

```
Localizador: "ui.overlay.expedition.lost"
Params: Floors, HealthLost
Ejemplo: "Perdiste en el piso 3 · −45 vida"
Clase: "toast--lose" (rojo)
```

### Retorno exitoso (Lost=false)

```
Localizador: "ui.overlay.expedition.return"
Params: Floors, MaterialGained, HealthLost
Ejemplo: "Volviste: 5 pisos · +45 material · −12 vida"
Clase: "toast--win" (azul, si Winner==Player) o "toast--lose" (rojo, si Winner==Rival) o "toast--draw" (gris, si empate)
```

**Construcción de energy string:**
```csharp
string energy = r.HealthLost > 0 ? "−" + r.HealthLost : "+" + (-r.HealthLost);
```

**Renderización:**
1. Limpia clases previas (toast--win/lose/draw)
2. Si r.Lost: usa ExpeditionLostKey + "toast--lose"
3. Si !r.Lost: usa ExpeditionReturnKey + clase según Winner
4. Fija displayStyle=Flex, toastTimer=toastSeconds

**Duración:** 6 segundos (configurable); Update decrementa toastTimer y oculta cuando ≤0.

## Flujo S124

1. `ExpeditionBridge.ApplyResult()` calcula HealthLost, Floors, Lost → `GameEvents.ExpeditionReturned(return)`
2. `InfoOverlayUITK.HandleExpeditionReturned(r)` recibe evento
3. `ShowExpeditionToast(r)`:
   - Si Lost: texto de derrota + clase rojo
   - Si !Lost: texto con Floors + MaterialGained + HealthLost + color por Winner
4. `Update()`: decrementa toastTimer, oculta cuando ≤0

## Cambios por Sesión

- **S68:** InputHint.ActionKey renombrado
- **S93:** UiPanels.RootOf() helper
- **S95:** materialLabel para AdventureMaterial
- **S121:** Toast expedición (evento OnExpeditionReturned)
- **S124:** Diferencia Lost vs retorno; ExpeditionLostKey; HealthLost en lugar de EnergySpent; Floors ahora en ambos textos

## Invariantes

- Toast label puede ser null (edición o docs sin UXML); pendingToast guarda para mostrar después
- Textos localizados vía Loc
- Suscripción/desuscripción simétrica OnEnable/OnDisable
- HealthLost negativo = ganancia de vida (poco probable, pero soportado)

## Vinculado a

[[Index/05 - UI System]], [[Index/14 - Localization]], [[Index/24 - Puente Tienda-Arena]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[Loc]], [[GameManager]], [[GameEvents]], [[ExpeditionBridge]], [[UiPanels]], [[ArenaRunDirector]] (S124)
