---
tags: [memory-bank, script, player-world]
---

# MoriMochiSpawner.md

**Ruta:** `World/MoriMochiSpawner.cs`

**Responsabilidad:** Instancia criaturas en escena desde `CreatureRegistrySO`. Crea `MoriMonchiController` en puntos de spawn. Cola prioritaria `breederQueue` para criaturas en `BusyReason.Breeding` — se colocan directamente en su corral via `BreedingContainer.ReclaimDirect` en lugar de ser disparadas por el cañón. Al completarse una cría (`OnBreedingCompleted`), registra `birthOriginPen[child] = mother.HomePenKey` para que el recién nacido vuele desde el `LaunchPoint` del corral de origen.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[CreatureRegistrySO]], [[MoriMonchiController]], [[MoriMochiAgent]], [[MoriMonchiVisualizer]], [[PartVisualBankSO]]
