$ErrorActionPreference = 'SilentlyContinue'
$path = Join-Path $PSScriptRoot '..\..\MoriMonchiVault\Index\09 - Active Context.md'
if (Test-Path -LiteralPath $path) {
    "===== 09 - ACTIVE CONTEXT (inyectado por hook SessionStart) ====="
    Get-Content -LiteralPath $path -Raw
    "===== fin Active Context : este es el estado al abrir sesion ====="
}
