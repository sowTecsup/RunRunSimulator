---
tags: [script, world, combat]
---

# MoriMonchiCombatVisualizer.cs

**Ruta:** `World/Creatures/MoriMonchiCombatVisualizer.cs`

**Responsabilidad:** Derivada de `MoriMonchiVisualizer` que expone UnityEvents Feel para feedback visual/audio durante combate. Vive en el prefab del peleador dentro del Combat Visualizer. Los eventos están organizados en TabGroups de Odin (Ataque/Recibe/Estado): OnAttack, OnHitDealt, OnCritDealt (Ataque); OnHitTaken, OnCritTaken (Recibe); OnCombatStart, OnDead, OnVictory (Estado). OnHpChanged es una subclase `HpChangedEvent : UnityEvent<float, float>` (HP actual, HP máximo) para permitir tweening de barras directamente desde el inspector. Los 9 métodos `Play*()` invocan su evento correspondiente; el `CombatVisualizerService` los llama durante el replay vía los `CombatNode`.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[MoriMonchiVisualizer]], [[CombatVisualizerService]], [[CombatNode]]
