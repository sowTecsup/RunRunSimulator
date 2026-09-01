---
tags: [script, visual, ui]
---

# MonchiMoodSetSO.cs

**Ruta:** `Data/MonchiMoodSetSO.cs`

**Responsabilidad:** Tabla de emociones-a-materiales de caras. Mapea cada `MonchiMood` (12 valores) a una lista de materiales de caras que pueden renderizar ese humor. `GetFace(mood)` selecciona aleatoriamente una cara de la lista del mood; cae back a Neutral si no hay listaencontrada. Botón editor `PopulateFromEnum` precarga entradas vacías para cada valor del enum MonchiMood sin perder datos existentes. Es un SO puro de configuración, editado en el inspector para cambiar qué caras rotan por humor sin tocar código.

**Vinculado a:** [[Index/10 - Visualization]]

**Conexiones:** [[MonchiVisualBankSO]], [[MonchiVisualizer]], Enums (`Core/Enums/`, S93) (MonchiMood)
