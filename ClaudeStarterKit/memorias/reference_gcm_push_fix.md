---
name: gcm-push-fix
description: "Causa raíz y fix del quirk \"git push se cuelga esperando el diálogo de GCM\" (S90/S94)"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 7e27f4e7-76d3-4a7c-9456-c0d8a5c9ba57
  modified: 2026-09-01T20:47:44.841Z
---

`git push` por HTTPS se colgaba en sesión desatendida porque Git Credential Manager tiene **dos cuentas** de GitHub guardadas (`KurusuDes` y `sowTecsup`) y abre un diálogo para elegir una. No hay `gh` instalado.

**Fix verificado (S94, 2026-09-01):** indicar la cuenta en el push, en modo no interactivo:
```powershell
$env:GCM_INTERACTIVE = 'never'; git -c credential.username=sowTecsup push origin main
```
El repo `RunRunSimulator` vive bajo `sowTecsup`. Alternativa permanente si Juan lo aprueba: `git config credential.https://github.com.username sowTecsup` en el repo.
