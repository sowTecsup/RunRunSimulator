---
tags: [memory-bank, ugs, cloud, auth, persistence]
---

# 04 — UGS & Cloud

> Relacionados: [[03 - Combat]] (Cloud Code scripts y Scheduler), [[07 - Persistence & Identity]] (save scoped por playerId), [[02 - Genetics & Breeding]] (breeding async usa Custom Data).

## CloudSyncService

- Adjuntar al mismo GameObject que `GameManager`. Asignar `CreatureRegistrySO`.
- Requiere en Unity Dashboard: **Authentication** (Anonymous + **Unity Player Accounts**) + **Cloud Save**.

### Autenticación

- Unity Player Account via `PlayerAccountService.StartSignInAsync()` (browser).
- Primer login abre browser; siguientes launches reanudan sesión silenciosamente via `SessionTokenExists`.
- **Sesión persistente**: en `Start`, si `AuthenticationService.Instance.SessionTokenExists` → `SignInAnonymouslyAsync()` reutiliza el token cacheado sin browser.
- **Player name**: visible en inspector `[Status]`. Botón `Update Name` en bloque `[Account]`.
- **Sign Out**: cierra la sesión actual; el session token se conserva (el próximo launch auto-reanuda).

### Auto-pull en login

`OnSignedInComplete` ejecuta:
1. `SaveSystem.SetUserScope(playerID)` — el save local pasa de `creature_database.json` a `creature_database_<playerId>.json`. Si existe el unscoped pero no el scoped, hay **migración automática** la primera vez.
2. `LoadInto` — cache local.
3. `await PullAsync()` — override desde cloud si hay data.

### Auto-push en Mint/Breed (legado del flujo manual)

`GameManager.TryPushToCloud()` se invoca después de `SaveDatabase` en `MintRandomCreature` y `BreedCreatures` — fire-and-forget. Requiere asignar el `CloudSyncService` en el inspector del `GameManager`. **Hoy esto vive en el listener de `OnRegistryChanged`** (ver [[07 - Persistence & Identity]]).

### Botones manuales (inspector, `EnableIf(_isSignedIn)`)

- **Push to Cloud** / **Pull from Cloud**.
- **Reset All Progress (DEV)**: borra keys de Cloud Save + vacía JSON local + borra sync_meta.

### sync_meta.json

Local registra timestamps de seguridad para detección de rollback/edición manual.

### Dev mode

CHEAT ALERT solo imprime en consola — activar bloqueo en post-Etapa 2.3 con Cloud Code firmando tokens.

## CloudCodeTester (DEV)

- `TestRandom` — invoca `test-random.js` (returns 1-4).
- `TestCustomData` — invoca `test-customdata.js` (read/write/read isolated en Custom Data).
- **Force Matchmaking Tick (DEV)** — llama directo a `process-matchmaking` (bypasea scheduler+trigger).

## UGS CLI — setup completo

Ver [[03 - Combat]] sección "Scheduler — arquitectura de 3 piezas" para el setup paso a paso.

**Highlights:**
- Binario standalone `ugs.exe` en el PATH.
- Service Account con roles `Unity Environments Admin` + `Cloud Code Editor/Viewer/Publisher` a nivel project, y `Owner` a nivel organization.
- `ugs login` + `ugs config set project-id <id>` + `ugs config set environment-name production`.

### REST API para schedules (lo que el CLI no cubre)

```
GET/DELETE https://services.api.unity.com/scheduler/v1/projects/<PROJECT_ID>/environments/<ENV_ID>/configs[/<CONFIG_ID>]
```

Basic Auth con `base64(<KEY_ID>:<SECRET_KEY>)`.

- Project ID: `14ef2aa0-ac88-457a-be73-9164939d87b0`
- Environment `production`: `6f9c7d83-1396-4de7-ba1c-ba01cec186df`

## Service Account ≠ Project Secrets

- **Service Account Keys** (Organization → Administration → Service Accounts → Keys): para autenticar la CLI/herramientas externas.
- **Project Secrets** (Proyecto → Cloud Code → Secrets): variables de entorno para que los scripts JS accedan a APIs externas en runtime.

NO confundirlas.

## Quirks de Cloud Code (resumen)

- `setCustomItem` firma correcta: **3 args** `(projectId, customId, body)` con body `{key, value}`. La firma de 4 args silently corrompe el body.
- `value` **NO acepta arrays top-level** — siempre envolver en `{ entries: [...] }`.
- Método correcto: `getCustomItems` (plural con array), NO `getCustomItem`.
- Auth via `accessToken: context.serviceToken` en el constructor del `DataApi`.
- `deleteCustomItem` firma no verificada → preferir `splice` + reescribir `{ entries }`.

## Archivos clave

```
Assets/RunRunSimulator/Scripts/Systems/Cloud/
├── CloudSyncService.cs               # MonoBehaviour: Unity Player Account auth + auto-pull on login + push/pull/reset + SyncMeta
└── CloudCodeTester.cs                # MonoBehaviour DEV: TestRandom / TestCustomData / ForceMatchmakingTick

CloudCode/
├── test-random.js                    # Diagnostic
├── test-customdata.js                # Diagnostic
└── (scripts de combat/breeding: ver [[03 - Combat]] y [[02 - Genetics & Breeding]])
```

## Etapa del roadmap

- **2.3 Integración Unity Services (async battles)** ✅ — Auth + Cloud Save (push/pull/auto-sync) + Cloud Code (enqueue/dequeue/process-matchmaking) + Scheduler+Trigger (cron 1h funcionando) + modo Instant + Busy persistente.
