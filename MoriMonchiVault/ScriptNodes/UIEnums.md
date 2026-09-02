---
tags: [enum, ui, state]
---

# UIEnums.cs

**Ruta:** `Core/Enums/UIEnums.cs`

**Responsabilidad:** Enumeraciones para sistema de UI y estados del jugador. Contiene: `UIPanelType` (8 paneles: None/CreatureGrid/MorimonchiDetail/Breeding/Combat/Storage/Store/Transaction), `PlayerStateType` (4 estados: None/Exploring/Menu/Building).

**S93:** Consolidación de enums de UI en archivo dedicado.

**S95:** Agregado `Combat = 4` para panel de combate Dragon RPS.

## Enumeraciones

| Enum | Valores | Descripción |
|------|---------|-------------|
| `UIPanelType` | None (0), CreatureGrid (1), MorimonchiDetail (2), Breeding (3), Combat (4), Storage (5), Store (6), Transaction (7) | Panel activo de UI |
| `PlayerStateType` | None (0), Exploring (1), Menu (2), Building (3) | Modo del jugador (qué puede hacer) |

## Uso

- `UIPanelType` — identifica qué panel está abierto; usado por `UIManager.OpenPanel()` para rutear entrada/lógica
- `PlayerStateType` — constrains inputs (Exploring permite navegación/interacción; Menu abre/cierra paneles; Building activa BuildModeController)

## Cambios S95

- **Combat = 4:** Nuevo panel de combate Dragon RPS (UIPanelType.Combat)

## Vinculado a

- [[Index/05 - UI System]]
- [[Index/21 - Combate v3 - Dragon RPS]]
- [[UIManager]] — gestiona estado y apertura de paneles

**Conexiones:** [[UIManager]], [[BuildModeController]], [[PlayerInputs]], [[CombatPanelUITK]]

