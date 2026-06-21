---
tags: [script, world]
---

# FurRenderer.cs

**Ruta:** `World/Creatures/FurRenderer.cs`

**Responsabilidad:** Renderiza pelaje volumétrico mediante técnica de shell-layers. Crea en tiempo de ejecución una jerarquía de GameObjects (cascaras) sobre el mesh original, cada una con su propio material parametrizado. Soporta dinámicas de viento, gravedad y rim-lighting en tiempo real; actualiza propiedades al validar en editor.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** Requiere `MeshFilter` en el mismo GameObject; consume un `Shader` asignado en inspector (acoplamiento débil, no importa cual).
