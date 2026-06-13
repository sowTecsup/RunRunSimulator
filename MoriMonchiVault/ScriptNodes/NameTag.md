---
tags: [memory-bank, script, player-world]
---

# NameTag.cs

**Ruta:** `World/NameTag.cs`

**Responsabilidad:** Label world-space UITK sobre criaturas. Billboard (opcional `uprightOnly`). Dos layouts: **pen** (glyph género + nombre + personalidad + corazón/timer si incubando) y **default** (nombre + estado busy/dead + intent + "[E] Acariciar" si reacción amistosa y jugador mirando). Muestra "Petting..." 1.5s tras acariciar. `CountdownText` para huevo (mm:ss / "¡Listo! [E]"). Distancia de visibilidad configurable.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiAgent]], [[CreatureDNA]]
