---
tags: [script, ui, expedition, uitk]
---

# ExpeditionPanelUITK.cs

**Ruta:** `UI/ExpeditionPanelUITK.cs`

**Responsabilidad:** Panel UITK para elegir elenco antes de bajar. Lista criaturas vivas filtradas por vida > 0, elegibles si pueden explorar. Máximo 3. Emite `ExpeditionBridge.RequestDeparture(ids)`. **S124:** Elegibilidad por vida. **S129:** Gate de cuidados (`CareGateSO`); cada tarjeta muestra TRES barras (vida, energía, afecto) con `NeedsDisplay` y resalta la necesidad que bloquea (`WeakestNeed`).

## Campos

| Campo | Descripción |
|-------|-------------|
| `UIDocument document` | Doc UITK |
| `UIPanelType panel` | Tipo Expedition |
| `int maxPick` | Max elegibles (3) |
| `CareGateSO careGate` | **S129:** Gate de cuidados para elegibilidad |
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
|--------|--------|
| `Rebuild()` | Filtra vivas, ordena por elegibilidad via `CreatureAvailability.CanExplore(dna, careGate)` |
| `BuildCard(dna, ok)` | Retrato + nombre + **3 barras** (Health, Energy, Affect) resaltando necesidad débil si bloqueada |
| `BuildBar(need, value, weak)` | Barra individual con color y ancho según `NeedsDisplay` |
| `ToggleAt(int index)` | Selecciona si elegible |
| `Depart()` | Emite RequestDeparture(ids) |
| `CompareEntries(a, b)` | Ordena por elegibilidad primero, luego por Health desc |

## Cambios S129

- Elegibilidad: `CreatureAvailability.CanExplore(dna, careGate)` (vida + energía + afecto vs thresholds)
- BuildCard: **3 barras** (Health, Energy, Affect) con `NeedsDisplay.ColorClass` y `NeedsDisplay.Fill01`
- Resaltado: necesidad más débil que bloquea elegibilidad (clase `exp-card__bar-track--weak`)
- Removido helper privado `HealthColorClass` (usa `NeedsDisplay` centralizado)
- StateTextFor: muestra razón de bloqueo (necesidad débil)

## Flujo

1. OnEnable: suscribe UIManager
2. Start: cableado, Rebuild
3. Rebuild: filtra vivas, ordena por elegibilidad
4. Seleccionar hasta 3
5. Depart: emite IDs

## Invariantes S129

- Max `maxPick` elegidas
- Solo elegibles pueden seleccionarse
- Botón "Ir" habilitado si ≥1 seleccionada
- Elegibilidad determinada por `CreatureAvailability.CanExplore(dna, careGate)`
- Tres barras siempre visibles; débil resaltada

## Vinculado a

[[Index/24 - Puente Tienda-Arena]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ExpeditionBridge]], [[GameManager]], [[CreatureRegistrySO]], [[CreatureAvailability]], [[CareGateSO]], [[NeedsDisplay]]
