---
tags: [script, cloud, sync]
---

# CloudSyncService.cs

**Ruta:** `Systems/Cloud/CloudSyncService.cs`

**Responsabilidad:** Núcleo MonoBehaviour que orquesta autenticación + sincronización. Compone `CloudAuth` (identidad UGS) + `CloudSyncOps` (operaciones sync). Fachada pública: `InitializeAsync()`, `PushAsync()`, `PullAsync()`, `ResetProgressAsync()`, `UpdatePlayerNameAsync()`. **S75:** Sin operaciones de combate o notificaciones de resultados.

## Secuencia Post-Sign-In

1. Cargar creatures/furniture/inventory locales
2. Cargar social graph local (S65)
3. FetchServerTime
4. Pull cloud (override si existe)
5. Dispara GameEvents.RegistryReloaded, FurnitureReloaded, InventoryReloaded

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `InitializeAsync()` | Resume sesión (anón o Unity Account) |
| `PushAsync()` | Fire-and-forget push a cloud |
| `PullAsync()` | Fetch cloud, override locales, dispara eventos |
| `ResetProgressAsync()` | Dev: limpia cloud + resets locales |
| `UpdatePlayerNameAsync(string)` | Actualiza nombre de jugador |

## Cambios en S75

- **SIN:** Operaciones de combate
- **SIN:** NotifyPendingCombatResults
- **MANTIENE:** Push/Pull/Reset de creatures/furniture/inventory

## Vinculado a

- [[Index/07 - Persistence & Identity]]

**Conexiones:** [[CloudAuth]], [[CloudSyncOps]], [[GameEvents]], [[SaveSystem]]
