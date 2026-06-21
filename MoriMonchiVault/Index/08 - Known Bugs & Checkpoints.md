---
tags: [index, core]
---

# 08 - Known Bugs & Checkpoints

**Bugs Activos:**
| Bug | Causa | Estado |
|-----|-------|--------|
| Tilting al cargar escena | N rebakes NavMesh solapados | Mitigado (inaceptable en produccion) |
| DeathChance hardcoded en JS (15%) | Solo afecta combate local | Sin sincronizar |
| BREED_DURATION_MS hardcoded en JS (30min) | Solo afecta display local | Sin sincronizar |
| Matchmaking pool sin race-condition handling | Dos llamadas simultaneas se pisan | Aceptable testing |
| Auto-repeticion de input en grilla | Navega 1 paso por pulsacion | Menor |

**Checkpoints Diseno (Breeding Async):**
- Busy-lock server-enforced (hoy lo escribe el cliente)
- Generacion de la cria server-side (hoy local + push)
- Cross-device countdown (BreedReadyAt no viaja entre dispositivos)
- Crash entre hatch-ready y crear la cria (riesgo bajo)

**Pendientes Codigo:**
- Countdown en Resultados: detectar instant_pool vs timer
- Ordenar cola Resultados por QueuedAt ascendente
- Arbol de ascendencia/descendencia renderizado mejorado
- Prewarm de pool MoriMochiSpawner (prewarmCount en Awake)

**Bugs Resueltos (referencia):**
| Bug | Fix |
|-----|-----|
| Stutter pop ragdoll a agente | Lerp posicion+rotacion, Warp diferido al final |
| Criatura atascada en Reacting con needs criticos | TryEnterNeedSeeking al inicio de TickReacting |
| E-key grab roto tras collider trigger | RaycastAll + QueryTriggerInteraction.Collide |
| NameTag no actualizaba tras reuso pool | ResolveElements compara docRoot con root actual |
| Criatura aparecia/desaparecia primer spawn | OnRegistryReloaded ya no llama ClearAll |
| Sistema prioridad UI | Stack LIFO + action maps exclusivos |
| Gap async battle log | AsyncCombatService.ApplyResult dispara OnCombatLogged |
| Muebles elevados al recargar | Coroutine ClearAll yield return null Sync |
| Throw sin IThrowable en WorldPropInstance | Fallback Rigidbody.linearVelocity directo |
| StorageContainer re-captura inmediata | justEjectedId salta 2 FixedUpdate |
