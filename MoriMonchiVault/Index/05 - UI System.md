---
tags: [index, ui]
---

# 05 - UI System

**Responsabilidad:** Interfaz UITK, enrutamiento input a paneles, aislamiento Gameplay/Menu. Stack LIFO de paneles.

**Core:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[UIManager]] | `UI/UIManager.cs` | Hub paneles + stack LIFO + router input + bus eventos UI static |
| [[UIInputs]] | `UI/UIInputs.cs` | Dueno action map UI (Navigate/Submit/Cancel stepped) |
| [[PanelTrigger]] | `Interactables/PanelTrigger.cs` | Bridge mundo UI (IInteractable abre panel) |

**Panel Controllers:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[BreedingPanelUITK]] | `UI/BreedingPanelUITK.cs` | Panel breeding |
| [[BuildBrowserUITK]] | `UI/BuildBrowserUITK.cs` | Build browser muebles |
| [[CombatPanelUITK]] | `UI/CombatPanelUITK.cs` | Panel combate |
| [[CreatureGridUI]] | `UI/CreatureGridUI.cs` | Grid criaturas uGUI |
| [[CreatureGridUITK]] | `UI/CreatureGridUITK.cs` | Grid criaturas UITK |
| [[CreatureGridView]] | `UI/CreatureGridView.cs` | Vista grid base |
| [[CreatureVisualUI]] | `UI/CreatureVisualUI.cs` | Render 3D en UI |
| [[HotbarHUDUITK]] | `UI/HotbarHUDUITK.cs` | HUD hotbar (overlay) |
| [[InfoOverlayUITK]] | `UI/InfoOverlayUITK.cs` | Overlay contextual |
| [[MorimonchiDetailInfoUITK]] | `UI/MorimonchiDetailInfoUITK.cs` | Detalle criatura |
| [[StoragePanelUITK]] | `UI/StoragePanelUITK.cs` | Panel almacenamiento |
| [[StorePanelUITK]] | `UI/StorePanelUITK.cs` | Panel tienda |

**Reglas de Oro:**
- Jamas usar SetActive en UITK (destruye rootVisualElement). Usar style.display = None.
- Action Maps Player y UI son mutuamente excluyentes. Solo uno activo a la vez.
- Paneles siempre en GameObjects activos (pueden escuchar eventos aunque ocultos).
