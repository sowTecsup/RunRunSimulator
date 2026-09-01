---
tags: [script, furniture, spawning]
---

# FurnitureSpawner.cs

**Ruta:** `Systems/Furniture/FurnitureSpawner.cs`

**Responsabilidad:** Instancia/remueve muebles en escena desde `FurnitureRegistrySO`. Escucha `GameEvents.OnFurnitureReloaded`. Tras instanciar cada mueble, resuelve la definición via `FurnitureDefinitionSO.GetByID()` y entrega anchor key al componente `MoriMochiContainer.SetAnchorKey()` para que los monchis sepan dónde anclarse.

**S93:** Usa `GetByID()` (método heredado de KeyedDatabaseSO). Vinculación de anchor key con SetAnchorKey().

## Métodos Principales

- Spawn furniture prefabs desde registry
- Resolve definición por ID via database
- Set anchor key en MoriMochiContainer

## Event Handling

- `OnFurnitureReloaded` — trigger para resync escena con registry (cloud pull)

## Vinculado a

- [[Index/10 - Furniture & Building]]

**Conexiones:** [[FurnitureRegistrySO]], [[FurnitureDefinitionSO]], [[GameEvents]], [[MoriMochiContainer]], [[PlacedFurniture]]

