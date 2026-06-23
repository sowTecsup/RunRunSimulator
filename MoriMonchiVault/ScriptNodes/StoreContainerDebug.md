---
tags: [script, debug, world]
---

# StoreContainerDebug.cs

**Ruta:** `World/Containers/StoreContainerDebug.cs`

**Responsabilidad:** Componente debug (patrón F3: API pública, refs Odin, sin acoplamiento a internals) sobre un StoreContainer. Muestra en inspector: lista de MMs adentro + lista de NPCs interactuando. Botón "Spawn Test Customer" → fuerza spawn inmediato.

**Requerimientos:**
- `[RequireComponent(typeof(StoreContainer))]`.
- `[DisallowMultipleComponent]`.

**Propiedades Odin:**
- `Inside` (ShowInInspector, ReadOnly, PropertyOrder 0) — TableList de filas:
  - `Name` (string) — nombre MM o "MM".
  - `Gender` (CreatureGender).
  - `Busy` (bool).
  - `Price` (int) — estimado vía `CustomerService.EstimateAverage()`.
  Itera `container.Occupants` (MoriMochiAgent[]).

- `Interacting` (ShowInInspector, ReadOnly, PropertyOrder 1) — TableList de filas:
  - `Archetype` (string) — nombre archetype o "Cliente".
  - `State` (NpcAgent.NpcState).
  - `Target` (string) — nombre MM que intenta comprar o "—".
  Filtra `NpcController.Instance.Active` por `npc.CurrentDisplay == container`.

- `SpawnTestCustomer()` (Button, PropertyOrder 2) — llama `NpcController.Instance.ForceSpawn()`.

**Internals:**
- `struct InsideRow` / `struct NpcRow` — data holders para Odin TableList.
- `Awake()` → caché `container`.
- Getters sin estado mutable (puro lectura cada frame).

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[StoreContainer]], [[NpcAgent]], [[NpcController]], [[CustomerService]], [[CreatureDNA]], [[CustomerArchetypeSO]]
