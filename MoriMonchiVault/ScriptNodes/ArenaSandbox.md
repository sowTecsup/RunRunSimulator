---
tags: [script, world, expedition, sandbox]
---

# ArenaSandbox.cs

**Ruta:** `World/Expedition/ArenaSandbox.cs`

**Responsabilidad:** Escena de pruebas `ArenaSandbox.unity` para observar comportamientos emergentes de criaturas en entorno controlado. Genera N criaturas al azar con semilla determinista (mismo mint que `GameManager.MintRandomCreature`), las suelta sobre el NavMesh, mantiene sus necesidades llenas, y siembra minerales recolectables (1 central de alto valor + 4 de esquinas). Expone listas públicas de criaturas spawneadas y minerales para que `ArenaCueOverlay` las dibuje. Nota: la escena no está en Build Settings; es solo para desarrollo/debugging.

## Métodos Públicos

**Configuración:**
- `Spawn()` — generador principal llamado en Start(). Inicializa `activeSeed` (fijo o aleatorio), genera N criaturas con DNA aleatorio pero determinista, las instancia con posiciones muestreadas en NavMesh, inicializa cada una, y siembra minerales.

**Interfaz:**
- `Respawn()` — **Botón Odin**: destruye todas las criaturas y minerales, luego llama a `Spawn()`.
- `Reseed()` — **Botón Odin**: genera seed nuevo y respawnea.

**Propiedades (read-only):**
- `Spawned → IReadOnlyList<MoriMonchiController>` — criaturas activas generadas.
- `Minerals → IReadOnlyList<MaterialPickup>` — minerales sembrads (central + esquinas).
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

**Configuración de spawn:**
- `observer` (Transform) — cámara o punto de observación para pasar a `Initialize()`.
- `spawnCenter` (Transform) — centro del círculo de spawn (por defecto: transform del ArenaSandbox).
- `seed` (int, default 4242) — semilla base.
- `randomizeEachPlay` (bool, default false) — si true, ignora `seed` y usa `System.Environment.TickCount`.
- `count` (int, min 1, default 3) — cantidad de criaturas a spawnear.
- `spawnRadius` (float, min 1, default 4) — radio máximo del círculo de spawn alrededor del centro.
- `keepNeedsFull` (bool, default true) — si true, en Update() rellena Health/Energy/Affect a 100 cada frame.

**Configuración de minerales:**
- `cornerMinerals` (int, min 0, default 4) — cantidad de minerales de esquina (máximo 4).
- `cornerInset` (float, min 0, default 6) — distancia desde el borde de la arena hacia adentro.
- `cornerJitter` (float, min 0, default 2) — variación aleatoria de posición de esquina.
- `centerMineralScale` (float, min 1, default 2.5) — escala del cristal central.
- `centerMineralValue` (int, min 1, default 5) — valor del mineral central.
- `arenaHalfSize` (float, min 0, default 20) — medio tamaño de la arena (40×40).

## Ciclo de Actualización

```csharp
Start():
  Spawn()

Update():
  if (keepNeedsFull):
    foreach criatura en spawned:
      Health = Energy = Affect = 100
```

## Invariantes S97

- **Determinismo:** semilla fija (4242) o nueva por `Reseed()`; mint usa el mismo orden que `GameManager` (Gender, Element, Role, Stats, Diales, Name, Stamp).
- **NavMesh AllAreas:** se fuerza `areaMask = NavMesh.AllAreas` antes de `Initialize()` porque Arena no tiene áreas de tienda; contrasta con tienda que usa `.FreeAreaMask`.
- **Persistencia NavMesh:** el `NavMeshData` debe ser asset guardado en `Scenes/ArenaSandbox/NavMesh-NavMesh.asset` o se pierde al reiniciar editor.
- **Needs llenas:** `keepNeedsFull=true` simplifica debug de IA sin hambre; en play real solo se popula en espawn.
- **Minerales escondidos:** los de esquina pueden caer fuera del radio de percepción (6 m); es decisión de diseño pendiente (Index/23 8.7).

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]], [[Index/06 - Player & World]]

## Conexiones

- [[MoriMonchiController]], [[MoriMochiAgent]] (initialize)
- [[CreatureGenerator]] (mint)
- [[RoleWorldProfileSO]], [[SocialTuningSO]], [[ExpeditionRulesSO]], [[MonchiVisualBankSO]], [[FurTypeDatabaseSO]], [[CreatureDatabaseSO]]
- [[MaterialPickup]] (prefab de mineral)
- [[ArenaCueOverlay]] (lee Spawned y Minerals)
- [[NameTag]], [[NavMeshAgent]]
