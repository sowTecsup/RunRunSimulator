<#
gen-image.ps1 — genera sprites de UI desde los prompts .md de
Resources/Sprites/UI/Ideas usando la API de Gemini (Nano Banana).
Lee el prompt, el nombre destino y el aspect del propio .md.

Uso:
  $env:GEMINI_API_KEY = "tu-key"        # o: setx GEMINI_API_KEY "tu-key" (persiste)
  ./Tools/gen-image.ps1 slot-frame      # una pieza  (Ideas/slot-frame.md)
  ./Tools/gen-image.ps1 -All            # todas
  ./Tools/gen-image.ps1                 # lista las ideas disponibles
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0)] [string] $Idea,
    [switch] $All,
    [string] $Model = "gemini-3.1-flash-image-preview"
)

$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# La key sale del entorno; si no está, de Tools/gemini.key (ignorado por git).
$key = $env:GEMINI_API_KEY
if ([string]::IsNullOrWhiteSpace($key)) {
    $keyFile = Join-Path $PSScriptRoot "gemini.key"
    if (Test-Path $keyFile) { $key = (Get-Content -LiteralPath $keyFile -Raw).Trim() }
}
if ([string]::IsNullOrWhiteSpace($key)) {
    Write-Error "Falta la API key. Opcion A: setx GEMINI_API_KEY '...'  |  Opcion B: pega la key en Tools/gemini.key (ya esta gitignored)."
}

$root     = Split-Path -Parent $PSScriptRoot
$ideasDir = Join-Path $root "Assets/RunRunSimulator/Resources/Sprites/UI/Ideas"
$outDir   = Join-Path $root "Assets/RunRunSimulator/Resources/Sprites/UI"

# Extrae el blockquote (líneas '>') bajo un encabezado "## <Heading>".
function Get-Section {
    param([string[]] $Lines, [string] $Heading)
    $start = -1
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match "^\s*##\s*$Heading\b") { $start = $i + 1; break }
    }
    if ($start -lt 0) { return $null }
    $buf = @()
    for ($i = $start; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -match '^\s*>') { $buf += ($Lines[$i] -replace '^\s*>\s?', '') }
        elseif ($buf.Count -gt 0) { break }
    }
    return ($buf -join ' ').Trim()
}

# Dirección de arte compartida (Ideas/_style.md → "## Style"), antepuesta a cada prompt.
$styleFile = Join-Path $ideasDir "_style.md"
$style = if (Test-Path $styleFile) { Get-Section -Lines (Get-Content -LiteralPath $styleFile) -Heading 'Style' } else { "" }

function Invoke-Idea {
    param([string] $Path)

    $lines = Get-Content -LiteralPath $Path
    $text  = $lines -join "`n"

    $prompt = Get-Section -Lines $lines -Heading 'Prompt'
    if ([string]::IsNullOrWhiteSpace($prompt)) { Write-Warning "Sin sección '## Prompt' en $Path"; return }
    if (-not [string]::IsNullOrWhiteSpace($style)) { $prompt = "$style`n`n$prompt" }

    $dest = [regex]::Match($text, 'Resources/Sprites/UI/([A-Za-z0-9_\-]+\.png)')
    if (-not $dest.Success) { Write-Warning "Sin 'PNG destino' en $Path"; return }
    $outName = $dest.Groups[1].Value

    $asp    = [regex]::Match($text, '\*\*Aspect:\*\*\s*([0-9]+:[0-9]+)')
    $aspect = if ($asp.Success) { $asp.Groups[1].Value } else { "1:1" }

    $body = @{
        contents         = @(@{ parts = @(@{ text = $prompt }) })
        generationConfig = @{
            responseModalities = @('IMAGE')
            imageConfig        = @{ aspectRatio = $aspect }
        }
    } | ConvertTo-Json -Depth 12

    $uri = "https://generativelanguage.googleapis.com/v1beta/models/${Model}:generateContent"
    Write-Host "-> $([IO.Path]::GetFileName($Path))  ($aspect)  => $outName" -ForegroundColor Cyan

    $bytes = [Text.Encoding]::UTF8.GetBytes($body)
    try {
        $resp = Invoke-RestMethod -Uri $uri -Method Post -ContentType 'application/json; charset=utf-8' `
            -Headers @{ 'x-goog-api-key' = $key } -Body $bytes
    }
    catch {
        $r = $_.Exception.Response
        if ($r) {
            $sr = New-Object IO.StreamReader($r.GetResponseStream())
            Write-Warning ("API HTTP $([int]$r.StatusCode): " + $sr.ReadToEnd())
        }
        else { Write-Warning $_.Exception.Message }
        return
    }

    $parts = $resp.candidates[0].content.parts
    $img   = $parts | Where-Object { $_.inlineData -or $_.inline_data } | Select-Object -First 1
    if (-not $img) {
        $txt = ($parts | Where-Object { $_.text } | ForEach-Object { $_.text }) -join "`n"
        Write-Warning "Sin imagen en la respuesta. Texto del modelo: $txt"
        return
    }

    $data = if ($img.inlineData) { $img.inlineData.data } else { $img.inline_data.data }
    if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir | Out-Null }
    $outPath = Join-Path $outDir $outName
    [IO.File]::WriteAllBytes($outPath, [Convert]::FromBase64String($data))
    Write-Host "   OK $outPath" -ForegroundColor Green

    # Post-proceso: si el .md pide chroma key (**Key:** green), volver el verde en alfa.
    $keyFlag = [regex]::Match($text, '\*\*Key:\*\*\s*(\S+)')
    if ($keyFlag.Success -and $keyFlag.Groups[1].Value -notmatch '^(none|no)$') {
        & (Join-Path $PSScriptRoot 'key-transparency.ps1') -Path $outPath
    }
}

if ($All) {
    Get-ChildItem -LiteralPath $ideasDir -Filter *.md |
        Where-Object { $_.Name -notlike '_*' } |
        ForEach-Object { Invoke-Idea -Path $_.FullName }
}
elseif ($Idea) {
    $p = Join-Path $ideasDir (($Idea -replace '\.md$', '') + ".md")
    if (-not (Test-Path $p)) { Write-Error "No existe $p" }
    Invoke-Idea -Path $p
}
else {
    Write-Host "Uso: gen-image.ps1 <idea>  |  gen-image.ps1 -All`nIdeas disponibles:"
    Get-ChildItem -LiteralPath $ideasDir -Filter *.md |
        Where-Object { $_.Name -notlike '_*' } |
        ForEach-Object { Write-Host ("  - " + $_.BaseName) }
}
