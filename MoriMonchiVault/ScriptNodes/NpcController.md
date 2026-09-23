---
tags: [script, world, npc]
---

# NpcController.cs

**Ruta:** `World/Npc/NpcController.cs`

**Responsabilidad:** Singleton spawn/despawn de NPCs (clientes). Cadence controllada por `minSpawnInterval`, `maxSpawnInterval`, `maxSimultaneous`. Update() tick, TrySpawnOne() instancia si condiciones. S131: Solo genera clientes cuando DayScheduleSO.BlockAt(currentMinute).CustomersOpen == true; escucha `GameEvents.OnDayBlockChanged` para parar/reanudar spawn.

## Campos Serializados

| Campo | Tipo | Propósito |
|-------|------|----------|
| `spawnPoint` | Transform | Punto de entrada |
| `exitPoint` | Transform | Punto de salida |
| `register` | CashRegister | Registro al que van los clientes |
| `minSpawnInterval` | float | Segundos reales mínimo entre spawns |
| `maxSpawnInterval` | float | Segundos reales máximo entre spawns |
| `maxSimultaneous` | int | Máximo clientes simultáneos en escena |
| `defaultAgentPrefab` | NpcAgent | Prefab fallback |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `ForceSpawn()` | `void` | Spawn inmediato (dev); reinicia timer |
| `TrySpawnOne()` | `NpcAgent` | Intenta spawn si condiciones, retorna instancia o null |
| `Despawn(NpcAgent agent)` | `void` | Remueve de active y destruye GO |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Active` | `IReadOnlyList<NpcAgent>` | Clientes vivos en escena |
| `ExitPoint` | `Transform` | Destino al salir |

## Cambios S131

**Nuevo behavior:**
- Suscribe a `GameEvents.OnDayBlockChanged` en OnEnable
- En el handler, comprueba `block?.CustomersOpen` (del DayBlockDef)
- Si CustomersOpen == false: detiene spawning (pausa timer, no llama TrySpawnOne)
- Si CustomersOpen == true: reanuda spawning normal

**Lógica de Update:**
```csharp
if (!customersOpen) return;  // No spawn si no abierto
if (spawnTimer > 0) { spawnTimer -= deltaTime; return; }
if (Active.Count >= maxSimultaneous) return;

TrySpawnOne();
spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);
```

## Ciclo de Vida

1. `OnEnable()` → suscribe `OnDayBlockChanged`
2. `OnDayBlockChanged(block)` → `customersOpen = block?.CustomersOpen ?? false`
3. Update: si customersOpen && condiciones → TrySpawnOne
4. `OnDisable()` → desuscribe
5. `OnDestroy()` → limpia active list

## Vinculado a

- [[Index/04 - Customer System]]
- [[Index/09 - Active Context]]

## Conexiones

**Data:**
- [[DayScheduleSO]] — proporciona block con CustomersOpen flag
- [[DayBlockDef]] — contiene CustomersOpen

**Sistemas:**
- [[GameClock]] → dispara OnDayBlockChanged
- [[GameEvents]] — suscripción
- [[NpcAgent]], [[StoreDisplayRegistry]], [[CashRegister]], [[CustomerService]]

## Notas (S131 HC-4)

- **CustomersOpen flag:** Únicamente controla si NpcController spawna. Clientescorrientes ya despawnean por salida normal (no relacionado).
- **Pausa timer:** Cuando CustomersOpen=false, no decrementamos spawnTimer (se pausa effectivamente).
- **Safe block:** `block?.CustomersOpen ?? false` — fallback a false si bloque null.
