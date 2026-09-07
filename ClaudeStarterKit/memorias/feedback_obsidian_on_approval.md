---
name: obsidian-write-on-approval
description: Escribir en el vault Obsidian (MoriMonchiVault) solo al cerrar diseño y con aprobación explícita
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 2548035f-914d-4562-8260-4ee1000d49f2
---

No actualizar el vault Obsidian (`MoriMonchiVault/`) a mitad de un diseño. Escribir **solo cuando el diseño está cerrado Y el usuario aprueba explícitamente**. Durante la teorización/iteración, las propuestas viven en la conversación, no en el vault.

**Why:** El usuario decide cuándo se documenta; refuerza el workflow de CLAUDE.md (proponer → validar → aplicar). Documenté decisiones mid-design sin aprobación y lo corrigió.
**How to apply:** Antes de tocar archivos del vault, confirmar diseño cerrado + obtener aprobación. El código (no-vault) y los memorias sí se pueden escribir con luz verde. Esto NO aplica a `09 - Active Context` al cierre de sesión, que igualmente se propone primero.
