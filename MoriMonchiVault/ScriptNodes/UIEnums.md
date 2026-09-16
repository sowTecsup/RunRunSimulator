---
tags: [enum, ui, state]
---

# UIEnums.cs

**Ruta:** `Core/Enums/UIEnums.cs`

**Responsabilidad:** Enumeraciones para sistema de UI y estados del jugador. Contiene: `UIPanelType` (9 paneles: None/CreatureGrid/MorimonchiDetail/Breeding/Combat/Storage/Store/Transaction/**Expedition**), `PlayerStateType` (4 estados: None/Exploring/Menu/Building).

**S93:** Consolidación de enums de UI en archivo dedicado.

**S95:** Agregado `Combat = 4` para panel de combate Dragon RPS.

**S120:** Agregado `Expedition = 8` para panel de selección de equipo.

## Enumeraciones

| Enum | Valores | Descripción |
|------|---------|-------------|
| `UIPanelType` | None (0), CreatureGrid (1), MorimonchiDetail (2), Breeding (3), Combat (4), Storage (5), Store (6), Transaction (7), **Expedition (8)** | Panel activo de UI |
| `PlayerStateType` | None (0), Exploring (1), Menu (2), Building (3) | Modo del jugador (qué puede hacer) |

## Uso

- `UIPanelType` — identifica qué panel está abierto; usado por `UIManager.OpenPanel()` para rutear entrada/lógica
- `PlayerStateType` — constrains inputs (Exploring permite navegación/interacción; Menu abre/cierra paneles; Building activa BuildModeController)

## Cambios por Sesión

- **S95:** Combat = 4 (panel de combate Dragon RPS)
- **S120:** Expedition = 8 (panel de bajada: elegir equipo antes de arena)

## S120-S122

- **S120:** `UIPanelType.Expedition = 8` nuevo. Usado por ExpeditionPanelUITK para mostrar lista de criaturas elegibles (filtro energía ≥30) y seleccionar hasta 3.
- **S121-S122:** Sin cambios a UIEnums.

## Vinculado a

- [[Index/05 - UI System]]
- [[Index/21 - Combate v3 - Dragon RPS]]
- [[Index/24 - Puente Tienda-Arena]] (S120)
- [[UIManager]] — gestiona estado y apertura de paneles

**Conexiones:** [[UIManager]], [[BuildModeController]], [[PlayerInputs]], [[ExpeditionPanelUITK]], [[CombatPanelUITK]]
