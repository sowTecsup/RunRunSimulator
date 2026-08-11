---
tags: [script, cloud]
---

# CloudSyncOps.cs

**Ruta:** `Systems/Cloud/CloudSyncOps.cs`

**Responsabilidad:** Operaciones de sincronización pura con Cloud Save. Push/Pull/Reset de registry/furniture/inventory. SyncMeta anti-cheat (timestamps locales). **S75:** Sin operaciones de combate (quitadas NotifyPendingCombatResultsAsync, combat queue keys).

## Métodos

- **PushAsync()** — Serializa estado local, empuja a Cloud Save, muta timestamps locales
- **PullAsync()** — Descarga desde Cloud, deserializa, dispara eventos Reloaded
- **ResetProgressAsync()** — Cancela progreso, limpia cloud, vacía local

## Cambios en S75

- **ELIMINADO:** `NotifyPendingCombatResultsAsync()` (demolición del combate async)
- **ELIMINADO:** keys de combat queue de cloud operations
- **MANTIENE:** Push/Pull/Reset de creatures/furniture/inventory

## Vinculado a

- [[Index/07 - Persistence & Identity]]

**Conexiones:** [[CloudSyncService]], [[GameEvents]], [[SaveSystem]]
