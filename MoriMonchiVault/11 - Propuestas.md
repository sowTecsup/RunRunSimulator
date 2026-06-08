---
tags: [memory-bank, propuestas, refactor, auditoria]
---

# 11 — Propuestas de Refactor

> Relacionados: [[09 - Active Context]] (estado actual de la sesión), [[00 - Index]] (mapa general del vault).

## 🔴 Prioridad Alta

---

### Propuesta 1 — Eliminar controladores legacy FirstPersonController + ThirdPersonController

**Qué es**: Dos scripts en `Assets/RunRunSimulator/Scripts/Player/` que implementan controladores de personaje completos pero no pertenecen al proyecto actual.

| Script | Líneas | Contenido |
|--------|--------|-----------|
| `FirstPersonController.cs` | ~100 | FP con dash, inputs directos via `InputSystem`, lógica de movimiento standalone. |
| `ThirdPersonController.cs` | ~350 | TP con wall-run, turrets, granadas, cámara orbital — mecánicas de un shooter/action. |

**Por qué es problema**:
- **Código muerto**: coexisten con el sistema real (`PlayerController` + `PlayerInputs` + `PlayerAnimator`), que ya está integrado vía el bus de eventos y `IUINavigable`/`BuildingInputs`.
- **Ruido cognitivo**: cada búsqueda o refactor del sistema de player real encuentra estos archivos como falsos positivos.
- **Mantenimiento**: cualquier cambio en sistemas compartidos (física, input, cámara) requiere revisar si estos prototipos se ven afectados.

**Propuesta**:
1. Verificar que ningún prefab o escena referencie `FirstPersonController` o `ThirdPersonController` (búsqueda global de referencias).
2. Si no hay referencias activas → eliminar ambos archivos + confirmar build exitoso.
3. Si hay referencias → migrar a `PlayerController` y eliminar después.

**Prioridad**: 🔴 Alta — riesgo bajo, beneficio inmediato de limpieza.

---

### Propuesta 2 — Separar UI de dominio en BreedingController y CombatController

**Qué es**: `BreedingController.cs` y `CombatController.cs` en `Systems/` mezclan lógica de dominio con campos serializados para inputs de debug (strings de creatureIDs), botones Odin de test, y manipulación directa del `CreatureRegistrySO`.

**Por qué es problema**:
- Viola la regla del CLAUDE.md: **sin complejidad innecesaria** — los campos de debug serializados contaminan el inspector y el contrato público del MonoBehaviour.
- Viola la separación de responsabilidades: la lógica de breeding/combat debería estar en servicios (`BreedingService`, `CombatService`) y estos controllers deberían ser solo UI o thin orchestration.
- Dificulta testing: la lógica de dominio está acoplada al ciclo de vida de MonoBehaviour.

**Propuesta**:
1. Extraer toda lógica de dominio (validación de padres, cálculo de herencia, aplicación de resultado de combate) a los servicios existentes (`BreedingService.Breed()`, `AsyncCombatService.ApplyResult()`).
2. Los controllers quedan como **thin wrappers** que toman inputs del usuario, llaman al servicio, y reaccionan a eventos (`OnBreedingCompleted`, `OnCombatLogged`).
3. Eliminar campos serializados de debug (IDs de criatura, botones de test). Si se necesitan para el editor, usar `[Button]` en un `Service` con `[Conditional("UNITY_EDITOR")]`.
4. Renombrar a `BreedingPanelHandler` / `CombatPanelHandler` si son exclusivamente UI.

**Prioridad**: 🔴 Alta — toca la arquitectura central del juego (breeding + combate).

---

### Propuesta 3 — Eliminar singleton estático frágil en ScriptableObjects

**Qué es**: `CombatManagerSO.Current`, `InheritanceOddsTableSO.Current`, `PersonalityProfileSO.Current` se asignan a sí mismos en `OnEnable` (`Current = this`), creando un singleton estático accesible desde cualquier parte del código.

**Por qué es problema**:
- **Frágil**: si dos instancias del SO existen en el proyecto (carga duplicada de asset, prefab mal referenciado), `Current` se sobrescribe silenciosamente y el código en otro lugar recibe la instancia incorrecta.
- **Oculto**: la dependencia no es visible desde el Inspector ni desde el constructor — cualquier script puede llamar `CombatManagerSO.Current` sin que la dependencia esté declarada.
- **Dificulta testing**: no se puede sustituir la instancia por un mock fácilmente.

**Propuesta**:
1. Reemplazar `Current` por inyección directa de referencia (arrastrar el SO al slot del Inspector) en cada MonoBehaviour que lo necesite.
2. Si hay muchos consumidores, centralizar en un `ServiceLocator` o `ScriptableObjectRegistry` que se inicialice en el `GameManager` y se pase por evento a quien lo necesite.
3. Mantener `Current` solo como **fallback de editor** envuelto en `#if UNITY_EDITOR` con un log de warning si se detecta duplicado.
4. Los SOs que son puramente datos (sin estado mutable) pueden quedarse como referencia directa — el problema es solo la asignación automática.

**Prioridad**: 🔴 Alta — bug latente que puede causar datos corruptos en producción.

---

## 🟡 Prioridad Media

---

### Propuesta 4 — Slim down GameManager

**Qué es**: `GameManager.cs` en `Core/` orquestra múltiples responsabilidades: referencia directa a `CreatureRegistrySO`, inicialización de `CloudSyncService`, minteo de criaturas (`MintRandomCreature`), persistencia (save + push), manejo de escenas (`TeleportToScene`), y suscripción a eventos.

**Por qué es problema**:
- **Monolítico**: concentra demasiadas responsabilidades en una sola clase, violando el principio de responsabilidad única.
- **Acoplamiento**: cualquier cambio en cloud, persistence, breeding, o escenas requiere tocar `GameManager`.
- **Referencia directa a Singleton**: `GameManager.Instance` es llamado desde muchos lugares, lo que el CLAUDE.md desalienta explícitamente (los eventos deben transportar la data).

**Propuesta**:
1. **Mover `MintRandomCreature()`** a `CreatureRegistrySO` o a un `CreatureFactory` standalone.
2. **Mover lógica de persistencia** (save + push) a un flujo puramente basado en eventos: `OnRegistryChanged` → `SaveSystem` + `CloudSyncService` se suscriben independientemente.
3. `GameManager` se queda como **orquestrador de inicialización** (setup de SOs, suscripción de servicios, startup de cloud) y punto de entrada de la escena.
4. Eliminar o reducir dependencias directas a `Instance` en otros scripts — reemplazar con payload de eventos.

**Prioridad**: 🟡 Media — no es urgente, pero facilitará la evolución futura (Etapa 3.x, tienda, mercado online).

---

### Propuesta 5 — Namespacing consistente

**Qué es**: ~80% de los scripts en `Assets/RunRunSimulator/Scripts/` no tienen namespace. Algunos usan `namespace Systems.Combat`, `namespace Systems.Breeding`, `namespace Core`, pero es inconsistente entre carpetas y archivos similares.

**Por qué es problema**:
- **Colisiones potenciales**: clases como `CreatureDNA`, `GameEvents`, `SaveSystem` son nombres genéricos que podrían colisionar con otras librerías.
- **Navegación**: en el editor de C# (Rider/VS), los namespaces agrupan clases relacionadas y facilitan el autocompletado.
- **Estándar del proyecto**: el CLAUDE.md especifica estructura de carpetas pero no namespaces — definirlos formaliza la arquitectura.

**Propuesta**:
1. Definir jerarquía:
   - `RunRunSimulator.Core` — GameManager, GameEvents, SaveSystem, CreatureRegistrySO
   - `RunRunSimulator.Data` — CreatureDNA, CreatureStats, PartDefinitionSO, databases
   - `RunRunSimulator.Systems.Combat` — CombatService, AsyncCombatService, CombatManagerSO
   - `RunRunSimulator.Systems.Breeding` — BreedingService, InheritanceOddsTableSO
   - `RunRunSimulator.Systems.Cloud` — CloudSyncService, CloudCodeTester
   - `RunRunSimulator.Systems.Furniture` — FurnitureService, FurnitureSpawner, FurnitureRegistrySO
   - `RunRunSimulator.Player` — PlayerController, PlayerInputs, PlayerAnimator
   - `RunRunSimulator.World` — MoriMochiSpawner, MoriMochiAgent, NameTag, NeedStation
   - `RunRunSimulator.UI` — UIManager, paneles, IUINavigable
   - `RunRunSimulator.Interactables` — IInteractable, IThrowable, PanelTrigger, ThrowableObject
2. Agregar `namespace` a cada archivo en bloque (todos los archivos de una carpeta a la vez).
3. No tocar lógica interna — solo agregar `namespace ... { }` y los `using` necesarios.

**Prioridad**: 🟡 Media — mejora organizativa sin impacto funcional.

---

### Propuesta 6 — Estandarizar regiones y comentarios

**Qué es**: El código usa mezcla de estilos para organizar secciones dentro de un archivo: algunos usan `// ── Lifecycle ──`, otros usan `#region Lifecycle`, otros no tienen separación. Similar para comentarios de clase (XML docs vs `//` simple).

**Por qué es problema**:
- **Inconsistencia visual**: cada archivo se organiza distinto, aumentando carga cognitiva al saltar entre scripts.
- **Mantenimiento**: al tocar un archivo, no está claro qué convención seguir.

**Propuesta**:
1. Estandarizar en `#region` / `#endregion` con nombres consistentes:
   - `#region Lifecycle` — Awake, OnEnable, Start, OnDisable, OnDestroy
   - `#region Public Methods`
   - `#region Private Methods`
   - `#region Event Handlers`
2. Reemplazar `// ── ... ──` por `#region` en todos los archivos.
3. Agregar XML doc (`/// <summary>`) en métodos públicos de API. Los privados pueden quedar sin doc.

**Prioridad**: 🟡 Media — puramente cosmético/infraestructura.

---

## 🟢 Prioridad Baja

---

### Propuesta 7 — Visibilidad de métodos botón Odin

**Qué es**: Varios métodos están marcados como `public` únicamente porque un botón `[Button]` de Odin en el Inspector los necesita visibles. Ejemplos: `SignInAnonButton`, `SignInButton`, `ResetProgressButton` en `CloudSyncService`; `TeleportToScene` en `GameManager`.

**Por qué es problema**:
- **API inflada**: métodos que solo deberían existir en editor o debug aparecen como parte del contrato público de la clase.
- **Riesgo en builds release**: un método `public async void ResetProgressButton()` puede ser llamado accidentalmente desde código de gameplay o desde un evento mal suscrito.

**Propuesta**:
1. Envolver métodos de debug/editor con `#if UNITY_EDITOR` (no con `[Conditional]` porque los botones Odin igual los muestran).
2. Cambiar visibilidad a `internal` o `private` donde sea posible.
3. Para botones de Odin que llaman a lógica real (ej: `PushButton()` → `PushAsync()`), mantener el botón pero delegar a un método `public` con nombre semántico (`PushAsync`) mientras el botón mismo es `private` o `internal`.

**Prioridad**: 🟢 Baja — no afecta funcionalidad actual, pero limpia el API surface de cara a Etapa 3.x (multiplayer, mercado online).

---

## Resumen de prioridades

| # | Propuesta | Prioridad | Impacto | Riesgo |
|---|-----------|-----------|---------|--------|
| 1 | Eliminar controladores legacy | 🔴 Alta | Limpieza | Bajo |
| 2 | Separar UI de dominio (Breeding/Combat) | 🔴 Alta | Arquitectura | Medio |
| 3 | Eliminar singleton estático en SOs | 🔴 Alta | Estabilidad | Medio |
| 4 | Slim down GameManager | 🟡 Media | Arquitectura | Medio |
| 5 | Namespacing consistente | 🟡 Media | Organización | Bajo |
| 6 | Estandarizar regiones/comentarios | 🟡 Media | Legibilidad | Bajo |
| 7 | Visibilidad métodos botón Odin | 🟢 Baja | API surface | Bajo |
