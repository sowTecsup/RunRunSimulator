---
description: Abre sesion de trabajo MoriMonchis siguiendo el protocolo de CLAUDE.md
argument-hint: "[sistema o tarea opcional]"
---

Abri sesion de trabajo siguiendo el protocolo de CLAUDE.md (pasos 1-4):

1. Lee `MoriMonchiVault/Index/09 - Active Context.md` y resumime en 3-4 lineas el estado actual y el siguiente paso pendiente. (Si el hook SessionStart ya lo inyecto en contexto, no lo releas: usalo.)
2. Tarea de hoy: $ARGUMENTS
   - Si hay tarea, identifica el dominio con `MoriMonchiVault/00 - Index.md` y deci que nota `Index/XX` y que `ScriptNodes/*.md` vas a necesitar leer.
   - Si no hay tarea, preguntame en que vamos a trabajar.
3. Lee la nota Index del dominio y los ScriptNodes relevantes ANTES de abrir cualquier `.cs`.
4. NO abras codigo `.cs` ni escribas todavia. Presentame el plan (paso 5 del protocolo) y espera mi confirmacion.

**Tope del brief de plan: 150 palabras**, mas la lista numerada de decisiones que tomo yo. Ver `## Estilo de respuesta` en CLAUDE.md.

- **Dentro**: la regla del cambio en una frase; los lotes por nombre y cual se corta si falta tiempo; que muta fuera de codigo y necesita mi OK; mis decisiones numeradas.
- **Fuera**: la lista de notas y ScriptNodes que leiste; tablas de diseno completas; nombres de metodos, campos y archivos por lote; el estado previo (ya esta en contexto).
- Cerrar con "el detalle sale a pedido". Si lo pido, ahi si desarrollas.
