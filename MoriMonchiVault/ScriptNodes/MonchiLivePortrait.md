---
tags: [script, ui, service, singleton, graphics, camera]
---

# MonchiLivePortrait.cs

**Ruta:** `UI/MonchiLivePortrait.cs`

**Responsabilidad (S57d):** Singleton runtime cámara live para retrato en vivo de MoriMochis spawneados en el mundo. Hermano de MonchiPortraitService dentro de GameScene (GO `MonchiPortraitStudio`). **Aislamiento por capas (S57d):** Filma la criatura **EN VIVO** cuando su carta de detalle está abierta mediante la técnica de layer culling — comienza (Begin) moviendo todo el subtree del ModelRoot de la criatura a la capa dedicada `MonchiFocus` (slot 10), guardando los layers originales transform-por-transform, y al finalizar (End) restaura exactamente cada layer original. La LiveCamera tiene culling mask solo-`MonchiFocus` y clear color SolidColor con alpha 0 → retrato = solo la criatura animándose sobre fondo transparente, sin oclusores ni fondo del mundo. La Main Camera también renderiza MonchiFocus así que en el mundo se ve la criatura normal en simultáneo. **Evasión de oclusores eliminada (S57d):** la técnica de layer aislamiento hace innecesaria la revisión de líneas de visión (la cámara no renderiza oclusores, ve a través de todo). Búsqueda de criatura por UniqueID en `MoriMochiSpawner.Instance.SpawnedEntries` mediante la propiedad **`MoriMonchiController.Visualizer`** (nueva en S57d) para acceder al ModelRoot sin GetComponentInChildren. Autogestionada via LateUpdate: desactiva cámara y repinta foto estática vía `MonchiPortraitUI.Apply` si el elemento fue removido, está oculto (helper `IsHidden` que camina ancestros mirando `resolvedStyle.display == None`), o la criatura despawneó — cero acople con los caminos de cierre del panel. Representación pura, sin persistencia ni GameEvents.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `liveCamera` | `Camera` | Cámara dedicada (disabled por defecto), renderiza a `rt` |
| `textureSize` | `int` | Tamaño RenderTexture (512px default) |
| `framePadding` | `float` | Multiplicador radio bounds → encuadre (0.9 default, más compacto) |
| `cameraPitch` | `float` | Eje X rotación cámara (12° default) |
| `cameraYaw` | `float` | Eje Y rotación cámara (155° default — vista 3/4) |
| `followDamp` | `float` | Factor damping seguimiento suave (8f default) |

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Instance` | `static MonchiLivePortrait` | Singleton |
| `rt` | `RenderTexture` | Target render (ARGB32, 16 bits depth), creado en Awake |
| `element` | `VisualElement` | UIElement portret activo siendo filmado (null si no hay live) |
| `dna` | `CreatureDNA` | DNA de la criatura siendo filmada (para fallback a foto si despawneó) |
| `target` | `Transform` | Transform del controller siendo filmado |
| `modelRoot` | `Transform` | Transform del ModelRoot de la criatura (obtenido vía `controller.Visualizer.ModelRoot`) |
| `focusLayer` | `int` | Layer ID de "MonchiFocus" (cached en Awake) |
| `originalLayers` | `List<(Transform t, int layer)>` | Capas originales de cada transform en el subtree, restauradas en End/auto-cierre |

## Métodos Públicos

### `Begin(VisualElement portraitElement, CreatureDNA portraitDna) → bool`

Inicia captura live: busca criatura en spawner, aísla layers via MonchiFocus, wirea cámara al VisualElement, renderiza a rt.

**Precondiciones validadas:**
- `portraitElement != null` (nodo UITK válido)
- `portraitDna != null` (DNA existe)
- `liveCamera != null` (serializado)
- `MoriMochiSpawner.Instance != null` (spawner disponible)

**Búsqueda de target:**
- Itera `MoriMochiSpawner.Instance.SpawnedEntries` (Dict[UniqueID, Creature controller])
- Busca entrada con `Key == portraitDna.UniqueID`
- Válida que `Value != null` y `gameObject.activeInHierarchy == true`
- **S57d: Obtiene ModelRoot vía `kv.Value.Visualizer.ModelRoot`** (nueva propiedad pública)
- Retorna false si no encuentra o criatura está inactiva

**Setup si encontrada:**
1. Si ya hay live anterior, llama `End()` (fallback a foto estática)
2. Cachea `element`, `dna`, `target`, `modelRoot`
3. **Aislamiento layer (S57d):**
   - Obtiene todos los Transforms del subtree: `modelRoot.GetComponentsInChildren<Transform>(true)`
   - Para cada transform: guarda `(t, t.gameObject.layer)` en `originalLayers`
   - Asigna `t.gameObject.layer = focusLayer` (MonchiFocus)
4. Habilita cámara, actualiza posición via `UpdateCameraTransform(1f)` (teleport)
5. Conecta `rt` al backgroundImage del VisualElement: `StyleBackground(Background.FromRenderTexture(rt))`
6. Limpia backgroundColor, configura `BackgroundSizeType.Contain`
7. Retorna true

**Si no hay criatura spawneada:** retorna false, caller cae a `Apply()` (foto fotomatón estática).

### `End() → void`

Desactiva cámara, repinta VisualElement con foto estática vía `MonchiPortraitUI.Apply`, restaura layers originales, limpia estado live.

**Lógica:**
1. Deshabilita liveCamera
2. Restaura layers: `RestoreLayers()` (restaura cada transform a su layer original)
3. Si element aún tiene panel (no fue destruido):
   - Llama `MonchiPortraitUI.Apply(element, dna)` — vuelve a la foto del fotomatón
4. Limpia `element = null`, `dna = null`, `target = null`, `modelRoot = null`

**Nota:** Es importante que el Apply caiga de manera transparente sin conocer que acabó el live.

## Métodos Privados

### `RestoreLayers() → void`

Restaura la capa original de cada Transform guardado en `originalLayers`, luego limpia la lista.

**Lógica:**
1. Para cada `(t, layer)` en `originalLayers`:
   - Si `t != null`: `t.gameObject.layer = layer`
2. `originalLayers.Clear()`

**Nota:** Validación de null en t previene errores si un transform fue destruido entre Begin y End.

### `UpdateCameraTransform(float lerpT) → void`

Cálculo de posición/rotación cámara: encuadre dinámico bounds, seguimiento suave.

**Pasos:**

1. **Compute bounds** de target:
   - Obtiene SkinnedMeshRenderers en `target.GetComponentsInChildren<SkinnedMeshRenderer>(true)`
   - Si existen, unifica bounds de todos
   - Si no hay renderers, fallback: `new Bounds(target.position + Vector3.up * 0.5f, Vector3.one)` (pivot estimado)

2. **Distancia encuadre:**
   - `radius = bounds.extents.magnitude * framePadding` (radio dilatado)
   - `dist = radius / sin(fov/2)` (trigonometría encuadre)

3. **Dirección base** relativa a rotación criatura:
   - `dir = target.rotation * Quaternion.Euler(cameraPitch, cameraYaw, 0) * Vector3.forward`
   - Así la cámara siempre filma 3/4 relativo a la cara del MoriMochi

4. **Interpolación suave (S57d — sin oclusión):**
   - `wanted = bounds.center - dir * dist`
   - `Lerp(currentPos, wanted, lerpT)` — damp suave sin comprobar oclusores

5. **Rotación:**
   - `LookRotation(bounds.center - camPos, Vector3.up)` — mira centro criatura

### `IsHidden(VisualElement ve) → static bool`

Valida si VisualElement está oculto (display: none) en ancestros.

**Lógica:**
- Camina `ve` → `ve.hierarchy.parent` hasta raíz
- Retorna true si algún ancestro (incluyendo ve) tiene `resolvedStyle.display == DisplayStyle.None`
- Retorna false si tree completo visible

**Nota:** `worldBound` no sirve aquí porque display:none en un ancestro conserva el último layout; helper camina display explícitamente.

## Lifecycle

**Awake:**
- Singleton pattern: si Instance ya existe (y ≠ this), Destroy(gameObject) y retorna
- Asigna `Instance = this`
- Crea RenderTexture(textureSize, textureSize, 16, ARGB32)
- Asigna `liveCamera.targetTexture = rt`
- Deshabilita `liveCamera.enabled = false` (activada solo en Begin)
- Cachea `focusLayer = LayerMask.NameToLayer("MonchiFocus")` (S57d)

**OnDestroy:**
- Si `Instance == this`, asigna `Instance = null`
- Limpia RenderTexture: `rt.Release()`, `Destroy(rt)` (previene leaks)

**LateUpdate:**
- Si `element == null`, retorna (no hay live activo)
- Valida estado live:
  - `element.panel == null` → elemento fue removido del árbol UI
  - `IsHidden(element)` → elemento oculto por ancestro
  - `target == null` → criatura desreferenciada
  - `modelRoot == null` → ModelRoot inaccesible
  - `!target.gameObject.activeInHierarchy` → criatura despawneó
- Si cualquiera es true:
  - Llama `End()` (transición a foto estática + restaura layers)
  - Retorna
- Si todo válido: llama `UpdateCameraTransform(Time.deltaTime * followDamp)` — seguimiento suave

## Vinculado a

- [[Index/05 - UI System]]
- [[MonchiPortraitService]] — hermano fotomatón (booth oculto, foto estática)
- [[MonchiPortraitUI]] — consumer (VisualElement.ApplyLive, fallback a Apply)
- [[MorimonchiDetailInfoUITK]] — consumer principal (Populate retrato header)
- [[MoriMochiSpawner]] — proveedor SpawnedEntries (lookup criatura por UniqueID)
- [[MoriMonchiController]] — acceso a Visualizer.ModelRoot (S57d)
- [[CreatureDNA]] — input DNA (UniqueID lookup)

## Conexiones

**Entrada:**
- `Begin(VisualElement, CreatureDNA)` desde `MonchiPortraitUI.ApplyLive` (que es llamada por MorimonchiDetailInfoUITK.Populate)
- Lookup criatura + visualizer en `MoriMochiSpawner.Instance.SpawnedEntries` (internal assembly access) y `controller.Visualizer` (S57d)

**Salida:**
- RenderTexture rt → backgroundImage VisualElement
- LateUpdate transición a foto estática vía `MonchiPortraitUI.Apply` + restaura layers (end automático)
- Layers de mundo restaurados exactos (S57d — sin pisar capas ajenas)

## Notas

- **Singleton autogestionado:** Awake asigna Instance; LateUpdate chequea validez y autocierra
- **Zero acople de cierre:** Panel no necesita llamar a End(); si se cierra/oculta, LateUpdate detecta y cierra automáticamente
- **Fallback transparente:** `ApplyLive` → Begin (éxito live) o Apply (fallback foto) — caller nunca sabe si es live
- **Aislamiento por layer (S57d):** La criatura se mueve a MonchiFocus, LiveCamera filma SOLO esa capa (culling mask), resto del mundo invisible. Al terminar, RestoreLayers restaura exactamente cada transform a su capa original.
- **Cámara clear alpha 0 (S57d):** Background transparente permite integración limpia en UI sin halos
- **Sin oclusión (S57d):** IsBlocked eliminado — layer aislamiento hace innecesaria la evasión dinámica
- **Bounds fallback:** Si no hay SkinnedMeshRenderers, estima pivot (head height) — evita crash si criatura es only-colliders
- **Damp suave:** followDamp multiplica deltaTime, genera transición 0.125s típico (8 * 0.016ms) — no instantáneo
- **RenderTexture lifecycle:** Creado Awake, limpiado OnDestroy — previene leak si panel persiste
- **RGBA32 matching:** rt ARGB32 = transparencia + color, coincide con BackgroundSize.Contain sin distorsión
