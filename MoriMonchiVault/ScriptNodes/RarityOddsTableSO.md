---
tags: [script, genetics]
---

# RarityOddsTableSO.cs

**Ruta:** `Data/Genetics/RarityOddsTableSO.cs`

**Responsabilidad:** Table de probabilidades normalizada para samplear rareza de criaturas. Expone diccionario editable (Common a Legendary) con pesos relativos. Método `Roll()` realiza weighted random sampling; proporciona visualización en tiempo de editor de odds efectivos (%).

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** Consumido por generadores de DNA (spawn/breeding) para determinar `Rarity` del resultado.
