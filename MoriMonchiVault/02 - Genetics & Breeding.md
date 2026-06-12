---
tags: [memory-bank, genetics, breeding, dna, visual-assembler]
---
1
# 02 — Genetics & Breeding

## Responsabilidad Core (TL;DR)
Define la identidad, progresión y aspecto visual de las criaturas. Resuelve emparejamientos genéticos, herencia de atributos y el ensamblaje modular de mallas 3D.

## Source of Truth & Centralización
- **Data (DTO):** `CreatureDNA.cs`. Contiene identidad (UniqueID), linaje, stats, genoma y registro de combates.
- **Orquestador (Asset):** `CreatureDatabaseSO.cs`. Referencia sub-DBs (Arms, Eyes, etc) y valida IDs.
- **Lógica de Cruce:** `BreedingService.cs` (lógica local pura) y `AsyncBreedingService.cs` (comunicación UGS).
- **Diccionario Visual:** `PartVisualBankSO.cs`. Mapeo de IDs de partes a Prefabs 3D.

## Visual Assembler (Ensamblaje 3D)
- **Visualizer (`MoriMonchiVisualizer.cs`):** Componente en la raíz del prefab. Actúa como el mapa de sockets (Body, ArmL, ArmR, EyeL, EyeR, Mouth).
- **Partes (`BodyPartJoint.cs`):** Cada prefab define su propio punto de pivote (`insertionJoint`) y si debe espejarse (`isMirror`).
- **Invariante Visual:** El prefab del cuerpo base NO tiene referencias rígidas a brazos/ojos. El Visualizer ensambla dinámicamente instanciando hijos en los sockets, alineando los `insertionJoint`.

## Flujo de Breeding (Async)
1. **Validación Local:** Padres vivos (`!IsDead`), bajo límite de crías (`BreedCount < 4`), y libres (`!IsBusy`).
2. **Start (Cloud):** Cliente pide inicio. UGS estampa el timestamp en `breeding_eggs_<playerId>`. Cliente marca padres como `BusyState.Breeding`.
3. **Hatch (Cloud):** Se valida el tiempo contra el reloj del servidor. UGS borra el huevo del array.
4. **Breed (Local):** `BreedingService.Breed()` ejecuta la herencia: 50/50 por slot (abuelos/random según `InheritanceOddsTableSO`). Stats ±1 delta (mínimo 1).
5. **Eventos:** Se dispara `GameEvents.OnBreedingCompleted` y subsecuentemente `OnRegistryChanged`.

## Auto-Pairing en Corral (`BreedingContainer`)

Extiende el flujo de Breeding Async con una capa visual-world en escena:

- `BreedingAffinityTableSO`: `SerializedScriptableObject` con diccionario `(Personality, Personality) → float`. Matriz **6×6** simétrica (incluye `Grumpy`). Botón *Seed Defaults* carga valores razonables. Patrón `Current` singleton igual que `InheritanceOddsTableSO`.
- `BreedingContainer` tira un dado cada `rollInterval` segundos. Filtra por elegibilidad idéntica a `BreedingController.FillRandomBreeders` (`!IsDead`, `!IsBusy`, género opuesto, bajo límite de crías). Chance = `affinity × diceChance`. Pareja exitosa aplica cooldown por `UniqueID` para evitar re-roll inmediato.
- Backend híbrido: `useAsyncBreed` toggle en inspector — `true` → `AsyncBreedingService.StartBreedingAsync` (genera huevo server-side); `false` → breed local instantáneo.
- Hook Feel-ready: `UnityEvent onPairFormed` (sin acoplamiento de código).

**Pendiente próxima sesión:** `BreedingController` como singleton para que `BreedingContainer` resuelva `AsyncBreedingService` + `BreedingAffinityTableSO` desde él, sin doble asignación en inspector. También: cartelito visual sobre la pareja durante apereamiento y al momento de hatchear.

## Reglas de Oro (Invariantes)
- **Genetic String Inmutable:** El string (ej. `BS0-A3-E1-M2-FF00AA`) nunca cambia tras nacer.
- **Autoridad del Server:** El cliente jamás escribe la fecha de inicio/fin de un breed, evita el time-cheat.
- **IDs sin guiones:** Los IDs autogenerados de las partes (`BS1`, `A0`) nunca llevan guión medio `-`.
- **Gender y Personality:** Son meta-atributos generados proceduralmente; NO forman parte del genetic string.
