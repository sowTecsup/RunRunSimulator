---
tags: [script, world, dev, expedition, tools]
---

# ArenaClashDev.cs

**Ruta:** `World/Expedition/ArenaClashDev.cs`

**Responsabilidad:** Herramienta de desarrollo para testing manual de movimientos de choque en la arena. Expone botones en Odin Inspector para disparar ataques específicos por tipo (Embestida/Picada/Coletazo) contra rivales, tanto índice directo como par más cercano automático. No tiene lógica de juego; solo facilita QA de combate en play mode.

## Campos serializados

- **sandbox:** referencia a [[ArenaSandbox]] para acceder a criaturas spawneadas
- **tuning:** referencia a [[ClashTuningSO]] para acceder a movimientos
- **attackerIndex:** índice (0-N) del atacante en sandbox.Spawned (default 0)

## Propiedades (solo lectura en inspector)

- **Attacker:** nombre legible del atacante actual (si existe) o "—"

## Métodos públicos (botones Odin)

- `Embestida()` — dispara Horn contra rival más cercano a attackerIndex
- `Picada()` — dispara Wings contra rival más cercano a attackerIndex
- `Coletazo()` — dispara Back contra rival más cercano a attackerIndex
- `ClosestEmbestida()` — busca par más cercano en maxDistance 7m, dispara Horn
- `ClosestPicada()` — busca par más cercano en maxDistance 9m, dispara Wings
- `ClosestColetazo()` — busca par más cercano en maxDistance 3.5m, dispara Back
- `Fire(ClashMoveSO move) → bool` — dispara move contra rival más cercano a attackerIndex
- `FireClosestPair(ClashMoveSO move, float maxDistance) → bool` — busca par más cercano dentro de maxDistance, dispara move desde el primero
- `Fire(ClashMoveSO move, int index) → bool` — dispara move desde agente en índice index contra su rival más cercano (validado, loguea resultado)

## Lógica

1. **Fire(move, index)** valida:
   - Play mode activo
   - move != null, sandbox != null
   - índice válido (0 <= index < Spawned.Count)
   - agente en índice válido (no null)
2. Busca rival más cercano en arena que:
   - No sea el atacante
   - Sea rival (AreRivals por ExpeditionTeams)
   - No esté sostenido/volando/recuperándose
3. Si existe rival, llama `attacker.ForceClash(move, rival)` (de [[MoriMochiAgent]])
4. Loguea resultado: nombre atacante → tipo movimiento contra nombre rival: "ok" o "rechazado"

## Diferencia con automático

- [[AgentClash.TryEngage()]] valida Boldness, cooldown, EngageRange automáticamente
- `Fire()` **no valida** eso; fuerza el ataque sin importar estado

## Consumo

- Solo se usa en editor Play mode
- Típicamente en ArenaSandbox scene para debugging visual de combates

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[ArenaSandbox]]
- [[MoriMochiAgent]]
- [[ClashMoveSO]]
- [[ClashTuningSO]]
