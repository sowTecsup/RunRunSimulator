# RunRunSimulator — MoriMonchis — CLAUDE.md

> **Memory Bank**: el detalle técnico vive en `MoriMonchiVault/` (Obsidian). Este archivo es el núcleo: reglas no-negociables + orientación + índice. Léelo siempre primero; lee el vault según la tarea.

## 📚 Source of truth

| Recurso | Para qué |
|---------|----------|
| 🟣 [Notion Wiki](https://www.notion.so/36cac10136a781819b74e176ed7c00d9) | Diseño vivo, decisiones, preguntas abiertas. Cuando dudes de **diseño**, abre Notion. |
| 📁 `MoriMonchiVault/` (Obsidian) | Detalle de **implementación**, quirks técnicos, archivos clave. Cuando dudes de **código**, lee del vault. |

---

## Qué es este proyecto (1 línea)

Simulador de tienda retro 3D ambientado en los 80s. El jugador cría/pelea **MoriMonchis** (criaturas tipo Gremlins + Furby + Tamagotchi) con **genética visible, muerte permanente y combate async server-side**. Más en [[MoriMonchiVault/01 - GDD Core|01 - GDD Core]].

## Nombre oficial de las criaturas

- **Singular**: MoriMochi · **Plural**: MoriMonchis.
- Código interno: `Creature` / `CreatureDNA` (generalidad).
- UI, logs visibles al jugador, naming de assets: **MoriMochi/MoriMonchis**.

## Stack Técnico

- **Motor**: Unity 3D (C#).
- **Inspector**: Odin Inspector — SIEMPRE usar `SerializedScriptableObject`, `[OdinSerialize]` para Diccionarios, y atributos de Odin para UI de editor (`[Title]`, `[BoxGroup]`, `[Button]`, `[TableList]`, `[Searchable]`, etc.).
- **Serialización**: Newtonsoft.Json — package `com.unity.nuget.newtonsoft-json` (no `Unity.Plastic.Newtonsoft.Json`).
- **Backend**: Unity Gaming Services (UGS) — Authentication (Player Accounts), Cloud Save (Player Data + Custom Data), Cloud Code (JS scripts), Scheduler (cron triggers).
- **Dev tooling**: UGS CLI (`ugs`).
- **Arte**: 3D, partes como FBX, ensamblaje con anchor points 2-2-1 (2 arms + 2 eyes + 1 mouth).

## Selección de modelo

Antes de comenzar cualquier tarea, evaluar si el modelo actual (Sonnet) es adecuado. **Avisar al usuario si se recomienda cambiar a Opus** antes de proceder.

**Opus** para: diseño de sistemas nuevos con muchas decisiones interconectadas (economía, tienda, meta-game), arquitectura que afecte múltiples etapas del roadmap, análisis de trade-offs complejos.

**Sonnet** para: implementación de features concretas, refactoring, bugfixes, trabajo dentro de sistemas ya diseñados.

---

## 🚨 Reglas de código (NO NEGOCIABLES)

1. **Desacoplamiento estricto vía eventos**: cada sistema (genética, batalla, tienda) es independiente. La comunicación cross-sistema pasa por `GameEvents` (bus estático), nunca por referencias directas ni llamadas a singletons del otro sistema. **Regla de oro: el evento transporta la data.** Un suscriptor recibe el `registry` (u otro payload) en el evento y trabaja sobre él — NO vuelve a buscarlo con `GameManager.Instance.Registry`.
2. **Persistencia solo por evento**: ningún script de gameplay llama `SaveSystem.SaveDatabase` ni `PushToCloud` directamente. Disparan `GameEvents.RegistryChanged(registry)` y `GameManager` (único dueño de persistencia) hace el save+push. Excepción: `CloudSyncService` (capa de sync) y el flush final en `GameManager.OnApplicationQuit`. Reload externo (cloud pull/reset) usa `OnRegistryReloaded` → solo UI, sin re-push.
3. **No comentar el QUÉ**: solo comentar el POR QUÉ cuando hay un invariante no obvio.
4. **Sin features adelantadas**: no implementar mecánicas hasta su etapa del roadmap. La persistencia local JSON es válida desde Etapa 1.3.
5. **DNA como string ligero**: `CreatureDNA.ToStringID()` / `FromID()` son el contrato de red — no romperlo. El timestamp es metadata de registro, no forma parte del genetic string.
6. **IDs de partes**: nunca pueden contener el carácter `-` (es el separador del DNA string).
7. **Odin siempre**: cualquier ScriptableObject con Diccionarios hereda de `SerializedScriptableObject`. Usar `[OdinSerialize]` explícitamente.
8. **Sin complejidad innecesaria**: no añadir campos, abstracciones ni features que no hayan sido pedidos. Tres líneas similares son mejor que una abstracción prematura.
9. **Desuscribir siempre**: todo MonoBehaviour que se suscribe a un `GameEvents` lo hace en `OnEnable` y se desuscribe en `OnDisable`. Un `event static` mantiene vivo al suscriptor (leak + excepción al disparar sobre un objeto destruido).

---

## Arquitectura de eventos (filosofía)

Bus estático `GameEvents.cs` (namespace global). Publicadores y suscriptores dependen **solo del bus**. **Los eventos transportan la data**: el payload (registry, CombatResult, etc.) viaja en el invoke, el suscriptor no busca al singleton.

Helper estático por evento (`RegistryChanged(so) => OnRegistryChanged?.Invoke(so)`). Un solo evento de mutación con payload (no dos en paralelo).

| Evento | Quién dispara |
|--------|---------------|
| `OnRegistryChanged` | toda mutación de gameplay |
| `OnRegistryReloaded` | `CloudSyncService` tras pull/reset (UI-only, sin push) |
| `OnCreatureMinted` | `GameManager.MintRandomCreature` |
| `OnCombatCompleted` | combate local |
| `OnCombatLogged` | `AsyncCombatService.ApplyResult` |
| `OnBreedingCompleted` | breeding local + async |
| `OnFurnitureChanged` | toda mutación de furniture (`FurnitureService`) |
| `OnFurnitureReloaded` | reload de furniture (clear+resync; UI/spawner-only, sin push) |

Eventos UI viven en `UIManager` como `static event Action` (separados de `GameEvents`). Acción maps `Player`/`UI` mutuamente excluyentes, conmutados en `OnUIFocusChanged`. Detalle en [[MoriMonchiVault/05 - UI System|05 - UI System]] y [[MoriMonchiVault/07 - Persistence & Identity|07 - Persistence & Identity]].

---

## Estructura de carpetas (top-level)

```
Assets/RunRunSimulator/Scripts/
├── Core/          # Bus, persistencia, generación, tipos base, GameManager
├── Systems/       # Breeding · Combat · Cloud · Furniture (cada uno desacoplado vía GameEvents)
├── UI/            # UIManager + paneles uGUI/UITK + IUINavigable
├── Player/        # PlayerInputs · PlayerController · PlayerAnimator (FP)
├── Interactables/ # IInteractable · IThrowable (drop-a-script)
├── World/         # MoriMochiSpawner · MoriMochiAgent · NameTag (NavMesh)
└── Data/          # CreatureDNA, SOs, parts, databases

CloudCode/         # Scripts JS server-side + .sched/.tr (UGS Scheduler+Trigger)
MoriMonchiVault/   # Memory Bank (Obsidian)
```

Mapeo detallado de cada script → vault.

---

## Roadmap (status compacto)

| Etapa | Estado |
|-------|--------|
| 1.1 Arquitectura genética + DNA + Databases | ✅ |
| 1.2 Visualizador de criaturas | 🔶 Grilla inspector ✅, falta 3D |
| 1.3 Sistema de Breeding | 🔶 Local ✅, refinamientos pendientes |
| 2.1 Sistema de Estadísticas | 🔶 BaseStats + stats por pieza ✅ |
| 2.2 Combate local + Battle Log | ✅ |
| 2.3 Integración UGS (async battles) | ✅ |
| 2.4 Breeding Async (timer server-side) | ✅ |
| 2.5 Vida en Escena (NavMesh + personalidad) | ✅ |
| 3.1 Tienda Local (furniture + economía) | ✅ (pendiente menor: persistir CurrentStock en cloud + deploy get-server-time.js) |
| 3.2 Mercado Online | 🔲 |

Detalle por feature en [[MoriMonchiVault/02 - Genetics & Breeding|02]], [[MoriMonchiVault/03 - Combat|03]], [[MoriMonchiVault/06 - Player & World|06]]. Pendientes en [[MoriMonchiVault/08 - Known Bugs & Checkpoints|08]].

---

## 📍 Índice del Memory Bank (`MoriMonchiVault/`)

**Antes de tocar código, lee el archivo del vault relevante a la tarea.**

| Archivo | Cuándo leerlo |
|---------|---------------|
| [[MoriMonchiVault/00 - Index\|00 - Index]] | Mapa completo (qué leer según tarea) |
| [[MoriMonchiVault/01 - GDD Core\|01 - GDD Core]] | Visión, core loop, naming, pilares |
| [[MoriMonchiVault/02 - Genetics & Breeding\|02 - Genetics & Breeding]] | DNA, partes, IDs, BreedingService, InheritanceOdds, breeding async |
| [[MoriMonchiVault/03 - Combat\|03 - Combat]] | CombatService local + Async dual mode + Scheduler + Custom Data quirks |
| [[MoriMonchiVault/04 - UGS & Cloud\|04 - UGS & Cloud]] | Auth, CloudSync, CLI, REST API, Service Accounts |
| [[MoriMonchiVault/05 - UI System\|05 - UI System]] | UIManager hub, IUINavigable, stack/router, paneles UITK |
| [[MoriMonchiVault/06 - Player & World\|06 - Player & World]] | FP controller, Cinemachine, grab/throw, MoriMonchis vivos, NavMesh |
| [[MoriMonchiVault/07 - Persistence & Identity\|07 - Persistence & Identity]] | SaveSystem, registry, scoped saves, GameEvents detallado |
| [[MoriMonchiVault/08 - Known Bugs & Checkpoints\|08 - Known Bugs & Checkpoints]] | Bugs activos + checkpoints futuros |
| [[MoriMonchiVault/09 - Active Context\|09 - Active Context]] | **Qué se está tocando AHORA** (actualizar cada sesión) |
| [[MoriMonchiVault/10 - Furniture & Building\|10 - Furniture & Building]] | Grid de placement, FurnitureService/Spawner, building mode, economía/tienda |

> **Convención**: cuando empieces una sesión nueva, lee primero `09 - Active Context` para ver el estado y luego los archivos relevantes a la tarea. Actualiza `09 - Active Context` al cerrar.

---

## Workflow de mantenimiento de documentación

Tres capas, tres dueños, tres triggers:

| Capa | Dueño | Cuándo |
|------|-------|--------|
| **Notion** (diseño) | Usuario | Decisión de diseño nueva, pregunta resuelta. Yo NO toco Notion. |
| **`MoriMonchiVault/`** (implementación) | Claude (a pedido) | Cambió contrato público, quirk técnico nuevo, sub-etapa cerrada, script renombrado/movido. |
| **`CLAUDE.md`** (núcleo) | Claude (a pedido) | Regla nueva, cambio de stack, roadmap status flip, nuevo archivo top-level del vault. |
| **`09 - Active Context`** | Claude (cada sesión) | Apertura y cierre. |

**Regla operativa**: al cerrar sesión, **yo propongo** qué actualizar (lista corta con justificación), **el usuario valida**, **yo aplico**. Si no propongo nada, asumir que no aprendí nada que merezca capturarse.

Si solo se arregló un bug menor sin cambiar diseño/contratos → **no actualizar** (el git log basta).

**Backup del CLAUDE.md original**: `ClaudeOld.md` en la raíz (no leer salvo migración).
