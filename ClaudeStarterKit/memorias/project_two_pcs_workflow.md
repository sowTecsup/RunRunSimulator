---
name: two-pcs-workflow
description: Juan alterna entre la PC del trabajo (Docente) y la PC de casa (PC2, junction C:\Users\USUARIO\Documents\GitHub -> E:\GitHub); al abrir sesion hacer git fetch y revisar ramas s<NN>-pc2 antes de asumir estado
metadata:
  type: project
---

Juan trabaja el proyecto en dos PCs: la del trabajo (`C:\Users\Docente\Desktop\UnityProyects\RunRunSimulator`, usuario git Sowtank) y la de casa (PC2, junction `C:\Users\USUARIO\Documents\GitHub\RunRunSimulator` → `E:\GitHub\RunRunSimulator`). Una sesion puede quedar sin cerrar ni pushear en una PC y continuar en la otra; lo de la otra PC llega como rama `s<NN>-pc2` (ej. `s95-pc2`, 2026-09-01/02, fast-forward sobre `main`).

**Why:** en S95 (2026-09-02) el trabajo local sin commitear de la PC del trabajo (E1-E2 con `HornPower` tirado) quedo superado por lo hecho en casa (potencial heredado); se descarto (Juan pidio borrar toda rama que no fuera `main`, incluido el respaldo `wip/s94-pc1-descartado`) y `main` se hizo fast-forward a `s95-pc2` (fe72738). Politica de ramas: solo vive `main`; las ramas `s<NN>-pc2` se borran apenas se mergean.

**How to apply:** al abrir sesion en cualquier PC: `git fetch --all --prune`, listar ramas `s*-pc2`/`wip/*`, comparar mtime local vs fechas de commit antes de descartar nada; tras un checkout que cambie la escena abierta, Unity muestra el modal "The open scene(s) have been modified externally" y el MCP no responde al ping hasta que Juan aprieta **Reload**. El CLI `unity` de la PC del trabajo esta en 1.0.0-beta.7 y rechaza el paquete `com.unity.pipeline` 0.5.0-exp.1 del proyecto ("too old to parse command lines"); la PC2 tiene beta.6 y funciona — decision de Juan si correr `unity pipeline upgrade`. Push con [[gcm-push-fix]].
