---
tags: [memory-bank, script, persistence]
---

# GameManager.cs

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Único orquestador de persistencia: escucha `GameEvents` y ejecuta `SaveSystem.SaveDatabase` + `CloudSyncService.PushToCloud`.

**Vinculado a:** [[Index/07 - Persistence & Identity]]

**Conexiones:** [[GameEvents]], [[SaveSystem]], [[CloudSyncService]], [[CreatureRegistrySO]], [[CreatureGenerator]]
