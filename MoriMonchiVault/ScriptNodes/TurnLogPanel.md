---
tags: [script, ui, uitk]
---

# TurnLogPanel.cs

**Ruta:** `CombatPrototype/TurnLogPanel.cs`

**Responsabilidad:** Panel UITK que renderiza el log de turnos ejecutados. Se suscribe a `CombatPrototypeManager.TurnLogChanged` y reconstruye la UI al recibirlo. Muestra turnos en orden inverso (más reciente arriba). Cabecera de turno en amarillo (#FFD24A) con estilo bold; líneas de evento en gris (#DDD), whiteSpace normal. El panel es absolute positioned en el rincón superior izquierdo con fondo oscuro semitransparente (#0D0F14E0), borde, y `ScrollView` para contenido largo. Se desactualiza (display: None) cuando el log está vacío.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatPrototypeManager]], [[TurnLogEntry]]
