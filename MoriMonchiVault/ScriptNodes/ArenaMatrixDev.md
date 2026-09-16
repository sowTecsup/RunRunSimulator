---
tags: [script, world, expedition, dev, harness]
---

# ArenaMatrixDev.cs

**Ruta:** `World/Expedition/ArenaMatrixDev.cs`

**Responsabilidad:** Harness de desarrollo para simulaciones arena (matriz planes × rivales × semillas). Itera combinaciones, aplica órdenes a DNAs, ejecuta rondas, registra CSV. S122: incluye Role. **S124:** Aviso y salto si `ArenaBases.RoleFor()` falla (Role inválido).

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `void Run(players, rivals, seeds, csvPath)` | Inicia simulación |
| `void Stop()` | Aborta |

## Propiedades

| Propiedad | Descripción |
|-----------|-------------|
| `IsRunning` | Simulación activa |
| `Done` | Completada |
| `Completed / Total` | Progreso |
| `Progress` | UI label |

## Flujo (S124)

1. Itera semillas → players → rivals
2. Por cada: lee Role, abre bases
3. `Apply(entry, Team)`: itera bases
   - **S124:** Si `RoleFor()` retorna invalid: Debug.LogWarning + skip entrada
4. Round.Launch() → registra CSV
5. CSV: Role + base + resultado

## Cambios S124

- `Apply()`: valida Role con `ArenaBases.RoleFor()`
- Si falla: aviso (Debug.LogWarning) y salta entrada
- No rompe simulación; continúa siguiente

## Invariantes

- Role determinístico por Entry
- Regresión de balance post-cambios ArenaBases

## Vinculado a

[[Index/22 - Bajada Nocturna y Linaje]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ArenaBases]], [[ArenaSandbox]], [[ArenaRosterSO]]
