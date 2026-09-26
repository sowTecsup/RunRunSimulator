---
tags: [script, visual, database]
---

# MonchiVisualBankSO.cs

**Ruta:** `Data/Databases/MonchiVisualBankSO.cs`

**Responsabilidad:** Banco visual centralizado del modelo Suriyun. Mantiene la lista de cuerpos FBX prefabricados (`bodies` list) con posibilidad de sobrescrituras por BodyShape ID (`bodyOverrides` dict), el AnimatorController compartido, la lista de materiales gema para brillantes, referencia al MoodSet, y **S134:** diccionario de prefabs de partes modulares (`partMeshes` dict) indexados por Part ID. `GetBody(bodyShapeId)` devuelve determinísticamente (hash FNV-1a % count) el cuerpo correspondiente al BodyShapeID, priorizando overrides. `GetPartMesh(partId)` devuelve el prefab FBX de parte si existe en el diccionario. `GetGem(uniqueId)` devuelve el material gema usando el mismo hash determinístico sobre uniqueID. Usa `StableHash()` interna para garantizar consistencia en replay/red.

## Métodos Públicos

| Método | Parámetros | Descripción |
|--------|-----------|-------------|
| `GetBody(string bodyShapeId)` | `bodyShapeId` | Retorna body prefab: busca override primero, sino aplica hash FNV-1a % count a lista bodies |
| `GetPartMesh(string partId)` | `partId` | **S134 NUEVO** Retorna prefab FBX de parte (ID → GameObject con Armature + SkinnedMeshRenderers); null si partId vacío o no en diccionario |
| `GetGem(string uniqueId)` | `uniqueId` | Retorna material gema: hash FNV-1a % count sobre gemMaterials |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `bodies` | `List<GameObject>` | Pool de body FBX base (Suriyun); GetBody hace hash determinístico sobre lista |
| `bodyOverrides` | `Dictionary<string, GameObject>` | Overrides explícitos por BodyShapeID (ej. "BS0" → custom body); prioridad sobre bodies list |
| `partMeshes` | `Dictionary<string, GameObject>` | **S134 NUEVO** Mapeo Part ID → prefab FBX de parte (ej. "H0" → horn FBX con Armature, "BK0" → back FBX, "W0" → wing FBX); GetPartMesh(partId) busca aquí |
| `animatorController` | `RuntimeAnimatorController` | Controller compartido asignado a Animator del body |
| `gemMaterials` | `List<Material>` | Pool de materiales brillantes (shiny); GetGem hace hash determinístico |
| `moodSet` | `MonchiMoodSetSO` | Reference al MoodSet para swapeo de materiales Face |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `AnimatorController` | `RuntimeAnimatorController` | Getter del controller |
| `MoodSet` | `MonchiMoodSetSO` | Getter del mood set |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `StableHash(string s)` | Calcula hash FNV-1a de string: seed 2166136261u, XOR cada char, mult 16777619u, retorna positivo (& 0x7FFFFFFF). Determinístico, no colisiona típicamente para IDs cortos (H0, BK1, W0, etc.). Usado por GetBody/GetGem para indexación modular. |

## Invariantes

- `bodies` list siempre tiene al menos 1 elemento (fallback si override falta o cuenta=0)
- `bodyOverrides` prioridad máxima: GetBody busca override primero
- `partMeshes` vacío = sin partes modulares (MonchiVisualizer.GraftPart fallback a baked)
- `gemMaterials` vacío = GetGem retorna null (MonchiVisualizer.ApplyLook fallback a fur genético)
- `moodSet` requerido por MonchiVisualizer.SetMood; null = silencio visual
- **S134:** GetPartMesh(null) → null; GetPartMesh("") → null; GetPartMesh("H0") → prefab o null (no existe)

## Cambios S134

**Diccionario partMeshes y GetPartMesh() — NUEVO:**

```csharp
[OdinSerialize]
[DictionaryDrawerSettings(KeyLabel = "Part ID", ValueLabel = "Part Mesh")]
private Dictionary<string, GameObject> partMeshes = new Dictionary<string, GameObject>();

public GameObject GetPartMesh(string partId)
{
    if (string.IsNullOrEmpty(partId))
        return null;
    
    if (partMeshes != null && partMeshes.TryGetValue(partId, out var partMesh))
        return partMesh;
    
    return null;
}
```

**Propósito:**
- Centraliza prefabs de partes modulares (Cuernos, Espalda, Alas) mapeados por Part ID
- MonchiVisualizer.GraftPart() consulta aquí para obtener prefab antes de injertar via MonchiPartGrafter
- IDs de partes no pueden contener "-" (separador del DNA string; regla S132)
- Ejemplos esperados: "H0", "H1" (cuernos), "BK0", "BK1", "BK2", "BK3" (espalda), "W0", "W1" (alas), "FC0", "FC1" (cara — si modular fuera soporte)

**Flujo S134:**
1. CreatureDNA (ej. "BSx-H1-BK2-W0-RRGGBB") incluye HornID, BackID, WingID
2. MonchiVisualizer.Assemble(dna) llama GraftPart(dna.HornID, "Horn"), etc.
3. GraftPart consulta bank.GetPartMesh("H1") → retorna prefab o null
4. Si no null: desactiva renderers "Horn*" baked, injerta FBX "H1" via MonchiPartGrafter.Graft()
5. Si null: GraftPart retorna silenciosamente, body mantiene baked "Horn*" renderer

**Impacto S134:**
- Sistema modular: cada creatura puede combinar 3 partes independientes (Horn, Back, Wing) sin duplicar body base
- Determinístico: mismo DNA = mismo visual (mismo BodyShapeID + HornID/BackID/WingID = mismas mallas injertadas)
- Escalable: añadir partes nuevas = añadir entrada al diccionario partMeshes en editor
- Fallback: partId vacío/nil o no en banco = body mantiene renderer baked (no falla, no warning)

**Editor Odin:**
- `[DictionaryDrawerSettings(KeyLabel = "Part ID", ValueLabel = "Part Mesh")]` → inspector legible con columnas Part ID | Part Mesh
- Drag-drop prefabs FBX en el diccionario directamente

## Vinculado a

- [[Index/02 - Genetics & Breeding]] — partes modulares en genética
- [[Index/10 - Visualization]] — ensamblado visual

## Conexiones

**Consumido por:**
- [[MonchiVisualizer.Assemble()]] → llama GetBody(bodyShapeId), GraftPart → GetPartMesh(partId)
- [[MonchiVisualizer.GraftPart()]] — consulta GetPartMesh per slot
- [[MonchiPartGrafter.Graft()]] — recibe prefab de GetPartMesh como entrada

**Entrada:**
- Editor: diccionarios bodies, bodyOverrides, partMeshes, gemMaterials, animatorController, moodSet

**Salida:**
- GetBody: body FBX base
- GetPartMesh: prefab FBX de parte (S134)
- GetGem: material shiny
- AnimatorController: RuntimeAnimatorController para Animator
- MoodSet: MonchiMoodSetSO para mood faces

## Notas S134

- **Modular assembly:** partMeshes es la fuente única de verdad para partes (no duplicar en otro SO)
- **Naming convención:** Part IDs sin guión (H0, BK1, W0); guión es separador en DNA string
- **Prefab reqs:** cada partMesh debe tener:
  - GameObject root con nombre obvio (ej. "Horn_H0", "Back_BK2", "Wing_W0")
  - Hijo Armature con Transform hierarchy de huesos
  - SkinnedMeshRenderer(s) dentro de Armature (ej. "Horn_*", "Back_*", "Wing_*", "Deco_RRGGBB_*")
  - Nombres de huesos = exactamente coincidentes con bodyInstance Armature (MonchiPartGrafter remapea por nombre)
- **Determinismo:** hash FNV-1a garantiza que ID → prefab siempre retorna lo mismo
- **Backup baked:** si partMesh nil/no existe → no falla, body simplemente usa renderer baked (graceful fallback)
