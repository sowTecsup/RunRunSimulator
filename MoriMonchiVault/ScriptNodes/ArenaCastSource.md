---
tags: [script, world, expedition, data-loading, static-utility]
---

# ArenaCastSource.cs

**Ruta:** `World/Expedition/ArenaCastSource.cs`

**Responsabilidad:** Utilidad estática de lectura pura (sin estado mutable) para cargar el elenco de criaturas desde el save local o muestrear un pool vía semilla. Busca el archivo `creature_database*.json` más reciente en PersistentDataPath, deserializa e filtra vivos.

## Métodos Estáticos

- `LoadLocal() → List<CreatureDNA>` — lee save local:
  1. Lista archivos `creature_database*.json` en Application.persistentDataPath
  2. Si no hay: retorna empty
  3. Ordena por LastWriteTimeUtc desc (más reciente primero)
  4. SaveSystem.Deserialize(file[0].ReadAllText())
  5. Filtra: `!dna.IsDead`
  6. Ordena por Timestamp asc
  7. Debug.Log cantidad
  8. Try-catch + warning si error
  9. Retorna lista

- `Pick(List<CreatureDNA> pool, int count, int seed) → List<CreatureDNA>` — muestrea pool:
  1. Si pool vacío o count ≤ 0: retorna empty
  2. Fisher-Yates shuffle seeded
  3. Toma primeros `count` elementos
  4. Retorna lista

## Invariantes S102

- **Lectura pura:** sin estado, sin mutación global
- **Determinístico:** Pick es reproducible por seed vía Fisher-Yates
- **Más reciente:** ordena por LastWriteTime (respeta histórico de saves)
- **Filtro vivos:** solo IsDead=false
- **Logging:** info y warning a consola para debugging
- **Non-alloc parcial:** reutiliza lista Order en Pick, pero crea nueva para resultado

## Casos de Uso

- **ArenaCastPlanner.Prepare():** llama LoadLocal() + Pick() si Mode=LocalSave
- **ArenaSandbox:** obtiene elenco rival desde LoadLocal()

## Conexiones

- [[SaveSystem]] (Deserialize)
- [[ArenaCastPlanner]] (consume LoadLocal + Pick)
- [[CreatureDNA]] (filtro IsDead, Timestamp)
- [[ArenaSandbox]] (opción LocalSave vs Roster)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]
