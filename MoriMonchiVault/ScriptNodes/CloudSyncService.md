---
tags: [script, cloud, sync]
---

# CloudSyncService.cs

**Ruta:** `Systems/Cloud/CloudSyncService.cs`

**Responsabilidad:** Núcleo MonoBehaviour que orquesta autenticación + sincronización. Compone `CloudAuth` (identidad UGS) + `CloudSyncOps` (operaciones sync). Fachada pública: `InitializeAsync()`, `PushAsync()`, `PullAsync()`, `ResetProgressAsync()`, `UpdatePlayerNameAsync()`. **S75:** Sin operaciones de combate o notificaciones de resultados. **S119:** HandleSignedInAsync() ahora carga social graph post-autenticación.

## Secuencia Post-Sign-In (S119 ACTUALIZADO)

1. Cargar creatures locales
2. **S119 NUEVO:** Cargar social graph local (SaveSystem.LoadSocialGraph)
3. Cargar furniture locales
4. Cargar inventory local
5. FetchServerTime
6. Pull cloud (override si existe)
7. Dispara GameEvents.RegistryReloaded, FurnitureReloaded, InventoryReloaded

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Task InitializeAsync()` | Resume sesión (anón o Unity Account) |
| `Task PushAsync()` | Fire-and-forget push a cloud |
| `Task PullAsync()` | Fetch cloud, override locales, dispara eventos |
| `Task ResetProgressAsync()` | Dev: limpia cloud + resets locales |
| `Task UpdatePlayerNameAsync(string)` | Actualiza nombre de jugador |

## Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `ServerOffset` | TimeSpan | Desfase servidor (para GameManager.Now) |
| `StartupSyncDone` | bool | true tras completar PullAsync() en init |

## Cambios en S75

- **SIN:** Operaciones de combate
- **SIN:** NotifyPendingCombatResults
- **MANTIENE:** Push/Pull/Reset de creatures/furniture/inventory

## Cambios en S119

- **HandleSignedInAsync()** ahora llama `SaveSystem.LoadSocialGraph(registry)` tras LoadInto()
- Social graph cargado post-autenticación, antes de sincronización cloud
- Permite sincronizar relaciones sociales al restaurar sesión

## Vinculado a

[[Index/07 - Persistence & Identity]], [[Index/24 - Puente Tienda-Arena]]

**Conexiones:** [[CloudAuth]], [[CloudSyncOps]], [[GameEvents]], [[SaveSystem]], [[ExpeditionBridge]]
