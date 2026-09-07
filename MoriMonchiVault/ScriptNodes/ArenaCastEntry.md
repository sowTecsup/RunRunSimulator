---
tags: [script, data, struct, expedition]
---

# ArenaCastEntry.cs

**Ruta:** `World/Expedition/ArenaCastEntry.cs`

**Responsabilidad:** Struct serializable que representa criatura en elenco de arena. Contiene DNA, equipo, **órdenes** (S104), ocupación y sitio derivados de órdenes. Inmutable durante combate; mutado por UI antes de launch.

**Campos:**
```csharp
public struct ArenaCastEntry
{
    public CreatureDNA Dna;
    public ExpeditionTeam Team;         // None, Player, Rival
    public ArenaOrders Orders;          // Loot/Contact/Posture (S104 NUEVO)
    public Occupation Occupation { get; }  // derivado de Orders (S104, readonly)
    public ArenaSite Site { get; }      // derivado de Orders (S104, readonly)
}
```

**Propiedades (S104):**
- `Occupation Occupation { get; }` — calculado como `ArenaOrderRules.ToOccupation(Orders)`. Guardado en delegated return, no field (solo lectura)
- `ArenaSite Site { get; }` — calculado como `ArenaOrderRules.ToSite(Orders)`. Guardado en delegated return (solo lectura)

**Invariantes:**
- Occupation/Site derivados de Orders (S104) → no se mutan independientemente
- Orders clampeado por DNA si personalidad bloquea (ArenaOrderRules.Clamp)
- Inmutable durante combate; mutado por ArenaPlanPanel antes de launch
- Entrada recordada en ArenaCastPlanner.remembered() (Dna.CustomName → Orders last)

**Construcción:**
- ArenaCastPlanner.Prepare() → FromRoster() o LocalSave
- ArenaPlanPanel.SetPlayerPlan() → mutación pre-launch

**Vinculado a:** [[Index/22 - Arena (S103-S104)]]

**Conexiones:** [[CreatureDNA]], [[ArenaOrders]], [[ArenaOrderRules]], [[ArenaCastPlanner]], [[ArenaSandbox]], [[AgentExpedition]], [[WorldEnums]]
