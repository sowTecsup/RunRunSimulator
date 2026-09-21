---
tags: [enum, ui, state]
---

# UIEnums

**Ruta:** `Core/Enums/UIEnums.cs`

**Responsabilidad:** Enumeraciones para sistema de UI y estados del jugador. Contiene: `UIPanelType` (9 valores con hueco en 4), `PlayerStateType` (4 estados de jugador).

**S128:** `Combat = 4` permanece **como hueco** (se borró todo combate RPS pero no se renumera). Otros paneles: None/CreatureGrid/MorimonchiDetail/Breeding/Storage/Store/Transaction/Expedition.

## Enumeraciones

### UIPanelType

| Valor | Descripción |
|-------|-------------|
| `None = 0` | No panel abierto |
| `CreatureGrid = 1` | Grilla de criaturas |
| `MorimonchiDetail = 2` | Ficha de criatura |
| `Breeding = 3` | Panel de cría |
| ~~`Combat = 4`~~ | **HUECO (S128):** demolición RPS, no renumera siguientes |
| `Storage = 5` | Almacén de props |
| `Store = 6` | Tienda de compras |
| `Transaction = 7` | Historial/transacciones |
| `Expedition = 8` | Selección de equipo |

### PlayerStateType

| Valor | Descripción |
|-------|-------------|
| `None = 0` | Estado inicial |
| `Exploring = 1` | Libre en tienda; permite navegación/interacción |
| `Menu = 2` | Abre/cierra paneles (no movimiento) |
| `Building = 3` | Modo construcción de muebles (BuildModeController) |

## Uso

- **UIPanelType:** identifica qué panel está abierto; usado por `UIManager.OpenPanel()` para rutear entrada/lógica
- **PlayerStateType:** constraints inputs (qué acciones permite cada estado)

## Cambios Históricos

**S95:** Combat = 4 agregado (Dragon RPS).
**S120:** Expedition = 8 agregado (panel bajada).
**S128:** Combat eliminado (RPS demolido); valor 4 queda como hueco, no se renumera (evita serialización breaks).

## Nota de Diseño

El hueco en 4 es intencional. Renumerar causaría breakage de:
- Saves existentes con valores serializados
- Prefabs/escenas con referencias a índices

## Vinculado a

[[Index/05 - UI System]]

**Conexiones:** [[UIManager]], [[BuildModeController]], [[PlayerInputs]], [[ExpeditionPanelUITK]]

