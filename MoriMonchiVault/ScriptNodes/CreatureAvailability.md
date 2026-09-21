---
tags: [utility, static, lifecycle]
---

# CreatureAvailability

**Ruta:** `Data/Genetics/CreatureAvailability.cs`

**Responsabilidad:** Utilidad estática que responde si se puede usar una criatura en diferentes contextos. Tres preguntas: libre (sin restricciones), bien cuidada (cumple umbrales de necesidades), apta para explorar (ambas). Calcula cuál es la necesidad más crítica.

## Métodos Públicos

| Método | Parámetros | Retorna | Descripción |
|--------|-----------|---------|-------------|
| `IsFree` | `CreatureDNA dna` | `bool` | ¿No está muerta, vendida ni ocupada (BusyReason)? |
| `IsWellCared` | `CreatureDNA dna`, `CareGateSO gate` | `bool` | ¿Cumple umbrales de `gate` en Health/Energy/Affect?` |
| `CanExplore` | `CreatureDNA dna`, `CareGateSO gate` | `bool` | `IsFree && IsWellCared` (lista apta para bajada) |
| `WeakestNeed` | `CreatureDNA dna`, `CareGateSO gate` | `NeedType?` | Cuál necesidad está más deficiente (null si bien cuidada) |

## Lógica

**IsFree:** `!IsDead && !IsSold && !IsBusy` (verifica campos de DNA)

**IsWellCared:** Compara `dna.Needs.{Health/Energy/Affect}` contra `gate.{MinHealth/MinEnergy/MinAffect}`. null gate = siempre true.

**WeakestNeed:** Calcula brecha = `MinX - Needs.X` para cada necesidad, retorna la de mayor brecha. Null si gate=null, dna=null, dna.Needs=null, o ya IsWellCared.

## Caso de uso

- UI: mostrar si criatura puede bajar (CanExplore → mostrar botón)
- UI: hint de qué necesidad cuidar (WeakestNeed → texto "Necesita energía")
- Bajada: filtrar elenco antes de depart (CanExplore)

## Vinculado a

[[Index/23 - Arena y bajada nocturna]]

**Conexiones:** [[CreatureDNA]], [[CareGateSO]], [[NeedsState]]
