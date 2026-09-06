---
tags: [script, data, scriptableobject, expedition]
---

# ArenaRosterSO.cs

**Ruta:** `Data/Expedition/ArenaRosterSO.cs`

**Responsabilidad:** Tabla de configuración de MoriMonchis para la sandbox Arena. Define entrada (`Entry`) con nombre, equipo (Player/Rival), personalidad (Sociability/Boldness), apariencia (BodyShapeID, BaseColor), y **S101 NUEVO:** ocupación (Gather/Guard/Break/Decoy/Explore). Usada por `ArenaSandbox` para spawnear agentes deterministas con ocupación asignada. Botón `PopulateDefaults()` precarga 6 ejemplares con ocupaciones variadas (3 Player, 3 Rival).

## Estructura

**Nested class Entry (S101 ACTUALIZADO):**
```csharp
public class Entry
{
    public string Name = "";
    public ExpeditionTeam Team = ExpeditionTeam.Player;
    [Range(0f, 1f)] public float Sociability = 0.5f;
    [Range(0f, 1f)] public float Boldness = 0.5f;
    public string BodyShapeID = "";
    public Color BaseColor = new Color(0f, 0f, 0f, 0f);
    public Occupation Occupation = Occupation.Gather;  // S101 NUEVO
}
```

**Campos Públicos:**
- `Entries` (List<Entry>) — lista de criaturas a spawnear.

## Métodos

- `PopulateDefaults()` — **Botón Odin**: inicializa `Entries` si está vacío con 6 ejemplares (3 Player + 3 Rival). **S101:** cada uno con Occupation predefinida (ej: Osado=Guard, Tímida=Gather, Equilibrado=Gather, Fiero=Break, Cauta=Gather, Templado=Decoy).

**Ejemplo S101:**
```csharp
Entries.Add(new Entry { 
  Name = "Osado", Team = ExpeditionTeam.Player, Sociability = 0.25f, Boldness = 0.9f, 
  Occupation = Occupation.Guard  // S101: guardián
});
Entries.Add(new Entry { 
  Name = "Fiero", Team = ExpeditionTeam.Rival, Sociability = 0.25f, Boldness = 0.9f, 
  Occupation = Occupation.Break  // S101: rompe
});
Entries.Add(new Entry { 
  Name = "Templado", Team = ExpeditionTeam.Rival, Sociability = 0.5f, Boldness = 0.5f, 
  Occupation = Occupation.Decoy  // S101: distrae
});
```

## Invariantes S101 + S98

- **Ocupación dual:** Sociability/Boldness modulan comportamiento dentro de ocupación (ej: Bold + Guard = vigilancia más agresiva).
- **Equipos:** Player vs Rival. Helper static `ExpeditionTeams.AreRivals()`.
- **Ocupación default:** si Entry.Occupation == Occupation.Explore → traducir a Gather en AgentExpedition.TryEngage().
- **Apariencia:** BodyShapeID y BaseColor personalizan el look.
- **Extensibilidad:** agregar Entry en Inspector sin recompile; `ArenaSandbox.Spawn()` itera y spawnea con Occupation y Team.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]] (sección 8.10: Ocupaciones)

## Conexiones

[[ArenaSandbox]], [[MoriMochiAgent]], [[AgentExpedition]], [[Occupation]], [[ExpeditionTeam]], [[AgentContext]]
