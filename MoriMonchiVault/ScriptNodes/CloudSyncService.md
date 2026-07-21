---
tags: [script, cloud]
---

# CloudSyncService.cs

**Ruta:** `Systems/Cloud/CloudSyncService.cs`

**Responsabilidad (S53 — Fase 6 composición):** Núcleo MonoBehaviour delgado que orquesta autenticación + sincronización. Compone: `CloudAuth` (identidad UGS) + `CloudSyncOps` (operaciones sync). Setup: `Start()` instancia ambas, `Awake()` carga refs de GameManager (registry/furnitureRegistry/inventory). Fachada pública intacta para consumidores externos (GameManager): `InitializeAsync()`, `PushAsync()`, `PullAsync()`, `ResetProgressAsync()`, `UpdatePlayerNameAsync()`, `ServerOffset`. Secuencia post-sign-in (`HandleSignedInAsync`): cargar locales + scope por PlayerID + FetchServerTime + Pull + NotifyPending. Ciclo: (1) `auth.InitializeAsync()` → resume session || anónimo || espera Unity Account; (2) callback dispara `HandleSignedInAsync()` → setup local + Func coordinator que llama `syncOps.PullAsync()`. Panel Odin Buttons: sign in (anón/Unity), sign out, update name, reset progress (dev), push, pull. Displays readonly: Player ID, Name, Auth Method, Last Pull, Last Known Cloud Push, Security Status, Server Offset. Botones y displays delegando a auth/syncOps.

**Vinculado a:** [[Index/04 - UGS & Cloud]]

**Conexiones:** [[CloudAuth]], [[CloudSyncOps]], [[GameManager]], [[GameEvents]], [[SaveSystem]]

**Organización (S53 composición — Fase 6):**
- `CloudSyncService.cs` — núcleo MonoBehaviour: lifecycle, coordination, fachada pública
- `CloudAuth.cs` — identidad UGS (dueño único)
- `CloudSyncOps.cs` — operaciones de sync (dueño único)
