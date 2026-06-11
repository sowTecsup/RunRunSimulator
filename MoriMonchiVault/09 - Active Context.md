---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-11 (sesión 5)
**Foco**: Petting system — follow/react cooldown, hint en NameTag, `IInteractable` en `MoriMochiAgent`, `TryPetNearbyCreature` vía OverlapSphere en `PlayerController`.

| Archivo | Qué cambió |
|---------|-----------|
| `World/MoriMochiAgent.cs` | `IInteractable` · cooldowns · `IsInFriendlyReaction/CanBePetted/IsBeingPetted` · `IsPlayerFacingMe()` · `Interact()` |
| `World/NameTag.cs` | `petHintLabel` · hints "Petting..." / "[E] Acariciar" |
| `Player/PlayerController.cs` | `creatureLayer` · `TryPetNearbyCreature()` (OverlapSphere) |
| `UI Toolkit/NameTagUITK.uxml` | Label `pet-hint-label` |
| `UI Toolkit/NameTagUITKStyle.uss` | `.tag__pet-hint` (amarillo, oculto por defecto) |

---

## Setup pendiente en Unity (código ✅ — solo editor)

### 1 · Assets (crear en Project)

| Asset | Pasos |
|-------|-------|
| `ItemDatabase` SO | Clic der → Create → RunRunSimulator → Item Database |
| `ItemDefinition` × N | Uno por producto vendible. Furniture: asignar `FurnitureDef` (bridge F#). WorldProp: asignar `Prefab` + `Category`. |
| `PlayerInventory` SO | Create → RunRunSimulator → Player Inventory |
| **Validate & Sync IDs** | Abrir `ItemDatabase`, arrastrar defs al buffer → botón **Populate from Buffer** → **Validate & Sync IDs** |

### 2 · Prefabs (crear/modificar)

| Prefab | Componentes requeridos |
|--------|----------------------|
| **WorldProp** | Rigidbody + `ThrowableObject` + `WorldPropInstance` · layer del `grabMask` |
| **DeliveryBox** | Malla + collider sólido (grabMask) + `DeliveryBox` |
| **Furniture** | (ya existentes) — sin cambios |

### 3 · Objetos de escena

| GameObject | Componentes / asignaciones nuevas |
|-----------|----------------------------------|
| **GameManager** | Asignar campo `furnitureRegistry` (FurnitureRegistrySO) + `inventory` (PlayerInventorySO) |
| **StoreManager** *(cambió)* | `StoreManager` → `catalog` (**ShopCatalogSO**, ya NO lista inline) + `deliveryBoxPrefab` + `deliverySpawnPoint` |
| **StorageContainer** *(nuevo)* | Collider sólido (grabMask) + collider trigger (zona captura) + `StorageContainer` → `database` + `ejectPoint` |
| **HotbarController** *(nuevo)* | `HotbarController` → `database` (ItemDatabaseSO) + `handAnchor` (mismo que holdAnchor) |
| **Trigger de tienda** | Objeto con `PanelTrigger` (`panel = Store`) en el mostrador/computadora → tap E abre el StorePanel |

> ⚠️ **Asset nuevo `ShopCatalogSO`**: Create → RunRunSimulator → Shop Catalog. Llenar **Furniture for sale** (arrastrar FurnitureDef + precio/descuento) y **World props for sale** (arrastrar ItemDef + precio/descuento). Uno por tienda.
> ⚠️ Las **ItemDefinition** ya NO tienen opción Furniture (son WorldProp puro). El furniture se vende vía el catálogo de la tienda directo desde su `FurnitureDefinitionSO`.

### 4 · UI (UIDocuments + controllers)

| Panel | UIDocument | Standalone/UIManager | Asignaciones del controller |
|-------|-----------|---------------------|----------------------------|
| **Hotbar HUD** | Siempre activo, `StandartPanelSettings` | Standalone (no mapear) | `database` (ItemDatabaseSO) |
| **Storage** | Puede ser inactivo | UIManager → `UIPanelType.Storage` | `document`, `database` |
| **Store** *(nuevo)* | Puede ser inactivo | UIManager → `UIPanelType.Store` (**=6**) | `document`, `store` (StoreManager de escena) |
| **Info Overlay** *(nuevo)* | Siempre activo, picking-mode Ignore | Standalone (no mapear) | `document`, (opcional) editar `hints[]` |
| **Build Browser** | Puede ser inactivo | Standalone (no mapear) | `document`, `database` (FurnitureDatabaseSO), `buildMode` |

> ⚠️ El `BuildBrowserUITK` y el `InfoOverlayUITK` **NO** se registran en el dict de UIManager (no son panels focusables).
> El `StorePanelUITK` **SÍ** se mapea en el dict (`Store → su GameObject`), como Storage.

### 5 · Flujo de prueba

```
tap E sobre el trigger de tienda → abre StorePanel (UIManager)
  → ←→ cambia tab (Muebles / Objetos / Consumibles) · ↑↓ fila · Submit/Comprar
    → [Muebles]  → inventory.AddFurniture(F#) → aparece en el Build Browser
    → [Objetos / Consumibles] → DeliveryBox cae en deliverySpawnPoint
       → tap E → spawna el WorldProp en escena
         → tap E sobre WorldProp → hotbar · wheel navega · hold E lanza · Q suelta · click usa
           → lanzar hacia StorageContainer → auto-captura → tap E abre Storage → Sacar (Q) ejecta
```

---

## Próximos pasos

### Deuda técnica (pendientes de código)

- **CurrentStock en Cloud Save**: `StoreShopData.CurrentStock` no se persiste en cloud (volátil por sesión). Evaluar si es necesario antes de release.
- **Play-mode use effects**: `WorldPropCategory.Food`/`Medicine` → efecto en MoriMochi objetivo vía `OnItemUsed`. Etapa futura.
- **Batalla instantánea**: mostrar `"Instantánea"` en Tab 3 de CombatPanel.
- **Ordenar Resultados** (Tab 3) de más antiguo a más nuevo por `QueuedAt`.
- Redeploy cloud: `run-combat.js`, `process-matchmaking.js`, `get-queue-status.js`, `dequeue-combat.js`.
- Bloquear `TryLift` de corral ocupado en `BuildModeController`/`FurnitureService`.
- Cablear `FlushToCloud()` en el logout de `CloudSyncService`.

---

## Backlog de sesiones — Próximas implementaciones

> Estas sesiones se trabajan en orden. Al completar una, moverla a "Sesión anterior" en este archivo y marcar su etapa en el roadmap de `CLAUDE.md`.

---

### Sesión próxima — Visual Assembler · Etapa 1.2 · **Sonnet**

Implementación concreta con diseño claro — no hay decisiones de arquitectura abiertas.

**Objetivo**: leer un `CreatureDNA` y ensamblar el modelo 3D en runtime usando un banco de prefabs separado de los data SOs.

#### Paso 1 — `PartVisualBankSO` · (`Data/`)
- `SerializedScriptableObject` (Odin)
- 4 diccionarios `[OdinSerialize]`: `bodies`, `arms`, `eyes`, `mouths` (key = part ID, value = Prefab)
- Métodos: `GetBody(id)`, `GetArm(id)`, `GetEye(id)`, `GetMouth(id)` → `null` si no existe (fail-soft)
- `CreateAssetMenu: "RunRunSimulator/Databases/Part Visual Bank"`

#### Paso 2 — `BodyAnchorConfig` · (componente en el body prefab)
- Refs de Transform explícitas (más robusto que string lookups):
  - `Transform[] armAnchors` (2)
  - `Transform[] eyeAnchors` (2)
  - `Transform mouthAnchor` (1)
- Vive en el prefab del body part (hijo `Model` del agente MoriMochi)

#### Paso 3 — `CreatureModelAssembler` · (clase estática, `World/`)
```
static void Assemble(CreatureDNA dna, PartVisualBankSO bank, Transform modelRoot)
  → limpia hijos visuales de modelRoot
  → instancia body prefab → obtiene BodyAnchorConfig
  → instancia arm prefab en armAnchors[0] y [1]
  → instancia eye prefab en eyeAnchors[0] y [1]
  → instancia mouth prefab en mouthAnchor
  → aplica PrimaryColor (hex del DNA) al body MeshRenderer via MaterialPropertyBlock
  → devuelve el body MeshRenderer (para que MoriMochiAgent lo use como bodyRenderer)
```

#### Paso 4 — Wire en `MoriMochiAgent.Initialize()`
- `[SerializeField] PartVisualBankSO visualBank` en el inspector del agente
- Tras bindear el DNA: `CreatureModelAssembler.Assemble(dna, visualBank, modelTransform)`
- `bodyRenderer` apunta al renderer del body ensamblado (reemplaza el cubo placeholder)
- El tint de personalidad se aplica como overlay `MaterialPropertyBlock` sobre ese renderer

#### Test de la sesión
- Crear 2–3 prefabs placeholder por slot (cubos/esferas de colores distintos)
- Popular el `PartVisualBankSO` con esos placeholders
- Verificar ensamblaje correcto al spawnear en escena

> ⚠️ **Dependencia de arte**: los prefabs reales (FBX) llegan después. El assembler está diseñado para funcionar con cualquier prefab que tenga `BodyAnchorConfig`. Los placeholder cubos son suficientes para validar el sistema.

---

### Sesión 2 — StoreContainer · Etapa 3.1 extensión · **Sonnet**

No depende del Visual Assembler — puede adelantarse si conviene.

**Objetivo**: contenedor de exhibición donde los MoriMonchis confinados tienen sus needs satisfechas automáticamente. Gancho futuro para que NPCs pidan comprar criaturas expuestas.

```
StoreContainer : MoriMochiContainer
  + [SerializeField] float needsRestoreRate   // cada N segundos
  + [SerializeField] float restoreAmount      // monto por tick (Health/Energy/Affect)
  → coroutine AutoRestoreNeeds()
      foreach occupant: dna.Needs.AddHealth/Energy/Affect(restoreAmount)
  + event Action<CreatureDNA> OnOccupantRequested   // hook para NPC futuro (no implementar el NPC aún)
```

- Los NameTags existentes muestran needs en tiempo real → no requiere UI nueva.
- `OccupantDNAs` ya está en la clase base → expuesto para el sistema de compra NPC.

---

### Sesión 3 — BreedingContainer · Etapa 1.3 extensión · **Opus (diseño) → Sonnet (impl)**

Depende del Visual Assembler para el impacto visual completo. Requiere una mini-sesión de diseño antes de codear.

**Preguntas a resolver con Opus antes de implementar:**
1. `BreedingCompatibilityChartSO`: ¿matriz 6×6 de personalidades con `float probability`? ¿Score continuo o umbral binario? ¿Configurable por par o por personalidad?
2. ¿El container usa `BreedingService.Breed()` (local, sin anti-cheat) o el sistema async existente? → *Tentativa: local en esta etapa, async queda para Etapa 3.2.*
3. ¿Qué condición dispara el apareamiento? ¿Solo estar en el container? ¿Timer mínimo de convivencia?

**Estructura tentativa (post-diseño):**

```
BreedingCompatibilityChartSO
  + [OdinSerialize] Dictionary<(Personality, Personality), float> compatibilityMatrix
  + [Button] Populate Defaults
  + float GetCompatibility(Personality a, Personality b)

BreedingContainer : MoriMochiContainer
  + [SerializeField] BreedingCompatibilityChartSO chart
  + [SerializeField] float minCohabitationTime   // segundos antes de evaluar compatibilidad
  → OnOccupantAdded(): si hay par Male+Female → StartCompatibilityCheck()
  → CompatibilityCheck(): roll contra chart.GetCompatibility → si pasa → StartBreedingSequence(a, b)
  → StartBreedingSequence(agentA, agentB):
      agentes navegan uno hacia el otro (dentro de areaMask)
      spawn indicador corazón world-space sobre el par
      timer configurable
      al completar → BreedingService.Breed() → GameEvents.OnBreedingCompleted
```

**UI — `BreedingContainerPanelUITK`:**
- Lista de pares activos con timer countdown
- Escalable a múltiples pares simultáneos
- Capacidad inicial de prueba: 2 (1 par posible)

---

## Cómo usar esta nota en sesiones futuras

Cuando arranque una sesión nueva:
1. Leo este archivo primero (después del `CLAUDE.md`).
2. Actualizo "Sesión actual" con fecha + foco + tabla de archivos (breve).
3. Avanzo la primera sesión del backlog y la elimino de la lista al completarla.

Si el `Active Context` queda desactualizado (no se ha tocado en muchos días), tratarlo como **stale** — el código y los archivos del vault son autoritativos.
