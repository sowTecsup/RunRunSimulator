---
tags: [index, world]
---

# 06 - Player & World

**Responsabilidad:** Control jugador FP, simulacion criaturas vivas (NavMesh + Necesidades + Personalidad), interacciones fisicas.

**Player:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[PlayerInputs]] | `Player/PlayerInputs.cs` | Dueno action map Player (eventos estaticos) |
| [[PlayerController]] | `Player/PlayerController.cs` | Movimiento first-person |
| [[PlayerAnimator]] | `Player/PlayerAnimator.cs` | Capa de animacion |

**Interactables:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[ThrowableObject]] | `Interactables/ThrowableObject.cs` | Fisica grab/throw (velocity follow, no teleport) |

**Criaturas en Escena:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[MoriMochiSpawner]] | `World/MoriMochiSpawner.cs` | Instancia criaturas del registry |
| [[MoriMochiAgent]] | `World/MoriMochiAgent.cs` | Cerebro NavMesh + necesidades + personalidad |
| [[MoriMonchiController]] | `World/MoriMonchiController.cs` | Facade Agent + Visualizer |
| [[MoriMonchiVisualizer]] | `World/MoriMonchiVisualizer.cs` | Ensamblaje 3D sockets |
| [[BodyPartJoint]] | `World/BodyPartJoint.cs` | Punto conexion + mirror |
| [[NameTag]] | `World/NameTag.cs` | Label world-space UITK |

**NeedStation System:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[NeedStation]] | `World/NeedStation.cs` | Estacion abstracta (slot capacity, fill rate, auto-registro) |
| [[Feeder]] | `World/Feeder.cs` | Restaura Health |
| [[PlayZone]] | `World/PlayZone.cs` | Restaura Affect |
| [[RestZone]] | `World/RestZone.cs` | Restaura Energy |
| [[NeedStationRegistry]] | `World/NeedStationRegistry.cs` | Indice runtime (GetClosest) |

**Contenedores:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[MoriMochiContainer]] | `World/MoriMochiContainer.cs` | Corral base (trigger volume, capacity, areaMask) |
| [[StoreContainer]] | `World/StoreContainer.cs` | Corral que restaura 3 needs a rate/s |

**World Props:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[HotbarController]] | `World/HotbarController.cs` | Hotbar 6-slots play-mode |
| [[WorldPropInstance]] | `World/WorldPropInstance.cs` | Tag identidad props (IInteractable) |

**Reglas de Oro:**
- Game feel via UnityEvents a MMF_Player (sin referencias directas a Feel en C#)
- Spawner es reactivo (escucha OnRegistryChanged, no autoritativo)
- WorldArea enum debe coincidir con strings de NavMesh areas
