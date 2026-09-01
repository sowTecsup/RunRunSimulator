---
tags: [enum, ui, state]
---

# UIEnums.cs

**Ruta:** `Core/Enums/UIEnums.cs`

**Responsabilidad:** Enumeraciones para sistema de UI y estados del jugador. Contiene: `UIPanelType` (7 paneles: None/CreatureGrid/MorimonchiDetail/Breeding/Storage/Store/Transaction), `PlayerStateType` (4 estados: None/Exploring/Menu/Building).

**S93:** Consolidación de enums de UI en archivo dedicado.

## Enumeraciones

| Enum | Valores | Descripción |
|------|---------|-------------|
| `UIPanelType` | None (0), CreatureGrid (1), MorimonchiDetail (2), Breeding (3), Storage (5), Store (6), Transaction (7) | Panel activo de UI |
| `PlayerStateType` | None (0), Exploring (1), Menu (2), Building (3) | Modo del jugador (qué puede hacer) |

## Uso

- `UIPanelType` — identifica qué panel está abierto; usado por `UIManager.OpenPanel()` para rutear entrada/lógica
- `PlayerStateType` — constrains inputs (Exploring permite navegación/interacción; Menu abre/cierra paneles; Building activa BuildModeController)

## Vinculado a

- [[Index/05 - UI System]]
- [[UIManager]] — gestiona estado y apertura de paneles

**Conexiones:** [[UIManager]], [[BuildModeController]], [[PlayerInputs]]

