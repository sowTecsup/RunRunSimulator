---
tags: [script, world, expedition, ui]
---

# ArenaClockControl.cs

**Ruta:** `World/Expedition/ArenaClockControl.cs`

**Responsabilidad:** Control de velocidad y pausa en arena. MonoBehaviour que escucha teclado (1-4 para velocidad, Space para pausa) y aplica cambios a `MMTimeManager` y `Time.timeScale`. Velocidades configurables (default 1, 2, 5, 10). Static `Speed` y `Paused` para lectura global.

**Propiedades estáticas:**
- `static float Speed { get; }` — escala actual (default 1f)
- `static bool Paused { get; }` — si tiempo parado

**Métodos públicos:**
- `void CycleSpeed()` — avanza al siguiente índice de velocidad (circular)
- `void TogglePause()` — alterna Paused, aplica Speed o 0
- `void Set(float speed)` — establece velocidad (mínimo 0.1f), despausa

**Internals:**
- `speedIndex` — índice actual en array `speeds[]`
- `OnDisable()` — restaura Speed=1 y Paused=false
- Input checkeado en Update (Keyboard.current)
- Aplica escala a MMTimeManager.NormalTimeScale/CurrentTimeScale/TargetTimeScale y Time.timeScale

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[ArenaRoundHud]]
