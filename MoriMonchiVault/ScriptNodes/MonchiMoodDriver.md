---
tags: [script, animation, behavior]
---

# MonchiMoodDriver.cs

**Ruta:** `World/Creatures/MonchiMoodDriver.cs`

**Responsabilidad:** Driver de emociones por estado interno del agente. Tick desincronizado cada 2.5-5s (per-creature stagger para evitar sincronización visual). Lee Condition (Sick → Enfermo, InNeed → Triste) e Intent (Resting → Dormido, Eating → Feliz, Playing → Emocionado, Held/Fleeing → Asustado, Tumbling → Mareado) del MoriMochiAgent, default mezcla 35% Feliz / 65% Neutral. No interfiere si DragonAnimationDriver.IsBusy (durante combate). Llama visualizer.SetMood() para cambiar la cara. Genera variedad visual orgánica sin afectar gameplay.

**Vinculado a:** [[Index/10 - Visualization]]

**Conexiones:** [[MonchiVisualizer]], [[MoriMochiAgent]], [[DragonAnimationDriver]], [[Enums]] (MonchiMood, CreatureCondition, CreatureIntent)
