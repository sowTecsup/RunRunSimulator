---
tags: [combat, visualization, events, bus, 3v3]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatVisualEvents

Bus estático de eventos para la visualización de combates (replay). Centraliza toda comunicación entre `CombatVisualizerService` (orquestador) y subscribers (UI, animadores, popups). **S61b:** Enum `CombatTurnPhase` nuevo + evento `OnPhase(phase, actorSide)` para sincronizar cámaras Cinemachine por etapa del turno. **S61:** Evento nuevo `OnLogAppend(CombatVisualLogLine)` para append incremental del log en tiempo real (una línea por beat de proc). **S58:** `CombatVisualLogLine` gana campos `HasUnit`, `UnitSide`, `UnitIndex` para filtrado en UI (mostrar solo reacciones/muertes con unit marker). **S59:** evento nuevo `OnUnitHover(CombatVisualSide, int, bool)` para hover externo (UI card slot).

[Ver nodo completo para detalles técnicos completos de eventos, structs y cambios por sesión]
