---
tags: [memory-bank, player, world, navmesh, personality]
---

# 06 — Player & World

## Responsabilidad Core (TL;DR)
Gestiona el control del jugador en primera persona, la simulación de criaturas vivas en la escena mediante NavMesh, las interacciones físicas (agarrar/lanzar), y el comportamiento autónomo impulsado por Necesidades y Personalidad.

## Source of Truth & Centralización
- **Control Jugador:** `PlayerController.cs` (Físicas/Lógica) y `PlayerInputs.cs` (Enrutador de Input).
- **Simulación Mundo:** `MoriMochiSpawner.cs` (Instanciador/Object Pooling reactivo al Registry).
- **Cerebro Criatura:** `MoriMochiAgent.cs` (FSM de estados combinando NavMesh y Rigidbody).
- **Configuración Comportamiento:** `PersonalityProfileSO.cs`. Define radios de visión, preferencia de áreas y velocidades sin necesidad de hardcodear switches.

## Flujo del Jugador e Interacciones
1. **Estados Mutuamente Excluyentes:** `Exploring` (Input de jugador activo, retícula fija) vs `Menu` (Cámara bloqueada, retícula libre controlada por UI).
2. **Comandos (Tecla E):** 
   - *Tap E:* Dispara interfaz `IInteractable` (abrir paneles).
   - *Hold E:* Dispara interfaz `IThrowable` (agarrar criaturas o cajas).
   - *Click:* Lanza objeto sostenido convergiendo al centro del raycast de la vista.
3. **Petting:** Si una criatura reacciona amistosamente y el jugador la mira fijamente (`IsPlayerFacingMe()`), el tap E acaricia a la criatura, regenerando su stat `Affect` (con cooldowns).

## Flujo de IA y Simulacion (`MoriMochiAgent`)
- **Dualidad NavMesh ↔ Física:** La criatura vive 99% del tiempo usando NavMeshAgent (`isTrigger = true`). Al ser agarrada, empujada o lanzada, ejecuta `DetachToPhysics()`, pasando a ser controlada por físicas puras (Ragdoll), sin usar `PhysicMaterials` (los rebotes se calculan por código). Al frenar, ejecuta `RejoinNavMesh()`.
- **Sistema de Needs:** El agente pierde pasivamente `Health`, `Energy` y `Affect`. Su cerebro entra a `SeekingNeed`, buscando estaciones disponibles a través del `NeedStationRegistry`. Si tiene una Need en nivel crítico, ignora interacciones sociales con el jugador.
- **Corrales de Confinamiento:** Para atrapar una criatura en un mueble, debe ser lanzada dentro. El mueble (`MoriMochiContainer`) la detecta y le restringe su `areaMask` de NavMesh. No sale hasta que el jugador la agarra de nuevo.

## Reglas de Oro (Invariantes)
- **Desacoplamiento de Juice:** Todo el "game feel" y feedback visual (partículas, squash and stretch al rebotar) se dispara vía `UnityEvents` puros en el inspector conectados a `MMF_Player` (Asset externo Feel). El script en C# jamás tiene referencias directas a scripts de Feel.
- **El Spawner es reactivo, no autoritativo:** El `MoriMochiSpawner` no "crea" vida por su cuenta. Sólo lee `OnRegistryChanged` y mantiene la escena sincronizada con el estado de la Data.
- **Nombres de Áreas NavMesh:** El enum `WorldArea` debe coincidir exactamente con los strings generados en la ventana de NavMesh (Ej. `ShopBackroom`).
