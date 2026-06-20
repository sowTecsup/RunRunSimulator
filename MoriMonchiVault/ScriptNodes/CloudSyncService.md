---
tags: [memory-bank, script, cloud]
---

# CloudSyncService.cs

**Ruta:** `Systems/Cloud/CloudSyncService.cs`

**Responsabilidad:** Capa de sincronización con UGS Cloud Save. Push/Pull, auth, dispara `OnRegistryReloaded` tras pull externo.

**Vinculado a:** [[Index/04 - UGS & Cloud]]

**Conexiones:** [[GameManager]], [[GameEvents]], [[CloudCodeTester]]

**Organización (partial class):**
- `CloudSyncService.cs` — núcleo: constants/SyncMeta/fields/lifecycle/meta helpers
- `CloudSyncService.Auth.cs` — auth+init+cuenta
- `CloudSyncService.Sync.cs` — validate+reset+push+pull
