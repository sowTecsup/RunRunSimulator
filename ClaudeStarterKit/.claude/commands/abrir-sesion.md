---
description: Abre sesion de trabajo siguiendo el protocolo de CLAUDE.md
argument-hint: "[sistema o tarea opcional]"
---

Abri sesion de trabajo siguiendo el protocolo de CLAUDE.md (pasos 1-4):

1. Lee `<PROYECTO>Vault/Index/09 - Active Context.md` y resumime en 3-4 lineas el estado
   actual y el siguiente paso pendiente. (Si el hook SessionStart ya lo inyecto en
   contexto, no lo releas: usalo.)
2. Corre el health-check del editor (`unity status`) y decime si esta listo, compilando o
   caido. Si hay un segundo equipo en juego, hace `git fetch` y revisa si hay ramas nuevas.
3. Tarea de hoy: $ARGUMENTS
   - Si hay tarea, identifica el dominio con `<PROYECTO>Vault/00 - Index.md` y deci que
     nota `Index/XX` y que `ScriptNodes/*.md` vas a necesitar leer.
   - Si no hay tarea, preguntame en que vamos a trabajar.
4. Lee la nota Index del dominio y los ScriptNodes relevantes ANTES de abrir cualquier `.cs`.
5. NO abras codigo `.cs` ni escribas todavia. Presentame el plan y espera mi confirmacion.
