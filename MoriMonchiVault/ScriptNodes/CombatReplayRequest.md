---
tags: [script, combat, visualizer, replay, utility]
---

# CombatReplayRequest

**Ruta:** `Systems/CombatVisualizer/CombatReplayRequest.cs`

**Responsabilidad:** Servicio estático cross-escena para solicitar el replay de un combate 3v3. Almacena transitoriamente el ID del luchador y el índice del combate, coordinando con `CombatSceneManager` para cargar la escena de visualización. **S41:** Firma de `CanReplay()` INVERTIDA — ahora **EXIGE record 3v3** (SelfTeam != null) + valida los 6 IDs resolubles en registry. **ResolveOpponent() BORRADO** (deprecated, equipos resueltos por CombatVisualizerService).

## Descripción General (S37 + S41)

Permite al usuario solicitar el replay de un combate desde cualquier punto (detail panel o historial) sin pasar datos complejos entre escenas. Almacena referencias simples (UniqueID, FightIndex) en propiedades estáticas, carga la escena de combate, y deja que `CombatSceneManager.ConsumeReplayRequest()` resuelva e inicie el replay. **S41:** Bloquea 1v1 legacy (SelfTeam == null); requiere 3v3 (SelfTeam != null).

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `CombatSceneName` | `const string` | Nombre de la escena (`"CombatVisualizerMM"`) |
| `Pending` | `bool` | Si true, hay replay solicitado |
| `SelfId` | `string` | UniqueID del luchador |
| `FightIndex` | `int` | Índice en CombatHistory |

## Métodos Públicos

### `CanReplay(CreatureDNA self, CombatRecord rec, CreatureRegistrySO registry) → bool`

**S41 — INVERTIDO:** Valida si es posible reproducir un combate:
- `self != null` && `rec != null` && `registry != null`
- `rec.Turns != null && rec.Turns.Count > 0`
- **`rec.SelfTeam != null` — EXIGE record 3v3** (S41 cambio)
- **`rec.SelfTeamIds != null && rec.OpponentTeamIds != null`** (S41 nuevo)
- **Todos los 6 IDs (3 self + 3 opponent) resolubles en registry** (S41 nuevo)

```csharp
public static bool CanReplay(CreatureDNA self, CombatRecord rec, CreatureRegistrySO registry)
{
    if (self == null || rec == null || registry == null) return false;
    if (rec.Turns == null || rec.Turns.Count == 0) return false;
    
    // S41: EXIGE 3v3
    if (rec.SelfTeam == null || rec.SelfTeamIds == null || rec.OpponentTeamIds == null) return false;
    
    // S41: valida los 6 IDs
    foreach (var id in rec.SelfTeamIds)
        if (!registry.TryGet(id, out _)) return false;
    foreach (var id in rec.OpponentTeamIds)
        if (!registry.TryGet(id, out _)) return false;
    
    return true;
}
```

**Raciónale S41:** Tras hard-reset no existen records 1v1 legacy. Todos los combates ahora son 3v3. El bloqueo de S37 se INVIERTE: 1v1 legacy son NO replayables, 3v3 nuevos EXIGEN 6 IDs resolubles.

### `Request(CreatureDNA self, CombatRecord rec)`

Solicita un replay:
1. Almacena `self.UniqueID` en `SelfId` y el índice del record en `FightIndex`
2. Marca `Pending = true`
3. Valida que el record esté en `self.CombatHistory`; si no, logea warning, marca `Pending = false`, retorna
4. Llama `GameManager.Instance.FlushForSceneChange()` (flush local + cloud)
5. Carga escena via `SceneManager.LoadScene(CombatSceneName)`

### `Clear()`

Limpia state:
- `Pending = false`
- `SelfId = null`
- `FightIndex = -1`

## Cambios S41

**CanReplay() INVERTIDO:**
```csharp
// S41: bloquea records 1v1 legacy, exige 3v3 + 6 IDs resolubles
if (rec.SelfTeam == null || rec.SelfTeamIds == null || rec.OpponentTeamIds == null)
    return false;

// Valida los 6 IDs
foreach (var id in rec.SelfTeamIds)
    if (!registry.TryGet(id, out _)) return false;
foreach (var id in rec.OpponentTeamIds)
    if (!registry.TryGet(id, out _)) return false;
```

**ResolveOpponent() ELIMINADO:** Deprecated. El equippo oponente se resuelve internamente en `CombatVisualizerService.Play()` vía `record.OpponentTeamIds`.

**Impacto:** Usuarios que tenían records 1v1 legacy no pueden replayarlos. Post-hard-reset no existen, así que no es problema práctico. Todos los nuevos combates son 3v3 y replayables si los 6 IDs están en registry.

## Conexiones

**Entrada:**
- `MorimonchiDetailInfoUITK.BuildCombatCard()` → botón ▶ llama `Request()` si `CanReplay()`
- `CombatPanelUITK.Tabs.ShowHistory()` → botón "▶ Ver replay" llama `Request()` si `CanReplay()`

**Salida:**
- `CombatSceneManager.ConsumeReplayRequest()` — consumidor principal, resuelve self + record, llama `CombatVisualizerService.Play(self, record)`
- `GameManager.FlushForSceneChange()` — persist antes de LoadScene

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatRecord]] — almacena qué replay (S41: 3v3 only)
- [[CombatService]] — simula el combate
- [[CombatSceneManager]] — consume el request
- [[CombatVisualizerService]] — ejecuta replay (S41: 3v3)
- [[GameManager]] — `FlushForSceneChange()`

## Notas

- **State transitorio:** In-memory, no se persiste. Si app cierra durante LoadScene, Pending se pierde.
- **S41 Cambio radical:** De "bloquea 3v3" a "exige 3v3". Records 1v1 legacy YA NO replayables.
- **6 IDs:** self[0..2] + opponent[0..2] deben estar todos en registry. Si uno fue vendido/eliminado, CanReplay retorna false.
- **Sin validación en Request():** `CanReplay()` se llama en la UI antes de Request; Request asume que el record es válido.
