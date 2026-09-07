---
tags: [script, world, expedition, communication]
---

# TeamBlackboard.cs

**Ruta:** `World/Expedition/TeamBlackboard.cs`

**Responsabilidad:** Pizarra compartida por equipo (Player/Rival) que centraliza inteligencia de exploración (S103). Recopila vetas conocidas reportadas por scouts, pings visuales, ciclos de visita. Métodos: `NextSite()` elige siguiente veta, `ReportVein()` registra con deduplicación, `BestKnownVein()` puntúa por cantidad+distancia (S104: `excludeLode` bias pequeños), `NearestSite()` fallback puro lejano (S104 NEW), `MarkVisited()` tracking. Pings se limpian tras 6s.

**Constructor:**
- `TeamBlackboard(ExpeditionTeam team)` — inicializa vacío

**Propiedades:**
- `ExpeditionTeam Team { get; }` — identifica equipo
- `IReadOnlyList<KnownVein> KnownVeins { get; }`
- `IReadOnlyList<BlackboardPing> Pings { get; }`
- `int Reports { get; }` — conteo de reportes frescos

**Métodos públicos:**
- `void SetSites(IReadOnlyList<MaterialPickup> veins)` — reset: limpia known, visited, pings, Reports=0. Llamado por ArenaSandbox al prepara expedición
- `MaterialPickup NextSite(Vector3 from, out bool newCycle)` — retorna veta más cercana no visitada. newCycle=true si se reciclaron visited (scout completó pasada)
- `void MarkVisited(MaterialPickup site)` — marca como visitada, guarda lastSite (preferencia en Nearest)
- `bool ReportVein(MaterialPickup vein, float now, float repeatSeconds)` — registra veta, retorna true si fresca (incrementa Reports, añade ping). Fresco = no existe en known O (quantity cambió Y pasó repeatSeconds). Usado por AgentScout.Tick()
- `MaterialPickup BestKnownVein(Vector3 from, MaterialPickup exclude, bool excludeLode = false)` → MaterialPickup — score = Remaining / (1 + distance*0.15). **S104:** si excludeLode=true, salta lodes (para Gatherer que quiere vetas chicas). Usado por AgentGatherer.PlannedSite()
- `MaterialPickup NearestSite(Vector3 from, bool excludeLode)` → MaterialPickup — **S104 NUEVO** fallback puro distancia, no puntúa por cantidad. Usado por AgentGatherer como último recurso
- `void PrunePings(float now)` — limpia pings > 6s. Llamado tras ReportVein

**Internals:**
- `MaterialPickup Nearest(Vector3 from)` — veta no visitada más cercana
- Const `PingKeepSeconds = 6f`

**Structs:**
- `KnownVein` — Vein (ref), Remaining (cantidad), ReportedAt (timestamp)
- `BlackboardPing` — Position (3D), Time (timestamp)

**S103:** Instanciada por `ArenaSandbox.BoardFor(team)`, poblada en `SpawnCast()` con minerales. Consultada por `AgentScout` (NextSite) y `AgentExpedition.TryGatherEngage()` (BestKnownVein). Dibujada por ArenaRoomCueOverlay como anillos + pings.

**S104:** Métodos `BestKnownVein` y `NearestSite` con parámetro `excludeLode` para sesgar recolección hacia vetas chicas cuando se ordena Small.

**Invariantes:**
- Un board por equipo (instanciado lazy)
- Pings efímeros: 6s máximo de visualización
- Reports contador acumulativo en sesión
- ExcludeLode es hint, no obligatorio (Gatherer todavía puede minar lodes si no hay vetas)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaSandbox]], [[AgentScout]], [[AgentGatherer]], [[AgentExpedition]], [[ArenaRoomCueOverlay]], [[MaterialPickup]], [[ExpeditionRulesSO]]
