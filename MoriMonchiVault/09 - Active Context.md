---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-11 (sesión 6)
**Foco**: Visual Assembler — Etapa 1.2 completada. Ensamblaje 3D de MoriMonchis desde DNA en runtime.

| Archivo | Qué cambió |
|---------|-----------|
| `World/BodyPartJoint.cs` *(nuevo)* | Script por prefab de parte: `isMirror` + `insertionJoint` + gizmo (cyan=mirror, amarillo=single) |
| `World/MoriMonchiVisualizer.cs` *(nuevo)* | Ensambla el modelo; tiene los 6 sockets; botón Setup crea los child Transforms |
| `World/MoriMonchiController.cs` *(nuevo)* | Facade: wires Agent + Visualizer sin que se conozcan entre sí |
| `World/MoriMochiSpawner.cs` | Usa `MoriMonchiController` en lugar de `MoriMochiAgent` directamente |
| `World/MoriMochiAgent.cs` | Removido: `bodyRenderer`, `ApplyTint()`, auto-find en Awake |
| `Data/PartVisualBankSO.cs` *(nuevo)* | SO diccionario de prefabs por slot; Populate from DB + Fill Defaults |
| `Core/GameManager.cs` | Campo `partVisualBank` + property `PartVisualBank` |

**Archivos a borrar manualmente en Unity:**
- `World/BodyShapeJoints.cs` — diseño descartado, código muerto

---

## Arquitectura del Visual Assembler (diseño final)

```
MoriMonchiVisualizer (en agent root)
  ├── modelRoot → hijo "Model" del prefab
  ├── bodySocket    → hijo de modelRoot, creado con botón Setup
  ├── armSocketL    → hijo de modelRoot
  ├── armSocketR    → hijo de modelRoot
  ├── eyeSocketL    → hijo de modelRoot
  ├── eyeSocketR    → hijo de modelRoot
  └── mouthSocket   → hijo de modelRoot

BodyPartJoint (en cada prefab de parte)
  ├── isMirror       → true = mismo prefab en L y R; R flipa localScale.x
  └── insertionJoint → Transform hijo que se alinea al socket origin (null = pivot propio)
```

**Flujo de ensamblaje:**
1. Visualizer tiene los 6 socket Transforms pre-ubicados (Setup button los crea una vez)
2. `Assemble(dna, bank)` → instancia cada parte como hijo de su socket
3. Alinea el `insertionJoint` del prefab al origen del socket (`localPos = -insertionJoint.localPos`)
4. Si `isMirror = true`: segunda instancia en socket R con `localScale.x *= -1`
5. Body renderer detectado via `GetComponentInChildren<Renderer>()` solo en el body
6. Color primario + tint de personalidad vía `MaterialPropertyBlock`

**Lección de diseño clave:** el body prefab no sabe nada sobre las otras partes. El Visualizer ES el mapa de sockets. Cada parte solo conoce su propio punto de inserción.

---

## Setup pendiente en Unity (código ✅ — solo editor)

### Visual Assembler (Etapa 1.2)

| Paso | Estado |
|------|--------|
| Crear `PartVisualBankSO` asset | ⬜ |
| Asignar en `GameManager` | ⬜ |
| Crear prefabs placeholder (cubos/esferas) con `BodyPartJoint` por slot | ⬜ |
| Popular el banco con esos placeholders | ⬜ |
| Prefab del MoriMochi: agregar `MoriMonchiVisualizer` + `MoriMonchiController`, click Setup | ⬜ |
| Posicionar los 6 sockets en el prefab en sus ubicaciones reales | ⬜ |
| Verificar ensamblaje al spawnear | ⬜ |

### Deuda técnica anterior (persistente)

- **CurrentStock en Cloud Save**: `StoreShopData.CurrentStock` no se persiste en cloud (volátil por sesión).
- **Batalla instantánea**: mostrar `"Instantánea"` en Tab 3 de CombatPanel.
- **Ordenar Resultados** (Tab 3) de más antiguo a más nuevo por `QueuedAt`.
- Redeploy cloud: `run-combat.js`, `process-matchmaking.js`, `get-queue-status.js`, `dequeue-combat.js`.
- Bloquear `TryLift` de corral ocupado en `BuildModeController`/`FurnitureService`.
- Cablear `FlushToCloud()` en el logout de `CloudSyncService`.

---

## Backlog de sesiones — Próximas implementaciones

> Al completar una sesión, moverla a "Sesión anterior" y marcar su etapa en el roadmap de `CLAUDE.md`.

---

### Sesión próxima — StoreContainer · Etapa 3.1 extensión · **Sonnet**

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

### Sesión 3 — BreedingContainer · Etapa 1.3 extensión · **Sonnet**

Depende del Visual Assembler para el impacto visual completo.
**Preguntas de diseño resueltas** (ya no requiere Opus):

1. **Matriz de compatibilidad**: `BreedingCompatibilityChartSO` — matriz simétrica de `float [0-1]` entre personalidades.
2. **Mecánica de trigger**: timer cada X segundos tira dados entre TODOS los agentes del container; empareja a los ganadores. El timer arranca cuando hay al menos la cantidad mínima de ocupantes.
3. **Múltiples pares**: sí — pueden coexistir múltiples pares activos simultáneamente.
4. **Breeding local** en esta etapa (no async — queda para 3.2).

**Estructura:**

```
BreedingCompatibilityChartSO
  + [OdinSerialize] Dictionary<(Personality, Personality), float> compatibilityMatrix
  + [Button] Populate Defaults
  + float GetCompatibility(Personality a, Personality b)

BreedingContainer : MoriMochiContainer
  + [SerializeField] BreedingCompatibilityChartSO chart
  + [SerializeField] int  minOccupantsToStart       // timer no corre si hay menos
  + [SerializeField] float rollIntervalSeconds       // cada cuánto se tiran los dados
  → StartRolling() cuando se alcanza minOccupants
  → RollPairings(): evalúa todos los pares Male+Female presentes, roll contra chart
  → StartBreedingSequence(agentA, agentB):
      agentes navegan uno hacia el otro
      spawn indicador corazón world-space
      timer configurable
      al completar → BreedingService.Breed() → GameEvents.OnBreedingCompleted
```

**UI — `BreedingContainerPanelUITK`:**
- Lista de pares activos con timer countdown
- Capacidad inicial de prueba: 2 (1 par posible)

---

## Sesiones anteriores completadas

| Sesión | Fecha | Foco | Estado |
|--------|-------|------|--------|
| Sesión 5 | 2026-06-04 | Petting system — follow/react cooldown, hint NameTag, IInteractable en MoriMochiAgent | ✅ |
| Sesión 4 | — | Tienda local (furniture + economía, StoreManager, ShopCatalogSO) | ✅ |
| Sesión 3 | — | Vida en escena (NavMesh + personalidad) | ✅ |
| Sesión 6 | 2026-06-11 | Visual Assembler (Etapa 1.2) | ✅ |

---

## Cómo usar esta nota en sesiones futuras

1. Leer este archivo primero (después del `CLAUDE.md`).
2. Actualizar "Sesión actual" con fecha + foco + tabla de archivos.
3. Avanzar la primera sesión del backlog y eliminarla de la lista al completarla.

Si el `Active Context` queda desactualizado, tratarlo como **stale** — el código y los archivos del vault son autoritativos.
