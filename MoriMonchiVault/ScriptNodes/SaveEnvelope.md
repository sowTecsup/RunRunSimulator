---
tags: [persistence, data-structure, serialization]
---

# SaveEnvelope

**Ruta:** `Scripts/Logic/SaveEnvelope.cs` (assembly `MoriMonchi.Logic`)

**Responsabilidad:** Estructura de datos que encapsula cada guardado en disco y nube. Contiene `Version` (entero), `SavedAtTicks` (timestamp UTC), y `Data` (JToken con el payload JSON). Nunca se instancia directamente; solo [[SaveMigrations]] la construye y escribe.

**S128:** Introducida para robustecer guardados. Toda persistencia pasa por sobre: creaturas, muebles, inventario, grafo social.

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Version` | `int` | Versión del formato de guardado (actualmente 2) |
| `SavedAtTicks` | `long` | `DateTime.UtcNow.Ticks` al escribir; 0 si heredado |
| `Data` | `JToken` | Payload serializado (null si guardado vacío o corrupto) |

## Ciclo de Vida

1. [[SaveMigrations.Read]] lee JSON de disco/nube, lo envuelve en SaveEnvelope
2. Migraciones internas avanzan `Version` si es necesario
3. [[SaveMigrations.Write]] serializa a JSON con sobre completo
4. [[SaveSystem]] lee/escribe usando SaveMigrations (nunca directamente SaveEnvelope)

## Versiones (actuales y planeadas)

| Version | Lotes | Cambios |
|---------|-------|---------|
| v1 (legacy) | Todos | Sin sobre; data directa en raíz |
| v2 | Inventory | `AdventureMaterial` → `Minerita`; se borran `PassiveMaterial` y `EvolutionEssence` |
| v3 (planeado C4) | Todos | Añadir campos de ciclo de vida |
| v4 (planeado C6) | Todos | Sistema de reloj |

## Vinculado a

[[Index/07 - Persistence & Identity]] (S128 sección)

**Conexiones:** [[SaveMigrations]], [[SaveSystem]]

