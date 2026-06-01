---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-01
**Foco**: Tuning del lanzamiento (PlayerController) y reacción post-lanzamiento del MoriMochiAgent.

### Qué se hizo

- **`PlayerController.cs`** — Lanzar ahora respeta el pitch de la cámara: `cameraTransform.forward` directo. `throwAimDistance` eliminado; reemplazado por `throwUpwardBias` (default 0.15, rango 0–1 en Inspector).
- **`MoriMochiAgent.cs`** — Nuevo estado `Recovering`. Flujo post-lanzamiento: física → `Held` → `BeginGetUp()` (warp + desactiva `updateRotation`) → `Recovering` (aturdido `downedDelay` + slerp vertical `getUpDuration`) → `EnterRoaming()`. Dos parámetros nuevos expuestos en Inspector bajo **Recovery (after being thrown)**.

### Próximos pasos inmediatos

- Entrar a Unity y ajustar en Play `throwUpwardBias`, `downedDelay`, `getUpDuration` hasta que el feel sea correcto.
- Setup de escena pendiente (Etapa 2.5): NavMesh bake + 3 Areas, prefab cubo, asset PersonalityProfileTable, wiring del spawner.

## Archivos en juego en la sesión actual

| Archivo | Por qué |
|---------|---------|
| `Scripts/Player/PlayerController.cs` | Dirección de lanzamiento |
| `Scripts/World/MoriMochiAgent.cs` | Estado Recovering + BeginGetUp/TickRecovering |
| `MoriMonchiVault/06 - Player & World.md` | Documentado |

## Cómo usar esta nota en sesiones futuras

Cuando arranque una sesión nueva:
1. Leo este archivo primero (después del `CLAUDE.md`).
2. Borro lo de la sesión pasada y escribo qué estoy haciendo ahora.
3. Listo los 2-4 archivos del vault relevantes para esta sesión (no los leo todos).

Si el `Active Context` queda desactualizado (no se ha tocado en muchos días), tratarlo como **stale** — el código y los archivos del vault son autoritativos sobre cualquier "estado actual" que esta nota describa.

## Notas / pendientes que el usuario quiere recordar

(Vacío. Agregar acá lo que importe entre sesiones pero no merezca un sistema entero.)
