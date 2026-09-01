---
tags: [script, cloud]
---

# CloudEndpoint.cs

**Ruta:** `Systems/Cloud/CloudEndpoint.cs`

**Responsabilidad:** Entry point estático para llamadas a Cloud Code. Dos overloads de `CallAsync()`: raw string, o genérico deserializado. Método utilitario `Guarded()` para wrapping try-catch automático: ejecuta task, captura exception, loguea, actualiza status UI. Simplifica repetición de catch blocks en llamadores (CloudAuth, CloudSyncOps).

**S93:** Agregado método `Guarded(statusOp, logOp, op, setStatus)` (4 params). Reduces copy-paste de error handling en suscriptores.

## Métodos Estáticos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `CallAsync(endpoint, payload)` | `Task<string>` | Invoca Cloud Code, retorna JSON string raw |
| `CallAsync<T>(endpoint, payload)` | `Task<T>` | Invoca Cloud Code, retorna deserializado a T |
| `Guarded(statusOp, logOp, op, setStatus)` | `Task<bool>` | Ejecuta op con try-catch; actualiza status UI; retorna true si éxito |

## Guarded Pattern (S93)

```csharp
public static async Task<bool> Guarded(
    string statusOp,           // Mensaje status (ej. "Push")
    string logOp,              // Log prefix (ej. "PushAsync")
    Func<Task> op,             // Operación a ejecutar
    Action<string> setStatus)  // Callback para actualizar UI status
{
    try
    {
        await op();
        return true;
    }
    catch (Exception e)
    {
        setStatus($"{statusOp} error: {e.Message}");
        Debug.LogError($"[CloudSync] {logOp} failed: {e}");
        return false;
    }
}
```

Uso:
```csharp
await CloudEndpoint.Guarded("Push", "PushAsync", PushInternalAsync, s => status = s);
```

## Vinculado a

- [[Index/04 - Cloud Services]]

**Conexiones:** [[CloudAuth]], [[CloudSyncOps]], [[CloudCodeService]]

