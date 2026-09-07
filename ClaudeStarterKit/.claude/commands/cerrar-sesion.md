---
description: Cierra sesion (actualiza Active Context, dispara vault-documenter, commit y push)
---

Cerra la sesion siguiendo el protocolo de CLAUDE.md:

1. Corre `git status --porcelain` y arma la lista de scripts `.cs` modificados o creados en
   esta sesion, marcando cada uno NUEVO o MODIFICADO.
2. Actualiza `<PROYECTO>Vault/Index/09 - Active Context.md`: que se toco esta sesion y cual
   es el siguiente paso. Al final de la nota deja la lista de `.cs` modificados/creados con
   su estado.
3. Si hubo `.cs` con cambio de contrato o responsabilidad, invoca el sub-agente
   `vault-documenter` (Agent tool, `subagent_type: vault-documenter`) pasandole esa lista
   en el formato:

   ```
   SCRIPTS TOCADOS EN ESTA SESION:
   - [ruta] -> [NUEVO | MODIFICADO] -> ScriptNodes/[NombreScript].md
   ```

   - Si fue solo un bug cosmetico sin cambio de contrato, omiti el vault-documenter y decimelo.
4. NO toques la wiki de diseno. El `notion-documenter` solo corre si yo te lo autorizo
   explicitamente esta sesion.
5. Commit y push: `git add -A`, commit con mensaje `S{N}: resumen de lo hecho` (cuerpo con
   vinetas por bloque) y `git push`. Si el push falla, deja el commit hecho y reportalo.
6. Reportame que quedo actualizado, el hash del commit y que quedo pendiente.
