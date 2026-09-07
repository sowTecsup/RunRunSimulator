---
tags: [script, data, expedition, struct]
---

# ArenaRoomRead.cs

**Ruta:** `Data/Expedition/ArenaRoomRead.cs`

**Responsabilidad:** Struct que captura fotografía de la sala de arena para presentación al jugador: valor de lode central, cantidad y total de vetas, conteo de obstáculos, distancias desde salida (a lode y a veta más cercana). Input para `ArenaSandbox.ReadRoom()` post-generación.

**Campos públicos:**
- `int LodeValue` — unidades del cristal central
- `int VeinCount` — cantidad de vetas chicas
- `int VeinTotal` — total de unidades en vetas
- `int Obstacles` — conteo de obstáculos en la sala
- `float NearVeinDistance` — distancia en metros a la veta más cercana desde salida
- `float CenterDistance` — distancia en metros al lode central desde salida

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[ArenaOrderCatalog]], [[ArenaPlanPanel]]
