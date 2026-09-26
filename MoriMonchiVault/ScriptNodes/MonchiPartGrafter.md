---
tags: [script, visual, utility, genetics, modular]
---

# MonchiPartGrafter.cs

**Ruta:** `World/Creatures/MonchiPartGrafter.cs`

**Responsabilidad:** Utilidad estática (sin estado) para injertar prefabs de partes FBX modulares en un cuerpo MoriMochi instanciado. Remapea huesos por nombre, recalcula bindposes (transformación matriz bone world-to-local), hereda rootBone y localBounds del renderer más grande del cuerpo, reparenta los SkinnedMeshRenderers al bodyInstance y destruye la jerarquía sobrante de la parte. No modifica el prefab de parte — trabaja sobre copias instantiadas. **S134 NEW**.

## Método Público

| Método | Parámetros | Descripción |
|--------|-----------|-------------|
| `Graft(GameObject, GameObject, List<SkinnedMeshRenderer>)` | `partPrefab, bodyInstance, into` | Injertar partPrefab en bodyInstance. Recorre todos SkinnedMeshRenderers dentro de partInstance, remapea sus huesos a los del cuerpo, recalcula bindposes, toma rootBone de bodyInstance, copia localBounds escaladas (x2.5), reparenta renderers al bodyInstance y añade a lista `into`. Limpia partInstance tras completar. |

## Flujo de Injerto (Graft)

1. **Instancia la parte** en el cuerpo (parente temporal) con posición/rotación/escala identity
2. **Recolecta huesos del cuerpo** en diccionario (nombre → Transform), excluyendo aquellos ya dentro de partInstance y evitando duplicados
3. **Selecciona boundsSource** — busca el SkinnedMeshRenderer más grande del cuerpo (sqrMagnitude máximo de localBounds), sirve para heredar rootBone consistente
4. **Para cada SkinnedMeshRenderer de la parte:**
   - **Remapea huesos:** itera sobre bones[] de la parte, busca cada uno en bodyBones diccionario. Si no existe → warning y fallback al hueso original. Si existe → toma del cuerpo.
   - **Recalcula bindposes:** copia mesh (instancia única), recompone bindPoses usando matriz `newBone.worldToLocalMatrix * partBone.localToWorldMatrix * oldBindPose` para cada hueso remapeado. Resultado: mesh esqueletado al espacio del cuerpo.
   - **Hereda rootBone y localBounds:** toma rootBone del boundsSource (si existe) o del renderer original (si boundsSource no tiene uno de cuerpo). LocalBounds heredan center + size*2.5 (escala de bounds para generar sombras dinámicas consistentes).
   - **Setea capas:** renderer hereda layer del bodyInstance (para culling/máscaras de cámara)
   - **Reparenta:** renderer se mueve como hijo directo de bodyInstance (en posición world preservada)
   - **Colecta:** se añade a lista `into`
5. **Desactiva y destruye partInstance** (ya no necesaria, renderers están reparentados)

## Constantes

| Constante | Valor | Descripción |
|-----------|-------|-------------|
| `BoundsScale` | 2.5f | Multiplicador de localBounds heredados (controla tamaño sombra dinámica, mayor = sombra más grande) |

## Vinculado a

- [[Index/02 - Genetics & Breeding]] — partes modulares en genetica
- [[Index/10 - Visualization]] — ensamblado visual modular (S134)

## Conexiones

- [[MonchiVisualizer.GraftPart()]] — orquestador que llama Graft tras desactivar renderers baked
- [[MonchiVisualBankSO.GetPartMesh()]] — obtiene prefab de parte (ID → GameObject FBX con Armature + SkinnedMeshRenderers)

## Notas S134

- **Modular assembly:** permite composición dinámica de MoriMonchis combinando HornID+BackID+WingID sin duplicar meshes de cuerpo base
- **Bindpose recalculo:** crítico — transforma bindposes al espacio del nuevo armature (solver de "hueso perdido → fallback + warning")
- **BoundsScale x2.5:** empírico, ajustado para que sombras dinámicas de partes injertadas sean legibles
- **Sin estado:** static + parámetros → testeble, reutilizable, sin side effects ocultos
