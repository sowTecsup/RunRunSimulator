---
tags: [script, data, expedition, plans]
---

# ArenaMatrixPlans.cs

**Ruta:** `World/Expedition/ArenaMatrixPlans.cs`

**Responsabilidad:** Catalogo de planes (configuraciones de órdenes + personalidades) para simulaciones de arena. Define struct `ArenaMatrixTeam` (Name, Orders[], Boldness[], Sociability[]) y factories. Expone 3 conjuntos predefinidos: `Plans16` (16 arquetipos × loot), `Subset10` (10 mejores), `Personalities6` (6 perfiles de personalidad pura).

**Struct ArenaMatrixTeam:**
- `string Name` — nombre del equipo (ej "Muralla", "Jauria")
- `ArenaOrders[] Orders` — array de 3 órdenes (one per creature)
- `float[] Boldness` — array de 3 valores [0,1]
- `float[] Sociability` — array de 3 valores [0,1]

**Factories estáticas:**
- `ArenaMatrixTeam Team(string name, ArenaOrders a, b, c)` — crea equipo con orders 3-tuple y personalidades default (0.5, 0.5)
- `ArenaMatrixTeam Team(string name, ArenaOrders[] orders, float[] boldness, float[] sociability)` — crea con personalidades custom

**Planes predefinidos:**

**Plans16:** 16 equipos (4 arquetipos × 4 loot combos):
- Guardián: GuaC (Big), GuaV (Small)
- Cazador: CazC (Big), CazV (Small)
- Recolector: RecC (Big), RecV (Small)
- Señuelo: SenC (Big), SenV (Small)
- Equipos: Muralla, MurallaVetas, Hormiguero, HormigueroSenuelo, Jauria, JauriaCentro, Emboscada, Mixta, Codicia, Escolta, Senuelos, DobleGuardia, ContraJauria, Fortin, Engano, Rebano

**Subset10:** Top 10 de Plans16 para testing rápido:
- Muralla, Fortin, Jauria, Hormiguero, HormigueroSenuelo, Senuelos, Engano, Emboscada, Codicia, Mixta

**Personalities6:** 6 perfiles puros (personalidades locked extremas + órdenes):
- RosterA: Cazador Big, Recolector Small, Guardián Small + personalidades asimétri cas
- RosterB: variación RosterA con diferentes órdenes
- TresTimidos: 3 Recolectores, todos tímidos (0.15) y sociables (0.85)
- TresOsados: 3 Cazadores, todos osados (0.90) y solitarios (0.25)
- OsadosSociables: Guardián + Guardián + Recolector, osados×2 + tímido, sociables×2 + sociable
- TimidosSolitarios: Señuelo + Señuelo + Recolector, todos tímidos/solitarios

**Métodos públicos:**
- `static ArenaMatrixTeam Find(ArenaMatrixTeam[] set, string name)` — busca por nombre en array

**Integración:**
- Usado por `ArenaMatrixDev.Run()` para iterar planes
- Consumido por devConsole matrix simulator
- Ordenes y personalidades se inyectan en DNAs por índice de criatura

**Invariantes:**
- Cada equipo: 3 órdenes, 3 boldness, 3 sociability (sync index)
- Plans16 es la matriz completa (base para Subset10)
- Personalities6 explora extremos de personalidad para balance testing

**Vinculado a:** [[Index/23 - Arena Sandbox y Expedicion]]

**Conexiones:** [[ArenaMatrixDev]], [[ArenaOrders]], [[ExpeditionTeam]], [[CreatureDNA]]
