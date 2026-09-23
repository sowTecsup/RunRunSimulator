---
tags: [script, scriptable-object, world-state]
---

# WorldStateSO

**Ruta:** `Data/World/WorldStateSO.cs`

**Responsabilidad:** ScriptableObject runtime que mantiene el estado del mundo (día actual, minuto del día, paso del tutorial). Propiedades públicas: `Day`, `MinuteOfDay`, `TutorialStep`. Métodos: `GetData()` (devuelve `WorldStateData` para persistencia), `LoadFrom(WorldStateData)` (carga desde persistencia). Se suscribe a `GameEvents.OnWorldStateChanged` en GameManager para disparar persistencia automática vía `SaveSystem.SaveWorldState()`.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `day` | int | Día actual (1-indexed, default 1) |
| `minuteOfDay` | float | Minuto del día actual (0-1440, default 360 = 6 AM) |
| `tutorialStep` | int | Paso actual del tutorial (default 0 = tutorial no iniciado) |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Day` | int | Getter/setter para día |
| `MinuteOfDay` | float | Getter/setter para minuto del día |
| `TutorialStep` | int | Getter/setter para paso del tutorial |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetData()` | `WorldStateData` | Serializa estado actual a struct para persistencia |
| `LoadFrom(WorldStateData data)` | `void` | Carga estado desde persistencia; null-safe (defaults: día=1, minuto=360, tutorial=0) |

## WorldStateData (Serializable)

```csharp
[Serializable]
public class WorldStateData
{
    public int   Day          = 1;
    public float MinuteOfDay  = 360f;
    public int   TutorialStep = 0;
}
```

Struct ligero para transferencia de estado entre runtime (SO) y persistencia (SaveSystem).

## Ciclo de Vida

1. **Awake/Init (GameScene):** Inyectado en GameManager. `LoadWorldState()` cargado vía `SaveSystem.LoadWorldState()` en el bootstrap.
2. **Runtime:** GameClock actualiza `MinuteOfDay` cada frame. Cambios mutuales disparan `GameEvents.WorldStateChanged()`.
3. **Persistencia:** `GameManager.OnEnable()` suscribe `PersistWorldState()` a `OnWorldStateChanged` → dispara `SaveSystem.SaveWorldState()` + push a cloud.
4. **Cloud Reload:** `CloudSyncService` dispara `GameEvents.OnWorldStateReloaded()` cuando se sincroniza desde cloud.

## Vinculado a

- [[Index/09 - Active Context]]
- [[GameManager]] — propietario, inyecta en GameClock
- [[GameClock]] — lee/muta vía `Instance.state` en Update
- [[SaveSystem]] — persistencia (SaveWorldState/LoadWorldState/Serialize/Deserialize)
- [[GameEvents]] — OnWorldStateChanged, OnWorldStateReloaded
- [[CloudSyncService]] — reload desde cloud

## Conexiones

**Entrada:**
- Inyectado vía inspector en GameManager
- `SaveSystem.LoadWorldState()` carga archivo persistente
- `CloudSyncService` reload desde cloud

**Salida:**
- `GameEvents.WorldStateChanged()` cuando Day/MinuteOfDay/TutorialStep mutan
- `GameEvents.WorldStateReloaded()` cuando se sincroniza desde cloud
- Serializado a JSON vía `SaveSystem.SerializeWorldState()`

## Notas (S131 HC-4)

- **Introducido S131:** Nuevo SO para unificar estado de mundo (antes desacoplado).
- **Default MinuteOfDay:** 360 = 6:00 AM (amanecer).
- **TutorialStep:** Reservado para guía progresiva (no usado aún en S131).
- **Persistencia scoped:** SaveSystem respeta user scope (archivo `world_state_[userID].json` si autenticado).
- **Cloud first design:** Estado se sincroniza con cloud; local es caché.
