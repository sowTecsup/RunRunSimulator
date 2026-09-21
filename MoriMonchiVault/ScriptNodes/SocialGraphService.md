---
tags: [script, systems, social, history, service]
---

# SocialGraphService.cs

**Ruta:** `Systems/Social/SocialGraphService.cs`

**Responsabilidad:** Servicio estático que mantiene el historial de afinidad social entre pares de MoriMonchis en memoria (S65 Social V2). Almacena deltas de afinidad acumulados por interacción (juego, siesta, pelea) para ajustar dinámicamente la afinidad base (`SocialAffinity.Compute()`). Ownea el diccionario `deltas` (key = PairKey ordenada) y expone métodos para registrar interacciones, importar/exportar data y querying. Usado por AgentSenses para calcular `EffectiveAffinity`, y por SaveSystem/CloudSyncService para persistencia local. **S129:** Suscriptor de `OnCreatureDeparted` para limpiar el grafo cuando una criatura parte (muerte/venta).

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `EffectiveAffinity(CreatureDNA a, b, SocialTuningSO t)` | `float` | Retorna afinidad seed + delta, clampada [-1, 1]. Si a/b null o UniqueID vacío, retorna solo seed. Usado por AgentSenses en cada percept de Monchi. |
| `RecordInteraction(string idA, idB, SocialInteractionKind kind)` | `void` | Registra interacción (PlayChase +0.06, SleepTogether +0.08, GremlinFight −0.1 vía HistoryDeltaClamp ±0.5). Solo el lado que notifica lo registra para evitar doble delta. Setea Dirty=true. |
| `ExportData()` | `Dictionary<string, float>` | Retorna copia profunda de deltas, setea Dirty=false. Usado por SaveSystem.SaveSocialGraph(). |
| `ImportData(Dictionary<string, float> data, Func<string, bool> idExists)` | `void` | Limpia deltas, re-popula desde data. Poda de huérfanos: si idExists provisto, salta pares donde algún ID ya no existe en registry. Setea Dirty=false. Usado por SaveSystem.LoadSocialGraph() post-sign-in. |
| `Clear()` | `void` | Limpia deltas y setea Dirty=false. Usado en reset de progress o cierre. |
| **S129:** `RemoveFromHistory(string id)` | `void` | Remueve todas las parejas que involucren `id` del grafo (criatura partió). Setea Dirty=true. |

## Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Dirty` | `bool` { get; private set; } | Flag que indica si deltas fue mutado desde último ExportData(). True tras RecordInteraction/RemoveFromHistory, False tras ExportData/ImportData/Clear. Usado por CloudSyncService para saber si pushear. |

## Métodos Internos (Private)

- `GetDelta(string idA, idB) → float` — lookup ordenada en deltas; retorna 0 si no existe
- `PairKey(string idA, idB) → string` — crea key ordenada (`string.CompareOrdinal`): `"idA|idB"` si A ≤ B, else `"idB|idA"`. Garantiza consistencia independientemente del orden de argumentos.

## Detalles de Interacción

**PlayChase:** Ganancia de +0.06 afinidad por completar persecución (ambos lados, ambición de historias S65 es evitar doble delta — solo el lado que "notifica" final registra).

**SleepTogether:** Ganancia de +0.08 afinidad por completar siesta compartida.

**GremlinFight:** Pérdida de −0.1 afinidad por terminar pelea (ambos). Almacenado como −t.FightAffinityLoss en código (double-negative anti-pattern).

**Clampeo:** Cada delta acumulado se clampea a ±t.HistoryDeltaClamp (default ±0.5), no per-interaction sino global por par. La afinidad final se clampea a [−1, 1] en EffectiveAffinity.

## Patrones Clave

1. **PairKey ordenada:** `string.CompareOrdinal()` asegura que el par (A,B) siempre produce la misma key que (B,A).
2. **Diccionario estático:** Vive en memoria runtime; no serializado directamente (lo hace SaveSystem en OnQuit/OnPause).
3. **Flag Dirty:** Optimización para CloudSync — solo pushea si hubo cambio desde último pull.
4. **Poda en ImportData:** Evita mantener historia de criaturas ya muertas (orfandad).
5. **S129 Suscripción:** `OnCreatureDeparted` dispara `RemoveFromHistory()` automáticamente.

## Ciclo de Vida S129

1. `OnCreatureDeparted(CreatureDNA dna)` → dispara suscriptor
2. `RemoveFromHistory(dna.UniqueID)` → busca y elimina todos los pares con ese ID
3. `Dirty = true` → próximo push incluye cambios
4. `GameEvents.RegistryChanged` también dispara persist (redundancia defensiva)

## Notas

- NO es MonoBehaviour; es estático puro. Inicialización implícita on first access.
- El orden de argumentos (idA, idB) en RecordInteraction no importa: PairKey normaliza.
- Seeds de afinidad (`SocialAffinity.Compute`) NO se mutaran; deltas solo se suman.
- S65: SOLO persistencia local (social_graph_<playerId>.json). Sync a cloud es futuro (S67 async batch).

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/14 - Social V2]] (capa de historia)
- [[Index/28 - Cimientos y camino a Game Ready]]

## Conexiones

**Entrada:**
- `AgentSocial.CompleteFromPartner()`, `AgentSocial.TickSocializing()` — registran interacciones
- `SaveSystem.LoadSocialGraph()` — carga history al sign-in
- `SaveSystem.SaveSocialGraph()` — exporta para persistencia
- **S129:** `GameEvents.OnCreatureDeparted` — limpia pares huérfanos

**Salida:**
- `AgentSenses.Tick()` — consulta `EffectiveAffinity()` para cada Monchi percibido
- `GameManager.FlushToCloud()` — llama SaveSocialGraph si Dirty
- `CloudSyncService.HandleSignedInAsync()` — importa history con poda
