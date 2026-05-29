# RunRunSimulator — MoriMonchis — CLAUDE.md

## 📚 Fuente de Verdad — Leer SIEMPRE primero

> **Antes de cualquier sesión de trabajo, consultar el Notion Wiki.** Contiene el GDD completo, todas las decisiones de diseño tomadas, el roadmap actualizado y las preguntas de diseño abiertas. Este CLAUDE.md es un resumen de referencia rápida; el Notion es el documento vivo y autoritativo.

| Recurso | URL |
|---------|-----|
| 🟣 **Notion Wiki (Hub)** | https://www.notion.so/36cac10136a781819b74e176ed7c00d9 |

**Estructura del Notion** — el hub se divide en dos pilares + dos páginas transversales. Regla mental: **diseño/mecánicas → Gameplay; cómo está construido → Arquitectura.**

- 🎮 **Gameplay (GDD)** — el QUÉ: Concepto y Pilares · Sistema Genético (Diseño) · Breeding · Combate, Venganza y Bidding · Evolución y Ciclo de Vida · Honorarios / Liga del Cielo · Tienda, Economía y Onboarding
- 🏗️ **Arquitectura (Dev)** — el CÓMO: Arquitectura General · Genética — Implementación · Identidad y Persistencia · Breeding — Implementación · Combate Local — Implementación · Combate Async + UGS · UGS CLI & Scheduler
- 📋 **Decisiones de Diseño** — registro consolidado de decisiones cerradas, agrupadas por sistema
- ❓ **Preguntas Abiertas** — solo lo aún sin resolver

**Qué sub-página abrir según la tarea:**
- Genética/DNA/partes → *Sistema Genético (Diseño)* (diseño) + *Genética — Implementación* (código)
- Breeding → *Breeding* (diseño) + *Breeding — Implementación* (código)
- Batalla/combate → *Combate, Venganza y Bidding* (diseño) + *Combate Local — Implementación* + *Combate Async + UGS* (código)
- UGS/cloud/scheduler → *Combate Async + UGS* + *UGS CLI & Scheduler*
- Tienda/economía/onboarding → *Tienda, Economía y Onboarding*
- Persistencia/save/registry → *Identidad y Persistencia*
- Lore → *Concepto y Pilares* + *Honorarios / Liga del Cielo*
- Decisiones ya tomadas → *Decisiones de Diseño*
- Preguntas aún abiertas → *Preguntas Abiertas*

> Al resolver una pregunta de *Preguntas Abiertas*, moverla a *Decisiones de Diseño*. Al cambiar diseño, actualizar la sub-página de Gameplay correspondiente; al cambiar implementación, la de Arquitectura.

---

## Selección de modelo

Antes de comenzar cualquier tarea, evaluar si el modelo actual (Sonnet) es adecuado. **Avisar al usuario si se recomienda cambiar a Opus** antes de proceder.

Cambiar a Opus cuando la tarea implique:
- Diseño de sistemas nuevos desde cero con muchas decisiones interconectadas (economía, tienda, meta-game)
- Arquitectura que afecte múltiples etapas del roadmap simultáneamente
- Análisis de trade-offs complejos sin respuesta obvia

Sonnet es suficiente para:
- Implementación de features concretas (scripts, refactoring, bugfixes)
- Trabajo dentro de sistemas ya diseñados
- Tareas con requisitos claros y acotados

---

## Qué es este proyecto
Simulador de tienda retro 3D ambientado en los años 80s. El jugador es el dueño de una tienda que se sumó a la tendencia "MoriMonchis": criaturas biológicas del tamaño de la palma de la mano (estética Gremlins + Furby + Tamagotchi). En la trastienda opera un club de peleas clandestino asíncrono. Referencia de género: los *Simulator games* del mercado actual (PowerWash Simulator, etc.) pero con genética y muerte permanente.

## Nombre oficial de las criaturas
- **Singular**: MoriMochi
- **Plural**: MoriMonchis
- En código interno usar `Creature` / `CreatureDNA` para generalidad. En UI, logs visibles al jugador y naming de assets usar MoriMochi/MoriMonchis.

## Stack Técnico
- **Motor**: Unity 3D (C#)
- **Inspector**: Odin Inspector — SIEMPRE usar `SerializedScriptableObject`, `[OdinSerialize]` para Diccionarios, y atributos de Odin para UI de editor (`[Title]`, `[BoxGroup]`, `[Button]`, `[TableList]`, `[Searchable]`, etc.)
- **Serialización**: Newtonsoft.Json — package `com.unity.nuget.newtonsoft-json` requerido para persistencia de criaturas
- **Backend**: Unity Gaming Services (UGS) — Authentication (Player Accounts), Cloud Save (Player Data + Custom Data), Cloud Code (JS scripts), Scheduler (cron triggers)
- **Dev tooling**: UGS CLI (`ugs`) — binario standalone para desplegar schedules. Service Account con roles `Unity Environments Admin` + `Cloud Code Editor/Viewer/Publisher` a nivel project, y `Owner` a nivel organization
- **Arte**: 3D, partes como FBX, ensamblaje con anchor points

---

## Arquitectura actual

```
CloudCode/                            # Server-side scripts y schedules (fuera del build Unity, se suben al Dashboard via CLI o manualmente)
├── enqueue-combat.js                 # Cloud Code: append entry a matchmaking_pool. Returns {status,poolSize}
├── process-matchmaking.js            # Cloud Code (scheduled): drena pool, empareja, simula, escribe combat_results en cada player
├── run-combat.js                     # Cloud Code (legacy modo Instant): enqueue + match + simulate en una sola llamada
├── test-random.js                    # Diagnostic: returns random 1-4
├── test-customdata.js                # Diagnostic: read/write/read isolated en Custom Data
└── matchmaking-tick.sched            # UGS Scheduler: cron "0 * * * *" → invoca process-matchmaking

Assets/RunRunSimulator/Scripts/
├── Enums.cs                          # Rarity, PartSet, CreatureGender, PartRole, Tier, BusyReason
├── Interfaces.cs
├── GameManager.cs                    # Lab: Generate / Mint + SOURCE OF TRUTH de los assets compartidos (getters Registry/Database/RarityOddsTable/InheritanceOddsTable/CombatConfig + PushToCloud)
├── CreatureGenerator.cs              # static: GenerateRandom(db, oddsTable?)
├── BreedingService.cs                # static: Breed() — traversal árbol genealógico
├── BreedingController.cs             # MonoBehaviour: UI breeding (Fill Random Breeders + Breed). Referencia GameManager. Espejo de CombatController
├── CombatService.cs                  # static: Simulate() — combate local por turnos, evolución, muerte
├── CombatController.cs               # MonoBehaviour: UI local combat + Async Combat (Instant + Timer buttons). Referencia GameManager
├── AsyncCombatService.cs             # MonoBehaviour: EnqueueInstantAsync / EnqueueScheduledAsync / PollResultsAsync. Referencia GameManager
├── CloudCodeTester.cs                # MonoBehaviour DEV: TestRandom / TestCustomData / ForceMatchmakingTick
├── CloudSyncService.cs               # MonoBehaviour: Unity Player Account auth + auto-pull on login + Cloud Save push/pull/reset + SyncMeta. Referencia GameManager
├── SaveSystem.cs                     # static: SaveDatabase / LoadInto / Serialize (scoped por playerId, migración automática)
├── Data/
│   ├── CreatureDNA.cs                # Genética + Identidad + Linaje + Progresión + Tier/slot + Stats + IsDead
│   ├── CreatureRegistrySO.cs         # SO registry: Dictionary<string, CreatureDNA> — InfoBox warning + Sync btn
│   ├── CreatureDatabaseSO.cs         # SO orquestador: refs sub-DBs + validación de IDs
│   ├── CreaturePartData.cs
│   ├── PartNameBank.cs               # static: pools de nombres por (PartSet, PartRole)
│   ├── RarityOddsTableSO.cs          # SO: pesos por Rarity → Roll() independiente por slot
│   ├── InheritanceOddsTableSO.cs     # SO singleton: odds breeding — pesos configurables en inspector (sin JSON)
│   ├── CombatManagerSO.cs            # SO singleton: EvolutionChance, DeathChance, CritChance, MaxRounds, MaxFightCount(5)
│   ├── CombatResult.cs               # Data class: WinnerID, LoserID, Log, LoserDied, WinnerEvolved, IsDraw
│   ├── Parts/
│   │   ├── BodyPart.cs               # abstract SO: ID[ReadOnly], Name, Rarity, Tier, Set + HP/Attack/Speed
│   │   ├── ArmPart.cs                # GetPartRole() = Arm
│   │   ├── EyePart.cs                # GetPartRole() = Eye
│   │   ├── MouthPart.cs              # GetPartRole() = Mouth
│   │   └── BodyShapePart.cs          # GetPartRole() = Body
│   └── Databases/
│       ├── PartDatabaseSO.cs         # abstract generic: IDPrefix + Sync All IDs + Roll All Names
│       ├── ArmDatabaseSO.cs          # IDPrefix = "A"  → A0, A1, A2…
│       ├── EyeDatabaseSO.cs          # IDPrefix = "E"  → E0, E1, E2…
│       ├── MouthDatabaseSO.cs        # IDPrefix = "M"  → M0, M1, M2…
│       └── BodyShapeDatabaseSO.cs    # IDPrefix = "BS" → BS0, BS1, BS2…
```

---

## Roadmap

| Etapa | Sub-etapa | Estado |
|-------|-----------|--------|
| 1 | 1.1 Arquitectura genética + DNA string + Databases de partes | ✅ Completo |
| 1 | 1.2 Visualizador de criaturas (leer DNA → ensamblar Prefab 3D) | 🔲 Siguiente |
| 1 | 1.3 Sistema de Breeding (herencia, linaje, registro, persistencia) | 🔶 En progreso |
| 2 | 2.1 Sistema de Estadísticas (HP, Fuerza, Velocidad desde partes) | 🔶 Iniciado — BaseStats en DNA + stats por pieza en BodyPart SO |
| 2 | 2.2 Simulador de Batalla local → Battle Log | ✅ — CombatService completo: turnos, empate, límite de peleas, log detallado por turno |
| 2 | 2.3 Integración Unity Services (async battles) | ✅ — Auth + Cloud Save (push/pull/auto-sync) + Cloud Code (enqueue-combat + process-matchmaking) + Scheduler (cron 1h) + modo Instant para testing |
| 3 | 3.1 Tienda Local (NPCs, inventario, vitrinas) | 🔲 Pendiente |
| 3 | 3.2 Mercado Online (P2P via Unity Services) | 🔲 Pendiente |

**Etapa 1.3 — estado detallado:**

| Feature | Estado |
|---------|--------|
| `BreedingService.Breed()` con traversal genealógico | ✅ |
| `InheritanceOddsTableSO` SO puro (pesos configurables en inspector) | ✅ |
| `CreatureRegistrySO` registry visual [ReadOnly] + JSON source of truth | ✅ |
| `SaveSystem` persistencia JSON completa | ✅ |
| `GameManager.MintRandomCreature()` y `BreedCreatures()` | ✅ |
| Validación límite máximo de crías (4) — `BreedingService.MaxBreedCount` | ✅ |
| Validación límite máximo de combates (5) — `CombatManagerSO.MaxFightCount` | ✅ |
| `IsDead` bloquea breed y combate (validado en BreedingService y CombatService) | ✅ |
| Herencia de stats en Breed: 50/50 madre/padre + delta ±1, mínimo 1 | ✅ |
| GameManager: Fill Random Breeders + Fill Random Fighters con info de límites | ✅ |
| Género por battle-index del padre (actualmente 50/50) | 🔲 Pendiente (Etapa 2) |
| Bonus de rareza en la 4ª cría (última posible) | 🔲 Pendiente |
| Herencia del nivel Tier de las partes | 🔲 Pendiente |

---

## Reglas de código

1. **Desacoplamiento estricto**: cada sistema (genética, batalla, tienda) es independiente. Comunicación via interfaces o eventos, no referencias directas cruzadas.
2. **No comentar el QUÉ**: solo comentar el POR QUÉ cuando hay un invariante no obvio.
3. **Sin features adelantadas**: no implementar UGS ni mecánicas de batalla hasta Etapa 2. La persistencia local JSON es válida desde Etapa 1.3.
4. **DNA como string ligero**: `CreatureDNA.ToStringID()` / `FromID()` son el contrato de red — no romperlo. El timestamp es metadata de registro, no forma parte del genetic string.
5. **IDs de partes**: nunca pueden contener el carácter `-` (es el separador del DNA string).
6. **Odin siempre**: cualquier ScriptableObject con Diccionarios hereda de `SerializedScriptableObject`. Usar `[OdinSerialize]` explícitamente.
7. **Sin complejidad innecesaria**: no añadir campos, abstracciones ni features que no hayan sido pedidos explícitamente. Tres líneas similares son mejor que una abstracción prematura.

---

## Sistema de IDs de Partes

- Los IDs son **auto-generados** por la database. **No se editan manualmente** — `BodyPart.ID` es `[ReadOnly]`.
- Formato por tipo: `BS0`, `BS1`… / `A0`, `A1`… / `E0`, `E1`… / `M0`, `M1`…
- El botón **Sync All IDs** en cada database renumera TODO desde 0 y escribe el valor de vuelta en `part.ID`. Usar en setup inicial, nunca con DNA strings ya distribuidos en red.

---

## Sistema de Nombres de Partes (PartNameBank)

- `PartNameBank.cs` — clase estática, pools de 5 palabras por cada `(PartSet, PartRole)`.
- Botón **Roll Name** en cada `BodyPart` SO: genera nombre individual.
- Botón **Roll All Names** en cada database SO: genera nombres en bulk para todas las partes.
- Nombre de criatura = `"{body} {arm} {eye} {mouth}"` → `CreatureDNA.GetDisplayName(db)`.
- Las palabras están temáticamente ligadas al `PartSet` (GooGang = pegajoso, ZapZone = eléctrico, etc.).

---

## Identidad de Criaturas (CreatureDNA)

```
ToStringID() = "BS0-A3-E1-M2-FF00AA"              // genetic string — contrato de red (inmutable)
UniqueID     = "BS0-A3-E1-M2-FF00AA-{Ticks}"      // clave en el registro
BirthDate    = DateTime (UTC)
Stamp()      → setea Timestamp + BirthDate de forma atómica antes de registrar
```

- `CustomName` — nombre editable, auto-asignado en Mint y Breed via `CreatureNameBank.GetRandomName()`. Formato: adjetivo + sustantivo ("Fuzzy Blob"). Editable por el usuario.
- `MotherID`, `FatherID`, `ChildrenIDs` — referencias por `UniqueID` (no genetic strings)
- `Gender` — `Unknown` hasta mintearse. Se asigna 50/50 en `Mint` y en `Breed`.
- `FightCount`, `WinCount`, `BreedCount` — progresión, escritos por CombatService y BreedingService
- `BodyTier`, `ArmTier`, `EyeTier`, `MouthTier` — Tier por slot, independiente por instancia (Tier1 al nacer)
- `BaseHP`, `BaseAttack`, `BaseSpeed` — stats base aleatorios 1–10, asignados en Mint
- `IsDead` — muerte permanente; bloquea combate y breeding si es `true`

---

## Sistema de Breeding (InheritanceOddsTableSO)

Probabilidades por defecto — configurables, normalizadas internamente:

| Origen | Peso por defecto |
|--------|-----------------|
| Padres directos | 40 |
| Abuelos | 20 |
| Bisabuelos | 10 |
| Mutación aleatoria | 20 |
| Base / entorno | 10 |

- Cada slot (body, arm, eye, mouth) hace su **propio roll independiente**.
- Mutación y Base → parte aleatoria del pool completo (sin filtro de rarity ni set).
- Si un ancestro no existe en el registro, cae automáticamente al fallback random.
- Pesos editables directo en el inspector del SO asset — la serialización es la de Unity (no JSON).
- Singleton: `InheritanceOddsTableSO.Current` (se setea en `OnEnable` del SO).
- **Validaciones en `BreedingService.Breed()`**: `IsDead`, género correcto, `BreedCount < MaxBreedCount (4)`. `BreedCount` se incrementa en ambos padres dentro del servicio.
- **Stats del hijo**: cada stat (HP, ATK, SPD) hereda 50/50 de madre o padre, luego aplica delta aleatorio de -1, 0 o +1. Mínimo garantizado: 1.
- `GameManager`: botón **Fill Random Breeders** — selecciona hembra + macho vivos bajo el límite. Muestra info de breeds restantes en inspector.

---

## Sistema de Rareza (RarityOddsTableSO)

- Pesos relativos configurables por `Rarity`. Por defecto: Common 60 / Uncommon 25 / Rare 10 / Epic 4 / Legendary 1.
- `CreatureGenerator.GenerateRandom(db, oddsTable)` → cada uno de los 4 slots hace su propio `oddsTable.Roll()`.
- Si no se asigna tabla, el generador elige sin filtro de rareza.

---

## Part Sets (PartSet enum)

Cada `BodyPart` tiene `Set` que agrupa partes en un tema visual/lore. 10 sets: `GooGang`, `BogBrigade`, `FuzzFactory`, `CosmicCreeps`, `NeonNightmares`, `CrunchCrew`, `GrimGlobs`, `SpudSquad`, `MoldMob`, `ZapZone`. Colores dinámicos en inspector con `[GUIColor]`.

---

## Tier (enum)

`Tier1 = 1`, `Tier2 = 2`, `Tier3 = 3`. Campo en `BodyPart`. Las partes nacen en Tier1. La evolución de Tier durante combate es Etapa 2.

---

## Gender (CreatureGender enum)

`Unknown` (sin registrar), `Male`, `Female`. **NO forma parte del DNA string.** GDD target: género basado en battle-index del padre — pendiente Etapa 2.

---

## Persistencia (SaveSystem)

| Archivo | Contenido | Formato |
|---------|-----------|---------|
| `creature_database.json` | Registro completo de criaturas + árbol genealógico | Newtonsoft.Json |

- `SaveDatabase(registry)` se llama automáticamente en `Mint`, `Breed` y `OnApplicationQuit`.
- `LoadInto(registry)` se llama en `Awake` del GameManager — popula el SO desde JSON.
- `UnityEngine.Color` → hex string via custom `UnityColorConverter`.
- **Dependencia**: package `com.unity.nuget.newtonsoft-json` `3.2.1` en Package Manager (namespace `Newtonsoft.Json`, **no** `Unity.Plastic.Newtonsoft.Json`).
- `CreatureRegistrySO` es el SO asset asignado en GameManager → Setup. JSON es la única fuente de verdad; el SO es vista visual [ReadOnly].

---

## Anchor Points Estándar (Visualizador — Etapa 1.2)

- Estándar fijo: **2 arm anchors + 2 eye anchors + 1 mouth anchor** (formato 2-2-1)
- Partes = hijos del prefab con Transform propio (sin merge de mesh) — intercambiables en runtime
- Se requiere **preview en editor** (editor-time assembly al seleccionar un DNA)

---

## Convenciones de CreateAssetMenu

```
RunRunSimulator/Parts/Arm
RunRunSimulator/Parts/Eye
RunRunSimulator/Parts/Mouth
RunRunSimulator/Parts/Body Shape
RunRunSimulator/Databases/Arm Database
RunRunSimulator/Databases/Eye Database
RunRunSimulator/Databases/Mouth Database
RunRunSimulator/Databases/Body Shape Database
RunRunSimulator/Creature Database (Orchestrator)
RunRunSimulator/Creature Registry
RunRunSimulator/Rarity Odds Table
RunRunSimulator/Inheritance Odds Table
RunRunSimulator/Combat Manager
```

---

## Sistema de Combate (CombatService)

- Stats efectivos = `BaseStat (DNA) + Σ(part.Stat + (tier-1))` por slot. Calculados en runtime, no almacenados.
- Orden por `Speed`; empates aleatorios. 20% crit = ×3 daño. Safety cap: `MaxRounds = 50`.
- **Límite de peleas**: `MaxFightCount = 5` (en `CombatManagerSO`). `CombatService.Simulate()` valida que `FightCount < MaxFightCount` antes de simular.
- **Empate**: si ninguno llega a 0 HP antes de `MaxRounds` → `IsDraw = true`, `FightCount++` en ambos, sin evolución ni muerte.
- **Log por turno**: cada round loguea quién ataca primero, daño, si fue crit, y HP restante del defensor. La línea final siempre incluye nombre, UniqueID y parte evolucionada.
- **Evolución (ganador)**: el ganador **siempre** evoluciona una parte aleatoria elegible (< Tier3). Si todas están en Tier3, se loguea que no hay más evolución posible.
- **Evolución futura (pendiente)**: la parte a evolucionar se elegirá por peso según su tier actual — las partes en Tier1 tienen más probabilidad de ser seleccionadas que las de Tier2, y éstas más que las de Tier3. Regla diseñada: **70% peso Tier1 → Tier2 / 20% peso Tier2 → Tier3 / 10% peso Tier3** (tier máximo, sin efecto). Si un pool está vacío se excluye y los pesos restantes se renormalizan. `CombatManagerSO.EvolutionChance` queda reservado para reglas futuras.
- **Muerte (perdedor)**: probabilidad configurable `DeathChance` en `CombatManagerSO`.
- `CombatManagerSO.Current` — singleton configurable. Asignar en `GameManager → Setup`.
- `GameManager`: botón **Fill Random Fighters** — selecciona 2 criaturas vivas con peleas disponibles. Muestra fights restantes en inspector.

## UGS Cloud Save (CloudSyncService)

- Adjuntar al mismo GameObject que `GameManager`. Asignar `CreatureRegistrySO`.
- Requiere en Unity Dashboard: **Authentication** (Anonymous + **Unity Player Accounts**) + **Cloud Save**.
- **Autenticación**: Unity Player Account via `PlayerAccountService.StartSignInAsync()` (browser). Primer login abre browser; siguientes launches reanudan sesión silenciosamente via `SessionTokenExists`.
- **Player name**: visible en inspector `[Status]`. Botón `Update Name` en bloque `[Account]`.
- **Sign Out**: cierra la sesión actual; el session token se conserva (el próximo launch auto-reanuda).
- **Sesión persistente**: en `Start`, si `AuthenticationService.Instance.SessionTokenExists` → `SignInAnonymouslyAsync()` reutiliza el token cacheado sin browser.
- **Auto-pull en login**: `OnSignedInComplete` ejecuta `SaveSystem.SetUserScope(playerID)` → `LoadInto` (cache local) → `await PullAsync()` (override desde cloud si hay data).
- **Auto-push en Mint/Breed**: `GameManager.TryPushToCloud()` se invoca después de `SaveDatabase` en `MintRandomCreature` y `BreedCreatures` — fire-and-forget. Requiere asignar el `CloudSyncService` en el inspector del `GameManager`.
- **Save local scoped por playerId**: el archivo pasa de `creature_database.json` a `creature_database_<playerId>.json` después del sign-in. Si existe el unscoped pero no el scoped, hay **migración automática** la primera vez. Permite testing con múltiples cuentas/instancias sin que se pisen.
- Push/Pull manual también disponibles via botones del inspector (`EnableIf(_isSignedIn)`).
- `sync_meta.json` local registra timestamps de seguridad para detección de rollback/edición manual.
- **Reset All Progress (DEV)**: borra keys de Cloud Save + vacía JSON local + borra sync_meta.
- **Dev mode**: CHEAT ALERT solo imprime en consola — activar bloqueo en post-Etapa 2.3 con Cloud Code firmando tokens.

---

## Sistema de Combate Async (AsyncCombatService + Cloud Code)

Arquitectura **dual** con dos modos coexistentes que comparten el mismo pool en Cloud Save Custom Data.

### Modo Instant — `run-combat.js`

Botón naranja "Enqueue for Combat (Instant)". El cliente llama `run-combat`, que en un mismo request:
1. Lee el pool. Si hay opponent → simula y escribe resultados a ambos players. Returns `{status: "matched"}` y el cliente hace `PollResultsAsync()` inmediato.
2. Si no hay opponent → enqueue y returns `{status: "waiting"}`.

Use case: testing rápido entre dos cuentas activas.

### Modo Timer/Scheduled — `enqueue-combat.js` + `process-matchmaking.js`

Botón morado "Enqueue for Combat (Timer)". Flow:
1. Cliente llama `enqueue-combat` → solo agrega al pool, returns `{status: "queued"}` inmediato. Cliente puede cerrar el juego.
2. Unity Scheduler dispara `process-matchmaking` cada hora UTC (cron `0 * * * *`). El script drena el pool, hace shuffle, empareja evitando self-match, simula cada par y escribe `combat_results` en el Player Data de cada jugador. Leftover odd-one-out vuelve al pool.
3. Cliente vuelve al juego, presiona "Check Pending Results" → `PollResultsAsync()` aplica los resultados localmente.

### Custom Data — quirks descubiertos

- `setCustomItem` firma correcta: **3 args** `(projectId, customId, body)` donde body es `{key, value}`. La firma de 4 args silently corrompe el body.
- `value` **NO acepta arrays top-level** — siempre envolver en `{ entries: [...] }`. Empírico, la doc no lo dice.
- El método se llama `getCustomItems` (plural con array de keys), NO `getCustomItem` singular.
- Auth via `accessToken: context.serviceToken` en el constructor del `DataApi`.

### Identificación de oponentes en logs

`CombatResult` incluye `OpponentName` (criatura), `OpponentPlayerId` (UUID), `OpponentPlayerName` (display name de `AuthenticationService.GetPlayerNameAsync()`). El log final se ve:

```
[AsyncCombat] "Fuzzy Blob" — WON vs Sowtank's "Slimy Goo" [PugP4yQqEij...] | Evolved: Arm
```

### Scheduler — gestión via UGS CLI

El Dashboard de Unity NO permite crear schedules (pestaña Triggers → opción Schedule en gris). Hay que usar la **UGS CLI** (binario standalone desde GitHub Releases).

Setup minimal:
1. Bajar `ugs-windows-x64.exe`, renombrar a `ugs.exe`, agregar al PATH
2. Crear Service Account en Dashboard → Organization → Administration → Service Accounts → generar Keys
3. Asignar roles: project (`Cloud Code Editor/Viewer/Publisher`, `Unity Environments Admin`) + organization (`Owner`)
4. `ugs login` + `ugs config set project-id <id>` + `ugs config set environment-name production`
5. Editar `CloudCode/matchmaking-tick.sched` con el cron deseado
6. `ugs deploy CloudCode/matchmaking-tick.sched`
7. Verificar con `ugs sched list`

⚠️ **Restricción de Unity**: schedule mínimo cada 1 hora. Para testing inmediato hay un botón **"Force Matchmaking Tick (DEV)"** en `CloudCodeTester.cs` que llama directo a `process-matchmaking`.

### Service Account ≠ Project Secrets

- **Service Account Keys** (Organization → Administration → Service Accounts → Keys): para autenticar la CLI/herramientas externas.
- **Project Secrets** (Proyecto → Cloud Code → Secrets): variables de entorno para que los scripts JS accedan a APIs externas en runtime.

NO confundirlas — son cosas distintas.

## Bugs conocidos (pendientes de fix)

- `DeathChance` está hardcoded a 15% en `process-matchmaking.js` y `run-combat.js`. Cambiar el `CombatManagerSO.DeathChance` solo afecta el combate local. Para sincronizar habría que pasar el valor como param o duplicarlo manualmente.
- No hay race-condition handling en el matchmaking pool — dos llamadas simultáneas pueden pisarse. Aceptable en testing; para producción con tráfico real, agregar `writeLock` del SetItemBody.
