---
description: Cierra sesion MoriMonchis (actualiza Active Context y dispara vault-documenter)
---

Cerra la sesion siguiendo el protocolo de CLAUDE.md (pasos 8-9):

1. Corré `git status --porcelain` y armá la lista de scripts `.cs` modificados o creados en esta sesion, marcando cada uno NUEVO o MODIFICADO.
2. Actualizá `MoriMonchiVault/Index/09 - Active Context.md`: qué se tocó esta sesion y cuál es el siguiente paso. Al final de la nota dejá la lista de `.cs` modificados/creados con su estado.
3. Si hubo `.cs` con cambio de contrato o responsabilidad, invocá el sub-agente `vault-documenter` (Agent tool, `subagent_type: vault-documenter`) pasándole esa lista en el formato:
   ```
   SCRIPTS TOCADOS EN ESTA SESION:
   - [ruta] -> [NUEVO | MODIFICADO] -> ScriptNodes/[NombreScript].md
   ```
   - Si fue solo un bug cosmético sin cambio de contrato, omití el vault-documenter y decímelo.
4. NO toques Notion. El `notion-documenter` solo corre si yo te lo autorizo explícitamente esta sesion.
5. Commit y push: `git add -A`, commit con mensaje `S{N}: resumen de lo hecho` (cuerpo con viñetas por bloque; trailers `Co-Authored-By` y `Claude-Session` del system-reminder) y `git push`. Si el push falla, dejá el commit hecho y reportalo.
6. Reportame qué quedó actualizado, el hash del commit y qué quedó pendiente.
