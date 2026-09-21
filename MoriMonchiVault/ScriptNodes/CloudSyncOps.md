---
tags: [cloud, synchronization, networking]
---

# CloudSyncOps

**Ruta:** `Systems/Cloud/CloudSyncOps.cs`

**Responsabilidad:** Orquestador de sincronización nube/local con reconciliación inteligente. `SyncOnStartupAsync()` reemplaza al pull ciego: compara tres timestamps (`CloudPushedAt` nube, `LocalKnownCloudAt` meta local, `LatestLocalSavedAt()` disco) para decidir subir, bajar o aplicar conflicto con respaldo. `PushAsync()` y `PullAsync()` manejan 4 claves (`creatureregistry`, `furnitureregistry`, `playerinventory`, `socialgraph`). **S128:** Nuevo método startup, timestamps, conflictos. **S129:** Registry serializa `RegistryData` (Alive + Departed) en versión 3.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `PushAsync()` | `Task` | Sube los 4 archivos a nube; si otro push en curso → repite al terminar |
| `PullAsync()` | `Task` | Baja los 4 archivos (no recomendado post-S128) |
| `SyncOnStartupAsync()` | `Task` | **S128** Reconcilia nube vs local sin perder datos |
| `ResetProgressAsync()` | `Task` | Borra TODOS datos nube (debug) |
| `RefreshSecurityDisplay()` | `void` | Actualiza displays de timestamp para debug |

## SyncOnStartupAsync (S128)

**Decisión por tres números:**

```
CloudPushedAt = CloudSaveService meta.CloudPushedAt
LocalKnownCloudAt = sync_meta.json local
LocalLatestSaved = SaveSystem.LatestLocalSavedAt()

Si CloudPushedAt == 0 (nube vacía):
  → Sube local a nube (primera vez)
  → Actualiza meta

Si CloudPushedAt == LocalKnownCloudAt (al día):
  → Si LocalLatestSaved > LocalKnownCloudAt (hay cambios locales no subidos):
    → Sube local
  → Si LocalLatestSaved <= LocalKnownCloudAt (sin cambios):
    → Nada

Si CloudPushedAt > LocalKnownCloudAt (otro dispositivo subió):
  → Si LocalLatestSaved > LocalPulledAt (cambios locales PENDIENTES):
    → CONFLICTO: BackupLocal("conflict"), aplica cloud (gana el más nuevo), actualiza meta
  → Si LocalLatestSaved <= LocalPulledAt (sin cambios pendientes):
    → Baja todo de nube
```

**Timestamps:** todo en `DateTime.UtcNow.Ticks`; metadata en `sync_meta_{playerId}.json` (local).

## Manejo de Conflictos (S128)

1. Detecta: `CloudPushedAt > LocalKnownCloudAt && LocalLatestSaved > LocalPulledAt`
2. Respalda: `SaveSystem.BackupLocal("conflict")` → 4 archivos `.conflict.bak.json`
3. Aplica: descarga de nube a archivos locales
4. Actualiza: `sync_meta.json` con nuevos timestamps

**Invariante:** si ambos lados cambiaron, **gana el más nuevo**. Los respaldos `.conflict.bak.json` preservan lo local descartado.

## Claves Nube (4)

| Clave | Tipo | Contenido |
|-------|------|----------|
| `creatureregistry` | Player Data | Criaturas `{ Version: 3, SavedAtTicks, Data: RegistryData { Alive, Departed } }` |
| `furnitureregistry` | Player Data | Muebles colocados |
| `playerinventory` | Player Data | Inventario dabloons + Minerita |
| `socialgraph` | Player Data | Grafo de afinidad social |

Todas encapsuladas en [[SaveEnvelope]] (versión + timestamp). **S129:** `creatureregistry` cambió a V3 con estructura `{ Alive, Departed }`.

## Push (S128)

**En curso:** si llega otro `PushAsync()` mientras uno corre → `pushAgain = true` (antes se descartaba). Al terminar, repite una vez si `pushAgain` está seteado.

Flujo:
1. Valida firma (compare `CloudPushedAt` nube vs `LocalKnownCloudAt` local)
2. Valida saldo (`LocalPulledAt > 0` = una sincronización pasó)
3. Sube 4 claves con sobre (V3 para registry)
4. Actualiza `sync_meta.json` con `CloudPushedAt = UtcNow.Ticks`
5. Si `pushAgain` → repite

## Pull (no recomendado en S128+)

Baja todo de nube sin reconciliación. **Usar `SyncOnStartupAsync()` en su lugar para startup.**

## Metadata Local (sync_meta.json)

```json
{
  "LocalPulledAt": 1726956000000,
  "LocalKnownCloudAt": 1726956000000,
  "CloudPushedAt": 1726956000000
}
```

Se lee/escribe en `Application.persistentDataPath/sync_meta_{playerId}.json`.

## Seguridad

- `ValidateBeforePush()` detecta cheats (si `LocalKnownCloudAt` no coincide con `CloudPushedAt`)
- `SecurityStatus` display: "OK" / "CHEAT ALERT (dev: push allowed)" / "No pull registered — fresh account"
- Debug log en conflictos

## Cambios S129

- **CAMBIO:** Registry push/pull ahora serializa `RegistryData { Alive, Departed }` (V3)
- **NO CAMBIO:** Lógica de timestamps y conflictos

## Vinculado a

[[Index/07 - Persistence & Identity]] (S128 sección)

**Conexiones:** [[CloudSyncService]], [[SaveSystem]], [[CloudAuth]], [[CreatureRegistrySO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[GameEvents]], [[RegistryData]]
