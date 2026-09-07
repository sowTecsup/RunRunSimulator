---
tags: [script, world, expedition, planning]
---

# ArenaCastPlanner.cs

**Ruta:** `World/Expedition/ArenaCastPlanner.cs`

**Responsabilidad:** Planificador de elenco de arena que construye lista de criaturas a spawnear según modo (Roster vs LocalSave), aplica planes de **órdenes** rivales por semilla (S104), remembers cambios del jugador. S103: selección explícita. S104: órdenes clampeadas por DNA, ocupación y sitio derivados.

**Constructor:**
- `ArenaCastPlanner(CreatureRegistrySO registry, ArenaRosterSO roster, ExpeditionRulesSO rules)` — (S104) almacena rules para Clamp()

**Métodos públicos:**
- `void Prepare(int roomSeed, int castSeed, int freeCount)` — construye planned
- `void SetPlayerOrders(int index, ArenaOrders orders)` — (S104 NUEVO) actualiza entry[index].Orders, clampeado
- `void SetPlayerPlan(int index, Occupation occupation, ArenaSite site)` — (S103) legacy; traduce a Orders vía ArenaOrderRules.FromOccupation
- `void SelectLocal(IReadOnlyList<CreatureDNA> picks)` — (S103) carga selección explícita del picker
- `void ClearLocalSelection()` — (S103) limpia
- `void Clamp()` — (S104 NUEVO) reaplicaclamping a todos rivales vía ArenaOrderRules.Clamp

**Propiedades:**
- `IReadOnlyList<ArenaCastEntry> Planned { get; }`
- `ArenaCastMode Mode` — Roster o LocalSave
- `int LocalCount` — criaturas LocalSave
- `bool LocalAvailable`, `HasRoster`, `HasLocalSelection`
- `IReadOnlyList<CreatureDNA> LocalPool { get; }` — (S103)

**Constantes RivalPlans (S104 NUEVO: órdenes en lugar de ocupación):**
```
7 patrones de ArenaOrders para rivales, distribuidos por roomSeed % 7
Ejemplo: [Big/Flee/Protect, Big/Flee/Protect, Small/Fight/Aggressive]
```

Cada rival recibe:
- Orders = RivalPlans[roomSeed % 7][rivalIndex % 3]
- Orders clampeado por DNA si personalidad bloquea (ArenaOrderRules.Clamp)
- Occupation/Site derivados de Orders

**Flujo Prepare (S104 actualizado):**
1. Limpia planned, seed RNG
2. Modo Player: FromRoster/LocalSave, Team=Player, Orders=Default, Remembered()
3. Modo Rival:
   - ordersplan = RivalPlans[Abs(roomSeed) % 7]
   - Para cada rival: Orders=ordersplan[rivalIdx%3], clamped, added to planned

**S103 Cambios:**
- SelectLocal() + ClearLocalSelection()
- LocalPool propiedad pública
- Prepare() prioriza localSelection

**S104 Cambios:**
- Constructor acepta ExpeditionRulesSO rules
- RivalPlans vector de ArenaOrders (no Occupation)
- SetPlayerOrders() método nuevo
- Clamp() reaplicaclamping post-mutación UI
- Orders field en ArenaCastEntry (no Occupation/Site almacenados, derivados)
- Remembered() ahora guarda/restaura Orders (no Occupation/Site)

**Invariantes:**
- Clamping automático en Prepare() y al mutar (SetPlayerOrders → Clamp)
- Orden determinística: misma semilla → mismos RivalPlans
- LocalSelection tiene prioridad sobre aleatorio en Prepare

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaRosterSO]], [[ArenaCastEntry]], [[ArenaSandbox]], [[ArenaCastPicker]], [[CreatureDNA]], [[ArenaOrders]], [[ArenaOrderRules]], [[ExpeditionRulesSO]]
