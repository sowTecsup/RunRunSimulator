---
tags: [memory-bank, ugs, cloud, auth, persistence]
---

# 04 - UGS & Cloud

**Responsabilidad:** Autenticacion (Unity Player Accounts), Cloud Save, Cloud Code serverless.

**Scripts:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[CloudSyncService]] | `Systems/Cloud/CloudSyncService.cs` | Auth, push/pull Cloud Save |
| [[CloudCodeTester]] | `Systems/Cloud/CloudCodeTester.cs` | Dev tool probar Cloud Code desde editor |

**Flujo Sync:** Auto-Pull login Auto-Push en cada mutacion (GameManager escucha eventos) sync_meta.json para detectar manipulacion local.

**Arquitectura Backend:** Scripts JS en CloudCode/. Scheduler emite evento cron (.sched) Trigger (.tr) ejecuta script JS. Deploy via CLI (ugs deploy) con Service Account.

**Reglas de Oro (Custom Data):**
- No arrays top-level en JSON (usar { entries: [...] })
- setCustomItem: firma de 3 argumentos (projectId, customId, body)
- getCustomItems falla con multiples keys: leer una por una
- Service Account = deploy CLI; Project Secrets = env-vars para scripts JS
