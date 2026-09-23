---
tags: [script, singleton, time-system]
---

# GameClock

**Ruta:** `Systems/Time/GameClock.cs`

**Responsabilidad:** Singleton reloj del juego. Actualiza `WorldStateSO` cada frame según escala temporal de `DayScheduleSO`. Propiedades: `Day`, `MinuteOfDay`, `TotalMinutes`, `Block`, `GameMinutesPerRealSecond`, `Paused`, `Loaded`. Dispara eventos: `GameEvents.DayStarted(day)` (cambio de día), `GameEvents.DayBlockChanged(block)` (cambio de bloque), `GameEvents.WorldStateChanged(state)` (mutación general). Métodos públicos: `AdvanceToNextBlock()`, `AdvanceToNextDay()` (para dev/testing).

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `schedule` | `DayScheduleSO` | SO de bloques horarios (requerido, AssetsOnly) |
| `state` | `WorldStateSO` | Referencia a estado del mundo (resuelto en Awake vía GameManager) |
| `blockIndex` | int | Índice actual del bloque para detección de cambio |
| `loaded` | bool | Flag: estado cargado desde persistencia (se fija en HandleWorldStateReloaded) |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Instance` | `static GameClock` | Singleton |
| `Day` | int | Día actual (null-safe: default 1) |
| `MinuteOfDay` | float | Minuto actual (null-safe: default 0) |
| `TotalMinutes` | long | Minuto absoluto (Day * 1440 + MinuteOfDay) para comparaciones BreedReadyAt |
| `Block` | `DayBlockDef` | Bloque horario actual (null si schedule ausente) |
| `GameMinutesPerRealSecond` | float | Escala temporal desde `schedule.GameMinutesPerRealSecond` |
| `NeedsTimeScale` | float | Escala aplicada a consumo de necesidades (0 si no loaded o Paused, GameMinutesPerRealSecond si ok) |
| `Paused` | bool | Flag de pausa (usado por UI para detener tiempo sin recargar) |
| `Loaded` | bool | ¿Estado cargado? (readonly) |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `AdvanceToNextBlock()` | `void` | Salta a bloque siguiente (circular); dispara eventos |
| `AdvanceToNextDay()` | `void` | Salta a amanecer del próximo día si ya pasó amanecer; si no pasó, saltapróximo amanecer |

## Ciclo de Vida

1. **Awake:** Crea singleton, resuelve `state` desde `GameManager.Instance.WorldState`.
2. **OnEnable:** Suscribe a `GameEvents.OnWorldStateReloaded` (cloud reload) y `GameEvents.OnExpeditionReturned` (salto a próximo día tras expedición).
3. **Update:** Si cargado y no pausado:
   - Avanza `state.MinuteOfDay += Time.deltaTime * GameMinutesPerRealSecond`
   - Si sobrepasa 1440, incrementa día y reciclza minuto
   - Detecta cambio de bloque; dispara `DayBlockChanged` + `WorldStateChanged`
   - Debug log: `[GameClock] Dia {day} · bloque {blockNameKey}`
4. **OnDisable:** Desuscribe eventos.
5. **OnDestroy:** Limpia singleton.

## TotalMinutes

Usado por `IncubationService` para calcular `BreedReadyAt`:
```csharp
public long TotalMinutes => Day * 1440L + (long)MinuteOfDay;
```

**Ejemplo:** Day 3, MinuteOfDay 600 → TotalMinutes = 3*1440 + 600 = 4920. Incubación: BreedReadyAt = 4920 + 30 (minutos) = 4950. Cuando TotalMinutes >= 4950, huevo listo.

## Event Handlers

**HandleWorldStateReloaded(WorldStateSO reloaded)**
- Cargado desde cloud. Actualiza `state = reloaded`, fija `blockIndex`, `loaded = true`, dispara `DayBlockChanged`.

**HandleExpeditionReturned(ExpeditionReturn r)**
- Expedición terminada. Llama `AdvanceToNextDay()` (salta a amanecer).

## Métodos Privados

**Update flow:**
```
if not loaded or Paused or state/schedule null → skip
state.MinuteOfDay += Time.deltaTime * GameMinutesPerRealSecond
if state.MinuteOfDay >= 1440:
    state.Day++, state.MinuteOfDay -= 1440
    DayStarted(state.Day)
newIndex = schedule.BlockIndexAt(state.MinuteOfDay)
if newIndex != blockIndex:
    blockIndex = newIndex
    Log bloque
    DayBlockChanged(Block)
    WorldStateChanged(state)
```

## Cambios S131

**Introducido S131:** Nuevo sistema de reloj unificado. Antes el tiempo era estático o simulado manualmente. Ahora:
- Actualización fluida en Update basada en `GameMinutesPerRealSecond`
- Integración con `WorldStateSO` y `DayScheduleSO`
- Eventos centralizados para reacciones de sistemas
- Cloud reload en HandleWorldStateReloaded

## Vinculado a

- [[Index/09 - Active Context]]
- [[WorldStateSO]] — estado que actualiza
- [[DayScheduleSO]] — bloques horarios
- [[GameManager]] — proporciona WorldStateSO en Awake
- [[GameEvents]] — dispara DayStarted, DayBlockChanged, WorldStateChanged
- [[IncubationService]] — lee TotalMinutes para BreedReadyAt
- [[NeedsState]] — usa NeedsTimeScale para degradación
- [[ExpeditionService]] — invoca AdvanceToNextDay() al retornar

## Conexiones

**Entrada:**
- Inyectado schedule vía inspector
- `GameManager.Instance.WorldState` resuelto en Awake
- Cloud reload via `GameEvents.OnWorldStateReloaded`

**Salida:**
- `GameEvents.DayStarted(day)` cuando cambia día
- `GameEvents.DayBlockChanged(block)` cuando cambia bloque
- `GameEvents.WorldStateChanged(state)` cuando muta estado
- `state.Day` y `state.MinuteOfDay` actualizados cada frame

## Notas (S131 HC-4)

- **Paused:** Útil para UI modales (pausa sin salir de escena). NeedsTimeScale devuelve 0.
- **Loaded flag:** Protege contra actualización antes de que cloud cargue. HandleWorldStateReloaded fija en true.
- **TotalMinutes:** Long para evitar overflow (puede ser millones de minutos).
- **Block null-safe:** Si schedule ausente, Block = null, GameMinutesPerRealSecond = 1f.
- **Amanecer:** AdvanceToNextDay() usa primer bloque StartHour como amanecer (default 6 AM vía DayScheduleSO.SeedDefaults).
