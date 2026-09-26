param([Parameter(Mandatory)][string]$Name, [string]$Out = (Join-Path $env:TEMP 'monchi_parts'), [string]$Views = 'side,front', [string]$Letter = 'A')
$Blender = 'C:\Program Files\Blender Foundation\Blender 4.5\blender.exe'
& $Blender --background --factory-startup --python (Join-Path $PSScriptRoot 'build_part.py') -- $Name $Out $Views $Letter | Select-String 'PART_DONE|Traceback|Error'
