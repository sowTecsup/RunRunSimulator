---
tags: [script, combat, visualizer, replay, utility]
---

# CombatReplayRequest

**Ruta:** `Systems/CombatVisualizer/CombatReplayRequest.cs`

**Responsabilidad:** Servicio estático cross-escena para solicitar el replay de un combate. Almacena transitoriamente el ID del luchador y el índice del combate, coordinando con `CombatSceneManager` para cargar la escena de visualización, esperar a que `GameManager` esté disponible, y delegar a `CombatVisualizerService.Play()`. **S37:** Retorna false en `CanReplay()` para records 3v3 hasta que el visualizador 3v3 esté listo (Fase 4).

## Descripción General (S34 + S37)

Permite al usuario solicitar el replay de un combate desde cualquier punto de la aplicación (detail panel o historial) sin pasar datos de combate complejos entre escenas. En su lugar, almacena referencias simples (UniqueID, FightIndex) en propiedades estáticas, carga la escena de combate, y deja que `CombatSceneManager.ConsumeReplayRequest()` resuelva los datos cuando se inicie. **S37:** Bloquea automáticamente replays 3v3 (SelfTeam != null).

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
3. Retorna null si no resuelve

### `CanReplay(CreatureDNA self, CombatRecord rec, CreatureRegistrySO registry) → bool`

**S37 - Actualizado:** Valida si es posible reproducir un combate:
- `self != null`
- `rec != null`
- `rec.Turns` no nulo y con al menos 1 turno
- El rival es resoluble via `ResolveOpponent()`
- **S37 NUEVO:** `rec.SelfTeam == null` — **retorna false si es record 3v3** (SelfTeam != null = equipo, visualizador 1v1 no soporta)

**Raciónale S37:** Records 1v1 legacy (SelfStats != null, SelfTeam == null) siguen siendo replayables en visualizador 1v1. Records 3v3 nuevos (SelfTeam != null, SelfStats puede ser null) quedan bloqueados hasta que el visualizador 3v3 esté listo (Fase 4).

### `Request(CreatureDNA self, CombatRecord rec)`

Solicita un replay:
1. Almacena `self.UniqueID` en `SelfId` y el índice del record en `FightIndex`
2. Marca `Pending = true`
3. Llama `GameManager.Instance.FlushForSceneChange()` (flush local + cloud)
4. Carga la escena `CombatSceneName` via `SceneManager.LoadScene()`

**Nota:** Si el record no está en `self.CombatHistory` o `CanReplay()` retorna false, logea warning y marca `Pending = false`.

### `Clear()`

Limpia el estado:
- `Pending = false`
- `SelfId = null`
- `FightIndex = -1`

## Cambios S37

**Método `CanReplay()` — Bloqueo de 3v3:**
```csharp
// S37 check: bloquea records 3v3 hasta visualizador 3v3
if (rec.SelfTeam != null)
{
    Debug.LogWarning($"[CombatReplayRequest] Record 3v3 no soportado en visualizador 1v1. Pendiente Fase 4 (visualizador 3v3).");
    return false;
}
```

**Impacto:** Usuarios que completan combates 3v3 no pueden ver el replay desde la UI. Los records se guardan (CombatHistory), pero CanReplay() los bloquea hasta que el visualizador 3v3 sea implementado.

**Timeline:** Fase 4 (futuro) implementará `CombatVisualizerService` para 3v3, entonces se removerá este check.

## Conexiones

**Entrada:**
- `MorimonchiDetailInfoUITK.BuildCombatCard()` → botón ▶ llama `CombatReplayRequest.Request(dna, rec)` si `CanReplay()`
- `CombatPanelUITK.Tabs.ShowHistory()` → botón "▶ Ver replay" llama `CombatReplayRequest.Request()` si `CanReplay()`

**Salida:**
- `CombatSceneManager.ConsumeReplayRequest()` — consumidor principal, resuelve self + opponent + record, llama `CombatVisualizerService.Play()` (1v1 only)
- `GameManager.FlushForSceneChange()` — asegura que el estado se persista antes de cambiar escena

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CombatRecord]] — almacena qué replay se solicita (1v1 legacy vía SelfStats, 3v3 vía SelfTeam)
- [[CombatService]] — simula el combate que se va a reproducir
- [[CombatSceneManager]] — consume el request en Start()
- [[CombatVisualizerService]] — ejecuta el replay (1v1 only)
- [[GameManager]] — `FlushForSceneChange()` persist antes de LoadScene
- [[MorimonchiDetailInfoUITK]] — abre replay desde detail panel
- [[CombatPanelUITK.Tabs]] — abre replay desde historial

## Notas

- **Estado transitorio:** Pensado para sobrevivir el cambio de escena sin serializar a disco. Si el usuario cierra la app durante una LoadScene, el Pending se pierde (recomendable como es).
- **S37 Bloqueo:** Records 3v3 se persisten normalmente pero no pueden ser replayados. No es error, es bloqueo preventivo (visualizador no está listo).
- **Backward compat:** Records 1v1 legacy (SelfStats != null, SelfTeam == null) siguen siendo replayables sin cambios. Solo 3v3 nuevos son bloqueados.
- **Validación en CombatSceneManager:** `ConsumeReplayRequest()` revalida selfId + FightIndex + rival antes de intentar Play, con timeout de 3s esperando a GameManager.
- **Sin persistencia:** A diferencia de CombatRecord, este es puro in-memory, no se guarda en JSON.
