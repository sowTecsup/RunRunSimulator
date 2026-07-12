---
tags: [script, world, anchor]
---

# MoriMochiContainer

**Ruta:** `World/Containers/MoriMochiContainer.cs`

**Responsabilidad:** Corral base con `BoxCollider` trigger. Implementa `IAnchorPlace` para generalizar "lugar donde una criatura está anclada". En `Start()` deriva el `AnchorKey` del `PlacedFurnitureMarker` y se auto-registra en `AnchorRegistry`. En `OnDestroy()` desregistra.

Admite criaturas lanzadas (`OnTriggerEnter`) o soltadas dentro (`OnTriggerStay`) hasta `capacity`. Rebota si está lleno (`BounceOut`). `Admit()` es la entrada del jugador (lanzada): estampa `LocationKey`/`LocationSlot` en el DNA y persiste via `GameEvents.RegistryChanged`. `Release()` es el retiro del jugador (agarrada): limpia el ancla y persiste. `DetachOccupant()` es el ciclo de vida silencioso (pool/reinit): desregista del censo pero NO persiste.

Expone `Occupants` (IReadOnlyList) y tabla `OccupantInfos` para inspector (nombre/género/**rol** S39). `Claim()` protegido es compartido por admisión y `BreedingContainer`. `EnterConfinement()` confina al agente (cambia areaMask).

## Cambios S39

**OccupantInfo struct:**
- Antes: `{ Name, Gender, Personality }`
- Ahora: `{ Name, Gender, Role }` — muestra el Role de combate, no Personality

**RefreshOccupantInfos:**
```csharp
public List<OccupantInfo> OccupantInfos => occupants
    .Where(a => a != null && a.DNA != null)
    .Select(a => new OccupantInfo 
    { 
        Name = a.DNA.CustomName, 
        Gender = a.DNA.Gender, 
        Role = a.DNA.Role  // S39: era Personality
    })
    .ToList();
```

**Tabla Inspector:**
La tabla de ocupantes en Odin Inspector ahora muestra:
- Nombre
- Género (glyph ♂/♀)
- **Rol** ("Protector", "Agresivo", "Empático") en lugar de Personalidad

## Campos Principales

| Campo | Tipo | Propósito |
|-------|------|----------|
| `area` | BoxCollider | Trigger del corral (inspeccionado o auto-grabbed en Awake). |
| `anchorKey` | string | Clave del lugar (furniture cell "x_y" o nombre si no hay marker). Derivada en Start. |
| `capacity` | int | Máximo ocupantes. |
| `occupants` | List<MoriMochiAgent> | Censo (agregado por Claim, removido por Release/DetachOccupant). |

## API pública (incluye IAnchorPlace)

| Método | Firma | Propósito |
|--------|-------|----------|
| `AnchorKey` { get; } | string | Property: clave del lugar (IAnchorPlace). |
| `AnchorPosition(int slot)` | Vector3 | IAnchorPlace: retorna `Center` (dónde el spawner deposita el cuerpo). |
| `TryReclaim(MoriMochiAgent agent, int slot)` | bool | IAnchorPlace: confina el agente via `Claim()`. Retorna false si lleno/ya dentro/confinement falla. |
| `Claim(MoriMochiAgent agent)` | bool (protected) | Confina y registra ocupante. Compartido por admisión (jugador) y reclaim (carga). |
| `Admit(MoriMochiAgent agent)` | void (private) | Admisión por lanzamiento: valida confinement, estampa LocationKey/-1, persiste. |
| `Release(MoriMochiAgent agent)` | void (virtual) | Retiro por agarrada del jugador: limpia LocationKey/-1, persiste. Base para BreedingContainer. |
| `DetachOccupant(MoriMochiAgent agent)` | void | Desacoplamiento silencioso (pool/reinit): remueve del censo sin persistir. |
| `Occupants` { get; } | IReadOnlyList<MoriMochiAgent> | Censo actual. |
| `OccupantInfos` { get; } | List<OccupantInfo> | Tabla Odin con nombre/género/**rol** (S39) |
| `Center` { get; } | Vector3 | Centro del trigger (para acarreo/courtship/birth launch). |
| `InteriorBounds` { get; } | Bounds | Bounds del trigger. |
| `IsFull` { get; } | bool | `occupants.Count >= capacity`. |

## OccupantInfo struct (S39)

```csharp
public struct OccupantInfo
{
    [ReadOnly] public string         Name;
    [ReadOnly] public CreatureGender Gender;
    [ReadOnly] public Role           Role;  // S39: was Personality
}
```

## Conexiones

- **`AnchorRegistry`**: Se registra en `Start()`, desregistra en `OnDestroy()`.
- **`PlacedFurnitureMarker`**: El contenedor lee su `AnchorCell` en `Start()` para derivar la clave.
- **`MoriMochiAgent`**: Confina via `EnterConfinement()`. Agente llama `Release()` en `OnGrab`. 
- **`GameEvents`**: Dispara `RegistryChanged` en `Admit()`/`Release()` (persiste).
- **`MoriMochiSpawner`**: Consulta registry para `TryReclaim()` en carga.
- **`BreedingContainer`**: Hereda y llama `base.Start()/OnDestroy()` para ancla automática.
- **`StoreContainer`**: Hereda, gestiona ocupantes NPCs aparte (array `usePointOccupants`).

## Notas de Implementación

- `LocationKey` = "" indica criatura suelta (no anclada).
- Entrada por jugador (`Admit`) persiste; ciclo de vida (`DetachOccupant`) no. Retiro por jugador (`Release`) persiste.
- Confinamiento falla si el piso del corral no está pintado con el área de cría y horneado (bake) — se devuelve a física.
- Virtual `Release()` permite a subclases (BreedingContainer) cancelar breeding al retirar.
- **S39 cambio:** Tabla OccupantInfos ahora muestra Role en lugar de Personality.

**Vinculado a:** [[Index/06 - Player & World]]
