---
tags: [script, ui, expedition, uitk]
---

# ExpeditionPanelUITK.cs

**Ruta:** `UI/ExpeditionPanelUITK.cs`

**Responsabilidad:** Panel UITK para elegir elenco antes de bajar. Lista criaturas vivas filtradas por vida > 0, elegibles si no ocupadas. Máximo 3. Emite `ExpeditionBridge.RequestDeparture(ids)`. **S124:** Elegibilidad solo por vida (no energía). Barra de vida en tarjeta.

## Campos

| Campo | Descripción |
|-------|-------------|
| `UIDocument document` | Doc UITK |
| `UIPanelType panel` | Tipo Expedition |
| `int maxPick` | Max elegibles (3) |
| `List<VisualElement> cards` | Tarjetas |
| `List<CreatureDNA> dnas` | DNAs |
| `List<bool> eligible` | Elegibilidad |
| `List<bool> picked` | Selección |

## Métodos Públicos (IUINavigable)

| Método | Descripción |
|--------|-------------|
| `OnUINavigate(Vector2 dir)` | Navega tarjetas |
| `OnUISubmit()` | Toggle selección |
| `OnUICancel()` | Retorna false |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `Rebuild()` | Filtra vivas, ordena por elegibilidad |
| `BuildCard(dna, ok)` | Retrato + nombre + barra de vida (no barra energía) |
| `ToggleAt(int index)` | Selecciona si elegible |
| `Depart()` | Emite RequestDeparture(ids) |

## Cambios S124

- Elegibilidad: **vida > 0** (no energía ≥ 30)
- BuildCard: **barra de vida** en lugar de barra de energía
- Sin gate de energía: cualquier criatura viva es elegible

## Flujo

1. OnEnable: suscribe UIManager
2. Start: cableado, Rebuild
3. Rebuild: filtra vivas > 0
4. Seleccionar hasta 3
5. Depart: emite IDs

## Invariantes

- Max `maxPick` elegidas
- Solo vivas pueden seleccionarse
- Botón "Ir" habilitado si ≥1 seleccionada

## Vinculado a

[[Index/24 - Puente Tienda-Arena]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ExpeditionBridge]], [[GameManager]], [[CreatureRegistrySO]]
