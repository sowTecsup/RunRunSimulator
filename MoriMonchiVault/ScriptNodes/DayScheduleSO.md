---
tags: [script, scriptable-object, scheduling]
---

# DayScheduleSO

**Ruta:** `Data/Time/DayScheduleSO.cs`

**Responsabilidad:** ScriptableObject que define el ciclo diario del juego (bloques horarios, escala temporal real-a-juego). Lista de `DayBlockDef` con inicio de hora, flags de qué se habilita (clientes, expediciones). Métodos: `BlockIndexAt(minuteOfDay)` (devuelve índice del bloque actual), `BlockAt(minuteOfDay)` (devuelve `DayBlockDef`). Propiedades: `RealSecondsPerDay` (escala temporal), `GameMinutesPerRealSecond` (inverso). Usado por `GameClock` para determinar bloque actual y disparar eventos.

## DayBlockDef (Serializable)

```csharp
[Serializable]
public class DayBlockDef
{
    public string NameKey;           // Localization key (e.g., "ui.clock.block.shop")
    public int    StartHour;         // Hora de inicio del bloque (0-23)
    public bool   CustomersOpen;     // ¿Clientes abiertos?
    public bool   ExpeditionOpen;    // ¿Expediciones abiertas?
}
```

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `realSecondsPerDay` | float | Segundos reales de wall-clock para 1 día (1440 minutos juego). Default 1440 = tiempo real 1:1 |
| `blocks` | `List<DayBlockDef>` | Tabla de bloques horarios del día (TableList Odin) |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `RealSecondsPerDay` | float | Escala de tiempo (solo lectura) |
| `Blocks` | `IReadOnlyList<DayBlockDef>` | Acceso de solo lectura a bloques |
| `GameMinutesPerRealSecond` | float | Tasa: 1440 / RealSecondsPerDay (escala de tiempo de GameClock) |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `BlockIndexAt(float minuteOfDay)` | `int` | Devuelve índice de bloque actual; busca el bloque con `StartHour` más alto que sea ≤ hora actual |
| `BlockAt(float minuteOfDay)` | `DayBlockDef` | Devuelve bloque actual o null si no hay bloques |

## Algoritmo BlockIndexAt

Busca lineal por startHour descendente. Si `minuteOfDay = 1200` (20:00), busca bloque con StartHour más alto ≤ 20:00. Fallback: último bloque en lista (seguro si está ordenado).

```csharp
public int BlockIndexAt(float minuteOfDay)
{
    float hour = minuteOfDay / 60f;
    int best = -1;
    int bestStartHour = int.MinValue;
    for (int i = 0; i < blocks.Count; i++)
    {
        if (blocks[i].StartHour <= hour && blocks[i].StartHour >= bestStartHour)
        {
            bestStartHour = blocks[i].StartHour;
            best = i;
        }
    }
    return best >= 0 ? best : blocks.Count - 1;
}
```

## Seed Defaults (Odin Button)

`SeedDefaults()` carga configuración estándar (4 bloques):

| Bloque | Hora Inicio | Nombre Key | Clientes | Expedición |
|--------|-------------|-----------|----------|-----------|
| 0 | 6 | `ui.clock.block.free` | No | No |
| 1 | 9 | `ui.clock.block.shop` | Sí | No |
| 2 | 18 | `ui.clock.block.manage` | No | No |
| 3 | 23 | `ui.clock.block.night` | No | Sí |

## Escala Temporal

- **Default:** `realSecondsPerDay = 1440f` → 1 segundo real = 1 minuto juego (sin escalar)
- **Rápido (testing):** `realSecondsPerDay = 60f` → 1 segundo real = 24 minutos juego (24x speed)
- **Fórmula:** `GameMinutesPerRealSecond = 1440 / realSecondsPerDay`

GameClock usa `GameMinutesPerRealSecond` para avanzar estado cada frame:
```csharp
state.MinuteOfDay += Time.deltaTime * GameMinutesPerRealSecond;
```

## Cambios S131

**Introducido S131:** Nuevo SO que centraliza el esquema de bloques (antes distribuido o hardcodeado). Permite tuning gráfico de horarios sin código.

## Vinculado a

- [[GameClock]] — consume BlockIndexAt/BlockAt para determinar bloque actual
- [[GameEvents]] — DayBlockChanged dispara cuando cambia de bloque
- [[Index/09 - Active Context]]

## Conexiones

**Entrada:**
- Asignado en inspector de GameClock (`schedule`)
- Tunable vía Odin TableList

**Salida:**
- `GameClock.Block` lee actual vía `schedule.BlockAt(MinuteOfDay)`
- `GameEvents.DayBlockChanged()` cuando cambia
- Flags `CustomersOpen`/`ExpeditionOpen` controlan disponibilidad de sistemas

## Notas

- **Validación:** BlockAt devuelve null si no hay bloques (safe fallback en GameClock).
- **Overlapping:** Si dos bloques tienen mismo StartHour, el último en lista gana (ambiguo; evitar).
- **Wrapping:** AdvanceToNextBlock() en GameClock maneja wrap to next day si hay overflow.
- **Localization ready:** NameKey es acceso a `Loc` (sistema de strings); no hardcodeado.
