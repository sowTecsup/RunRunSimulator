---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-02
**Foco**: Consolidación de docs permanentes antes de la siguiente etapa — **Furniture** → [[10 - Furniture & Building]] y **trabajo 3D de MoriMonchis** → [[06 - Player & World]]. Próximo: arrancar Furniture Fase 2 (Building mode). Sesión previa (2026-06-01): pulido 3D de MoriMonchis + Furniture Fase 1.

### Qué se hizo

**MoriMonchis (3D: rebote/knock/throw/tint/recovery)** → ✅ consolidado en [[06 - Player & World]]
- Hecho: rebote tipo peluche + knock en cadena (`IThrowable.Knock`) + settle robusto (`IsGrounded`+`maxThrownTime`) + levantarse escalado por `RecoverySpeed`, todo 100% por código (sin PhysicMaterials). Throw que converge a la mira (`throwAimDistance` 30 m). Tint por personalidad (`MaterialPropertyBlock`). `ConfineToArea` → `AreaPreference`. Feel-ready (5 `UnityEvent`).
- **Detalle completo (mecánicas, tunables, estructura del prefab, mapeo de personalidades) → [[06 - Player & World]]** (doc permanente; ya no se mantiene acá).

**Furniture — Fase 1 (data), Etapa 3.1** → ✅ consolidado en [[10 - Furniture & Building]]
- Hecho: 4 archivos de data (`FurnitureDefinitionSO`/`FurnitureDatabaseSO`/`PlacedFurniture`/`FurnitureRegistrySO`) + `PlacementGrid` + `FurnitureSpawner` + `FurnitureService` (`TryPlace`/`TryRemove` + botones Odin de test). `Enums.cs` (+`Building`, +`FurnitureCategory`) y `GameEvents.cs` (+`OnFurnitureChanged`/`OnFurnitureReloaded`).
- **Detalle completo, contratos, setup en Unity y plan de Fase 2/3 → [[10 - Furniture & Building]]** (doc permanente; ya no se mantiene acá).

## Próximos pasos (retomar acá la próxima sesión)

**Furniture — siguiente: Fase 2 (Building mode)** → plan completo en [[10 - Furniture & Building]]
- Resumen: action map `Building` + `PlayerStateType.Building`, ghost preview (verde/rojo según `grid.CanPlace`), flujo click→F→Esc y borrado con click derecho, todo sobre `TryPlace`/`TryRemove`. Después: Fase 3 (economía/tienda) y persistencia (JSON+cloud). Detalle, contratos e invariantes en el doc permanente.

**MoriMonchis**
- Acordarse de pulsar **Populate Defaults** en `PersonalityProfileTable` (los campos `ConfineToArea` viejos en el .asset se reemplazan por `AreaPreference`).
- Setup de escena Etapa 2.5 sigue pendiente (NavMesh bake + 3 Areas + prefab + wiring spawner).
- Probar en Unity y ajustar números de rebote/knock/throw.

## Archivos en juego en la sesión actual

| Archivo | Por qué |
|---------|---------|
| `Scripts/World/MoriMochiAgent.cs` | Rebote, knock, settle, tint, preferencia, gizmos, Feel hooks |
| `Scripts/Player/PlayerController.cs` | Throw hacia la mira |
| `Scripts/Core/Interfaces.cs` · `Interactables/ThrowableObject.cs` | `IThrowable.Knock` |
| `Scripts/Data/PersonalityProfileSO.cs` | AreaPreference, RecoverySpeed, Tint |
| `Scripts/World/MoriMochiSpawner.cs` | Spawn sesgado a área preferida |
| `Scripts/Data/Furniture*.cs` · `Systems/Furniture/*.cs` | Sistema de muebles Fase 1 |
| `Scripts/Core/Enums.cs` · `GameEvents.cs` | Building state, FurnitureCategory, eventos furniture |

## Cómo usar esta nota en sesiones futuras

Cuando arranque una sesión nueva:
1. Leo este archivo primero (después del `CLAUDE.md`).
2. Borro lo de la sesión pasada y escribo qué estoy haciendo ahora.
3. Listo los 2-4 archivos del vault relevantes para esta sesión (no los leo todos).

Si el `Active Context` queda desactualizado (no se ha tocado en muchos días), tratarlo como **stale** — el código y los archivos del vault son autoritativos.

## Notas / pendientes que el usuario quiere recordar

- Furniture: retomar en **Fase 2 (Building mode)** — plan e implementación consolidados en [[10 - Furniture & Building]].
