---
tags: [script, cloud, sync]
---

# CloudSyncService.cs

**Ruta:** `Systems/Cloud/CloudSyncService.cs`

**Responsabilidad (S53 — Fase 6 composición):** Núcleo MonoBehaviour delgado que orquesta autenticación + sincronización. Compone: `CloudAuth` (identidad UGS) + `CloudSyncOps` (operaciones sync). Setup: `Start()` instancia ambas, refs de GameManager (registry/furnitureRegistry/inventory). Fachada pública intacta para consumidores externos (GameManager): `InitializeAsync()`, `PushAsync()`, `PullAsync()`, `ResetProgressAsync()`, `UpdatePlayerNameAsync()`, `ServerOffset`. **S65:** `HandleSignedInAsync()` ahora llama `SaveSystem.LoadSocialGraph()` post-load de locales para cargar historial de afinidad con poda de huérfanos. Ciclo post-sign-in: (1) cargar locales (creatures/furniture/inventory); (2) **S65:** cargar social graph local; (3) scope por PlayerID; (4) FetchServerTime; (5) Pull cloud (override si existe). Panel Odin Buttons: sign in (anón/Unity), sign out, update name, reset progress (dev), push, pull. Displays readonly: Player ID, Name, Auth Method, Last Pull, Last Known Cloud Push, Security Status, Server Offset.

## Secuencia Post-Sign-In (HandleSignedInAsync)

```
1. SaveSystem.SetUserScope(auth.PlayerID)
2. SaveSystem.LoadInto(registry)  ← local creatures
3. SaveSystem.LoadSocialGraph(registry)  ← S65 NUEVO: local social history
4. GameEvents.RegistryReloaded(registry)  ← UI rebuild
5. SaveSystem.LoadFurniture(furnitureRegistry)  ← local furniture
6. GameEvents.FurnitureReloaded(furnitureRegistry)
7. SaveSystem.LoadInventory(inventory)  ← local inventory
8. GameEvents.InventoryReloaded(inventory)
9. _ = syncOps.PullAsync()  ← fetch + override si cloud exists
```

**S65:** LoadSocialGraph entre LoadInto (creatures) y RegistryReloaded para asegurar que la poda de huérfanos venga contra registry actualizado.

## Métodos Públicos (Fachada)

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `InitializeAsync()` | `Task` | Delega a `auth.InitializeAsync()` — resume sesión, anón o espera Unity Account |
| `PushAsync()` | `Task` | Delega a `syncOps.PushAsync()` — fire-and-forget push de registries locales a cloud |
| `PullAsync()` | `Task` | Delega a `syncOps.PullAsync()` — fetch de cloud, override locales si existe, dispara GameEvents.RegistryReloaded |
| `ResetProgressAsync()` | `Task` | Delega a `syncOps.ResetProgressAsync()` — dev button, limpia cloud + resets locales |
| `UpdatePlayerNameAsync(string newName)` | `Task` | Delega a `auth.UpdatePlayerNameAsync(newName)` |

## Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `ServerOffset` | `TimeSpan` { get; } | Offset de servidor (UTC - local), usado por GameManager.ServerNow para time-aware operations |

## Panels Odin Inspector

**Status (readonly displays):**
- Player ID, Player Name, Signed In, Auth Method
- Last Pull (timestamp + edad)
- Last Known Cloud Push (timestamp + edad)
- Security Status (warning si operaciones out-of-sync)
- Server Time Offset (±HH:MM:SS)

**Account (buttons + input, si signed in):**
- Sign In (anónimo / Unity Player Account)
- Sign Out
- Update Player Name (textfield + button)

**Operaciones (dev buttons):**
- Push Now
- Pull Now
- Reset Progress (destructivo)

## Notas

- Delgada fachada que NO expone CloudAuth ni CloudSyncOps; si necesitas acceder a detalles, va a través de displays readonly del inspector.
- Multi-instance support: cada instancia de Unity puede tener PlayerID distinto (diferente scope de saves).
- S65: SocialGraph cargado ANTES de Pull para asegurar que poda de huérfanos sea consistente.
- S53 descomposición: composición CloudAuth + CloudSyncOps mantiene separation of concerns (identidad vs sync).

## Vinculado a

- [[Index/04 - UGS & Cloud]]
- [[Index/07 - Persistence & Identity]]
- [[MoriMonchiVault/Index/14 - Social V2]] (S65 historial social)

## Conexiones

**Internos (composición S53):**
- `CloudAuth` — maneja autenticación UGS + ServerOffset
- `CloudSyncOps` — maneja operaciones de sync + blobs de cloud

**Externos:**
- `GameManager` — orquesta FlushToCloud que llama PushAsync
- `SaveSystem` — carga/guarda locales (creatures, furniture, inventory, **S65:** social graph)
- `GameEvents` — dispara RegistryReloaded, FurnitureReloaded, InventoryReloaded en pull
