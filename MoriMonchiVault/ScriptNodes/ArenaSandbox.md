---
tags: [script, world, expedition, sandbox]
---

# ArenaSandbox.cs

**Ruta:** `World/Expedition/ArenaSandbox.cs`

**Responsabilidad:** Escena de pruebas `ArenaSandbox.unity` para observar comportamientos emergentes de criaturas en entorno controlado. **S99 NUEVO:** Genera criaturas desde `ArenaRosterSO` (si `useRoster == true`), spawnea por equipos (Player en esquina inferior-izquierda, Rival en esquina superior-derecha), y asigna Team via `Perceivable.SetTeam()`. Fallback: genera N criaturas al azar con semilla determinista. Mantiene necesidades llenas, siembra minerales recolectables (1 central de alto valor + 4 de esquinas). Expone listas públicas de criaturas spawneadas y minerales para que `ArenaCueOverlay` las dibuje. Nota: la escena no está en Build Settings; es solo para desarrollo/debugging.

## Métodos Públicos

**Configuración:**
- `Spawn()` — generador principal llamado en Start(). **S99:** si `useRoster && roster != null`, spawnea entrada por entrada con Team y parámetros de ArenaRosterSO (Sociability, Boldness, Name, BodyShapeID, Color). Fallback: genera N criaturas aleatorias. Setea Teams vía `perceivable.SetTeam(team)` línea 147. Siembra minerales.
- `SpawnCreature(CreatureDNA, Vector3 around, float radius, System.Random, NavMeshQueryFilter, ExpeditionTeam team)` — **S99 NUEVO param:** `team`. Instancia controller, busca Perceivable, **llama `perceivable.SetTeam(team)`**, inicializa agente.

**Interfaz:**
- `Respawn()` — **Botón Odin**: destruye todas las criaturas y minerales, luego llama a `Spawn()`.
- `Reseed()` — **Botón Odin**: genera seed nuevo y respawnea.

**Propiedades (read-only):**
- `Spawned → IReadOnlyList<MoriMonchiController>` — criaturas activas generadas.
- `Minerals → IReadOnlyList<MaterialPickup>` — minerales sembrados (central + esquinas).
- `ActiveSeed → int` — seed usado esta ejecución.

## Campos Configurables (Inspector)

**Referencias requeridas:**
- `creaturePrefab` (MoriMonchiController) — prefab a clonar para cada criatura.
- `profileTable` (RoleWorldProfileSO) — tabla de perfiles de rol.
- `socialTuning` (SocialTuningSO) — tuning social global.
- `expeditionRules` (ExpeditionRulesSO) — reglas de expedición (se ve si `Current != null`).
- `visualBank` (MonchiVisualBankSO) — banco de visuales.
- `furDatabase` (FurTypeDatabaseSO) — database de tipos de pelaje.
- `creatureDatabase` (CreatureDatabaseSO) — database de partes genéticas.
- `mineralPrefab` (MaterialPickup) — prefab de cristal recolectable.

**Configuración de spawn (S97-S99):**
- `observer` (Transform) — cámara o punto de observación para pasar a `Initialize()`.
- `spawnCenter` (Transform) — centro del círculo de spawn (por defecto: transform del ArenaSandbox).
- `seed` (int, default 4242) — semilla base (si randomizeEachPlay = false).
- `randomizeEachPlay` (bool, default false) — si true, usa `System.Environment.TickCount`.
- `count` (int, min 1, default 3) — cantidad de criaturas (fallback si NO useRoster).
- `spawnRadius` (float, min 1, default 4) — radio máximo del círculo de spawn (fallback).
- `keepNeedsFull` (bool, default true) — si true, rellena Health/Energy/Affect a 100 cada frame.
- `tagShowDistance` (float) — distancia máxima para mostrar NameTag.
- `tagReferenceDistance` (float) — distancia de referencia para escala de NameTag.

**Configuración de Elenco (S99 NUEVO):**
- `roster` (ArenaRosterSO) — tabla de criaturas predefinidas con nombre, equipo, Sociability, Boldness, apariencia.
- `useRoster` (bool, default true) — si true, spawnea desde `roster.Entries`; si false, genera aleatorios.
- `teamSpawnInset` (float, min 0, default 9) — distancia desde el borde de la arena hacia adentro (para esquinas Player/Rival).
- `teamSpawnRadius` (float, min 0.5, default 2.5) — radio de spawn alrededor de la esquina del equipo.

**Configuración de minerales (S97):**
- `mineralPrefab` (MaterialPickup) — cristal recolectable.
- `cornerMinerals` (int, min 0, default 4) — cantidad de minerales de esquina.
- `cornerInset` (float, min 0, default 6) — distancia desde el borde de la arena hacia adentro.
- `cornerJitter` (float, min 0, default 2) — variación aleatoria de posición de esquina.
- `centerMineralScale` (float, min 1, default 2.5) — escala del cristal central.
- `centerMineralValue` (int, min 1, default 5) — valor de material del central (vs 1 para esquinas).
- `arenaHalfSize` (float, min 0, default 20) — semi-ancho de la arena cuadrada (para cálculos de esquina).

## Flujo S99

```
Spawn():
  if (useRoster && roster != null && roster.Entries.Count > 0):
    foreach entry in roster.Entries:
      dna = MintRandom()
      copiar entry.Sociability → dna.Sociability
      copiar entry.Boldness → dna.Boldness
      copiar entry.Name → dna.CustomName (si no vacío)
      copiar entry.BodyShapeID → dna.BodyShapeID (si no vacío)
      copiar entry.BaseColor → dna.BaseColor (si alpha > 0)
      dna.Stamp()
      
      cornerPos = TeamCorner(entry.Team, center)  // S99 NUEVO: esquina por equipo
      SpawnCreature(dna, cornerPos, teamSpawnRadius, rng, filter, entry.Team)
  else:
    for i = 0 to count:
      dna = MintRandom()
      SpawnCreature(dna, center, spawnRadius, rng, filter, ExpeditionTeam.None)
  
  SpawnMinerals(rng, filter, center)
```

## TeamCorner (S99 NUEVO)

```csharp
private Vector3 TeamCorner(ExpeditionTeam team, Vector3 center)
{
    switch (team)
    {
        case ExpeditionTeam.Player:
            return center + new Vector3(-1f, 0f, -1f) * (arenaHalfSize - teamSpawnInset);  // esquina inferior-izquierda
        case ExpeditionTeam.Rival:
            return center + new Vector3(1f, 0f, 1f) * (arenaHalfSize - teamSpawnInset);    // esquina superior-derecha
        default:
            return center;  // neutral: centro
    }
}
```

## Invariantes S99

- **Roster-driven:** si `roster` existe y `useRoster=true`, spawnea exactamente tantos agentes como entradas en roster (no aleatorio, ni "count").
- **Team asignación:** `perceivable.SetTeam(team)` es el punto de mutación; sin esto, los agentes quedan en `ExpeditionTeam.None` (neutrales).
- **Separación física:** Player y Rival spawnean en esquinas opuestas (`TeamCorner`), facilitando conflictos de expedición y socialización rechazada.
- **Necesidades plenas:** `keepNeedsFull=true` en Inspector mantiene Health/Energy/Affect a 100 para enfoque en comportamiento autónomo (sin supervivencia).
- **Determinismo:** seed fijo (o `System.Environment.TickCount` si randomizeEachPlay) permite reproducibilidad.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

**Referencias de entrada:**
- **S99:** [[ArenaRosterSO]] (tabla de criaturas predefinidas)
- [[ExpeditionRulesSO]] (reglas de expedición, timings de beats)
- [[RoleWorldProfileSO]], [[MonchiVisualBankSO]], [[FurTypeDatabaseSO]], [[CreatureDatabaseSO]]
- [[SocialTuningSO]] (tuning social global)

**Generación:**
- [[MoriMonchiController]] (prefab spawneado)
- [[CreatureGenerator]] (genera DNA aleatorio)
- **S99:** [[Perceivable]], [[ExpeditionTeam]] (setea team)
- [[NavMeshAgent]], [[NavMesh]] (muestreo de posiciones)

**Referencias de lectura (UI/Debugging):**
- [[ArenaCueOverlay]] (itera Spawned y Minerals para dibujar guías)
- [[NameTag]] (accede a tagShowDistance/tagReferenceDistance)
- [[Unity.Cinemachine.CinemachineTargetGroup]] (agrupa cámaras)
