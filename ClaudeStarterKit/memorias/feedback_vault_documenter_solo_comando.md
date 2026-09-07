---
name: vault-documenter-solo-comando
description: NUNCA disparar vault-documenter por iniciativa propia — solo cuando Juan lo pide explícitamente (comando /cerrar-sesion)
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 3579421f-1498-4af2-bf08-9116259c4d00
  modified: 2026-07-21T19:49:26.526Z
---

Juan revocó (2026-07-21, S59) la autorización permanente del paso 10 de CLAUDE.md: el sub-agente `vault-documenter` NO se ejecuta automáticamente al cierre de sesión.

**Why:** Juan quiere controlar cuándo se documenta el vault; lo dispara él con su comando dedicado (`/cerrar-sesion`). CLAUDE.md decía "autorizado, ejecutar siempre al cierre" — esa regla quedó obsoleta y puede inducir a error hasta que se actualice.

**How to apply:** Al cerrar sesión, actualizar `09 - Active Context` normalmente, pero NO invocar `vault-documenter` salvo pedido explícito de Juan en esa sesión (típicamente vía `/cerrar-sesion`). Igual criterio que ya regía para [[notion-documenter]].
