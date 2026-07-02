---
tags: [script, combat, visualizer, replay, utility]
---

# CombatReplayRequest

**Ruta:** `Systems/CombatVisualizer/CombatReplayRequest.cs`

**Responsabilidad:** Servicio estático cross-escena para solicitar el replay de un combate. Almacena transitoriamente el ID del luchador y el índice del combate, coordinando con `CombatSceneManager` para cargar la escena de visualización, esperar a que `GameManager` esté disponible, y delegar a `CombatVisualizerService.Play()`.

## Descripción General (S34)

Permite al usuario solicitar el replay de un combate desde cualquier punto de la aplicación (detail panel o historial) sin pasar datos de combate complejos entre escenas. En su lugar, almacena referencias simples (UniqueID, FightIndex) en propiedades estáticas, carga la escena de combate, y deja que `CombatSceneManager.ConsumeReplayRequest()` resuelva los datos cuando se inicie.

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `CombatSceneName` | `const string` | Nombre de la escena de visualización de combates (`"CombatVisualizerMM"`) |
| `Pending` | `bool` | Si true, hay un replay solicitado listo para consumir |
| `SelfId` | `string` | UniqueID del luchador cuyo combate se va a reproducir |
| `FightIndex` | `int` | Índice en el `CombatHistory` del combate a reproducir |

## Métodos Públicos

### `ResolveOpponent(CombatRecord rec, CreatureDNA self, CreatureRegistrySO registry) → CreatureDNA`

Busca el rival en el registro:
1. Primero intenta por `OpponentDnaId`
2. Si no encuentra, busca por `OpponentName`
3Retorna null si no resuelve

### `CanReplay(CreatureDNA self, CombatRecord rec, CreatureRegistrySO registry) → bool`

Valida si es posible reproducir un combate:
- `self != null`
- `rec != null`
- `rec.Turns` no nulo y con al menos 1 turno
- El rival es resoluble via `ResolveOpponent()`

### `Request(CreatureDNA self, CombatRecord rec)`

Solicita un replay:
1. Almacena `self.UniqueID` en `SelfId` y el índice del record en `FightIndex`
2. Marca `Pending = true`
3. Llama `GameManager.Instance.FlushForSceneChange()` (flush local + cloud)
4. Carga la escena `CombatSceneName` via `SceneManager.LoadScene()`

**Nota:** Si el record no está en `self.CombatHistory`, logea warning y marca `Pending = false`.

### `Clear()`

Limpia el estado:
- `Pending = false`
- `SelfId = null`
- `FightIndex = -1`

## Conexiones

**Entrada:**
- `MorimonchiDetailInfoUITK.BuildCombatCard()` → boton ▶ llama `CombatReplayRequest.Request(dna, rec)` si `CanReplay()`
- `CombatPanelUITK.Tabs.ShowHistory()` → boton "▶ Ver replay" llama `CombatReplayRequest.Request()`

**Salida:**
- `CombatSceneManager.ConsumeReplayRequest()` → consumidor principal, resuelve self + opponent + record, llama `CombatVisualizerService.Play()`
- `GameManager.FlushForSceneChange()` → asegura que el estado se persista antes de cambiar escena

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatRecord]] — almacena qué replay se solicita
- [[CombatService]] — simula el combate que se va a reproducir
- [[CombatSceneManager]] — consume el request en Start()
- [[CombatVisualizerService]] — ejecuta el replay
- [[GameManager]] — `FlushForSceneChange()` persist antes de LoadScene
- [[MorimonchiDetailInfoUITK]] — abre replay desde detail panel
- [[CombatPanelUITK.Tabs]] — abre replay desde historial

## Notas

- **Estado transitorio:** Pensado para sobrevivir el cambio de escena sin serializar a disco. Si el usuario cierra la app durante una LoadScene, el Pending se pierde (recomendable como es).
- **Validación en CombatSceneManager:** `ConsumeReplayRequest()` revalidateselfId + FightIndex + rival antes de intentar Play, con timeout de 3s esperando a GameManager.
- **Sin persistencia:** A diferencia de CombatRecord, este es puro in-memory, no se guarda en JSON.
