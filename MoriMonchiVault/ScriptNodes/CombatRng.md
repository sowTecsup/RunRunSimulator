---
tags: [combat, rng, deterministic, seed]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatRng

**Ruta:** `Systems/Combat/CombatRng.cs`

**Responsabilidad:** RNG determinista xorshift32 inyectado en todas las operaciones aleatorias del combate. Misma seed en dos máquinas → misma secuencia de números. Nunca System.Random ni UnityEngine.Random en combate.

## Constructor & Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `CombatRng(int seed)` | — | Inicializa estado xorshift a partir del seed. Si seed=0, usa valor por defecto `0x9E3779B9` |
| `NextFloat()` | `float` | Próximo float [0, 1), avanza el estado |
| `Range(int minInclusive, int maxExclusive)` | `int` | Próximo entero en rango, vía `NextFloat()` |

## Implementación

xorshift32 de 32 bits: estado interno mutable. Cada `NextFloat()` aplica tres XOR shifts y normaliza a `[0, 1)`.

**Determinismo:** Mismo seed → mismo estado inicial → misma secuencia indefinida. Clave para que ambos clientes (local y async) simulen combates idénticos.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatService]] — inyectado en `Simulate(seed)` y `SimulateCore(rng)`
- [[AsyncCombatService]] — inyectado vía CloudMatchBlob.Seed

## Conexiones

**Uso interno en CombatService:**
- `TakeTurn()` → rolls evasión, crit
- `FireProcs()` → rolls chance de procs
- `CombatEvolution.TryEvolveRandomSlot()` → selecciona slot random
- `CombatService.Simulate()` → crea instancia con seed Guid hash

**En AsyncCombatService:**
- `ApplyResult(CloudMatchBlob)` → crea `new CombatRng(r.Seed)` para simular core idéntico

## Notas

- No es singleton, se instancia per-simulación.
- Estadísticamente uniforme en comportamiento aleatorio (no perfect distribution, pero suficiente para juego).
