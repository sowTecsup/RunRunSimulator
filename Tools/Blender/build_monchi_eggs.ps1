param(
    [string]$Blender = 'C:\Program Files\Blender Foundation\Blender 4.5\blender.exe',
    [string]$Work = (Join-Path $env:TEMP 'monchi_blender')
)
$ErrorActionPreference = 'Stop'
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$here = $PSScriptRoot
$fbxDir = Join-Path $root 'Assets\Suriyun\Dragons_SD\FBX'
$pattern = Join-Path $root 'Assets\RunRunSimulator\Resources\Textures\MoriMochi\Patterns\MonchiPattern_00.png'
$models = Join-Path $root 'Assets\RunRunSimulator\Resources\Models\MoriMochi'
$eggTex = Join-Path $root 'Assets\RunRunSimulator\Resources\Textures\MoriMochi\Eggs'
New-Item -ItemType Directory -Force $Work | Out-Null

foreach ($mode in 'egg', 'slime') {
    & $Blender --background --factory-startup --python (Join-Path $here 'monchi_egg.py') -- $Work 'none' $mode | Select-String 'EGG shell|Traceback'
    $blend = Join-Path $Work ($(if ($mode -eq 'slime') { 'MonchiSlime.blend' } else { 'MonchiEgg.blend' }))
    & $Blender --background --factory-startup $blend --python (Join-Path $here 'transfer_skin.py') -- $Work $fbxDir $pattern $mode | Select-String 'NOSE|SINK|SCALES|SKIN_DONE|Traceback'
}
& $Blender --background --factory-startup (Join-Path $Work 'MonchiSlime_Skin.blend') --python (Join-Path $here 'rig_slime.py') -- $Work | Select-String 'RIG_DONE|Traceback'
& $Blender --background --factory-startup --python (Join-Path $here 'gen_scales.py') -- (Join-Path $Work 'EggScales.png') | Select-String 'SCALES_DONE|Traceback'

Copy-Item (Join-Path $Work 'MonchiEgg.fbx'), (Join-Path $Work 'MonchiSlime.fbx') $models -Force
Copy-Item (Join-Path $Work 'EggScales.png') $eggTex -Force
Write-Output "OK -> $models"
