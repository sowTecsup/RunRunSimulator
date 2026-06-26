<#
key-transparency.ps1 — convierte un fondo chroma (verde puro #00FF00) en
transparencia real (alfa). Pensado para los PNG opacos de Nano Banana:
se genera con fondo + hueco en verde y este script lo vuelve PNG 32bpp con alfa.

Uso:
  ./Tools/key-transparency.ps1 -Path "Assets/.../equip_slot_frame.png"
  ./Tools/key-transparency.ps1 -Path img.png -GThreshold 110 -Margin 35
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $Path,
    [string] $Out,
    [int] $GThreshold = 110,   # G minimo para considerar el pixel "verde"
    [int] $Margin     = 35     # cuanto debe superar G a R y a B
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
if ([string]::IsNullOrWhiteSpace($Out)) { $Out = $Path }

# Cargar desde bytes (no deja el archivo bloqueado, así podemos sobrescribirlo).
$bytesIn = [IO.File]::ReadAllBytes($Path)
$ms  = New-Object IO.MemoryStream (, $bytesIn)
$src = [System.Drawing.Bitmap]::new($ms)
$w = $src.Width; $h = $src.Height
$rect = [System.Drawing.Rectangle]::new(0, 0, $w, $h)
$fmt  = [System.Drawing.Imaging.PixelFormat]::Format32bppArgb

$dst = [System.Drawing.Bitmap]::new($w, $h, $fmt)
$sd = $src.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly,  $fmt)
$od = $dst.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::WriteOnly, $fmt)
$n = $w * $h * 4
$buf = New-Object byte[] $n
[System.Runtime.InteropServices.Marshal]::Copy($sd.Scan0, $buf, 0, $n)

# Orden BGRA en memoria. Verde dominante => fondo (alfa 0). Resto opaco + de-spill del halo.
$keyed = 0
for ($i = 0; $i -lt $n; $i += 4) {
    $b = $buf[$i]; $g = $buf[$i + 1]; $r = $buf[$i + 2]
    if ($g -ge $GThreshold -and ($g - $r) -ge $Margin -and ($g - $b) -ge $Margin) {
        $buf[$i] = 0; $buf[$i + 1] = 0; $buf[$i + 2] = 0; $buf[$i + 3] = 0
        $keyed++
    }
    else {
        $buf[$i + 3] = 255
        if ($g -gt $r -and $g -gt $b) {
            $m = [Math]::Max($r, $b); $buf[$i + 1] = [byte]$m
        }
    }
}

[System.Runtime.InteropServices.Marshal]::Copy($buf, 0, $od.Scan0, $n)
$src.UnlockBits($sd); $dst.UnlockBits($od)
$src.Dispose(); $ms.Dispose()

$dst.Save($Out, [System.Drawing.Imaging.ImageFormat]::Png)
$dst.Dispose()

$pct = [Math]::Round(100 * $keyed / ($w * $h), 1)
Write-Host "   alfa: $keyed px transparentes ($pct%) -> $([IO.Path]::GetFileName($Out))" -ForegroundColor Green
