---
tags: [index, core]
---

# 00 — Index (AI Entry Point)

> Lee esto PRIMERO. Te dice exactamente qué archivos leer antes de tocar código.

---

## 📁 Vault Structure

```
MoriMonchiVault/
├── 00 - Index.md              ← ESTE ARCHIVO (entry point para IA)
├── Index/                     ← Notas principales por dominio y diseno (01-21 · la 20 se borro con el prototipo en S93 · 09b = digest historico S8-S88)
└── ScriptNodes/               ← Un nodo por script .cs (~200)
```

---

## 🧭 Quick Routing (task → read these first)

| Task | Read in `Index/` | Then read in `ScriptNodes/` |
|------|-------------------|-----------------------------|
| **DNA, parts, databases** | [[Index/02 - Genetics & Breeding]] | [[CreatureDNA]], [[BodyPart]], [[PartDatabaseSO]] |
| **Breeding mechanic** | [[Index/02 - Genetics & Breeding]] | [[BreedingService]], [[BreedingAffinityTableSO]], [[InheritanceOddsTableSO]], [[BreedingContainer]] |
| **Visual assembly (3D, Suriyun DragonSD) · animación viva (S98: gestos, fidgets, mirar, giros)** | [[Index/02 - Genetics & Breeding]], [[Index/23 - Arena Sandbox y Expedicion]] | [[MonchiVisualizer]], [[MonchiVisualBankSO]], [[DragonAnimationDriver]], [[MonchiLocomotionAnimator]], [[MonchiGestureDriver]], [[MonchiGestureSetSO]], [[MonchiGazeDriver]], [[MonchiMoodDriver]] |
| **DISEÑO VIGENTE: Linaje + Bajada Nocturna + mecánica "esquina a esquina" de Juan (S96-S97, DRAFT)** ⭐ | [[Index/22 - Bajada Nocturna y Linaje (Draft)]] (Parte 8 = S97: mecánica de Juan, tipos de parte, modelo de utilidad, etapas, plan de realismo) | — (diseño; la implementación está en la fila siguiente) |
| **Arena sandbox · expedición · guías visuales · elenco y equipos · choque físico v1 (S97-S100, Fase 1 ✅ · Fase 2 en curso · Etapa 3 pasos 1 y 3 ✅)** | [[Index/23 - Arena Sandbox y Expedicion]], [[Index/22 - Bajada Nocturna y Linaje (Draft)]] (8.6-8.9) | [[ArenaSandbox]], [[ArenaRosterSO]], [[AgentExpedition]], [[ExpeditionRulesSO]], [[ExpeditionRuleBase]], [[ArenaCueOverlay]], [[CueDrawer]], [[CueStyleSO]], [[MaterialPickup]], [[Perceivable]] (equipos), [[NameTag]] (colores por equipo), **S100:** [[AgentClash]], [[ClashMoveSO]], [[ClashTuningSO]], [[ArenaClashDev]] (botones), [[ArenaCameraDirector]] (cámara que se acerca al choque) |
| **Combate v3 Dragon RPS (S92-S95 — 🪦 FALLIDO S96, código en pie)** | [[Index/21 - Combate v3 - Dragon RPS]] | `Scripts/DragonRps/` (DragonRpsRules, DragonRpsMatch, DragonRpsSession, etc.) |
| **Combate prototipo táctico S77-S88 (NO VALIDÓ — DEMOLIDO S93)** | [[Index/09b - Session Digest (S8-S88)]] (timeline S77-S88); código, escena, assets, nota `Index/20` y ScriptNodes borrados — recuperables en git `3cc5eb5` | — |
| **Combate viejo 3v3 (DEMOLIDO S75 — solo historia)** | [[Index/03 - Combat]] (historico), [[Index/09b - Session Digest (S8-S88)]] | — |
| **Cloud sync (UGS)** | [[Index/04 - UGS & Cloud]] | [[CloudSyncService]], [[CloudAuth]], [[CloudSyncOps]] |
| **Auth & Cloud Save** | [[Index/04 - UGS & Cloud]], [[Index/07 - Persistence & Identity]] | [[CloudSyncService]], [[SaveSystem]], [[GameManager]] |
| **UI panels & navigation** | [[Index/05 - UI System]] | [[UIManager]], [[UIInputs]], [[PanelTrigger]] |
| **Player FP controller** | [[Index/06 - Player & World]] | [[PlayerInputs]], [[PlayerController]] |
| **Creature AI (NavMesh)** | [[Index/06 - Player & World]] | [[MoriMochiAgent]], [[AgentBrain]], [[NeedStationRegistry]] |
| **Needs system** | [[Index/06 - Player & World]] | [[NeedStation]], [[Feeder]], [[NeedsState]] |
| **Containers / pens** | [[Index/06 - Player & World]] | [[MoriMochiContainer]], [[StoreContainer]], [[BreedingContainer]] |
| **Hotbar / world props** | [[Index/06 - Player & World]] | [[HotbarController]], [[WorldPropInstance]], [[ThrowableObject]] |
| **Save/load & persistence** | [[Index/07 - Persistence & Identity]] | [[SaveSystem]], [[GameEvents]], [[GameManager]] |
| **Event bus architecture** | [[Index/07 - Persistence & Identity]] | [[GameEvents]] |
| **Building mode** | [[Index/10 - Furniture & Building]] | [[BuildModeController]], [[BuildingInputs]], [[PlacementGrid]] |
| **Store & economy** | [[Index/10 - Furniture & Building]] | [[StoreManager]], [[ShopCatalogSO]], [[DeliveryBox]] |
| **Furniture system** | [[Index/10 - Furniture & Building]] | [[FurnitureService]], [[FurnitureSpawner]], [[FurnitureDefinitionSO]] |
| **Known bugs / issues** | [[Index/08 - Known Bugs & Checkpoints]] | — |
| **Current work session** | [[Index/09 - Active Context]] | — |
| **Unity MCP (editor en vivo, escena, SOs, Play, ProBuilder)** | [[Index/12 - Unity MCP]] | — |
| **Dirección de diseño del combate (autobattler 3v3, roles)** | [[Index/13 - Combat Design Direction]] | — |
| **Prompts de referencias de arte (pixel kit "Diario del Pet Shop")** | [[Index/14 - Art Prompts]] | — |
| **Rumbo del proyecto: autobattler, mercado y pitch** | [[Index/15 - Theorycrafting S71 - Autobattler y Marketing]] | — |
| **Estado real de cada frente, acoples y cuellos de botella** | [[Index/16 - Diagnostico por Frentes]] | — |
| **Refundación del combate: lentes, géneros, formatos** | [[Index/17 - Refundacion del Combate]] | — |
| **Pilares del rediseño (día/noche · genes · ítem · Cutie Marks)** — Partes 7-8 (tablero) 🪦 descartadas S76 | [[Index/18 - Pilares del Rediseno (Draft)]] | — |
| **COMBATE NUEVO: Predictive Tactical Extraction (expedición · plantillas · secuencia · extracción)** ⚠️ DRAFT | [[Index/19 - Combate Nuevo - Predictive Tactical Extraction]] | — |
| **COMBATE v3: Dragon RPS (RPS rígido · deck 6 · mano 3 · 3 golpes)** ✅ FUENTE DE VERDAD de la mecánica de combate · **Parte 9 = plan de la DEMO JUGABLE E1-E5 (paleta + auditoría por etapa), S94 arranca en E1** | [[Index/21 - Combate v3 - Dragon RPS]] | — |
| **Invariantes rescatados de comentarios (S93)** — nota TEMPORAL hasta que los ScriptNodes los absorban | [[Index/09c - Invariantes rescatados de comentarios (S93)]] | — |

---

## 🏗️ Directory Structure (source code)

```
Assets/RunRunSimulator/Scripts/
├── DragonRps/     # combate v3 S92+ (logica pura, cero dependencias de UnityEngine)
├── Core/          # GameManager, GameEvents (10 eventos), SaveSystem, Interfaces · Enums/ (6 archivos desde S93)
├── Data/          # Genetics/ (CreatureDNA, registry) · Parts/ (Horn/Back/Wing/Face/BodyShape) · Databases/ · Equipment/ · Items/ · Social/ · Expedition/ (CueStyleSO, ExpeditionRuleBase, ExpeditionRulesSO — S97 · ArenaRosterSO S99 · ClashMoveSO, ClashTuningSO S100)
├── Systems/       # Desacoplados vía GameEvents
│   ├── Breeding/  # BreedingService, AsyncBreedingService
│   ├── Cloud/     # CloudSyncService (+ CloudAuth/CloudSyncOps)
│   ├── Customers/ # NPCs compradores (FSM, cola, negociación)
│   ├── Furniture/ # BuildModeController, FurnitureService, PlacementGrid
│   ├── Localization/ # Loc, LocEnumMaps
│   ├── Social/    # SocialGraphService
│   ├── Stats/     # stats/point-buy
│   └── Store/     # StoreManager, ShopCatalogSO, DeliveryBox
├── Editor/        # CreatureRegistryDevTools (menu MoriMonchi/Registry, tooling dev fuera del SO de datos, S93)
├── UI/            # UIManager, UIInputs, panel controllers UITK · CreatureDisplay + UiPanels (helpers compartidos, S93)
├── Player/        # PlayerInputs, PlayerController, BuildingInputs
├── Interactables/ # PanelTrigger, ThrowableObject
├── Shaders/       # MonchiCue.shader (guías vectoriales SDF, S97) · UIRingOverlay.shader
└── World/         # AI/ (MoriMochiAgent + colaboradores, AgentExpedition S97, equipos en Perceivable S99, AgentClash S100), Creatures/ (MonchiVisualizer, MonchiGestureDriver/MonchiGazeDriver S98), NeedStation, containers · Expedition/ (ArenaSandbox, ArenaCueOverlay, CueDrawer, MaterialPickup — S97 · ArenaClashDev, ArenaCameraDirector — S100)
```

---

## 🔌 Architectural Patterns (non-negotiable)

### 1. Event Bus (`GameEvents.cs`)
Cross-system communication is ALWAYS via static events. NEVER direct references between systems.
- **Gameplay mutations** → `GameEvents.OnRegistryChanged` / `OnFurnitureChanged`
- **UI events** → `UIManager` static events (separate bus)
- **Input events** → `PlayerInputs` / `UIInputs` / `BuildingInputs` static events
- **Rule**: event carries the payload; subscriber never fetches a singleton

### 2. Singleton pattern
Used for runtime services: `GameManager.Instance`, `CloudSaveService.Instance`, `NeedStationRegistry.Instance`, `StorageContainer.Instance`, `PartVisualBankSO.Current`, `BreedingAffinityTableSO.Current`, `InheritanceOddsTableSO.Current`

### 3. Persistence pipeline
```
Mutation → GameEvents → GameManager → SaveSystem (disk) → CloudSyncService (cloud)
```
No gameplay script calls `SaveDatabase` or `PushToCloud` directly.

### 4. Input isolation
Three mutually exclusive action maps: `Player`, `UI`, `Building`. Only one active at a time. `UIInputs` enables/disables based on `UIManager.OnUIFocusChanged`.

### 5. NeedsState special rule
`NeedsState` (Health/Energy/Affect) lives inside `CreatureDNA` and mutates every frame. It MUST NOT fire `GameEvents.RegistryChanged` (would spam cloud save). Flushes only on quit/pause via `GameManager`.

---

## 🏷️ Tag / Keyword Index

| Keyword | Look in |
|---------|---------|
| DNA, genetic string, part ID | [[CreatureDNA]], [[PartDatabaseSO]] |
| Stats (HP/Attack/Speed) | [[CreatureStats]], [[BodyPart]] |
| Personality (diales Sociability/Boldness), tint | [[MoriMochiAgent]], [[AgentBrain]] |
| Por qué criar, linaje, adopción, bajada nocturna, salas, ferales, nervio, esquina a esquina, tipos de parte, modelo de utilidad | [[Index/22 - Bajada Nocturna y Linaje (Draft)]] (dirección vigente desde S96, draft; Parte 8 = S97) |
| Arena, sandbox, expedición, minerales, reglas de expedición, guías visuales, cues, anillo de percepción, ruta curva, choque, embestida, picada, coletazo, mareado | [[Index/23 - Arena Sandbox y Expedicion]], [[AgentExpedition]], [[ArenaCueOverlay]], [[AgentClash]] (S100) |
| Combat, fight, battle, RPS, dragon duel | [[Index/21 - Combate v3 - Dragon RPS]] (🪦 fallido S96, ver Index/22; el prototipo táctico quedó histórico en S92; el 3v3 viejo murió en S75) |
| Breeding, cross, hatch | [[BreedingService]], [[BreedingContainer]] |
| Affinity, compatibility | [[BreedingAffinityTableSO]] |
| Inheritance odds | [[InheritanceOddsTableSO]] |
| Cloud save, push, pull | [[CloudSyncService]], [[SaveSystem]] |
| UI panel, stack, focus | [[UIManager]], [[UIInputs]] |
| Building mode, ghost, grid | [[BuildModeController]], [[PlacementGrid]] |
| Store, buy, sell, catalog | [[StoreManager]], [[ShopCatalogSO]] |
| Need, hunger, energy, affect | [[NeedStation]], [[NeedsState]] |
| NavMesh, agent, AI | [[MoriMochiAgent]] |
| Container, corral, pen | [[MoriMochiContainer]] |
| Hotbar, slot, throw | [[HotbarController]], [[ThrowableObject]] |
| Interact, IInteractable | [[Interfaces]], [[PanelTrigger]] |
| Persist, save, load, JSON | [[SaveSystem]], [[GameManager]] |

---

## ⚙️ How to use this vault (for AI)

1. **Identify your task** in the Quick Routing table above
2. **Read the Index/ note** for design intent, flow, and invariants
3. **Read the specific ScriptNodes** for implementation details:
   - Each node has: responsibility, file path, connected scripts
   - Follow the `[[wikilinks]]` to understand relationships
4. **Check [[Index/08 - Known Bugs & Checkpoints]]** for active issues
5. **Check [[Index/09 - Active Context]]** for current session state
6. **Only then read the `.cs` files** — you'll already know what they do

> 📍 **Design discussions** live in [Notion](https://www.notion.so/36cac10136a781819b74e176ed7c00d9). This vault is the code-focused distilled version.
