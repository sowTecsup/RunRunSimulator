---
tags: [script, genetics, breeding, world-container]
---

# BreedingContainer

**Ruta:** `World/Containers/BreedingContainer.cs`

**Responsabilidad:** Corral de cría (hereda de MoriMochiContainer). Pairing automático con affinity×diceChance, restaura pasivamente necesidades. Gestiona visuals (courtship poses). S131: Cría síncrona vía `BreedingController.StartBreeding/TryHatch/CancelBreeding`. IsAdult usa `CreatureDNA.AgeDays(today)`.

## Interacción (IInteractable)

**Tap E:**
1. Detecta si hay criaturas criando (BusyReason.Breeding)
2. Si huevo listo: `BreedingController.TryHatch(motherID, fatherID)` → procesa HatchResult
   - Hatched: anima eclosión
   - NotReady: toast "Aún falta tiempo"
   - InsufficientMinerita: toast "Insuficiente Minerita"
   - Invalid: toast "Error"
3. Si huevo no listo: muestra timer restante

## Métodos S131 (Cría Síncrona)

| Método | Descripción |
|--------|-------------|
| `BreedingController.StartBreeding(motherID, fatherID)` | Inicia cría local (marca BusyState, deduce energía, fija BreedReadyAt) |
| `BreedingController.TryHatch(motherID, fatherID)` | Hatcha si ready (retorna HatchResult) |
| `BreedingController.CancelBreeding(motherID, fatherID)` | Cancela cría (limpia BusyState) |

## IsAdult (S131)

```csharp
private bool IsAdult(CreatureDNA dna)
{
    if (dna == null) return false;
    int today = GameClock.Instance != null ? GameClock.Instance.Day : 1;
    int ageDays = dna.AgeDays(today);
    
    // Threshold: ejemplo 7 días = adulto
    return ageDays >= AdultAgeThresholdDays;  // default 7
}
```

**Propósito:** Determinar elegibilidad de cría por edad (no solo rol).

## TryRollPair (S131)

Cambios menores en S131:
- Sigue usando `BreedingController.Instance.GetAffinity(mother.Role, father.Role)` (S39)
- Agregó check: `IsAdult(mother) && IsAdult(father)` antes de pairing

```csharp
if (!IsAdult(mother) || !IsAdult(father)) return false;  // S131
float affinity = BreedingController.Instance?.GetAffinity(mother.Role, father.Role) ?? 0.5f;
float pairChance = affinity * diceChance;
```

## Campos Serializados (S131)

| Campo | Tipo | Propósito |
|-------|------|----------|
| `adultAgeThresholdDays` | int | Edad mínima para reproducción (default 7) |

## Vinculado a

- [[Index/02 - Genetics & Breeding]]
- [[Index/09 - Active Context]]

## Conexiones

**Sistemas:**
- [[BreedingController]] — API de cría síncrona
- [[IncubationService]] — orquestador (vía BreedingController)
- [[GameClock]] — proporciona Day para AgeDays
- [[GameEvents]] — reacciona a RegistryChanged

## Notas (S131 HC-4)

- **Cría síncrona:** StartBreeding ejecuta localmente (sin await). TryHatch retorna HatchResult enum.
- **Edad adulta:** AgeDays(today) compara BirthDay contra Day actual (simple resta clamped).
- **HatchResult handling:** switch(result) en Interact para animaciones/toasts.
- **Pairing:** Solo adultos pueden emparejar; afinidad por Role (S39).
