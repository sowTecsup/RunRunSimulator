---
tags: [script, world, expedition, planning]
---

# ArenaCastPlanner.cs

**Ruta:** `World/Expedition/ArenaCastPlanner.cs`

**Responsabilidad:** Planificador de elenco que construye criaturas a spawnear (Roster vs LocalSave), aplica órdenes rivales. S120: integra SelectedIds. S122: asigna Role a rivales. **S124:** Propiedad `RivalsEnabled` (desactiva IA rival en Buff); `Clamp(role, base)` sin parámetro rules.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `void Prepare(int roomSeed, int castSeed, int freeCount)` | Construye planned; si SelectedIds, fuerza esas 3 |
| `void SetPlayerOrders(int index, ArenaOrders o)` | Actualiza órdenes entrada |
| `void SelectLocal(IReadOnlyList<CreatureDNA> picks)` | Carga selección explícita |
| `void SetMode(ArenaCastMode mode)` | Setter modo |
| `IReadOnlyList<string> GetTeamIds(ExpeditionTeam team)` | IDs por equipo |

## Propiedades

| Propiedad | Descripción |
|-----------|-------------|
| `IReadOnlyList<ArenaCastEntry> Planned { get; }` | Elenco |
| `ArenaCastMode Mode` | Roster/LocalSave |
| `bool RivalsEnabled` | **(S124)** True por default; desactiva IA rival si Buff |
| `IReadOnlyList<CreatureDNA> LocalPool { get; }` | Pool local |

## Flujo Prepare (S120-S124)

1. Si SelectedIds no vacío: busca 3 criaturas jugador
2. Rival: FromRoster/mint con Role (base abierta)
3. Si RivalsEnabled=false (Buff): IA rival desactiva

## Cambios S124

- `RivalsEnabled` property — setter por ArenaSandbox.SetFloor()
- `Clamp(role, base)` — sin parámetro rules

## Invariantes

- Clamping automático
- SelectedIds + LocalPool = elenco forzado
- Role determinístico por seed

## Vinculado a

[[Index/24 - Puente Tienda-Arena]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ExpeditionHandoff]], [[ArenaSandbox]], [[ArenaBases]], [[ArenaOrderRules]]
