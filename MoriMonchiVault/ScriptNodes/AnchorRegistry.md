---
tags: [script, world]
---

# AnchorRegistry.cs

**Ruta:** `World/Containers/AnchorRegistry.cs`

## Responsabilidad

Índice runtime de lugares donde las criaturas pueden estar ancladas (cría, estante de tienda, corrales normales). Generaliza la relación "este MoriMochi vive AQUÍ" para que al cargar una criatura anchada se coloque DIRECTAMENTE en su lugar (sin ser lanzada por el cañón). Registro estático mantiene un diccionario `AnchorKey` → `MoriMochiContainer`. Los lugares se auto-registran en `Start` y desregistran en `OnDestroy`, sin gestión manual.

## API pública del registro

| Método | Firma | Propósito |
|--------|-------|----------|
| `Register(MoriMochiContainer)` | `void` | Registra un lugar (invocado en Start del lugar). Ignora si `place == null` o `AnchorKey` es vacío. Clave puede ser sobreescrita (re-register de la misma key con distinta instancia). |
| `Unregister(MoriMochiContainer)` | `void` | Desregistra un lugar (invocado en OnDestroy). Sólo remueve si la instancia almacenada es la MISMA (evita clobbering si OnDestroy dispara fuera de orden). |
| `TryGet(string key, out MoriMochiContainer place)` | bool | Busca un lugar por clave. Retorna false si la clave es vacía o no existe. Tipado como `out` (patrón estándar). |

## Cambios S93

- **Removido:** interfaz `IAnchorPlace` (contrato de tipos generalizados). Ahora el registro es directamente `Dictionary<string, MoriMochiContainer>`.
- **Simplificación:** todos los tipos ancla (corral, cría, tienda) heredan de `MoriMochiContainer` e implementan los métodos públicos `AnchorKey`, `AnchorPosition()`, `TryReclaim()` directamente (duck typing sin interfaz formal).

## Conexiones

- **`MoriMochiContainer`, `BreedingContainer`, `StoreContainer`**: Se auto-registran en Start(), derivan `AnchorKey` del `PlacedFurnitureMarker` (o custom key via `SetAnchorKey()`), se desregistran en OnDestroy().
- **`MoriMochiSpawner`**: Consulta `AnchorRegistry.TryGet(dna.LocationKey)` en `SpawnOne()` para colocar criaturas anchadas via `TryPlaceAtAnchor()`. Si el lugar desapareció, cae al cañón.
- **`CreatureDNA`**: Persiste `LocationKey`/`LocationSlot` en el DNA (""/-1 si suelto).

## Notas de implementación

- Estático (no MonoBehaviour singleton) → patrón como `NeedStationRegistry` (mismo dominio World, sin ciclo de vida que gestionar).
- Un lugar con `AnchorKey == ""` es ignorado (no es un ancla válida, típicamente precarga).
- On-load (`OnRegistryReloaded`), el spawner consulta el registro ANTES de lanzar cañón.
