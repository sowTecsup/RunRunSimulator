---
tags: [script, world, visual, presentation, shader]
---

# MonchiTeamRim.cs

**Ruta:** `World/Creatures/MonchiTeamRim.cs`

**Responsabilidad:** Presentador que anula el rim light genético de un MoriMochi si pertenece al equipo Rival. Lee `agent.Team` en `LateUpdate()` y al cambiar llama `visualizer.SetRimOverride()` (color/power/mask rojo de rival) o `ClearRimOverride()` (vuelve al rim genético). Vinculado en el prefab del agente; tuneado vía knobs serializados en el inspector.

## Responsabilidad Específica

- Monitorea cambio de team (Player ↔ Rival ↔ otro)
- Si es Rival: escribe rim light rojo (1, 0.3, 0.22) en el shader vía `SetRimOverride()`
- Si no es Rival: restaura rim genético vía `ClearRimOverride()`
- Sin lógica de juego; puro presentation

## Campos Públicos/Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `agent` | `MoriMochiAgent` [Required] | Ref al agente |
| `visualizer` | `MonchiVisualizer` [Required] | Ref al visualizador (escribe MPB) |
| `rivalRimColor` | `Color` | Color del rim si Rival (default 1, 0.3, 0.22 = rojo pastel) |
| `rivalRimPower` | `float` [Range(0,1)] | Power del rim si Rival (default 0.7 = bastante luminoso) |
| `rivalRimInsideMask` | `float` [Range(0,1)] | InsideMask del rim si Rival (default 0.4 = afecta interior) |

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `appliedTeam` | `ExpeditionTeam` | Team anterior (cache) |
| `applied` | `bool` | Flag si override fue aplicado |

## Métodos

- `LateUpdate()` — detecta cambio de team, aplica/limpia override
- `OnDisable()` — limpia override al destruir

## Flujo

1. `LateUpdate()` lee `agent.Team`
2. Si `applied && team == appliedTeam`: retorna (nada cambió)
3. Else:
   - `appliedTeam = team`
   - `applied = true`
   - Si `team == ExpeditionTeam.Rival`: `visualizer.SetRimOverride(rivalRimColor, rivalRimPower, rivalRimInsideMask)`
   - Else: `visualizer.ClearRimOverride()`
4. `OnDisable()`: reset flag y limpia override

## Integración

- Hijo del GameObject del agente (MoriMonchiController)
- Prefab del agente incluye refs a MoriMochiAgent y MonchiVisualizer
- Se ejecuta pasivamente cada frame; solo escribe si team cambió
- Knobs editables en el inspector para tuning visual

## Parámetros Por Defecto

```csharp
rivalRimColor = new Color(1f, 0.3f, 0.22f)    // Rojo pastel
rivalRimPower = 0.7f                           // Luminosidad moderada
rivalRimInsideMask = 0.4f                      // Afecta interior
```

## Invariantes

- Sin lógica; puro presentation
- Override es exclusivo (Rival OR no Rival, nunca ambos)
- OnDisable garantiza limpieza
- No toca materiales directamente; delega a MonchiVisualizer.SetRimOverride()

## Cambios S110

- **NUEVO:** Método `SetRimOverride()` en MonchiVisualizer (línea 109-115 en .cs)
  - Escribe `_RimLightColor`, `_RimLight_Power`, `_RimLight_InsideMask`, `_Is_LightColor_RimLight = 0` en MPB
  - Recalcula tintado vía `ApplyLook()`
- **NUEVO:** Método `ClearRimOverride()` en MonchiVisualizer (línea 118-123 en .cs)
  - Restaura rim genético

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[MonchiVisualizer]] — host de SetRimOverride/ClearRimOverride

## Conexiones

**Entrada:**
- `MoriMochiAgent.Team`

**Salida:**
- MPB del pelaje (rim light override)

