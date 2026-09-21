---
tags: [script, ui, presenter]
---

# DetailInfoTabPresenter.cs

**Ruta:** `UI/DetailInfoTabPresenter.cs`

**Responsabilidad:** Presenter UITK para tab Info. Muestra 5 filas de partes genéticas (tier, potencial, techo), 3 barras de necesidades (color por `NeedsDisplay`), badge de "Apta para bajar" si `CreatureAvailability.CanExplore()`, y progresión BreedCount. **S93:** Usa `CreatureDisplay.StateOf()`. **S95:** Potencial para Horn/Back/Wing si > 0, o "empty" si sin potencial. **S129:** Eliminada fila de stats (CreatureStats removido); agregadas barras de necesidades.

## Contenido del Tab Info

| Elemento | Descripción |
|----------|-------------|
| 5 partes | BodyShape, Horn, Back, Wing, Face con tier y potencial/techo |
| 3 barras necesidades | Health / Energy / Affect con relleno + color (NeedsDisplay) |
| Badge Apta | ✓ si `CreatureAvailability.CanExplore(dna, careGate)` |
| BreedCount | "Criada N veces" (demografía) |

## Métodos Clave

| Método | Descripción |
|--------|-------------|
| `OnDataSet(CreatureDNA dna)` | Actualiza tab al cambiar criatura seleccionada |
| `AddPartRow(name, tier, potential, potentialMax)` | Crea fila de parte con tier y potencial |
| `UpdateNeedsBar(needType, value)` | Actualiza barra con `NeedsDisplay.Fill01()` + color |
| `UpdateAptaBadge(creatureAvailable)` | Muestra/oculta badge si apta |

## Cambios S129

- **ELIMINADO:** Fila de stats (Constitution, Attack, Speed, Defense, Luck, Evasion)
- **AGREGADO:** 3 barras de necesidades (Health, Energy, Affect)
- **AGREGADO:** Badge de apta para bajar
- **MANTIENE:** Partes genéticas, tier, potencial, BreedCount

## Vinculado a

[[Index/05 - UI System]]
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[NeedsDisplay]], [[CreatureAvailability]], [[CreatureDNA]], [[CreatureDisplay]], [[MorimonchiDetailInfoUITK]], [[CareGateSO]]
