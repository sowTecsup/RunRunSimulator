---
tags: [script, combate, dragon-rps, orchestration]
---

# DragonRpsMatch.cs

**Ruta:** `DragonRps/DragonRpsMatch.cs`

**Responsabilidad:** Orquestación de un combate completo. Clase estática. Resultado encapsulado en `DragonRpsResult` (HitsA/HitsB, Rounds, Winner). Método estático `Play(dragonA, dragonB, policyA, policyB, seed, log)` itera rondas hasta victoria (3 golpes). En cada ronda: ambos eligen acción (rock/paper/scissors) según policy, se resuelve con `ResolveRound()`: RPS rules aplicadas, si hay empate espejo se chequea potencia (poder del ataque), si poderes iguales nadie golpea ("espejo parejo"), ambos roban cartas, rebaraje si se agotan. `IsOver()` retorna true si alguien alcanzó HitsToWin golpes. `Winner()` desempata por comparación de hits.

**S93:** Regla "espejo parejo = nadie golpea" (no mutuo hit). `Reshuffle()` al agotarse deck. `IsOver()` solo por golpes.

## Métodos Estáticos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Play(dragonA, dragonB, policyA, policyB, seed, log)` | `DragonRpsResult` | Simula match completo; retorna resultado |
| `IsOver(sideA, sideB)` | `bool` | True si A o B alcanzó HitsToWin |
| `Winner(sideA, sideB)` | `int` | 1 si A gana, 2 si B, 0 si empate |
| `ResolveRound(sideA, sideB, actionA, actionB)` | `string` | Juega ronda, actualiza hits, loguea outcome |

## Clase DragonRpsResult

| Campo | Tipo |
|-------|------|
| `HitsA` | `int` |
| `HitsB` | `int` |
| `Rounds` | `int` |
| `Winner` | `int` (1/2/0) |

## Lógica de Scoring

1. **RPS puro:** Rock > Scissors > Paper > Rock
2. **Empate (espejo):** Compara `Dragon.Power[(int)action]`
   - Si poderes iguales → "espejo parejo, nadie cede" (sin golpe)
   - Si A más fuerte → A golpea
   - Si B más fuerte → B golpea
3. Golpe solo en victoria clara (no en empate)

## Rebaraje

Tras cada ronda si `!CanAct`:
```csharp
sideA.Reshuffle();
sideB.Reshuffle();
```

Llena hand de nuevo hasta tamaño HandSize.

## Vinculado a

- [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsRules]], [[DragonRpsDragon]], [[DragonRpsSide]], [[DragonRpsBrain]], [[DragonRpsSession]], [[DragonRpsHarness]]

