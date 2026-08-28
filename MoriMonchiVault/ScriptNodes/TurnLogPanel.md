---
tags: [script, ui, uitk]
---

# TurnLogPanel.cs

**Ruta:** `CombatPrototype/TurnLogPanel.cs`

**Responsabilidad:** Panel UITK que renderiza el log de turnos ejecutados. Se suscribe a `CombatPrototypeManager.TurnLogChanged` y reconstruye la UI al recibirlo. Muestra turnos en orden inverso (más reciente arriba). Cabecera de turno en amarillo (#FFD24A) con estilo bold; líneas de evento en gris (#DDD), whiteSpace normal. Panel absolute positioned superior izquierda: **ancho 280px, altura máxima 560px** (S88), fondo oscuro semitransparente (#0D0F14E0), borde, `ScrollView` para contenido largo. Tamaños de fuente: 15 cabecera de turno, 13 líneas (S88). Se desactualiza (display: None) cuando el log está vacío. **Guarda anti-huérfano S88**: método `IsUiStale()` verifica si `panel == null` o `panel.panel != document.rootVisualElement.panel` (detecta reconstrucción); `Update()` llama `Rebuild()` si stale (auto-curación).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatPrototypeManager]], [[TurnLogEntry]]
