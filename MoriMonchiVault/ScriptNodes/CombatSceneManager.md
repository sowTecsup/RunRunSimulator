---
tags: [script, ui, combat, scene, replay]
---

# CombatSceneManager.cs

**Ruta:** `Systems/CombatVisualizer/CombatSceneManager.cs`

**Responsabilidad:** Gestor de navegación de escena en la escena de combate. Singleton implícito (el GameObject persiste en la escena). Cablea el botón "Volver" y navega de vuelta a la escena de juego. **S34:** Consume replay requests cross-escena (`CombatReplayRequest`), resolviendo datos con timeout defensivo antes de delegar a `CombatVisualizerService.Play()`.

## Setup

Requiere un `UIDocument` con acceso a `btn-home` (botón volver). En Start resuelve la raíz visual y busca el botón; al clickearlo llama `ReturnToGameScene()`. OnDisable desuscribe para evitar memory leaks.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `ReturnToGameScene()` | Detiene visualizador, carga escena `gameSceneName` |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | UIDocument de la escena de combate |
| `gameSceneName` | `string` | Nombre de la escena de juego principal (default: "GameScene") |

## Cambios S34 — ConsumeReplayRequest Corrutina

Nuevo comportamiento en Start: si `CombatReplayRequest.Pending` es true, inicia corrutina `ConsumeReplayRequest()` que:

1. **Espera GameManager.Instance/Registry** (timeout 3s)
   - GameManager NO persiste entre escenas
   - La escena de combate tiene su propio GameManager que carga de disco
   - Retries cada frame hasta timeout

2. **Resuelve SelfId** via `registry.TryGet(CombatReplayRequest.SelfId)`
   - Timeout 3s, reintentos
   - Falla = logea warning, limpia Pending, retorna

3. **Valida FightIndex**
   - Revisa `self.CombatHistory != null && 0 <= FightIndex < Count`
   - Falla = logea warning, limpia Pending, retorna

4. **Resuelve rival** via `CombatReplayRequest.ResolveOpponent(record, self, registry)`
   - Busca por OpponentDnaId o OpponentName
   - Falla = logea warning, limpia Pending, retorna

5. **Limpia y delega** via `CombatReplayRequest.Clear()` + `CombatVisualizerService.Play(self, opponent, record)`

**Código:**
```csharp
private IEnumerator ConsumeReplayRequest()
{
    float elapsed = 0f;
    while ((GameManager.Instance == null || GameManager.Instance.Registry == null) 
           && elapsed < ReplayResolveTimeout)
    {
        elapsed += Time.deltaTime;
        yield return null;
    }
    if (GameManager.Instance == null || GameManager.Instance.Registry == null)
    {
        Debug.LogWarning("[CombatSceneManager] Timeout esperando GameManager/Registry para el replay.");
        CombatReplayRequest.Clear();
        yield break;
    }

    CreatureDNA self = null;
    elapsed = 0f;
    while (!GameManager.Instance.Registry.TryGet(CombatReplayRequest.SelfId, out self) 
           && elapsed < ReplayResolveTimeout)
    {
        elapsed += Time.deltaTime;
        yield return null;
    }
    if (self == null)
    {
        Debug.LogWarning($"[CombatSceneManager] No se encontró la criatura '{CombatReplayRequest.SelfId}' en el registro para el replay.");
        CombatReplayRequest.Clear();
        yield break;
    }

    int fightIndex = CombatReplayRequest.FightIndex;
    if (self.CombatHistory == null || fightIndex < 0 || fightIndex >= self.CombatHistory.Count)
    {
        Debug.LogWarning("[CombatSceneManager] Índice de pelea inválido para el replay.");
        CombatReplayRequest.Clear();
        yield break;
    }

    var record   = self.CombatHistory[fightIndex];
    var opponent = CombatReplayRequest.ResolveOpponent(record, self, GameManager.Instance.Registry);
    if (opponent == null)
    {
        Debug.LogWarning("[CombatSceneManager] El rival no está en el registro. No se puede reproducir el combate.");
        CombatReplayRequest.Clear();
        yield break;
    }

    CombatReplayRequest.Clear();
    CombatVisualizerService.Instance?.Play(self, opponent, record);
}
```

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatReplayRequest]] — S34, servicio cross-escena de replay
- [[CombatVisualizerService]] — ejecuta el replay una vez resueltos los datos
- [[GameManager]] — acceso a Registry post-carga de escena
- [[CreatureDNA]], [[CombatRecord]] — datos persistidos

## Conexiones

**Entrada:**
- `CombatReplayRequest.Pending` — flag de solicitud cross-escena
- `GameManager.Instance.Registry` — acceso a criatura y rival

**Salida:**
- `CombatVisualizerService.Play()` — delegación tras resolver
- Scene load via `SceneManager.LoadScene()` en `ReturnToGameScene()`

## Notas

- **Timeout defensivo:** 3s esperando GameManager en cada paso, evita hang infinito si escena no carga
- **Null-tolerante:** Todos los checks incluyen fallbacks con logging
- **CombatReplayRequest.Clear():** Se llama al inicio de Play o si falla validación, para limpiar state transitorio
- **Registro dinámico:** CreatureDNA/rival pueden no estar en el nuevo GameManager si fueron vendidos o eliminados; se valida en tiempo real
