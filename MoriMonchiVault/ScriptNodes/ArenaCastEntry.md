---
tags: [script, data, struct, expedition]
---

# ArenaCastEntry.cs

**Ruta:** `World/Expedition/ArenaCastEntry.cs`

**Responsabilidad:** Struct serializable que representa una criatura en el elenco de una ronda de arena. Contiene DNA, equipo, ocupación y sitio de recolecta.

## Campos

```csharp
public struct ArenaCastEntry
{
    public CreatureDNA Dna;
    public ExpeditionTeam Team;         // None, Player, Rival
    public Occupation Occupation;       // Gather, Guard, Break, Decoy
    public ArenaSite Site;              // Center, NearVein, FarVein
}
```

- `Dna` — criatura clonada desde Roster o LocalSave
- `Team` — determina entrada/salida y bando
- `Occupation` — rol táctico de la criatura (se puede cambiar antes de Launch)
- `Site` — ubicación de recolecta si Occupation=Gather (ignorado si no)

## Invariantes S102

- **Inmutable durante combate:** ArenaCastEntry no cambia mientras ArenaRound.IsRunning
- **Entrada recordada:** ArenaCastPlanner.remembered() cachea (Occupation, Site) por DNA.CustomName
- **Sitio ignorado para Decoy:** Occupation.Decoy no respeta Site (tiene lógica propia)

## Construcción

Típicamente creado en:
1. ArenaCastPlanner.Prepare() → FromRoster() o LocalSave
2. ArenaPlanPanel.ChooseOccupation/ChooseSite → mutación vía SetPlayerPlan

## Conexiones

- [[CreatureDNA]] (Dna field)
- [[ArenaCastPlanner]] (constructor Prepare)
- [[ArenaSandbox]] (almacena en PlannedCast)
- [[ArenaRound]] (itera cast para SpawnCast)
- [[AgentExpedition]] (lee Site/Occupation del agente)
- [[WorldEnums]] (ExpeditionTeam, Occupation, ArenaSite)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
