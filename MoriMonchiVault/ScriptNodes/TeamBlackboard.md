---
tags: [script, data, expedition, communication]
---

# TeamBlackboard.cs

**Ruta:** `World/Expedition/TeamBlackboard.cs`

**Responsabilidad:** Pizarra por equipo (Player/Rival) que recopila inteligencia de exploración (S103): vetas conocidas reportadas por scouts, pings de reporte, ciclos de visita. Estructuras `KnownVein` (MaterialPickup, Remaining, ReportedAt) y `BlackboardPing` (posición, timestamp). Métodos: `NextSite()` para elegir siguiente veta a visitar (nearest no visitada), `ReportVein()` para registrar veta (fresca si cambió quantity o pasó `repeatSeconds`), `BestKnownVein()` para IA de recolección, `MarkVisited()` para tracking. Pings se descartan tras `PingKeepSeconds` (6s).

**Constructor:**
- `TeamBlackboard(ExpeditionTeam team)` — inicializa vacío

**Propiedades:**
- `ExpeditionTeam Team { get; }` — identifica equipo
- `IReadOnlyList<KnownVein> KnownVeins { get; }`
- `IReadOnlyList<BlackboardPing> Pings { get; }`
- `int Reports { get; }` — conteo de reportes frescos

**Métodos públicos:**
- `SetSites(IReadOnlyList<MaterialPickup> veins)` — reset inicial: limpia known, visited, pings, Reports=0
- `MaterialPickup NextSite(Vector3 from, out bool newCycle)` — retorna veta más cercana no visitada; newCycle=true si se reciclaron visited
- `MarkVisited(MaterialPickup site)` — marca como visitada, guarda lastSite
- `bool ReportVein(MaterialPickup vein, float now, float repeatSeconds)` — registra veta, retorna true si fresca (incrementa Reports, añade ping). Fresco = no existe en known O (quantity cambió Y pasó repeatSeconds)
- `MaterialPickup BestKnownVein(Vector3 from, MaterialPickup exclude)` — score = Remaining / (1 + distance*0.15), retorna best
- `PrunePings(float now)` — limpia pings viejos (now - time > 6s)

**Internals:**
- `Nearest(Vector3 from)` — elige veta no visitada más cercana, evita lastSite si hay múltiples

**Structs:**
- `KnownVein` — Vein (ref), Remaining (cantidad conocida), ReportedAt (timestamp del último report)
- `BlackboardPing` — Position (3D), Time (timestamp del ping)

**S103:** Instanciada por `ArenaSandbox.BoardFor(team)` (lazy), poblada en `SpawnCast()` con minerales de la sala. Consultada por `AgentScout.TryEngage()` y `AgentExpedition.TryGatherEngage()` para navegación inteligente. Dibujada por `ArenaRoomCueOverlay.DrawBlackboards()` como anillos de vetas conocidas + pings.

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[AgentScout]], [[AgentExpedition]], [[ArenaRoomCueOverlay]], [[MaterialPickup]]
