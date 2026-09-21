# Manifiesto de Cloud Code — MoriMonchis

Escrito en la sesión HC-1 (plan `MoriMonchiVault/Index/29` §4.7). Este archivo existe porque hasta ahora no había forma de saber desde el repo qué endpoints están publicados en el dashboard de UGS y cuáles quedaron huérfanos.

La columna **Publicado** la completa Juan desde el dashboard de Unity Cloud Code. El resto lo mantiene la IA cuando toca un `.js` o su llamador.

## Endpoints vivos

| Script | Quién lo llama (C#) | Publicado | Notas |
|---|---|---|---|
| `start-breeding.js` | `Systems/Breeding/AsyncBreedingService.cs:12` | | Duración de cría **hardcodeada en el servidor** (`BREED_DURATION_MS = 30 min`). `InheritanceOddsTableSO.BreedDurationMinutes` es solo display. **Se retira en HC-4** (la cría pasa al reloj de juego local, `Index/29` §13.4). |
| `hatch-breeding.js` | `Systems/Breeding/AsyncBreedingService.cs:13` | | Valida `readyAt` contra el reloj del servidor. **Se retira en HC-4.** |
| `cancel-breeding.js` | `Systems/Breeding/AsyncBreedingService.cs:14` | | **Se retira en HC-4.** |
| `cancel-all-breeding.js` | `Systems/Breeding/AsyncBreedingService.cs:15` · `Systems/Cloud/CloudSyncOps.cs:18` (`ResetProgressAsync`) | | **Se retira en HC-4**; `ResetProgressAsync` deja de llamarlo. |
| `get-server-time.js` | `Systems/Cloud/CloudAuth.cs:101` | | No usa Cloud Save. Alimenta `GameManager.Now` / `ServerNow` (offset de servidor). **Se queda**: la reconciliación por fecha de C2 y el anti-cheat dependen de una hora que el cliente no elige. |

## Diagnóstico (no son lógica de juego)

| Script | Quién lo llama (C#) | Publicado | Notas |
|---|---|---|---|
| `test-random.js` | `Systems/Cloud/CloudCodeTester.cs:24` | | Health-check del pipeline de endpoints. |
| `test-customdata.js` | `Systems/Cloud/CloudCodeTester.cs:43` | | Escribe/lee la clave `diagnostic_ping`. |

## Dos almacenamientos distintos en Cloud Save

No se mezclan, y conviene no confundirlos al depurar:

- **Player Data**, escrito desde C# por `CloudSyncOps`: claves `creatureregistry`, `furnitureregistry`, `playerinventory`, `socialgraph` (nueva en HC-1) y `sync_meta`. Desde HC-1 cada valor es un **sobre** `{ Version, SavedAtTicks, Data }` (`Scripts/Logic/SaveMigrations.cs`).
- **Custom Data**, escrito desde los `.js` de cría: una sola clave `breeding_eggs_{playerId}`. Los `.js` **no leen ni escriben** ninguna clave de Player Data, así que el sobre de HC-1 no los rompe.

## A despublicar en el dashboard (tarea de Juan)

Endpoints del **combate asíncrono demolido en S75** y del **Dragon RPS borrado en HC-1**. No están en este repo (nunca se versionaron) pero pueden seguir publicados y aceptando llamadas:

- cualquier endpoint de matchmaking / cola de combate;
- cualquier endpoint que escriba el buzón de resultados de combate;
- cualquier endpoint de resolución de combate por semilla.

Si al revisar el dashboard aparece alguno que no figura en las dos primeras tablas de este archivo, es huérfano: despublicarlo y anotarlo aquí.
