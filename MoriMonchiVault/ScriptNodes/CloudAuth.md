---
tags: [script, cloud]
---

# CloudAuth.cs

**Ruta:** `Systems/Cloud/CloudAuth.cs`

**Responsabilidad:** Gestión singleton de identidad UGS (Authentication). Dueño único de: `IsSignedIn`, `PlayerID`, `PlayerName`, `AuthMethod` (Anonymous/Unity Account/Session resumed), `ServerOffset` (clock sync para anti-cheat, desde `get-server-time` Cloud Code). Ciclo: `InitializeAsync()` → setup events + resume session si existe token → callback `onSignedInComplete(method)` dispara secuencia de coordinador (`HandleSignedInAsync` en CloudSyncService). Sign in: anónimo directo O Unity Account via browser (event-driven por `PlayerAccountService`). Sign out limpia estado. `UpdatePlayerNameAsync()` persiste nuevo nombre en UGS. Status callback Action<string> transporta mensajes UI de cada paso (signing in / auth failed / session expired).

**Vinculado a:** [[Index/04 - UGS & Cloud]]

**Conexiones:** [[CloudSyncService]], [[CloudSyncOps]]
