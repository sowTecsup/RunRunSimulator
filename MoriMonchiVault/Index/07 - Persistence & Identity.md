---
tags: [index, cloud]
---

# 07 - Persistence & Identity

**Responsabilidad:** Persistencia local JSON (aislado por cuenta), identidad inmutable de criaturas, bus de eventos global.

**Scripts:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[GameManager]] | `Core/GameManager.cs` | Ciclo de vida + orquestador persistencia (save+push) |
| [[GameEvents]] | `Core/GameEvents.cs` | Bus eventos cross-system estatico |
| [[SaveSystem]] | `Core/SaveSystem.cs` | I/O JSON a disco (Newtonsoft) |
| [[CreatureRegistrySO]] | `Data/CreatureRegistrySO.cs` | Cache memoria Dictionary<string, CreatureDNA> |
| [[FurnitureRegistrySO]] | `Data/FurnitureRegistrySO.cs` | Cache memoria muebles colocados |
| [[PlayerInventorySO]] | `Data/PlayerInventorySO.cs` | Inventario persistente del jugador |

**Pipeline Persistencia:**
Mutacion GameEvents.OnRegistryChanged GameManager.SaveDatabase (disco) CloudSyncService.PushToCloud (nube)

**Excepcion NeedsState:** No dispara eventos (cada frame). Flush solo en quit/pause.

**Identidad Criatura:**
- Genetic String: ToStringID() (inmutable, ej: BS0-A3-E1-M2-FF00AA)
- UniqueID: incluye Timestamp ticks (ej: BS0-A3-E1-M2-FF00AA-{Ticks})
- IDs de partes jamas usan guion medio

**Reglas de Oro:**
- Cero acoplamiento: evento transporta payload, suscriptor no busca singleton
- Scoping por jugador: JSON con formato _<playerId>
- sync_meta.json para detectar manipulacion local de savefiles
