---
tags: [script, world, expedition, planning]
---

# ArenaCastPlanner.cs

**Ruta:** `World/Expedition/ArenaCastPlanner.cs`

**Responsabilidad:** Planificador de elenco de arena que construye lista de criaturas a spawnear según modo (Roster vs LocalSave), aplica planes de **órdenes** rivales por semilla (S104), remembers cambios del jugador, **S109:** copia IDs de partes genéticas del Roster Entry al DNA para determinar habilidades. S103: selección explícita. S104: órdenes clampeadas por DNA, ocupación y sitio derivados. **S119:** Constructor acepta `Func<CreatureDNA> mint` en lugar de CreatureRegistrySO.

**Constructor:**
- `ArenaCastPlanner(ArenaRosterSO roster, Func<CreatureDNA> mint, ExpeditionRulesSO rules)` — **S119:** mint es callback para generar DNAs (ej: ArenaSandbox.MintRandom o GameManager.MintRandomCreature)

**Métodos públicos:**
- `void Prepare(int roomSeed, int castSeed, int freeCount)` — construye planned
- `void SetPlayerOrders(int index, ArenaOrders orders)` — (S104 NUEVO) actualiza entry[index].Orders, clampeado
- `void SelectLocal(IReadOnlyList<CreatureDNA> picks)` — (S103) carga selección explícita del picker
- `void ClearLocalSelection()` — (S103) limpia
- `void SetMode(ArenaCastMode mode)` — (S119 renombrado) antes era propiedad setter

**Propiedades:**
- `IReadOnlyList<ArenaCastEntry> Planned { get; }`
- `ArenaCastMode Mode` — Roster o LocalSave
- `int LocalCount` — criaturas LocalSave
- `bool LocalAvailable`, `HasRoster`, `HasLocalSelection`, `HasTeams`
- `IReadOnlyList<CreatureDNA> LocalPool { get; }` — (S103)

**Método Privado FromRoster (S109 ACTUALIZADO):**
```csharp
private ArenaCastEntry FromRoster(ArenaRosterSO.Entry entry, ArenaOrders orders)
{
    var dna = mint();  // Crea DNA via callback (S119)
    dna.Sociability = entry.Sociability;
    dna.Boldness = entry.Boldness;
    if (!string.IsNullOrEmpty(entry.Name)) dna.CustomName = entry.Name;
    if (!string.IsNullOrEmpty(entry.BodyShapeID)) dna.BodyShapeID = entry.BodyShapeID;
    
    // S109 NUEVO: copia IDs de partes si no están vacíos
    if (!string.IsNullOrEmpty(entry.HornID)) dna.HornID = entry.HornID;
    if (!string.IsNullOrEmpty(entry.BackID)) dna.BackID = entry.BackID;
    if (!string.IsNullOrEmpty(entry.WingID)) dna.WingID = entry.WingID;
    
    if (entry.BaseColor.a > 0f) dna.BaseColor = entry.BaseColor;
    dna.Stamp();  // S119: sellos el DNA tras mutación
    
    return new ArenaCastEntry { Dna = dna, Team = entry.Team, Orders = ArenaOrderRules.Clamp(dna, rules, orders) };
}
```

**Constantes RivalPlans (S104: órdenes en lugar de ocupación):**
```
6 patrones de ArenaOrders para rivales (Loot/Contact/Posture), distribuidos por roomSeed % 6
Ejemplo: [Big/Flee/Protect, Big/Flee/Protect, Small/Fight/Aggressive]
```

Cada rival recibe:
- Orders = RivalPlans[roomSeed % 6][rivalIndex % 3]
- Orders clampeado por DNA si personalidad bloquea (ArenaOrderRules.Clamp)
- Occupation/Site derivados de Orders

**Flujo Prepare (S104 actualizado, S109 con partes genéticas, S119 con mint):**
1. Limpia planned, seed RNG
2. Modo Free (sin teams): mint() LocalCount veces, Team=None
3. Modo Player (LocalSave/Roster):
   - FromRoster/LocalSave, Team=Player, Orders=Default, Remembered()
   - **S109:** FromRoster() copia HornID, BackID, WingID + Stamp()
4. Modo Rival:
   - ordersplan = RivalPlans[Abs(roomSeed) % 6]
   - Para cada rival: Orders=ordersplan[rivalIdx%3], clamped, added to planned
   - **S109:** FromRoster() copia partes genéticas del Entry + Stamp()

**S103 Cambios:**
- SelectLocal() + ClearLocalSelection()
- LocalPool propiedad pública
- Prepare() prioriza localSelection

**S104 Cambios:**
- Constructor acepta ExpeditionRulesSO rules
- RivalPlans vector de ArenaOrders (no Occupation)
- SetPlayerOrders() método nuevo
- Orders field en ArenaCastEntry (no Occupation/Site almacenados, derivados)
- Remembered() ahora guarda/restaura Orders (no Occupation/Site)

**S109 Cambios:**
- `FromRoster()` copia IDs de partes (HornID, BackID, WingID) del Roster Entry al DNA
- Si los IDs no están vacíos, sobrescriben los defaults
- `dna.Stamp()` al final para actualizar timestamp
- Ejemplo: Entry con HornID="horn-prong" → DNA.HornID="horn-prong" → AbilityDatabaseSO.Resolve() retorna habilidad específica

**S119 Cambios:**
- Constructor: **mint cambiado** de CreatureRegistrySO a Func<CreatureDNA>
- FromRoster() usa mint() en lugar de registry.MintRandom()
- Desacoplamiento: planner no conoce GameManager o registry, solo callback
- **SetMode() renombrado:** antes era propiedad autocalculada, ahora setter explícito

**Invariantes:**
- Clamping automático en Prepare() y al mutar (SetPlayerOrders → Clamp)
- Orden determinística: misma semilla → mismos RivalPlans y partes genéticas
- LocalSelection tiene prioridad sobre aleatorio en Prepare
- **Partes genéticas:** si Entry.BodyShapeID/HornID vacío, DNA mantiene default (no sobrescribir con vacío)
- **S119:** mint callback responsable de generar DNAs válidos (ArenaSandbox.MintRandom o similar)

**Vinculado a:** [[Index/22 - Bajada Nocturna y Linaje]], [[Index/23 - Arena Sandbox y Expedicion]], [[Index/24 - Puente Tienda-Arena]]

**Conexiones:** [[ArenaRosterSO]], [[ArenaCastEntry]], [[ArenaSandbox]], [[ArenaCastPicker]], [[CreatureDNA]], [[ArenaOrders]], [[ArenaOrderRules]], [[ExpeditionRulesSO]], [[AbilityDatabaseSO]], [[CreatureGenerator]]
