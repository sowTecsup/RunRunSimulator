param([Parameter(Mandatory)][string]$Names, [Parameter(Mandatory)][string]$Tag, [string]$Out = (Join-Path $env:TEMP 'monchi_parts'), [string]$Views = 'side,front')
$Blender = 'C:\Program Files\Blender Foundation\Blender 4.5\blender.exe'
& $Blender --background --factory-startup --python (Join-Path $PSScriptRoot 'build_set.py') -- $Names $Out $Tag $Views | Select-String 'SET_DONE|Traceback|Error'
