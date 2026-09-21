---
tags: [script, ui]
---

# InfoOverlayUITK

**Ruta:** `UI/InfoOverlayUITK.cs`

**Responsabilidad:** Overlay contextual siempre-visible (top-left hints de controles, top-right fecha/dabloons/**Minerita**, toast de retorno expedición). **S128:** actualiza labels de monedas para reflejar dos divisas (Dabloons + Minerita en lugar de "material de aventura").

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | UIToolkit doc tree (overlay siempre visible) |
| `hints` | `InputHint[]` | Array de controles mostrados top-left |
| `toastSeconds` | `float` | Duración del toast expedición (default 6s) |

## Campos Privados (S128+)

- `Label dateLabel` — hora y fecha top-right
- `Label dabloonsLabel` — cantidad de dabloons
- `Label mineritaLabel` — **S128** cantidad de Minerita (antes "material de aventura") |
- `Label expeditionToastLabel` — toast de retorno expedición
- `float toastTimer` — cuenta atrás del toast
- `ExpeditionReturn? pendingToast` — resultado en cola si toast label no está wired aún

## Lifecycle

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe a `GameEvents.OnInventoryChanged`, `InventoryReloaded`, `OnExpeditionReturned` |
| `Start()` | Resuelve labels (date, dabloons, minerita, expeditionToast) |
| `Update()` | Decrementa `toastTimer`; si ≤0 oculta toast |
| `OnDisable()` | Desuscribe todos |
| `HandleExpeditionReturned(ExpeditionReturn r)` | Callback; renderiza toast |
| `ShowExpeditionToast(ExpeditionReturn r)` | Toast diferente según Lost |

## Toast Expedición (S124)

### Derrota (Lost=true)
```
"Perdiste en el piso N · −M vida"
Clase: "toast--lose" (rojo)
```

### Retorno exitoso (Lost=false)
```
"Volviste: N pisos · +M Minerita · −K vida"
Clase: "toast--win" (azul si victoria), "toast--lose" (rojo si derrota)
```

## Integración S128

- `mineritaLabel` reemplaza `materialLabel` (dos monedas distintas)
- Toast muestra `MineritaGained` en lugar de genérico "material"
- Ambos labels actualizados vía `GameEvents.InventoryChanged`

## Vinculado a

[[Index/05 - UI System]]
[[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[Loc]], [[GameManager]], [[GameEvents]], [[ExpeditionBridge]], [[UiPanels]]

