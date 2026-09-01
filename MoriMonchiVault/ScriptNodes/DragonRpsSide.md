---
tags: [script, combate, dragon-rps, state]
---

# DragonRpsSide.cs

**Ruta:** `DragonRps/DragonRpsSide.cs`

**Responsabilidad:** Estado en-juego de un lado del combate: dragon asignado, deck shuffleado privadamente, mano de cartas (HandSize), descarte público, contador de golpes. Guarda RNG privadamente para rebarajeos deterministas. Métodos: `Play(action)` mueve de mano a descarte, `Draw()` repone desde deck (si hay), `Reshuffle()` (vuelve descarte al deck + rebaraja + rellena mano), `RemainingByType()` (calcula público: cuántas de cada tipo quedan = reparto original - descartadas). `CanAct` señala si hay mano o no.

**S93:** Método `Reshuffle()` agrega. RNG guardado privadamente (final). Motor de habilidad: `RemainingByType()` debe ser visible en UI.

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Dragon` | `DragonRpsDragon` | Dragon de este lado |
| `Deck` | `List<DragonAction>` | Cartas no jugadas (shuffle privado) |
| `Hand` | `List<DragonAction>` | Cartas jugables (tamaño <= HandSize) |
| `Discard` | `List<DragonAction>` | Cartas ya jugadas |
| `Hits` | `int` | Golpes acumulados |

## Propiedades

| Propiedad | Retorna | Descripción |
|-----------|---------|-------------|
| `CanAct` | `bool` | True si Hand.Count > 0 |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Play(DragonAction action)` | `void` | Mueve acción de Hand a Discard |
| `Draw()` | `void` | Mueve última carta de Deck a Hand (si Deck no vacío) |
| `Reshuffle()` | `void` | Vuelve Discard al Deck, rebaraja, rellena Hand hasta HandSize |
| `RemainingByType()` | `int[]` | Array de conteos: cuántas de cada acción quedan por jugar (basado en descarte actual) |

## Ciclo de Vida

1. Constructor: `Dragon.BuildDeck()` → shuffle → DrawInitialHand (HandSize)
2. Cada ronda: `Play(action)` → `Draw()` → si `!CanAct` → `Reshuffle()`
3. Match termina cuando alguien alcanza HitsToWin

## RemainingByType Detalle

Retorna array de size ActionCount:
```
remaining[actionType] = Dragon.Counts[actionType] - cardsOfTypInDiscard
```

Esta info es pública (no está oculta) para permitir estrategia visible en UI.

## Vinculado a

- [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[DragonRpsRules]], [[DragonRpsDragon]], [[DragonRpsMatch]], [[DragonRpsSession]]

