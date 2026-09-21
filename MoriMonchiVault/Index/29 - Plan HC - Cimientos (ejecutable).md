---
tags: [index, plan, cimientos, hc]
---

# 29 - Plan HC · Cimientos (plan de ejecución para el orquestador)

> Hito HC de [[Index/25 - Hitos hasta el lanzamiento]]. Mapa y razones en [[Index/28 - Cimientos y camino a Game Ready]]. Este documento es el **plan ejecutable**: lo lee el orquestador (Opus) al abrir la sesión, lo reparte a `morimonchi-coder` (uno por lote) y verifica en el editor por Unity MCP. Escrito en S127 leyendo el código real de cada archivo que toca. Nada de lo que dice aquí se rediseña sin preguntarle a Juan.

---

## 0 · Antes de empezar (obligatorio)

> **Raíz del código: `Assets/RunRunSimulator/Scripts/`** (NO `Assets/Scripts/`). Las rutas de este plan se escriben relativas a esa carpeta. Los `.js` de Cloud Code están en `CloudCode/` en la raíz del repo, fuera de `Assets/`. Anotado en S128: los tres exploradores arrancaron contra la ruta equivocada y hubo que redirigirlos (~90k tokens de desperdicio).

1. Leer `09 - Active Context`, [[Index/28 - Cimientos y camino a Game Ready]] §1 (dos monedas), §3 (cimientos) y §7 (decisiones), [[Index/07 - Persistence & Identity]] y [[Index/24 - Puente Tienda-Arena]] §3.
2. ScriptNodes: `SaveSystem`, `GameManager`, `GameEvents`, `CloudSyncService`, `CloudSyncOps`, `PlayerInventorySO`, `CreatureRegistrySO`, `CreatureDNA`, `StoreManager`, `ShopCatalogSO`, `DeliveryBox`, `BuildBrowserUITK`, `StorePanelUITK`, `InfoOverlayUITK`, `NpcAgent`, `ExpeditionBridge`, `ExpeditionHandoff`, `ExpeditionPanelUITK`, `ArenaRun`, `ArenaRunDirector`, `ArenaFloorPanel`, `MoriMochiSpawner`, `BreedingService`, `UIManager`, `DevToolsConsole`.
3. Confirmar que compila con 0 errores antes de tocar nada (`read_console`).
4. **Respaldar el guardado de Juan** antes de la primera sesión: copiar `creature_database_*.json`, `player_inventory_*.json`, `furniture_registry_*.json`, `social_graph_*.json` y `sync_meta_*.json` de `persistentDataPath` a una carpeta con fecha. Este hito cambia el formato del guardado.
5. Reglas que más se ignoran en sesiones largas: **sin comentarios**, **VFX solo por MMFeedbacks**, **≤ 400 líneas** (`StorePanelUITK` está en 379: lo nuevo va en un colaborador), **composición, no partials**, **ningún script de juego guarda ni sube a la nube** (solo eventos), **un evento sin suscriptor no se agrega**.

---

## 1 · Decisiones de Juan ya tomadas (S127)

| # | Decisión |
|---|---|
| 1 | La segunda moneda se llama **Minerita** y es el material que se asegura en la bajada. `passiveMaterial` y `evolutionEssence` se borran. |
| 2 | **Solo la exploración da Minerita.** Ningún trabajo de tienda la produce. |
| 3 | **Solo un MoriMochi bien cuidado puede bajar.** El cuidado es la puerta; dentro de la exploración las necesidades de la tienda **no influyen** (todos entran con la vida de la run llena). |
| 4 | Evolucionar = **subir el nivel de la parte**. La decisión vive en un **ScriptableObject ejecutable** para poder cambiarla por otra (ver §8). |
| 5 | **La cría pasa al reloj de juego** (local, sin Cloud Code), y **para eclosionar el huevo hace falta Minerita** (ver §13). |
| 6 | **La suciedad entra** como uno de los sistemas de entretenimiento de la tienda (mantener ocupado al jugador). No es cimiento: va en E1 (`Index/28`). |
| 7 | **Stats y equipo se borran** porque no se usan, y **se actualiza la UI** (ver §12). |
| 8 | **El tutorial guía al jugador hasta comprar su primera caja de MoriMonchis en la PC, que está gratis con 100 % de descuento** (ver §14). |

Confirmaciones menores resueltas en §9.

---

## 2 · Reparto por sesiones

| Sesión | Piezas | Por qué juntas |
|---|---|---|
| **HC-1** | C1 limpieza · C2 guardado · C3 cartera | C2 crea la cadena de migraciones y C3 es su primer paso (v1 → v2) |
| **HC-2** | C1b borrado de stats y equipo (§12) · C4 ciclo de vida | Comparten el segundo paso de migración (v2 → v3) y la ficha de la criatura |
| **HC-3** | C5 catálogo y caja de MoriMonchis | No toca el formato; es requisito del tutorial |
| **HC-4** | C6 reloj y cría local con Minerita (§13) | Tercer paso de migración (v3 → v4) |
| **HC-5** | C7 cáscara, arranque y tutorial (§14) · primera build | Usa C5 (caja gratis) y el estado de mundo de C6 |

El orquestador puede fundir dos sesiones si el ritmo lo permite. Si falta tiempo se corta **C6** (C7 crea entonces el archivo de estado de mundo, ver §14.1). Nunca C2.

---

## 3 · C1 · Limpieza del combate viejo

**Regla:** lo que no es del juego vigente no viaja más.

### Borrar (archivos completos)
- `Scripts/DragonRps/` entero (8 archivos).
- `Scripts/Systems/Combat/` entero (`DragonRpsService`, `DragonRpsGenes`, `DragonRpsRival`, `CombatTuningSO`, `CombatOutcome`).
- `Scripts/UI/`: `CombatPanelUITK`, `CombatDuelPresenter`, `CombatPickPresenter`, `CombatResultPresenter`, `RpsTriangleElement`.
- Sus UXML/USS y `CombatTuning.asset`.

### Editar
- `Core/Enums/UIEnums.cs`: quitar `Combat = 4` (los demás valores **no se renumeran**).
- `Core/DevToolsConsole.cs`: quitar los botones de combate (bloque ~120-180) y sus campos.
- `Data/Genetics/CreatureDNA.cs`: quitar `CombatCooldownUntil` y `HeldItemId` (Newtonsoft ignora las propiedades sobrantes del JSON viejo: no requiere migración).
- `UI/CreatureDisplay.cs`: quitar la rama de estado que lee el cooldown de combate.
- `Data/Items/ItemDefinitionSO.cs` + `Core/Enums/ItemEnums.cs`: quitar `Trigger` / `ItemTriggerKind` (sin lector).
- Verificar con grep que no queda ninguna referencia a `DragonRps`, `CombatTuning`, `CombatOutcome`, `UIPanelType.Combat`, `RpsTriangle`, `CombatCooldownUntil`, `HeldItemId`, `ItemTriggerKind`.

### Muta fuera de código (OK de Juan, por MCP)
1. `GameScene`: objeto del panel de combate y su entrada en el diccionario de paneles de `UIManager`; ref de combate en `DevToolsConsole`.
2. Prefab `Furnitures/Containers/Ring`: quitar el `PanelTrigger`. El Ring queda como mueble decorativo (hay guardados con uno colocado) y **sale del catálogo**; E3 lo puede reusar como estación de entrenamiento.
3. `ShopCatalog.asset`: quitar la fila del Ring.
4. Re-guardar los 3 assets de arquetipo de cliente (arrastran `WeightCombat` huérfano).
5. Tabla `Strings`: borrar las claves `ui.combat.*` y `status.*` de cooldown si quedan sin uso.

### No se toca en C1
Stats y equipo se borran en **C1b (§12, sesión HC-2)**, no aquí: su borrado cambia el guardado y la ficha de la criatura.

### Verificación
Compila con 0 errores; grep limpio; Play en `GameScene`: los 6 disparadores de panel restantes abren; ningún `Missing Script` en escena (`execute_code` contando componentes nulos); un Ring colocado no abre nada y no lanza excepción.

---

## 4 · C2 · Guardado robusto

**Regla:** el guardado tiene versión, fecha y tamaño acotado; nunca gana nadie a ciegas.

### 4.1 Sobre con versión — `Core/SaveEnvelope.cs` (NUEVO)
```
public class SaveEnvelope
{
    public int    Version;
    public long   SavedAtTicks;      // DateTime.UtcNow.Ticks al escribir
    public JToken Data;
}
```

### 4.2 Cadena de migraciones — `Core/SaveMigrations.cs` (NUEVO, estática pura: solo `Newtonsoft.Json.Linq`)
```
public enum SaveKind { Registry, Furniture, Inventory, Social }

public static class SaveMigrations
{
    public const int CurrentVersion = 2;                 // C4 lo sube a 3
    public static SaveEnvelope Read(string json, SaveKind kind);
        // raíz con "Version" entero y "Data" → sobre; si no → legado = { Version 1, SavedAtTicks 0, Data = raíz }
        // aplica en orden cada paso (kind, v) con v < CurrentVersion y devuelve el sobre en CurrentVersion
    public static string Write(JToken data, long savedAtTicks);
}
```
Un guardado de registro legado es un diccionario cuya clave es el `UniqueID` (siempre contiene `-`): no puede confundirse con un sobre.

Pasos de HC-1 (v1 → v2): `Inventory`: `AdventureMaterial` → `Minerita`; se eliminan `PassiveMaterial` y `EvolutionEssence`. Los otros tres tipos pasan sin cambios.

### 4.3 `Core/SaveSystem.cs` (MODIFICADO)
- Todo `Save*`/`Serialize*` escribe el sobre (`SaveMigrations.Write`); todo `Load*`/`Deserialize*` lee por `SaveMigrations.Read` y deserializa `envelope.Data`. `CloudSyncOps` ya serializa y deserializa a través de `SaveSystem`, así que la nube recibe el mismo sobre sin cambios propios.
- `public static long LatestLocalSavedAt()` → el mayor `SavedAtTicks` de los cuatro archivos del scope (0 si no hay ninguno).
- `public static void BackupLocal(string suffix)` → copia los cuatro archivos del scope a `*.{suffix}.bak.json` (pisa el respaldo anterior).
- `SerializeSocialGraph()` / `DeserializeSocialGraph(json)` para la nube.
- Cloud Code de cría no lee `creatureregistry` (usa su propia clave `breeding_eggs_{playerId}`): el sobre no lo rompe. Verificado en `CloudCode/*.js`.

### 4.4 Reconciliación por fecha — `Systems/Cloud/CloudSyncOps.cs` (MODIFICADO)
Nueva operación que reemplaza al `PullAsync` ciego del arranque:
```
public async Task SyncOnStartupAsync()
```
1. Lee `sync_meta` de la nube (`CloudPushedAt`) y el meta local (`LocalKnownCloudAt`), más `SaveSystem.LatestLocalSavedAt()`.
2. **Nube sin datos** → si hay guardado local, `PushAsync`; fin.
3. **`LocalKnownCloudAt == CloudPushedAt`** (nadie más subió) → no se baja nada; si `LatestLocalSavedAt > LocalKnownCloudAt`, `PushAsync`.
4. **Distintos** (otra PC subió): si lo local no tiene cambios sin subir (`LatestLocalSavedAt <= LocalKnownCloudAt` o no hay guardado local) → `PullAsync`. Si los tiene → **conflicto**: gana el más nuevo entre `LatestLocalSavedAt` y `CloudPushedAt`; antes de aplicar, `SaveSystem.BackupLocal("conflict")`; si gana la nube → `PullAsync`, si gana lo local → `PushAsync`. `SecurityStatus` dice qué pasó.
5. `PullAsync` y `PushAsync` siguen existiendo para los botones de desarrollo.

`CloudSyncService.HandleSignedInAsync` llama `SyncOnStartupAsync` en lugar de `PullAsync`. `StartupSyncDone` no cambia de contrato.

### 4.5 Push agrupado — `Core/GameManager.cs` + `CloudSyncOps.cs` (MODIFICADO)
- `GameManager`: `Persist`, `PersistFurniture` y `PersistInventory` guardan a disco al instante y llaman `RequestPush()`; `[SerializeField, Min(1f)] private float pushDelaySeconds = 5f`; `Update` dispara `PushToCloud()` cuando vence el plazo (tiempo sin escala). `FlushToCloudAsync` (salir, pausar, bajar) sube de inmediato y cancela el pendiente. Hoy muebles e inventario **no** piden push: ahora sí.
- `CloudSyncOps.PushAsync`: si llega un pedido con otro en curso, en vez de descartarlo marca `pushAgain` y repite una vez al terminar.

### 4.6 Grafo social a la nube
Clave nueva `socialgraph` en el payload de push y en la lectura de pull (import con el mismo filtro de ids vivos que `LoadSocialGraph`). `ResetProgressAsync` también la borra.

### 4.7 Manifiesto de Cloud Code — `CloudCode/README.md` (NUEVO, no es código del juego)
Tabla: script · quién lo llama · publicado (lo completa Juan desde el dashboard) · notas. Lista aparte de endpoints del combate viejo a **despublicar** (tarea de Juan en el dashboard; memoria `project_async_combat`).

### 4.8 Red de seguridad — assembly de lógica pura
- `Scripts/Logic/MoriMonchi.Logic.asmdef` (NUEVO). Nace **solo con lo nuevo y sin dependencias del resto**: `SaveEnvelope`, `SaveMigrations`, `SaveKind`. `Assembly-CSharp` lo ve solo.
- `Tests/EditMode/MoriMonchi.Logic.Tests.asmdef` (NUEVO) con pruebas de: legado v1 → v2 de inventario (renombre y borrado), sobre ya en versión actual (no se toca), registro legado con 3 criaturas (envuelve sin perder campos), JSON vacío o corrupto (devuelve sobre vacío, no lanza).
- Lo que ya existe y es puro (`ArenaRun`, herencia, valuación) **no se muda ahora**: depende de enums y tipos de `Assembly-CSharp`. Se muda en H7.
- **Parar y preguntar** si el asmdef no resuelve Newtonsoft sin tocar `Packages/manifest.json`.

### Verificación
1. Pruebas EditMode en verde (`run_tests`).
2. Play con el guardado real de Juan: carga las mismas N criaturas, M muebles y saldos; los cuatro archivos quedan reescritos con `Version: 2`.
3. Sonda de conflicto: tocar `CloudPushedAt` de la nube por `execute_code` (o subir desde una segunda sesión anónima no aplica: es por jugador) → simular con el meta local: poner `LocalKnownCloudAt` en un valor viejo y un cambio local sin subir → aparece `*.conflict.bak.json` y gana el más nuevo.
4. Diez mutaciones seguidas en 2 s (mintear 10) → **un** `[CloudSync] Pushed` en consola.
5. Ida y vuelta a la arena: el push termina antes de cargar la escena (como en S124) y al volver no se baja nada (`LocalKnownCloudAt == CloudPushedAt`): el aviso de vuelta aparece sin esperar el pull.

---

## 5 · C3 · Cartera de dos monedas

**Regla:** dabloons y Minerita; todo ingreso y todo gasto pasa por una sola puerta, con motivo.

### 5.1 `Core/Enums/StoreEnums.cs`
```
public enum Currency { Dabloons = 0, Minerita = 1 }
```

### 5.2 `Data/Player/PlayerInventorySO.cs` (MODIFICADO)
- Campos: `dabloons`, `minerita` (`[PreviouslySerializedAs("adventureMaterial")]`). Se borran `passiveMaterial` y `evolutionEssence`.
- API de monedas (reemplaza `AddDabloons`, `SpendDabloons`, `ResetDabloons`, `AddAdventureMaterial`, `AdventureMaterial`, `PassiveMaterial`, `EvolutionEssence`):
```
public int  Balance(Currency c);
public void Add(Currency c, int amount);          // ignora amount <= 0
public bool TrySpend(Currency c, int amount);     // false si no alcanza o amount <= 0
public void ResetCurrency(Currency c);            // solo desarrollo
```
- `InventoryData`: `Dabloons`, `Minerita`.

### 5.3 `Systems/Store/Wallet.cs` (NUEVO, estática, ≤ 60 líneas) — la puerta
```
public static int  Balance(Currency c);
public static void Add(Currency c, int amount, string reason);
public static bool TrySpend(Currency c, int amount, string reason);
```
Usa `GameManager.CurrentInventory`; tras mutar dispara **un** `GameEvents.InventoryChanged`; registra `[Wallet] +46 Minerita (expedition) → 130`. Ningún otro script llama `inventory.Add/TrySpend` de monedas.

### 5.4 Llamadores a migrar
`NpcAgent.AcceptCurrentOffer` (motivo `adoption`; deja de disparar `InventoryChanged` a mano), `StoreManager.BuyFurniture` / `BuyWorldProp` y su reembolso (`store`, `store-refund`), `ExpeditionBridge.ApplyResult` (`expedition`), `DevToolsConsole` (`dev`), `InfoOverlayUITK.RefreshDabloons` y `StorePanelUITK` (leen `Balance`).

### 5.5 Textos visibles
`ExpeditionReturn.MaterialGained` → `MineritaGained`. Tabla `Strings` en/es: `ui.overlay.material`, `ui.overlay.expedition.return`, `ui.overlay.expedition.lost` dicen **Minerita**. En la arena, todo texto visible que diga "material" pasa a "Minerita" (`ArenaFloorPanel`, `ArenaRoundHud`, `ArenaResultPanel`, `ArenaPlanPanel`); los nombres internos (`MaterialPickup`, `PlayerSecured`) no se renombran.

### Muta fuera de código (OK de Juan)
`PlayerInventory.asset` (re-guardar), tabla `Strings`, y el ícono/etiqueta del overlay si Juan quiere distinguir las dos monedas por color (regla `color sobre texto`).

### Verificación
Guardado viejo con `AdventureMaterial: 84` → tras cargar, `Balance(Minerita) == 84` y el archivo ya no tiene las tres claves viejas; venta a un cliente, compra de mueble, compra de ítem y vuelta de la bajada: cuatro líneas `[Wallet]` con saldo correcto y un solo `InventoryChanged` por operación; grep: cero llamadas a monedas fuera de `Wallet` y `PlayerInventorySO`.

---

## 6 · C4 · Ciclo de vida de la criatura

**Regla:** una sola respuesta a "¿se puede usar esta criatura?" y una sola forma de irse. Las que se fueron dejan de pesar, pero siguen siendo ancestros.

### 6.1 Registro con dos estantes — `Data/Genetics/CreatureRegistrySO.cs` (MODIFICADO)
```
private Dictionary<string, CreatureDNA> creatures;   // vivas y presentes
private Dictionary<string, CreatureDNA> departed;    // muertas y adoptadas

public bool TryGet(string id, out CreatureDNA dna);             // busca en los dos (ancestros, árbol, relaciones siguen funcionando)
public Dictionary<string, CreatureDNA> GetAll();                // SOLO vivas
public IReadOnlyDictionary<string, CreatureDNA> Departed { get; }
public bool Depart(string id);                                  // mueve de creatures a departed
public RegistryData GetData();  public void LoadFrom(RegistryData data);
public const int MaxDeparted = 300;
```
`RegistryData { Dictionary<string, CreatureDNA> Alive; Dictionary<string, CreatureDNA> Departed; }`. Al pasar de `MaxDeparted` se descartan las más antiguas **que no sean madre ni padre de ninguna viva** (la herencia consulta hasta bisabuelos: `BreedingService.ExpandGenerations`).

Por qué así y no un historial aparte: `TryGet` sigue resolviendo ancestros, árbol genealógico y relaciones sin tocar esos lectores; `GetAll` deja de cargar muertas y vendidas, así que los ~12 filtros `IsDead || IsSold` repartidos quedan inofensivos (se limpian en el mismo lote donde se toque cada archivo, no todos ahora).

### 6.2 Migración v2 → v3 (`SaveMigrations`, `Registry`)
La raíz pasa de diccionario a `{ Alive, Departed }`; las entradas con `IsDead == true` o `BusyState == "Sold"` van a `Departed`. `CurrentVersion = 3`. Pruebas: registro v1 con una muerta, una vendida y dos vivas → `Alive` 2, `Departed` 2; `Generation` intacta.

### 6.3 Generación — `CreatureDNA.Generation` (int, NUEVO)
Minteada = 1. `BreedingService`: `child.Generation = max(madre, padre) + 1`. Relleno único al cargar: si vale 0, se calcula caminando padres (memoizado) en el mismo barrido de `GameManager` que hoy corre tras `LoadFrom`.

### 6.4 Salidas con dueño — `Systems/Creatures/CreatureLifecycle.cs` (NUEVO, estática, ≤ 80 líneas)
```
public static void Kill(CreatureDNA dna);                 // IsDead = true → Depart → eventos
public static void Adopt(CreatureDNA dna);                // BusyState = Sold, SaleDate = ahora → Depart → eventos
```
Ambas: `registry.Depart(id)`, `GameEvents.CreatureDeparted(dna)`, `GameEvents.RegistryChanged(registry)`. Únicos lugares del código que escriben `IsDead` o `BusyReason.Sold`.

`Core/GameEvents.cs`: `OnCreatureDeparted(CreatureDNA)`. Suscriptores reales (regla: sin suscriptor no se agrega): `InfoOverlayUITK` (aviso "X fue adoptada" / "X no volvió", clave nueva en/es) y `SocialGraphService` (borra sus aristas; hoy quedan huérfanas). `MoriMochiSpawner` no necesita suscribirse: ya despawnea lo que desaparece de `GetAll` al oír `RegistryChanged` (línea ~301).

Llamadores: `NpcAgent.AcceptCurrentOffer` → `CreatureLifecycle.Adopt` + `Wallet.Add`; `ExpeditionBridge` → `CreatureLifecycle.Kill`.

### 6.5 Disponibilidad única — `Data/Genetics/CreatureAvailability.cs` (NUEVO, estática pura)
```
public static bool IsFree(CreatureDNA dna);                          // no muerta y sin BusyState
public static bool IsWellCared(CreatureDNA dna, CareGateSO gate);    // las tres necesidades sobre su umbral
public static bool CanExplore(CreatureDNA dna, CareGateSO gate);     // IsFree && IsWellCared
public static NeedType? WeakestNeed(CreatureDNA dna, CareGateSO gate); // la que impide bajar, para la UI
```
`Data/Expedition/CareGateSO.cs` (NUEVO, asset): `MinHealth = 60`, `MinEnergy = 60`, `MinAffect = 0` (números de partida; los ajusta Juan). Lo usan `ExpeditionPanelUITK` (reemplaza `!IsBusy && Health > 0`), y `IsFree` reemplaza las comprobaciones sueltas de cría y venta (`BreedingBreedTabPresenter`, `BreedingDevConsole`, `NpcAgent.BestPickFromContainer`).

### 6.6 La decisión 3 en la bajada
- `ArenaRunDirector.Start`: deja de llamar `run.SetStartHealth(id, dna.Needs.Health)`; todas entran con `ArenaRun.MaxHealth`. Se conserva el llenado de `names`.
- `ArenaRun.ToResult`: `HealthById` se reemplaza por `List<string> FallenIds` (vida de la run en 0). `ExpeditionResult`/`ExpeditionReturn`: fuera `HealthById` y `HealthLost`; `Fallen` se conserva.
- `ExpeditionBridge.ApplyResult`: ya no toca `Needs`. Con `permadeathEnabled`: `Kill` a cada caída, y a toda la terna si `Lost`. Sin el flag, volver no le cuesta nada a la criatura (*"bajar es gratis"*, S124).
- `ExpeditionPanelUITK`: la tarjeta muestra las tres necesidades como tres marcas de color (apta / baja) y, si no puede bajar, resalta la que falta; nada de párrafos (regla `color sobre texto`). Las claves `ui.expedition.energy`/`tired` se reescriben.
- Textos del aviso de vuelta sin "vida": "Volviste del piso {0} · +{1} Minerita · {2} caídas".

### 6.7 Calibración del cuidado (datos, no código)
Hoy la vida cae 0,5/s suelta (de 100 a 0 en ~3 min) y 6 de 10 criaturas están en 0. Con el cuidado como puerta, el decaimiento tiene que dejar a una criatura atendida apta durante una sesión de tienda. Medir en Play cuánto tarda una criatura en perder la aptitud y proponer a Juan los valores del perfil de necesidades (son campos serializados de `MoriMochiAgent`/perfil, no código). C6 después los pasa a tiempo de juego.

### Muta fuera de código (OK de Juan)
Asset `CareGate.asset` y su ref en el panel de bajada; UXML/USS del panel de bajada; tabla `Strings`; `CreatureRegistry.asset` (re-guardar).

### Verificación
Pruebas EditMode de la migración v2 → v3. Play con el guardado de Juan: vivas en `GetAll`, muertas y vendidas en `Departed`, el árbol genealógico de una criatura con abuelo vendido sigue mostrando al abuelo; cría entre dos criaturas cuyo abuelo está en `Departed` hereda sin errores. Venta: la criatura sale del mundo, aparece el aviso, el grafo social ya no la nombra, un solo push. Bajada con `permadeathEnabled` encendido por sonda y caída forzada (`ArenaRun` por `execute_code`): al volver la caída está en `Departed` con `IsDead`. Panel de bajada: una criatura con afecto bajo aparece no apta y resalta el afecto.

---

## 7 · C5 · Catálogo unificado y propiedad

**Regla:** todo lo que se compra es una fila de catálogo con precio y forma de entrega; lo comprado se posee y solo lo poseído se puede usar.

Modelo de propiedad de muebles: **desbloqueo** (se compra una vez, se coloca las veces que se quiera). Es el modelo que ya tienen los datos (`furnitureOwned` es un conjunto y `BuyResult.AlreadyOwned` existe); no se agrega conteo.

### 7.1 El modo construcción respeta la propiedad — `UI/BuildBrowserUITK.cs`
`RefreshPieces` filtra por `inventory.HasFurniture(def.Id)`; se suscribe a `OnInventoryChanged`/`OnInventoryReloaded` (y se desuscribe) para refrescar; el vacío dice dónde comprar.
**Relleno único** para no dejar a Juan sin muebles: tras cargar muebles e inventario, `GameManager` concede la propiedad de toda definición que ya esté colocada (`furnitureRegistry` → `inventory.AddFurniture`), y dispara `InventoryChanged` solo si agregó algo.

### 7.2 Caja de MoriMonchis
- `Data/Store/CreatureBoxSO.cs` (NUEVO): `Id`, `DisplayName`, `Description`, `Count` (cuántas criaturas trae). Sin garantías de rareza ni filtros todavía (regla 4).
- `ShopCatalogSO`: `CreatureBoxListing { CreatureBoxSO Box; StoreShopData Shop; }` + lista + `RestockAll` la incluye.
- `StoreManager.BuyCreatureBox(CreatureBoxSO box, StoreShopData shop)`: `Wallet.TrySpend(Dabloons, precio, "store")` → instancia la `DeliveryBox` configurada con la caja.
- `DeliveryBox`: `Configure(CreatureBoxSO)`; al abrir, mintea `Count` criaturas por el camino único de minteo y se destruye. El minteo sale de `GameManager.MintRandomCreature` a un método reutilizable (`GameManager.MintCreature()` que devuelve el ADN; el botón Odin lo llama) para que la caja, el arranque (C7) y el botón usen lo mismo. Un solo `RegistryChanged` por caja.
- Dónde aparecen: confirmar en `MoriMochiSpawner` si hay API para nacer en una posición; si la hay, salen de la caja; si no, por el spawner como hoy (no bloquea).

### 7.3 Pestaña en la tienda — `UI/StoreCreatureTabPresenter.cs` (NUEVO colaborador)
`StorePanelUITK` (379 líneas) no crece: le pasa el `root`, el `StoreManager` y el refresco de saldo. Cuarta pestaña "MoriMonchis". UXML/USS con la paleta `--mm-*`.

### 7.4 Llenar el catálogo (datos)
`ShopCatalog.asset`: filas para los muebles definidos que hoy no se venden (hay 10 definiciones y 2 filas) y una caja (`Caja de 2 MoriMonchis`). Los precios los propone el orquestador en una tabla corta y **los aprueba Juan** antes de escribir el asset.

### No entra todavía
Filas de cosméticos y de mejoras de tienda: se agregan en E1 junto con su primer contenido real (regla 4).

### Muta fuera de código (OK de Juan)
`ShopCatalog.asset`, asset de la caja, prefab `DeliveryBox` si necesita otra malla para la caja de criaturas, UXML/USS de la tienda y del modo construcción, tabla `Strings`.

### Verificación
Con el guardado de Juan: el modo construcción muestra solo lo poseído, que incluye todo lo ya colocado; comprar un mueble nuevo lo hace aparecer sin reabrir. Comprar la caja: baja el saldo con línea `[Wallet]`, aparece la caja, al abrirla el registro sube en `Count` con un solo `RegistryChanged` y un solo push, y las criaturas aparecen en el mundo. Sin dabloons: `InsufficientFunds` y nada cambia.

---

## 8 · Evolución como ScriptableObject ejecutable (decisión 4 · se ejecuta en E2, no en HC)

Se deja escrito aquí porque fija el enchufe que C3 habilita.
```
public abstract class EvolutionEffectSO : SerializedScriptableObject
{
    public string DisplayName;  public string Description;
    public abstract bool CanApply(CreatureDNA dna, ClashSlot slot);
    public abstract int  Cost(CreatureDNA dna, ClashSlot slot);      // en Minerita
    public abstract void Apply(CreatureDNA dna, ClashSlot slot);
}
```
- Primer efecto: `PartLevelUpEffectSO` → sube `HornTier`/`WingTier`/`BackTier` hasta `Tier3` (campos que ya existen y hoy nunca se escriben); costo por nivel en una tabla del asset.
- `EvolutionService.Evolve(dna, slot, effect)`: `CanApply` → `Wallet.TrySpend(Minerita, Cost, "evolution")` → `Apply` → `RegistryChanged`. Cambiar la regla de diseño = otro asset (`SwapPartEffectSO`, reroll de habilidad), sin tocar el servicio ni la pantalla.
- Falta decidir en E2 qué mejora el nivel dentro de la bajada (la habilidad de esa parte).

---

## 9 · Confirmaciones de Juan (S127, resueltas)

1. **Los potenciales de parte se conservan**: son el techo del nivel de la parte en E2 (criar sube el techo, la Minerita lo alcanza). **El proyecto Cutie Marks se mantiene** (H2): borrar el equipo no lo cancela; las marcas ocuparán ese lugar con su propio diseño.
2. **El costo de eclosionar sube con cada parte**: no es plano ni por generación. Lectura del orquestador, a confirmar por Juan al ejecutar HC-4: costo = base + un extra por cada nivel de parte por encima de 1, sumando cuerno, alas y espalda de los dos padres. Los dos números viven en el asset (§13.4), así que cambiar la fórmula es cambiar datos.
3. **Volver de la bajada amanece al día siguiente** (§13.2).

---

## 10 · Cierre de cada sesión

- `09 - Active Context`: lo tocado, lo medido y la lista de `.cs` NUEVOS / MODIFICADOS / **BORRADOS** (C1 borra ~20: sus ScriptNodes se eliminan).
- `Index/28`: marcar la pieza ✅ y actualizar su fila del inventario de sistemas.
- `Index/07 - Persistence & Identity` e `Index/10 - Furniture & Building`: reflejar sobre con versión, reconciliación, cartera y propiedad (cambian contratos públicos).
- Reportar consumo de tokens (subagentes vs orquestador vs desperdicio).
- `vault-documenter` **solo** si Juan corre `/cerrar-sesion`.

## 11 · Parar y preguntar a Juan si…

- El asmdef de lógica no resuelve Newtonsoft sin tocar el manifiesto de paquetes (4.8).
- Algún lector de `registry.GetAll()` necesitaba ver muertas o vendidas (historial de ventas, estadísticas) y se rompe con 6.1.
- El diccionario de paneles de `UIManager` no admite quitar `Combat` sin perder las otras entradas (C1).
- Los precios del catálogo (7.4) o los umbrales del cuidado (6.5).

---

## 12 · C1b · Borrado de stats y equipo (decisión 7 · sesión HC-2)

**Regla:** lo que no afecta el juego no se guarda, no se hereda y no se muestra. La ficha de la criatura pasa a mostrar lo que sí importa.

### Borrar (archivos completos)
`Data/Equipment/` (3), `Data/Databases/EquipmentDatabaseSO.cs`, `Systems/Stats/` (`CreatureStats`, `EquipmentStats`), `UI/DetailEquipTabPresenter.cs`, `UI/EquipmentBackpackUITK.cs`, y los assets de `ScriptableObjects/Equipment/` (9 ítems, paleta, base de datos) con sus UXML/USS.

### Editar
- `CreatureDNA`: fuera `BaseConstitution`, `BaseAttack`, `BaseSpeed`, `BaseDefense`, `BaseLuck`, `BaseEvasion`, `Equipped` y todo el bloque `#if UNITY_EDITOR` de ranuras de equipo. Los **tiers se quedan** (son el nivel de la parte de E2) y los **potenciales también** (son su techo, §9.1).
- `Core/Enums/ItemEnums.cs`: fuera `StatType` y `EquipmentSlot`; `LocEnumMaps.EquipmentSlotName` también.
- `PlayerInventorySO`: fuera `equipmentGrids`, su API (`AddEquipment`, `RemoveEquipmentAt`, `MoveEquipment`, `GetEquipment`, `ClearEquipmentOwned`) e `InventoryData.EquipmentGrids`.
- `CreatureGenerator.RandomBaseStats` y sus llamadas (`GameManager.MintRandomCreature`, `ArenaSandbox` ~380); `BreedingService.InheritStat` y las tres líneas de herencia de stats.
- `GameManager`: fuera `equipmentDatabase` y su getter.
- `ValuationHandler`: fuera `statsBonus`; `CustomerPricingSO.StatsMultiplier` y `CustomerArchetypeSO.WeightStats` también. La valuación queda en tiers + crías hasta que E1 la rehaga.
- `MoriMochiAgent`: fuera el bloque de inspector de stats (~459-498: `StatCon…StatEva`, `StatsBase`, `StatsFinal`, `StatLine`, `StatValue`); baja de 714 líneas.
- `DevToolsConsole`: fuera lo que toque stats; el botón de potenciales (~140) se queda.

### La UI (pedido explícito de Juan)
- **Ficha (`MorimonchiDetailInfoUITK` + `DetailInfoTabPresenter`)**: desaparece la pestaña Equipo y el bloque CON/ATK/SPD. En su lugar: **las tres necesidades como barras de color** (vida, energía, afecto) con la marca de "apta para bajar" de `CreatureAvailability` (C4), y las **tres partes con su nivel** y su techo (el potencial). Es el mismo lugar donde E2 pondrá el botón de evolucionar.
- **Grilla (`CreatureGridUITK`, `CreatureGridView`)**: fuera las tres ranuras de equipo por tarjeta, las columnas CON/ATK/SPD y el resumen de equipo.
- **Cría (`BreedingBreedTabPresenter` ~166 y ~227)**: fuera la comparación de stats de los padres; queda partes, rol, rasgos y crías restantes.
- Color antes que texto; paleta `--mm-*`.

### Migración (mismo salto v2 → v3 que C4)
`Inventory`: se elimina `EquipmentGrids`. `Registry`: se eliminan de cada criatura los seis stats y `Equipped` (para que el archivo no arrastre basura). Pruebas EditMode de ambos pasos.

### Muta fuera de código (OK de Juan)
UXML/USS de la ficha, la grilla y la cría; assets de equipo borrados; assets de arquetipos y de pricing re-guardados; claves `equipslot.*` y de stats fuera de la tabla `Strings`; refs de `equipmentDatabase` en `GameScene` (`GameManager`, `CreatureGridUITK`, `CreatureGridView`, `MorimonchiDetailInfoUITK`).

### Verificación
Grep limpio de `EquipmentSO`, `EquipmentSlot`, `StatType`, `CreatureStats`, `EquipmentStats`, `BaseConstitution`, `Equipped`; compila con 0 errores; ficha, grilla y panel de cría abren sin excepciones y sin elementos huérfanos (`execute_code` buscando por nombre los elementos borrados); mintear, criar y vender siguen funcionando; el guardado migrado ya no contiene las claves.

---

## 13 · C6 · Reloj de juego y cría local con Minerita (decisión 5 · sesión HC-4)

**Regla:** el tiempo del juego es del juego: un día con bloques, que se guarda y se pausa. Nada de gameplay mira el calendario real. La cría corre en ese reloj y **eclosionar cuesta Minerita**.

### 13.1 Estado de mundo — nuevo tipo de guardado
`SaveKind.World` → `world_state.json` + clave de nube `worldstate`.
```
public class WorldStateData { public int Day = 1; public float MinuteOfDay = 360f; public int TutorialStep = 0; }
```
Dueño: `GameManager` lo carga y lo guarda como los otros (evento `GameEvents.WorldStateChanged`, suscriptor `GameManager`). `TutorialStep` lo usa C7.

### 13.2 El reloj — `Systems/Time/GameClock.cs` (NUEVO, servicio de `GameScene`, ≤ 150 líneas)
```
public static GameClock Instance;
public int   Day { get; }            public float MinuteOfDay { get; }     // 0-1440
public long  TotalMinutes { get; }   // Day * 1440 + MinuteOfDay
public DayBlockDef Block { get; }    public float GameMinutesPerRealSecond { get; }
public bool  Paused { get; set; }
public void  AdvanceToNextBlock();   public void AdvanceToNextDay();      // desarrollo y "volver de la bajada"
```
- `Data/Time/DayScheduleSO.cs` (NUEVO): `RealSecondsPerDay` y lista de bloques `{ NameKey, StartHour, CustomersOpen, ExpeditionOpen }`. **El diseño de los bloques es un dato.** Asset inicial: los 4 bloques de `Index/18` 1.2 (6-9 libre · 9-18 tienda · 18-23 gestión · 23-6 noche con `ExpeditionOpen`).
- Eventos en `GameEvents`: `OnDayStarted(int day)` y `OnDayBlockChanged(DayBlockDef block)`. Suscriptores reales: `InfoOverlayUITK` (reloj arriba a la derecha), `NpcController` (clientes solo con `CustomersOpen`), `StoreManager` (restock por día), `ExpeditionPanelUITK` (bajar solo con `ExpeditionOpen`).
- Se pausa con el menú de pausa (C7). No corre en la arena: al salir se guarda; **al volver de la bajada salta al inicio del día siguiente** (§9.3).
- `DevToolsConsole`: botones "siguiente bloque" y "siguiente día", para que el reloj no estorbe las pruebas.

### 13.3 Lo que deja el tiempo real
| Hoy | Pasa a |
|---|---|
| `CreatureDNA.AgeDays` (días reales desde `BirthDate`) | `int BirthDay` (día de juego) + `AgeDays(int today)`; `BirthDate` y `Timestamp` se quedan (identidad). Migración v3 → v4: `BirthDay = díaActual − edadRealEnDías`, mínimo 1. Llamadores: `BreedingContainer.IsAdult`, `NameTag`, `TransactionPanelUITK`. |
| `ShopCatalogSO`: descuentos por día de la semana y mes, restock por tercio de mes, `GameManager.Now` | `RestockEveryDays` y `DiscountEveryDays` sobre `Day`; fuera `DiscountDay`/`DiscountMonth`/`RestockPeriod`. Llamadores: `StoreManager`, `StorePanelUITK`. |
| `NpcController` con `Time.time` | sigue en segundos, pero solo aparece con `CustomersOpen`. |
| Decaimiento de necesidades por segundo real (`AgentBrain` ~190) | multiplicado por `GameMinutesPerRealSecond` contra valores por hora de juego; a velocidad por defecto da lo mismo que hoy. La calibración es la de §6.7. |
| `SaleDate`, push, `sync_meta` | siguen en tiempo real (no son gameplay). |

### 13.4 Cría local con Minerita
- `Systems/Breeding/AsyncBreedingService.cs` → se reemplaza por **`IncubationService`** (mismo objeto de escena, sin red): `StartBreeding(motherId, fatherId)`, `CancelBreeding(...)`, `TryHatch(motherId, fatherId)`. Valida con `CreatureAvailability.IsFree`, cobra la energía de hoy, marca `BusyReason.Breeding` y fija `BreedReadyAt = GameClock.TotalMinutes + duración`.
- `CreatureDNA.BreedReadyAt` cambia de significado: **minutos de juego** (antes, milisegundos reales). Migración v3 → v4: todo huevo en curso queda listo ya.
- `InheritanceOddsTableSO`: `BreedDurationMinutes` (hoy decorativo) pasa a ser la duración real **en minutos de juego**; campos nuevos `HatchCostBase = 10` y `HatchCostPerPartLevel = 5`, con `int HatchCost(CreatureDNA mother, CreatureDNA father)` = base + extra × niveles de parte por encima de 1 entre los dos padres (§9.2).
- `TryHatch`: exige huevo listo **y** `Wallet.TrySpend(Currency.Minerita, odds.HatchCost(mother, father), "hatch")`; si no alcanza, no eclosiona y el huevo espera (no se pierde). Después, el `HatchLocally` de hoy, que además fija `BirthDay` y `Generation`.
- `BreedingEggsTabPresenter` y `NameTag`: tiempo restante en horas de juego; el botón de eclosionar muestra el costo con el color de la Minerita y se apaga si no alcanza (color antes que texto).
- Se retira Cloud Code de cría: `BreedingContainer`/`BreedingController` llaman al servicio local; `CloudSyncOps.ResetProgressAsync` deja de llamar `cancel-all-breeding`; los cuatro `.js` de cría se marcan "retirados" en `CloudCode/README.md` para que Juan los despublique. El límite "un huevo por jugador" que imponía el servidor pasa a ser **un huevo por corral de cría** (ya es así en el mundo).
- **Candado que hay que evitar:** eclosionar pide Minerita, la Minerita sale solo de bajar y bajar pide criaturas. No se traba mientras el jugador tenga criaturas o dabloons; la red de seguridad final es la caja gratis de §14.2.

### Muta fuera de código (OK de Juan)
`GameScene`: objeto `GameClock`, reloj en el UXML del overlay, refs; asset `DaySchedule`; `InheritanceOddsTable.asset`; `ShopCatalog.asset` (campos de calendario); tabla `Strings`.

### Verificación
Pruebas EditMode: migración v3 → v4, cálculo de bloque por minuto, `AgeDays`. Play: el reloj avanza, cambia de bloque, se guarda y reaparece igual tras reiniciar; clientes solo de día; bajar solo de noche (y por el botón de desarrollo a cualquier hora); ida y vuelta a la arena amanece al día siguiente; cría sin sesión en la nube funciona; huevo listo con 0 de Minerita no eclosiona y con saldo sí, con línea `[Wallet] -10 Minerita (hatch)`; ninguna llamada a `start-breeding`/`hatch-breeding` en consola.

---

## 14 · C7 · Cáscara, arranque y tutorial (decisión 8 · sesión HC-5)

**Regla:** se puede abrir el juego, empezar de cero, ser guiado hasta tener los primeros MoriMonchis, pausar, ajustar y salir, sin el editor.

### 14.1 Flujo de escenas y cáscara
- Escena nueva **`Title`**, primera en la build: Continuar · Nueva partida (con confirmación; usa el reset que ya existe) · Ajustes · Salir.
- `Core/SceneFlow.cs` (NUEVO, estática): único dueño de los nombres de escena y de los saltos (título → tienda; la ida y vuelta a la arena sigue en `ExpeditionHandoff`, que toma los nombres de aquí).
- **Pausa** (Escape sin panel abierto): continuar · ajustes · volver al título. Pausa `GameClock` y `Time.timeScale`; volver al título vuelca y sube antes de cargar.
- **Ajustes** (`PlayerPrefs`, son del dispositivo y no del guardado): volumen general, calidad, pantalla completa, idioma (`Loc`), y la leyenda de controles según el dispositivo activo (hoy está fija en teclado dentro de `InfoOverlayUITK`).
- **Salir:** `FlushToCloudAsync` con tope y `Application.Quit`.
- Si C6 se cortó, el archivo de estado de mundo de §13.1 se crea aquí (solo con `TutorialStep`).

### 14.2 La caja gratis (decisión 8)
- `CreatureBoxListing` gana `FreeWhileNoCreatures` (bool). `StoreManager` calcula precio 0 para esa fila **mientras el registro no tenga ninguna criatura viva**; la tienda la muestra con la etiqueta de oferta "GRATIS · -100 %".
- Cubre dos casos con una sola regla: el jugador nuevo, y el jugador que lo perdió todo (sin criaturas y sin dabloons no hay forma de conseguir Minerita).
- `StoreManager` avisa la compra con `GameEvents.StorePurchased(string listingId)` (suscriptor real: el tutorial). Una compra a precio 0 no pasa por `Wallet`.

### 14.3 Estado inicial — `Data/Player/StarterKitSO.cs` (NUEVO)
Dabloons iniciales, muebles poseídos de arranque y **una distribución mínima ya colocada** (un corral y un comedero) para que las criaturas de la caja tengan dónde vivir. Se aplica una sola vez, cuando `SyncOnStartupAsync` (C2) confirma que no hay guardado local ni datos en la nube.

### 14.4 El tutorial — `Systems/Tutorial/TutorialDirector.cs` + `Data/Tutorial/TutorialStepSO.cs` (NUEVOS)
Un paso = clave de texto de una línea, objetivo en el mundo (opcional) y la señal que lo completa. El director escucha, avanza, guarda `TutorialStep` y dibuja la guía; los pasos son assets.

| # | Paso | Se completa con |
|---|---|---|
| 1 | Moverse y mirar | el jugador se desplaza |
| 2 | Ir a la PC (guía curva en el piso hasta la terminal) | entra en el radio de la PC |
| 3 | Abrir la tienda online | `UIManager` abre `UIPanelType.Store` |
| 4 | Ir a la pestaña MoriMonchis | la pestaña queda activa |
| 5 | Comprar la caja gratis | `GameEvents.OnStorePurchased` con la fila de la caja |
| 6 | Abrir la caja de entrega (guía hasta la caja) | `OnRegistryChanged` con criaturas nuevas |

- La guía usa el dibujante que ya existe (`CuePathDrawer`/`CueDrawer`), a la vara Shapes desde el primer intento (memoria `guias-visuales-vara-shapes`): punteado, puntas redondas, movimiento, curva y transición.
- Una línea de texto por paso como máximo; el resto es guía y color. Saltable desde pausa. Los pasos siguientes (corral, alimentar, primera bajada) son de H6 y se agregan como assets.

### 14.5 Primera build de PC
Por Unity CLI. Lista a destapar: NavMesh en runtime con mallas no legibles (tienda y arena), 257 assets en `Resources`, serialización de Odin en build, orden de escenas (`Title`, `GameScene`, `ArenaSandbox`). Lo que falle se anota en `Index/08`; no se arregla todo aquí.

### Muta fuera de código (OK de Juan)
Escena `Title` y build settings; objetos de pausa, ajustes y tutorial en `GameScene`; UXML/USS nuevos; assets `StarterKit`, pasos del tutorial y la fila de la caja gratis en `ShopCatalog.asset`; tabla `Strings` en/es.

### Verificación
Borrar el guardado local y usar una cuenta sin datos: título → nueva partida → tienda con el kit colocado → el tutorial lleva a la PC, la caja figura gratis, se compra, se abre y nacen los MoriMonchis → `TutorialStep` guardado; reiniciar no repite el tutorial. Con criaturas vivas la caja vuelve a su precio. Pausa congela reloj y criaturas. Salir deja `[CloudSync] Pushed` antes de cerrar. La build de PC arranca y llega a la tienda.
