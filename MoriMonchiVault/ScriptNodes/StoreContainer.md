---
tags: [script, world]
---

# StoreContainer.cs

**Ruta:** `World/Containers/StoreContainer.cs`

**Responsabilidad:** Corral que restaura las 3 necesidades a `restoreRate/s`. Hereda `MoriMochiContainer`. Evento `OnDisplayContentsChanged(IReadOnlyList<MoriMochiAgent>)` disparado cuando el count de ocupantes cambia (polling en Update).

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiContainer]], [[NeedsState]], [[MoriMochiAgent]]
