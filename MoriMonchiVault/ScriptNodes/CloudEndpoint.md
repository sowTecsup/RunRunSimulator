---
tags: [script, cloud]
---

# CloudEndpoint.cs

**Ruta:** `Systems/Cloud/CloudEndpoint.cs`

**Responsabilidad:** Single entry point para llamadas a Cloud Code (Endpoint.CallAsync + JSON deserialize). Clase estática con dos overloads: `CallAsync(string endpoint, Dictionary<string, object> payload)` devuelve `Task<string>` (raw); `CallAsync<T>(endpoint, payload)` devuelve `Task<T>` (deserializado). Cada servicio (AsyncCombatService, AsyncBreedingService) llama aquí y luego reconcilia su propio registro diferente.

**Vinculado a:** [[Index/04 - Cloud Services]]

**Conexiones:** [[AsyncCombatService]], [[AsyncBreedingService]]
