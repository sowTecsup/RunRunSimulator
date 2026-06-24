---
tags: [script, world]
---

# StoreContainer.cs

**Ruta:** `World/Containers/StoreContainer.cs`

## Responsabilidad

Vitrina de tienda que exhibe MoriMonchis para venta. Hereda `MoriMochiContainer`, por lo que implementa `IAnchorPlace` automáticamente: MoriMonchis colocados en estantes persisten `LocationKey` y se recolocan directo en carga. Restaura las 3 necesidades a `restoreRate/s`. Gestiona puntos de uso (use points) para NPCs clientes (patrón idéntico a `NeedStation`): navegación sin solapamiento, snappeo a NavMesh, reserva/libera slots.

## Cambios en S21

- No hay cambios lógicos en la clase. Hereda automáticamente la persistencia de ancla vía `MoriMochiContainer` (`LocationKey`/`LocationSlot` en DNA).
- Array privado `usePointOccupants` (antes sin nombre específico o implícito en `usePoints`): gestiona ocupación de NPC por slot.

## Campos principales

| Campo | Tipo | Propósito |
|-------|------|----------|
| `restoreRate` | float | Necesidades (salud/energía/afecto) restauradas por segundo a ocupantes. |
| `usePoints` | List<Transform> | Posiciones dónde se paran NPCs para examinar (snappeo a NavMesh). Si vacío → slot implícito en `transform.position`. |
| `usePointOccupants` | NpcAgent[] | Censo de ocupación de slots (null = libre). |

## API pública

| Método | Firma | Propósito |
|--------|-------|----------|
| `HasFreeUsePoint` { get; } | bool | True si hay un slot disponible. |
| `TryReserveUsePoint(NpcAgent, Vector3, int, float, out Vector3)` | bool | Reserva el slot más cercano a `from`, snappea a NavMesh, retorna posición. Re-llamada con el mismo agente retorna el slot ya reservado. |
| `ReleaseUsePoint(NpcAgent)` | void | Libera el slot del agente (tipicamente al salir de la tienda). |
| `OnEnable()` | void | Auto-registra en `StoreDisplayRegistry`. |
| `OnDisable()` | void | Auto-desregistra. |
| `Update()` | void | Restaura necesidades a ocupantes cada frame. |

## Conexiones

- **`MoriMochiContainer` (base)**: Hereda ancla, `Claim()`, `Occupants`, persistencia. MoriMonchis para venta estampa `LocationKey` automáticamente.
- **`AnchorRegistry`**: Registrado via `base.Start()` (del padre `MoriMochiContainer`).
- **`CreatureDNA`**: `LocationKey`/`LocationSlot` persiste (estante donde el MoriMochi está en venta).
- **`StoreDisplayRegistry`**: Se registra en `OnEnable()`, desregistra en `OnDisable()` (búsqueda de vitrinas por GameManager).
- **`NpcAgent`**: Consulta `HasFreeUsePoint` y usa `TryReserveUsePoint()/ReleaseUsePoint()` para navegación de clientes.
- **`MoriMochiSpawner`**: Consulta `AnchorRegistry` para colocar MoriMonchis en estantes en carga.

## Notas de implementación

- `SlotCount` → número de `usePoints` o 1 (implícito).
- `SlotPosition(i)` → posición del punto i, o `transform.position` si no existe.
- Gizmos: esferas amarillas (libres), rojas (ocupadas) + líneas al contenedor.
- S21: MoriMonchis en venta ahora persisten su estante (`LocationKey = AnchorKey` del contenedor) sin API nueva.

**Vinculado a:** [[Index/06 - Player & World]]
