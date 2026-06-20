---
tags: [memory-bank, tech-debt, refactor, architecture]
---

# 11 - Technical Debt & Refactor Roadmap

> Auditoría 2026-06-19 (Sesión de mantenimiento). 97 scripts, ~15.000 líneas.
> Esta nota es la hoja de ruta viva de saneamiento. Las fases están priorizadas por (impacto × leverage ÷ riesgo).

---

## 🗺️ Mapa por secciones (capas de la arquitectura)

| Capa | Carpetas | Responsabilidad | Salud |
|------|----------|-----------------|-------|
| **Data (estado puro)** | `Data/` (24), `Data/Parts`, `Data/Databases` | DNA, partes, SOs, registros. Sin orquestación. | 🟢 Sana |
| **Core (servicios + bus)** | `Core/` (7) | GameManager, GameEvents, SaveSystem, Enums, Interfaces, ColorGenetics | 🟡 GameManager monolítico |
| **Systems (orquestación)** | `Systems/Combat`, `Breeding`, `Cloud`, `Furniture`, `Store` | Lógica de dominio, red, dueños de persistencia | 🟡 Controllers mezclan debug/UI |
| **World (representación 3D)** | `World/` (17) | AI, spawn, contenedores, needs, nametags | 🔴 MoriMochiAgent monstruoso |
| **UI (representación 2D)** | `UI/` (14) | Paneles UITK | 🔴 Paneles gigantes |
| **Player / Input** | `Player/` (4) | FP controller, action maps | 🟢 Sana |
| **Interactables** | `Interactables/` (2) | IInteractable triggers | 🟢 Sana |

### Hotspots medidos (archivos > 450 líneas = parten 2+ dominios)
| Líneas | Archivo | Dominios mezclados |
|--------|---------|--------------------|
| **1189** | `World/MoriMochiAgent.cs` | FSM + ragdoll/física + NavMesh-rebake + needs + reacción-jugador + confinamiento/cortejo + carry/throw |
| **849** | `UI/CombatPanelUITK.cs` | datos + binding + animación |
| **637** | `UI/BreedingPanelUITK.cs` | datos + binding + selección |
| **622** | `World/MoriMochiSpawner.cs` | pool + colas + colocación en corral |
| **568** | `Systems/Cloud/CloudSyncService.cs` | pull/push/reset + reconciliación |
| **478** | `UI/MorimonchiDetailInfoUITK.cs` | datos + binding |
| **468** | `World/BreedingContainer.cs` | corral + cortejo + nacimiento + server |
| **462** | `Systems/Combat/AsyncCombatService.cs` | endpoints + reconciliación |

---

## 🧭 Regla de arquitectura general (la regla de oro técnica)

> **Una responsabilidad por archivo, una dirección de comunicación, un dueño por dato.**

1. **Capas, sin saltos de dos niveles**: `Data` (estado) → `Systems/Core` (orquestación, dueños de persistencia y red) → `World/UI` (representación). La representación LEE estado y reacciona a eventos; **nunca** persiste ni toca la nube directamente.
2. **Comunicación cruzada solo por bus o servicio explícito**: `GameEvents` (gameplay), eventos `static` de `UIManager` (UI), eventos de Inputs. Un consumidor **nunca** hace `Find*`/`GetComponentInParent` para localizar otro sistema. El evento transporta la data.
3. **Límite de tamaño/dominio**: si un archivo supera ~400 líneas **o** mezcla 2+ dominios (datos, presentación, física, red), se parte en colaboradores con una sola responsabilidad cada uno.
4. **Singleton = servicio runtime; SO = data**. Un servicio de runtime puede ser singleton (`GameManager.Instance`). Un ScriptableObject expone su instancia activa **de una sola forma elegida** (ver Fase 4). No mezclar ambos criterios.

Esta regla resume y subordina las 10 reglas de código de `CLAUDE.md`; cuando una decisión no esté cubierta por las 10, se aplica ésta.

---

## 🛠️ Hoja de ruta (fases)

### Fase 0 — Higiene barata 🟢 riesgo bajo
- [x] ~~Eliminar FirstPerson/ThirdPersonController~~ (ya no existen — nota previa estaba stale).
- [x] **Auditoría de código muerto (2026-06-19): SIN código muerto.** `CombatManagerSO` está vivo (GameManager, AsyncCombatService, CombatController, CombatService, CombatPanelUITK). `CloudCodeTester` es herramienta dev legítima (botones Odin, 0 refs porque se invoca desde el Inspector). No hay nada que borrar.
- [x] **Regla de arquitectura general codificada en `CLAUDE.md`** (sección propia antes de las 10 reglas).
- [ ] **Namespacing**: 96/97 scripts sin namespace. **Single assembly (sin `.asmdef`)** → es un cambio ATÓMICO: namespar parcialmente rompe la visibilidad global. Hacerlo en una pasada con Unity abierto para compilar y cazar `using` faltantes. Payoff modesto; evaluar si vale la churn vs. saltar a F1.
- [ ] Estandarizar `#region` (hoy mezclado con bloques sueltos).
- [ ] **Organizar `Scripts/`**: alinear las subcarpetas de código con las capas de la regla (Data/Systems/World/UI/Core). Mover scripts huérfanos a su capa; `World/` (17) mezcla AI + contenedores + props → considerar sub-carpetas (`World/AI`, `World/Containers`, `World/Props`).
- [ ] **Organizar SO**: en código, `Data/` (24) mezcla DNA, databases, SOs de config y tablas → reflejar las subcarpetas que ya existen en los assets. En disco, `ScriptableObjects/` ya tiene `Databases`/`FurnitureSystem`/`Parts/{Mouth,Eye,Body,Arms}`/`Breeding`/`ItemSystem`; auditar SO sueltos y darles su subcategoría (Genetics, Config, Tables) de forma consistente código↔assets.

### Fase 1 — Partir `MoriMochiAgent` (1189) ✅ HECHO (2026-06-19)
**Corrección de enfoque vs. plan original:** extraer MonoBehaviours colaboradores habría EMPEORADO el acoplamiento — el FSM comparte un núcleo mutable único (`state`, `NavMeshAgent`, `Rigidbody`, timers) que todos los dominios leen/escriben; separarlos exigiría exponer ese estado como público. **Se usó `partial class`**: un solo componente (misma serialización, mismo prefab, cero estado público nuevo), código repartido por concern. Quirk de arquitectura: para un FSM cohesivo, "partir en colaboradores" = `partial class`, no componentes separados.
- `MoriMochiAgent.cs` (~243) — núcleo: campos, lifecycle, dispatch, helpers NavMesh compartidos, gizmos.
- `MoriMochiAgent.Tuning.cs` (~201) — todos los `[SerializeField]` Odin + readouts + dev buttons.
- `MoriMochiAgent.Brain.cs` (~358) — estados + needs + reacciones + intent + queries.
- `MoriMochiAgent.Physics.cs` (~274) — colisión/knock/throw/ragdoll/recovery/handoff.
- `MoriMochiAgent.Confinement.cs` (~136) — pen + courtship + supervivencia a rebake + pooling.
- Verificación: diff de contenido contra el original = exacto (857 líneas idénticas), llaves balanceadas por archivo, sin cambio de comportamiento ni de API pública.

### Fase 2 — Partir paneles UI gigantes ✅ HECHO (2026-06-19)
Mismo veredicto que F1: los paneles comparten estado UI mutable (refs de elementos, listas de cards, índices, `region`) → `partial class`, no componentes. Cortados por concern contiguo:
- `CombatPanelUITK` (849) → `.cs` (241, núcleo/lifecycle/wiring/data) + `.Tabs.cs` (390, contenido de 4 pestañas) + `.Navigation.cs` (234, IUINavigable+foco). Verificado exacto (563 líneas), llaves balanceadas.
- `BreedingPanelUITK` (637) → `.cs` (170) + `.Content.cs` (273, candidatos/huevos/preview/breed/hatch) + `.Navigation.cs` (210). Verificado exacto (418 líneas), llaves balanceadas.
- Patrón establecido para UITK: **Core (lifecycle+wiring+data) / Content (build+bind) / Navigation (IUINavigable+foco)**.
- `MorimonchiDetailInfoUITK` (478) → `.cs` (289, núcleo+Info+Combat) + `.Trees.cs` (196, tabs Linaje/Descendencia). Verificado exacto (327 líneas). **Todos los paneles UI >400 líneas partidos.**

### Fase 2.5 — Otros hotspots World ✅ HECHO (2026-06-19)
- `MoriMochiSpawner` (622) → `.cs` (398, motor: prewarm/sync/pump/spawn) + `.Pool.cs` (62) + `.Ballistics.cs` (66, solve velocity/landing) + `.Debug.cs` (123, dev buttons + gizmos). Mismo patrón partial-class. Verificado exacto (404 líneas), llaves balanceadas.

### Fase 3 — Slim Core/Systems 🟡 (tech-debt previo #2, #4)
- Separar debug/serialización Odin de la lógica de dominio en `BreedingController` y `CombatController`.
- Adelgazar `GameManager` (mint + persistencia + escenas + eventos en un solo archivo).

### Fase 4 — Unificar acceso a SO 🟡
Hoy: unos SO son singleton (`Current = this` en `BreedingAffinityTableSO`, `FurTypeDatabaseSO`, `InheritanceOddsTableSO`, `PersonalityProfileSO`, `CombatManagerSO`), otros se pasan por referencia (`CreatureLifeStageTableSO`). **Elegir UNA convención** y aplicarla a los 6. Decisión pendiente de Juan.

### Fase 5 — Red y reconciliación 🟡 (parcial)
- `CloudSyncService` (568) ✅ partido → `.cs` (133, núcleo+meta) + `.Auth.cs` (246, auth+init+cuenta) + `.Sync.cs` (218, validate+reset+push+pull). Verificado exacto (353 líneas), llaves balanceadas.
- PENDIENTE: revisar duplicación de patrón endpoint→reconciliación en `AsyncCombatService`/`AsyncBreedingService`; extraer helper común si se confirma (esto SÍ es refactor de lógica, no solo split).

---

## 📋 Tabla resumen de prioridad

| # | Item | Fase | Impacto | Riesgo |
|---|------|------|---------|--------|
| 1 | Namespacing `RunRun.*` | 0 | Organización | Bajo |
| 2 | Código muerto / debug gated | 0 | Limpieza | Bajo |
| 3 | Partir `MoriMochiAgent` en colaboradores | 1 | Arquitectura | Medio |
| 4 | Partir paneles UI (Combat/Breeding) | 2 | Mantenibilidad | Medio |
| 5 | Separar debug/UI de dominio en Controllers | 3 | Arquitectura | Medio |
| 6 | Adelgazar `GameManager` | 3 | Arquitectura | Medio |
| 7 | Unificar convención de acceso a SO | 4 | Estabilidad | Medio |
| 8 | Deduplicar Async services + CloudSync | 5 | Arquitectura | Medio |
