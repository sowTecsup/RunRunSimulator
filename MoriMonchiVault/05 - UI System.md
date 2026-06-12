---
tags: [memory-bank, ui, uitk, panels, input]
---

# 05 — UI System

## Responsabilidad Core (TL;DR)
Gestiona la interfaz de usuario construida en UI Toolkit, el enrutamiento exclusivo de inputs (gamepad/teclado) hacia los paneles, y el aislamiento entre los modos "Gameplay" y "Menú".

## Source of Truth & Centralización
- **Manager Principal:** `UIManager.cs` (GameObject en escena). Mantiene la pila (Stack LIFO) de paneles con foco.
- **Input Router:** `UIInputs.cs`. Dueño absoluto del Action Map "UI". Centraliza la navegación del mando/teclado y la emite para el `UIManager`.
- **Buses Clandestinos:** Los eventos puramente visuales o de navegación viven como `static event Action` dentro del `UIManager` (ej. `RequestPanelToggle`), separados de `GameEvents`.

## Flujo del Stack LIFO
1. **Trigger:** Un objeto del mundo (`PanelTrigger` con `IInteractable`) o un input global pide abrir un panel mediante `UIManager.RequestPanelToggle(UIPanelType)`.
2. **Push:** `UIManager` activa el panel (UITK) y lo pone en el tope de la pila. Se dispara `OnUIFocusChanged(true)`, lo que apaga el Action Map "Player" y enciende el "UI", deteniendo al avatar.
3. **Foco:** Solo el panel en el tope (que implementa `IUINavigable`) recibe los inputs de `UIInputs`.
4. **Pop (Cancel):** Al presionar ESC/B, se llama a `OnUICancel()` en el panel. Si el panel devuelve `false` (no requiere el botón Atrás internamente), el UIManager cierra el panel y lo quita de la pila. Si la pila queda a 0, se devuelve el control al jugador.

## Reglas de Oro (Invariantes de UITK)
- **Nunca usar SetActive:** Los GameObject que portan paneles UITK jamás se desactivan, de lo contrario se destruye el `rootVisualElement`. Se ocultan mediante `style.display = DisplayStyle.None`.
- **Mutua Exclusión:** Los Action Maps `Player` y `UI` son inversamente excluyentes. No pueden operar al mismo tiempo.
- **Paneles Auto-Actualizados:** Como los paneles están siempre en GameObjects encendidos, pueden escuchar eventos (como `OnRegistryChanged`) y reconstruir sus datos en background aunque no estén visibles.
