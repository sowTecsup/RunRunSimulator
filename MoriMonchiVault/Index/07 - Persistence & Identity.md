---
tags: [index, cloud]
---

# 07 - Persistence & Identity

**Responsabilidad:** Persistencia local JSON (aislado por cuenta), identidad inmutable de criaturas, bus de eventos global.

**Scripts:**
| Script | Ruta | Rol |
|--------|------|-----|
| [[GameManager]] | `Core/GameManager.cs` | Ciclo de vida + orquestador persistencia (save+push) |
| [[GameEvents]] | `Core/GameEvents.cs` | Bus eventos cross-system estatico |
| [[SaveSystem]] | `Core/SaveSystem.cs` | I/O JSON a disco (Newtonsoft) |
| [[CreatureRegistrySO]] | `Data/CreatureRegistrySO.cs` | Cache memoria Dictionary<string, CreatureDNA> |
| [[FurnitureRegistrySO]] | `Data/FurnitureRegistrySO.cs` | Cache memoria muebles colocados |
| [[PlayerInventorySO]] | `Data/PlayerInventorySO.cs` | Inventario persistente del jugador |

**Pipeline Persistencia:**
Mutacion GameEvents.OnRegistryChanged GameManager.SaveDatabase (disco) CloudSyncService.PushToCloud (nube)

**Excepcion NeedsState:** No dispara eventos (cada frame). Flush solo en quit/pause.

**Identidad Criatura:**
- Genetic String: ToStringID() (inmutable, ej: BS0-A3-E1-M2-FF00AA)
- UniqueID: incluye Timestamp ticks (ej: BS0-A3-E1-M2-FF00AA-{Ticks})
- IDs de partes jamas usan guion medio

**Reglas de Oro:**
- Cero acoplamiento: evento transporta payload, suscriptor no busca singleton
- Scoping por jugador: JSON con formato _<playerId>
- sync_meta.json para detectar manipulacion local de savefiles

---

## S128 · Sobre con version, reconciliacion y cartera (hito HC, piezas C2 y C3)

**Todo archivo de guardado es ahora un sobre**, en disco y en la nube:

```
{ "Version": 2, "SavedAtTicks": <UtcNow.Ticks al escribir>, "Data": { ...lo de antes... } }
```

- `Scripts/Logic/SaveEnvelope.cs` y `SaveMigrations.cs` viven en el assembly propio **`MoriMonchi.Logic`** (solo Newtonsoft, nada de Unity ni de tipos del juego). `Assembly-CSharp` lo ve por `autoReferenced`.
- `SaveMigrations.Read(json, kind)` **nunca lanza**: JSON nulo, vacio o corrupto devuelve un sobre vacio. Un guardado legado (sin `Version` y `Data` en la raiz) entra como v1 y se migra. Un sobre de version futura se devuelve intacto.
- Paso v1 a v2 (`SaveKind.Inventory`): `AdventureMaterial` pasa a `Minerita`; se borran `PassiveMaterial` y `EvolutionEssence`. Los otros tres tipos pasan sin cambios.
- `SaveKind` = `Registry | Furniture | Inventory | Social`. C4 sube a v3 y C6 a v4.
- Pruebas: `Tests/EditMode/SaveMigrationsTests.cs`, 10 casos.

**Arranque: se compara antes de pisar.** `CloudSyncOps.SyncOnStartupAsync()` reemplaza al `PullAsync()` ciego y decide con tres numeros (`CloudPushedAt` de la nube, `LocalKnownCloudAt` del meta local, `SaveSystem.LatestLocalSavedAt()`): nube vacia sube lo local; si nadie mas subio no baja nada; si otra PC subio y lo local no tiene cambios pendientes baja; y si los dos cambiaron es **conflicto**: se escribe `*.conflict.bak.json` de los cuatro archivos **antes** de aplicar y gana el mas nuevo. `SecurityStatus` dice que rama corrio.

**Push agrupado.** `GameManager` guarda a disco al instante y encola la subida (`pushDelaySeconds = 5`, `Update()` con tiempo sin escala para que la pausa no lo congele); `FlushToCloudAsync` cancela el pendiente y sube ya. Muebles e inventario piden push por primera vez. `PushAsync` que llega con otro en curso ya no se descarta: se repite una vez al terminar.

**Quinta clave en la nube:** `socialgraph`, con el mismo filtro de ids vivos que `LoadSocialGraph` al importar.

**La cartera tiene una sola puerta.** `Wallet` (estatico, `Systems/Store/Wallet.cs`) es el unico lugar que suma o gasta: `Balance(Currency)`, `Add(Currency, int, string reason)`, `TrySpend(Currency, int, string reason)`. Registra el motivo (`[Wallet] +46 Minerita (expedition) -> 130`) y dispara **un** `GameEvents.InventoryChanged` solo si muto. `PlayerInventorySO` expone `Balance`/`Add`/`TrySpend`/`ResetCurrency`; ninguna pantalla resta monedas por su cuenta.

**Invariante de orden (bug arreglado en S128):** una operacion compuesta muta **todo** antes de cobrar, porque el evento de `Wallet` es el que guarda. `StoreManager.BuyFurniture` comprueba saldo, concede el mueble y cobra al final; al reves, el desbloqueo no llegaba al disco.

**Manifiesto de Cloud Code:** `CloudCode/README.md` en la raiz del repo (que script llama quien, que esta publicado, que hay que despublicar). Los `.js` de cria usan **Custom Data** (`breeding_eggs_{playerId}`) y no tocan ninguna clave de Player Data: el sobre no los rompe.

---

## S129 · Guardado v3: dos estantes y sin stats (hito HC, piezas C1b y C4)

**El registro dejo de ser un diccionario plano.** En `v3` la raiz del guardado de criaturas es:

```
"Data": { "Alive": { id: dna, ... }, "Departed": { id: dna, ... } }
```

- Paso v2 a v3 (`SaveKind.Registry`): toda entrada con `IsDead == true` o `BusyState == "Sold"` va a `Departed`, y de **cada** criatura se borran `BaseConstitution`, `BaseAttack`, `BaseSpeed`, `BaseDefense`, `BaseLuck`, `BaseEvasion` y `Equipped` (el ADN ya no los tiene). Paso v2 a v3 (`SaveKind.Inventory`): fuera `EquipmentGrids`.
- `CurrentVersion = 3`. Pruebas: 16 casos en `SaveMigrationsTests`.
- **Una build vieja no puede leer un guardado v3**: la cadena es hacia adelante y un sobre de version futura se devuelve intacto. Antes de abrir el juego en la otra PC hay que actualizarla.

**Quien guarda que.** `SaveSystem.SaveDatabase` y el push de `CloudSyncOps` serializan `registry.GetData()` (los dos estantes), **nunca `GetAll()`**: guardar solo las vivas borraria a las idas del archivo. `LoadInto` y el pull entran por `LoadFrom(RegistryData)`. `LoadDatabaseCopy()` sigue devolviendo solo vivas (su consumidor es el elenco de la arena).

**El registro tiene dos estantes.** `CreatureRegistrySO`: `GetAll()` = solo vivas · `Departed` = idas · `GetAllKnown()` = las dos (lo usa el arbol genealogico) · `TryGet` busca en las dos, asi que la herencia sigue resolviendo abuelos y bisabuelos · `Depart(id)` mueve · `MaxDeparted = 300`, y al pasarse se descartan las mas antiguas por `Timestamp` **que no sean madre ni padre de ninguna viva**.

**Una sola salida.** `CreatureLifecycle.Kill(dna)` y `Adopt(dna)` (`Systems/Creatures/`) son los unicos lugares del codigo que escriben `IsDead` o `BusyReason.Sold`: mutan, llaman `Depart` y disparan `GameEvents.CreatureDeparted(dna)` + `RegistryChanged`. Suscriptores reales del evento nuevo: `InfoOverlayUITK` (aviso) y `SocialGraphService` (borra las aristas de quien se fue).

**Una sola respuesta a "se puede usar".** `CreatureAvailability` (estatica pura) con `IsFree` / `IsWellCared` / `CanExplore` / `WeakestNeed`, contra el asset `CareGate.asset` (`CareGateSO`: `MinHealth = 60`, `MinEnergy = 60`, `MinAffect = 0`, **sin calibrar**). Con `gate == null` no bloquea.

**`CreatureDNA.Generation`** (int): minteada 1, cria `max(madre, padre) + 1`, y relleno unico al cargar dentro de `CreatureRegistrySO.ReconcileColors()` (memoizado, con corte de ciclo) para todo lo que venga en 0.
