---
tags: [script, data, scriptableobject, expedition]
---

# ArenaRosterSO.cs

**Ruta:** `Data/Expedition/ArenaRosterSO.cs`

**Responsabilidad:** Tabla de configuración de MoriMonchis para la sandbox Arena. Define entrada (`Entry`) con nombre, equipo (Player/Rival), personalidad (Sociability/Boldness), apariencia (BodyShapeID, BaseColor), **IDs de partes (S109 NUEVO: HornID, BackID, WingID)** y ocupación. Usada por `ArenaSandbox` para spawnear agentes deterministas con ocupación y habilidades asignadas. Botón `PopulateDefaults()` precarga 6 ejemplares con ocupaciones variadas (3 Player, 3 Rival).

## Estructura

**Nested class Entry (S109 ACTUALIZADO):**
```csharp
public class Entry
{
    public string Name = "";
    public ExpeditionTeam Team = ExpeditionTeam.Player;
    [Range(0f, 1f)] public float Sociability = 0.5f;
    [Range(0f, 1f)] public float Boldness = 0.5f;
    public string BodyShapeID = "";
    public string HornID = "";           // S109 NUEVO
    public string BackID = "";           // S109 NUEVO
    public string WingID = "";           // S109 NUEVO
    public Color BaseColor = new Color(0f, 0f, 0f, 0f);
    public Occupation Occupation = Occupation.Gather;
}
```

**Campos Públicos:**
- `Entries` (List<Entry>) — lista de criaturas a spawnear.

## Métodos

- `PopulateDefaults()` — **Botón Odin**: inicializa `Entries` si está vacío con 6 ejemplares (3 Player + 3 Rival). Cada uno con Occupation predefinida y opcionalmente BodyShapeID + IDs de parte vacíos (defaults).

## Flujo de Uso

1. **En Editor:** inspector muestra lista de Entries; cada entrada personalizable (nombre, equipo, personalidad, partes genéticas)
2. **ArenaCastPlanner.FromRoster():** copia Entry a CreatureDNA:
   - Sociability, Boldness, CustomName
   - BodyShapeID (si no vacío)
   - **S109:** HornID, BackID, WingID (si no vacíos, sobrescriben DNA.HornID/BackID/WingID)
3. **AbilityDatabaseSO.Resolve():** resuelve habilidades según los IDs copiados
4. **AgentAbilities.Bind():** asigna habilidades al agente

## Invariantes S109

- **IDs de parte opcionales:** si vacío en Entry, DNA mantiene defaults (no sobrescribir)
- **Ocupación dual:** Sociability/Boldness modulan comportamiento dentro de ocupación (ej: Bold + Guard = vigilancia más agresiva)
- **Equipos:** Player vs Rival. Helper static `ExpeditionTeams.AreRivals()`
- **Ocupación default:** si Entry.Occupation == Occupation.Explore → traducir a Gather en AgentExpedition.TryEngage()
- **Apariencia:** BodyShapeID, BaseColor, + ahora IDs de partes genéticas personalizan el look y habilidades
- **Extensibilidad:** agregar Entry en Inspector sin recompile; `ArenaSandbox.Spawn()` itera y spawnea con Occupation, Team e IDs de parte

**Ejemplo S109:**

```csharp
Entry { 
  Name = "Osado Cuernudo", 
  Team = ExpeditionTeam.Player, 
  Sociability = 0.25f, 
  Boldness = 0.9f,
  BodyShapeID = "dragon-buff",
  HornID = "horn-prong",        // S109: define habilidad de cuerno
  BackID = "back-spikes",       // S109: define habilidad de espalda
  WingID = "wing-swift",        // S109: define habilidad de alas
  Occupation = Occupation.Guard 
}
```

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]] (sección 8.10: Ocupaciones)

## Conexiones

[[ArenaSandbox]], [[ArenaCastPlanner]], [[MoriMochiAgent]], [[AgentExpedition]], [[Occupation]], [[ExpeditionTeam]], [[AgentContext]], [[AbilityDatabaseSO]], [[CreatureDNA]]
