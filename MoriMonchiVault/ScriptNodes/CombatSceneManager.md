---
tags: [script, ui, combat, scene, replay, 3v3]
---

# CombatSceneManager.cs

**Ruta:** `Systems/CombatVisualizer/CombatSceneManager.cs`

**Responsabilidad:** Gestor de navegación de la escena de combate (CombatVisualizerMM). Cables el botón "Volver" para regresar al juego. Consume replay requests cross-escena (`CombatReplayRequest`), resolviendo datos con timeout defensivo. **S41:** Firma cambió — `ConsumeReplayRequest()` ya NO resuelve rival. Solo resuelve `self` + obtiene `record`, luego llama `CombatVisualizerService.Play(self, record)` directamente (el service resuelve equipos vía registry).

## Setup

Requiere un `UIDocument` con botón `btn-home`. En Start resuelve la raíz visual, busca botón, suscribe click a `ReturnToGameScene()`. OnDisable desuscribe para evitar memory leaks.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `ReturnToGameScene()` | Detiene visualizador, carga escena `gameSceneName` |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `document` | `UIDocument` | UIDocument de la escena |
| `gameSceneName` | `string` | Nombre escena juego (default: "GameScene") |

## ConsumeReplayRequest Corrutina (S41 SIMPLIFICADA)

Iniciada en Start si `CombatReplayRequest.Pending` es true. Workflow:

1. **Espera GameManager.Instance/Registry** (timeout 3s)
   - GameManager NO persiste entre escenas
   - La escena carga su propio GameManager desde disco
   - Reintentos cada frame hasta timeout

2. **Resuelve SelfId** via `registry.TryGet(CombatReplayRequest.SelfId)`
   - Timeout 3s, reintentos
   - Falla = logea warning, limpia Pending, retorna

3. **Valida FightIndex**
   - Revisa `self.CombatHistory != null && 0 <= FightIndex < Count`
   - Falla = logea warning, limpia Pending, retorna

4. **Obtiene record**
   - `var record = self.CombatHistory[fightIndex]`

5. **Limpia y delega** via `CombatReplayRequest.Clear()` + `CombatVisualizerService.Play(self, record)` — **S41 NUEVO SIGNATURE**

**Código S41:**
```csharp
private IEnumerator ConsumeReplayRequest()
{
    // Espera GameManager + Registry
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

    // Resuelve self
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
        Debug.LogWarning($"[CombatSceneManager] No encontré '{CombatReplayRequest.SelfId}'.");
        CombatReplayRequest.Clear();
        yield break;
    }

    // Valida FightIndex
    int fightIndex = CombatReplayRequest.FightIndex;
    if (self.CombatHistory == null || fightIndex < 0 || fightIndex >= self.CombatHistory.Count)
    {
        Debug.LogWarning("[CombatSceneManager] Índice de pelea inválido.");
        CombatReplayRequest.Clear();
        yield break;
    }

    // S41: obtiene record y delega directo a Play(self, record)
    var record = self.CombatHistory[fightIndex];
    CombatReplayRequest.Clear();
    CombatVisualizerService.Instance?.Play(self, record);  // S41 NEW: firma cambió
}
```

## Cambios S41

**Firma ConsumeReplayRequest():**
- S37: `Play(self, opponent, record)` — resolvía rival localmente
- **S41: `Play(self, record)` — rival resuelto en CombatVisualizerService** (simplificación, el service ya necesita resolver equipos vía registry para 3v3)

**Lógica eliminada:** Línea que resolvía rival vía `CombatReplayRequest.ResolveOpponent()`. Ahora es solo: obtener self + record + delegar.

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatReplayRequest]] — servicio cross-escena (S41: firma Play cambió)
- [[CombatVisualizerService]] — ejecuta replay (S41: firma Play nueva)
- [[GameManager]] — acceso Registry post-carga
- [[CreatureDNA]], [[CombatRecord]] — datos persistidos

## Conexiones

**Entrada:**
- `CombatReplayRequest.Pending` — flag solicitud cross-escena
- `GameManager.Instance.Registry` — acceso a self + record

**Salida:**
- `CombatVisualizerService.Play(self, record)` — delegación directo (S41)
- Scene load via `SceneManager.LoadScene()` en `ReturnToGameScene()`

## Notas

- **Timeout defensivo:** 3s esperando GameManager en cada step, evita hang infinito
- **Null-tolerante:** Todos los checks incluyen fallbacks con logging
- **CombatReplayRequest.Clear():** Se llama antes de Play para limpiar state transitorio
- **S41 Simplificación:** Menos resoluciones (solo self), el visualizer se encarga de equipos
