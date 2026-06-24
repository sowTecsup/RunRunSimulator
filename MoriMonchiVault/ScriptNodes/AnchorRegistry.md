---
tags: [script, world]
---

# AnchorRegistry.cs

**Ruta:** `World/Containers/AnchorRegistry.cs`

## Responsabilidad

Índice runtime de lugares donde las criaturas pueden estar ancladas (cría, estante de tienda, corrales normales). Generaliza la relación "este MoriMochi vive AQUÍ" para que al cargar una criatura anchada se coloque DIRECTAMENTE en su lugar (sin ser lanzada por el cañón). La interface `IAnchorPlace` define el contrato; el registro estático mantiene un diccionario `AnchorKey` → `IAnchorPlace`. Los lugares se auto-registran en `Start` y desregistran en `OnDestroy`, sin gestión manual.

## Interface IAnchorPlace

Implementadores: `MoriMochiContainer`, `BreedingContainer`, `StoreContainer`.

| Miembro | Tipo | Propósito |
|---------|------|----------|
| `AnchorKey` { get; } | string | Clave única del lugar (ej: "3_5" del PlacedFurnitureMarker). "" = no colocado. |
| `AnchorPosition(int slot)` | Vector3 | Dónde el spawner deposita el cuerpo ANTES de reclaman (típicamente el centro del lugar). |
| `TryReclaim(MoriMochiAgent agent, int slot)` | bool | Confina/sienta el agente aquí. Retorna false si no está listo (no hay espacio, piso no pintado). Sólo se invoca en carga si el DNA tenía LocationKey. |

## API pública del registro

| Método | Firma | Propósito |
|--------|-------|----------|
| `Register(IAnchorPlace)` | `void` | Registra un lugar (invocado en Start del lugar). Ignora si `place == null` o `AnchorKey` es vacío. Clave puede ser sobreescrita (re-register de la misma key con distinta instancia). |
| `Unregister(IAnchorPlace)` | `void` | Desregistra un lugar (invocado en OnDestroy). Sólo remueve si la instancia almacenada es la MISMA (evita clobbering si OnDestroy dispara fuera de orden). |
| `TryGet(string key, out IAnchorPlace place)` | bool | Busca un lugar por clave. Retorna false si la clave es vacía o no existe. Tipado como `out` (patrón estándar). |

## Conexiones

- **`MoriMochiContainer`, `BreedingContainer`, `StoreContainer`**: Implementan `IAnchorPlace`, derivan `AnchorKey` del `PlacedFurnitureMarker` en `Start`, se auto-registran.
- **`MoriMochiSpawner`**: Consulta `AnchorRegistry.TryGet(dna.LocationKey)` en `SpawnOne()` para colocar criaturas anchadas via `TryPlaceAtAnchor()`. Si el lugar desapareció, cae al cañón.
- **`CreatureDNA`**: Persiste `LocationKey`/`LocationSlot` en el DNA (""/-1 si suelto).

## Notas de implementación

- Estático (no MonoBehaviour singleton) → patrón como `NeedStationRegistry` (mismo dominio World, sin ciclo de vida que gestionar).
- Un lugar con `AnchorKey == ""` es ignorado (no es un ancla válida, típicamente precarga).
- On-load (`OnRegistryReloaded`), el spawner consulta el registro ANTES de lanzar cañón.
