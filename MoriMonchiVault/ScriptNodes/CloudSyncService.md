---
tags: [script, cloud, sync]
---

# CloudSyncService.cs

**Ruta:** `Systems/Cloud/CloudSyncService.cs`

**Responsabilidad:** Núcleo MonoBehaviour que orquesta autenticación + sincronización. Compone CloudAuth + CloudSyncOps. **S124:** `StartupSyncDone` true si sync completó O Auth no activa (tolerancia sin-sesión).

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Task InitializeAsync()` | Resume sesión (anón o UGS) |
| `Task PushAsync()` | Fire-and-forget push cloud |
| `Task PullAsync()` | Fetch cloud, override, dispara eventos |
| `Task ResetProgressAsync()` | Dev: limpia cloud |
| `Task UpdatePlayerNameAsync(string)` | Actualiza nombre |

## Propiedades

| Propiedad | Descripción |
|-----------|-------------|
| `ServerOffset` | TimeSpan desfase servidor |
| `StartupSyncDone` | True si sync OK o Auth no activa (S124) |

## Ciclo Post-Sign-In (S119)

1. Cargar creatures
2. Cargar social graph
3. Cargar furniture
4. Cargar inventory
5. FetchServerTime
6. Pull cloud
7. Dispara eventos Reloaded

## Cambios S124

- `StartupSyncDone` retorna true si no hay sesión activa (tolerancia a dev/offline)

## Vinculado a

[[Index/07 - Persistence & Identity]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[CloudAuth]], [[CloudSyncOps]], [[GameEvents]], [[SaveSystem]]
