# RunRunSimulator — MoriMonchis — CLAUDE.md

> Juan, empieza cada mensaje diciendo "Juan:". Este archivo es tu regla de oro: leelo primero siempre.

**Memory Bank:** el detalle tecnico vive en `MoriMonchiVault/` (Obsidian). Este archivo es el nucleo: reglas no-negociables + orientacion + indice. Leelo siempre primero; lee el vault segun la tarea.

---

## Source of truth

| Recurso | Para que |
|---------|----------|
| Notion Wiki | Diseno vivo, decisiones, preguntas abiertas. Cuando dudes de **diseno**, abre Notion. |
| `MoriMonchiVault/` (Obsidian) | Detalle de **implementacion**, quirks tecnicos, archivos clave. Cuando dudes de **codigo**, lee del vault. |
| `MoriMonchiVault/ScriptNodes/` | Un nodo `.md` por cada script `.cs`. Leer antes de abrir el codigo fuente. |

---

## Protocolo de trabajo para IA 

1. **Abrir sesion**: leer `MoriMonchiVault/Index/09 - Active Context.md` (estado actual)
2. **Identificar sistema**: usar `MoriMonchiVault/00 - Index.md` (routing por tarea)
3. **Leer diseno**: abrir `MoriMonchiVault/Index/XX - Tema.md` (diseno, flujo, invariantes)
4. **Leer script nodes**: abrir `MoriMonchiVault/ScriptNodes/NombreScript.md` (responsabilidad, conexiones)
5. **Planear con Opus**: disenar la solucion antes de picar codigo. Evaluar alternativas, invariantes, impacto en otros sistemas.
6. **Solo entonces leer `.cs`**: ya sabes que hace cada script y como se conecta. Confirmar que el plan encaja.
7. **Generar sub-agentes**: delegar tareas concretas a sub-agentes (uno por archivo o responsabilidad). Cada sub-agente recibe el plan, la ruta del archivo, y las reglas de codigo.
8. **Cerrar sesion**: actualizar `09 - Active Context.md` con lo tocado y siguiente paso
9. **Cada mensaje** empieza con "Juan:" seguido del contenido

---

## Que es este proyecto (1 linea)

Simulador de tienda retro 3D ambientado en los 80s. El jugador cria/pelea MoriMonchis (criaturas tipo Gremlins + Furby + Tamagotchi) con genetica visible, muerte permanente y combate async server-side. Mas en [[MoriMonchiVault/Index/01 - GDD Core]].

## Nombre oficial de las criaturas

- **Singular**: MoriMochi · **Plural**: MoriMonchis.
- Codigo interno: `Creature` / `CreatureDNA` (generalidad).
- UI, logs visibles al jugador, naming de assets: **MoriMochi/MoriMonchis**.

## Stack Tecnico

- **Motor**: Unity 3D (C#).
- **Inspector**: Odin Inspector. SIEMPRE usar `SerializedScriptableObject`, `[OdinSerialize]` para Diccionarios, y atributos Odin para UI de editor (`[Title]`, `[BoxGroup]`, `[Button]`, `[TableList]`, `[Searchable]`).
- **Serializacion**: Newtonsoft.Json (`com.unity.nuget.newtonsoft-json`).
- **Backend**: Unity Gaming Services (UGS) Authentication (Player Accounts), Cloud Save (Player Data + Custom Data), Cloud Code (JS scripts), Scheduler (cron triggers).
- **Dev tooling**: UGS CLI (`ugs`).
- **Arte**: 3D, partes como FBX, ensamblaje con anchor points 2-2-1 (2 arms + 2 eyes + 1 mouth).

---

## Arquitectura de vault (MoriMonchiVault/)

```
MoriMonchiVault/
├── 00 - Index.md              ← Entry point IA (routing por tarea)
├── Index/                     ← 11 notas principales por dominio (01-11)
│   ├── 01 - GDD Core.md
│   ├── 02 - Genetics & Breeding.md
│   ├── ...
│   └── 11 - Technical Debt.md
└── ScriptNodes/               ← 95 nodos, uno por script .cs
```

---

## Arquitectura del codigo

### Estructura de carpetas (source)

```
Assets/RunRunSimulator/Scripts/
├── Core/          # GameManager, GameEvents, SaveSystem, Enums, Interfaces, CreatureGenerator
├── Data/          # CreatureDNA, BodyPart, databases, SOs
│   ├── Databases/ # ArmDatabaseSO, EyeDatabaseSO, etc.
│   └── Parts/     # ArmPart, EyePart, MouthPart, BodyShapePart
├── Systems/       # Desacoplados via GameEvents
│   ├── Breeding/  # BreedingService, AsyncBreedingService, BreedingController
│   ├── Combat/    # CombatService, AsyncCombatService, CombatController
│   ├── Cloud/     # CloudSyncService, CloudCodeTester
│   ├── Furniture/ # BuildModeController, FurnitureService, PlacementGrid
│   └── Store/     # StoreManager, ShopCatalogSO, DeliveryBox
├── UI/            # UIManager, UIInputs, 12 panel controllers UITK
├── Player/        # PlayerInputs, PlayerController, BuildingInputs, PlayerAnimator
├── Interactables/ # PanelTrigger, ThrowableObject
└── World/         # MoriMochiAgent, NeedStation*, HotbarController, containers

CloudCode/         # Scripts JS server-side + .sched/.tr
MoriMonchiVault/   # Memory Bank (Obsidian)
```

### Patrones arquitectonicos clave

1. **Event Bus (GameEvents.cs)**: Comunicacion cross-system via eventos estaticos. El evento transporta el payload. NEVER referencias directas entre sistemas.
2. **Tres buses separados**: GameEvents (gameplay), UIManager events static (UI), Input events static (PlayerInputs/UIInputs/BuildingInputs).
3. **Singletons**: GameManager.Instance, CloudSaveService.Instance, NeedStationRegistry.Instance, StorageContainer.Instance, PartVisualBankSO.Current, BreedingAffinityTableSO.Current, InheritanceOddsTableSO.Current.
4. **Pipeline persistencia**: Mutacion → GameEvents → GameManager → SaveSystem (disco) → CloudSyncService (nube). Ningun gameplay script llama save/push directo.
5. **Aislamiento de input**: Tres action maps mutuamente excluyentes: Player, UI, Building. Solo uno activo a la vez.
6. **NeedsState rule**: Necesidades (Health/Energy/Affect) mutan cada frame en RAM. NO disparan RegistryChanged. Flush solo en quit/pause via GameManager.
7. **Odin serialization**: Todos los SO con diccionarios heredan de `SerializedScriptableObject` con `[OdinSerialize]`.

---

## Reglas de codigo (NO NEGOCIABLES)

1. **Desacoplamiento estricto via eventos**: Cada sistema independiente. Comunicacion cross-system solo por `GameEvents`. El evento transporta la data. Suscriptor NO busca `GameManager.Instance.Registry`.
2. **Persistencia solo por evento**: Ningun gameplay script llama `SaveSystem.SaveDatabase` ni `PushToCloud`. Solo emiten `GameEvents.RegistryChanged`. `GameManager` es el unico dueno de persistencia.
3. **Sin comentarios en codigo**: No anadir `//` ni `/* */` sin pedido expreso de Juan. La documentacion vive en el vault.
4. **Sin features adelantadas**: No implementar mecanicas hasta su etapa del roadmap.
5. **DNA como string ligero**: `ToStringID()`/`FromID()` son el contrato de red. Timestamp es metadata, no parte del genetic string.
6. **IDs de partes**: nunca pueden contener `-` (separador del DNA string).
7. **Odin siempre**: `SerializedScriptableObject` con `[OdinSerialize]` para diccionarios.
8. **Sin complejidad innecesaria**: No anadir campos, abstracciones ni features no pedidos. Tres lineas similares > abstraccion prematura.
9. **Desuscribir siempre**: `OnEnable` suscribe, `OnDisable` desuscribe. Un `event static` mantiene vivo al suscriptor (leak + excepcion al disparar sobre objeto destruido).

---

## Eventos (GameEvents.cs)

| Evento | Quien dispara |
|--------|---------------|
| `OnRegistryChanged` | toda mutacion de gameplay |
| `OnRegistryReloaded` | CloudSyncService tras pull/reset (UI-only, sin push) |
| `OnCreatureMinted` | GameManager.MintRandomCreature |
| `OnCombatCompleted` | combate local |
| `OnCombatLogged` | AsyncCombatService.ApplyResult |
| `OnBreedingCompleted` | breeding local + async |
| `OnFurnitureChanged` | FurnitureService (toda mutacion) |
| `OnFurnitureReloaded` | reload furniture (clear+resync, sin push) |

Eventos UI viven en `UIManager` como `static event Action` (separados de GameEvents). Action maps Player/UI mutuamente excluyentes, conmutados en `OnUIFocusChanged`. Detalle en [[MoriMonchiVault/Index/05 - UI System]] y [[MoriMonchiVault/Index/07 - Persistence & Identity]].

---

## Roadmap (status compacto)

| Etapa | Estado |
|-------|--------|
| 1.1 Arquitectura genetica + DNA + Databases | Completado |
| 1.2 Visualizador de criaturas | Grilla inspector completado, falta 3D |
| 1.3 Sistema de Breeding | Local completado, refinamientos pendientes |
| 2.1 Sistema de Estadisticas | BaseStats + stats por pieza completado |
| 2.2 Combate local + Battle Log | Completado |
| 2.3 Integracion UGS (async battles) | Completado |
| 2.4 Breeding Async (timer server-side) | Completado |
| 2.5 Vida en Escena (NavMesh + personalidad) | Completado |
| 3.1 Tienda Local (furniture + economia) | Completado (pendiente CurrentStock en cloud + deploy get-server-time.js) |
| 3.2 Mercado Online | Pendiente |

Detalle por feature en [[MoriMonchiVault/Index/02 - Genetics & Breeding]], [[MoriMonchiVault/Index/03 - Combat]], [[MoriMonchiVault/Index/06 - Player & World]]. Pendientes en [[MoriMonchiVault/Index/08 - Known Bugs & Checkpoints]].

---

## Workflow de mantenimiento de documentacion

| Capa | Dueno | Cuando |
|------|-------|--------|
| **Notion** (diseno) | Juan | Decision de diseno nueva, pregunta resuelta. Yo NO toco Notion. |
| **MoriMonchiVault/** (implementacion) | IA (a pedido) | Cambio contrato publico, quirk tecnico nuevo, sub-etapa cerrada, script renombrado/movido. |
| **CLAUDE.md** (nucleo) | IA (a pedido) | Regla nueva, cambio de stack, roadmap status flip, nuevo archivo top-level del vault. |
| **09 - Active Context** | IA (cada sesion) | Apertura y cierre de sesion. |

**Regla operativa**: al cerrar sesion, propongo que actualizar (lista corta con justificacion), Juan valida, yo aplico.

**09 - Active Context debe listar explicitamente** al final: que archivos `.cs` se modificaron y que archivos se crearon. Esto permite que un agente post-sesion actualice los ScriptNodes/ y vault correspondiente.

Si solo se arreglo un bug menor sin cambiar diseno/contratos → no actualizar vault (el git log basta).

**Backup del CLAUDE.md original**: `ClaudeOld.md` en la raiz (no leer salvo migracion).
