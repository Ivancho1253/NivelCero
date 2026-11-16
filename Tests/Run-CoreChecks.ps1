$ErrorActionPreference = 'Stop'
$workspacePath = Split-Path $PSScriptRoot -Parent
$sourcePaths = @(
    (Join-Path $workspacePath 'Unity/Assets/NivelCero/Scripts/Core/CircuitPuzzle.cs'),
    (Join-Path $workspacePath 'Unity/Assets/NivelCero/Scripts/Core/ProgressData.cs'),
    (Join-Path $PSScriptRoot 'CoreChecks.cs')
)
Add-Type -Path $sourcePaths
[CoreChecks]::Run()
