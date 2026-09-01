---
tags: [script, ui, service, singleton, graphics, camera]
---

# MonchiLivePortrait.cs

**Ruta:** `UI/MonchiLivePortrait.cs`

**Responsabilidad (S57d):** Singleton runtime cámara live para retrato en vivo de MoriMochis spawneados en el mundo. Hermano de MonchiPortraitService dentro de GameScene (GO `MonchiPortraitStudio`). **Aislamiento por capas (S57d):** Filma la criatura **EN VIVO** cuando su carta de detalle está abierta mediante la técnica de layer culling — comienza (Begin) moviendo todo el subtree del ModelRoot de la criatura a la capa dedicada `MonchiFocus` (slot 10), guardando los layers originales transform-por-transform, y al finalizar (End) restaura exactamente cada layer original. La LiveCamera tiene culling mask solo-`MonchiFocus` y clear color SolidColor con alpha 0 → retrato = solo la criatura animándose sobre fondo transparente, sin oclusores ni fondo del mundo. La Main Camera también renderiza MonchiFocus así que en el mundo se ve la criatura normal en simultáneo. **Evasión de oclusores eliminada (S57d):** la técnica de layer aislamiento hace innecesaria la revisión de líneas de visión (la cámara no renderiza oclusores, ve a través de todo). Búsqueda de criatura por UniqueID en `MoriMochiSpawner.Instance.SpawnedEntries` mediante la propiedad **`MoriMonchiController.Visualizer`** (nueva en S57d) para acceder al ModelRoot sin GetComponentInChildren. Autogestionada via LateUpdate: desactiva cámara y repinta foto estática vía `MonchiPortraitUI.Apply` si el elemento fue removido, está oculto (helper `IsHidden` que camina ancestros mirando `resolvedStyle.display == None`), o la criatura despawneó — cero acople con los caminos de cierre del panel. Representación pura, sin persistencia ni GameEvents. **S93:** Singleton compacto (sin cambios de responsabilidad).

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|----------|
| `liveCamera` | `Camera` | Cámara dedicada (disabled por defecto), renderiza a `rt` |
| `textureSize` | `int` | Tamaño RenderTexture (512px default) |
| `framePadding` | `float` | Multiplicador radio bounds → encuadre (0.9 default, más compacto) |
| `cameraPitch` | `float` | Eje X rotación cámara (12° default) |
| `cameraYaw` | `float` | Eje Y rotación cámara (155° default — vista 3/4) |
| `followDamp` | `float` | Factor damping seguimiento suave (8f default) |

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|----------|
| `Instance` | `static MonchiLivePortrait` | Singleton |
| `rt` | `RenderTexture` | Target render (ARGB32, 16 bits depth), creado en Awake |
| `element` | `VisualElement` | UIElement retrato activo siendo filmado (null si no hay live) |
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
- Obtiene ModelRoot vía `kv.Value.Visualizer.ModelRoot` (S57d)
- Retorna false si no encuentra o criatura está inactiva

### `End() → void`

Desactiva cámara, repinta VisualElement con foto estática vía `MonchiPortraitUI.Apply`, restaura layers originales, limpia estado live.

## Lifecycle

**Awake:**
- Singleton pattern: si Instance ya existe (y ≠ this), Destroy(gameObject) y retorna
- Asigna `Instance = this`
- Crea RenderTexture(textureSize, textureSize, 16, ARGB32)
- Asigna `liveCamera.targetTexture = rt`
- Deshabilita `liveCamera.enabled = false` (activada solo en Begin)
- Cachea `focusLayer = LayerMask.NameToLayer("MonchiFocus")` (S57d)

**LateUpdate:**
- Valida estado live; si alguna precondición falla, llama `End()` (transición a foto estática)
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
- `Begin(VisualElement, CreatureDNA)` desde `MonchiPortraitUI.ApplyLive`
- Lookup criatura + visualizer en `MoriMochiSpawner.Instance.SpawnedEntries` y `controller.Visualizer`

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
- **S93:** Singleton en forma compacta (sin cambios de funcionalidad)
