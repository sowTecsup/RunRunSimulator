---
tags: [script, cloud]
---

# CloudSyncService.cs

**Ruta:** `Systems/Cloud/CloudSyncService.cs`

**Responsabilidad:** Capa de sincronización con UGS Cloud Save. Auth, Push/Pull de registry+metadata+furniture+inventory, security metadata (anti-cheat: timestamps LocalPulledAt/LocalKnownCloudAt/CloudPushedAt). **`ResetProgressAsync()`** (en .Sync.cs): borra TODO el estado MM server-side + local + JSON. Secuencia: (1) llama `CloudEndpoint.CallAsync(CANCEL_ALL_BREEDING, {})` para cancelar huevos; (2) itera registry, para cada criatura con BusyState=QueuedForCombat, llama `CloudEndpoint.CallAsync(DEQUEUE_COMBAT, {creatureId})`; (3) borra 5 Cloud Save keys via `CloudSaveService.DeleteAsync()`: `creatureregistry`, `sync_meta`, `furnitureregistry`, `playerinventory`, `combat_results`. (4) vaciá caché local + recargar JSON + dispara `RegistryReloaded`/`FurnitureReloaded`/`InventoryReloaded` SIN push (limpieza pura). Pull/Push disparan `OnRegistryReloaded` solo en Pull.

**Vinculado a:** [[Index/04 - UGS & Cloud]]

**Conexiones:** [[GameManager]], [[GameEvents]], [[CloudCodeTester]]

**Organización (partial class):**
- `CloudSyncService.cs` — núcleo: constants/SyncMeta/fields/lifecycle/meta helpers
- `CloudSyncService.Auth.cs` — auth+init+cuenta
- `CloudSyncService.Sync.cs` — validate+reset+push+pull
