---
tags: [script, furniture, service]
---

# FurnitureService.cs

**Ruta:** `Systems/Furniture/FurnitureService.cs`

**Responsabilidad:** CRUD de muebles: place, remove, rotate. Modifica `FurnitureRegistrySO`, dispara `GameEvents.OnFurnitureChanged()` para persistencia automática. Resuelve definiciones via `FurnitureDefinitionSO.GetByID()`.

**S93:** Usa `GetByID()` para lookups de definición.

## Métodos Principales

- `Place(furnitureId, position, rotation, ...)` — Añade a registry
- `Remove(furnitureId)` — Borra de registry
- `Rotate(furnitureId, newRotation)` — Actualiza rotación

## Event Pattern

Cada mutación → `GameEvents.OnFurnitureChanged(registry)` → GameManager persiste + cloud push

## Vinculado a

- [[Index/10 - Furniture & Building]]

**Conexiones:** [[FurnitureRegistrySO]], [[FurnitureSpawner]], [[PlacementGrid]], [[GameEvents]], [[BuildModeController]], [[FurnitureDefinitionSO]]

