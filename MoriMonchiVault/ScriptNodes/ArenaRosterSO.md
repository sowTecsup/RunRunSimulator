---
tags: [script, data, scriptableobject, expedition]
---

# ArenaRosterSO.cs

**Ruta:** `Data/Expedition/ArenaRosterSO.cs`

**Responsabilidad:** Tabla de configuración de MoriMonchis para la sandbox Arena. Cada Entry contiene nombre, equipo, personalidad (Sociability/Boldness), apariencia (BodyShapeID, BaseColor), IDs de partes (S109), **Role (S122)** y ocupación (heredado, no usado en S122). **S122:** Role abre 2 de 3 bases; la variante sale del Role y se mapea a órdenes concretas.

**Entry struct (S109, S122):**
- `string Name` — nombre de la criatura
- `ExpeditionTeam Team` — Player o Rival
- `float Sociability`, `Boldness` — personalidad (0-1)
- `string BodyShapeID`, `HornID`, `BackID`, `WingID` — IDs de partes genéticas (S109)
- `Color BaseColor` — color del cuerpo
- `Role Role` — **(S122)** Protector/Agresivo/Empático
- `Occupation Occupation` — heredado; no usado en S122 (ahora derivado de Role+base)

**Métodos:**
- `PopulateDefaults()` — **Botón Odin**: precarga 6 ejemplares con roles variados

**S109 Cambios:**
- Agregados HornID, BackID, WingID (opcionales)

**S122 Cambios:**
- **Role field nuevo** (enum Protector/Agresivo/Empático)
- Occupation seguirá existiendo para compatibilidad, pero Prepare() ignora y usa bases
- ArenaMatrixDev puede iterar sobre Role/bases en lugar de Occupation

**Flujo S122:**
1. Entry.Role determina qué bases están abiertas
2. ArenaCastPlanner.FromRoster() copia Role al DNA
3. Panel de plan muestra Role + dos bases abiertas
4. Órdenes derivadas de Role+base seleccionada

**Invariantes:**
- Role y bases: autoridad única en S122
- IDs de parte opcionales (S109)
- Ocupación heredado pero ignorado en S122+

**Vinculado a:** [[Index/24 - Puente Tienda-Arena]], [[Index/22 - Bajada Nocturna y Linaje]] (S122)

**Conexiones:** [[ArenaCastPlanner]], [[ArenaBases]], [[ArenaMatrixDev]], [[CreatureDNA]]
