---
tags: [script, data, expedition]
---

# ArenaRoundSummary.cs

**Ruta:** `World/Expedition/ArenaRoundSummary.cs`

**Responsabilidad:** Captura estadísticas finales de ronda en struct `ArenaRoundStat` por criatura. Extrae DNA, **órdenes (S104)**, team, counters (asegurados, minados, huidas, golpes, caídas, reportes). Consumida por ArenaRound.End() y ArenaResultPanel.Show(). **S119:** Stats retorna con Id de DNA para historial y serialización.

## Struct ArenaRoundStat

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | string | **S119** UniqueID del DNA (para referenciar en historial/persistencia) |
| `Name` | string | CustomName de DNA |
| `Team` | ExpeditionTeam | Player o Rival |
| `Orders` | ArenaOrders | Órdenes activas (S104 NUEVO) |
| `Color` | Color | BaseColor (a=1f) |
| `Secured` | int | Material depositado en salida (asegurado) |
| `Collected` | int | Material minado total (incluyendo perdido) |
| `HitsLanded` | int | Golpes exitosos en clash |
| `TimesKnocked` | int | Veces derribado |
| `Reports` | int | Vetas reportadas (scouts) |
| `Fled` | int | Conteo de huidas (S104 NUEVO) |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `static List<ArenaRoundStat> Capture(IReadOnlyList<MoriMonchiController> spawned)` | `List<ArenaRoundStat>` | Itera controllers, extrae stats de cada agente, retorna lista serializable |

## Flujo Capture (S119)

```
1. Para cada MoriMonchiController en spawned:
   - Si null o DNA null, skip
   - Extrae:
     - Id = agent.DNA.UniqueID
     - Name = agent.DNA.CustomName
     - Team = agent.Team
     - Orders = agent.Orders (S104)
     - Color = agent.DNA.BaseColor (a = 1f)
   - Metrics del Agent:
     - Secured = agent.SecuredMaterial
     - Collected = agent.CollectedMaterial
     - HitsLanded = agent.ClashHitsLanded
     - TimesKnocked = agent.ClashTimesKnocked
     - Reports = agent.ScoutReports
     - Fled = agent.TimesFled (S104)
   - Crea ArenaRoundStat y añade a resultado
2. Retorna lista serializable
```

## S104 Cambios

- ArenaRoundStat.Orders agregado (extrae agent.Orders)
- ArenaRoundStat.Fled agregado (extrae agent.TimesFled)
- Usado por ArenaResultPanel para mostrar "huyó N veces"

## S119 Cambios

- **ArenaRoundStat.Id agregado** para serialización con UniqueID
- Permite historial de resultados identifica criaturas en cloud/saves
- ExpeditionResult.Stats ahora serializable con Id

## Invariantes

- Stats congelados post-ronda (snapshot inmutable)
- Color.a forzado a 1f (opacidad visual)
- Null-checks previenen crashes con controllers destruidos
- Métricas vienen directamente del Agent (no recalculadas)

## Uso en S119

- ArenaPlanPanel.lastResult.Stats = Capture(arena.Spawned)
- ExpeditionResult.Stats serializado y retornado a tienda
- ExpeditionBridge.ApplyResult() puede usar Stats para análisis
- Historial de rondas puede identificar criaturas por Id

## Vinculado a

[[Index/22 - Bajada Nocturna y Linaje]], [[Index/23 - Arena Sandbox y Expedicion]], [[Index/24 - Puente Tienda-Arena]]

**Conexiones:** [[MoriMonchiController]], [[MoriMochiAgent]], [[ArenaRound]], [[ArenaResultPanel]], [[CreatureDNA]], [[ArenaOrders]], [[ArenaPlanPanel]], [[ExpeditionResult]], [[ExpeditionBridge]]
