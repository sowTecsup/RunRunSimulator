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
├── Index/                     ← Notas principales por dominio (01-11)
└── ScriptNodes/               ← Un nodo por script .cs
```

---

## 🧭 Quick Routing (task → read these first)

| Task | Read in `Index/` | Then read in `ScriptNodes/` |
|------|-------------------|-----------------------------|
| **DNA, parts, databases** | [[Index/02 - Genetics & Breeding]] | [[CreatureDNA]], [[BodyPart]], [[PartDatabaseSO]] |
| **Breeding mechanic** | [[Index/02 - Genetics & Breeding]] | [[BreedingService]], [[BreedingAffinityTableSO]], [[InheritanceOddsTableSO]], [[BreedingContainer]] |
| **Visual assembly (3D)** | [[Index/02 - Genetics & Breeding]] | [[MoriMonchiVisualizer]], [[BodyPartJoint]], [[PartVisualBankSO]] |
| **Local combat** | [[Index/03 - Combat]] | [[CombatService]], [[CombatRecord]], [[CombatTurn]] |
| **Async combat (UGS)** | [[Index/03 - Combat]], [[Index/04 - UGS & Cloud]] | [[AsyncCombatService]], [[CloudSyncService]] |
| **Auth & Cloud Save** | [[Index/04 - UGS & Cloud]], [[Index/07 - Persistence & Identity]] | [[CloudSyncService]], [[SaveSystem]], [[GameManager]] |
| **UI panels & navigation** | [[Index/05 - UI System]] | [[UIManager]], [[UIInputs]], [[PanelTrigger]] |
| **Player FP controller** | [[Index/06 - Player & World]] | [[PlayerInputs]], [[PlayerController]] |
| **Creature AI (NavMesh)** | [[Index/06 - Player & World]] | [[MoriMochiAgent]], [[NeedStationRegistry]], [[PersonalityProfileSO]] |
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
| **Pilares del rediseño + LA MECÁNICA (tablero de desvíos · archipiélago · genes=conectores)** ⚠️ DRAFT | [[Index/18 - Pilares del Rediseno (Draft)]] | — |

---

## 🏗️ Directory Structure (source code)

```
Assets/RunRunSimulator/Scripts/
├── Core/          # GameManager, GameEvents, SaveSystem, Enums, Interfaces
├── Data/          # CreatureDNA, BodyPart, databases, SOs
│   ├── Databases/ # ArmDatabaseSO, EyeDatabaseSO, etc.
│   └── Parts/     # ArmPart, EyePart, MouthPart, BodyShapePart
├── Systems/       # Desacoplados vía GameEvents
│   ├── Breeding/  # BreedingService, AsyncBreedingService
│   ├── Combat/    # CombatService, AsyncCombatService
│   ├── Cloud/     # CloudSyncService, CloudCodeTester
│   ├── Furniture/ # BuildModeController, FurnitureService, PlacementGrid
│   └── Store/     # StoreManager, ShopCatalogSO, DeliveryBox
├── UI/            # UIManager, UIInputs, 12 panel controllers
├── Player/        # PlayerInputs, PlayerController, BuildingInputs
├── Interactables/ # PanelTrigger, ThrowableObject
└── World/         # MoriMochiAgent, NeedStation, HotbarController, containers
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
| Personality, tint | [[PersonalityProfileSO]], [[MoriMochiAgent]] |
| Combat, fight, battle | [[CombatService]], [[AsyncCombatService]] |
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
