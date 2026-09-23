---
tags: [cloud, synchronization, networking]
---

# CloudSyncOps

**Ruta:** `Systems/Cloud/CloudSyncOps.cs`

**Responsabilidad:** Orquestador de sincronización nube/local con reconciliación inteligente. `SyncOnStartupAsync()` reconcilia tres timestamps. `PushAsync/PullAsync()` manejan 5 claves (creature/furniture/inventory/social/**world_state**). S131: Constructor recibe `WorldStateSO`; sincroniza estado de mundo con cloud.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `PushAsync()` | `Task` | Sube 5 archivos a nube (+ **worldstate**) |
| `PullAsync()` | `Task` | Baja 5 archivos (+ **worldstate**) |
| `SyncOnStartupAsync()` | `Task` | Reconcilia nube vs local (5 claves) |
| `ResetProgressAsync()` | `Task` | Borra TODOS datos nube (debug) |
| `RefreshSecurityDisplay()` | `void` | Actualiza displays para debug |

## Claves Sincronizadas (S131)

| Clave | Contenido | Cambio S131 |
|-------|-----------|------------|
| `creatureregistry` | RegistryData (Alive+Departed) | — |
| `furnitureregistry` | Placed furniture dict | — |
| `playerinventory` | Cartera (Dabloons/Minerita) | — |
| `socialgraph` | Pairwise relationships | — |
| **`worldstate`** | WorldStateData (day/minute/tutorial) | **NUEVO** |

## Constructor S131

```csharp
public CloudSyncOps(CloudAuth auth, CreatureRegistrySO registry, 
                    FurnitureRegistrySO furniture, PlayerInventorySO inventory,
                    WorldStateSO worldState,  // S131
                    Action<string> setStatus)
{
    // Guarda referencias
}
```

**Propósito:** WorldState se sincroniza junto con otros datos en push/pull.

## SyncOnStartupAsync S131

- Reconcilia ahora 5 claves (antes 4)
- `SaveKind.World` para migrations
- WorldStateReloaded dispara al cargar

## Cambios S131

**Agregado:**
- `private WorldStateSO worldState` — referencia
- `worldstate` clave en push/pull/reset
- Manejo de `SaveKind.World` en migrations

**Propósito:** Sincronización de día/minuto del juego con cloud.

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[Index/09 - Active Context]]

## Conexiones

- [[CloudAuth]] — autenticación
- [[SaveSystem]] — serialización (con WorldState)
- [[CloudSyncService]] — orquestador superior
- [[GameManager]], [[GameEvents]]

## Notas (S131 HC-4)

- **5 claves:** creature, furniture, inventory, social, **worldstate** (nuevo).
- **Constructor:** CloudSyncService pasa worldState en new.
- **Backward compat:** Guardados v3 siguen migrando a v4 sin pérdida.
