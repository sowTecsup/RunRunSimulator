---
tags: [script, data, scriptableobject, expedition]
---

# ArenaRosterSO.cs

**Ruta:** `Data/Expedition/ArenaRosterSO.cs`

**Responsabilidad:** Tabla de configuración de MoriMonchis para la sandbox Arena. Define entrada (`Entry`) con nombre, equipo (Player/Rival), personalidad (Sociability/Boldness) y apariencia (BodyShapeID, BaseColor). Usada por `ArenaSandbox` para spawnear agentes. Botón `PopulateDefaults()` precarga 6 ejemplares (3 Player: Osado/Tímida/Equilibrado, 3 Rival: Fiero/Cauta/Templado).

## Estructura

**Nested class Entry:**
```csharp
public class Entry
{
    public string Name = "";
    public ExpeditionTeam Team = ExpeditionTeam.Player;
    [Range(0f, 1f)] public float Sociability = 0.5f;
    [Range(0f, 1f)] public float Boldness = 0.5f;
    public string BodyShapeID = "";
    public Color BaseColor = new Color(0f, 0f, 0f, 0f);
}
```

**Campos Públicos:**
- `Entries` (List<Entry>) — lista de criaturas a spawnear. Dibujada con `[ListDrawerSettings(ShowFoldout=false, DefaultExpandedState=true)]` para comodidad.

## Métodos

- `PopulateDefaults()` — **Botón Odin**: inicializa `Entries` si está vacío con 6 ejemplares (3 Player + 3 Rival). Cada uno con nombre, equipo, Sociability/Boldness predefinidos. Marca dirty.

## Ciclo de Vida

```csharp
OnEnable():
  (sin lógica; es un SO puro)

PopulateDefaults():
  if (Entries.Count == 0) {
    Entries.Add(new Entry { Name = "Osado", Team = ExpeditionTeam.Player, Sociability = 0.25f, Boldness = 0.9f });
    Entries.Add(new Entry { Name = "Tímida", Team = ExpeditionTeam.Player, Sociability = 0.85f, Boldness = 0.15f });
    Entries.Add(new Entry { Name = "Equilibrado", Team = ExpeditionTeam.Player, Sociability = 0.5f, Boldness = 0.5f });
    Entries.Add(new Entry { Name = "Fiero", Team = ExpeditionTeam.Rival, Sociability = 0.25f, Boldness = 0.9f });
    Entries.Add(new Entry { Name = "Cauta", Team = ExpeditionTeam.Rival, Sociability = 0.85f, Boldness = 0.15f });
    Entries.Add(new Entry { Name = "Templado", Team = ExpeditionTeam.Rival, Sociability = 0.5f, Boldness = 0.5f });
  }
```

## Invariantes S98

- **Personalidad dual:** Sociability y Boldness modular el comportamiento del agente spawneado (via `MoriMochiAgent` + `AgentBrain`/`AgentSocial`).
- **Equipos:** `ExpeditionTeam.Player` vs `ExpeditionTeam.Rival`. Helper static `ExpeditionTeams.AreRivals()` detecta conflicto.
- **Apariencia:** BodyShapeID apunta a `BodyShapeDatabaseSO`; BaseColor es override genético.
- **Extensibilidad:** agregar Entry en Inspector sin recompile; `ArenaSandbox.OnEnable()` itera `Current.Entries` y spawnea.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

[[ArenaSandbox]], [[MoriMochiAgent]], [[AgentBrain]], [[AgentSocial]], [[ExpeditionTeam]], [[BodyShapePart]]
