---
tags: [script, world, expedition, sandbox]
---

# ArenaSandbox.cs

**Ruta:** `World/Expedition/ArenaSandbox.cs`

**Responsabilidad:** Escena de pruebas `ArenaSandbox.unity` para observar comportamientos emergentes. **S101 NUEVO:** Genera criaturas desde `ArenaRosterSO` (si `useRoster == true`) con ocupaciones (Gather/Guard/Break/Decoy), spawnea por equipos, asigna Team, Occupation, y HomeExit. Fallback: genera N criaturas al azar. Mantiene necesidades llenas, siembra minerales recolectables, spawnea ExitZones por equipo (salida donde depositar). Crea `ArenaLayoutBuilder` para generar obstáculos y vetas por semilla. **S100:** Referencia a `ClashTuningSO` para que choque funcione.

## Métodos Públicos

**Configuración:**
- `Spawn()` — generador principal llamado en Start(). **S101:** si `useRoster && roster != null`, spawnea entrada por entrada con Team, Occupation y parámetros. Setea Teams y Occupations vía perceivable y agent. Spawnea ExitZones por equipo. Siembra minerales con ArenaLayoutBuilder.
- `SpawnExits(NavMeshQueryFilter, Vector3 center)` — **S101 NUEVO:** instancia ExitZone prefab para cada equipo en esquinas (teamSpawnInset desde centro).
- `Respawn()` — **Botón Odin**: destruye y re-spawnea.
- `Reseed()` — **Botón Odin**: genera seed nuevo y respawnea.

**Propiedades (read-only):**
- `Spawned → IReadOnlyList<MoriMonchiController>` — criaturas activas.
- `Minerals → IReadOnlyList<MaterialPickup>` — minerales sembrados.
- `Exits → IReadOnlyList<ExitZone>` — salidas por equipo. **S101 NUEVO**
- `ExitFor(ExpeditionTeam team) → ExitZone` — getter de salida por equipo. **S101 NUEVO**
- `ActiveSeed → int` — seed usado esta ejecución.

## Campos Configurables (Inspector)

**Referencias requeridas:**
- `creaturePrefab` (MoriMonchiController)
- `profileTable` (RoleWorldProfileSO)
- `socialTuning` (SocialTuningSO)
- `expeditionRules` (ExpeditionRulesSO)
- `clashTuning` (ClashTuningSO) — **S100**
- `visualBank` (MonchiVisualBankSO)
- `furDatabase` (FurTypeDatabaseSO)
- `creatureDatabase` (CreatureDatabaseSO)
- `mineralPrefab` (MaterialPickup)
- **S101 NUEVO:** `exitPrefab` (ExitZone) — para spawnear salidas

**Configuración de Elenco (S101 NUEVO):**
- `roster` (ArenaRosterSO) — tabla con Occupation por Entry
- `useRoster` (bool, default true)
- `teamSpawnInset` (float, default 9) — distancia de esquina
- `teamSpawnRadius` (float, default 2.5) — radio de spawn
- `exitInset` (float, default 4) — distancia de ExitZone desde esquina. **S101 NUEVO**

**Configuración de minerales:**
- `mineralPrefab` (MaterialPickup)
- `layout` (ArenaLayoutBuilder) — **S101 NUEVO:** generador de vetas por semilla
- `cornerMinerals` (int, default 4)
- `centerMineralScale` (float, default 2.5)
- `centerMineralValue` (int, default 5)
- `cornerMineralValue` (int, default 1)

## Flujo S101 + S100

```
Spawn():
  if (useRoster && roster.Entries.Count > 0):
    SpawnExits(filter, center)  // S101 NUEVO: crea ExitZone por Player/Rival
    if (layout != null) layout.Build(activeSeed, filter)  // S101: genera obstáculos y vetas
    
    foreach entry in roster.Entries:
      dna = MintRandom()
      dna.Sociability = entry.Sociability
      dna.Boldness = entry.Boldness
      (copiar Name, BodyShapeID, BaseColor)
      dna.Stamp()
      
      cornerPos = TeamCorner(entry.Team, center)
      SpawnCreature(dna, cornerPos, teamSpawnRadius, rng, filter, 
                    entry.Team, entry.Occupation,  // S101 NUEVO: Occupation
                    ExitFor(entry.Team))  // S101 NUEVO: HomeExit
```

## SpawnCreature (S101 ACTUALIZADO)

```csharp
private void SpawnCreature(CreatureDNA dna, Vector3 around, float radius,
                          System.Random rng, NavMeshQueryFilter filter,
                          ExpeditionTeam team, Occupation occupation,  // S101 NUEVOS
                          ExitZone homeExit)  // S101 NUEVO
{
  // ... instancia controller, busca Perceivable
  perceivable.SetTeam(team);  // S99
  agent.Team = team;  // S99
  agent.Occupation = occupation;  // S101 NUEVO
  agent.HomeExit = homeExit;  // S101 NUEVO
  agent.Initialize(dna, profileTable, observer);
}
```

## Invariantes S101 + S100 + S99

- **Ocupación por roster:** cada Entry en ArenaRosterSO define Occupation (Gather, Guard, Break, Decoy). Agentes spawneados heredan esa ocupación.
- **Team asignación:** Player vs Rival en esquinas opuestas, con ExitZones separadas.
- **HomeExit:** cada agente sabe su salida (`agent.HomeExit`); usado por AgentExpedition para Returning/Securing.
- **Layout builder:** genera obstáculos y vetas por semilla; NavMesh rehorneado; vetas cacheadas en Veins (para dibujo y gameplay).
- **Clash tuning cargado:** ClashTuningSO.Current disponible para AgentClash durante play.
- **Necesidades plenas:** keepNeedsFull=true mantiene Health/Energy/Affect a 100 para enfoque en comportamiento autónomo.

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

**Referencias de entrada:**
- [[ArenaRosterSO]] (tabla de criaturas con Occupation)
- [[Occupation]] (Gather, Guard, Break, Decoy, Explore)
- [[ExitZone]] (salida donde depositar material)
- [[ExpeditionRulesSO]], [[ClashTuningSO]], [[RoleWorldProfileSO]], [[MonchiVisualBankSO]], [[FurTypeDatabaseSO]], [[CreatureDatabaseSO]], [[SocialTuningSO]]

**Generación:**
- [[MoriMonchiController]], [[CreatureGenerator]], [[Perceivable]], [[ExpeditionTeam]], [[ArenaLayoutBuilder]]

**Referencias de lectura (UI/Debugging):**
- [[ArenaCueOverlay]] (itera Spawned y Minerals)
- [[ArenaRound]] (accede a Exits y Spawned)
- [[ArenaCameraDirector]] (accede a Spawned)
- **S101:** [[ArenaRoundHud]] (itera Spawned para mostrar Occupation en roster)
