---
tags: [index, expedition, plan, h0]
---

# 26 - Plan H0 · Bajada por pisos (plan de ejecución para el orquestador)

> Hito H0 de [[Index/25 - Hitos hasta el lanzamiento]]. Diseño decidido por Juan en [[Index/22 - Bajada Nocturna y Linaje (Draft)]] Parte 9 (S123). Este documento es el **plan ejecutable**: lo lee el orquestador (Opus) al abrir la sesión, lo reparte a `morimonchi-coder` (Sonnet, uno por lote) y verifica en el editor por Unity MCP. Nada de lo que dice aquí se rediseña sin preguntarle a Juan.

---

## 0 · Antes de empezar (obligatorio)

1. Leer `09 - Active Context` (S123), [[Index/24 - Puente Tienda-Arena]] §3 (arquitectura del puente) y §6d (revisión con los bugs a corregir), [[Index/22 - Bajada Nocturna y Linaje (Draft)]] Parte 9 (reglas de la bajada).
2. Leer los ScriptNodes de: `ExpeditionHandoff`, `ExpeditionBridge`, `ArenaSandbox`, `ArenaRound`, `ArenaPlanPanel`, `ArenaCastPlanner`, `ArenaRoundSummary`, `ArenaMatrixDev`, `ArenaMatrixPlans`, `ArenaOrderRules`, `ExpeditionRulesSO`, `CloudSyncService`, `GameManager`, `InfoOverlayUITK`.
3. Confirmar en el editor que compila con 0 errores antes de tocar nada (`read_console`).
4. Reglas que más se ignoran en sesiones largas: **sin comentarios en código**, **VFX solo por MMFeedbacks** (`Feedbacks/` en el prefab, nunca por código), **≤ 400 líneas por archivo** (`ArenaPlanPanel` está en 373: lo nuevo del panel va en un colaborador, no adentro), **composición, no partials**, **la arena nunca persiste ni toca la nube**.

---

## 1 · Regla del cambio

> **La arena deja de ser una ronda y pasa a ser una run de pisos con la misma semilla base. La ronda es la prueba del piso; un director de run acumula; el puente aplica el total al volver.**

### Reglas de juego (Parte 9, resumidas para el código)

| Regla | Valor |
|---|---|
| Semilla del piso n (n ≥ 1) | `FloorSeed(n) = unchecked((baseSeed * 73856093) ^ (n * 19349663)) & 0x7fffffff` |
| Tipo del piso n | `n % 3 == 0` → `Buff`; si no → `Enemies` |
| Ganar piso de enemigos | `round.Winner == Player` |
| Empatar | `Winner == None` → se sigue, sin bonus, sin perder (decisión pendiente de Juan; hasta que diga otra cosa, así) |
| Perder | `Winner == Rival` → run perdida: material 0, vuelta obligatoria, terna muere si `permadeathEnabled` (false en demos) |
| Energía por piso de enemigos | por criatura propia `−min(40, 20 + 5·TimesKnocked)` |
| Piso de buffo | sin rival; vetas gratis para el jugador; `+30` de energía a cada criatura propia al entrar; termina cuando no queda material o por tiempo |
| Material | suma de `PlayerSecured` por piso; solo cuenta si el jugador se retira |
| Retirarse | disponible entre pisos, nunca durante uno |

---

## 2 · Contratos nuevos (exactos)

### 2.1 `Core/Enums/WorldEnums.cs`
```
public enum ArenaFloorKind { Enemies = 0, Buff = 1 }
```

### 2.2 `Data/Expedition/ArenaRun.cs` (NUEVO, clase pura, sin Unity salvo `Mathf` si hace falta)
```
public class ArenaRun
{
    public int BaseSeed { get; }
    public int Floor { get; private set; }            // piso actual, 1-based; 0 = todavía no bajó
    public int Material { get; private set; }         // acumulado en juego
    public bool Lost { get; private set; }
    public IReadOnlyDictionary<string, int> EnergyById { get; }   // delta neto por UniqueID (negativo = gastó)
    public IReadOnlyList<string> TeamIds { get; }     // UniqueIDs de la terna

    public ArenaRun(int baseSeed, IReadOnlyList<string> teamIds);
    public int FloorSeed(int n);
    public ArenaFloorKind KindOf(int n);
    public int NextFloor => Floor + 1;
    public ArenaFloorKind NextKind => KindOf(NextFloor);

    public void EnterFloor();                                       // Floor++ ; si KindOf(Floor)==Buff suma +30 a cada id
    public void RecordFloor(ExpeditionTeam winner, int playerSecured, IReadOnlyList<ArenaRoundStat> stats);
        // Enemies: Material += playerSecured; por stat con Team==Player e Id en TeamIds: energía −min(40, 20+5·TimesKnocked); winner==Rival → Lost=true, Material=0
        // Buff:    Material += playerSecured; sin costo de energía
    public ExpeditionResult ToResult();
}
```
Constantes en la clase: `EnergyPerFloor 20`, `EnergyPerKnock 5`, `MaxEnergyPerFloor 40`, `BuffEnergy 30`, `BuffEvery 3`.

### 2.3 `Core/ExpeditionHandoff.cs` (MODIFICADO)
`ExpeditionResult` pierde `Stats` y gana:
```
public int Floors;                                    // pisos completados
public bool Lost;
public Dictionary<string, int> EnergyById;            // delta neto por UniqueID
```
`Seed` pasa a ser la semilla base de la run. `ExpeditionReturn` gana `int Floors` y `bool Lost`. Nada más cambia en el handoff.

### 2.4 `Systems/Expedition/ExpeditionBridge.cs` (MODIFICADO)
- `[SerializeField] private bool permadeathEnabled = false;`
- Se borran `energyPerTrip`, `energyPerKnock`, `maxEnergyPerTrip` (viven en `ArenaRun`).
- `ApplyResult`: material = `result.Lost ? 0 : result.PlayerSecured` (donde `PlayerSecured` ya es el acumulado); por cada `(id, delta)` de `EnergyById` con `registry.TryGet` y viva: `dna.Needs.AddEnergy(delta)`; si `result.Lost && permadeathEnabled`: matar por el camino existente de muerte de `CreatureDNA` (el que usa hoy la muerte permanente; buscar en el ScriptNode, no inventar uno); un solo `RegistryChanged` si tocó algo. `ExpeditionReturned` con `Floors`/`Lost`.
- **Arreglo §6d-2:** `Depart(ids)` se vuelve corrutina: `GameManager.Instance.FlushToCloudAsync()` esperada con tope de 5 s (`Task.IsCompleted` o timeout), después cursor y `GoToArena`.

### 2.5 `Core/GameManager.cs` (MODIFICADO, mínimo)
```
public Task FlushToCloudAsync()   // SaveDatabase + SaveSocialGraph + return cloudSync != null ? cloudSync.PushAsync() : Task.CompletedTask
public void FlushToCloud() => _ = FlushToCloudAsync();   // lo siguen usando quit/pausa
```

### 2.6 `Systems/Cloud/CloudSyncService.cs` (MODIFICADO, arreglo §6d-3)
En `Start`, después de `await auth.InitializeAsync()`: si no quedó sesión iniciada (`auth` expone `IsSignedIn`; si no existe, agregarlo en `CloudAuth` como getter del campo `isSignedIn`), `StartupSyncDone = true`.

### 2.7 `World/Expedition/ArenaRunDirector.cs` (NUEVO, MonoBehaviour en la escena `ArenaSandbox`, ≤ 150 líneas)
Dueño único de `ArenaRun`. Solo existe una run si `ExpeditionHandoff.CameFromStore`.
```
[Required, SerializeField] private ArenaSandbox sandbox;
[Required, SerializeField] private ArenaRound round;

public bool Active => run != null;
public ArenaRun Run => run;
public ArenaFloorKind CurrentKind { get; }           // KindOf(run.Floor), Enemies si no hay run
public bool FloorRecorded { get; }                   // la ronda actual ya se contó
public event Action FloorEnded;                       // tras RecordFloor (para que el panel refresque)

void Start()   // si CameFromStore: run = new ArenaRun(sandbox.ActiveSeed, ids de SelectedIds); EnterFloor(); sandbox.SetFloor(run.FloorSeed(1), run.KindOf(1))
void Update()  // si Active && round.IsOver && !FloorRecorded: run.RecordFloor(round.Winner, round.PlayerSecured, round.Summary); FloorRecorded = true; FloorEnded?.Invoke()
public void Continue()   // run.EnterFloor(); sandbox.SetFloor(run.FloorSeed(run.Floor), run.KindOf(run.Floor)); round.Reset(false); FloorRecorded = false
public void Retreat()    // ExpeditionHandoff.ReturnToStore(run.ToResult())
public void GiveUp()     // tras perder: igual que Retreat (el resultado ya trae Lost)
```
Orden de `Start`: el director debe correr **después** del `Start` de `ArenaSandbox` (que hoy hace `BuildRoom` con `randomizeEachPlay`): fijar Script Execution Order o hacer que el director espere un frame (`yield return null`) antes de crear la run. Preferir el frame de espera (sin tocar ProjectSettings).

### 2.8 `World/Expedition/ArenaSandbox.cs` (MODIFICADO)
- `public ArenaFloorKind FloorKind { get; private set; } = ArenaFloorKind.Enemies;`
- `public void SetFloor(int floorSeed, ArenaFloorKind kind)`: `seed = floorSeed; randomizeEachPlay = false; FloorKind = kind; Planner.RivalsEnabled = kind == ArenaFloorKind.Enemies; ResetRoom(false)`. (`ResetRoom(false)` ya hace `ClearCast` + `BuildRoom`.)
- `SpawnExits`: si `FloorKind == Buff`, solo la salida del jugador.
- `public bool AllMaterialTaken` → todos los `minerals` con `Taken` y ningún `Perceivable` de `Kind == Material` suelto en radio 200 (misma consulta que usa `ResetRoom`).
- `Start`: quitar `if (ExpeditionHandoff.CameFromStore) randomizeEachPlay = true;` (la semilla base la pone el director). La sala inicial de la bajada la construye `SetFloor` desde el director; el `BuildRoom` del `Start` queda para el modo dev (sin `CameFromStore`). Para no construir dos salas al bajar: en `Start`, si `CameFromStore`, **no** llamar `BuildRoom` ni `SpawnCast` (el director lo hace en el frame siguiente). El bloque de `TeamLocked` (elegir el elenco por `SelectedIds`) se conserva y corre antes.

### 2.9 `World/Expedition/ArenaCastPlanner.cs` (MODIFICADO)
- `public bool RivalsEnabled = true;` → en `Prepare`, en modo `LocalSave`, si es `false` no se mintean rivales (el bloque de `Roster` no cambia). `HasTeams` no cambia (sigue habiendo salida del jugador).
- **Arreglo §6d-4:** `Clamp(dna, rules, o)` → `Clamp(dna, o)` (borrar el parámetro `rules` aquí y en `ArenaOrderCatalog.PersonalityName`; actualizar los llamadores). La re-siembra `InitState(castSeed)` después de los rivales se deja.

### 2.10 `World/Expedition/ArenaRound.cs` (MODIFICADO)
En `Update`, además del reloj: `if (sandbox.FloorKind == ArenaFloorKind.Buff && sandbox.AllMaterialTaken) End();`. `End` no cambia: en un piso de buffo `RivalSecured` es 0, así que `Winner` = `Player` si aseguró algo y `None` si no.

### 2.11 Panel entre pisos — `World/Expedition/ArenaFloorPanel.cs` (NUEVO colaborador de `ArenaPlanPanel`, ≤ 200 líneas)
`ArenaPlanPanel` no crece: le pasa el `root` y las refs, y este colaborador es dueño de los elementos nuevos del UXML.
```
public ArenaFloorPanel(VisualElement root, ArenaRunDirector director, ArenaSandbox sandbox);
public void Refresh();   // llamado por ArenaPlanPanel.Refresh()
public void Dispose();   // desconecta clicks
```
Elementos (nuevos en `ArenaPlanPanel.uxml`, estilos en `ArenaPlanPanelStyle.uss` con la paleta `--mm-*`):
- `Label plan-floor` (arriba del elenco): "Piso N · Enemigos" / "Piso N · Buffo: +30 energía y vetas gratis"; cuando hay resultado: "Piso N ganado · llevás M" / "Piso N empatado · llevás M" / "Perdiste en el piso N · botín perdido".
- `Button btn-continue` "Seguir → Piso N+1: {tipo}", `Button btn-retreat` "Retirarse (asegura M)", `Button btn-giveup` "Volver a la tienda".
Reglas de visibilidad (solo cuando `director.Active`; si no, todo `display: none` y el panel queda como hoy):
- Antes de jugar el piso actual (`!round.IsOver`): `plan-floor` con el tipo; visibles `btn-play` (texto "¡A LA SALA!" como hoy), `btn-retreat` solo si `run.Floor > 1` (no se puede retirar sin haber jugado el primer piso); ocultos `btn-continue`, `btn-giveup`, `btn-return`, `btn-room`, `btn-palette`, `btn-cast`, `btn-pick`, `btn-shuffle`.
- Con el piso contado (`director.FloorRecorded`) y no perdido: visibles `btn-continue` y `btn-retreat`; oculto `btn-play`.
- Perdido: visible solo `btn-giveup`.
- `btn-continue` → `director.Continue()` y después `ArenaPlanPanel` vuelve al estado "antes de jugar" (el panel ya se refresca por `PlannedCast`/`ActiveSeed` en su `Update`).
- `btn-retreat` → `director.Retreat()`; `btn-giveup` → `director.GiveUp()`.
`ArenaPlanPanel` (MODIFICADO): crea el colaborador en `OnEnable`, lo refresca en `Refresh()`, lo desecha en `OnDisable`; borra `lastResult` y `ReturnToStore` (los reemplaza el director); `btn-return` desaparece del UXML. El `resultPanel` sigue mostrando el marcador del piso como hoy.

### 2.12 `UI/InfoOverlayUITK.cs` (MODIFICADO, mínimo)
Clave nueva `ui.overlay.expedition.lost` ("Perdiste en el piso {0} · sin botín · −{1} energía"); la existente pasa a "Volviste del piso {0} · +{1} material · −{2} energía" (`Floors`, `MaterialGained`, `EnergySpent`). El toast usa `toast--lose` si `Lost`.

### 2.13 Arreglo §6d-1 — `World/Expedition/ArenaMatrixPlans.cs` + `ArenaMatrixDev.cs`
- Borrar las órdenes huérfanas `GuaV` (Small·Fight·Protect) y `SenC` (Big·Flee·Aggressive) y todo equipo que las use; quedan las 6 variantes de `ArenaBases`.
- En `ArenaMatrixDev`, si `RoleFor` falla: `Debug.LogWarning` con el combo y saltar ese equipo (nunca clampear en silencio).

### 2.14 Limpieza §6d-4 — `Data/Expedition/ExpeditionRulesSO.cs`
Borrar `BoldFightLock`, `ShyFleeLock`, `SocialProtectLock`, `LonerAggressiveLock` (y el `[Title("Órdenes")]`). Verificar con grep que no queda lector.

---

## 3 · Lotes y reparto (un `morimonchi-coder` por lote; A, B y E en paralelo; C y D después)

| Lote | Coder | Archivos | Depende de |
|---|---|---|---|
| **E · Arreglos §6d** | E | `GameManager.cs`, `CloudSyncService.cs` (+ `CloudAuth.cs` si falta `IsSignedIn`), `ExpeditionBridge.cs` (solo `Depart` corrutina), `ArenaMatrixPlans.cs`, `ArenaMatrixDev.cs`, `ExpeditionRulesSO.cs`, `ArenaOrderRules.cs`, `ArenaOrderCatalog.cs` + llamadores de `Clamp`/`PersonalityName` | — |
| **A · Run** | A | `WorldEnums.cs`, `ArenaRun.cs` (NUEVO), `ExpeditionHandoff.cs` | — |
| **B · Sala por piso** | B | `ArenaSandbox.cs`, `ArenaCastPlanner.cs` (`RivalsEnabled`), `ArenaRound.cs` | — (usa el enum de A: acordar el nombre, compilar juntos) |
| **C · Director y puente** | C | `ArenaRunDirector.cs` (NUEVO), `ExpeditionBridge.cs` (`ApplyResult`), `InfoOverlayUITK.cs` | A, B |
| **D · Panel entre pisos** | D | `ArenaFloorPanel.cs` (NUEVO), `ArenaPlanPanel.cs`, `ArenaPlanPanel.uxml`, `ArenaPlanPanelStyle.uss` | C |

Si falta tiempo: se corta el piso de buffo (en `ArenaRun.KindOf` devolver siempre `Enemies`; el resto queda igual) — nunca el director ni el panel.

A cada coder se le pasa: la sección 2.x que le toca **copiada entera**, la ruta de cada archivo, y la regla "no toques nada fuera de tu lote; si necesitás un contrato de otro lote, usá el nombre exacto de este documento".

---

## 4 · Lo que muta fuera de código (pedir OK a Juan antes; después hacerlo por MCP)

1. **Escena `ArenaSandbox.unity`**: objeto nuevo `ArenaRunDirector` con el componente y refs `sandbox`/`round`.
2. **`ArenaPlanPanel.uxml`**: `Label plan-floor`, botones `btn-continue`, `btn-retreat`, `btn-giveup`; borrar `btn-return`. **`ArenaPlanPanelStyle.uss`**: clases `.plan-floor`, `.plan-floor--lost`, botones con `--mm-*`.
3. **Tabla `Strings` (en/es)**: `ui.overlay.expedition.return` (texto nuevo con 3 argumentos), `ui.overlay.expedition.lost` (NUEVA). Se agregan por MCP `execute_code` como en S120 (ver `Index/24` §6c). Los textos del panel de arena siguen hardcodeados en español como el resto de la arena (deuda de localización, no se paga aquí).

---

## 5 · Verificación en el editor (paso 8 del protocolo; sin capturas salvo que Juan las pida)

Todo por MCP: `read_console` (0 errores tras compilar), `manage_editor` Play, `execute_code` para sondas. Filtrar la consola por texto (quirk: `Debug.Log` sale como `Exception` en json).

1. **Compila** con 0 errores y 0 warnings nuevos.
2. **Sandbox solo** (Play directo en `ArenaSandbox`, sin `CameFromStore`): todo como antes — LocalSave, 3+3, botón Volver ausente, panel sin elementos de piso, `ArenaRunDirector.Active == false`.
3. **Bajada ganada**: Play en `GameScene` → terminal `PanelUI (2)` → 3 criaturas → arena: `plan-floor` = "Piso 1 · Enemigos", 3 rivales, 2 salidas, `btn-retreat` oculto → ¡A LA SALA! → al terminar: `FloorRecorded`, botones Seguir/Retirarse, `run.Material == PlayerSecured` → Seguir → Piso 2 (semilla `FloorSeed(2)` distinta, mismos 3 propios por `UniqueID`, rivales nuevos) → jugar → Seguir → **Piso 3 · Buffo**: 0 rivales, 1 salida, `EnergyById` con +30 en las 3 → jugar hasta `AllMaterialTaken` (o tiempo) → Retirarse → tienda: material = suma de los 3 pisos, energía por criatura = −c1 −c2 +30 (verificar contra `20+5·tumbadas` de cada piso), un solo `GameManager`, `timeScale` 1, toast con "Volviste del piso 3".
4. **Bajada perdida**: forzar que el rival gane el piso 1 (por sonda: `ExitFor(Rival).Deposit(50)` antes de que termine) → "Perdiste en el piso 1", solo Volver → tienda: material +0, energía cobrada, las 3 vivas (`permadeathEnabled` false), toast `toast--lose`.
5. **Retirarse en el piso 2 antes de jugarlo**: `btn-retreat` visible al entrar al piso 2 → vuelve con el material del piso 1.
6. **Arreglos §6d**: `Depart` con la nube apagada no bloquea más de 5 s; con sesión, el push termina antes de cargar la escena (log de `[CloudSync] Pushed` antes de `[ArenaSandbox] sala=`); sin sesión, el toast al volver aparece sin esperar 20 s; `ArenaMatrixDev` con una semilla y los 6 combos corre sin warnings de `RoleFor`.
7. Ninguna excepción nueva en consola en todo el recorrido.

Medir y anotar en `09 - Active Context`: pisos jugados, material por piso, energía por criatura, duración de un piso de buffo.

---

## 6 · Cierre

- `09 - Active Context`: bloque de sesión con lo tocado, lo medido y la lista de `.cs` NUEVOS/MODIFICADOS.
- `Index/24` §8: marcar los lotes ✅ y anotar desvíos respecto de este plan.
- `Index/25`: si el criterio de H0 se cumplió (incluye que Juan juegue 5 bajadas a 1×), marcar H0 ✅.
- Reportar consumo de tokens (subagentes vs orquestador vs desperdicio).
- `vault-documenter` **solo** si Juan corre `/cerrar-sesion`.

## 7 · Preguntas que no se responden solas (parar y preguntar a Juan)

- Si `CreatureDNA` no tiene un camino único de muerte reutilizable para el permadeath (2.4).
- Si el `Start` del director y el de `ArenaSandbox` no se pueden ordenar sin tocar Script Execution Order (2.7).
- Si el empate debe tratarse distinto de "seguir sin bonus" (sección 1).

---

## 8 · Ajustes de ejecución (S124, al leer el código; mandan sobre §2)

1. **Semilla base en el handoff.** `ExpeditionHandoff.GoToArena` genera `RunSeed` (`Environment.TickCount & 0x7fffffff`); `ArenaRun.FloorSeedOf(baseSeed, n)` es estático. `ArenaSandbox.Start`, si `CameFromStore`, fija `seed = ArenaRun.FloorSeedOf(RunSeed, 1)` y `randomizeEachPlay = false` antes de su `BuildRoom` de siempre: **una sola construcción al llegar** y sin depender del orden de `Start` con el director (el piso 1 siempre es `Enemies`).
2. **`SetFloor` no reconstruye.** `ArenaSandbox.SetFloor(seed, kind)` solo fija `seed`, `randomizeEachPlay = false`, `FloorKind` y `Planner.RivalsEnabled`; `ArenaRunDirector.Continue` llama `SetFloor` y después `round.Reset(false)` (que ya hace `ResetRoom`). `ArenaPlanPanel` **no** llama `round.Reset(false)` tras la ronda cuando `director.Active`.
3. **Estado del panel por el director, no por `round.IsOver`:** perdido → Volver; `FloorRecorded` → Seguir/Retirarse; si no → ¡A LA SALA! (+ Retirarse si `Floor > 1`).
4. **Ids de la terna:** `ArenaRun` guarda los ids conocidos = `TeamIds` ∪ ids de `Team == Player` vistos en los stats (la salida DEV sin selección llega con `TeamIds` vacío).
5. **Fin del buffo:** `AllMaterialTaken` exige además que ninguna criatura propia tenga `Agent.Carried > 0`.
6. **Energía neta:** `ExpeditionReturn.EnergySpent` = −(suma de deltas) y el overlay la muestra con signo (`−N` o `+N`). Claves: `ui.overlay.expedition.return` = "Volviste del piso {0} · +{1} material · {2} energía"; `ui.overlay.expedition.lost` = "Perdiste en el piso {0} · sin botín · {1} energía".
7. **Permadeath** = `dna.IsDead = true` (no hay otro camino de muerte en el código); con el flag apagado no se ejecuta.
8. **Reparto:** la limpieza de `Clamp`/`PersonalityName` pasa al lote B (comparte `ArenaCastPlanner`); C y D corren en paralelo en la segunda tanda.
9. **Después de lanzar:** `ArenaRunDirector` lleva `[DefaultExecutionOrder(-50)]` para que `Active` valga antes del primer `Refresh` del panel.

**Estado S124:** ✅ ejecutado y verificado (detalle en `09 - Active Context`). Pendiente del criterio de H0: 5 bajadas de Juan a 1×.

**Corrección S124 (Juan):** la energía sale de la bajada; se arriesga la vida (`Index/22` 9.2b). Las filas de energía de §1 y §2 quedan reemplazadas por vida: −15 por tumbada, +30 en buffo a vivas, caída pegajosa, gate = vida > 0.
