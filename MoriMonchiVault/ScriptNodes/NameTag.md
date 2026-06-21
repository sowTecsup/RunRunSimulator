---
tags: [script, world]
---

# NameTag.cs

**Ruta:** `World/Creatures/NameTag.cs`

**Responsabilidad:** Label world-space UITK sobre criaturas. Billboard (opcional `uprightOnly`). Dos layouts: **pen** (glyph género + nombre + personalidad + etapa+días + contador crías + corazón/timer si incubando, elevación `penRaise` y escala `penScale` para no clipar el suelo) y **default** (nombre + etapa+días + estado busy/dead + intent + "[E] Acariciar" si reacción amistosa y jugador mirando). Muestra "Petting..." 1.5s tras acariciar. `CountdownText` para huevo (mm:ss / "¡Listo! [E]"). Distancia de visibilidad configurable. Lee `LifeStageTable` de `BreedingController.Instance` para traducir `AgeDays` a etapa.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiAgent]], [[CreatureDNA]]
