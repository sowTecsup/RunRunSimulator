---
tags: [script, editor, tool, expedition, brush]
---

# ArenaShapeBrushTool.cs

**Ruta:** `Editor/ArenaShapeBrushTool.cs`

**Responsabilidad:** Editor tool para interacción interactiva con ArenaShapeBrush. Permite pintar/borrar regiones en scene view con preview en tiempo real. Gestiona eventos de mouse, preview mesh, help box.

**Herencia:** EditorTool (target: ArenaShapeBrush)

**Propiedades:**
- `override GUIContent toolbarIcon` — icono en editor (lápiz) + tooltip

**Métodos:**
- `override void OnActivated()` — inicializa caché (version, maskLength) y stroke points
- `override void OnToolGUI(EditorWindow window)` — maneja eventos y renderizado

**Eventos Manejados:**

| Evento | Acción |
|--------|--------|
| MouseDown (LMB, !Alt) | Undo.Record, limpia stroke, pinta en hit, marca evento |
| MouseDrag (LMB) | Agrega punto a stroke, pinta, repaint |
| MouseUp (LMB) | Aplica máscara, limpia stroke, EditorUtility.SetDirty |
| KeyDown ([) | Reduce BrushRadius (min 0.5) |
| KeyDown (]) | Aumenta BrushRadius (+0.5) |
| MouseMove | Repaint |
| Repaint | Reconstruye preview mesh si versión cambió |

**Raycast:**
1. Plane(Vector3.up, shape.Center.y) 
2. GUIPointToWorldRay(mouse position)
3. Obtiene hit point

**DrawBrushCursor:**
- Dibuja wireDisc (outline del pincel en posición mouse)
- Color: azul claro (pintar), rojo claro (borrar/Shift)
- DrawSolidDisc semitransparente como fill

**DrawStrokePreview:**
- Dibuja discos en cada punto del stroke (0.15 alfa)
- Muestra trayectoria de pinceladas

**RebuildPreviewMesh:**
- Caché por version y maskLength
- Si no cambió, retorna sin reconstruir
- BuildPreviewMesh() genera malla de celdas activas

**BuildPreviewMesh:**
- Itera (j, i) en máscara
- Si mask[j*size+i] != 0: agrega quad (4 verts, 2 tris)
- Genera IndexFormat según vertex count

**DrawHelpBox:**
- Texto: "Pincel de sala · arrastrar pinta · Shift borra · [ ] radio (X.X m) · soltar aplica"
- Ubicación: esquina superior izquierda (10, 10)

**Invariantes:**
- Plano de raycast fijo en shape.Center.y (altura del piso)
- Stroke points limitados a 200 (evita lag)
- AppendStrokePoint() filtra puntos muy cercanos (< radius*0.35)
- Preview mesh se actualiza solo si versión o maskLength cambió

**S111 Nuevo:**
- Editor tool interactivo para pincelado de forma

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[ArenaShapeBrush]], [[ArenaShape]]
