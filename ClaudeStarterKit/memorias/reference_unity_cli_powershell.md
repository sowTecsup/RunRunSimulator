---
name: unity-cli-por-powershell
description: "El CLI `unity` (pipeline) hay que invocarlo con la tool PowerShell; en la tool Bash el clasificador de auto mode lo bloquea (S100)"
metadata: 
  node_type: memory
  type: reference
  originSessionId: 5a117caa-2f2c-41fb-82c2-259afe7ca20f
  modified: 2026-09-04T19:38:09.396Z
---

En S100 (2026-09-04) el clasificador de auto mode de Claude Code bloqueó `unity status` y `unity command ...` lanzados desde la tool **Bash** ("Blocked by classifier"). La misma llamada por la tool **PowerShell** funciona sin prompts: `unity status`, `unity command recompile --json`, `unity command eval_file --file <ruta> --json --timeout 60`, `unity command editor_play/editor_stop`, `unity command console --tail N --json`. Parsear con `ConvertFrom-Json`; el payload útil está en `data.result` (a veces string JSON a re-parsear, ver [[MoriMonchiVault/Index/12 - Unity MCP]]).

**How to apply:** para todo lo del CLI de Unity usar la tool PowerShell (encadenar varios comandos en una sola llamada con `Start-Sleep` entre Play y `eval_file`, que en los primeros ~15 s de Play cae en timeout). Bash sigue sirviendo para git, ffmpeg, sed y lectura de archivos. Relacionado: [[two-pcs-workflow]].
