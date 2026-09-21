---
tags: [script, world, expedition, data-loading, static-utility]
---

# ArenaCastSource.cs

**Ruta:** `World/Expedition/ArenaCastSource.cs`

**Responsabilidad:** Utilidad estática de lectura pura (sin estado mutable) para cargar el elenco de criaturas desde el save local o muestrear un pool vía semilla. Busca el archivo `creature_database*.json` más reciente en PersistentDataPath, deserializa y filtra vivos. **S129:** `SaveSystem.Deserialize()` ahora devuelve `RegistryData`, usa `data.Alive` en lugar de acceso directo.

## Métodos Estáticos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `List<CreatureDNA> LoadLocal()` | Carga save local — lista archivos `creature_database*.json`, ordena por LastWriteTime desc (más reciente), deserializa, filtra vivos (IsDead=false), ordena por Timestamp asc, logguea |
| `List<CreatureDNA> Pick(List<CreatureDNA> pool, int count, int seed)` | Muestrea pool con Fisher-Yates shuffle seeded, retorna primeros count elementos |

## LoadLocal() Flujo (S129)

1. Llamar SaveSystem.LoadDatabaseCopy() si scope activo (S119: expedición siempre tiene scope)
2. Si scoped: retorna copia filtrada y ordenada
3. Si no scoped o error: lista archivos `creature_database*.json` en Application.persistentDataPath
4. Si no hay: retorna empty
5. Ordena por LastWriteTimeUtc desc (más reciente primero)
6. SaveSystem.Deserialize(file[0].ReadAllText()) → retorna `RegistryData`
7. Filtra: `data.Alive` — diccionario de vivas; itera Values y excluye IsDead
8. Ordena por Timestamp asc
9. Debug.Log(f"[ArenaCastSource] {result.Count} MoriMonchis vivos en {filename}")
10. Try-catch + warning si error
11. Retorna lista

## Pick() Flujo

1. Si pool vacío o count ≤ 0: retorna empty
2. Copia pool a new List<CreatureDNA>(pool)
3. Fisher-Yates shuffle seeded con System.Random(seed):
   - Para i = Count-1 down to 1:
     - j = rng.Next(i+1)
     - Swap order[i] ↔ order[j]
4. Toma primeros count elementos, añade a picked
5. Retorna picked

## Invariantes S129

- **Lectura pura:** sin estado, sin mutación global
- **Determinístico:** Pick es reproducible por seed vía Fisher-Yates
- **Más reciente:** LoadLocal ordena por LastWriteTime (respeta histórico de saves)
- **Filtro vivos:** solo del diccionario `data.Alive` (deserialización S129)
- **Logging:** info y warning a consola para debugging
- **S129:** integridad de save local previo a transición a arena con RegistryData

## Casos de Uso

- **ArenaCastPlanner.Prepare():** llama LoadLocal() + Pick() si Mode=LocalSave
- **ArenaCastPicker.BuildGrid():** usa sandbox.LocalPool que viene de ArenaCastSource
- **S129:** flujo expedición depende de LoadLocal() consistent con RegistryData.Alive

## Conexiones

- [[SaveSystem]] (Deserialize, LoadDatabaseCopy)
- [[ArenaCastPlanner]] (consume LoadLocal + Pick)
- [[CreatureDNA]] (filtro IsDead, Timestamp, BaseColor)
- [[ArenaSandbox]] (opción LocalSave vs Roster)
- [[RegistryData]] (estructura de save S129)

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]], [[Index/24 - Puente Tienda-Arena]]
