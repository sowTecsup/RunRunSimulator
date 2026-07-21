---
tags: [script, cloud]
---

# CloudSyncOps.cs

**Ruta:** `Systems/Cloud/CloudSyncOps.cs`

**Responsabilidad:** Operaciones de sincronización pura con Cloud Save. Dueño único de: Push/Pull/Reset de registry/furniture/inventory/combat_results, SyncMeta anti-cheat (LocalPulledAt/LocalKnownCloudAt/CloudPushedAt — timestamps Ticks UTC en JSON local por PlayerID), `ValidateBeforePush()` (compara local token vs cloud token; CHEAT ALERT en S53 permite push, Etapa 2.3 lo bloquea), `NotifyPendingCombatResultsAsync()` (lee cola de resultados pendientes post-pull para avisar al jugador). Seguridad: comprueba auth before every op. Push: serializa estado + muta LocalKnownCloudAt. Pull: deserializa + dispara OnRegistryReloaded/FurnitureReloaded/InventoryReloaded. ResetProgressAsync: cancela hijos vía CloudEndpoint + desencola combates + borra 5 keys cloud + vacía local (sin re-push). Reads identity de CloudAuth (readonly).

**Vinculado a:** [[Index/04 - UGS & Cloud]]

**Conexiones:** [[CloudAuth]], [[CloudSyncService]], [[GameEvents]], [[SaveSystem]], [[CloudEndpoint]]
